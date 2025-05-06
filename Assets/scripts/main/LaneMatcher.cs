// LaneMatcher.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaneMatcher : MonoBehaviour
{
    public ColorLaneManager colorLaneManager;
    public UserLineDrawer userLineDrawer;
    public float matchThreshold = 2.5f; // 🎯 프레셰 거리 임계값
    [Range(0f, 100f)]
    public float minMatchAccuracy = 50f; // 🎯 Inspector에서 설정할 수 있는 정확도 기준 (기본 50%)

    public void CompareAndFindClosestLane()
    {
        List<Vector2> userPoints2D = userLineDrawer.GetDrawnPoints2D();
        if (userPoints2D.Count < 2)
        {
            Debug.LogWarning("사용자 라인 점이 너무 적습니다.");
            return;
        }

        List<List<Vector2>> colorLanesPoints = colorLaneManager.GetAllLanePoints();
        if (colorLanesPoints.Count == 0)
        {
            Debug.LogWarning("Color Lane 리스트가 비어 있습니다.");
            return;
        }

        float minDistance = float.MaxValue;
        int bestMatchIndex = -1;

        for (int i = 0; i < colorLanesPoints.Count; i++)
        {
            float distance = FrechetDistanceCalculator.ComputeFrechetDistance(userPoints2D, colorLanesPoints[i]);

            var info = colorLaneManager.colorLanes[i].GetComponent<ColorLaneInfo>();
            string label = info != null ? info.shortcutName : $"Lane {i}";

            Debug.Log($"{label} → Fréchet Distance: {distance:F2}");

            if (distance < minDistance)
            {
                minDistance = distance;
                bestMatchIndex = i;
            }
        }

        if (bestMatchIndex != -1 && minDistance <= matchThreshold)
        {
            float score = Mathf.Clamp01(1f - (minDistance / matchThreshold));
            float accuracy = score * 100f;

            if (accuracy >= minMatchAccuracy)
            {
                string bestLabel = colorLaneManager.colorLanes[bestMatchIndex].GetComponent<ColorLaneInfo>()?.shortcutName ?? $"Lane {bestMatchIndex}";
                Debug.Log($"✅ Best matching lane: {bestLabel} (정확도: {accuracy:F1}%)");
                StartCoroutine(FlashLine(colorLaneManager.colorLanes[bestMatchIndex]));
            }
            else
            {
                Debug.LogWarning($"❌ 유사한 Color Lane이 존재하지만 정확도 부족 ({accuracy:F1}%)");
            }
        }
        else
        {
            Debug.LogWarning("❌ 일치하는 Color Lane이 존재하지 않습니다 (기준 초과).");
        }
    }

    IEnumerator FlashLine(LineRenderer line)
    {
        Color originalColor = line.startColor;
        for (int i = 0; i < 6; i++)
        {
            Color flashColor = (i % 2 == 0) ? Color.white : originalColor;
            line.startColor = flashColor;
            line.endColor = flashColor;
            line.material.color = flashColor;
            yield return new WaitForSeconds(0.15f);
        }
        line.startColor = originalColor;
        line.endColor = originalColor;
        line.material.color = originalColor;
    }
}