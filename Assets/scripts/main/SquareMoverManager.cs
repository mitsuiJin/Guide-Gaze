// SquareMoverManager.cs

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

        //const float MIN_SPEED = 2.7f;
        //const float MAX_SPEED = 7.2f;

        // N개 속도를 균등 분포로 생성
        float[] assignedSpeeds = new float[4]
         {
            5.5f, // 느림 (예: Z)
            3.0f, // 중간 (예: X)
            6.5f, // 빠름 (예: C)
            2.5f  // 가장 빠름 (예: V) → 이 이상은 피하는 게 좋음
         };

        for (int i = 0; i < count; i++)
        {
            var lane = lanes[i];
            float length = GetPathLength(lane.positions);
            float speed = assignedSpeeds[i];
            float duration = length / speed;

            moveSpeeds[i] = speed;
            CreateAndMoveSquare(lane, duration);

            Debug.Log($"[Lane {i}] 길이={length:F2}, 속도={speed:F2}, 도달시간={duration:F2}");
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