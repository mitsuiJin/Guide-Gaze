using UnityEngine;

/// <summary>
/// 원형 파동(웨이브) 정보를 담는 클래스
/// </summary>
[System.Serializable]
public class WaveCircle
{
    public Vector2 center = Vector2.zero; // 원의 중심
    public float radius = 1f;             // 반지름
    public float curveStrength = 0.5f;    // 곡률 세기 (바깥 원일수록 크게)
    public float angle = 0f;
}
