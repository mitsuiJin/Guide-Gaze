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
        List<float> userTimes = userLineDrawer.GetDrawnTimes();

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

        float minCombinedScore = float.MaxValue;
        int bestMatchIndex = -1;

        // 프레셰:0.7, DTW:0.3 가중치 (인스펙터에서 조절 가능)
        float frechetWeight = 0.7f;
        float dtwWeight = 0.3f;

        for (int i = 0; i < colorLanesPoints.Count; i++)
        {
            // 1. 프레셰 거리 계산 (공간 유사도)
            float frechetDist = FrechetDistanceCalculator.ComputeFrechetDistance(userPoints2D, colorLanesPoints[i]);

            // 2. DTW 거리 계산 (시간 유사도)
            var info = colorLaneManager.colorLanes[i].GetComponent<ColorLaneInfo>();
            List<float> refTimes = info?.referenceTimes ?? new List<float>();
            float dtwDist = refTimes.Count > 0 ?
                DTWCalculator.CalculateDTW(userTimes, refTimes) :
                Mathf.Infinity; // 기준 시간 없으면 무한대 처리

            // 3. 종합 점수 계산
            float combinedScore = (frechetDist * frechetWeight) + (dtwDist * dtwWeight);

            // 디버그 정보 출력
            string label = info != null ? info.shortcutName : $"Lane {i}";
            Debug.Log($"{label} → 프레셰: {frechetDist:F2}, DTW: {dtwDist:F2}, 종합: {combinedScore:F2}");

            // 최소 점수 갱신
            if (combinedScore < minCombinedScore)
            {
                minCombinedScore = combinedScore;
                bestMatchIndex = i;
            }
        }

        // 임계값 비교 (matchThreshold는 종합 점수 기준)
        if (bestMatchIndex != -1 && minCombinedScore <= matchThreshold)
        {
            float accuracy = Mathf.Clamp01(1f - (minCombinedScore / matchThreshold)) * 100f;
            string bestLabel = colorLaneManager.colorLanes[bestMatchIndex].GetComponent<ColorLaneInfo>()?.shortcutName
                ?? $"Lane {bestMatchIndex}";

            if (accuracy >= minMatchAccuracy)
            {
                Debug.Log($"✅ 매칭 성공: {bestLabel} (정확도 {accuracy:F1}%)");
                StartCoroutine(FlashLine(colorLaneManager.colorLanes[bestMatchIndex]));
            }
            else
            {
                Debug.LogWarning($"❌ 정확도 부족: {accuracy:F1}% (필요 {minMatchAccuracy}%)");
            }
        }
        else
        {
            Debug.LogWarning("❌ 조건을 만족하는 패턴이 없습니다.");
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