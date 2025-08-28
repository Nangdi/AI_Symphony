using UnityEngine;

public class MelodySystem : MonoBehaviour
{
    void Start()
    {
        // 예시 입력 멜로디 (도레미파솔라시도)
        int[] melody = { 1, 3, 5, 7, 8, 6, 4, 2 };

        // 1) 분류
        var label = MelodyClassifier.MelodyClassifier96.Classify(melody);
        Debug.Log($"Label: {label.Subtype}, Family: {label.Family}");

        // 2) 서브멜로디 생성
        int[] subMelody = MelodyClassifier.SubMelodyGenerator.Generate(melody, label);

        Debug.Log("SubMelody: [" + string.Join(",", subMelody) + "]");
    }
}
