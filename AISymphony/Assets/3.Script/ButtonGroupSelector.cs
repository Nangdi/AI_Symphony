using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum LineNum
{
    Line1,
    Line2,
    Line3,
    Line4,
    Line5,
    Line6,
    Line7,
    Line8
}
public class ButtonGroupSelector : MonoBehaviour
{
    [SerializeField]
    private NotePlayerSynced notePlayer;
    public List<Button> groupButtons;
    private Button selected;
    public LineNum lineNum;
    void Start()
    {
        for (int i = 0; i < groupButtons.Count; i++)
        {
            int index = i;
            groupButtons[i].onClick.AddListener(()=> SetNote(index));
        }
        foreach (var btn in groupButtons)
        {
            btn.onClick.AddListener(() => OnButtonSelected(btn));
        }
        int ran = Random.Range(0, 8);
        groupButtons[ran].onClick.Invoke();
    }

    void OnButtonSelected(Button btn)
    {
        if (selected != null)
            SetVisual(selected, false);  // 이전 버튼 비활성화

        selected = btn;
        SetVisual(selected, true);     // 현재 버튼 강조
    }

    void SetVisual(Button btn, bool isSelected)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = isSelected ? Color.red : Color.white;
        btn.colors = cb;
    }

    public void SetNote(int noteNum)
    {
        notePlayer.SetMelody(noteNum, (int)lineNum);
    }
}

