using UnityEngine;
using System.Runtime.InteropServices;
using Tobii.GameIntegration.Net;

public class GazeDebugger : MonoBehaviour
{
    public GameObject debugDotPrefab;  // Inspector에서 지정한 노란색 점 프리팹
    private GameObject debugDot;

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();
#endif

    void Start()
    {
#if UNITY_STANDALONE_WIN
        var hwnd = GetActiveWindow();
        TobiiGameIntegrationApi.SetApplicationName("MyUnityGazeApp");  // 고정된 앱 이름 지정
        TobiiGameIntegrationApi.TrackWindow(hwnd);  // 현재 창 핸들 등록
        Debug.Log($"[Tobii] 창 핸들 등록 완료: {hwnd}");
#endif

        Debug.Log($"[Tobii] API 초기화됨? {TobiiGameIntegrationApi.IsApiInitialized()}");
        Debug.Log($"[Tobii] 트래커 연결됨? {TobiiGameIntegrationApi.IsTrackerConnected()}");

        var info = TobiiGameIntegrationApi.GetTrackerInfo();
        if (info != null)
            Debug.Log($"[Tobii] 모델명: {info.ModelName}, 펌웨어: {info.FirmwareVersion}");
        else
            Debug.LogWarning("[Tobii] 트래커 정보를 불러올 수 없습니다.");
    }

    void Update()
    {
        GazePoint gazePoint;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gazePoint))
        {
            float gazeX = gazePoint.X;
            float gazeY = gazePoint.Y;

            // 디버그 로그 출력
            Debug.Log($"👁️ Gaze Norm({gazeX:F3}, {gazeY:F3})");

            // -1~1 범위 → 스크린 픽셀 좌표로 변환
            Vector3 screenPos = new Vector3(
                (gazeX + 1f) * 0.5f * Screen.width,
                (gazeY + 1f) * 0.5f * Screen.height,
                10f // 카메라 앞 거리
            );

            // 월드 좌표로 변환
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            Debug.Log($"→ World ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");

            // 디버그 점 찍기
            if (debugDotPrefab != null)
            {
                if (debugDot == null)
                    debugDot = Instantiate(debugDotPrefab, worldPos, Quaternion.identity);
                else
                    debugDot.transform.position = worldPos;
            }

            // 선 그리기 (시각화용)
            Debug.DrawLine(Camera.main.transform.position, worldPos, Color.yellow);
        }
        else if (Time.frameCount % 20 == 0) // 너무 자주 찍지 않도록
        {
            Debug.LogWarning("❗ Tobii 시선 좌표를 가져올 수 없습니다.");
        }
    }
}
