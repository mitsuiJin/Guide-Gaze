//using UnityEngine;

//public class CurveBetweenObjects : MonoBehaviour
//{
//    public LineRenderer lineRenderer;  // LineRenderer
//    public Transform startPoint;  // 시작점 (첫 번째 오브젝트)
//    public Transform endPoint;    // 끝점 (두 번째 오브젝트)
//    public Transform controlPoint;  // 제어점 (곡선의 형태를 결정)

//    public int numberOfPoints = 50;  // 그릴 점의 개수

//    void Start()
//    {
//        // LineRenderer의 점 개수를 설정
//        lineRenderer.positionCount = numberOfPoints;

//        // 곡선 그리기
//        DrawBezierCurve();
//    }

//    void DrawBezierCurve()
//    {
//        // 0부터 1까지 t 값으로 점을 계산 (t는 곡선의 진행도)
//        for (int i = 0; i < numberOfPoints; i++)
//        {
//            float t = i / (float)(numberOfPoints - 1);  // 0 ~ 1 사이의 값
//            Vector3 pointOnCurve = CalculateQuadraticBezierPoint(t, startPoint.position, controlPoint.position, endPoint.position);
//            lineRenderer.SetPosition(i, pointOnCurve);
//        }
//    }

//    // 2차 Bezier curve 계산
//    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
//    {
//        // p0, p1, p2: 시작점, 제어점, 끝점
//        float u = 1 - t;
//        float tt = t * t;
//        float uu = u * u;

//        // 2차 Bezier 공식
//        Vector3 point = uu * p0; // (1 - t)^2 * p0
//        point += 2 * u * t * p1; // 2 * (1 - t) * t * p1
//        point += tt * p2; // t^2 * p2

//        return point;
//    }
//}


using UnityEngine;

public class CurveBetweenObjects : MonoBehaviour
{
    public LineRenderer lineRenderer;    // LineRenderer
    public Transform startPoint;     // 시작점 (첫 번째 오브젝트)
    public Transform endPoint;       // 끝점 (네 번째 오브젝트)
    public Transform controlPoint1;  // 첫 번째 제어점
    public Transform controlPoint2;  // 두 번째 제어점

    public int numberOfPoints = 50;    // 그릴 점의 개수

    void Start()
    {
        // LineRenderer의 점 개수를 설정
        lineRenderer.positionCount = numberOfPoints;

        // 3차 베지어 곡선 그리기
        DrawCubicBezierCurve();
    }

    void DrawCubicBezierCurve()
    {
        // 0부터 1까지 t 값으로 점을 계산 (t는 곡선의 진행도)
        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = i / (float)(numberOfPoints - 1);    // 0 ~ 1 사이의 값
            Vector3 pointOnCurve = CalculateCubicBezierPoint(t, startPoint.position, controlPoint1.position, controlPoint2.position, endPoint.position);
            lineRenderer.SetPosition(i, pointOnCurve);
        }
    }

    // 3차 Bezier curve 계산
    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // p0: 시작점, p1: 첫 번째 제어점, p2: 두 번째 제어점, p3: 끝점
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float ttt = tt * t;
        float uuu = uu * u;

        // 3차 Bezier 공식
        Vector3 point = uuu * p0;           // (1 - t)^3 * p0
        point += 3 * uu * t * p1;       // 3 * (1 - t)^2 * t * p1
        point += 3 * u * tt * p2;       // 3 * (1 - t) * t^2 * p2
        point += ttt * p3;               // t^3 * p3

        return point;
    }
}