// MultiLineRendererGenerator.cs
using UnityEngine;
using System.Collections.Generic;

public class MultiLineRendererGenerator : MonoBehaviour
{
    public Transform startPoint;                     // 시작 지점
    public List<Transform> targetPoints;             // 여러 끝 지점
    public Material lineMaterialTemplate;            // 기본 머티리얼 템플릿 (Inspector에서 할당)
    public int curveResolution = 20;                 // 곡선 정밀도
    public Vector2 baseCurveOffset = new Vector2(0, 3f); // 2D 곡선 기본 높이 오프셋

    void Start()
    {
        foreach (Transform target in targetPoints)
        {
            CreateCurveLine(startPoint.position, target);
        }
    }

    void CreateCurveLine(Vector3 start3D, Transform target)
    {
        // 2D 평면에서 작업하므로 Z축 무시
        Vector2 start = new Vector2(start3D.x, start3D.y);
        Vector2 end = new Vector2(target.position.x, target.position.y);

        GameObject lineObj = new GameObject("LineTo_" + target.name);
        lineObj.transform.parent = this.transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.1f;

        // 🎯 머티리얼 복제 후 색상 적용 (각 라인마다 다르게)
        Material uniqueMaterial = new Material(lineMaterialTemplate);
        Color randomColor = RandomColor();
        uniqueMaterial.color = randomColor;
        lr.material = uniqueMaterial;

        lr.startColor = lr.endColor = randomColor;

        // 각 선마다 고유한 두 개의 곡선 제어점 생성
        Vector2 control1 = Vector2.Lerp(start, end, 0.33f) + Random.insideUnitCircle * baseCurveOffset.magnitude;
        Vector2 control2 = Vector2.Lerp(start, end, 0.66f) + Random.insideUnitCircle * baseCurveOffset.magnitude;

        // 곡선 포인트 생성 (3차 베지어)
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector2 point2D = GetCubicBezier(start, control1, control2, end, t);
            lr.SetPosition(i, new Vector3(point2D.x, point2D.y, 0f)); // Z는 항상 0
        }
    }

    Vector2 GetCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        return Mathf.Pow(1 - t, 3) * p0 +
               3 * Mathf.Pow(1 - t, 2) * t * p1 +
               3 * (1 - t) * Mathf.Pow(t, 2) * p2 +
               Mathf.Pow(t, 3) * p3;
    }

    Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }
}