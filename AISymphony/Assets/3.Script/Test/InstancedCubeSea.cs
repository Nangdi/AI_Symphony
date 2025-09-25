using UnityEngine;
using System.Collections.Generic;

public class InstancedCubeSea : MonoBehaviour
{
    // ───────── Refs ─────────
    [Header("Refs")]
    public Mesh cubeMesh;
    public Material instancedMaterial;   // 머티리얼에 Enable GPU Instancing 체크!

    // ───────── Grid ─────────
    [Header("Grid")]
    public int gridX = 160;
    public int gridZ = 80;
    public float spacing = 0.5f;
    public float baseSize = 0.22f;

    // ───────── Wave (사인파) ─────────
    [Header("Wave")]
    public float freqX = 0.7f, freqZ = 1.1f;
    public float speedX = 1.0f, speedZ = 0.7f;
    public float amplitude = 0.4f;

    // ───────── Follow (리드/래그) ─────────
    [Header("Follow (lead/lag)")]
    public float leadRangeSec = 0.30f;
    public float followSpeedMin = 4f;
    public float followSpeedMax = 8f;

    // ───────── Shape (세로 기둥) ─────────
    [Header("Shape (pillar)")]
    public float pillarHeight = 5.0f;
    public float pillarThickness = 0.14f;
    public bool anchorBottomToSurface = false;

    // ───────── Note Pulse ─────────
    [Header("Note Pulse (center strongest)")]
    public int notesX = 32;
    public int pitchesZ = 8;
    public float noteRadius = 1.5f;          // 반응 반경(월드)
    public float pulseScaleMax = 0.8f;       // Y 길이만 적용되는 최대치
    public float falloffExp = 2.2f;          // (1 - d/r)^exp

    [Header("Pulse Decay Only")]
    public float pulseDecay = 2.5f;          // ▼ 내려가는 속도(/s)만 조절

    // ───────── Tech Palette (어두운 톤) ─────────
    [Header("Tech Palette")]
    [Range(0, 1)] public float centerHue = 0.56f; // 시안/블루 중심
    [Range(0, 0.5f)] public float hueRange = 0.08f;
    public float hueNoiseScale = 0.08f;
    public float hueSpeed = 0.10f;
    [Range(0, 1)] public float saturation = 1.0f;
    [Range(0, 1)] public float valueBase = 0.30f;     // 어둡게
    [Range(0, 1)] public float valuePulseBoost = 0.30f;

    // ───────── Pulse Bar (직선 조명) ─────────
    [Header("Pulse Bar (optional)")]
    public Transform pulseBar;                 // 세로 쿼드/실린더 등(Emission 머티리얼 권장)
    public float barWidth = 0.12f;
    public float barLength = 3.0f;
    public float barUpSpeed = 2.5f;            // 올라가는 속도
    public float barDownSpeed = 1.8f;          // 내려오는 속도
    public float barFadePerSec = 1.6f;         // 서서히 사라짐
    public float barHeightOffset = 0.0f;
    public bool barColorByPitch = true;       // 음계에 맞춰 색

    // ───────── 내부 버퍼 ─────────
    Matrix4x4[] matrices;
    Vector4[] colors;          // per-instance _Color
    Vector3[] basePos;
    float[] pulse;           // 현재 펄스(0..)
    float[] pulseTime;       // 펄스 시작 시각(셰이더/바 이동용)
    float[] yCur;
    float[] leadSec;
    float[] followSpeed;
    float[] hueOffset;

    const int BATCH = 1023;
    List<Matrix4x4[]> batchMatrices;
    List<Vector4[]> batchColors;
    List<float[]> batchPulse;
    List<float[]> batchPulseTime;
    List<MaterialPropertyBlock> batchMPB;

    // 바 라이트 상태
    int lastCenterIdx = -1;
    float lastCenterTime = 0f;
    float barIntensity = 0f;
    Renderer barRenderer;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    static float N2(float x, float y) => Mathf.PerlinNoise(x, y);

