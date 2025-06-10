// SquareMoverManager.cs
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
    // 각 레인별 네모의 속도 (참고용으로 Inspector에 표시될 수 있음)
    public float[] currentLaneSpeeds;
    // 미리 정의된 속도 세트 (4개의 레인에 순서대로 할당됨)
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
        // MultiLineRendererGenerator의 이벤트 구독
        MultiLineRendererGenerator.OnLanesRegenerated += HandleLanesRegenerated;
        // Debug.Log("[SquareMoverManager] Subscribed to OnLanesRegenerated event.");
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        MultiLineRendererGenerator.OnLanesRegenerated -= HandleLanesRegenerated;
        // Debug.Log("[SquareMoverManager] Unsubscribed from OnLanesRegenerated event.");
        ClearAllMovers(); // 오브젝트 비활성화/파괴 시 정리
    }

    private void Start()
    {
        // 초기 네모 생성 (ColorLaneManager가 준비될 때까지 대기)
        StartCoroutine(InitialSetupMoversWhenReady());
    }

    /// <summary>
    /// MultiLineRendererGenerator에서 레인이 재생성되었다는 이벤트를 처리합니다.
    /// </summary>
    private void HandleLanesRegenerated()
    {
        // Debug.Log("[SquareMoverManager] Received OnLanesRegenerated event. Re-initializing movers.");
        // 새 라인이 완전히 준비될 수 있도록 한 프레임 대기 후 실행 (안정성 확보)
        StartCoroutine(ReinitializeMoversAfterFrame());
    }

    /// <summary>
    /// 게임 시작 시 또는 초기 레인이 준비되었을 때 네모들을 설정하는 코루틴입니다.
    /// </summary>
    private IEnumerator InitialSetupMoversWhenReady()
    {
        // ColorLaneManager와 해당 레인이 준비될 때까지 대기
        // OnLanesRegenerated 이벤트가 Start보다 먼저 발생할 수도 있으므로, 이 대기는 여전히 유용합니다.
        while (ColorLaneManager.Instance == null ||
               ColorLaneManager.Instance.GetAllColorLanes() == null ||
               ColorLaneManager.Instance.GetAllColorLanes().Count == 0)
        {
            yield return null; // 다음 프레임까지 대기
        }
        // Debug.Log("[SquareMoverManager] Initial lanes are ready or detected. Proceeding with SetupMovers.");
        SetupMovers();
    }

    /// <summary>
    /// 레인 재생성 이벤트 후 한 프레임 대기하고 네모들을 재설정합니다.
    /// </summary>
    private IEnumerator ReinitializeMoversAfterFrame()
    {
        yield return null; // 다음 프레임까지 대기하여 새 레인 정보가 완전히 반영되도록 함
        SetupMovers();
    }

    /// <summary>
    /// 모든 기존 네모와 관련 코루틴을 정리합니다.
    /// </summary>
    private void ClearAllMovers()
    {
        // Debug.Log("[SquareMoverManager] Clearing all existing movers...");
        if (activeMoveCoroutines != null)
        {
            foreach (var coroutine in activeMoveCoroutines)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            activeMoveCoroutines.Clear();
        }

        // SquareMoverManager의 자식으로 있는 모든 MovingSquare 오브젝트 삭제
        foreach (Transform child in transform)
        {
            if (child.gameObject.name.StartsWith("MovingSquare_"))
            {
                Destroy(child.gameObject);
            }
        }
        // Debug.Log("[SquareMoverManager] All movers cleared.");
    }

    /// <summary>
    /// 현재 활성화된 레인들을 기반으로 네모들을 설정하고 이동시킵니다.
    /// </summary>
    private void SetupMovers()
    {
        ClearAllMovers(); // 이전 네모들 확실히 정리

        if (ColorLaneManager.Instance == null)
        {
            Debug.LogWarning("[SquareMoverManager] ColorLaneManager.Instance is null. Cannot setup movers.");
            return;
        }

        var lanes = ColorLaneManager.Instance.GetAllColorLanes();
        if (lanes == null || lanes.Count == 0)
        {
            // Debug.Log("[SquareMoverManager] No lanes available from ColorLaneManager. Movers will not be set up.");
            currentLaneSpeeds = new float[0]; // 속도 배열도 초기화
            return;
        }

        int laneCount = lanes.Count;
        // Debug.Log($"[SquareMoverManager] Setting up movers for {laneCount} lanes.");

        currentLaneSpeeds = new float[laneCount]; // 현재 레인 수에 맞게 속도 배열 크기 조정

        for (int i = 0; i < laneCount; i++)
        {
            var laneInfo = lanes[i];

            // 레인 정보 유효성 검사
            if (laneInfo == null || laneInfo.positions == null || laneInfo.positions.Count < 2 || laneInfo.lineRenderer == null)
            {
                Debug.LogWarning($"[SquareMoverManager] Lane {i} (Name: {(laneInfo?.gameObject.name ?? "N/A")}) is invalid or has insufficient data. Skipping mover creation for this lane.");
                if (i < currentLaneSpeeds.Length) currentLaneSpeeds[i] = 0f; // 해당 레인 속도 0으로 기록
                continue;
            }

            float length = GetPathLength(laneInfo.positions);

            // predefinedSpeeds 배열의 크기를 넘어서는 인덱스 접근 방지
            float speed = (i < predefinedSpeeds.Length) ? predefinedSpeeds[i] : predefinedSpeeds[predefinedSpeeds.Length - 1];

            if (speed <= 0) // 속도가 0 이하인 경우 처리
            {
                Debug.LogWarning($"[SquareMoverManager] Lane {i} ('{laneInfo.gameObject.name}') has invalid speed ({speed}). Using a very small default speed.");
                speed = 0.1f; // 0으로 나누는 것을 방지하기 위한 매우 느린 속도
            }
            float duration = length / speed;

            if (i < currentLaneSpeeds.Length) currentLaneSpeeds[i] = speed; // 계산된 속도 저장

            CreateAndMoveSquare(laneInfo, duration, i);
            // Debug.Log($"[SquareMoverManager] Created mover for Lane {i} ('{laneInfo.gameObject.name}'). Length={length:F2}, Speed={speed:F2}, Duration={duration:F2}");
        }
    }

    /// <summary>
    /// 특정 레인에 네모를 생성하고 이동 코루틴을 시작합니다.
    /// </summary>
    private void CreateAndMoveSquare(ColorLaneInfo lane, float duration, int uniqueIndex)
    {
        // 네이밍에 lane의 GameObject 이름을 사용하여 어떤 레인에 속한 네모인지 구분 용이하게 함
        GameObject squareObj = new GameObject($"MovingSquare_on_{lane.gameObject.name}_id{uniqueIndex}");
        squareObj.transform.parent = this.transform; // SquareMoverManager의 자식으로 설정

        var sr = squareObj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;

        if (lane.lineRenderer == null || !lane.lineRenderer.enabled)
        {
            Debug.LogError($"[SquareMoverManager] Cannot create square for lane '{lane.gameObject.name}'. LineRenderer is null or disabled.");
            Destroy(squareObj);
            return;
        }

        float size = lane.lineRenderer.widthMultiplier * sizeMultiplier;
        squareObj.transform.localScale = new Vector3(size, size, 1f);
        sr.sortingOrder = 2; // 라인보다 위에 보이도록 (라인의 sortingOrder가 1 또는 그 이하로 가정)

        if (matchLineColor)
        {
            sr.color = lane.lineRenderer.startColor;
        }

        if (lane.positions != null && lane.positions.Count > 0)
        {
            squareObj.transform.position = lane.positions[0]; // 경로의 시작점에 배치
        }
        else
        {
            // 경로 정보가 없는 경우 (이론상 발생하면 안됨)
            Debug.LogWarning($"[SquareMoverManager] Lane '{lane.gameObject.name}' has no positions. Square will start at origin.");
            squareObj.transform.position = Vector3.zero;
        }

        Coroutine moveRoutine = StartCoroutine(MoveSquareLoop(squareObj.transform, lane.positions, duration));
        activeMoveCoroutines.Add(moveRoutine);
    }

    /// <summary>
    /// 네모가 지정된 경로를 따라 반복적으로 이동하는 코루틴입니다.
    /// </summary>
    private IEnumerator MoveSquareLoop(Transform square, List<Vector3> path, float duration)
    {
        // 입력 값 유효성 검사
        if (square == null || path == null || path.Count < 2)
        {
            // Debug.LogError("[SquareMoverManager] MoveSquareLoop cannot start due to invalid parameters (square, path, or path count).");
            yield break;
        }
        if (duration <= 0)
        {
            // Debug.LogError($"[SquareMoverManager] MoveSquareLoop cannot start due to invalid duration: {duration}. Setting square to end of path.");
            square.position = path[path.Count - 1]; // 바로 끝점으로 이동
            yield break;
        }


        while (true) // 경로를 따라 무한 반복 이동
        {
            square.position = path[0]; // 매 루프 시작 시, 경로의 첫 번째 위치로 네모를 리셋

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                if (square == null) yield break; // 이동 중 네모가 파괴된 경우 코루틴 중지

                float t = elapsedTime / duration; // 현재 진행도 (0에서 1 사이)

                // 경로상의 정확한 위치 계산
                float totalSegments = path.Count - 1;
                float currentPathProgress = t * totalSegments; // 전체 경로에서의 진행도 (예: 0 ~ 3 사이 값, 점이 4개일 경우)
                int currentIndex = Mathf.FloorToInt(currentPathProgress);
                // 다음 인덱스가 경로 길이를 초과하지 않도록 보장
                int nextIndex = Mathf.Min(currentIndex + 1, path.Count - 1);

                // 현재 세그먼트 내에서의 보간 값 (0에서 1 사이)
                float segmentLerpT = currentPathProgress - currentIndex;

                Vector3 pos = Vector3.Lerp(path[currentIndex], path[nextIndex], segmentLerpT);
                square.position = pos;

                elapsedTime += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }
            // 한 바퀴 이동 완료 후, 네모를 경로의 마지막 지점에 정확히 위치시킴
            if (square != null) square.position = path[path.Count - 1];

            // 선택: 한 루프 완료 후 잠시 대기하고 다시 시작하려면 아래 주석 해제
            // yield return new WaitForSeconds(0.1f); 
        }
    }

    /// <summary>
    /// 주어진 경로(점들의 리스트)의 총 길이를 계산합니다.
    /// </summary>
    private float GetPathLength(List<Vector3> path)
    {
        if (path == null || path.Count < 2) return 0f;

        float length = 0f;
        for (int i = 0; i < path.Count - 1; i++) // path.Count - 1 까지 반복해야 마지막 세그먼트 포함
        {
            length += Vector3.Distance(path[i], path[i + 1]);
        }
        return length;
    }
}