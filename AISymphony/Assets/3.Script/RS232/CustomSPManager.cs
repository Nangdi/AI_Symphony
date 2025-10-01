using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using UnityEngine;

public class CustomSPManager : SerialPortManager
{
    [SerializeField] NotePlayerSynced mainNotePlayer;
    protected override void Awake()
    {
        base.Awake();
    }
    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    ReceivedData("D1423");
        //}
    }
    protected override void Start()
    {
        base.Start();
    }

    protected override void ReceivedData(string _data)
    {
        if(_data.Length < 5)
        {
            Debug.Log($"�ҷ������� : {_data}");
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
        Debug.Log($"{_data} int�迭�� ����");
        int[] melodyArray = ConvertToIntArray(_data);
        foreach (var item in melodyArray)
        {
            if(item < 0)
            {
                Debug.Log($"�ҷ������� 0���Ե� �迭");
                return;
            }
        }
        mainNotePlayer.ChangeMelody(mainNotePlayer, melodyArray);

    }

    public int[] ConvertToIntArray(string _seatSignal)
    {

        int[] seatIndex = _seatSignal.Select(c =>( c - '0')-1).ToArray();

        return seatIndex;

    }//NotePlayerSynced.array�� �迭�ֱ�

    public int[] ParseSignal(string signal)
    {
        // �� ���� �� ���� ��ȯ �� -1 ����
        int[] result = signal
            .Select(c => (c - '0') - 1)  // '1' �� 0, '8' �� 7
            .ToArray();

        return result;
    }

}
