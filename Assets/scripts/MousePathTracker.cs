using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;

public class MousePathTracker : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private float samplingRate = 0.016f; // 약 60Hz
    [SerializeField] private float similarityThreshold = 0.85f;
    [SerializeField] private int maxPathPoints = 500;
    [SerializeField] private Color userPathColor = Color.red;
    [SerializeField] private Color referencePathColor = Color.blue;
    [SerializeField] private float lineWidth = 0.1f;

    [Header("참조 경로")]
    [SerializeField] private Vector2[] referencePathPoints;
    [SerializeField] private bool loadReferenceFromFile = false;
    [SerializeField] private string referencePathFile = "path.json";

    [Header("이벤트")]
    public UnityEvent onPathMatchSuccess;
    public UnityEvent onPathMatchFail;
    public UnityEvent<float> onSimilarityUpdated;

    // 내부 변수
    private List<Vector2> userPath = new List<Vector2>();
    private List<Vector2> normalizedUserPath = new List<Vector2>();
    private List<Vector2> normalizedReferencePath = new List<Vector2>();
    private float timeSinceLastSample = 0f;
    private bool isTrackingActive = false;
    private LineRenderer userPathRenderer;
    private LineRenderer referencePathRenderer;
    private bool hasComputedSimilarity = false;
    private float currentSimilarity = 0f;

    // 시작 설정
    void Start()
    {
        // 라인 렌더러 생성 및 설정
        SetupLineRenderers();

        // 참조 경로 로드
        if (loadReferenceFromFile)
        {
            LoadReferencePathFromFile();
        }
        else if (referencePathPoints != null && referencePathPoints.Length > 0)
        {
            normalizedReferencePath = NormalizePath(new List<Vector2>(referencePathPoints));
            DrawPath(referencePathRenderer, referencePathPoints);
        }
        else
        {
            Debug.LogError("참조 경로가 설정되지 않았습니다!");
        }
    }

    // 매 프레임 실행
    void Update()
    {
        if (!isTrackingActive) return;

        // 샘플링 타이밍 확인
        timeSinceLastSample += Time.deltaTime;
        if (timeSinceLastSample >= samplingRate)
        {
            // 마우스 위치 가져오기
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
            Vector2 mouseWorldPos = new Vector2(worldPos.x, worldPos.y);

            // 마우스가 움직였고 이전 위치와 다르면 저장
            if (userPath.Count == 0 || Vector2.Distance(mouseWorldPos, userPath[userPath.Count - 1]) > 0.01f)
            {
                userPath.Add(mouseWorldPos);

                // 최대 포인트 수 제한
                if (userPath.Count > maxPathPoints)
                {
                    userPath.RemoveAt(0);
                }

                // 경로 시각화 업데이트
                UpdateUserPathVisualization();

                // 충분한 데이터가 모였으면 유사도 계산
                if (userPath.Count > 10)
                {
                    CalculateAndUpdateSimilarity();
                }
            }

            timeSinceLastSample = 0f;
        }

        // 사용자 입력 처리
        HandleUserInput();
    }

    // 사용자 입력 처리
    private void HandleUserInput()
    {
        // ESC 키를 눌러 추적 시작/중지 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleTracking();
        }

        // Space 키를 눌러 경로 초기화
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetUserPath();
        }

        // Enter 키를 눌러 유사도 확인 및 이벤트 트리거
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            EvaluatePathMatch();
        }
    }

    // 추적 시작/중지 토글
    public void ToggleTracking()
    {
        isTrackingActive = !isTrackingActive;
        Debug.Log(isTrackingActive ? "추적 시작" : "추적 중지");
    }

    // 사용자 경로 초기화
    public void ResetUserPath()
    {
        userPath.Clear();
        normalizedUserPath.Clear();
        hasComputedSimilarity = false;
        currentSimilarity = 0f;
        onSimilarityUpdated?.Invoke(0f);

        // 라인 렌더러 초기화
        if (userPathRenderer != null)
        {
            userPathRenderer.positionCount = 0;
        }

        Debug.Log("사용자 경로 초기화");
    }

    // 라인 렌더러 설정
    private void SetupLineRenderers()
    {
        // 사용자 경로용 라인 렌더러
        GameObject userPathObj = new GameObject("UserPathRenderer");
        userPathObj.transform.SetParent(transform);
        userPathRenderer = userPathObj.AddComponent<LineRenderer>();
        userPathRenderer.startWidth = lineWidth;
        userPathRenderer.endWidth = lineWidth;
        userPathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        userPathRenderer.startColor = userPathColor;
        userPathRenderer.endColor = userPathColor;
        userPathRenderer.positionCount = 0;

        // 참조 경로용 라인 렌더러
        GameObject refPathObj = new GameObject("ReferencePathRenderer");
        refPathObj.transform.SetParent(transform);
        referencePathRenderer = refPathObj.AddComponent<LineRenderer>();
        referencePathRenderer.startWidth = lineWidth;
        referencePathRenderer.endWidth = lineWidth;
        referencePathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        referencePathRenderer.startColor = referencePathColor;
        referencePathRenderer.endColor = referencePathColor;
        referencePathRenderer.positionCount = 0;
    }

    // 사용자 경로 시각화 업데이트
    private void UpdateUserPathVisualization()
    {
        if (userPathRenderer == null || userPath.Count == 0) return;

        userPathRenderer.positionCount = userPath.Count;
        for (int i = 0; i < userPath.Count; i++)
        {
            userPathRenderer.SetPosition(i, new Vector3(userPath[i].x, userPath[i].y, 0));
        }
    }

    // 경로 그리기
    private void DrawPath(LineRenderer renderer, IList<Vector2> path)
    {
        if (renderer == null || path == null || path.Count == 0) return;

        renderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            renderer.SetPosition(i, new Vector3(path[i].x, path[i].y, 0));
        }
    }

    // 파일에서 참조 경로 로드
    private void LoadReferencePathFromFile()
    {
        try
        {
            string json = System.IO.File.ReadAllText(Application.dataPath + "/Resources/" + referencePathFile);
            Vector2[] loadedPath = JsonUtility.FromJson<Vector2[]>(json);
            referencePathPoints = loadedPath;
            normalizedReferencePath = NormalizePath(new List<Vector2>(loadedPath));
            DrawPath(referencePathRenderer, loadedPath);
        }
        catch (Exception e)
        {
            Debug.LogError("참조 경로 파일 로드 실패: " + e.Message);
        }
    }

    // 경로 정규화 (0-1 범위로 변환)
    private List<Vector2> NormalizePath(List<Vector2> path)
    {
        if (path.Count < 2) return path;

        // 경로의 범위 계산
        float minX = path.Min(p => p.x);
        float maxX = path.Max(p => p.x);
        float minY = path.Min(p => p.y);
        float maxY = path.Max(p => p.y);

        // 범위가 너무 작으면 조정
        float rangeX = maxX - minX;
        float rangeY = maxY - minY;
        if (rangeX < 0.001f) rangeX = 1f;
        if (rangeY < 0.001f) rangeY = 1f;

        // 정규화된 경로 생성
        List<Vector2> normalizedPath = new List<Vector2>();
        foreach (Vector2 point in path)
        {
            normalizedPath.Add(new Vector2(
                (point.x - minX) / rangeX,
                (point.y - minY) / rangeY
            ));
        }

        // 일정한 포인트 수(101개)로 리샘플링
        return ResamplePath(normalizedPath, 101);
    }

    // 경로 리샘플링 (일정한 포인트 수로 변환)
    private List<Vector2> ResamplePath(List<Vector2> path, int pointCount)
    {
        if (path.Count < 2) return path;
        if (path.Count == pointCount) return path;

        List<Vector2> result = new List<Vector2>();

        // 첫 번째 포인트 추가
        result.Add(path[0]);

        // 경로의 총 길이 계산
        float totalLength = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            totalLength += Vector2.Distance(path[i - 1], path[i]);
        }

        // 균등한 간격으로 포인트 추가
        float stepSize = totalLength / (pointCount - 1);
        float currentDistance = 0f;
        int currentIndex = 0;

        for (int i = 1; i < pointCount - 1; i++)
        {
            float targetDistance = i * stepSize;

            // 현재 거리가 목표 거리에 도달할 때까지 세그먼트 이동
            while (currentDistance + Vector2.Distance(path[currentIndex], path[currentIndex + 1]) < targetDistance)
            {
                currentDistance += Vector2.Distance(path[currentIndex], path[currentIndex + 1]);
                currentIndex++;
            }

            // 마지막 세그먼트에서 보간
            float remainingDistance = targetDistance - currentDistance;
            float segmentLength = Vector2.Distance(path[currentIndex], path[currentIndex + 1]);
            float t = remainingDistance / segmentLength;

            result.Add(Vector2.Lerp(path[currentIndex], path[currentIndex + 1], t));
        }

        // 마지막 포인트 추가
        result.Add(path[path.Count - 1]);

        return result;
    }

    // 유사도 계산 및 업데이트
    private void CalculateAndUpdateSimilarity()
    {
        // 사용자 경로 정규화
        normalizedUserPath = NormalizePath(userPath);

        // 유사도 계산
        currentSimilarity = ComputePathSimilarity(normalizedUserPath, normalizedReferencePath);
        hasComputedSimilarity = true;

        // 이벤트 발생
        onSimilarityUpdated?.Invoke(currentSimilarity);
    }

    // 경로 매칭 평가 및 이벤트 트리거
    public void EvaluatePathMatch()
    {
        if (!hasComputedSimilarity || normalizedUserPath.Count < 10)
        {
            Debug.Log("유효한 경로 데이터가 부족합니다. 더 많은 포인트를 추적하세요.");
            return;
        }

        // 유사도 임계값 비교 및 이벤트 트리거
        if (currentSimilarity >= similarityThreshold)
        {
            Debug.Log($"경로 매칭 성공! 유사도: {currentSimilarity:F2}");
            onPathMatchSuccess?.Invoke();
        }
        else
        {
            Debug.Log($"경로 매칭 실패. 유사도: {currentSimilarity:F2}, 임계값: {similarityThreshold}");
            onPathMatchFail?.Invoke();
        }
    }

    // 교차 상관 분석을 사용한 경로 유사도 계산
    private float ComputePathSimilarity(List<Vector2> path1, List<Vector2> path2)
    {
        if (path1.Count < 10 || path2.Count < 10)
            return 0f;

        // 두 경로의 신호로 변환 (x와 y 좌표 인터리빙)
        float[] signal1 = new float[path1.Count * 2];
        float[] signal2 = new float[path2.Count * 2];

        for (int i = 0; i < path1.Count; i++)
        {
            signal1[i * 2] = path1[i].x;
            signal1[i * 2 + 1] = path1[i].y;
        }

        for (int i = 0; i < path2.Count; i++)
        {
            signal2[i * 2] = path2[i].x;
            signal2[i * 2 + 1] = path2[i].y;
        }

        // 정규화된 교차 상관 계산
        int maxLag = Mathf.Min(signal1.Length, signal2.Length) / 4;
        float maxCorrelation = 0f;

        for (int lag = -maxLag; lag <= maxLag; lag++)
        {
            float correlation = ComputeNormalizedCrossCorrelation(signal1, signal2, lag);
            maxCorrelation = Mathf.Max(maxCorrelation, correlation);
        }

        return maxCorrelation;
    }

    // 정규화된 교차 상관 계산
    private float ComputeNormalizedCrossCorrelation(float[] signal1, float[] signal2, int lag)
    {
        // 평균 계산
        float mean1 = 0, mean2 = 0;
        foreach (float val in signal1) mean1 += val;
        foreach (float val in signal2) mean2 += val;
        mean1 /= signal1.Length;
        mean2 /= signal2.Length;

        // 교차 상관 및 정규화 팩터 계산
        float numerator = 0;
        float denom1 = 0, denom2 = 0;

        for (int i = 0; i < signal1.Length; i++)
        {
            int j = i + lag;
            if (j < 0 || j >= signal2.Length) continue;

            float diff1 = signal1[i] - mean1;
            float diff2 = signal2[j] - mean2;

            numerator += diff1 * diff2;
            denom1 += diff1 * diff1;
        }

        for (int i = 0; i < signal2.Length; i++)
        {
            float diff2 = signal2[i] - mean2;
            denom2 += diff2 * diff2;
        }

        // 0으로 나누기 방지
        if (denom1 < 0.0001f || denom2 < 0.0001f)
            return 0f;

        return numerator / Mathf.Sqrt(denom1 * denom2);
    }
}
