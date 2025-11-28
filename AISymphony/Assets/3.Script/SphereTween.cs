using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SphereTween : MonoBehaviour
{
    private void Start()
    {
        OnNote();
    }
    private void OnNote()
    {
        //float calculPos = (posIndex + 1) - 16;
        //Vector3 temp = new Vector3(calculPos, 0, 0);
        //transform.transform.position = temp;
        transform.DOScaleY(10, 5f)
            .OnComplete(() => ResetSphere());
    }
    private void ResetSphere()
    {
        transform.DOScaleY(0, 0f);
    }
}
