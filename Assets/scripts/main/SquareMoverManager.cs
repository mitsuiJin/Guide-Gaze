using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareMoverManager : MonoBehaviour
{
    public static SquareMoverManager Instance { get; private set; }

    [Header("Square Properties")]
    public Sprite squareSprite;
    public bool matchLineColor = true;
    public float sizeMultiplier = 1.5f;

    [Header("Movement Settings")]
    public float[] currentLaneSpeeds;
    public float[] predefinedSpeeds = new float[4] { 2.5f, 3.0f, 3.5f, 4.0f };

    private List<Coroutine> activeMoveCoroutines = new List<Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        MultiLineRendererGenerator.OnLanesRegenerated += HandleLanesRegenerated;
    }

    private void OnDisable()
    {
        MultiLineRendererGenerator.OnLanesRegenerated -= HandleLanesRegenerated;
        ClearAllMovers();
    }

    private void Start()
    {
        StartCoroutine(InitialSetupMoversWhenReady());
    }

    private void HandleLanesRegenerated()
    {
        StartCoroutine(ReinitializeMoversAfterFrame());
    }

    private IEnumerator InitialSetupMoversWhenReady()
    {
        while (ColorLaneManager.Instance == null ||
               ColorLaneManager.Instance.GetAllColorLanes() == null ||
               ColorLaneManager.Instance.GetAllColorLanes().Count == 0)
        {
            yield return null;
        }
        SetupMovers();
    }

    private IEnumerator ReinitializeMoversAfterFrame()
    {
        yield return null;
        SetupMovers();
    }

    private void ClearAllMovers()
    {
        if (activeMoveCoroutines != null)
        {
            foreach (var coroutine in activeMoveCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            activeMoveCoroutines.Clear();
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject.name.StartsWith("MovingSquare_"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SetupMovers()
    {
        ClearAllMovers();

        if (ColorLaneManager.Instance == null)
        {
            Debug.LogWarning("[SquareMoverManager] ColorLaneManager.Instance is null.");
            return;
        }

        var lanes = ColorLaneManager.Instance.GetAllColorLanes();
        if (lanes == null || lanes.Count == 0)
        {
            currentLaneSpeeds = new float[0];
            return;
        }

        int laneCount = lanes.Count;
        currentLaneSpeeds = new float[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            var laneInfo = lanes[i];

            if (laneInfo == null || laneInfo.positions == null || laneInfo.positions.Count < 2 || laneInfo.lineRenderer == null)
            {
                Debug.LogWarning($"[SquareMoverManager] Lane {i} is invalid.");
                if (i < currentLaneSpeeds.Length) currentLaneSpeeds[i] = 0f;
                continue;
            }

            float speed = (i < predefinedSpeeds.Length) ? predefinedSpeeds[i] : predefinedSpeeds[predefinedSpeeds.Length - 1];
            if (speed <= 0) speed = 0.1f;

            if (i < currentLaneSpeeds.Length) currentLaneSpeeds[i] = speed;

            CreateAndMoveSquare(laneInfo, speed, i);
        }
    }

    private void CreateAndMoveSquare(ColorLaneInfo lane, float speed, int uniqueIndex)
    {
        GameObject squareObj = new GameObject($"MovingSquare_on_{lane.gameObject.name}_id{uniqueIndex}");
        squareObj.transform.parent = this.transform;

        var sr = squareObj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;

        if (lane.lineRenderer == null || !lane.lineRenderer.enabled)
        {
            Debug.LogError($"[SquareMoverManager] LineRenderer is null or disabled on {lane.gameObject.name}");
            Destroy(squareObj);
            return;
        }

        float size = lane.lineRenderer.widthMultiplier * sizeMultiplier;
        squareObj.transform.localScale = new Vector3(size, size, 1f);
        sr.sortingOrder = 2;

        if (matchLineColor)
        {
            sr.color = lane.lineRenderer.startColor;
        }

        squareObj.transform.position = lane.positions[0];

        Coroutine moveRoutine = StartCoroutine(MoveSquareLoop(squareObj.transform, lane.positions, speed));
        activeMoveCoroutines.Add(moveRoutine);
    }

    private IEnumerator MoveSquareLoop(Transform square, List<Vector3> path, float speed)
    {
        if (square == null || path == null || path.Count < 2)
            yield break;

        int currentSegment = 0;
        Vector3 start = path[currentSegment];
        Vector3 end = path[currentSegment + 1];
        square.position = start;

        while (true)
        {
            float traveled = 0f;
            float segmentLength = Vector3.Distance(start, end);

            while (traveled < segmentLength)
            {
                if (square == null) yield break;

                float step = speed * Time.deltaTime;
                traveled += step;
                float t = Mathf.Clamp01(traveled / segmentLength);
                square.position = Vector3.Lerp(start, end, t);

                yield return null;
            }

            currentSegment++;
            if (currentSegment >= path.Count - 1)
            {
                currentSegment = 0;
            }

            start = path[currentSegment];
            end = path[currentSegment + 1];
            square.position = start;
        }
    }

    private float GetPathLength(List<Vector3> path)
    {
        if (path == null || path.Count < 2) return 0f;

        float length = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector3.Distance(path[i], path[i + 1]);
        }
        return length;
    }
}
