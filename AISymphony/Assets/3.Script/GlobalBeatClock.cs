using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GlobalBeatClock : MonoBehaviour
{
    public static GlobalBeatClock I { get; private set; }

    [Header("Tempo")]
    public float bpm = 120f;
    public TMP_InputField bpmInput;
    [Range(1, 4)] public int division = 1; // 1=1/4, 2=1/8, 4=1/16

    //[Header("Clock")]
    public double startDspTime { get; private set; }
    public double intervalSec { get; private set; } // 1 division 길이

    private double beatLength;

    public List<NotePlayerSynced> players;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        bpmInput.onEndEdit.AddListener(OnBPMChanged);

    }

    void Start()
    {
        Recalc();
        // 모든 플레이어가 공통 기준을 갖도록 약간 뒤에 시작
        startDspTime = AudioSettings.dspTime + 0.3;
    }

    void Recalc() => intervalSec = (60.0 / bpm) / division;

    public void SetTempo(float newBpm)
    {
        // 진행 위치(phase) 유지
        double now = AudioSettings.dspTime;
        double songPos = now - startDspTime; // 초
        bpm = newBpm;
        //division = Mathf.Clamp(newDivision, 1, 4);
        Recalc();
        startDspTime = now - songPos;
        foreach (var item in players)
        {
            item.OnBPMChanged();
        }
    }
    private void OnBPMChanged(string value)
    {
        if (float.TryParse(value, out float newBpm))
        {
            SetTempo(newBpm);
        }
        else
        {
            Debug.LogWarning("BPM 입력이 올바르지 않습니다.");
        }
    }
    public double Now => AudioSettings.dspTime;
    public double SongPosSec => Now - startDspTime;
    public double SongPosTicks => SongPosSec / intervalSec; // division 단위 진행

    // 지금 시점 기준, n틱 단위 격자에 '다음'으로 양자화된 DSP 시각을 돌려줌
    public double NextQuantizedTime(int tickMultiple = 1)
    {
        double ticks = SongPosTicks;
        double nextIndex = Mathf.Ceil((float)ticks / tickMultiple) * tickMultiple;
        return startDspTime + nextIndex * intervalSec;
    }
    public float GetBeatProgress()
    {
        double elapsed = AudioSettings.dspTime - startDspTime;
        return (float)((elapsed % beatLength) / beatLength);
    }

    public double GetNextTick()
    {
        double elapsed = AudioSettings.dspTime - startDspTime;
        double beatsPassed = Mathf.FloorToInt((float)(elapsed / beatLength)) + 1;
        return startDspTime + (beatsPassed * beatLength);
    }
}
