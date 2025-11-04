using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum InstrumentType
{
    Accordion,
    Bell_Glockenspiel,
    DXPiano,
    Flute1,
    Guitar1,
    Guitar2,
    Marimba,
    Marimba_Classic,
    Marimba_Low_Mid,
    Piano1,
    Piano2,
    Pizzicato1,
    Pizzicato2,
    Trumpet1,
    Tuned_Percussion1,
    Tuned_Percussion2,
    Xylophone1,
    Xylophone2

}
public class MusicalInstrumentsStore : MonoBehaviour
{
   // 아코디언
    public AudioClip[] Accordion;
    // 벨 글로켄슈필
    public AudioClip[] Bell_Glockenspiel;
    // 디지털 피아노
    public AudioClip[] DXPiano;
    // 플루트
    public AudioClip[] Flute1;
    // 기타
    public AudioClip[] Guitar1;
    public AudioClip[] Guitar2;
    // 마림바
    public AudioClip[] Marimba;
    public AudioClip[] Marimba_Classic;
    public AudioClip[] Marimba_Low_Mid;
    // 피아노
    public AudioClip[] Piano1;
    public AudioClip[] Piano2;
    // 피치카토
    public AudioClip[] Pizzicato1;
    public AudioClip[] Pizzicato2;
    // 트럼펫
    public AudioClip[] Trumpet1;
    // 튜닝 퍼커션
    public AudioClip[] Tuned_Percussion1;
    public AudioClip[] Tuned_Percussion2;
    // 실로폰
    public AudioClip[] Xylophone1;
    public AudioClip[] Xylophone2;

    public AudioClip[][] instruments;

    public MelodyData[] datas;

    private void Awake()
    {
        instruments = new AudioClip[][]
           {
            Accordion,
            Bell_Glockenspiel,
            DXPiano,
            Flute1,
            Guitar1,
            Guitar2,
            Marimba,
            Marimba_Classic,
            Marimba_Low_Mid,
            Piano1,
            Piano2,
            Pizzicato1,
            Pizzicato2,
            Trumpet1,
            Tuned_Percussion1,
            Tuned_Percussion2,
            Xylophone1,
            Xylophone2
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
