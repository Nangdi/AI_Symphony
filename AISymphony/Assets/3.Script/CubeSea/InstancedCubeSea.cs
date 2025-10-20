using UnityEngine;
using System.Collections.Generic;

public class InstancedCubeSea : MonoBehaviour
{
    // ───────── Color Mode ─────────
    public enum ColorMode { TechPalette, TeamLabFlow, WarmAurora, DeepSeaBio, InkGold }
    [Header("Color Mode")]
    public ColorMode colorMode = ColorMode.TechPalette; // 기본은 기존 테크 팔레트
    [Header("Color Anchors (for non-Tech modes)")]
    public Color baseDeep = new Color(0.04f, 0.05f, 0.12f); // 딥네이비
    public Color baseViolet = new Color(0.15f, 0.06f, 0.26f); // 보라 기운
    public Color waterA = new Color(0.10f, 0.55f, 0.95f); // 시안
    public Color waterB = new Color(0.15f, 0.25f, 0.95f); // 블루
    public Color warmA = new Color(1.00f, 0.88f, 0.20f); // 옐로
    public Color warmB = new Color(1.00f, 0.38f, 0.60f); // 핑크
    public Color sandA = new Color(0.92f, 0.55f, 0.28f); // 앰버
    public Color sandB = new Color(0.98f, 0.78f, 0.55f); // 모래빛
    public Color inkBase = new Color(0.08f, 0.08f, 0.10f); // 먹색
    public Color goldAccent = new Color(1.00f, 0.82f, 0.28f); // 금색

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
    [Header("Pulse Color Shift")]
    public bool pulseInvertColor = true;     // 끄면 기존 컬러 유지
    [Range(0f, 1f)] public float pulseHueShift01 = 0.5f; // 0.5 = 180도(보색)
    [Range(0f, 1f)] public float pulseColorMix = 0.9f;   // 펄스 피크 때 보색 섞는 강도
    public float pulseSatMul = 1.05f;         // 보색쪽 채도 보정
    public float pulseValMul = 0.95f;         // 보색쪽 명도 보정(살짝 어둡게)
    public float pulseMixExp = 1.2f;          // 1보다 크면 초반 천천히, 끝에서 많이
                                              // ───────── Irregular Wave (비정형 파동) ─────────
    [Header("Irregular (subtle)")]
    public float irrNoiseScale = 0.20f;    // 공간 노이즈 스케일(큼→느슨)
    public float irrNoiseSpeed = 0.20f;    // 노이즈 시간 속도
    [Range(0, 0.4f)] public float irrTimeWarp = 0.10f;     // 시간 비틀기(초)
    [Range(0, 0.4f)] public float irrAmpJitter = 0.10f;    // 진폭 가변 비율
    [Range(0, 0.3f)] public float irrSecondWave = 0.08f;   // 2번째 파동 가중치
    public float irrSecondMul = 1.7f;      // 2번째 파동 주파수 배수

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
    float targetSpeed = 0;
    float targetColor = 0;
    void Start()
    {
        Vector2Int temp = JsonManager.instance.gameSettingData.gridVec2;
        gridX = temp.x;
        gridZ = temp.y;
        // 0.003125
        // 0.00275
        // 300 0.2 / 80 0.22 1.5    
        spacing = (300f / temp.x) * spacing;
        baseSize = (80f / temp.y) * baseSize;


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

            // 위치 기반 노이즈(0..1 → -0.5..0.5)
            float n = N2(bp.x * irrNoiseScale + t * irrNoiseSpeed,
                         bp.z * irrNoiseScale - t * irrNoiseSpeed) - 0.5f;

            // 시간 비틀기 + 진폭 흔들림(아주 약하게)
            float tWarp = tInst + n * irrTimeWarp;
            float ampMul = 1f + (n * 2f) * irrAmpJitter;   // -irrAmpJitter..+irrAmpJitter

            // 인스턴스 위상 약간 랜덤(기존 hueOffset 재사용)
            float phase = hueOffset[i] * 6.2831853f; // 0..2π

            // 기본 파동(해수면 느낌 유지)
            float baseWave =
                (Mathf.Sin(bp.x * freqX + tWarp * speedX + phase) +
                 Mathf.Sin(bp.z * freqZ + tWarp * speedZ + phase)) * amplitude * ampMul;

            // 아주 약한 2번째 파동을 섞어 규칙성 깨기
            float second =
                (Mathf.Sin(bp.x * (freqX * irrSecondMul) + tWarp * (speedX * 1.05f) + phase) +
                 Mathf.Sin(bp.z * (freqZ * irrSecondMul) + tWarp * (speedZ * 0.95f) + phase))
                * amplitude * irrSecondWave;

            float yTarget = baseWave + second;


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
            Color c = ComputeColor(bp, t, pulse[i], i);
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
        //speedX = Mathf.Lerp(speedX, targetSpeed, Time.deltaTime * 2f);
        centerHue = Mathf.Lerp(centerHue, targetColor, Time.deltaTime );
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
    Color ComputeColor(Vector3 bp, float t, float p, int i)
    {
        p = Mathf.Clamp01(p);
        Color col;

        switch (colorMode)
        {
            // ① TechPalette: 기존 HSV 노이즈(펄스에 의한 밝기 가감 제거)
            case ColorMode.TechPalette:
                {
                    float hn = N2(bp.x * hueNoiseScale + t * hueSpeed,
                                  bp.z * hueNoiseScale + hueOffset[i]); // 0..1
                    float h = Mathf.Clamp01(centerHue + (hn - 0.5f) * 2f * hueRange);
                    float v = Mathf.Clamp01(valueBase);                 // ← 펄스로 V 올리던 것 제거
                    col = Color.HSVToRGB(h, saturation, v);
                    break;
                }

            // ② teamLab 풍: 쿨 라인 + 웜 스팟 (펄스 화이트/채도 부스트 제거)
            case ColorMode.TeamLabFlow:
                {
                    // 바탕: Z 그라디언트
                    float minZ = basePos[0].z;
                    float maxZ = basePos[(gridZ - 1) * gridX].z;
                    float z01 = Mathf.InverseLerp(minZ, maxZ, bp.z);
                    col = Color.Lerp(baseDeep, baseViolet, z01);

                    // 흐르는 라인
                    float angRad = 95f * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
                    float linePhase = (bp.x * dir.x + bp.z * dir.y) * 0.42f + t * 0.70f;
                    float flowWave = 0.5f + 0.5f * Mathf.Cos(linePhase);
                    float flowMask = Mathf.SmoothStep(0.35f, 0.75f, flowWave);
                    Color flowCol = Color.Lerp(waterB, waterA, 0.5f + 0.5f * Mathf.Sin(linePhase * 0.6f));
                    col = Color.Lerp(col, flowCol, flowMask);

                    // 웜 스팟(꽃가루)
                    float n0 = N2(bp.x * 0.33f + t * 0.07f, bp.z * 0.33f + t * 0.05f);
                    float n1 = N2(bp.x * 0.77f - t * 0.03f, bp.z * 0.62f + t * 0.04f);
                    float speck = Mathf.Pow(Mathf.Clamp01(0.6f * n0 + 0.4f * n1), 2.2f);
                    float warmMask = Mathf.SmoothStep(0.78f, 0.92f, speck);
                    Color warmCol = Color.Lerp(warmA, warmB, N2(bp.x * 0.9f, bp.z * 0.9f));
                    col = Color.Lerp(col, warmCol, warmMask * 0.85f);
                    break;
                }

            // ③ Warm Aurora
            case ColorMode.WarmAurora:
                {
                    float minX = basePos[0].x, maxX = basePos[gridX - 1].x;
                    float x01 = Mathf.InverseLerp(minX, maxX, bp.x);
                    Color baseCol = Color.Lerp(sandA, sandB, x01);
                    float wobble = 0.5f + 0.5f * Mathf.Sin(bp.z * 0.35f + t * 0.4f);
                    Color waveCol = Color.Lerp(baseCol, new Color(1.0f, 0.55f, 0.55f), wobble * 0.25f);
                    float speck = Mathf.Pow(N2(bp.x * 0.9f, bp.z * 0.9f + t * 0.2f), 3f);
                    waveCol = Color.Lerp(waveCol, Color.white, speck * 0.08f);
                    col = Color.Lerp(waveCol, goldAccent, 0.0f); // 펄스 가산 제거
                    break;
                }

            // ④ Deep Sea Bio
            case ColorMode.DeepSeaBio:
                {
                    Color baseCol = Color.Lerp(new Color(0.02f, 0.05f, 0.08f),
                                               new Color(0.05f, 0.12f, 0.16f),
                                               Mathf.InverseLerp(basePos[0].z, basePos[(gridZ - 1) * gridX].z, bp.z));
                    float flow = 0.5f + 0.5f * Mathf.Sin(bp.x * 0.25f + t * 0.35f) *
                                             Mathf.Cos(bp.z * 0.21f - t * 0.27f);
                    Color aqua = Color.Lerp(new Color(0.05f, 0.6f, 0.6f),
                                            new Color(0.1f, 0.85f, 0.9f), flow);
                    float dotty = Mathf.Pow(N2(bp.x * 1.7f + t * 0.3f, bp.z * 1.6f - t * 0.25f), 6f);
                    col = Color.Lerp(baseCol, aqua, 0.35f + 0.45f * flow);
                    col = Color.Lerp(col, Color.cyan, dotty * 0.15f);
                    break;
                }

            // ⑤ Ink & Gold
            case ColorMode.InkGold:
                {
                    float vign = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(bp.x * 0.01f));
                    col = Color.Lerp(inkBase * 0.9f, inkBase, vign);
                    float inkN = N2(bp.x * 0.2f + t * 0.05f, bp.z * 0.2f - t * 0.04f);
                    col *= 0.8f + 0.2f * inkN;

                    float goldMask = Mathf.SmoothStep(0.88f, 0.97f,
                                      N2(bp.x * 0.9f, bp.z * 0.9f + t * 0.15f));
                    Color gold = Color.Lerp(col, goldAccent, goldMask * 0.25f); // 펄스 의존 제거
                    col = gold;
                    break;
                }

            default: col = Color.white; break;
        }

        // ── 공통 후처리: 펄스 시 보색으로 블렌드(펄스가 감쇠하면 원색으로 복귀) ──
        if (pulseInvertColor && p > 0f)
        {
            Color.RGBToHSV(col, out float H, out float S, out float V);
            float H2 = Mathf.Repeat(H + pulseHueShift01, 1f);      // 180° = 0.5
            float S2 = Mathf.Clamp01(S * pulseSatMul);
            float V2 = Mathf.Clamp01(V * pulseValMul);
            Color comp = Color.HSVToRGB(H2, S2, V2);

            float w = Mathf.Pow(p, pulseMixExp) * pulseColorMix;   // 펄스 세기→블렌드 강도
            col = Color.Lerp(col, comp, w);
        }

        return col;
    }
    public void UpdateEmotionInfluence(string colorEmotion , string speedEmotion)
    {
        //TechPalette.centerHue
        //기쁨 - 0.1~0.2
        //슬픔 - 0.5~0.6
        //Wave.speedX
        //안정 - 3 
        //화남 - 7
      
        switch (colorEmotion)
        {
            case "happy":
                targetColor = Random.Range(0.1f, 0.2f);
                break;
            case "sad":
                targetColor = Random.Range(0.5f, 0.6f);
                break;
        }
        switch (speedEmotion)
        {
            case "angry":
                targetSpeed = 7;
                break;
            case "calm":
                targetSpeed = 3;
                break;
        }
        speedX = targetSpeed;
    }
}
