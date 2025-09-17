using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomSPManager : SerialPortManager
{
    [SerializeField] NotePlayerSynced mainNotePlayer;
    protected override void Awake()
    {
        base.Awake();
    }
    protected override void Start()
    {
        base.Start();
    }

    protected override void ReceivedData(string _data)
    {
        _data = _data.Substring(1);
        if (_data.Length < 5)
        {
            _data += _data + _data + _data + _data + _data + _data + _data;
        }
     
        mainNotePlayer.melody = ConvertToIntArray(_data);
        Debug.Log($"{_data} int배열로 변경");

    }

    public int[] ConvertToIntArray(string _seatSignal)
    {

        int[] seatIndex = _seatSignal.Select(c => c - '0').ToArray();

        return seatIndex;

    }//NotePlayerSynced.array에 배열넣기
   
}
