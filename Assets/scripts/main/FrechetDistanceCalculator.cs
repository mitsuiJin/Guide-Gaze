using System.Collections.Generic;
using UnityEngine;

public static class FrechetDistanceCalculator
{
    public static float Calculate(List<Vector3> a, List<Vector3> b)
    {
        if (a == null || b == null || a.Count == 0 || b.Count == 0)
            return float.MaxValue;

        float[,] dp = new float[a.Count, b.Count];

        for (int i = 0; i < a.Count; i++)
        {
            for (int j = 0; j < b.Count; j++)
            {
                float dist = Vector3.Distance(a[i], b[j]);

                if (i == 0 && j == 0)
                    dp[i, j] = dist;
                else if (i == 0)
                    dp[i, j] = Mathf.Max(dp[i, j - 1], dist);
                else if (j == 0)
                    dp[i, j] = Mathf.Max(dp[i - 1, j], dist);
                else
                    dp[i, j] = Mathf.Max(Mathf.Min(dp[i - 1, j], dp[i - 1, j - 1], dp[i, j - 1]), dist);
            }
        }

        return dp[a.Count - 1, b.Count - 1];
    }
}
