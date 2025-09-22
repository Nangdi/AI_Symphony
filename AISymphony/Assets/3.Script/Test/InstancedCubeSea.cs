using UnityEngine;
using System.Collections.Generic;

public class InstancedCubeSea : MonoBehaviour
{
    // ───────── Refs ─────────
    [Header("Refs")]
    public Mesh cubeMesh;               // 임시 Cube의 MeshFilter.sharedMesh 드래그
    public Material instancedMaterial;  // 인스턴싱 지원 머티리얼 (Enable GPU Instancing ON)

    // ───────── Grid ─────────
    [Header("Grid")]
    public int gridX = 320;             // 총 개수 = gridX * gridZ (예: 160x80=12,800)
    public int gridZ = 160;
    public float spacing = 1f;
    public float baseSize = 0.22f;      // 두께/길이의 기본 스케일

    // ───────── Base Wave (사인파) ─────────
    [Header("Wave")]
    public float freqX = 0.7f, freqZ = 1.1f;
    public float speedX = 1.0f, speedZ = 0.7f;
    public float amplitude = 0.4f;

    // ───────── Follow (리드/래그) ─────────
    [Header("Follow (lead/lag)")]
    public float leadRangeSec = 0.30f;  // 큐브별 시간 오프셋 범위(±초)
    public float followSpeedMin = 4f;   // 느리게 따라가기 하한
    public float followSpeedMax = 8f;   // 빠르게 따라가기 상한

    // ───────── Shape (세로 기둥) ─────────
    [Header("Shape (pillar)")]
    public float pillarHeight = 5.0f;   // 세로 길이 배수
    public float pillarThickness = 0.14f; // X/Z 두께 배수 (가늘게 보이게 기본 0.14 권장)
    public bool anchorBottomToSurface = false; // true면 바닥이 수면(yCur)에 붙고 위로만 늘어남

    // ───────── Note 반응: 가운데 최고 ─────────
    [Header("Note Pulse (center strongest)")]
    public int notesX = 32;
    public int pitchesZ = 8;
    public float noteRadius = 1.5f;      // 반응 반경(월드)
    public float pulseScaleMax = 0.8f;   // 펄스 최대치(= Y 길이에만 적용)
    public float falloffExp = 2.0f;      // (1 - d/r)^exp (2~3 권장)
    public float scaleDecay = 2.5f;      // 펄스 감쇠 속도(/s)

    // ───────── 내부 버퍼 ─────────
    Matrix4x4[] matrices;
    Vector4[] colors;       // per-instance 색상 (_Color)
    Vector3[] basePos;      // 기준 위치
    float[] pulse;        // 펄스(=Y 길이에만 쓰임)
    float[] yCur;         // 스무딩된 현재 y
    float[] leadSec;      // 큐브별 시간 오프셋
    float[] followSpeed;  // 큐브별 추종 속도

    const int BATCH = 1023;
    List<Matrix4x4[]> batchMatrices;
    List<Vector4[]> batchColors;
    List<MaterialPropertyBlock> batchMPB;

    void Awake()
    {
        if (!cubeMesh || !instancedMaterial)
        {
            Debug.LogError("cubeMesh / instancedMaterial 를 할당하세요.");
            enabled = false; return;
        }
        instancedMaterial.enableInstancing = true;

        int count = gridX * gridZ;

        matrices = new Matrix4x4[count];
        colors = new Vector4[count];
        basePos = new Vector3[count];
        pulse = new float[count];
        yCur = new float[count];
        leadSec = new float[count];
        followSpeed = new float[count];

        var rnd = new System.Random(12345);
        for (int i = 0; i < count; i++)
        {
            float rLead = (float)rnd.NextDouble() * 2f - 1f;          // -1..+1
            leadSec[i] = rLead * leadRangeSec;
            followSpeed[i] = Mathf.Lerp(followSpeedMin, followSpeedMax, (float)rnd.NextDouble());
            yCur[i] = 0f; pulse[i] = 0f;
        }

        // 격자 배치(가운데 정렬)
        float x0 = -(gridX - 1) * 0.5f * spacing;
        float z0 = -(gridZ - 1) * 0.5f * spacing;

        int idx = 0;
        for (int z = 0; z < gridZ; z++)
            for (int x = 0; x < gridX; x++, idx++)
            {
                var p = new Vector3(x0 + x * spacing, 0f, z0 + z * spacing);
                basePos[idx] = transform.TransformPoint(p);
                matrices[idx] = Matrix4x4.TRS(basePos[idx], Quaternion.identity, Vector3.one * baseSize);
                colors[idx] = new Vector4(0.9f, 0.9f, 1.0f, 1);
            }

        // 배치 쪼개기
        batchMatrices = new List<Matrix4x4[]>();
        batchColors = new List<Vector4[]>();
        batchMPB = new List<MaterialPropertyBlock>();
        for (int start = 0; start < count; start += BATCH)
        {
            int n = Mathf.Min(BATCH, count - start);
            batchMatrices.Add(new Matrix4x4[n]);
            batchColors.Add(new Vector4[n]);
            batchMPB.Add(new MaterialPropertyBlock());
        }
    }

