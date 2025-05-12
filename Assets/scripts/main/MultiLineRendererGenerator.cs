using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 여러 타겟 포인트로 n차 베지어 곡선을 그리고,
/// 파동(원) 교차점마다 제어점 추가, 각 제어점마다 방향과 세기, 각도를 인스펙터에서 조절,
/// 곡선끼리 겹치지 않게 라인별 offset 반영
/// </summary>
public class MultiLineRendererGenerator : MonoBehaviour
{
    public Transform startPoint;                 // 시작점 (예: Ctrl)
    public List<Transform> targetPoints;         // 여러 끝점 (예: A, S, C, V 등)
    public Material lineMaterialTemplate;        // 라인 머티리얼 템플릿
    public int curveResolution = 40;             // 곡선 정밀도

    [Header("Wave Circles (원형 파동)")]
    public List<WaveCircle> waveCircles;         // 원형 파동 정보들

    void Start()
    {
        for (int i = 0; i < targetPoints.Count; i++)
        {
            CreateWaveCurveLine(startPoint.position, targetPoints[i], i, targetPoints.Count);
        }
    }

    /// <summary>
    /// 곡선을 생성: 시작~끝, 파동 교차점마다 제어점 추가, n차 베지어로 그리기
    /// </summary>
    void CreateWaveCurveLine(Vector3 start3D, Transform target, int lineIndex, int totalLines)
    {
        Vector2 start = new Vector2(start3D.x, start3D.y);
        Vector2 end = new Vector2(target.position.x, target.position.y);

        // 라인 오브젝트 생성
        GameObject lineObj = new GameObject("LineTo_" + target.name);
        lineObj.transform.parent = this.transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.1f;

        // ColorLaneInfo 컴포넌트 추가
        ColorLaneInfo cli = lineObj.AddComponent<ColorLaneInfo>();
        cli.positions = new List<Vector3>();

        // 머티리얼 복제 및 색상 설정
        Material uniqueMaterial = new Material(lineMaterialTemplate);
        Color randomColor = RandomColor();
        uniqueMaterial.color = randomColor;
        lr.material = uniqueMaterial;
        lr.startColor = lr.endColor = randomColor;

        // 1. 원형 파동과 교차점 찾기 (각각 어느 원에서 교차했는지 waveIndex 포함)
        var inflections = FindWaveIntersectionsWithWaveIndex(start, end);

        // 2. 제어점(시작, 교차점들, 끝점) 리스트 생성
        List<Vector2> controlPoints = new List<Vector2>();
        controlPoints.Add(start);

        // 교차점 순서대로 정렬 (시작점에서 가까운 순)
        inflections.Sort((a, b) =>
            Vector2.Distance(start, a.point).CompareTo(Vector2.Distance(start, b.point)));

        // 3. 각 변곡점마다 방향과 strength, angle 조절
        foreach (var inflect in inflections)
        {
            // 원의 중심→교차점 방향 벡터 (사분면 방향)
            Vector2 fromCenter = (inflect.point - waveCircles[inflect.waveIndex].center).normalized;

            float strength = waveCircles[inflect.waveIndex].curveStrength;
            float angle = waveCircles[inflect.waveIndex].angle;

            float lineOffset = GetLineOffsetMultiplier(lineIndex, totalLines);

            // angle을 적용하려면 (fromCenter 기준으로 회전)
            Vector2 rotated = Quaternion.Euler(0, 0, angle) * fromCenter;

            // 최종 제어점 위치 (사분면 방향 + angle + 라인별 offset)
            Vector2 control = inflect.point + rotated * strength * (1f + 0.5f * lineOffset);
            controlPoints.Add(control);
        }


        controlPoints.Add(end);

        // 4. n차 베지어 곡선 포인트 계산 (De Casteljau 알고리즘)
        List<Vector2> curvePoints = new List<Vector2>();
        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector2 pt = GetBezierPoint(t, controlPoints);
            curvePoints.Add(pt);
        }

        // 5. 라인렌더러에 포인트 적용
        lr.positionCount = curvePoints.Count;
        for (int i = 0; i < curvePoints.Count; i++)
        {
            lr.SetPosition(i, new Vector3(curvePoints[i].x, curvePoints[i].y, 0f));
            cli.positions.Add(new Vector3(curvePoints[i].x, curvePoints[i].y, 0f));
        }
    }

    /// <summary>
    /// 라인별로 고유한 offset multiplier를 적용하여 곡선이 서로 벌어지게 만듦
    /// </summary>
    float GetLineOffsetMultiplier(int lineIndex, int totalLines)
    {
        // 예: -1.5, -0.5, +0.5, +1.5 ... (중앙 기준 대칭 분산)
        float center = (totalLines - 1) / 2f;
        return (lineIndex - center);
    }

    /// <summary>
    /// 원형 파동(웨이브)들과 라인(시작~끝) 교차점 찾기 (waveIndex 포함)
    /// </summary>
    List<(Vector2 point, float strength, int waveIndex)> FindWaveIntersectionsWithWaveIndex(Vector2 start, Vector2 end)
    {
        List<(Vector2, float, int)> result = new List<(Vector2, float, int)>();
        Vector2 dir = (end - start).normalized;
        float lineLength = (end - start).magnitude;

        for (int waveIdx = 0; waveIdx < waveCircles.Count; waveIdx++)
        {
            var wave = waveCircles[waveIdx];
            Vector2 oc = start - wave.center;

            float a = Vector2.Dot(dir, dir);
            float b = 2 * Vector2.Dot(oc, dir);
            float c = Vector2.Dot(oc, oc) - wave.radius * wave.radius;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) continue; // 교차 없음

            float sqrtD = Mathf.Sqrt(discriminant);
            float t1 = (-b - sqrtD) / (2 * a);
            float t2 = (-b + sqrtD) / (2 * a);

            foreach (float t in new[] { t1, t2 })
            {
                if (t >= 0 && t <= lineLength)
                {
                    Vector2 intersection = start + dir * t;
                    result.Add((intersection, wave.curveStrength, waveIdx));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// n차 베지어 곡선의 한 점을 계산 (De Casteljau 알고리즘)
    /// </summary>
    Vector2 GetBezierPoint(float t, List<Vector2> points)
    {
        // De Casteljau 알고리즘
        List<Vector2> temp = new List<Vector2>(points);
        int n = temp.Count;
        for (int k = 1; k < n; k++)
        {
            for (int i = 0; i < n - k; i++)
            {
                temp[i] = (1 - t) * temp[i] + t * temp[i + 1];
            }
        }
        return temp[0];
    }

    /// <summary>
    /// 랜덤 컬러 생성
    /// </summary>
    Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }

    /// <summary>
    /// (선택 사항) 에디터에서 원형 파동 시각화
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (waveCircles == null) return;
        Gizmos.color = Color.cyan;
        foreach (var wave in waveCircles)
        {
            Gizmos.DrawWireSphere(new Vector3(wave.center.x, wave.center.y, 0), wave.radius);
        }
    }
}
