using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettiingActiver : MonoBehaviour
{
    public GameObject settingPanels;
    public Canvas debugTool;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetActivesettingPanels();
        }
    }
    private void SetActivesettingPanels()
    {
        Debug.Log("설정창 활성화");
        settingPanels.SetActive(!settingPanels.activeSelf);
        Cursor.visible = settingPanels.activeSelf;
        if(settingPanels.activeSelf)
        {
            debugTool.targetDisplay = 1;
        }
        else
        {
            debugTool.targetDisplay = 2;
        }

    }
}
