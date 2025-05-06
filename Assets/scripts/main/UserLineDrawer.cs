// userLineDrawer.cs
using UnityEngine;
using System.Collections.Generic;

public class UserLineDrawer : MonoBehaviour
{
    public Material lineMaterial;          // 사용자 라인 머티리얼
    public float minDistance = 0.1f;       // 점 사이 최소 거리
    public float lineWidth = 0.1f;         // 라인 두께

    private List<Vector3> drawnPoints = new List<Vector3>();
    private LineRenderer currentLine;
    private bool isDrawing = false;

    public LaneMatcher laneMatcher;        // 🎯 LaneMatcher 연결
    private bool lanesAutoRegistered = false; // 🎯 Color Lane 자동 등록 여부

    private readonly Color[] fixedColors = new Color[] {
        Color.red,
        new Color(1f, 0.5f, 0f), // 주황
        Color.yellow,
        Color.green,
        Color.blue,
        new Color(0.29f, 0f, 0.51f), // 남색 (보라계)
        new Color(0.58f, 0f, 0.83f)  // 보라
    };

    void Start()
    {
        // 사용자 선 라인 오브젝트 생성
        GameObject lineObj = new GameObject("UserDrawnLine");
        currentLine = lineObj.AddComponent<LineRenderer>();
        currentLine.material = new Material(lineMaterial);
        currentLine.startColor = Color.white;
        currentLine.endColor = Color.white;
        currentLine.widthMultiplier = lineWidth;
        currentLine.numCapVertices = 5;
        currentLine.numCornerVertices = 5;
        currentLine.positionCount = 0;
    }

    void Update()
    {
        // 🎯 Color Lane 자동 등록 (한 번만 실행)
        if (!lanesAutoRegistered && laneMatcher != null && laneMatcher.colorLaneManager != null)
        {
            LineRenderer[] allLines = GameObject.FindObjectsOfType<LineRenderer>();
            int colorIndex = 0;

            foreach (var lr in allLines)
            {
                if (lr.gameObject.name == "UserDrawnLine") continue;

                laneMatcher.colorLaneManager.colorLanes.Add(lr);

                // 고정 색상 및 shortcutName 설정
                if (colorIndex < fixedColors.Length)
                {
                    lr.startColor = fixedColors[colorIndex];
                    lr.endColor = fixedColors[colorIndex];
                    lr.material = new Material(lineMaterial);
                    lr.material.color = fixedColors[colorIndex];

                    // ColorLaneInfo 자동 추가 및 이름 기반 shortcutName 설정
                    var info = lr.gameObject.GetComponent<ColorLaneInfo>();
                    if (info == null)
                    {
                        info = lr.gameObject.AddComponent<ColorLaneInfo>();
                    }

                    // "LineTo_Ctrl+S" 형태에서 "Ctrl+S"만 추출
                    string rawName = lr.gameObject.name;
                    string[] split = rawName.Split('_');
                    string shortcut = split.Length > 1 ? split[1] : rawName;
                    info.shortcutName = shortcut;

                    colorIndex++;
                }
            }

            lanesAutoRegistered = true;
            Debug.Log($"[LaneMatcher] Color Lane 자동 등록 완료 (지연): {laneMatcher.colorLaneManager.colorLanes.Count}개");
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }
        else if (Input.GetMouseButton(0) && isDrawing)
        {
            AddPointIfFarEnough(GetMouseWorldPosition());
        }
        else if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            EndDrawing();
        }
    }

    void StartDrawing()
    {
        drawnPoints.Clear();
        currentLine.positionCount = 0;
        isDrawing = true;
    }

    void AddPointIfFarEnough(Vector3 point)
    {
        point.z = 0f;  // z축 고정
        if (drawnPoints.Count == 0 || Vector3.Distance(drawnPoints[drawnPoints.Count - 1], point) >= minDistance)
        {
            drawnPoints.Add(point);
            currentLine.positionCount = drawnPoints.Count;
            currentLine.SetPosition(drawnPoints.Count - 1, point);
        }
    }

    void EndDrawing()
    {
        isDrawing = false;
        Debug.Log("User line drawn with " + drawnPoints.Count + " points.");

        if (laneMatcher != null)
        {
            laneMatcher.CompareAndFindClosestLane();
        }
        else
        {
            Debug.LogWarning("LaneMatcher가 연결되어 있지 않습니다!");
        }
    }

    public List<Vector2> GetDrawnPoints2D()
    {
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in drawnPoints)
        {
            points2D.Add(new Vector2(p.x, p.y));
        }
        return points2D;
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            hitPoint.z = 0f;
            return hitPoint;
        }
        return Vector3.zero;
    }
}
