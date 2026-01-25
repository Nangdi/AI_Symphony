using UnityEngine;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Unity.Mathematics;
using UnityEditor;

public class MelodyToEmotion : MonoBehaviour
{
    private static readonly HttpClient client = new HttpClient();
    int[] melody = new int[32];
    private void Start()
    {
    }
    private async void Update()
    {
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    shuffle();
        //    await SendMelodyForEmotion(melody);
        //}
        //if (Input.GetKeyDown(KeyCode.S))
        //{
            //shuffle();
            //var emotion = MelodyEmotionAnalyzer.AnalyzeEmotion(melody);
            //Debug.Log($"emotion : {emotion}");
        //}
    }
    private void shuffle()
    {
        for (int i = 0; i < melody.Length; i++)
        {
            melody[i] = UnityEngine.Random.Range(0, 8);
        }
    }
    // 음계 매핑 (Hz)
    private static readonly float[] noteFreqs = new float[]
    {
        261.63f, // 0 = C3
        293.66f, // 1 = D3
        329.63f, // 2 = E3
        349.23f, // 3 = F3
        392.00f, // 4 = G3
        440.00f, // 5 = A3
        493.88f, // 6 = B3
        523.25f  // 7 = C4
    };


    public async Task SendMelodyForEmotion(int[] melody)
    {
        // 1. 멜로디 배열 → AudioClip 생성
        AudioClip clip = MelodyToAudioClip(melody, 44100, 0.15f); // 0.15초 per note

        // 2. WAV 변환
        byte[] wavData = AudioClipToWav(clip);

        // 3. Python 서버로 전송
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(wavData), "file", "melody.wav");

        var response = await client.PostAsync("http://127.0.0.1:8000/analyze", content);
        string json = await response.Content.ReadAsStringAsync();

        Debug.Log("Emotion Result: " + json);
    }

    private AudioClip MelodyToAudioClip(int[] melody, int sampleRate, float noteDuration)
    {
        int totalSamples = (int)(melody.Length * sampleRate * noteDuration);
        float[] samples = new float[totalSamples];

        int samplesPerNote = (int)(sampleRate * noteDuration);

        for (int i = 0; i < melody.Length; i++)
        {
            float freq = noteFreqs[melody[i]];
            for (int s = 0; s < samplesPerNote; s++)
            {
                int idx = i * samplesPerNote + s;
                samples[idx] = Mathf.Sin(2 * Mathf.PI * freq * s / sampleRate) * 0.5f; // 볼륨 0.5
            }
        }

        AudioClip clip = AudioClip.Create("melody", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // AudioClip → WAV 변환
    private byte[] AudioClipToWav(AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleRate = clip.frequency;
        short channels = 1;
        short bitsPerSample = 16;

        // WAV 헤더
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(0);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(0);

        foreach (var s in samples)
        {
            short val = (short)(s * short.MaxValue);
            writer.Write(val);
        }

        writer.Seek(4, SeekOrigin.Begin);
        writer.Write((int)(writer.BaseStream.Length - 8));
        writer.Seek(40, SeekOrigin.Begin);
        writer.Write((int)(writer.BaseStream.Length - 44));

        return stream.ToArray();
    }
}
