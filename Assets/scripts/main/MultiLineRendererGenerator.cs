using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 여러 타겟 포인트를 향해 곡선 라인을 생성하는 스크립트
/// </summary>
public class MultiLineRendererGenerator : MonoBehaviour
{
    public Transform startPoint;                     // 시작 지점 (기준점)
    public List<Transform> targetPoints;             // 여러 끝 지점
    public Material lineMaterialTemplate;            // 라인 머티리얼 템플릿 (색상은 스크립트에서 랜덤 적용)
    public int curveResolution = 20;                 // 곡선 정밀도 (포인트 개수)
    public Vector2 baseCurveOffset = new Vector2(0, 3f); // 곡선 제어점 offset

    void Start()
    {
        foreach (Transform target in targetPoints)
        {
            CreateCurveLine(startPoint.position, target);
        }
    }

    /// <summary>
    /// 단일 타겟에 대한 곡선 라인을 생성
    /// </summary>
    void CreateCurveLine(Vector3 start3D, Transform target)
    {
        // 2D 평면 기반 계산
        Vector2 start = new Vector2(start3D.x, start3D.y);
        Vector2 end = new Vector2(target.position.x, target.position.y);

        // 라인 오브젝트 생성
        GameObject lineObj = new GameObject("LineTo_" + target.name);
        lineObj.transform.parent = this.transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.1f;

        // 💡 ColorLaneInfo 컴포넌트 추가
        ColorLaneInfo cli = lineObj.AddComponent<ColorLaneInfo>();
        cli.positions = new List<Vector3>();

        // 머티리얼 복제 및 색상 설정
        Material uniqueMaterial = new Material(lineMaterialTemplate);
        Color randomColor = RandomColor();
        uniqueMaterial.color = randomColor;
        lr.material = uniqueMaterial;
        lr.startColor = lr.endColor = randomColor;

        // 제어점 계산 (3차 베지어 곡선용)
        Vector2 control1 = Vector2.Lerp(start, end, 0.33f) + Random.insideUnitCircle * baseCurveOffset.magnitude;
        Vector2 control2 = Vector2.Lerp(start, end, 0.66f) + Random.insideUnitCircle * baseCurveOffset.magnitude;

        // 곡선 포인트 계산 및 설정
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector2 point2D = GetCubicBezier(start, control1, control2, end, t);
            Vector3 point3D = new Vector3(point2D.x, point2D.y, 0f);
            lr.SetPosition(i, point3D);

            // 💾 ColorLaneInfo에도 저장
            cli.positions.Add(point3D);
        }
    }

    /// <summary>
    /// 3차 베지어 곡선 공식
    /// </summary>
    Vector2 GetCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        return Mathf.Pow(1 - t, 3) * p0 +
               3 * Mathf.Pow(1 - t, 2) * t * p1 +
               3 * (1 - t) * Mathf.Pow(t, 2) * p2 +
               Mathf.Pow(t, 3) * p3;
    }

    /// <summary>
    /// 랜덤 컬러 생성
    /// </summary>
    Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }
}
