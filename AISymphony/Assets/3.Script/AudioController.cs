using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource[] audios;

    public int volum;

    public void ChangeVolum(int _volum)
    {
        for (int i = 0; i < audios.Length; i++)
        {
            audios[i].volume = _volum;

        }
    }
}
