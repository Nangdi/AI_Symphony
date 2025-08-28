using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITextManager : MonoBehaviour
{
    public TMP_Text text;
    private string defaultText;
    // Start is called before the first frame update
    void Start()
    {
        defaultText = "�� Ÿ�� : label\r\n�йи� Ÿ�� : family";
    }

    // Update is called once per frame
    public void UpdateTypeText(string label , string family)
    {
        string tempstring = defaultText.Replace("label", label);
        tempstring = tempstring.Replace("family", family);
        text.text = tempstring;
    }
}
