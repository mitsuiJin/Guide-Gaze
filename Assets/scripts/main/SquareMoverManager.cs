// SquareMoverManager.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareMoverManager : MonoBehaviour
{
    public Sprite squareSprite;  // 시각화 객체의 스프라이트
    public bool matchLineColor = true;  // 라인 색상 일치 여부
    public float sizeMultiplier = 1.5f;  // 크기 조정 배율
    public float[] moveSpeeds;  // 각 Lane에 대한 이동 속도 배열

    private List<Coroutine> moveCoroutines = new List<Coroutine>();

    private void Start()
    {
        StartCoroutine(WaitAndCreateSquares());
    }

    // 경로의 평균 속도 계산 함수 (시각화 객체의 이동 속도 계산)
    private float CalculateObjectSpeed(List<Vector3> path, float moveDuration)
    {
        if (path == null || path.Count < 2)
            return 0f;

        // 경로의 전체 길이 계산
        float totalDistance = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            totalDistance += Vector3.Distance(path[i - 1], path[i]);
        }

        // 이동 시간을 이용하여 속도 계산 (속도 = 거리 / 시간)
        return totalDistance / moveDuration;  // 속도 계산 (단위: 유닛/초)
    }

    // Tracking Lane (시선 추적 경로)의 속도 계산
    private float CalculatePathSpeed(List<Vector3> path)
    {
        float totalDistance = 0f;
        float totalTime = path.Count;  // 예시: 1초 간격으로 샘플링된 것으로 가정

        for (int i = 1; i < path.Count; i++)
        {
            totalDistance += Vector3.Distance(path[i - 1], path[i]);
        }

        return totalTime > 0 ? totalDistance / totalTime : 0f;  // 속도 계산 (단위: 유닛/초)
    }

    // 속도 유사도 계산 함수
    public float CalculateSpeedSimilarity(List<Vector3> gazePath, List<Vector3> lanePath)
    {
        // 1. 시선 경로의 속도 계산
        float trackingLaneSpeed = CalculatePathSpeed(gazePath);

        // 2. Color Lane의 시각화 객체 속도 계산
        float laneSpeed = CalculateObjectSpeed(lanePath, moveSpeeds[0]);  // 예시: moveSpeeds[0]을 속도로 사용

        // 3. 속도 차이를 비율로 계산 (두 값이 비슷하면 1에 가까움)
        float speedDifference = Mathf.Abs(laneSpeed - trackingLaneSpeed) / Mathf.Max(laneSpeed, trackingLaneSpeed);

        // 4. 차이가 크면 유사도가 낮고, 차이가 작으면 유사도가 높도록 설정
        float similarity = Mathf.Exp(-speedDifference);  // 1에 가까울수록 유사도가 높음

        return similarity;
    }

    IEnumerator WaitAndCreateSquares()
    {
        while (ColorLaneManager.Instance == null ||
               ColorLaneManager.Instance.GetAllColorLanes() == null ||
               ColorLaneManager.Instance.GetAllColorLanes().Count == 0)
        {
            yield return null;
        }

        var lanes = ColorLaneManager.Instance.GetAllColorLanes();
        if (moveSpeeds == null || moveSpeeds.Length != lanes.Count)
        {
            moveSpeeds = new float[lanes.Count];
            for (int i = 0; i < lanes.Count; i++)
                moveSpeeds[i] = 1f;  // 기본 속도 1f로 설정
        }

        // 각 color lane에 대해 시각화 객체를 생성하고 이동시키기
        for (int i = 0; i < lanes.Count; i++)
        {
            var lane = lanes[i];
            float speed = moveSpeeds[i];  // 현재 lane에 대한 이동 속도

            // 전체 곡선 길이 측정
            float totalLength = GetPathLength(lane.positions);
            float duration = totalLength / speed;  // 속도에 맞춰 이동 시간 계산

            CreateAndMoveSquare(lane, duration);
        }
    }

    // 시각화 객체 생성 및 이동 함수
    void CreateAndMoveSquare(ColorLaneInfo lane, float duration)
    {
        GameObject squareObj = new GameObject("MovingSquare_" + lane.name);
        squareObj.transform.parent = lane.transform;

        var sr = squareObj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        float size = lane.lineRenderer.widthMultiplier * sizeMultiplier;
        squareObj.transform.localScale = new Vector3(size, size, 1f);

        if (matchLineColor)
            sr.color = lane.lineRenderer.startColor;

        if (lane.positions != null && lane.positions.Count > 0)
        {
            Vector3 startPos = lane.positions[0];
            squareObj.transform.position = startPos;
        }

        // 시각화 객체 이동 루틴 시작
        Coroutine moveRoutine = StartCoroutine(MoveSquareLoop(squareObj.transform, lane.positions, duration));
        moveCoroutines.Add(moveRoutine);
    }

    // 시각화 객체를 경로를 따라 이동시키는 코루틴
    IEnumerator MoveSquareLoop(Transform square, List<Vector3> path, float duration)
    {
        int count = path.Count;
        if (count < 2)
            yield break;

        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                float total = (count - 1);
                float ft = t * total;
                int idx = Mathf.FloorToInt(ft);
                int nextIdx = Mathf.Min(idx + 1, count - 1);
                float lerpT = ft - idx;

                Vector3 pos = Vector3.Lerp(path[idx], path[nextIdx], lerpT);
                square.position = pos;

                t += Time.deltaTime / duration;
                yield return null;
            }
        }
    }

    // 경로의 길이 계산
    float GetPathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }
        return length;
    }
}
