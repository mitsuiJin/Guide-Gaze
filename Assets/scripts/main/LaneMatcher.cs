using System.Collections.Generic;
using UnityEngine;

public class LaneMatcher : MonoBehaviour
{
    public static LaneMatcher Instance { get; private set; }

    [SerializeField] private GazeLineDrawer gazeLineDrawer;
    [SerializeField] private SquareMoverManager squareMoverManager;

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
            return;
        }

        List<ColorLaneInfo> colorLanes = ColorLaneManager.Instance.GetAllColorLanes();
        float[] objectSpeeds = squareMoverManager.moveSpeeds;

        if (colorLanes == null || colorLanes.Count == 0 || objectSpeeds == null || objectSpeeds.Length != colorLanes.Count)
        {
            Debug.LogError("❌ ColorLane 또는 속도 배열 문제가 있습니다.");
            return;
        }

        float gazeSpeed = CalculatePathSpeed(gazePath, timestamps);

        float minScore = float.MaxValue;
        ColorLaneInfo bestMatch = null;

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
            }
        }

        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 최종 선택된 레인: {bestMatch.name}");
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
}
