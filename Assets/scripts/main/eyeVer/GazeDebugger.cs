using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class GazeDebugger : MonoBehaviour
{
    private Vector3? lastGazePoint = null;
    private float pointLifetime = 1f;

    void Start()
    {
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");

        // [1] Unity 창 추적 설정
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        bool trackResult = TobiiGameIntegrationApi.TrackWindow(hwnd);
        Debug.Log("📺 TrackWindow 호출 결과: " + trackResult);

        // [2] 스트림 유지 설정
        TobiiGameIntegrationApi.UnsetAutoUnsubscribe(StreamType.Gaze);

        // [3] 초기화 확인
        bool ok = TobiiGameIntegrationApi.IsApiInitialized();
        Debug.Log("✅ API 초기화 결과: " + ok);

        var info = TobiiGameIntegrationApi.GetTrackerInfo();
        if (info != null)
        {
            Debug.Log("✅ Tracker 연결됨: " + info.ModelName);
        }
        else
        {
            Debug.LogWarning("❌ Tracker 정보 없음");
        }
    }

    void Update()
    {
        // [필수] 매 프레임 API 갱신
        TobiiGameIntegrationApi.Update();

        // 사용자 인식 안 되는 경우
        if (!TobiiGameIntegrationApi.IsPresent())
        {
            Debug.LogWarning("🚫 사용자 인식 안 됨 (IsPresent = false)");
            return;
        }

        // 시선 좌표 가져오기
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out GazePoint gp))
        {
            float gx = gp.X; // 정규화된 0~1 좌표
            float gy = gp.Y;

            // 정규화 → 월드좌표 (카메라 기준)
            float orthoSize = Camera.main.orthographicSize;
            float aspect = Camera.main.aspect;
            float worldX = (gx - 0.5f) * orthoSize * 2f * aspect;
            float worldY = (gy - 0.5f) * orthoSize * 2f;

            Vector3 worldPos = new Vector3(worldX, worldY, 0f);

            Debug.Log($"👁️ Gaze Norm({gx:F3}, {gy:F3}) → World {worldPos}");

            lastGazePoint = worldPos;
            StartCoroutine(ClearAfterDelay(pointLifetime));
        }
        else
        {
            Debug.LogWarning("❗ TryGetLatestGazePoint 실패: 시선 좌표 없음");
        }

        // 디버그 레이 표시
        if (lastGazePoint.HasValue)
        {
            Vector3 p = lastGazePoint.Value;
            Debug.DrawRay(p, Vector3.up * 0.1f, Color.yellow);
            Debug.DrawRay(p, Vector3.right * 0.1f, Color.yellow);
        }
    }

    IEnumerator ClearAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        lastGazePoint = null;
    }
}
