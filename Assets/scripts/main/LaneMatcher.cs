using System.Collections.Generic;
using UnityEngine;

public class LaneMatcher : MonoBehaviour
{
    public static LaneMatcher Instance { get; private set; }

    [SerializeField] private GazeLineDrawer gazeLineDrawer;
    [SerializeField] private SquareMoverManager squareMoverManager;

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

            // [1] Frechet 거리 계산
            float frechet = FrechetDistanceCalculator.Calculate(gazePath, lanePath);

            // [2] 방향 포함 속도 차이 계산 (절댓값 X)
            float speedDiff = gazeSpeed - laneSpeed;

            // [3] Z-score 정규화 (평균과 표준편차는 고정값 사용)
            float zFrechet = (frechet - 0.5492f) / 0.1752f;
            float zSpeedDiff = (speedDiff - (-0.0439f)) / 0.3927f;

            // [4] 신뢰도 기반 가중 평균 (α = 0.834)
            float matchError = 0.834f * zFrechet + 0.166f * zSpeedDiff;

            Debug.Log($"🔍 {lane.name}: matchError={matchError:F3}, zFrechet={zFrechet:F3}, zSpeedDiff={zSpeedDiff:F3}, frechet={frechet:F3}, speedDiff={speedDiff:F3}, gazeSpeed={gazeSpeed:F2}, laneSpeed={laneSpeed:F2}");

            if (matchError < minScore)
            {
                minScore = matchError;
                bestMatch = lane;
            }
        }

        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 최종 선택된 레인: {bestMatch.name}");
        }
    }

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
