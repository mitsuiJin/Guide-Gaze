using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LaneTracker : MonoBehaviour
{
    public LineRenderer colorLaneRenderer;
    public LineRenderer drawnLaneRenderer;
    public float frechetDistanceThreshold = 0.1f; // 조정 필요

    private List<Vector3> drawnPoints = new List<Vector3>();
    private bool isDrawing = false;
    private bool hasChecked = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;
            drawnPoints.Clear();
            drawnLaneRenderer.positionCount = 0;
            hasChecked = false;
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            if (drawnPoints.Count == 0 || Vector3.Distance(drawnPoints.Last(), mousePos) > 0.01f)
            {
                drawnPoints.Add(mousePos);
                drawnLaneRenderer.positionCount = drawnPoints.Count;
                drawnLaneRenderer.SetPositions(drawnPoints.ToArray());
            }
        }

        if (isDrawing && Input.GetMouseButtonUp(0) && !hasChecked)
        {
            List<Vector3> colorLanePoints = GetColorLanePoints();

            if (colorLanePoints.Count > 0 && drawnPoints.Count > 0)
            {
                float frechetDistance = CalculateFrechetDistance(colorLanePoints, drawnPoints);
                Debug.Log("Frechet Distance: " + frechetDistance);

                if (frechetDistance < frechetDistanceThreshold)
                {
                    Debug.Log("Success: Drawn lane matches the color lane!");
                }
                else
                {
                    Debug.Log("Failed: The drawn lane does not match the color lane.");
                }
            }
            else
            {
                Debug.LogWarning("Color lane 또는 그려진 선에 점이 없습니다.");
            }

            isDrawing = false;
            hasChecked = true;
        }
    }

    List<Vector3> GetColorLanePoints()
    {
        if (colorLaneRenderer == null) return new List<Vector3>();
        Vector3[] positions = new Vector3[colorLaneRenderer.positionCount];
        colorLaneRenderer.GetPositions(positions);
        return new List<Vector3>(positions);
    }

    float CalculateFrechetDistance(List<Vector3> path1, List<Vector3> path2)
    {
        int n = path1.Count;
        int m = path2.Count;

        if (n == 0 || m == 0)
        {
            return float.MaxValue;
        }

        // 거리 계산 결과를 저장할 DP 테이블
        float[,] dp = new float[n, m];

        // 초기값 계산
        dp[0, 0] = Vector3.Distance(path1[0], path2[0]);

        for (int i = 1; i < n; i++)
        {
            dp[i, 0] = Mathf.Max(dp[i - 1, 0], Vector3.Distance(path1[i], path2[0]));
        }

        for (int j = 1; j < m; j++)
        {
            dp[0, j] = Mathf.Max(dp[0, j - 1], Vector3.Distance(path1[0], path2[j]));
        }

        // DP 테이블 채우기
        for (int i = 1; i < n; i++)
        {
            for (int j = 1; j < m; j++)
            {
                float cost = Vector3.Distance(path1[i], path2[j]);
                dp[i, j] = Mathf.Min(Mathf.Max(dp[i - 1, j], cost),
                                    Mathf.Min(Mathf.Max(dp[i, j - 1], cost),
                                             Mathf.Max(dp[i - 1, j - 1], cost)));
            }
        }

        return dp[n - 1, m - 1];
    }
}