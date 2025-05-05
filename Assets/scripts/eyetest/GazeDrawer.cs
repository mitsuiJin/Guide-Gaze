using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(LineRenderer))]
public class GazeDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<Vector3> gazePoints = new List<Vector3>();
    private const int maxPoints = 200;

    void Start()
    {
        // Tobii 초기 설정
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);

        // LineRenderer 세팅
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;

        Debug.Log("🎯 GazeDrawer 초기화 완료");
    }

    void Update()
    {
        TobiiGameIntegrationApi.Update();

        GazePoint gp;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gp))
        {
            // gaze 좌표가 정상 범위(0~1)일 때만 처리
            if (gp.X >= 0 && gp.X <= 1 && gp.Y >= 0 && gp.Y <= 1)
            {
                // 화면 픽셀 좌표로 변환
                Vector3 screenPos = new Vector3(gp.X * Screen.width, gp.Y * Screen.height, 0f);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));

                gazePoints.Add(worldPos);
                if (gazePoints.Count > maxPoints)
                {
                    gazePoints.RemoveAt(0);
                }

                lineRenderer.positionCount = gazePoints.Count;
                lineRenderer.SetPositions(gazePoints.ToArray());
            }
        }
    }
}
