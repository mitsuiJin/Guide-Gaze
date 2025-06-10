using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Tobii Game Integration API를 통해 시선을 추적하고, 라인을 그리고, LaneMatcher에 비교 요청까지 수행.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GazeDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<Vector3> gazePoints = new List<Vector3>();
    private const int maxPoints = 200;

    public LaneMatcher laneMatcher;  // 🎯 LaneMatcher 연결

    private const float offsetX = 0.5f; // 💡 시선 위치 조정용 오프셋
    private const float offsetY = 0.15f;

    void Start()
    {
        // Tobii API 초기화
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);

        // LineRenderer 초기화
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.widthMultiplier = 0.05f;

        Debug.Log("🎯 GazeDrawer 초기화 완료");
    }

    void Update()
    {
        TobiiGameIntegrationApi.Update();

        GazePoint gp;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gp))
        {
            float correctedX = Mathf.Clamp01(gp.X + offsetX);
            float correctedY = Mathf.Clamp01(gp.Y + offsetY);

            Vector3 screenPos = new Vector3(correctedX * Screen.width, correctedY * Screen.height, 10f);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;

            gazePoints.Add(worldPos);
            if (gazePoints.Count > maxPoints)
                gazePoints.RemoveAt(0);

            lineRenderer.positionCount = gazePoints.Count;
            lineRenderer.SetPositions(gazePoints.ToArray());
        }

        // 🧪 Space 키 입력으로 비교 실행
        if (Input.GetKeyDown(KeyCode.Space))
        {
           // laneMatcher?.CompareAndFindClosestLane(GetGazePoints2D());
        }
    }

    public List<Vector2> GetGazePoints2D()
    {
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in gazePoints)
            points2D.Add(new Vector2(p.x, p.y));
        return points2D;
    }
}
