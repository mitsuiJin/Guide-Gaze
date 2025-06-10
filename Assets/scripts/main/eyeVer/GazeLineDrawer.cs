using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(LineRenderer))]
public class GazeLineDrawer : MonoBehaviour
{
    public static GazeLineDrawer Instance { get; private set; }

    private LineRenderer lineRenderer;
    private List<Vector3> gazePoints = new List<Vector3>();
    private List<float> gazeTimestamps = new List<float>();

    private const int maxPoints = 200;
    private bool isTracking = false;

    public LaneMatcher laneMatcher;

    [Header("Gaze 좌표 오프셋 (보정용)")]
    [SerializeField] private Vector2 gazeOffset = Vector2.zero;

    public List<Vector3> GetGazePoints()
    {
        return new List<Vector3>(gazePoints);
    }

    public List<float> GetGazeTimestamps()
    {
        return new List<float>(gazeTimestamps);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
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
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out GazePoint gp))
        {
            // 카메라 설정값 가져오기
            float orthoSize = Camera.main.orthographicSize;
            float aspect = Camera.main.aspect;

            // 월드 오차 기준 보정 → 정규화 좌표계 보정으로 환산
            float gx = gp.X + (gazeOffset.x / (orthoSize * aspect));
            float gy = gp.Y + (gazeOffset.y / orthoSize);

            // 월드 좌표로 변환
            float worldX = gx * orthoSize * aspect;
            float worldY = gy * orthoSize;
            Vector3 worldPos = new Vector3(worldX, worldY, 0f);


            gazePoints.Add(worldPos);
            gazeTimestamps.Add(Time.time);

            if (gazePoints.Count > maxPoints)
            {
                gazePoints.RemoveAt(0);
                gazeTimestamps.RemoveAt(0);
            }

            lineRenderer.positionCount = gazePoints.Count;
            lineRenderer.SetPositions(gazePoints.ToArray());

            // 디버깅 간격 로그 (옵션)
            if (gazeTimestamps.Count >= 2)
            {
                float interval = gazeTimestamps[^1] - gazeTimestamps[^2];
                // Debug.Log($"⏱ 시선 포인트 간격: {interval:F4}초");
            }
        }
    }

    void StartTracking()
    {
        isTracking = true;
        gazePoints.Clear();
        gazeTimestamps.Clear();
        lineRenderer.positionCount = 0;
        Debug.Log("🔺 시선 추적 시작");
    }

    void EndTracking()
    {
        isTracking = false;
        Debug.Log("🔻 시선 추적 정지 및 분석 준비 완료");

        if (laneMatcher == null)
        {
            laneMatcher = LaneMatcher.Instance;
        }

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
