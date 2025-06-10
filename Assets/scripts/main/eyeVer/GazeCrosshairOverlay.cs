using UnityEngine;
using Tobii.GameIntegration.Net;
using System.Diagnostics;
using UnityEngine.UI;

public class GazeCrosshairOverlay : MonoBehaviour
{
    private RectTransform horizontalLine;
    private RectTransform verticalLine;
    private Canvas overlayCanvas;

    // 필요한 오프셋. 필요시 0으로 조정 가능
    [SerializeField] private Vector2 gazeOffset = new Vector2(0.5f, 0.15f);

    void Start()
    {
        // ✅ Overlay Canvas 생성
        GameObject canvasObj = new GameObject("GazeOverlayCanvas");
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 📍 십자가 선 생성
        horizontalLine = CreateLine("HorizontalLine", new Vector2(20, 2));
        verticalLine = CreateLine("VerticalLine", new Vector2(2, 20));

        // ⚙️ Tobii 초기화
        TobiiGameIntegrationApi.SetApplicationName("MyUnityApp");
        TobiiGameIntegrationApi.TrackWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    // 🔧 GazeLineDrawer와 별개로 시선 추적을 항상 따라가도록 유지
    void Update()
    {
        TobiiGameIntegrationApi.Update();  // 항상 갱신

        GazePoint gp;
        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out gp))
        {
            float x = Mathf.Clamp01(gp.X + gazeOffset.x);
            float y = Mathf.Clamp01(gp.Y + gazeOffset.y);
            Vector2 screenPos = new Vector2(x * Screen.width, y * Screen.height);

            // 항상 화면상의 좌표로 십자가 위치 갱신
            horizontalLine.anchoredPosition = screenPos;
            verticalLine.anchoredPosition = screenPos;
        }
    }

    // 📦 십자가 선 오브젝트 생성
    private RectTransform CreateLine(string name, Vector2 size)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(overlayCanvas.transform, false);

        RectTransform rt = line.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = line.AddComponent<Image>();
        img.color = Color.red;

        return rt;
    }
}
