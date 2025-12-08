using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettiingActiver : MonoBehaviour
{
    public GameObject settingPanels;
    public CanvasGroup debugTool;
    public int index;
    private void Start()
    {
        //Screen.SetResolution(1200, 1200, false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetActivesettingPanels();
        }
    }
    private void SetActivesettingPanels()
    {
        settingPanels.SetActive(false);
        debugTool.alpha = 0;
        Cursor.visible = false;

        if (index == 0)
        { 
            settingPanels.SetActive(true);
            Cursor.visible = true;
        }
        if(index == 1)
        {
            debugTool.alpha = 1;
            Cursor.visible = true;
        }
        index++;
        index %= 3;
        //Debug.Log("설정창 활성화");
        //Cursor.visible = settingPanels.activeSelf;
        //if(settingPanels.activeSelf)
        //{
        //    debugTool.alpha = 1;
        //}
        //else
        //{
        //    debugTool.alpha = 0;
        //}

    }
}
