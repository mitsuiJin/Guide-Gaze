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

        if (moveSpeeds == null || moveSpeeds.Length != lanes.Count)
        {
            moveSpeeds = new float[lanes.Count];
            for (int i = 0; i < lanes.Count; i++)
                moveSpeeds[i] = 1f + 0.5f * i;  // 예: 1.0, 1.5, 2.0, 2.5 등 고정 설정
        }

        for (int i = 0; i < lanes.Count; i++)
        {
            var lane = lanes[i];
            float speed = moveSpeeds[i];

            float totalLength = GetPathLength(lane.positions);
            float duration = totalLength / speed;

            CreateAndMoveSquare(lane, duration);
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