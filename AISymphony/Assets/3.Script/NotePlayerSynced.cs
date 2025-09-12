using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum NotePlayerRole 
{ 
    Main,
    Sub
}

public class NotePlayerSynced : MonoBehaviour
{
    [SerializeField] private MusicalInstrumentsStore instrumentsStore;
    [SerializeField] private ScannerMover scannerMover;
    [SerializeField] private UITextManager uITextManager;
    [SerializeField] private ButtonGroupSelector[] buttonGroupSelectors;
    [SerializeField] private NotePlayerSynced subPlayer;
    [SerializeField] private NotePlayerSynced thirdPlayer;

    [Header("Role")]
    public NotePlayerRole role;
    [Header("Pattern")]
    public int[] melody;

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
    public TMP_Text debugGroupText;
    public string debugCashingGroupText;
    

    public int octave = 1;

    // 🔹 싱크 관련
    private GlobalBeatClock clock;
    private int lastScheduledStep = -1;


    //Test
    float currentTime;
    private void Awake()
    {
        melody = new int[buttonGroupSelectors.Length];
        for (int i = 0; i < buttonGroupSelectors.Length; i++)
        {
            buttonGroupSelectors[i].lineNum = i;
        }
        if (debugGroupText != null)
        {
            debugCashingGroupText = debugGroupText.text;
        }
    }
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
            int currentNoteIndex = targetStep % melody.Length;
            int note = melody[currentNoteIndex];
            if (note >= 0 && note < currentInstrument.Length)
            {
                float tempVolume = 0.5f;
                if (currenAudio != null && currenAudio.isPlaying)
                {

                    tempVolume = currenAudio.volume;
                    currenAudio.volume = 0;
                }
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
                currenAudio.volume = tempVolume;
                poolIndex++;
            }
            if (role == NotePlayerRole.Main /*&&(targetStep + 1) % melody.Length == 0*/ )
            {
                int[] tempAraay = new int[8];
                int groupIndx = buttonGroupSelectors[currentNoteIndex].groupNum;
                for (int i = 0; i < tempAraay.Length; i++)
                {
                    tempAraay[i] = melody[i + (8 * groupIndx)] + 1;
                }
                // 8번째까지 예약이 끝난 시점
                UpdateDebugGroup(groupIndx);
                UpdateSubmelody(tempAraay);
            }
            lastScheduledStep = targetStep;

            // 스캐너도 같은 스텝으로 이동
            //scannerMover.SetStep(targetStep % melody.Length);
        }

    }
    public void UpdateSubmelody(int[] melody)
    {
        var label = MelodyClassifier.MelodyClassifier96.Classify(melody );
        Debug.Log($"Label: {label.Subtype}, Family: {label.Family}");
        if (uITextManager != null) //메인 notePlayer에게만 할당했음 나머지 서브들은 null
        {

            uITextManager.UpdateTypeText(label.Subtype, label.Family);
        }
        // 2) 서브멜로디 생성
        int[] subMelody = MelodyClassifier.SubMelodyGenerator.Generate(melody, label);
        int[] tempAraay = new int[8];
        for (int i = 0; i < subMelody.Length; i++)
        {
            tempAraay[i] = subMelody[i] - 1;
        }
        int[] thirdMelod = GetHamony(melody, tempAraay);
        ChangeMelody(subPlayer, tempAraay);
        ChangeMelody(thirdPlayer, thirdMelod);
        Debug.Log("SubMelody: [" + string.Join(",", tempAraay) + "]");
    }
    //서브멜로디 교체메소드
    public void ChangeMelody(NotePlayerSynced player, int[] newMelody)
    {
        for (int i = 0; i < newMelody.Length; i++)
        {
            player.buttonGroupSelectors[i].groupButtons[newMelody[i]].onClick.Invoke();

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
        //ResetOctaveOption(index);
    }
    public void ResetOctaveOption(int index)
    {
        Debug.Log(index);
        octaveDropdown.ClearOptions();
        octaveDropdown.AddOptions(new List<string> { "3 octave", "4 octave" });
        octaveDropdown.value = 0;
        //if (index < 2)
        //{
        //    octaveDropdown.options.Remove(octaveDropdown.options[2]);
        //    octaveDropdown.options.Remove(octaveDropdown.options[0]);
        //    octaveDropdown.value = 0;
        //}
        //else if(index <4)
        //{
        //    octaveDropdown.options.Remove(octaveDropdown.options[2]);
        //    octaveDropdown.value = 1;

        //}
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
  public int[] GetHamony(int[] main , int[] sub)
    {

        int[] temp = new int[8];
        for (int i = 0; i < temp.Length; i++)
        {
            temp[i] = MelodyClassifier.GenerateOneHarmonyNote(main[i], sub[i]);
        }
        Debug.Log(temp);
        return temp;
       
    }
    public void UpdateDebugGroup(int groupNum)
    {
        debugGroupText.text = debugCashingGroupText + groupNum;
    }
}