    void Update()
    {
        float t = Time.time;
        int count = matrices.Length;

        for (int i = 0; i < count; i++)
        {
            Vector3 bp = basePos[i];

            // 리드/래그 적용된 시간
            float tInst = t + leadSec[i];

            // 사인파 타깃
            float xPhase = bp.x * freqX + tInst * speedX;
            float zPhase = bp.z * freqZ + tInst * speedZ;
            float yTarget = (Mathf.Sin(xPhase) + Mathf.Sin(zPhase)) * amplitude;

            // 스무딩 추종
            float alpha = Mathf.Clamp01(followSpeed[i] * Time.deltaTime);
            yCur[i] = Mathf.Lerp(yCur[i], yTarget, alpha);

            // ── Y만 길어지게(두께 X/Z 고정) ──
            float growY = 1f + pulse[i];                    // 펄스는 Y 길이에만 반영
            float sx = baseSize * pillarThickness;          // X 두께 고정
            float sy = baseSize * pillarHeight * growY;     // Y 길이만 증가
            float sz = baseSize * pillarThickness;          // Z 두께 고정
            Vector3 scale = new Vector3(sx, sy, sz);

            // 중심 Y (anchorBottomToSurface면 바닥을 수면에 붙임)
            float centerY = anchorBottomToSurface ? yCur[i] + sy * 0.5f : yCur[i];

            matrices[i].SetTRS(new Vector3(bp.x, centerY, bp.z), Quaternion.identity, scale);

            // 색상은 펄스에 맞춰 살짝 밝게 (선택사항)
            float boost = Mathf.Clamp01(pulse[i]);
            colors[i] = (Vector4)Color.Lerp(new Color(0.9f, 0.9f, 1.0f, 1), Color.white, boost);

            // 펄스 감쇠
            pulse[i] = Mathf.Max(0f, pulse[i] - Time.deltaTime * scaleDecay);
        }

        // 배치 드로우
        int cursor = 0;
        for (int b = 0; b < batchMatrices.Count; b++)
        {
            var mArr = batchMatrices[b];
            var cArr = batchColors[b];
            int n = mArr.Length;

            System.Array.Copy(matrices, cursor, mArr, 0, n);
            System.Array.Copy(colors, cursor, cArr, 0, n);
            cursor += n;

            var mpb = batchMPB[b];
            mpb.Clear();
            mpb.SetVectorArray("_Color", cArr); // 셰이더가 _Color 인스턴싱 지원 시 적용
            Graphics.DrawMeshInstanced(
                cubeMesh, 0, instancedMaterial, mArr, n, mpb,
                UnityEngine.Rendering.ShadowCastingMode.Off, false,
                gameObject.layer, null, UnityEngine.Rendering.LightProbeUsage.Off
            );
        }

        // 간단 테스트: Q키로 1..32 순서 & 1..8 랜덤 피치
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _testNote = (_testNote % notesX) + 1;                 // 1..notesX
            int ranPitch = Random.Range(1, pitchesZ + 1);         // 1..pitchesZ
            OnNotePlayed(_testNote, ranPitch);
        }
    }
    int _testNote = 0;

    /// 외부에서 호출: noteIndex=1..32, pitch=1..8
    public void OnNotePlayed(int noteIndex, int pitch)
    {
        // 1) 입력 클램프
        noteIndex = Mathf.Clamp(noteIndex, 1, notesX);
        pitch = Mathf.Clamp(pitch, 1, pitchesZ);

        // 2) 노트/음계 → 월드 좌표(격자 중앙 정렬)
        float u = (noteIndex - 0.5f) / notesX;
        float v = (pitch - 0.5f) / pitchesZ;

        float minX = basePos[0].x;
        float maxX = basePos[gridX - 1].x;
        float minZ = basePos[0].z;
        float maxZ = basePos[(gridZ - 1) * gridX].z;

        float cx = Mathf.Lerp(minX, maxX, u);
        float cz = Mathf.Lerp(minZ, maxZ, v);

        // 3) 중심에서 가까울수록 강하게 (가운데 최고)
        float r = noteRadius;
        float r2 = r * r;

        for (int i = 0; i < basePos.Length; i++)
        {
            Vector3 bp = basePos[i];
            float dx = bp.x - cx;
            float dz = bp.z - cz;
            float dist2 = dx * dx + dz * dz;
            if (dist2 > r2) continue;

            // (1 - d/r)^falloffExp : 중심=1, 반경 r에서 0
            float dist = Mathf.Sqrt(dist2);
            float w = Mathf.Pow(1f - Mathf.Clamp01(dist / r), falloffExp);

            // ✅ Y 길이에만 적용되는 펄스(두께 X/Z는 고정)
            pulse[i] = Mathf.Max(pulse[i], w * pulseScaleMax);
        }
    }
}
