using UnityEngine;

public class CurveBetweenObjects : MonoBehaviour
{
    public LineRenderer lineRenderer;  // LineRenderer
    public Transform startPoint;  // 시작점 (첫 번째 오브젝트)
    public Transform endPoint;    // 끝점 (두 번째 오브젝트)
    public Transform controlPoint;  // 제어점 (곡선의 형태를 결정)

    public int numberOfPoints = 50;  // 그릴 점의 개수

    void Start()
    {
        // LineRenderer의 점 개수를 설정
        lineRenderer.positionCount = numberOfPoints;

        // 곡선 그리기
        DrawBezierCurve();
    }

    void DrawBezierCurve()
    {
        // 0부터 1까지 t 값으로 점을 계산 (t는 곡선의 진행도)
        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = i / (float)(numberOfPoints - 1);  // 0 ~ 1 사이의 값
            Vector3 pointOnCurve = CalculateQuadraticBezierPoint(t, startPoint.position, controlPoint.position, endPoint.position);
            lineRenderer.SetPosition(i, pointOnCurve);
        }
    }

    // 2차 Bezier curve 계산
    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // p0, p1, p2: 시작점, 제어점, 끝점
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        // 2차 Bezier 공식
        Vector3 point = uu * p0; // (1 - t)^2 * p0
        point += 2 * u * t * p1; // 2 * (1 - t) * t * p1
        point += tt * p2; // t^2 * p2

        return point;
    }
}
