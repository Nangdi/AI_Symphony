using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum InstrumentType
{
    Violin,
    BassGuitar,
    AcusticGuitar,
    ClassicalGuitar,
    BarroqueOrgan,
    Flute,
    Harp,
    Piano,
    Vibraphone,
}
public class MusicalInstrumentsStore : MonoBehaviour
{
    public AudioClip[] AcusticGuitar;
    public AudioClip[] BarroqueOrgan;
    public AudioClip[] BassGuitar;
    public AudioClip[] ClassicalGuitar;
    public AudioClip[] Flute;
    public AudioClip[] Harp;
    public AudioClip[] Piano;
    public AudioClip[] Vibraphone;
    public AudioClip[] Violin;
    public AudioClip[][] instruments;
    private void Awake()
    {
        instruments = new AudioClip[][]
       {
        Violin,
        BassGuitar,
        AcusticGuitar,
        ClassicalGuitar,
        BarroqueOrgan,
        Flute,
        Harp,
        Piano,
        Vibraphone,
       };
    }
    void Start()
    {
       
    }
    public AudioClip[] GetInstrument(int index)
    {
        return instruments[index];
    }
}
