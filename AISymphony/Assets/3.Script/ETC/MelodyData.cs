using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "melodyPreset", menuName = "Custom/melodyData")]
public class MelodyData : ScriptableObject
{
    public int bpm;
    public int[] notes = new int[32]; 
    public float[] tempos = new float[32]; 
    public float[] strongys = new float[32]; 
}
