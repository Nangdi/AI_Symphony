using UnityEngine;
using DG.Tweening;

public class FloatingCubes : MonoBehaviour
{
    public GameObject cubePrefab;
    public int gridX = 64;
    public int gridZ = 64;
    public float spacing = 0.5f;
    private GameObject[,] cubes;

    void Start()
    {
        cubes = new GameObject[gridX, gridZ];
        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                Vector3 pos = new Vector3(x * spacing, 0, z * spacing);
                cubes[x, z] = Instantiate(cubePrefab, pos, Quaternion.identity, transform);
            }
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                GameObject cube = cubes[x, z];
                Vector3 pos = cube.transform.localPosition;

                float wave = Mathf.Sin((x * 0.2f) + t) * 0.5f;
                float wave2 = Mathf.Sin((z * 0.3f) + t * 1.2f) * 0.5f;

                pos.y = wave + wave2;
                cube.transform.localPosition = pos;
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

    // 노트 반응 (noteIndex=1~32, pitch=1~8)
    public void OnNotePlayed(int noteIndex, int pitch)
    {
        int x = noteIndex * gridX / 32;
        int z = pitch * gridZ / 8;

        Debug.Log(x);
        Debug.Log(z);
        var cube = cubes[x, z];
        var mat = cube.GetComponent<Renderer>().material;
        mat.SetColor("_EmissionColor", Color.HSVToRGB((float)pitch / 8f, 1, 1));

        // DOTween을 이용한 크기 펄스
        cube.transform.DOScale(Vector3.one * 2f, 0.2f)
            .SetEase(Ease.OutElastic)
            .OnComplete(() =>
            {
                cube.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutQuad);
            });
    }
}
