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

    private const float offsetX = 0.5f;
    private const float offsetY = 0.15f;

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

        Debug.Log("🎯 GazeDrawer 초기화 완료");
    }

    void Update()
    {
        TobiiGameIntegrationApi.Update();

        GazePoint gp;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gp))
        {
            // 🔐 오프셋 적용 후 Clamp (0~1 범위 유지)
            float correctedX = Mathf.Clamp01(gp.X + offsetX);
            float correctedY = Mathf.Clamp01(gp.Y + offsetY);

            Vector3 screenPos = new Vector3(correctedX * Screen.width, correctedY * Screen.height, 10f);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            gazePoints.Add(worldPos);
            if (gazePoints.Count > maxPoints)
                gazePoints.RemoveAt(0);

            lineRenderer.positionCount = gazePoints.Count;
            lineRenderer.SetPositions(gazePoints.ToArray());
        }
    }
}
