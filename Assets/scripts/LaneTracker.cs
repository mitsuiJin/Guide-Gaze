using UnityEngine;
using System.Collections.Generic;

public class LaneTracker : MonoBehaviour
{
    public LineRenderer colorLaneRenderer;
    public MousePathDrawer mouseDrawer;
    public float frechetDistanceThreshold = 0.1f;

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

        float frechetDistance = CalculateFrechetDistance(colorLanePoints, drawnPoints);
        Debug.Log("Frechet Distance: " + frechetDistance);

        if (frechetDistance < frechetDistanceThreshold)
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

    float CalculateFrechetDistance(List<Vector3> path1, List<Vector3> path2)
    {
        int n = path1.Count;
        int m = path2.Count;

        if (n == 0 || m == 0)
        {
            return float.MaxValue;
        }

        float[,] dp = new float[n, m];
        dp[0, 0] = Vector3.Distance(path1[0], path2[0]);

        for (int i = 1; i < n; i++)
            dp[i, 0] = Mathf.Max(dp[i - 1, 0], Vector3.Distance(path1[i], path2[0]));

        for (int j = 1; j < m; j++)
            dp[0, j] = Mathf.Max(dp[0, j - 1], Vector3.Distance(path1[0], path2[j]));

        for (int i = 1; i < n; i++)
        {
            for (int j = 1; j < m; j++)
            {
                float cost = Vector3.Distance(path1[i], path2[j]);
                dp[i, j] = Mathf.Min(
                    Mathf.Max(dp[i - 1, j], cost),
                    Mathf.Min(
                        Mathf.Max(dp[i, j - 1], cost),
                        Mathf.Max(dp[i - 1, j - 1], cost)
                    )
                );
            }
        }

        return dp[n - 1, m - 1];
    }
}
