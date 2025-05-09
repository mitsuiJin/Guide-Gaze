using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// 시선 추적 데이터를 기반으로 선을 그리는 스크립트 (Space 키로 시작/종료)
/// 종료 시 Frechet Distance를 이용해 가장 유사한 ColorLane을 찾아 하이라이트
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GazeLineDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<Vector3> gazePoints = new List<Vector3>();
    private const int maxPoints = 200;
    private bool isTracking = false;

    public LaneMatcher laneMatcher; // Inspector에서 할당 필요

    [Header("Gaze 좌표 오프셋 (보정용)")]
    [SerializeField] private Vector2 gazeOffset = new Vector2(0.5f, 0.15f);

    /// <summary>
    /// 현재 시선으로 그린 선의 위치 리스트 반환
    /// </summary>
    public List<Vector3> GetGazePoints()
    {
        return new List<Vector3>(gazePoints);
    }

    void Start()
    {
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.widthMultiplier = 0.05f;

        Debug.Log("🎯 GazeLineDrawer 초기화 완료");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTracking)
                EndTracking();
            else
                StartTracking();
        }

        if (!isTracking) return;

        TobiiGameIntegrationApi.Update();
        GazePoint gp;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gp))
        {
            // 🔧 오프셋 보정 적용
            float x = Mathf.Clamp01(gp.X + gazeOffset.x);
            float y = Mathf.Clamp01(gp.Y + gazeOffset.y);

            Vector3 screenPos = new Vector3(x * Screen.width, y * Screen.height, 10f);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            gazePoints.Add(worldPos);
            if (gazePoints.Count > maxPoints)
                gazePoints.RemoveAt(0);

            lineRenderer.positionCount = gazePoints.Count;
            lineRenderer.SetPositions(gazePoints.ToArray());
        }
    }

    void StartTracking()
    {
        isTracking = true;
        gazePoints.Clear();
        lineRenderer.positionCount = 0;
        Debug.Log("🔺 시선 추적 시작");
    }

    void EndTracking()
    {
        isTracking = false;
        Debug.Log("🔻 시선 추적 정지 및 분석 준비 완료");

        if (laneMatcher != null)
        {
            laneMatcher.CompareAndFindClosestLane();
        }
        else
        {
            Debug.LogWarning("❗ LaneMatcher가 연결되지 않음");
        }
    }
}
