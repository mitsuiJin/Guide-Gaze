// DTWCalculator.cs
using System.Collections.Generic;
using UnityEngine;

public static class DTWCalculator
{
    public static float CalculateDTW(List<float> seqA, List<float> seqB)
    {
        // 입력 검증: 빈 시퀀스 처리
        if (seqA == null || seqB == null || seqA.Count == 0 || seqB.Count == 0)
            return Mathf.Infinity;

        int n = seqA.Count;
        int m = seqB.Count;
        float[,] dtw = new float[n + 1, m + 1];

        // DTW 배열 초기화 (전체를 Infinity로)
        for (int i = 0; i <= n; i++)
            for (int j = 0; j <= m; j++)
                dtw[i, j] = Mathf.Infinity;

        dtw[0, 0] = 0; // 시작점 초기화

        // 동적 프로그래밍 계산
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                float cost = Mathf.Abs(seqA[i - 1] - seqB[j - 1]);
                dtw[i, j] = cost + Mathf.Min(
                    dtw[i - 1, j],   // 삽입
                    dtw[i, j - 1],   // 삭제
                    dtw[i - 1, j - 1]  // 매치
                );
            }
        }
        return dtw[n, m];
    }
}
