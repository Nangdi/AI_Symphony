using UnityEngine;
using DG.Tweening;
using System.Collections;

public class CubeSeaGO : MonoBehaviour
{
    [Header("Grid")]
    public GameObject cubePrefab;
    public int gridX = 128;         // 128x64 = 8192개 (PC면 충분)
    public int gridZ = 64;
    public float spacing = 0.5f;
    public float baseSize = 0.2f;

    [Header("Wave")]
    public float freqX = 0.7f, freqZ = 1.1f;
    public float speedX = 1.0f, speedZ = 0.7f;
    public float amplitude = 0.25f;

    [Header("Note mapping (1..32, 1..8)")]
    public int notesX = 32;
    public int pitchesZ = 8;

    // 내부
    Transform[,] trs;
    Vector3[,] basePos;
    MaterialPropertyBlock[,] mpb;
    static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (!cubePrefab) { Debug.LogError("Cube Prefab 할당하세요!"); enabled = false; return; }

        trs = new Transform[gridX, gridZ];
        basePos = new Vector3[gridX, gridZ];
        mpb = new MaterialPropertyBlock[gridX, gridZ];

        // 중앙 기준으로 격자 배치
        float x0 = -(gridX - 1) * 0.5f * spacing;
        float z0 = -(gridZ - 1) * 0.5f * spacing;

        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                var go = Instantiate(cubePrefab, transform);
                go.name = $"Cube_{x}_{z}";
                var t = go.transform;

                var pos = new Vector3(x0 + x * spacing, 0f, z0 + z * spacing);
                t.localPosition = pos;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one * baseSize;

                trs[x, z] = t;
                basePos[x, z] = pos;

                var r = go.GetComponent<Renderer>();
                var b = new MaterialPropertyBlock();
                if (r != null)
                {
                    r.GetPropertyBlock(b);
                    b.SetColor(EmissionID, Color.black); // Emission 안 쓰면 무시됨
                    r.SetPropertyBlock(b);
                }
                mpb[x, z] = b;

                // 가벼운 최적화
                if (r) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            }
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int x = 0; x < gridX; x++)
        {
            // 같은 x 열은 같은 xWorld을 공유 → 미세 최적화
            float xWorld = basePos[x, 0].x;
            float sx = Mathf.Sin(xWorld * freqX + t * speedX);

            for (int z = 0; z < gridZ; z++)
            {
                float zWorld = basePos[x, z].z;
                float sz = Mathf.Sin(zWorld * freqZ + t * speedZ);
                float y = (sx + sz) * amplitude;

                var p = trs[x, z].localPosition;
                p.y = y;
                trs[x, z].localPosition = p;
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            for (int i = 0; i < 32; i++)
            {
                for (int k = 0; k < 8; k++)
                {
                    OnNotePlayed(i, k);
                }
            }
            //OnNotePlayed(5, 7);
        }
    }

    /// 외부에서 호출: noteIndex=1..32, pitch=1..8
    public void OnNotePlayed(int noteIndex, int pitch)
    {
        noteIndex = Mathf.Clamp(noteIndex, 1, notesX);
        pitch = Mathf.Clamp(pitch, 1, pitchesZ);

        int gx = Mathf.RoundToInt((noteIndex - 0.5f) / notesX * (gridX - 1));
        int gz = Mathf.RoundToInt((pitch - 0.5f) / pitchesZ * (gridZ - 1));
        PulseArea(gx, gz, pitch);
    }

    void PulseArea(int cx, int cz, int pitch)
    {
        int radius = 2; // 셀 반경
        Color c = Color.HSVToRGB((pitch - 1) / 8f, 1f, 1f); // 음계별 색
        Color hdr = c * 5f; // Emission용 (머티리얼이 Emission 켜져 있어야 효과)

        int xMin = Mathf.Max(0, cx - radius);
        int xMax = Mathf.Min(gridX - 1, cx + radius);
        int zMin = Mathf.Max(0, cz - radius);
        int zMax = Mathf.Min(gridZ - 1, cz + radius);

        for (int x = xMin; x <= xMax; x++)
            for (int z = zMin; z <= zMax; z++)
            {
                var tr = trs[x, z];
                tr.DOKill(false);
                tr.DOScale(Vector3.one * baseSize * 1.8f, 0.12f).SetEase(Ease.OutQuad)
                  .OnComplete(() => tr.DOScale(Vector3.one * baseSize, 0.35f).SetEase(Ease.InOutQuad));
            }

        StopAllCoroutines();
        StartCoroutine(FlashEmission(xMin, xMax, zMin, zMax, hdr, 0.35f));
    }

    IEnumerator FlashEmission(int xMin, int xMax, int zMin, int zMax, Color hdr, float fadeTime)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            float k = 1f - t / fadeTime;
            for (int x = xMin; x <= xMax; x++)
                for (int z = zMin; z <= zMax; z++)
                {
                    var r = trs[x, z].GetComponent<Renderer>();
                    if (!r) continue;
                    var b = mpb[x, z];
                    b.SetColor(EmissionID, hdr * k);
                    r.SetPropertyBlock(b);
                }
            t += Time.deltaTime;
            yield return null;
        }
        // 리셋
        for (int x = xMin; x <= xMax; x++)
            for (int z = zMin; z <= zMax; z++)
            {
                var r = trs[x, z].GetComponent<Renderer>();
                if (!r) continue;
                var b = mpb[x, z];
                b.SetColor(EmissionID, Color.black);
                r.SetPropertyBlock(b);
            }
    }
}