    void Awake()
    {
        if (!cubeMesh || !instancedMaterial) { Debug.LogError("cubeMesh / instancedMaterial 미할당"); enabled = false; return; }
        instancedMaterial.enableInstancing = true;

        int count = gridX * gridZ;
        matrices = new Matrix4x4[count];
        colors = new Vector4[count];
        basePos = new Vector3[count];
        pulse = new float[count];
        pulseTime = new float[count];
        yCur = new float[count];
        leadSec = new float[count];
        followSpeed = new float[count];
        hueOffset = new float[count];

        var rnd = new System.Random(12345);
        for (int i = 0; i < count; i++)
        {
            leadSec[i] = ((float)rnd.NextDouble() * 2f - 1f) * leadRangeSec;
            followSpeed[i] = Mathf.Lerp(followSpeedMin, followSpeedMax, (float)rnd.NextDouble());
            hueOffset[i] = (float)rnd.NextDouble();
            pulse[i] = 0f; yCur[i] = 0f; pulseTime[i] = 0f;
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
                colors[idx] = new Vector4(1, 1, 1, 1);
            }

        // 배치
        batchMatrices = new List<Matrix4x4[]>();
        batchColors = new List<Vector4[]>();
        batchPulse = new List<float[]>();
        batchPulseTime = new List<float[]>();
        batchMPB = new List<MaterialPropertyBlock>();
        for (int start = 0; start < count; start += BATCH)
        {
            int n = Mathf.Min(BATCH, count - start);
            batchMatrices.Add(new Matrix4x4[n]);
            batchColors.Add(new Vector4[n]);
            batchPulse.Add(new float[n]);
            batchPulseTime.Add(new float[n]);
            batchMPB.Add(new MaterialPropertyBlock());
        }

        if (pulseBar)
        {
            barRenderer = pulseBar.GetComponentInChildren<Renderer>();
            pulseBar.localScale = new Vector3(barWidth, barLength, barWidth);
        }
    }

    // 에디터 테스트용
    int _testNote = 0;

    void Update()
    {
        float t = Time.time;
        int count = matrices.Length;

        for (int i = 0; i < count; i++)
        {
            Vector3 bp = basePos[i];

            // 리드/래그 시간
            float tInst = t + leadSec[i];

            // 사인파 목표
            float yTarget = (Mathf.Sin(bp.x * freqX + tInst * speedX) + Mathf.Sin(bp.z * freqZ + tInst * speedZ)) * amplitude;

            // 스무딩 추종
            float alpha = Mathf.Clamp01(followSpeed[i] * Time.deltaTime);
            yCur[i] = Mathf.Lerp(yCur[i], yTarget, alpha);

            // ── 펄스: 즉시 상승, 서서히 감쇠 ──
            pulse[i] = Mathf.Max(0f, pulse[i] - pulseDecay * Time.deltaTime);

            // ── Y만 길어지게 ──
            float sx = baseSize * pillarThickness;
            float sy = baseSize * pillarHeight * (1f + pulse[i]);
            float sz = baseSize * pillarThickness;
            float centerY = anchorBottomToSurface ? yCur[i] + sy * 0.5f : yCur[i];
            matrices[i].SetTRS(new Vector3(bp.x, centerY, bp.z), Quaternion.identity, new Vector3(sx, sy, sz));

            // ── 어두운 테크 팔레트 ──
            float hn = N2(bp.x * hueNoiseScale + t * hueSpeed, bp.z * hueNoiseScale + hueOffset[i]); // 0..1
            float h = Mathf.Clamp01(centerHue + (hn - 0.5f) * 2f * hueRange);
            float v = Mathf.Clamp01(valueBase + valuePulseBoost * Mathf.Clamp01(pulse[i]));
            Color c = Color.HSVToRGB(h, saturation, v);
            colors[i] = new Vector4(c.r, c.g, c.b, 1f);
        }

        // 배치 드로우(+ 인스턴스 데이터 전달)
        int cursor = 0;
        for (int b = 0; b < batchMatrices.Count; b++)
        {
            var mArr = batchMatrices[b];
            var cArr = batchColors[b];
            var pArr = batchPulse[b];
            var tArr = batchPulseTime[b];
            int n = mArr.Length;

            System.Array.Copy(matrices, cursor, mArr, 0, n);
            System.Array.Copy(colors, cursor, cArr, 0, n);
            System.Array.Copy(pulse, cursor, pArr, 0, n);
            System.Array.Copy(pulseTime, cursor, tArr, 0, n);
            cursor += n;

            var mpb = batchMPB[b];
            mpb.Clear();
            mpb.SetVectorArray("_Color", cArr);
            mpb.SetFloatArray("_Pulse", pArr);
            mpb.SetFloatArray("_PulseTime", tArr);
            Graphics.DrawMeshInstanced(
                cubeMesh, 0, instancedMaterial, mArr, n, mpb,
                UnityEngine.Rendering.ShadowCastingMode.Off, false,
                gameObject.layer, null, UnityEngine.Rendering.LightProbeUsage.Off
            );
        }

        // ── Pulse Bar(직선 조명) 이동/페이드 ──
        if (pulseBar && lastCenterIdx >= 0)
        {
            float tSince = Time.time - lastCenterTime;
            // 위로 갔다가 내려오는 비대칭 삼각 파
            float upT = Mathf.Max(1e-4f, 1f / barUpSpeed);
            float dnT = Mathf.Max(1e-4f, 1f / barDownSpeed);
            float cycle = upT + dnT;
            float tc = tSince % cycle;
            float pos01 = (tc < upT) ? (tc / upT) : (1f - (tc - upT) / dnT);

            float ySurface = yCur[lastCenterIdx];
            float syCenter = baseSize * pillarHeight * (1f + pulse[lastCenterIdx]);
            float y = ySurface + pos01 * syCenter + barHeightOffset;

            Vector3 bp = basePos[lastCenterIdx];
            pulseBar.position = new Vector3(bp.x, y, bp.z);
            pulseBar.localScale = new Vector3(barWidth, barLength, barWidth);

            // 색/강도 페이드
            barIntensity = Mathf.Max(0f, barIntensity - barFadePerSec * Time.deltaTime);
            if (barRenderer)
            {
                Color col = Color.white;
                if (barRenderer.sharedMaterial.HasProperty(BaseColorID)) col = barRenderer.material.GetColor(BaseColorID);
                if (barRenderer.sharedMaterial.HasProperty(EmissionColorID)) col = barRenderer.material.GetColor(EmissionColorID);
                Color outCol = col * (0.2f + 1.8f * barIntensity);
                if (barRenderer.sharedMaterial.HasProperty(BaseColorID)) barRenderer.material.SetColor(BaseColorID, outCol);
                if (barRenderer.sharedMaterial.HasProperty(EmissionColorID)) barRenderer.material.SetColor(EmissionColorID, outCol);
            }

            if (barIntensity <= 0.001f) lastCenterIdx = -1; // 수명 종료
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _testNote = (_testNote % notesX) + 1;
            OnNotePlayed(_testNote, Random.Range(1, pitchesZ + 1));
        }
#endif
    }

