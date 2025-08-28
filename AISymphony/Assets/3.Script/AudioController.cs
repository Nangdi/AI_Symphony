using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource[] audios;
    [SerializeField] private Slider volumeVar;

    public int volum;

    private void Start()
    {
        ChangeVolum(volumeVar.value);
    }
    public void ChangeVolum(float _volum)
    {
        for (int i = 0; i < audios.Length; i++)
        {
            audios[i].volume = _volum;

        }
    }
}
