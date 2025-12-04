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



    private string cashingString = "";
    protected override void Awake()
    {
        base.Awake();
    }
  
    protected override void Start()
    {
        InitRestTime();
        base.Start();


    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            ReceivedData("D12345678123456781234567812345600");
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ReceivedData("D12345678123456781234567812345670");
        }

        lapseTimer += Time.deltaTime;
        if (lapseTimer >= targetTime && !isWaitMode)
        {
            isWaitMode = true;
            lapseTimer = 0;
            SendData("H1");
            serialPortManager1.ReceivedData_public("T2");
            serialPortManager1.ReceivedData_public("B2");
            serialPortManager1.ReceivedData_public("S2");
        }
    }
    protected override void ReceivedData(string _data)
    {
        if (!cashingString.Equals(_data))
        {
            lapseTimer = 0;
            isWaitMode = false;
        }
        cashingString = _data;
        


        if(_data.Length < 32)
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

    public int[] ConvertToIntArray(string _seatSignal)
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
    public void UpdateRestTime()
    {
        float restTime = float.Parse(restTime_IF.text);
        targetTime = restTime;

        JsonManager.instance.gameSettingData.targetTime = targetTime;
    }
    public void InitRestTime()
    {
        restTime_IF.text = $"{JsonManager.instance.gameSettingData.targetTime}";
        UpdateRestTime();
    }
}
