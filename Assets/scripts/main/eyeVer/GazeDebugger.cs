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
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    void Update()
    {
        // 매 프레임 Tobii 업데이트 호출 필요
        TobiiGameIntegrationApi.Update();

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out GazePoint gp))
            {
                // 정규화된 시선 좌표 (0~1 범위)
                float gx = gp.X;
                float gy = gp.Y;

                // 카메라 기준 범위 계산
                // gp.X, gp.Y는 -1 ~ 1 기준이라고 가정
                float orthoSize = Camera.main.orthographicSize;
                float aspect = Camera.main.aspect;

                float worldX = gp.X * orthoSize * aspect;
                float worldY = gp.Y * orthoSize;
               Vector3 worldPos = new Vector3(worldX, worldY, 0f);

                Debug.Log($"👁️ Gaze Norm({gx:F3}, {gy:F3}) → World {worldPos}");

                lastGazePoint = worldPos;
                StartCoroutine(ClearAfterDelay(pointLifetime));
            }
            else
            {
                Debug.LogWarning("❗ Tobii 시선 좌표를 가져올 수 없습니다.");
            }
        }

        // DrawRay는 Scene 뷰에서만 보임
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
