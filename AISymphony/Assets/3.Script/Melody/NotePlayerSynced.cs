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
public enum PlayMode { Rhythm, Melody }

public class NotePlayerSynced : MonoBehaviour
{
    [SerializeField] private MusicalInstrumentsStore instrumentsStore;
    [SerializeField] private ScannerMover scannerMover;
    [SerializeField] private UITextManager uITextManager;
    [SerializeField] private ButtonGroupSelector[] buttonGroupSelectors;
    [SerializeField] private NotePlayerSynced subPlayer;
    [SerializeField] private NotePlayerSynced thirdPlayer;
    [SerializeField] private InstancedCubeSea cubeSea;
    [SerializeField] private MelodyEmotionAnalyzer emotionAnalyzer;
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

    public PlayMode playMode = PlayMode.Rhythm;

    
    public int octave = 1;

    // 🔹 싱크 관련
    private GlobalBeatClock clock;
    private int lastScheduledStep = -1;
    private double nextEventTime = 0.0;   // ✅ 다음 예약할 DSP 시간


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
        if (!toggle.isOn || currentInstrument == null || audioPool.Length == 0 || role.Equals(NotePlayerRole.Sub)) return;

        double now = AudioSettings.dspTime;

        // 아직 예약 시작 안 했으면 첫 예약 시점 설정
        if (nextEventTime <= 0)
            nextEventTime = now + clock.intervalSec;

