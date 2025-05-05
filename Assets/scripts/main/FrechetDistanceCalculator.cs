using System.Collections.Generic;
using UnityEngine;

public static class FrechetDistanceCalculator
{
    public static float ComputeFrechetDistance(List<Vector2> P, List<Vector2> Q)
    {
        int m = P.Count;
        int n = Q.Count;
        float[,] ca = new float[m, n];

        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                ca[i, j] = -1f;

        return Compute(ca, P, Q, m - 1, n - 1);
    }

    private static float Compute(float[,] ca, List<Vector2> P, List<Vector2> Q, int i, int j)
    {
        if (ca[i, j] > -1f)
            return ca[i, j];
        else if (i == 0 && j == 0)
            ca[i, j] = Vector2.Distance(P[0], Q[0]);
        else if (i > 0 && j == 0)
            ca[i, j] = Mathf.Max(Compute(ca, P, Q, i - 1, 0), Vector2.Distance(P[i], Q[0]));
        else if (i == 0 && j > 0)
            ca[i, j] = Mathf.Max(Compute(ca, P, Q, 0, j - 1), Vector2.Distance(P[0], Q[j]));
        else if (i > 0 && j > 0)
        {
            float minPrev = Mathf.Min(
                Compute(ca, P, Q, i - 1, j),
                Compute(ca, P, Q, i - 1, j - 1),
                Compute(ca, P, Q, i, j - 1)
            );
            ca[i, j] = Mathf.Max(minPrev, Vector2.Distance(P[i], Q[j]));
        }
        else
            ca[i, j] = float.PositiveInfinity;

        return ca[i, j];
    }
}
