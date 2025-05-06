using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class GazeTracker : MonoBehaviour
{
    void Start()
    {
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle); // 얘가 포인트였음. 얘 넣고 나니까  gaze point 얻을 수 있었으

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
