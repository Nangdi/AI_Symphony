using System;
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
    public float beatLength;
    public double startDspTime { get; private set; }
    public double intervalSec { get; private set; }

    public event Action<double, int> OnBeatStep; // 🔹 DSP 이벤트 발생 시 전달
    public List<NotePlayerSynced> players = new List<NotePlayerSynced>();

    private int stepCounter = 0;
    private double nextEventTime = 0.0;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (bpmInput != null)
            bpmInput.onEndEdit.AddListener(OnBPMInputChanged);
    }

    void Start()
    {
        RecalculateInterval();
        startDspTime = AudioSettings.dspTime + 0.3; // 약간 딜레이 후 시작
        nextEventTime = startDspTime;
    }

    void Update()
    {
        double now = AudioSettings.dspTime;

        // FPS와 관계없이 DSP시간 기준으로 예약 진행
        if (now + 0.05 >= nextEventTime) // 50ms lookahead
        {
            OnBeatStep?.Invoke(nextEventTime, stepCounter);
            stepCounter++;
            nextEventTime += intervalSec;
        }
    }

    // ──────────────────────────────────────────────
    // BPM 관리
    // ──────────────────────────────────────────────
    void RecalculateInterval() => intervalSec = (60.0 / bpm) / division;

    public void SetTempo(float newBpm)
    {
        double now = AudioSettings.dspTime;
        double songPos = now - startDspTime;
        bpm = Mathf.Max(10f, newBpm); // 최소 제한
        RecalculateInterval();
        startDspTime = now - songPos; // 위상 유지
        nextEventTime = now + intervalSec;
        //stepCounter = 0;

        // 🔹 모든 플레이어 큐 리셋
        foreach (var p in players)
            p.OnBPMChanged();
    }

    private void OnBPMInputChanged(string value)
    {
        if (float.TryParse(value, out float newBpm))
            SetTempo(newBpm);
        else
            Debug.LogWarning("⚠ BPM 입력값이 올바르지 않습니다.");
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
