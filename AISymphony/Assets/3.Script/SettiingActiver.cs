using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIActiver : MonoBehaviour
{
    public GameObject[] settingPanels;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetActivesettingPanels();
        }
    }
    private void SetActivesettingPanels()
    {
        foreach (var panel in settingPanels) { 
        panel.SetActive(!panel.activeSelf);
            Cursor.visible = panel.activeSelf;
        }
    }
}
