using System.Collections.Generic;
using UnityEngine;

public class LaneMatcher : MonoBehaviour
{
    public static LaneMatcher Instance { get; private set; }

    [SerializeField] private GazeLineDrawer gazeLineDrawer;
    [SerializeField] private SquareMoverManager squareMoverManager;
    [SerializeField] private GameObject targetKeyObject; // Inspector에서 타겟 오브젝트 지정

    [Range(0f, 1f)] public float alpha = 0.7f; // Frechet 비중

    private int currentTrialId = 1; // 1부터 시작하여 자동 증가

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

        // 로깅용 리스트
        List<string> laneNames = new List<string>();
        List<float> frechets = new List<float>();
        List<float> speedDiffs = new List<float>();

        for (int i = 0; i < colorLanes.Count; i++)
        {
            var lane = colorLanes[i];
            var lanePath = lane.GetWorldPoints();
            if (lanePath == null || lanePath.Count < 2) continue;

            float laneSpeed = objectSpeeds[i];

            // [1] 프레셰 거리 계산 및 지수 기반 정규화
            float frechet = FrechetDistanceCalculator.Calculate(gazePath, lanePath);
            float normFD = 1f - Mathf.Exp(-frechet);

            // [2] 속도 유사도 계산
            float speedSim = Mathf.Exp(-Mathf.Abs(gazeSpeed - laneSpeed));

            // [3] adjusted 점수 계산
            float adjusted = alpha * normFD + (1f - alpha) * (1f - speedSim);

            Debug.Log($"🔍 {lane.name}: adjusted={adjusted:F3}, normFD={normFD:F3}, speedSim={speedSim:F3}, [gazeSpeed={gazeSpeed:F2}, laneSpeed={laneSpeed:F2}, α={alpha:F1}]");

            // 로깅 리스트에 추가
            laneNames.Add(lane.name);
            frechets.Add(frechet);
            speedDiffs.Add(Mathf.Abs(gazeSpeed - laneSpeed));

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

            // 로깅
            CueLogger logger = FindFirstObjectByType<CueLogger>();
            if (logger != null)
            {
                string targetLaneName = targetKeyObject != null ? targetKeyObject.name : "Unknown";
                logger.LogTrial(currentTrialId, targetLaneName, bestMatch.name, laneNames, frechets, speedDiffs);
            }

            // trial ID 증가
            currentTrialId++;
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
