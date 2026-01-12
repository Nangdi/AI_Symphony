using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;
using System;
using UnityEngine.UI;
using TMPro;

public class VFXController : MonoBehaviour
{
    public VisualEffect vfx;
    public Transform sphereTransform;
    public GameObject ob;
    public int index;

    [SerializeField] private TMP_InputField spawnRate_IF;
    [SerializeField] private TMP_InputField scaleX_IF;
    [SerializeField] private TMP_InputField scaleY_IF;
    [SerializeField] private TMP_InputField rangeY_IF;
    private float rippleRangeX;
    private float addValue;
    public float waveSpeed = 5;
    private void Start()
    {
        InitPaticleValue();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            //sendVFXEventPos();
        }
        addValue += Time.deltaTime* waveSpeed;
        vfx.SetFloat("waveValue", addValue);

    }



    public void sendVFXEventPos(int currentIndex , int pitch)
    {
        //-28~28 , -8~-8
        float tempX = Mathf.Lerp(-rippleRangeX, rippleRangeX, currentIndex / 31f);
        float tempY = Mathf.Lerp(-2.5f, 2.5f, pitch / 7f);
        Vector2 pos = new Vector2(tempX, tempY);

        vfx.SetVector2("eventPos", pos);
        addValue = 1;
        vfx.SetFloat("waveValue", 1);
    }
    public void SetColorOrSpeed(string color , string speed)
    {
        float targetColor = 0;
        float targetSpeed = 0;
        switch (color)
        {
            case "happy":
                targetColor = 0;
                break;
            case "sad":
                targetColor = 1;
                break;
        }
        switch (speed)
        {
            case "angry":
                targetSpeed = 1;
                break;
            case "calm":
                targetSpeed = 0;
                break;
        }


        vfx.SetFloat("colorValue", targetColor);
        vfx.SetFloat("speed", targetSpeed);
    }
    public void SetPower(int strength)
    {
        vfx.SetFloat("pulseStrength", strength);
    }
    //인풋필드 OnEdit
    public void UpdatePaticleSetting()
    {
        //int spawnRate = int.Parse(spawnRate_IF.text);
        //float scaleX = float.Parse(scaleX_IF.text);
        //float scaleY = float.Parse(scaleY_IF.text);
        float rangeX = float.Parse(rangeY_IF.text);
        rippleRangeX = rangeX;
        //Vector2 paticleScale = new Vector2(scaleX, scaleY);
        //vfx.SetInt("Rate", spawnRate);
        //vfx.SetVector2("paticleScale", paticleScale);

        //JsonManager.instance.gameSettingData.paticleRate = spawnRate;
        JsonManager.instance.gameSettingData.rippleRangeX = rangeX;
        //JsonManager.instance.gameSettingData.paticleScale = paticleScale;



    }
    //시작할때 Json파일에서 초기값 받아오기 , InputFiled에 넣기
    private void InitPaticleValue()
    {
        spawnRate_IF.text = $"{JsonManager.instance.gameSettingData.paticleRate}";
        scaleX_IF.text = $"{JsonManager.instance.gameSettingData.paticleScale.x}";
        scaleY_IF.text = $"{JsonManager.instance.gameSettingData.paticleScale.y}";
        rangeY_IF.text = $"{JsonManager.instance.gameSettingData.rippleRangeX}";

        UpdatePaticleSetting();
    }
}
