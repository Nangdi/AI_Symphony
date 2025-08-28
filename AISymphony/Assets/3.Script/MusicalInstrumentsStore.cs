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
   // ���ڵ��
    public AudioClip[] Accordion;
    // �� �۷��˽���
    public AudioClip[] Bell_Glockenspiel;
    // ������ �ǾƳ�
    public AudioClip[] DXPiano;
    // �÷�Ʈ
    public AudioClip[] Flute1;
    // ��Ÿ
    public AudioClip[] Guitar1;
    public AudioClip[] Guitar2;
    // ������
    public AudioClip[] Marimba;
    public AudioClip[] Marimba_Classic;
    public AudioClip[] Marimba_Low_Mid;
    // �ǾƳ�
    public AudioClip[] Piano1;
    public AudioClip[] Piano2;
    // ��ġī��
    public AudioClip[] Pizzicato1;
    public AudioClip[] Pizzicato2;
    // Ʈ����
    public AudioClip[] Trumpet1;
    // Ʃ�� ��Ŀ��
    public AudioClip[] Tuned_Percussion1;
    public AudioClip[] Tuned_Percussion2;
    // �Ƿ���
    public AudioClip[] Xylophone1;
    public AudioClip[] Xylophone2;

    public AudioClip[][] instruments;
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
