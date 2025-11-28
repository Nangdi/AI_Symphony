using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PresetData", menuName = "Custom/presetData")]
public class PresetData : ScriptableObject
{
    public float[] data = new float[32];
}