    /// 외부에서 호출: noteIndex=1..notesX, pitch=1..pitchesZ
    public void OnNotePlayed(int noteIndex, int pitch)
    {
        noteIndex = Mathf.Clamp(noteIndex, 1, notesX);
        pitch = Mathf.Clamp(pitch, 1, pitchesZ);

        float u = (noteIndex - 0.5f) / notesX;
        float v = (pitch - 0.5f) / pitchesZ;
        float minX = basePos[0].x, maxX = basePos[gridX - 1].x;
        float minZ = basePos[0].z, maxZ = basePos[(gridZ - 1) * gridX].z;
        float cx = Mathf.Lerp(minX, maxX, u);
        float cz = Mathf.Lerp(minZ, maxZ, v);

        float r = noteRadius;
        float r2 = r * r;
        float now = Time.time;

        // 중심 인덱스(바 라이트용)
        int ix = Mathf.Clamp(Mathf.RoundToInt((noteIndex - 0.5f) / notesX * (gridX - 1)), 0, gridX - 1);
        int iz = Mathf.Clamp(Mathf.RoundToInt((pitch - 0.5f) / pitchesZ * (gridZ - 1)), 0, gridZ - 1);
        int centerIdx = iz * gridX + ix;

        for (int i = 0; i < basePos.Length; i++)
        {
            Vector3 bp = basePos[i];
            float dx = bp.x - cx, dz = bp.z - cz;
            float dist2 = dx * dx + dz * dz;
            if (dist2 > r2) continue;

            float dist = Mathf.Sqrt(dist2);
            float w = Mathf.Pow(1f - Mathf.Clamp01(dist / r), falloffExp);
            float target = w * pulseScaleMax;

            // ★ 즉시 상승
            if (target > pulse[i]) { pulse[i] = target; pulseTime[i] = now; }
        }

        // 바 라이트 갱신
        if (pulseBar)
        {
            lastCenterIdx = centerIdx;
            lastCenterTime = now;
            barIntensity = 1f;

            if (barColorByPitch && barRenderer)
            {
                float h = (pitch - 1f) / Mathf.Max(1, pitchesZ);
                Color pc = Color.HSVToRGB(h * 0.75f, 1f, 1f); // 파랑~보라 영역
                if (barRenderer.sharedMaterial.HasProperty(BaseColorID)) barRenderer.material.SetColor(BaseColorID, pc * 2f);
                if (barRenderer.sharedMaterial.HasProperty(EmissionColorID)) barRenderer.material.SetColor(EmissionColorID, pc * 2f);
            }
        }
    }
}
