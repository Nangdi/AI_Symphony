using UnityEngine;

public class MelodySystem : MonoBehaviour
{
    void Start()
    {
        // ���� �Է� ��ε� (�������ļֶ�õ�)
        int[] melody = { 1, 3, 5, 7, 8, 6, 4, 2 };

        // 1) �з�
        var label = MelodyClassifier.MelodyClassifier96.Classify(melody);
        Debug.Log($"Label: {label.Subtype}, Family: {label.Family}");

        // 2) �����ε� ����
        int[] subMelody = MelodyClassifier.SubMelodyGenerator.Generate(melody, label);

        Debug.Log("SubMelody: [" + string.Join(",", subMelody) + "]");
    }
}
