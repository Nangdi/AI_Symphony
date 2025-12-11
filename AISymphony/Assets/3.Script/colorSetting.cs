using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class colorSetting : MonoBehaviour
{
    public Volume volume;  // Inspector에서 할당할 Volume
    private ColorAdjustments colorAdjust;

    public TMP_InputField con_text;
    public TMP_InputField expo_text;
    public TMP_InputField satu_text;

    void Start()
    {
        colorAdjust.contrast.value = JsonManager.instance.gameSettingData.contrast;
        colorAdjust.postExposure.value = JsonManager.instance.gameSettingData.exposure;
        colorAdjust.saturation.value = JsonManager.instance.gameSettingData.saturation;

        InitColorValue();
        // VolumeProfile에서 ColorAdjustments 컴포넌트 가져오기
        if (volume.profile.TryGet(out ColorAdjustments ca))
        {
            colorAdjust = ca;
        }
        else
        {
            Debug.LogError("❌ VolumeProfile에 ColorAdjustments가 없습니다!");
        }
    }
    public void SetContrast(string textValue )
    {
          float temp = float.Parse( textValue );
        colorAdjust.contrast.value = temp;
        JsonManager.instance.gameSettingData.contrast = temp;
    }
    public void SetExposure(string textValue)
    {
        float temp = float.Parse(textValue);
        colorAdjust.postExposure.value = temp;
        JsonManager.instance.gameSettingData.exposure = temp;
    }
    public void SetSaturaion(string textValue)
    {
        float temp = float.Parse(textValue);
        colorAdjust.saturation.value = temp;
        JsonManager.instance.gameSettingData.saturation = temp;
    }
    //시작할때 Json파일에서 초기값 받아오기 , InputFiled에 넣기
    private void InitColorValue()
    {
        con_text.text = $"{JsonManager.instance.gameSettingData.contrast}";
        expo_text.text = $"{JsonManager.instance.gameSettingData.exposure}";
        satu_text.text = $"{JsonManager.instance.gameSettingData.saturation}";

        SetContrast(con_text.text);
        SetExposure(expo_text.text);
        SetSaturaion(satu_text.text);
    }
    public void SaveColor()
    {
        JsonManager.instance.gameSettingData.contrast = colorAdjust.contrast.value;
        JsonManager.instance.gameSettingData.exposure = colorAdjust.postExposure.value;
        JsonManager.instance.gameSettingData.saturation = colorAdjust.saturation.value;
    }
}
