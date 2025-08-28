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
    public double intervalSec { get; private set; } // 1 division ����

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
        // ��� �÷��̾ ���� ������ ������ �ణ �ڿ� ����
        startDspTime = AudioSettings.dspTime + 0.3;
    }

    void Recalc() => intervalSec = (60.0 / bpm) / division;

    public void SetTempo(float newBpm)
    {
        // ���� ��ġ(phase) ����
        double now = AudioSettings.dspTime;
        double songPos = now - startDspTime; // ��
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
            Debug.LogWarning("BPM �Է��� �ùٸ��� �ʽ��ϴ�.");
        }
    }
    public double Now => AudioSettings.dspTime;
    public double SongPosSec => Now - startDspTime;
    public double SongPosTicks => SongPosSec / intervalSec; // division ���� ����

    // ���� ���� ����, nƽ ���� ���ڿ� '����'���� ����ȭ�� DSP �ð��� ������
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
