// LaneMatcher.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro; // [추가] TextMeshPro를 사용하기 위해 네임스페이스를 추가합니다.

public class LaneMatcher : MonoBehaviour
{
    public static LaneMatcher Instance { get; private set; }

    [Header("Component References")] // [수정] 헤더 이름 변경
    [SerializeField] private GazeLineDrawer gazeLineDrawer;
    [SerializeField] private SquareMoverManager squareMoverManager;

    [Header("UI References")] // [추가] UI 참조를 위한 헤더
    [SerializeField] private TextMeshProUGUI frechetResultText; // [추가] 프레셰 결과 표시용 텍스트
    [SerializeField] private TextMeshProUGUI speedResultText; // [추가] 속도 유사도 결과 표시용 텍스트

    [Header("Matching Settings")] // [수정] 헤더 이름 변경
    [Range(0f, 1f)] public float alpha = 0.8f; // Frechet 가중치 비율 (0~1)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // [추가] 프로그램 시작 시 또는 비교 시작 전에 UI 텍스트를 초기화하는 함수
    private void Start()
    {
        ClearResultTexts();
    }

    public void CompareAndFindClosestLane()
    {
        // 필요한 컴포넌트 자동 탐색
        if (gazeLineDrawer == null)
            gazeLineDrawer = FindFirstObjectByType<GazeLineDrawer>();
        if (squareMoverManager == null)
            squareMoverManager = FindFirstObjectByType<SquareMoverManager>();
        if (gazeLineDrawer == null || squareMoverManager == null)
        {
            Debug.LogError("❌ 필수 컴포넌트가 누락되었습니다.");
            return;
        }

        List<Vector3> gazePath = gazeLineDrawer.GetGazePoints();
        List<float> timestamps = gazeLineDrawer.GetGazeTimestamps();

        // 유효성 검사
        if (gazePath == null || gazePath.Count < 2 || timestamps == null || timestamps.Count < 2)
        {
            Debug.LogWarning("⚠️ Gaze 경로 또는 타임스탬프가 유효하지 않습니다.");
            ClearResultTexts(); // [추가] 유효하지 않을 때도 텍스트 초기화
            return;
        }

        List<ColorLaneInfo> colorLanes = ColorLaneManager.Instance.GetAllColorLanes();
        float[] objectSpeeds = squareMoverManager.currentLaneSpeeds;

        if (colorLanes == null || colorLanes.Count == 0 || objectSpeeds == null || objectSpeeds.Length != colorLanes.Count)
        {
            Debug.LogError("❌ ColorLane 또는 속도 배열 문제가 있습니다.");
            ClearResultTexts(); // [추가] 유효하지 않을 때도 텍스트 초기화
            return;
        }

        float gazeSpeed = CalculatePathSpeed(gazePath, timestamps);

        float minScore = float.MaxValue;
        ColorLaneInfo bestMatch = null;
        float bestNormFD = -1f; // [추가] 최고 점수의 정규화된 프레셰 값을 저장할 변수
        float bestSpeedSim = -1f; // [추가] 최고 점수의 속도 유사도 값을 저장할 변수

        for (int i = 0; i < colorLanes.Count; i++)
        {
            var lane = colorLanes[i];
            var lanePath = lane.GetWorldPoints();
            if (lanePath == null || lanePath.Count < 2) continue;

            float laneSpeed = objectSpeeds[i];

            // [1] 프레셰 거리 계산 후 정규화
            float frechet = FrechetDistanceCalculator.Calculate(gazePath, lanePath);
            float normFD = 1f - Mathf.Exp(-frechet); // 프레셰 거리 정규화 (작을수록 좋음)

            // [2] 속도 유사도 계산 (1에 가까울수록 유사함)
            float speedSim = Mathf.Exp(-Mathf.Abs(gazeSpeed - laneSpeed));

            // [3] 통합 유사도 점수 계산 (작을수록 유사함)
            float adjusted = alpha * normFD + (1f - alpha) * (1f - speedSim);

            Debug.Log($"🔍 {lane.name}: adjusted={adjusted:F3}, normFD={normFD:F3}, speedSim={speedSim:F3}, [gazeSpeed={gazeSpeed:F2}, laneSpeed={laneSpeed:F2}, α={alpha:F1}]");

            if (adjusted < minScore)
            {
                minScore = adjusted;
                bestMatch = lane;
                bestNormFD = normFD;       // [추가] 최고 점수일 때의 프레셰 값 저장
                bestSpeedSim = speedSim;   // [추가] 최고 점수일 때의 속도 유사도 값 저장
            }
        }

        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 최종 선택된 레인: {bestMatch.name}");
            
            // [추가] 최종 선택된 레인의 결과값을 UI 텍스트로 업데이트
            UpdateResultTexts(bestNormFD, bestSpeedSim);
        }
        else
        {
            // [추가] 일치하는 레인이 없을 경우 UI 텍스트 초기화
            ClearResultTexts();
        }
    }

    /// <summary>
    /// 시선 경로의 평균 속도 계산 (전체 거리 / 전체 시간)
    /// </summary>
    private float CalculatePathSpeed(List<Vector3> path, List<float> timestamps)
    {
        float totalDist = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            totalDist += Vector3.Distance(path[i - 1], path[i]);
        }
        float totalTime = timestamps[^1] - timestamps[0];
        return totalTime > 0 ? totalDist / totalTime : 0f;
    }

    // [추가] 결과 텍스트를 업데이트하는 함수
    private void UpdateResultTexts(float normFD, float speedSim)
    {
        if (frechetResultText != null)
        {
            // normFD는 '비유사도'이므로 0에 가까울수록 모양이 유사합니다.
            frechetResultText.text = $"프레셰 거리: {normFD:F3}";
        }
        if (speedResultText != null)
        {
            // speedSim은 '유사도'이므로 1에 가까울수록 속도가 유사합니다.
            speedResultText.text = $"속도 유사도: {speedSim:F3}";
        }
    }

    // [추가] 결과 텍스트를 초기 상태로 되돌리는 함수
    private void ClearResultTexts()
    {
        if (frechetResultText != null)
        {
            frechetResultText.text = "프레셰 거리: -";
        }
        if (speedResultText != null)
        {
            speedResultText.text = "속도 유사도: -";
        }
    }
}