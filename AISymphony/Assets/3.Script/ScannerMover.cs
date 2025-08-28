using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEngine.UIElements;

public class ScannerMover : MonoBehaviour
{
    [Header("Position References")]
    public Transform distance1;  // 첫 박자 위치
    public Transform distance2;  // 마지막 박자 위치

    [Header("Settings")]
    public bool loop = true;
    public bool isStart = false;

    private float minX;
    private float maxX;
    public float followSpeed = 10f; // 스캐너가 타겟 좌표를 따라가는 속도
    // 참조
    private GlobalBeatClock clock;

    [SerializeField] private Transform[] notePositions; // 0~7 노트 위치
    [SerializeField] private int totalSteps = 8; // 노트 개수

    float startX;
    float endX;
    void Start()
    {
        clock = GlobalBeatClock.I;
        if (clock == null)
        {
            Debug.LogError("GlobalBeatClock이 씬에 없습니다!");
            enabled = false;
            return;
        }

        // 좌표 세팅
        minX = distance1.position.x;
        maxX = distance2.position.x;

        float halfDist = (notePositions[1].position.x - notePositions[0].position.x) / 2f;
        startX = notePositions[0].position.x - halfDist;

        // 🎯 끝 X = 마지막 노트의 X + (마지막과 전 노트의 X 거리 절반)
        float lastHalfDist = (notePositions[totalSteps - 1].position.x - notePositions[totalSteps - 2].position.x) / 2f;
        endX = notePositions[totalSteps - 1].position.x + lastHalfDist;

        //transform.position = new Vector3(notePositions[0].position.x, transform.position.y, transform.position.z);
    }

    void Update()
    {
        // 현재 진행된 Tick (division 단위)
        double songTicks = clock.SongPosTicks;
        int currentStep = (int)songTicks % totalSteps;
        int nextStep = (currentStep + 1) % totalSteps;

        // 0~1 사이의 박자 진행률
        float stepProgress = (float)(songTicks - Mathf.FloorToInt((float)songTicks));
        float fromX, toX;
        //if (currentStep == 0)
        //    fromX = startX;
        //else
            fromX = notePositions[currentStep].position.x;

        //if (nextStep == 0)
        //{
            // 끝에서 시작으로 넘어갈 때 → 순간 이동
            transform.position = new Vector3(fromX, transform.position.y, transform.position.z);
        //    return;
        //}
        //else if (nextStep == 7)
        //{

        //}
        //else
        //    {

        //    toX = notePositions[nextStep].position.x;
        //}

        //// 보간 이동 (X만 변경)
        //float newX = Mathf.Lerp(fromX, toX, stepProgress);
        //transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    public void SetStep(int step)
    {
        float fromX;
        //if (currentStep == 0)
        //    fromX = startX;
        //else
        fromX = notePositions[step].position.x;

        //if (nextStep == 0)
        //{
        // 끝에서 시작으로 넘어갈 때 → 순간 이동
        transform.position = new Vector3(fromX, transform.position.y, transform.position.z);
    }
}
