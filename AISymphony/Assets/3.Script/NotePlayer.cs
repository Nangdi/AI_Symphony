using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotePlayer : MonoBehaviour
{
    [SerializeField]
    private MusicalInstrumentsStore instrumentsStore;
    [SerializeField]
    private ScannerMover scannerMover;
    //�Ǳ⸶�� wav���ϵ�
    //���� ��Ʈ�����
    //bpm(����)
    public int[] melody = { 5, 0, 0, 0, 0, 0, 0, 0 };
    [Header("Tempo")]
    public float bpm = 120f;
    [Range(1, 4)]
    public int division = 1;          // 1=1/4, 2=1/8, 4=1/16
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip noteClip;
    public InstrumentType instrumentType;
    public AudioClip[] currentInstrument;
    public TMP_Dropdown dropdown;
    public TMP_Dropdown octaveDropdown;
    public Toggle toggle;
    public TMP_InputField bpmInput;
    public int currentIndex;
    public int octave =1;
    private double nextTick;          // ���� ��� �ð�(DSP)
    private double interval;          // ���� ����(��)
    private bool started;

    void Start()
    {
        // ���� ���� ���
        interval = (60.0 / bpm) / division;

        // ���� �ð� ����
        nextTick = AudioSettings.dspTime + 0.1; // 0.1�� �ں��� ����
        started = true;
        scannerMover.isStart = true;
        nextTick += interval;

        SetInstruments();
        bpmInput.onEndEdit.AddListener(OnBPMChanged);
    }
  
    void Update()
    {
        if (!started || noteClip == null || audioSource == null|| !toggle.isOn) return;

        double dspTime = AudioSettings.dspTime;

        // ���� ���� �ð��� ���� DSP �ð����� �������� ����
        if (dspTime + 0.05 >= nextTick) // Look-ahead 50ms
        {
            SetAudioClip();
            audioSource.PlayScheduled(nextTick);
            // ���� �ð� ����
            nextTick += interval;
        }
    }

    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
        interval = (60.0 / bpm) / division;
    }
    void OnBPMChanged(string value)
    {
        if (float.TryParse(value, out float newBpm))
        {
            SetBPM(newBpm);
            //scannerMover.SetBPM(newBpm);
        }
        else
        {
            Debug.LogWarning("BPM �Է��� �ùٸ��� �ʽ��ϴ�.");
        }
    }
    public void SetMelody(int changeNote, int lineNum)
    {
        melody[lineNum] = changeNote;
    }
    public void SetInstruments()
    {
        int index = dropdown.value;
        Debug.Log(index);
        currentInstrument = instrumentsStore.GetInstrument(index);
    }
    public void SetAudioClip()
    {
        int clipNum = melody[currentIndex] + (7 * (octave-1));
        noteClip = currentInstrument[clipNum];
        audioSource.clip = noteClip;
        currentIndex++;
        if(currentIndex >= 8)
        {
            currentIndex = 0;
        }
    }
    public void SetOctave()
    {
        octave = octaveDropdown.value + 1;
    }
}
