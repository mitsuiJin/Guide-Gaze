using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class MousePathDrawer : MonoBehaviour
{
    public float pointSpacing = 0.01f; // 연속된 점 사이 최소 거리
    public bool isDrawing { get; private set; } = false;

    private LineRenderer lineRenderer;
    private List<Vector3> drawnPoints = new List<Vector3>();

    public List<Vector3> DrawnPoints => new List<Vector3>(drawnPoints); // 외부 접근용 복사본 제공

    public Color lineColor = Color.blue;  // 원하는 선 색깔 설정 (예: 빨간색)

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        // 선의 두께를 얇게 설정
        lineRenderer.startWidth = 0.05f;  // 선 시작 두께
        lineRenderer.endWidth = 0.05f;    // 선 끝 두께

        // 선 색깔 설정
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            if (drawnPoints.Count == 0 || Vector3.Distance(drawnPoints.Last(), mousePos) > pointSpacing)
            {
                drawnPoints.Add(mousePos);
                lineRenderer.positionCount = drawnPoints.Count;
                lineRenderer.SetPosition(drawnPoints.Count - 1, mousePos);
            }
        }

        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
        }
    }

    public void StartDrawing()
    {
        drawnPoints.Clear();
        lineRenderer.positionCount = 0;
        isDrawing = true;
    }

    public void ClearDrawing()
    {
        drawnPoints.Clear();
        lineRenderer.positionCount = 0;
        isDrawing = false;
    }
}
