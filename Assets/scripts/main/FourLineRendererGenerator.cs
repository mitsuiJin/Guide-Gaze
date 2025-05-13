// FourLineRenderGenerator.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 커맨드 키(Ctrl)에서 Z/X/C/V 키로 향하는 곡선을 생성하는 전용 스크립트 (시각화 객체와의 충돌 방지를 위한 설계)
/// </summary>
public class FourLineRendererGenerator : MonoBehaviour
{
    public Transform ctrlKeyCenter;                  // 커맨드 키 (예: Ctrl) 중심 위치
    public List<Transform> targetKeys;               // 타겟 키 리스트: Z, X, C, V (순서 고정)
    public Material lineMaterialTemplate;            // 라인 머티리얼 템플릿
    public int curveResolution = 20;                 // 곡선 포인트 수
    public float curveHeight = 1.0f;                 // 곡선 높이 계수

    void Start()
    {
        for (int i = 0; i < targetKeys.Count; i++)
        {
            bool isUpward = (i == 0 || i == 2); // Z, C는 위로 볼록
            Vector2 ctrlAnchor = GetCtrlAnchor(i);
            CreateParabolicCurve(ctrlAnchor, targetKeys[i].position, isUpward, i);
        }
    }

    /// <summary>
    /// Ctrl 키에서 출발하는 위치를 z/x/c/v에 따라 상/우/하/좌로 분리하여 지정
    /// </summary>
    Vector2 GetCtrlAnchor(int index)
    {
        Vector2 basePos = new Vector2(ctrlKeyCenter.position.x, ctrlKeyCenter.position.y);
        float offset = 0.3f;

        return index switch
        {
            0 => basePos + Vector2.up * offset,     // Z: 위쪽
            1 => basePos + Vector2.right * offset,  // X: 오른쪽
            2 => basePos + Vector2.down * offset,   // C: 아래쪽
            3 => basePos + Vector2.left * offset,   // V: 왼쪽
            _ => basePos
        };
    }

    /// <summary>
    /// 2차 곡선을 따라 Color Lane을 생성 (볼록 방향에 따라 제어점 위치 조정)
    /// </summary>
    void CreateParabolicCurve(Vector2 start, Vector3 target3D, bool isUpward, int index)
    {
        Vector2 end = new Vector2(target3D.x, target3D.y);
        Vector2 mid = (start + end) * 0.5f;
        Vector2 offsetDir = new Vector2(-(end.y - start.y), end.x - start.x).normalized; // 수직 벡터

        Vector2 control = mid + offsetDir * (isUpward ? curveHeight : -curveHeight);

        GameObject lineObj = new GameObject("ParabolaTo_" + targetKeys[index].name);
        lineObj.transform.parent = this.transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.1f;

        Material mat = new Material(lineMaterialTemplate);
        Color color = RandomColor();
        mat.color = color;
        lr.material = mat;
        lr.startColor = lr.endColor = color;

        ColorLaneInfo cli = lineObj.AddComponent<ColorLaneInfo>();
        cli.positions = new List<Vector3>();

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector2 point = QuadraticBezier(start, control, end, t);
            Vector3 point3D = new Vector3(point.x, point.y, 0f);
            lr.SetPosition(i, point3D);
            cli.positions.Add(point3D);
        }
    }

    /// <summary>
    /// 2차 베지어 곡선 공식
    /// </summary>
    Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }

    Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }
}