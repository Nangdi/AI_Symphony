using System.Collections;
using System.Collections.Generic;
using System.Web;
using UnityEngine;
using System.Linq;

public class SerialPortController : MonoBehaviour
{
    public string seatSignal;


    public int[] ConvertToIntArray(string _seatSignal)
    {
        _seatSignal = _seatSignal.Substring(1);
        Debug.Log($"{_seatSignal} int�迭�� ����");
        int[] seatIndex = _seatSignal.Select(c => c - '0').ToArray();

        return seatIndex;

    }//NotePlayerSynced.array�� �迭�ֱ�
}

