using UnityEngine;

public class FluidCubeVisualizer : MonoBehaviour
{
    public Mesh cubeMesh;
    public Material cubeMat;
    public int instanceCount = 20000;  // ���� �� ť��
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
            colors[i] = new Vector4(0.2f, 0.2f, 0.2f, 1); // �⺻ ȸ��
        }
    }

    void Update()
    {
        // �⺻ �帧: Perlin Noise�� ��ġ ����
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = matrices[i].GetColumn(3);
            pos.y += Mathf.PerlinNoise(pos.x + Time.time, pos.z) * 0.01f;
            matrices[i].SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));
        }

        // GPU Instancing���� �ѹ��� �׸���
        Graphics.DrawMeshInstanced(cubeMesh, 0, cubeMat, matrices, instanceCount);

        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnNotePlayed(1, 8);
        }
    }

    // ��Ʈ �߻� �� ȣ��
    public void OnNotePlayed(int noteIndex, int pitch)
    {
        // noteIndex, pitch �� ���� ����
        float xRange = Mathf.Lerp(-20f, 20f, noteIndex / 32f);
        float yRange = Mathf.Lerp(-10f, 10f, pitch / 8f);

        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = matrices[i].GetColumn(3);
            if (Mathf.Abs(pos.x - xRange) < 2f && Mathf.Abs(pos.y - yRange) < 1f)
            {
                // ��¦�̴� ȿ��
                colors[i] = new Vector4(1, 0.5f, 0, 1);
                matrices[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.5f);
            }
        }
    }
}
