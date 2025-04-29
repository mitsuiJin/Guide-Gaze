//using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
//public class BezierCurve : MonoBehaviour
//{
//    public Transform startPoint;
//    public Transform endPoint;
//    public Transform controlPoint;  // 중간의 컨트롤 포인트
//    public int pointCount = 50;

//    private LineRenderer lineRenderer;

//    void Start()
//    {
//        lineRenderer = GetComponent<LineRenderer>();
//        lineRenderer.positionCount = pointCount;
//        lineRenderer.startWidth = 0.05f;
//        lineRenderer.endWidth = 0.05f;

//        DrawBezier();
//    }

//    void DrawBezier()
//    {
//        for (int i = 0; i < pointCount; i++)
//        {
//            float t = i / (float)(pointCount - 1);
//            Vector3 point = CalculateQuadraticBezierPoint(t, startPoint.position, controlPoint.position, endPoint.position);
//            lineRenderer.SetPosition(i, point);
//        }
//    }

//    // 2차 베지어 포인트 계산 함수
//    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
//    {
//        return Mathf.Pow(1 - t, 2) * p0 +
//               2 * (1 - t) * t * p1 +
//               Mathf.Pow(t, 2) * p2;
//    }
//}


using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BezierCurve : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public Transform controlPoint1;  // 첫 번째 컨트롤 포인트
    public Transform controlPoint2;  // 두 번째 컨트롤 포인트
    public int pointCount = 50;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointCount;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        DrawBezier();
    }

    void DrawBezier()
    {
        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            Vector3 point = CalculateCubicBezierPoint(t, startPoint.position, controlPoint1.position, controlPoint2.position, endPoint.position);
            lineRenderer.SetPosition(i, point);
        }
    }

    // 3차 베지어 포인트 계산 함수
    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0; // (1-t)^3 * P0
        p += 3 * uu * t * p1; // 3(1-t)^2 * t * P1
        p += 3 * u * tt * p2; // 3(1-t) * t^2 * P2
        p += ttt * p3; // t^3 * P3

        return p;
    }
}
