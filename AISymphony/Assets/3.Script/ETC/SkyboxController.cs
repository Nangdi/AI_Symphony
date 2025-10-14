using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("Skybox Material (DiagonalRotatingSkybox Shader 사용)")]
    public Material skyboxMaterial;

    [Header("회전 속도 (기본값 0.02)")]
    public float rotationSpeed = 0.02f;

    [Header("회전 축 (예: (1,-1,0) = ↘️ 방향)")]
    public Vector3 rotationAxis = new Vector3(1, -1, 0);

    void Update()
    {
        if (skyboxMaterial != null)
        {
            // Shader에 변수 전달
            skyboxMaterial.SetFloat("_RotationSpeed", rotationSpeed);
            skyboxMaterial.SetVector("_Axis", rotationAxis);
        }
    }
}
