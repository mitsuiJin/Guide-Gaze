using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareMoverManager : MonoBehaviour
{
    public static SquareMoverManager Instance { get; private set; }

    public Sprite squareSprite;
    public bool matchLineColor = true;
    public float sizeMultiplier = 1.5f;
    public float[] moveSpeeds;

    private List<Coroutine> moveCoroutines = new List<Coroutine>();

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
        StartCoroutine(WaitAndCreateSquares());
    }

    private IEnumerator WaitAndCreateSquares()
    {
        while (ColorLaneManager.Instance == null ||
               ColorLaneManager.Instance.GetAllColorLanes() == null ||
               ColorLaneManager.Instance.GetAllColorLanes().Count == 0)
        {
            yield return null;
        }

        var lanes = ColorLaneManager.Instance.GetAllColorLanes();
        int count = lanes.Count;
        moveSpeeds = new float[count];

        // 사전 정의된 속도 그룹 (길이에 따라 배정됨)
        float[] assignedSpeeds = new float[] { 2.8f, 3.5f, 4.3f, 5.5f };

        // 레인과 길이를 묶어서 리스트로 저장
        var laneLengthPairs = new List<(ColorLaneInfo lane, float length)>();

        for (int i = 0; i < count; i++)
        {
            float len = GetPathLength(lanes[i].positions);
            laneLengthPairs.Add((lanes[i], len));
        }

        // 길이 기준 오름차순 정렬
        laneLengthPairs.Sort((a, b) => a.length.CompareTo(b.length));

        for (int i = 0; i < laneLengthPairs.Count; i++)
        {
            var lane = laneLengthPairs[i].lane;
            float length = laneLengthPairs[i].length;

            // 정해진 속도 배열에서 인덱스에 따라 속도 선택
            float speed = assignedSpeeds[Mathf.Min(i, assignedSpeeds.Length - 1)];
            float duration = length / speed;

            int originalIndex = lanes.IndexOf(lane);
            moveSpeeds[originalIndex] = speed;

            CreateAndMoveSquare(lane, duration);
            Debug.Log($"[Lane {originalIndex}] 길이={length:F2}, 속도={speed:F2}, 도달시간={duration:F2}");
        }
    }

    private void CreateAndMoveSquare(ColorLaneInfo lane, float duration)
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
            squareObj.transform.position = lane.positions[0];
        }

        Coroutine moveRoutine = StartCoroutine(MoveSquareLoop(squareObj.transform, lane.positions, duration));
        moveCoroutines.Add(moveRoutine);
    }

    private IEnumerator MoveSquareLoop(Transform square, List<Vector3> path, float duration)
    {
        int count = path.Count;
        if (count < 2) yield break;

        // [1] 구간별 거리와 누적 거리 계산
        float[] segmentLengths = new float[count - 1];
        float[] accumulatedLengths = new float[count];
        float totalLength = 0f;

        for (int i = 0; i < count - 1; i++)
        {
            float segLen = Vector3.Distance(path[i], path[i + 1]);
            segmentLengths[i] = segLen;
            totalLength += segLen;
            accumulatedLengths[i + 1] = totalLength;
        }

        while (true)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float targetDistance = (elapsed / duration) * totalLength;

                // [2] targetDistance가 포함된 구간 찾기
                int segIndex = 0;
                for (int i = 0; i < accumulatedLengths.Length - 1; i++)
                {
                    if (targetDistance >= accumulatedLengths[i] && targetDistance <= accumulatedLengths[i + 1])
                    {
                        segIndex = i;
                        break;
                    }
                }

                // [3] 해당 구간 내에서의 보간 비율
                float segStart = accumulatedLengths[segIndex];
                float segEnd = accumulatedLengths[segIndex + 1];
                float segmentT = (targetDistance - segStart) / (segEnd - segStart);

                // [4] 위치 계산
                Vector3 pos = Vector3.Lerp(path[segIndex], path[segIndex + 1], segmentT);
                square.position = pos;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }


    private float GetPathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }
        return length;
    }
}
