using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gaze Line과 등록된 ColorLane 중 가장 유사한 것을 찾아 하이라이트
/// </summary>
public class LaneMatcher : MonoBehaviour
{
    [SerializeField] private GazeLineDrawer gazeLineDrawer;

    public void CompareAndFindClosestLane()
    {
        if (gazeLineDrawer == null)
        {
            Debug.LogError("❌ GazeLineDrawer가 할당되지 않았습니다.");
            return;
        }

        List<Vector3> gazePath = gazeLineDrawer.GetGazePoints();
        if (gazePath.Count < 2)
        {
            Debug.LogWarning("⚠️ Gaze 경로가 너무 짧아서 비교할 수 없습니다.");
            return;
        }

        List<ColorLaneInfo> colorLanes = ColorLaneManager.Instance.GetAllColorLanes();
        if (colorLanes == null || colorLanes.Count == 0)
        {
            Debug.LogError("❌ 등록된 ColorLane이 없습니다.");
            return;
        }

        float minDistance = float.MaxValue;
        ColorLaneInfo bestMatch = null;

        foreach (var lane in colorLanes)
        {
            List<Vector3> lanePath = lane.GetWorldPoints();
            float distance = FrechetDistanceCalculator.Calculate(gazePath, lanePath);

            Debug.Log($"🔍 {lane.name}와의 프레셰 거리: {distance:F3}");

            if (distance < minDistance)
            {
                minDistance = distance;
                bestMatch = lane;
            }
        }

        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 가장 유사한 Lane: {bestMatch.name}");
        }
    }
}
