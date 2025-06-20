// LaneMatcher.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class LaneMatcher : MonoBehaviour
{
    public static LaneMatcher Instance { get; private set; }

    // [추가] 각 레인별 UI 텍스트를 묶어서 관리하기 위한 클래스
    [System.Serializable]
    public class LaneUITargets
    {
        public string laneDirectionName; // 인스펙터에서 알아보기 쉽게 이름 부여 (e.g., "Right", "Top")
        public TextMeshProUGUI frechetText;
        public TextMeshProUGUI speedText;
    }

    [Header("Component References")]
    [SerializeField] private GazeLineDrawer gazeLineDrawer;
    [SerializeField] private SquareMoverManager squareMoverManager;

    [Header("UI References")]
    // [수정] 4개의 레인 UI를 관리할 리스트. 인스펙터에서 크기를 4로 설정하고 순서대로 할당해야 합니다.
    // 순서: 0=Right, 1=Top, 2=Left, 3=Bottom
    [SerializeField] private List<LaneUITargets> laneUIs = new List<LaneUITargets>(4); 
    [SerializeField] private TextMeshProUGUI bestMatchText; // [추가] 최종 선택된 레인을 표시할 텍스트

    [Header("Matching Settings")]
    [Range(0f, 1f)] public float alpha = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ClearResultTexts();
    }

    public void CompareAndFindClosestLane()
    {
        if (gazeLineDrawer == null) gazeLineDrawer = FindFirstObjectByType<GazeLineDrawer>();
        if (squareMoverManager == null) squareMoverManager = FindFirstObjectByType<SquareMoverManager>();
        
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
            ClearResultTexts();
            return;
        }

        List<ColorLaneInfo> colorLanes = ColorLaneManager.Instance.GetAllColorLanes();
        float[] objectSpeeds = squareMoverManager.currentLaneSpeeds;

        if (colorLanes == null || colorLanes.Count == 0 || objectSpeeds == null || objectSpeeds.Length != colorLanes.Count)
        {
            Debug.LogError("❌ ColorLane 또는 속도 배열 문제가 있습니다.");
            ClearResultTexts();
            return;
        }

        ClearResultTexts(); // 비교 시작 전 모든 텍스트 초기화

        float gazeSpeed = CalculatePathSpeed(gazePath, timestamps);

        float minScore = float.MaxValue;
        ColorLaneInfo bestMatchLane = null;
        int bestMatchSlotIndex = -1;

        // colorLanes 리스트의 순서가 보장되지 않으므로, slotIndex를 키로 하는 딕셔너리로 재정렬합니다.
        Dictionary<int, ColorLaneInfo> lanesBySlot = colorLanes.ToDictionary(lane => lane.slotIndex, lane => lane);

        for (int i = 0; i < laneUIs.Count; i++)
        {
            // i는 UI 슬롯 인덱스 (0=R, 1=T, 2=L, 3=B)
            if (!lanesBySlot.ContainsKey(i)) continue; // 해당 슬롯에 레인이 없으면 건너뛰기

            var lane = lanesBySlot[i];
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

            Debug.Log($"🔍 {lane.name} (Slot {i}): adjusted={adjusted:F3}, normFD={normFD:F3}, speedSim={speedSim:F3}");

            // [수정] 계산 결과를 즉시 해당 슬롯의 UI에 업데이트
            UpdateSingleLaneText(i, normFD, speedSim);

            if (adjusted < minScore)
            {
                minScore = adjusted;
                bestMatchLane = lane;
                bestMatchSlotIndex = i;
            }
        }

        if (bestMatchLane != null)
        {
            bestMatchLane.Highlight(true);
            Debug.Log($"✅ 최종 선택된 레인: {bestMatchLane.name} (Slot: {bestMatchSlotIndex})");
            
            // [추가] 최종 선택된 레인 정보 업데이트
            UpdateBestMatchText(bestMatchSlotIndex);
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

    // [추가] 특정 레인의 UI 텍스트만 업데이트하는 함수
    private void UpdateSingleLaneText(int slotIndex, float normFD, float speedSim)
    {
        if (slotIndex < 0 || slotIndex >= laneUIs.Count) return;

        var ui = laneUIs[slotIndex];
        if (ui.frechetText != null)
        {
            ui.frechetText.text = $"{ui.laneDirectionName} 프레셰 거리: {normFD:F3}";
        }
        if (ui.speedText != null)
        {
            ui.speedText.text = $"{ui.laneDirectionName} 속도 유사도: {speedSim:F3}";
        }
    }

    // [추가] 최종 선택된 레인의 방향을 텍스트로 표시하는 함수
    private void UpdateBestMatchText(int slotIndex)
    {
        if (bestMatchText == null) return;

        string directionName = "알 수 없음";
        switch (slotIndex)
        {
            case 0: directionName = "Right"; break;
            case 1: directionName = "Top"; break;
            case 2: directionName = "Left"; break;
            case 3: directionName = "Bottom"; break;
        }
        bestMatchText.text = $"선택: {directionName}";
    }

    // [수정] 모든 UI 텍스트를 초기화하는 함수
    private void ClearResultTexts()
    {
        foreach (var ui in laneUIs)
        {
            if (ui.frechetText != null) ui.frechetText.text = $"{ui.laneDirectionName} 프레셰 거리: -";
            if (ui.speedText != null) ui.speedText.text = $"{ui.laneDirectionName} 속도 유사도: -";
        }

        if (bestMatchText != null)
        {
            bestMatchText.text = "선택: -";
        }
    }
}