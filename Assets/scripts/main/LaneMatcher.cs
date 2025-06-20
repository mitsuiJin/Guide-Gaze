// LaneMatcher.cs
using System.Collections.Generic;
using UnityEngine;

public class LaneMatcher : MonoBehaviour
{
    public static LaneMatcher Instance { get; private set; }

    // 계산이 완료되었을 때 호출될 이벤트 정의
    // 파라미터: <모든 차선 결과 리스트, 최종 선택된 차선 이름>
    public static event System.Action<List<LaneResultData>, string> OnComparisonComplete;

    [Header("Component References")]
    [SerializeField] private GazeLineDrawer gazeLineDrawer;
    [SerializeField] private SquareMoverManager squareMoverManager;

    [Header("Matching Settings")]
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
        if (gazeLineDrawer == null) gazeLineDrawer = FindFirstObjectByType<GazeLineDrawer>();
        if (squareMoverManager == null) squareMoverManager = FindFirstObjectByType<SquareMoverManager>();

        if (gazeLineDrawer == null || squareMoverManager == null)
        {
            Debug.LogError("❌ 필수 컴포넌트가 누락되었습니다.");
            OnComparisonComplete?.Invoke(null, null); // 리스너에게 결과 없음을 알림
            return;
        }

        List<Vector3> gazePath = gazeLineDrawer.GetGazePoints();
        List<float> timestamps = gazeLineDrawer.GetGazeTimestamps();

        if (gazePath == null || gazePath.Count < 2 || timestamps == null || timestamps.Count < 2)
        {
            Debug.LogWarning("⚠️ Gaze 경로 또는 타임스탬프가 유효하지 않습니다.");
            OnComparisonComplete?.Invoke(null, null); // 리스너에게 결과 없음을 알림
            return;
        }

        List<ColorLaneInfo> colorLanes = ColorLaneManager.Instance.GetAllColorLanes();
        float[] objectSpeeds = squareMoverManager.currentLaneSpeeds;

        if (colorLanes == null || colorLanes.Count == 0 || objectSpeeds == null || objectSpeeds.Length != colorLanes.Count)
        {
            Debug.LogError("❌ ColorLane 또는 속도 배열 문제가 있습니다.");
            OnComparisonComplete?.Invoke(null, null); // 리스너에게 결과 없음을 알림
            return;
        }

        float gazeSpeed = CalculatePathSpeed(gazePath, timestamps);

        float minScore = float.MaxValue;
        ColorLaneInfo bestMatch = null;
        
        // 모든 차선의 결과 데이터를 담을 리스트 생성
        List<LaneResultData> allLaneResults = new List<LaneResultData>();

        for (int i = 0; i < colorLanes.Count; i++)
        {
            var lane = colorLanes[i];
            var lanePath = lane.GetWorldPoints();
            if (lanePath == null || lanePath.Count < 2) continue;

            float laneSpeed = objectSpeeds[i];

            // [1] 프레셰 거리 계산 후 정규화
            float frechet = FrechetDistanceCalculator.Calculate(gazePath, lanePath);
            float normFD = 1f - Mathf.Exp(-frechet);

            // [2] 속도 유사도 계산
            float perceptualRatio = 0.8f;
            float perceptualLaneSpeed = laneSpeed * perceptualRatio;
            float speedSim = Mathf.Exp(-Mathf.Abs(gazeSpeed - perceptualLaneSpeed));

            // [3] 통합 유사도 점수 계산
            float adjusted = alpha * normFD + (1f - alpha) * (1f - speedSim);

            Debug.Log($"🔍 {lane.name}: adjusted={adjusted:F3}, normFD={normFD:F3}, speedSim={speedSim:F3}, [gazeSpeed={gazeSpeed:F2}, laneSpeed={laneSpeed:F2}, α={alpha:F1}]");
            
            // 현재 차선의 결과 정보를 리스트에 추가
            allLaneResults.Add(new LaneResultData(lane.name, lane.keyName, normFD, speedSim));

            if (adjusted < minScore)
            {
                minScore = adjusted;
                bestMatch = lane;
            }
        }
        
        // 루프가 끝난 후, 계산된 모든 결과와 최종 선택된 차선 이름을 이벤트로 전달
        OnComparisonComplete?.Invoke(allLaneResults, bestMatch?.keyName);

        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 최종 선택된 레인: {bestMatch.keyName} (오브젝트명: {bestMatch.name})");
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