        // 예약할 시간이 됐는지 체크
        if (now >= nextEventTime)
        {
            int targetStep = lastScheduledStep + 1;
            int currentNoteIndex = targetStep % melody.Length;
           

            int note = melody[currentNoteIndex];
            if (note >= 0 && note < currentInstrument.Length)
            {
                if (playMode == PlayMode.Rhythm)
                    PlayRhythmStep();
                   
                else if (playMode == PlayMode.Melody)
                    PlayMelodyStep();
            }
            if (subPlayer.toggle.isOn)
            {
                subPlayer.PlaySubNote(nextEventTime);
            }
            if (thirdPlayer.toggle.isOn)
            {
                thirdPlayer.PlaySubNote(nextEventTime);

            }

            if (role == NotePlayerRole.Main)
            {
                int[] tempAraay = new int[8];
                int groupIndx = buttonGroupSelectors[currentNoteIndex].groupNum;
                for (int i = 0; i < tempAraay.Length; i++)
                {
                    tempAraay[i] = melody[i + (8 * groupIndx)] + 1;
                }
                UpdateDebugGroup(groupIndx);
                UpdateSubmelody(tempAraay);
                string temp = ConvertToP(currentNoteIndex);
                SerialPortManager.Instance.SendData(temp);
                cubeSea.OnNotePlayed(currentNoteIndex, note);


                if(currentNoteIndex % 8 ==0)
                {
                    for (int i = 0; i < tempAraay.Length; i++)
                    {
                        tempAraay[i] = melody[i + (8 * groupIndx)];
                    }
                    var emotion = emotionAnalyzer.AnalyzeEmotion(tempAraay);
                    cubeSea.UpdateEmotionInfluence(emotion.colorEmotion, emotion.speedEmotion);
                }
            }
            //마디 넘어가는 타이밍 잡기
            //8마디 분석 스크립트로 넘기기
            //이모션분석후  cubeSea 색상 바꾸기

        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            playMode = PlayMode.Rhythm;
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            playMode = PlayMode.Melody;

        }
    }
    void PlayRhythmStep()
    {
        int targetStep = lastScheduledStep + 1;
        int note = melody[targetStep % melody.Length];
        ScheduleNote(note, nextEventTime);

        lastScheduledStep = targetStep;
        nextEventTime += clock.intervalSec;
    }
    int[] melodyFlight = {
   9,8,7,6,5,4,5,6,
9,7,8,9,8,7,8,6,7,8,9,8,7,6,7,5,6,7,
2,3,4,5,4,3,4,7,6,7,5,7,6,5,4,3,4,3,
2,3,4,5,6,7,5,7,6,7,6,7,6,4,5,6,7,8,9,
10,11,9,10,11,9,10,11,4,5,6,7,8,9,10,
9,7,8,9,2,3,4,5,4,3,4,7,6,7
};
    float[] rhythmFlight = {
     4f,4f,4f,4f,4f,4f,4f,4f,
    1f,0.5f,0.5f,1f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,
    1f,0.5f,0.5f,1f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,
    1f,0.5f,0.5f,1f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,
    1f,0.5f,0.5f,1f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,
    1f,0.5f,0.5f,1f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,
    1f,0.5f,0.5f,1f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,0.5f,
};
    int melodyIndex = 0;
    void PlayMelodyStep()
    {
        int targetStep = lastScheduledStep + 1;
        int note = melodyFlight[targetStep % melodyFlight.Length];
        float length = rhythmFlight[targetStep % rhythmFlight.Length];
        Debug.Log(melodyFlight.Length);
        Debug.Log(rhythmFlight.Length);
        ScheduleNote(note, nextEventTime);
        lastScheduledStep = targetStep;
        bpm = clock.bpm;
        double quarterNoteSec = 60.0 / bpm;   // 1박자(4분음표) 길이
        nextEventTime += quarterNoteSec * length;
        Debug.Log($"bpm : {bpm} , 박자 : {quarterNoteSec * length}");
        //melodyIndex++;

        //if (melodyIndex >= melodyFlight.Length)
        //{
        //    melodyIndex = 0;
        //    playMode = PlayMode.Rhythm; // 곡 끝나면 다시 리듬모드
        //}
    }
    void ScheduleNote(int note, double when)
    { 
        if (note < 0 || note >= currentInstrument.Length) return;

        currenAudio = audioPool[poolIndex % audioPool.Length];
        currenAudio.clip = currentInstrument[note + (7 * (octave - 1))];
        currenAudio.PlayScheduled(when);
        poolIndex++;
    }
    public void PlaySubNote(double when)
    {
        int targetStep = lastScheduledStep + 1;
        int note = melody[targetStep % melody.Length];
        ScheduleNote(note, when);
        lastScheduledStep = targetStep;
    }
    public void UpdateSubmelody(int[] melody)
    {
        var label = MelodyClassifier.MelodyClassifier96.Classify(melody );
        //Debug.Log($"Label: {label.Subtype}, Family: {label.Family}");
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
        //Debug.Log("SubMelody: [" + string.Join(",", tempAraay) + "]");
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
        clock = GlobalBeatClock.I; // 마스터 시계 참조
        bpm = clock.bpm;
        Debug.Log($"BPM 변경 : {bpm}");
        // 기존 예약 초기화
        foreach (var a in audioPool)
        {
            a.Stop();
            a.clip = null;
        }

        // 새 intervalSec 반영해서 nextEventTime 리셋
        double now = AudioSettings.dspTime;
        nextEventTime = now + clock.intervalSec;

        //lastScheduledStep = -1;
        //poolIndex = 0;
    }
    private void ScheduleNextNote()
    {
        double now = clock.Now;
        int targetStep = lastScheduledStep + 1;
        double targetTime = now + clock.intervalSec; // 지금 기준으로 바로 예약

        int currentNoteIndex = targetStep % melody.Length;
        int note = melody[currentNoteIndex];

        if (note >= 0 && note < currentInstrument.Length)
        {
            currenAudio = audioPool[poolIndex % audioPool.Length];
            int clipNum = note + (7 * (octave - 1));
            currenAudio.clip = currentInstrument[clipNum];
            currenAudio.PlayScheduled(targetTime);
            poolIndex++;
        }

        lastScheduledStep = targetStep;
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            // 🔹 재생 시작 시 큐 초기화
            foreach (var a in audioPool)
            {
                a.Stop();
                a.clip = null;
            }

            double now = AudioSettings.dspTime;
            nextEventTime = now + clock.intervalSec;  // 다음 박자부터 예약
            lastScheduledStep = -1;
            poolIndex = 0;
        }
        else
        {
            // 멈출 때도 큐 초기화 (원하는 경우)
            nextEventTime = 0.0;
            lastScheduledStep = -1;
            poolIndex = 0;
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
        UnityEngine.Debug.Log(index);
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
        //Debug.Log(temp);
        return temp;
       
    }
    public void UpdateDebugGroup(int groupNum)
    {
        debugGroupText.text = debugCashingGroupText + groupNum;
    }
    string ConvertToP(int value)
    {
        // 0 ~ 31 → 1 ~ 32로 맞춤
        int num = value + 1;
        return "P" + num.ToString("D2");
    }
}
