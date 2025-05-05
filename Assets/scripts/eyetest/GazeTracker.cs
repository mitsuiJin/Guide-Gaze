using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class GazeTracker : MonoBehaviour
{
    void Start()
    {
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);

        Debug.Log($"🔌 연결됨: {TobiiGameIntegrationApi.IsTrackerConnected()}");
        Debug.Log($"🟢 활성화됨: {TobiiGameIntegrationApi.IsTrackerEnabled()}");
    }

    void Update()
    {
        TobiiGameIntegrationApi.Update();

        GazePoint gazePoint;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gazePoint))
        {
            Debug.Log($"🎯 Gaze 좌표: ({gazePoint.X}, {gazePoint.Y})");
        }
    }
}
