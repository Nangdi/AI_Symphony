using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FixedAspectCamera : MonoBehaviour
{
    // 원본 게임 비율 (32:10 = 3.2)
    public float targetAspect = 32f / 10f;

    private void Update()
    {
        Camera cam = GetComponent<Camera>();
        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;

        if (scale < 1.0f)
        {
            // 현재 화면이 더 좁음 → 위아래에 여백 (Letterbox)
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scale;
            rect.x = 0;
            rect.y = (1.0f - scale) / 2.0f;
            cam.rect = rect;
        }
        else
        {
            // 현재 화면이 더 넓음 → 좌우에 여백 (Pillarbox)
            float scaleWidth = 1.0f / scale;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}
