using UnityEngine;

public class FluidCubeVisualizer : MonoBehaviour
{
    public Mesh cubeMesh;
    public Material cubeMat;
    public int instanceCount = 20000;  // 수만 개 큐브
    private Matrix4x4[] matrices;
    private Vector4[] colors;

    void Start()
    {
        matrices = new Matrix4x4[instanceCount];
        colors = new Vector4[instanceCount];

        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-20f, 20f),
                Random.Range(-10f, 10f),
                Random.Range(-5f, 5f)
            );
            matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.2f);
            colors[i] = new Vector4(0.2f, 0.2f, 0.2f, 1); // 기본 회색
        }
    }

    void Update()
    {
        // 기본 흐름: Perlin Noise로 위치 변경
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = matrices[i].GetColumn(3);
            pos.y += Mathf.PerlinNoise(pos.x + Time.time, pos.z) * 0.01f;
            matrices[i].SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
        }

        // GPU Instancing으로 한번에 그리기
        Graphics.DrawMeshInstanced(cubeMesh, 0, cubeMat, matrices, instanceCount);

        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnNotePlayed(1, 8);
        }
    }

    // 노트 발생 시 호출
    public void OnNotePlayed(int noteIndex, int pitch)
    {
        // noteIndex, pitch → 영역 지정
        float xRange = Mathf.Lerp(-20f, 20f, noteIndex / 32f);
        float yRange = Mathf.Lerp(-10f, 10f, pitch / 8f);

        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = matrices[i].GetColumn(3);
            if (Mathf.Abs(pos.x - xRange) < 2f && Mathf.Abs(pos.y - yRange) < 1f)
            {
                // 반짝이는 효과
                colors[i] = new Vector4(1, 0.5f, 0, 1);
                matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.5f);
            }
        }
    }
}
