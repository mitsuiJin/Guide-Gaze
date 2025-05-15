// LaneMatcher.cs

using System.Collections.Generic;
using UnityEngine;

public class LaneMatcher : MonoBehaviour
{
    [SerializeField] private GazeLineDrawer gazeLineDrawer;  // 시선 경로를 추적하는 GazeLineDrawer 객체

    public void CompareAndFindClosestLane()
    {
        // gazeLineDrawer가 할당되지 않았으면 경고 출력
        if (gazeLineDrawer == null)
        {
            Debug.LogError("❌ GazeLineDrawer가 할당되지 않았습니다.");
            return;
        }

        // 시선 경로(gazePath)를 GazeLineDrawer에서 얻어옴
        List<Vector3> gazePath = gazeLineDrawer.GetGazePoints();
        if (gazePath.Count < 2)
        {
            Debug.LogWarning("⚠️ Gaze 경로가 너무 짧아서 비교할 수 없습니다.");
            return;
        }

        // Gaze 경로의 타임스탬프 리스트를 생성 (시선 경로에 대한 시간 정보)
        List<float> gazeTimestamps = GetTimestamps(gazePath);

        // ColorLane 경로 가져오기 (모든 색상 경로 리스트)
        List<ColorLaneInfo> colorLanes = ColorLaneManager.Instance.GetAllColorLanes();
        if (colorLanes == null || colorLanes.Count == 0)
        {
            Debug.LogError("❌ 등록된 ColorLane이 없습니다.");
            return;
        }

        // 가장 유사한 경로를 찾기 위한 변수들
        float minDistance = float.MaxValue;
        ColorLaneInfo bestMatch = null;
        float bestSpeedMatch = float.MinValue;  // 속도 유사도 저장

        // 모든 Color Lane에 대해 비교 작업을 수행
        foreach (var lane in colorLanes)
        {
            List<Vector3> lanePath = lane.GetWorldPoints();  // ColorLane의 경로

            // ColorLane 경로의 타임스탬프 리스트를 생성 (ColorLane 경로에 대한 시간 정보)
            List<float> laneTimestamps = GetTimestamps(lanePath);

            // 5. 프레셰 거리 계산 (시선 경로와 ColorLane 경로 간의 유사도)
            float distance = FrechetDistanceCalculator.Calculate(gazePath, lanePath);

            // 6. 속도 유사도 계산
            float speedSimilarity = CalculateSpeedSimilarity(gazePath, lanePath, gazeTimestamps, laneTimestamps);  // 속도 유사도 계산 함수

            // 7. 프레셰 거리와 속도 기반 순위 조정 (속도 유사성 비율을 반영하여 거리 계산)
            float adjustedDistance = distance / speedSimilarity;

            // 8. 최종적으로 가장 유사한 경로 선택 (조정된 거리 값이 최소인 경로를 선택)
            if (adjustedDistance < minDistance)
            {
                minDistance = adjustedDistance;
                bestMatch = lane;
                bestSpeedMatch = speedSimilarity;
            }

            // 디버그 로그로 각 경로에 대한 프레셰 거리와 속도 유사도를 출력
            Debug.Log($"🔍 {lane.name}와의 조정된 프레셰 거리: {adjustedDistance:F3}, 속도 유사도: {speedSimilarity:F3}");
        }

        // 가장 유사한 경로를 하이라이트
        if (bestMatch != null)
        {
            bestMatch.Highlight(true);
            Debug.Log($"✅ 가장 유사한 Lane: {bestMatch.name}, 속도 유사도: {bestSpeedMatch:F3}");
        }
    }

    // 경로의 평균 속도를 계산하는 함수 (시선 경로와 Color Lane 경로의 속도 계산)
    private float CalculatePathSpeed(List<Vector3> path, List<float> timestamps)
    {
        if (path == null || path.Count < 2 || timestamps == null || timestamps.Count < 2)
            return 0f;

        float totalDistance = 0f;
        float totalTime = timestamps[timestamps.Count - 1] - timestamps[0];  // 첫 번째와 마지막 타임스탬프 차이

        // 경로의 각 포인트 간 거리 계산
        for (int i = 1; i < path.Count; i++)
        {
            totalDistance += Vector3.Distance(path[i - 1], path[i]);
        }

        return totalTime > 0 ? totalDistance / totalTime : 0f;  // 속도 계산 (단위: 유닛/초)
    }

    // 속도 유사도 계산 함수 (속도 계산 시 타임스탬프 정보도 반영)
    // LaneMatcher.cs

    // LaneMatcher.cs

    private float CalculateSpeedSimilarity(List<Vector3> gazePath, List<Vector3> lanePath, List<float> gazeTimestamps, List<float> laneTimestamps)
    {
        // 1. 시선 경로의 속도 계산
        float trackingLaneSpeed = CalculatePathSpeed(gazePath, gazeTimestamps);

        // 2. Color Lane의 시각화 객체 속도 계산
        float laneSpeed = CalculatePathSpeed(lanePath, laneTimestamps);

        // 3. 속도 차이를 비율로 계산 (두 값이 비슷하면 1에 가까움)
        float speedDifference = Mathf.Abs(laneSpeed - trackingLaneSpeed) / Mathf.Max(laneSpeed, trackingLaneSpeed);

        // 4. 경로 길이에 따른 가중치 조정 (짧은 경로는 속도 차이를 크게 반영하지 않음)
        float pathLengthDifference = Mathf.Abs(GetPathLength(gazePath) - GetPathLength(lanePath)); // 경로 길이 차이
        float lengthWeight = Mathf.Exp(-pathLengthDifference / 3f);  // 경로 길이에 따른 가중치 (3f로 비율을 더 줄여줌)

        // 5. 최종 유사도 계산 (속도 차이와 경로 길이 차이를 반영)
        // 속도 차이를 계산한 후, 가중치로 경로 길이 차이를 보정
        float similarity = Mathf.Exp(-speedDifference) * lengthWeight;

        // 정규화 (0과 1 사이로 값을 조정)
        return Mathf.Clamp01(similarity);
    }





    // 경로의 길이 계산 함수
    private float GetPathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }
        return length;
    }


    // 타임스탬프 생성 함수 (경로에 대한 시간 정보 계산)
    private List<float> GetTimestamps(List<Vector3> path)
    {
        List<float> timestamps = new List<float>();

        // 타임스탬프 생성 (예: 1초 간격으로 타임스탬프를 증가)
        for (int i = 0; i < path.Count; i++)
        {
            timestamps.Add(i);  // 간단히 인덱스를 시간으로 사용
        }

        return timestamps;
    }
}
