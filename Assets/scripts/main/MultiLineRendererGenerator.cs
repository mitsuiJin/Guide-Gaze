using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MultiLineRendererGenerator : MonoBehaviour
{
    public Transform ctrlKeyCenter;
    public List<Transform> targetKeys;
    public Material lineMaterialTemplate;
    public int curveResolution = 20;
    public float startOffset = 0.3f;
    public float heightFactor = 0.3f;

    [Header("Height Factor by Direction")]
    public float topHeight = 0.2f;
    public float bottomHeight = 0.4f;
    public float leftHeight = 0.5f;
    public float rightHeight = 0.3f;

    private enum CtrlSide { Left, Top, Right, Bottom }

    private class LaneData
    {
        public int index;
        public Transform target;
        public CtrlSide side;
        public Vector2 start;
        public Vector2 end;
        public Vector2 control;
        public bool isUpward;
    }

    void Start()
    {
        if (targetKeys.Count != 4)
        {
            Debug.LogError("targetKeys는 4개여야 합니다.");
            return;
        }

        Vector2 ctrl = ctrlKeyCenter.position;

        // 상대 위치 계산
        var keyInfos = targetKeys.Select((t, i) => new
        {
            index = i,
            tf = t,
            local = (Vector2)t.position - ctrl,
            world = (Vector2)t.position
        }).ToList();

        // 위로 볼록 그룹: -x - y 값 기준 정렬 (Top, Left)
        var upwardSorted = keyInfos.OrderByDescending(k => -k.local.x - k.local.y).Take(2).ToList();
        var downwardSorted = keyInfos.Except(upwardSorted).OrderByDescending(k => k.local.x + k.local.y).Take(2).ToList();

        var lanes = new List<LaneData>();

        if (upwardSorted.Count == 2)
        {
            lanes.Add(new LaneData { index = upwardSorted[0].index, target = upwardSorted[0].tf, side = CtrlSide.Top, isUpward = true });
            lanes.Add(new LaneData { index = upwardSorted[1].index, target = upwardSorted[1].tf, side = CtrlSide.Left, isUpward = true });
        }

        if (downwardSorted.Count == 2)
        {
            lanes.Add(new LaneData { index = downwardSorted[0].index, target = downwardSorted[0].tf, side = CtrlSide.Bottom, isUpward = false });
            lanes.Add(new LaneData { index = downwardSorted[1].index, target = downwardSorted[1].tf, side = CtrlSide.Right, isUpward = false });
        }

        foreach (var lane in lanes)
        {
            lane.start = GetStartPoint(ctrl, lane.side);
            lane.end = lane.target.position;
            lane.control = CalculateControlPoint(lane.start, lane.end, lane.isUpward, AdjustedHeightFactor(lane.side));

            DrawQuadraticBezier(lane.index, lane.start, lane.control, lane.end, lane.target.name);
        }
    }

    float AdjustedHeightFactor(CtrlSide side)
    {
        return side switch
        {
            CtrlSide.Top => topHeight,
            CtrlSide.Bottom => bottomHeight,
            CtrlSide.Left => leftHeight,
            CtrlSide.Right => rightHeight,
            _ => 0.3f
        };
    }

    Vector2 GetStartPoint(Vector2 center, CtrlSide side)
    {
        return side switch
        {
            CtrlSide.Left => center + Vector2.left * startOffset,
            CtrlSide.Top => center + Vector2.up * startOffset,
            CtrlSide.Right => center + Vector2.right * startOffset,
            CtrlSide.Bottom => center + Vector2.down * startOffset,
            _ => center
        };
    }

    Vector2 CalculateControlPoint(Vector2 start, Vector2 end, bool isUpward, float height)
    {
        Vector2 mid = Vector2.Lerp(start, end, isUpward ? 0.4f : 0.35f);
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);
        Vector2 convex = isUpward ? Vector2.up : Vector2.down;
        float align = Mathf.Sign(Vector2.Dot(normal, convex));
        return mid + normal * align * Vector2.Distance(start, end) * height;
    }

    void DrawQuadraticBezier(int index, Vector2 start, Vector2 control, Vector2 end, string label)
    {
        GameObject lineObj = new GameObject("Curve_" + label);
        lineObj.transform.parent = this.transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.1f;

        Material mat = new Material(lineMaterialTemplate);
        Color color = Color.HSVToRGB(index / 4f, 1f, 1f);
        mat.color = color;
        lr.material = mat;
        lr.startColor = lr.endColor = color;

        ColorLaneInfo cli = lineObj.AddComponent<ColorLaneInfo>();
        cli.positions = new List<Vector3>();

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector2 pt = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
            Vector3 pt3 = new Vector3(pt.x, pt.y, 0f);
            lr.SetPosition(i, pt3);
            cli.positions.Add(pt3);
        }
    }
}
