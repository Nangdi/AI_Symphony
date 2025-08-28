using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotePlayerSynced : MonoBehaviour
{
    [SerializeField] private MusicalInstrumentsStore instrumentsStore;
    [SerializeField] private ScannerMover scannerMover;

    [Header("Pattern")]
    public int[] melody = { 5, 0, 0, 0, 0, 0, 0, 0 };

    [Header("Tempo")]
    public float bpm = 120f;
    [Range(1, 4)] public int division = 1; // 1=1/4, 2=1/8, 4=1/16

    [Header("Audio")]
    public AudioSource[] audioPool; // 풀링
    private AudioSource currenAudio;
    private int poolIndex;
    public AudioClip noteClip;
    public InstrumentType instrumentType;
    public AudioClip[] currentInstrument;
    public GameObject audioPrefab;
    [Header("UI")]
    public TMP_Dropdown dropdown;
    public TMP_Dropdown octaveDropdown;
    public Toggle toggle;
  

    public int octave = 1;

    // 🔹 싱크 관련
    private GlobalBeatClock clock;
    private int lastScheduledStep = -1;


    //Test
    float currentTime;
    void Start()
    {
        clock = GlobalBeatClock.I; // 마스터 시계 참조
        if (clock == null)
        {
            Debug.LogError("GlobalBeatClock이 씬에 없습니다!");
            enabled = false;
            return;
        }

        bpm = clock.bpm;
        division = clock.division;

        SetInstruments();
        toggle.onValueChanged.AddListener(OnToggleChanged);
        currentTime = bpm;
    }

    void Update()
    {
        if (!toggle.isOn || currentInstrument == null || audioPool.Length == 0) return;

        double now = clock.Now;
        int targetStep = lastScheduledStep + 1;
        double targetTime = clock.startDspTime + targetStep * clock.intervalSec;
        // lookAhead 안쪽이면 예약
        if (now >= targetTime)
        {
            int note = melody[targetStep % melody.Length];
            if (note >= 0 && note < currentInstrument.Length)
            {
                if (currenAudio != null && currenAudio.isPlaying)
                    currenAudio.volume = 0;
                int clipNum = note + (7 * (octave - 1));
                currenAudio = audioPool[poolIndex % audioPool.Length];
                currenAudio.clip = currentInstrument[clipNum];
                //while (true) 
                //{
                //    try
                //    {
                //        currenAudio.clip = currentInstrument[clipNum];
                //        break;

                //    }
                //    catch (System.Exception)
                //    {

                //        Debug.Log("index오류 현재클립 : " + clipNum);
                //        clipNum -= 7;
                //    }
                //}
                //2옥타브일때 3옥타브일때 높은도 처리해야함
                currenAudio.PlayScheduled(targetTime);
                currenAudio.volume = 100;
                poolIndex++;
            }

            lastScheduledStep = targetStep;

            // 스캐너도 같은 스텝으로 이동
            //scannerMover.SetStep(targetStep % melody.Length);
        }

    }

    public void SetBPM(float newBpm)
    {
        clock.SetTempo(newBpm);
    }

    public void OnBPMChanged()
    {
        //ShiftPool();
        //for (int i = 0; i < audioPool.Length; i++)
        //{
        //    audioPool[i].volume = 0;
        //}
        // 2) 글로벌 클럭 재설정
        bpm = clock.bpm;
        currentTime = bpm;
        // 3) 현재 시점의 스텝 계산
        int currentStep = Mathf.FloorToInt((float)clock.SongPosTicks) % melody.Length;

        // 4) 다음 스텝부터 다시 시작
        lastScheduledStep = currentStep - 1;
        if (lastScheduledStep < -1) lastScheduledStep = -1;
        //for (int i = 0; i < audioPool.Length; i++)
        //{
        //    audioPool[i].volume = 100;
        //}
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            // 다음 격자부터 합류
            double joinTime = clock.NextQuantizedTime(1);
            int joinStep = Mathf.RoundToInt((float)((joinTime - clock.startDspTime) / clock.intervalSec));
            lastScheduledStep = joinStep - 1;
        }
    }

    public void SetMelody(int changeNote, int lineNum)
    {
        melody[lineNum] = changeNote;
    }

    public void SetInstruments()
    {
        int index = dropdown.value;
        currentInstrument = instrumentsStore.GetInstrument(index);
        ResetOctaveOption(index);
    }
    public void ResetOctaveOption(int index)
    {
        Debug.Log(index);
        octaveDropdown.ClearOptions();
        octaveDropdown.AddOptions(new List<string> { "1 octave", "2 octave", "3 octave" });
        octaveDropdown.value = 1;
        if (index < 2)
        {
            octaveDropdown.options.Remove(octaveDropdown.options[2]);
            octaveDropdown.options.Remove(octaveDropdown.options[0]);
            octaveDropdown.value = 0;
        }
        else if(index <4)
        {
            octaveDropdown.options.Remove(octaveDropdown.options[2]);
            octaveDropdown.value = 1;

        }
    }

    public void SetOctave()
    {
        
        //if(octaveDropdown.options.Count <2  ||octaveDropdown.value == 0)
        //{
        //    octave = 2;
        //}
        //else
        //{
            octave = octaveDropdown.value + 1;
        //}
    }
    private void ShiftPool()
    {
        Destroy(audioPool[0].transform.parent.gameObject);
        GameObject ob = Instantiate(audioPrefab, transform, gameObject);
        for (int i = 0; i < ob.transform.childCount; i++)
        {
           ob.transform.GetChild(i).TryGetComponent(out audioPool[i]);
        }
    }
}
