using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

[Serializable]
public class MelodyData
{
    public int[] notes;
}

[Serializable]
public class EmotionResult
{
    public string emotion;
}

public class MelodyAnalyzer : MonoBehaviour
{
    public int[] currentMelody = new int[32];
    private int[] lastMelody = new int[32];
    private float stableTime = 0f;
    public float waitTime = 5f; // 5초 유지되면 분석

    void Update()
    {
        if (IsSameMelody(currentMelody, lastMelody))
        {
            stableTime += Time.deltaTime;
            if (stableTime >= waitTime)
            {
                StartCoroutine(SendMelodyToServer(currentMelody));
                stableTime = 0f; // 다시 초기화
            }
        }
        else
        {
            Array.Copy(currentMelody, lastMelody, 32);
            stableTime = 0f;
        }
    }

    bool IsSameMelody(int[] a, int[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    IEnumerator SendMelodyToServer(int[] melody)
    {
        MelodyData data = new MelodyData { notes = melody };
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("http://127.0.0.1:5000/analyze", ""))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                EmotionResult res = JsonUtility.FromJson<EmotionResult>(www.downloadHandler.text);
                Debug.Log("감정 분석 결과: " + res.emotion);
                // 👉 여기서 감정 결과에 따라 비주얼/사운드 변경
            }
            else
            {
                Debug.LogError("서버 요청 실패: " + www.error);
            }
        }
    }
}
