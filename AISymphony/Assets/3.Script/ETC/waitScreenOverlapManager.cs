using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waitScreenOverlapManager : MonoBehaviour
{
    [SerializeField]
    private RectTransform leftRect;
    [SerializeField]
    private RectTransform rightRect;
    
    private void Start()
    {
        SetOverLapPos(JsonManager.instance.gameSettingData.uiOverLapValue);
    }
    private void Update()
    {
       
        OverLapUpdate();
    }
    private void OverLapUpdate()
    {
        float horizon = -Input.GetAxis("Horizontal");
        if (Mathf.Abs(horizon) > 0.01f)   // µ•µÂ¡∏
        {
            SetOverLapPos(horizon);
        }

        //float vertical = Input.GetAxis("Vertical");
    }
    private void SetOverLapPos(float value)
    {
        float leftPosX = leftRect.anchoredPosition.x + value;
        float rightPosX = rightRect.anchoredPosition.x - value;

        leftRect.anchoredPosition = new Vector3(leftPosX, leftRect.anchoredPosition.y, leftRect.anchoredPosition.y);
        rightRect.anchoredPosition = new Vector3(rightPosX, rightRect.anchoredPosition.y, rightRect.anchoredPosition.y);

        JsonManager.instance.gameSettingData.uiOverLapValue = 960 - leftRect.anchoredPosition.x;
        Debug.Log($"{leftRect.anchoredPosition.x}");
    }
}
