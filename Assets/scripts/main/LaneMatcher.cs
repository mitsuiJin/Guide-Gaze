// LaneMatcher.cs

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

        float minAdjustedDistance = float.MaxValue;
        ColorLaneInfo bestMatch = null;
        float bestSpeedSim = 0f;

        for (int i = 0; i < colorLanes.Count; i++)
        {
            var lane = colorLanes[i];
            var lanePath = lane.GetWorldPoints();
            if (lanePath == null || lanePath.Count < 2) continue;

            float laneSpeed = objectSpeeds[i];
            float frechet = FrechetDistanceCalculator.Calculate(gazePath, lanePath);
            float speedSim = CalculateSpeedSimilarity(gazeSpeed, laneSpeed);
            float adjusted = frechet / Mathf.Max(speedSim, 0.0001f);

            Debug.Log($"🔍 {lane.name}: 프레셰={frechet:F3}, 속도 유사도={speedSim:F3}, 조정거리={adjusted:F3} [gazeSpeed={gazeSpeed:F2}, laneSpeed={laneSpeed:F2}]");

            if (adjusted < minAdjustedDistance)
            {
                minAdjustedDistance = adjusted;
                bestMatch = lane;
                bestSpeedSim = speedSim;
            }
        }

        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 가장 유사한 Lane: {bestMatch.name}, 속도 유사도: {bestSpeedSim:F3}");
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

    private float CalculateSpeedSimilarity(float gazeSpeed, float objectSpeed)
    {
        if (gazeSpeed <= 0f || objectSpeed <= 0f) return 0f;
        float diffRatio = Mathf.Abs(gazeSpeed - objectSpeed) / Mathf.Max(gazeSpeed, objectSpeed);
        return Mathf.Clamp01(Mathf.Exp(-diffRatio));
    }
}