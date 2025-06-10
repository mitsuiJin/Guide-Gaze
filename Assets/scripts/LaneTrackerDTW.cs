using UnityEngine;
using System.Collections.Generic;

public class LaneTrackerDTW:MonoBehaviour
{
    public LineRenderer colorLaneRenderer;
    public MousePathDrawer mouseDrawer;
    public float dtwDistanceThreshold = 0.2f; // DTW threshold 조정

    private bool hasChecked = false;

    void Update()
    {
        if (!mouseDrawer.isDrawing && !hasChecked && mouseDrawer.DrawnPoints.Count > 0)
        {
            CheckSimilarity();
            hasChecked = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            hasChecked = false;
        }
    }

    void CheckSimilarity()
    {
        List<Vector3> drawnPoints = mouseDrawer.DrawnPoints;
        List<Vector3> colorLanePoints = GetColorLanePoints();

        if (colorLanePoints.Count == 0 || drawnPoints.Count == 0)
        {
            Debug.LogWarning("Color lane 또는 그려진 선에 점이 없습니다.");
            return;
        }

        float dtwDistance = CalculateDTWDistance(colorLanePoints, drawnPoints);
        Debug.Log("DTW Distance: " + dtwDistance);

        if (dtwDistance < dtwDistanceThreshold)
        {
            Debug.Log("✅ Success: Drawn lane matches the color lane!");
        }
        else
        {
            Debug.Log("❌ Failed: The drawn lane does not match the color lane.");
        }
    }

    List<Vector3> GetColorLanePoints()
    {
        if (colorLaneRenderer == null) return new List<Vector3>();
        Vector3[] positions = new Vector3[colorLaneRenderer.positionCount];
        colorLaneRenderer.GetPositions(positions);
        return new List<Vector3>(positions);
    }

    // DTW 계산 함수
    float CalculateDTWDistance(List<Vector3> path1, List<Vector3> path2)
    {
        int n = path1.Count;
        int m = path2.Count;

        // Dynamic Programming 테이블
        float[,] dp = new float[n, m];

        // 초기값 설정
        dp[0, 0] = Vector3.Distance(path1[0], path2[0]);

        // 첫 번째 열 채우기
        for (int i = 1; i < n; i++)
        {
            dp[i, 0] = dp[i - 1, 0] + Vector3.Distance(path1[i], path2[0]);
        }

        // 첫 번째 행 채우기
        for (int j = 1; j < m; j++)
        {
            dp[0, j] = dp[0, j - 1] + Vector3.Distance(path1[0], path2[j]);
        }

        // 나머지 DP 테이블 채우기
        for (int i = 1; i < n; i++)
        {
            for (int j = 1; j < m; j++)
            {
                float cost = Vector3.Distance(path1[i], path2[j]);
                dp[i, j] = Mathf.Min(
                    Mathf.Min(dp[i - 1, j], dp[i, j - 1]),
                    dp[i - 1, j - 1]
                ) + cost;
            }
        }

        return dp[n - 1, m - 1];
    }
}
