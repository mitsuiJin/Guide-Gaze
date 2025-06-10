using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // LINQ를 사용하여 정렬

public class SquareMoverManager : MonoBehaviour
{
    // 싱글톤 패턴: 다른 스크립트에서 쉽게 접근 가능하게 함
    public static SquareMoverManager Instance { get; private set; }

    [Header("Square Properties")]
    public Sprite squareSprite;            // 사용할 Sprite 이미지
    public bool matchLineColor = true;     // 해당 레인의 색상을 네모에 적용할지 여부
    public float sizeMultiplier = 1.5f;    // 네모 크기 조절 계수 (라인 굵기 기준)

    [Header("Movement Settings")]
    public float[] currentLaneSpeeds;      // 각 레인에 실제 할당된 속도 정보
    public float[] predefinedSpeeds = new float[4] { 4.0f, 3.5f, 3.0f, 2.5f }; // 속도 우선순위 배열

    private List<Coroutine> activeMoveCoroutines = new List<Coroutine>(); // 현재 실행 중인 이동 코루틴 목록

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // 레인이 재생성될 때 이벤트 연결
        MultiLineRendererGenerator.OnLanesRegenerated += HandleLanesRegenerated;
    }

    private void OnDisable()
    {
        // 비활성화 시 이벤트 해제 및 정리
        MultiLineRendererGenerator.OnLanesRegenerated -= HandleLanesRegenerated;
        ClearAllMovers();
    }

    private void Start()
    {
        // 시작 시 레인이 준비될 때까지 대기 후 네모 생성
        StartCoroutine(InitialSetupMoversWhenReady());
    }

    private void HandleLanesRegenerated()
    {
        // 레인이 재생성된 경우 한 프레임 대기 후 네모 재설정
        StartCoroutine(ReinitializeMoversAfterFrame());
    }

    private IEnumerator InitialSetupMoversWhenReady()
    {
        // ColorLaneManager 및 레인 준비될 때까지 대기
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

    /// <summary>
    /// 기존 네모 삭제 및 코루틴 정리
    /// </summary>
    private void ClearAllMovers()
    {
        foreach (var coroutine in activeMoveCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        activeMoveCoroutines.Clear();

        // "MovingSquare_"로 시작하는 모든 자식 오브젝트 제거
        foreach (Transform child in transform)
        {
            if (child.gameObject.name.StartsWith("MovingSquare_"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 레인 정보를 기반으로 네모 생성 및 속도 설정
    /// </summary>
    private void SetupMovers()
    {
        ClearAllMovers();

        var lanes = ColorLaneManager.Instance.GetAllColorLanes();
        if (lanes == null || lanes.Count == 0)
        {
            currentLaneSpeeds = new float[0];
            return;
        }

        currentLaneSpeeds = new float[lanes.Count];

        // 레인 길이 기준 내림차순 정렬 (가장 긴 레인이 가장 먼저)
        var sortedLanes = lanes
            .Select(l => new { lane = l, length = GetPathLength(l.positions) })
            .OrderByDescending(l => l.length)
            .ToList();

        // 정렬된 레인에 대해 빠른 속도부터 순서대로 할당
        for (int i = 0; i < sortedLanes.Count; i++)
        {
            var laneInfo = sortedLanes[i].lane;

            // 길이가 긴 순서이므로 속도는 역순으로 할당 (긴 애가 느리게, 짧은 애가 빠르게 X)
            float assignedSpeed = (i < predefinedSpeeds.Length)
                ? predefinedSpeeds[predefinedSpeeds.Length - 1 - i]
                : predefinedSpeeds.First(); // 초과 시 가장 느린 속도 재사용

            int originalIndex = lanes.IndexOf(laneInfo);
            if (originalIndex >= 0 && originalIndex < currentLaneSpeeds.Length)
                currentLaneSpeeds[originalIndex] = assignedSpeed;

            CreateAndMoveSquare(laneInfo, assignedSpeed, originalIndex);
        }

    }

    /// <summary>
    /// 특정 레인에 네모를 생성하고 지정된 속도로 움직이게 함
    /// </summary>
    private void CreateAndMoveSquare(ColorLaneInfo lane, float speed, int uniqueIndex)
    {
        GameObject squareObj = new GameObject($"MovingSquare_on_{lane.gameObject.name}_id{uniqueIndex}");
        squareObj.transform.parent = this.transform;

        var sr = squareObj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;

        // 크기 설정 (라인 굵기 × sizeMultiplier)
        float size = lane.lineRenderer.widthMultiplier * sizeMultiplier;
        squareObj.transform.localScale = new Vector3(size, size, 1f);
        sr.sortingOrder = 2;

        if (matchLineColor)
            sr.color = lane.lineRenderer.startColor;

        // 시작 위치를 경로의 첫 지점으로 설정
        squareObj.transform.position = lane.positions[0];

        // 이동 코루틴 실행
        Coroutine moveRoutine = StartCoroutine(MoveSquareLoop(squareObj.transform, lane.positions, speed));
        activeMoveCoroutines.Add(moveRoutine);
    }

    /// <summary>
    /// 네모가 경로를 일정한 속도로 계속 반복 이동함
    /// </summary>
    private IEnumerator MoveSquareLoop(Transform square, List<Vector3> path, float speed)
    {
        if (square == null || path == null || path.Count < 2)
            yield break;

        int segmentIndex = 0;
        Vector3 start = path[segmentIndex];
        Vector3 end = path[segmentIndex + 1];
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

            // 다음 세그먼트로 이동
            segmentIndex++;
            if (segmentIndex >= path.Count - 1)
                segmentIndex = 0;

            start = path[segmentIndex];
            end = path[segmentIndex + 1];
            square.position = start;
        }
    }

    /// <summary>
    /// 경로의 전체 길이 계산 (벡터 거리 누적합)
    /// </summary>
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
