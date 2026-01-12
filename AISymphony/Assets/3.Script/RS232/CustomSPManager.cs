using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using TMPro;
using UnityEngine;

public class CustomSPManager : SerialPortManager
{
    [SerializeField] NotePlayerSynced mainNotePlayer;
    public TMP_Text receiveDataText;
    [SerializeField] SerialPortManager1 serialPortManager1;
    [SerializeField] TMP_InputField restTime_IF;
    [Header("타이머관련")]
    public float lapseTimer = 0;
    public bool isWaitMode = false;
    private float targetTime = 300f;
    private Coroutine restCoroutine;
    private int restModeIndex = 1;


    private string cashingString = "";
    protected override void Awake()
    {
        base.Awake();
    }
  
    protected override void Start()
    {
        InitRestTime();
        base.Start();
            StartCoroutine(RestMode());


    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            ReceivedData("D12345678123456781234567812345622");
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ReceivedData("D12345678123456781234567812345671");
        }
        if (!isWaitMode)
        {
            lapseTimer += Time.deltaTime;
            if (lapseTimer >= targetTime - 3 )
            {
                restCoroutine = StartCoroutine(RestMode());
            }
        }
      
       
    }
    protected override void ReceivedData(string _data)
    {
        //if (!cashingString.Equals(_data))
        //{
        //    lapseTimer = 0;
        //    isWaitMode = false;
        //}

        for (int i = 0; i < cashingString.Length; i++)
        {
            if (cashingString[i] != _data[i])
            {
                if (_data[i] == '0' || cashingString[i] == '0')
                {
                    Debug.Log("0이있음");
                    continue;
                }
                Debug.Log("다른신호인식");

                if (restCoroutine != null)
                {
                    StopCoroutine(restCoroutine);
                    restCoroutine = null;
                }
                ExitRestmode();


                break;
            }

        }
        cashingString = _data;


        if (_data.Length < 32)
        {
            Debug.Log($"불량데이터 : {_data}");
            return;
        }

        if (_data[0] == 'D')
        {
            _data = _data.Substring(1);
        }
        if (_data.Length < 5)
        {
            _data += _data + _data + _data + _data + _data + _data + _data;
        }
        Debug.Log($"{_data} int배열로 변경");
        int[] melodyArray = ConvertToIntArray(_data);
        string temp = "";
        for (int i = 0; i < melodyArray.Length; i++)
        {
            temp += melodyArray[i];
        }
        Debug.Log($"변경된 배열 {temp} , 길이 : {melodyArray.Length}");
        for (int i = 0; i < melodyArray.Length; i++)
        {
            if (melodyArray[i] < 0)
            {
                Debug.Log($"불량데이터 0포함된 배열인덱스 : {i+1}노트 고장");
                melodyArray[i] = 0;
            }
        }
        receiveDataText.text = _data;
        mainNotePlayer.ChangeMelody(mainNotePlayer, melodyArray);

    }

    private int[] ConvertToIntArray(string _seatSignal)
    {

        int[] seatIndex = _seatSignal.Select(c =>( c - '0')-1).ToArray();

        return seatIndex;

    }//NotePlayerSynced.array에 배열넣기

    public int[] ParseSignal(string signal)
    {
        // 각 문자 → 숫자 변환 → -1 적용
        int[] result = signal
            .Select(c => (c - '0') - 1)  // '1' → 0, '8' → 7
            .ToArray();

        return result;
    }
    public void UpdateTargetTime()
    {
        float restTime = float.Parse(restTime_IF.text);
        targetTime = restTime;

        JsonManager.instance.gameSettingData.targetTime = targetTime;
    }
    private void InitRestTime()
    {
        restTime_IF.text = $"{JsonManager.instance.gameSettingData.targetTime}";
        UpdateTargetTime();
    }
    private IEnumerator RestMode()
    {
        isWaitMode = true;
        //yield return new WaitForSeconds(1);
        Debug.Log("대기모드실행");
        lapseTimer = 0;
        serialPortManager1.SendData($"H{restModeIndex}");

        yield return new WaitForSeconds(6);
        //mainNotePlayer.SetDefualtMelody();
        //serialPortManager1.ReceivedData_public("T2");
        //serialPortManager1.ReceivedData_public("B2");
        //serialPortManager1.ReceivedData_public("S2");
        float[] zeroStrong = new float[32];
        
        mainNotePlayer.SetStrong(zeroStrong);
    }
    public void ExitRestmode()
    {
        lapseTimer = 0;
        if (isWaitMode)
        {
            int strong = GetStrongToRestmodeIndex(restModeIndex);
            serialPortManager1.ReceivedData_public($"S{strong}");
            restModeIndex++;
            if(restModeIndex > 5)
            {
                restModeIndex = 1;
            }

        }
        isWaitMode = false;
    }
    private int GetStrongToRestmodeIndex(int restIndex)
    {
        switch (restIndex)
        {
            case 1:
                return 2;
            case 2:
                return 3;
            default:
                return 1;
        }

    }
}
