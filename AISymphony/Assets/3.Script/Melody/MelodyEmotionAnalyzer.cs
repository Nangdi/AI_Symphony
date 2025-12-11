using UnityEngine;

public class MelodyEmotionAnalyzer : MonoBehaviour
{
    float avg = 0f;
    int upCount = 0, downCount , jumpCount , calmCount, angryCount = 0;
    int min = 99, max = -99;
    int upLife, downLife =0;

    float happyScore, sadScore, calmScore,angryScore = 0;
    public (string colorEmotion, string speedEmotion) AnalyzeEmotion(int[] notes)
    {

        JudgmentScore(notes);

        //Debug.Log($"happyScore : {happyScore} , sadScore : {sadScore} ,calmScore : {calmScore}, ,angry : {angryScore}" );
        var emotion = GetEmotionResults(happyScore, sadScore, calmScore, angryScore);
        //Debug.Log($"Color: {emotion.colorEmotion}, Speed: {emotion.speedEmotion}");
        //TechPalette.CenterHue
        //기쁨 - 0.1~0.2
        //슬픔 - 0.5~0.6
        //Wave.SpeedX
        //안정 - 3 
        //화남 - 7

        ResetScore();
        return emotion;
    }
    public void JudgmentScore(int[] notes)
    {
        int current = notes[0];
        for (int i = 0; i < notes.Length; i++)
        {
            //평균계산
            Arg(notes[i]);
            //연속패턴
            Sequence(notes[i] , current);
            //도약패턴
            Jump(notes[i] , current);
            //high low vlaue계산
            LowHigh(notes[i]);

            current = notes[i];
        }
        avg /= notes.Length;
        if (avg >= 5f)
        {
            happyScore += 3;
        }
        else if (avg <= 2f)
        {
            sadScore += 3;
        }
        else
        {
            calmScore += 0.5f;
        }

        if(max <= 3)
        {
            sadScore += 3;
        }
        else if(min >=4)
        {
            happyScore += 3;
        }
        else if(max - min <= 2)
        {
            calmScore += 5;
        }
    }
    public void Jump(int note, int current)
    {

        int jumpValue = Mathf.Abs(note - current);
        if (jumpValue >= 3)
        {
            jumpCount++;
            if (jumpCount > 2)
            {
                happyScore+= 0.6f;
            }
        }
        if(jumpValue >= 4)
        {
            angryCount++;
            if (jumpCount > 2)
            {
                angryScore += 2f;
            }
        }
    }
    public void Arg(int note)
    {
        avg += note;
    }
    public void Sequence(int note , int current)
    {
        if (current < note)
        {
            upCount++;
            downLife++;
            calmCount = 0;
            if (upCount >= 3)
            {
                happyScore += 1.5f;
            }
            if (downLife >= 2)
            {
                downCount = 0;
                downLife = 0;

            }

        }
        else if (current > note)
        {
            downCount++;
            upLife++;
            calmCount = 0;
            if (downCount >= 3)
            {
                sadScore += 1.5f;
            }
            if (upLife >= 2)
            {
                upCount = 0;
                upLife = 0;
            }
        }
        else if(current == note)
        {
            calmCount++;
            if (calmCount >= 3)
            {
                calmScore += 1.5f;
            }
        }
    }
    public void LowHigh(int note)
    {
        if (note > max)
        {
            max = note;
        }
        if (note < min)
        {
            min = note;
        }
    }
    private void ResetScore()
    {
        avg = 0f;
        upCount = 0;
        downCount = 0;
        jumpCount = 0;
        calmCount = 0;
        angryCount = 0;
        min = 99;
        max = -99;

        happyScore = 0;
        sadScore = 0;
        calmScore = 0;
        angryScore = 0;
        upLife = 0;
        downLife = 0;
    }
    (string colorEmotion, string speedEmotion) GetEmotionResults(
     float happy, float sad, float calm, float angry)
    {
        string colorResult = (happy >= sad) ? "happy" : "sad";
        string speedResult = (angry > calm) ? "angry" : "calm";

        return (colorResult, speedResult);
    }
}
