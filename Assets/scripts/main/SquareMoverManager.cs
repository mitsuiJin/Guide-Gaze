using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 각 선마다 서로 다른 속도로 네모가 이동하도록 하는 매니저.
/// </summary>
public class SquareMoverManager : MonoBehaviour
{
    [Tooltip("네모 도형에 사용할 Sprite (예: Square, Quad 등)")]
    public Sprite squareSprite;

    [Tooltip("도형의 색상(라인 색상과 동일하게 맞추려면 true)")]
    public bool matchLineColor = true;

    [Tooltip("라인 굵기 대비 도형 크기 계수 (1.5 = 1.5배)")]
    public float sizeMultiplier = 1.5f;

    [Tooltip("각 선별 이동 시간(초). 선 개수와 배열 길이가 같아야 함!")]
    public float[] moveDurations;

    private List<Coroutine> moveCoroutines = new List<Coroutine>();

    private void Start()
    {
        StartCoroutine(WaitAndCreateSquares());
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
        Debug.Log($"[SquareMoverManager] 곡선 개수: {lanes.Count}");

        // 배열 길이 체크
        if (moveDurations == null || moveDurations.Length != lanes.Count)
        {
            Debug.LogWarning("[SquareMoverManager] moveDurations 배열 길이가 곡선 개수와 다릅니다. 기본값(5초)으로 채웁니다.");
            // 배열 길이 맞추기
            moveDurations = new float[lanes.Count];
            for (int i = 0; i < lanes.Count; i++)
                moveDurations[i] = 5f;
        }

        for (int i = 0; i < lanes.Count; i++)
        {
            var lane = lanes[i];
            float duration = moveDurations[i];
            Debug.Log($"[SquareMoverManager] 네모 생성 시도: {lane.name}, 속도: {duration}초");
            CreateAndMoveSquare(lane, duration);
        }
    }

    void CreateAndMoveSquare(ColorLaneInfo lane, float duration)
    {
        GameObject squareObj = new GameObject("MovingSquare_" + lane.name);
        squareObj.transform.parent = lane.transform;

        var sr = squareObj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;

        float lineWidth = lane.lineRenderer.widthMultiplier;
        float size = lineWidth * sizeMultiplier;
        squareObj.transform.localScale = new Vector3(size, size, 1f);

        if (matchLineColor)
            sr.color = lane.lineRenderer.startColor;

        if (lane.positions != null && lane.positions.Count > 0)
        {
            Vector3 startPos = lane.positions[0];
            startPos.z = 0f;
            squareObj.transform.position = startPos;
        }

        Debug.Log($"[SquareMoverManager] 네모 생성: {squareObj.name}, 위치: {squareObj.transform.position}");

        Coroutine moveRoutine = StartCoroutine(MoveSquareLoop(squareObj.transform, lane.positions, duration));
        moveCoroutines.Add(moveRoutine);
    }

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
                pos.z = 0f;
                square.position = pos;

                t += Time.deltaTime / duration;
                yield return null;
            }
        }
    }
}
