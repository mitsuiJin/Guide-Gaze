// DTWCalculator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

// 두 시계열(float 리스트) 사이의 DTW 거리 계산
public static class DTWCalculator
{
    // DTW 거리 계산 (입력: 두 float 리스트)
    public static float CalculateDTW(List<float> seqA, List<float> seqB)
    {
        int n = seqA.Count;
        int m = seqB.Count;
        float[,] dtw = new float[n + 1, m + 1];

        // DTW 배열 초기화
        for (int i = 0; i <= n; i++) dtw[i, 0] = Mathf.Infinity;
        for (int j = 0; j <= m; j++) dtw[0, j] = Mathf.Infinity;
        dtw[0, 0] = 0;

        // 동적 프로그래밍으로 최단 경로 계산
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                float cost = Mathf.Abs(seqA[i - 1] - seqB[j - 1]);
                dtw[i, j] = cost + Mathf.Min(
                    dtw[i - 1, j],    // 삽입
                    dtw[i, j - 1],    // 삭제
                    dtw[i - 1, j - 1] // 매치
                );
            }
        }
        return dtw[n, m];
    }
}
