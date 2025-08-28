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
    //악기마다 wav파일들
    //현재 노트음계들
    //bpm(박자)
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
    private double nextTick;          // 다음 재생 시각(DSP)
    private double interval;          // 박자 간격(초)
    private bool started;

    void Start()
    {
        // 박자 간격 계산
        interval = (60.0 / bpm) / division;

        // 시작 시각 설정
        nextTick = AudioSettings.dspTime + 0.1; // 0.1초 뒤부터 시작
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

        // 다음 예약 시각이 현재 DSP 시간보다 지났으면 예약
        if (dspTime + 0.05 >= nextTick) // Look-ahead 50ms
        {
            SetAudioClip();
            audioSource.PlayScheduled(nextTick);
            // 다음 시각 갱신
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
            Debug.LogWarning("BPM 입력이 올바르지 않습니다.");
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
