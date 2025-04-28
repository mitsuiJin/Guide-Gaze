using Tobii.Research.Unity;
using UnityEngine;

public class GazeTrailController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // GazeTrail 인스턴스를 사용하여 시선 경로 활성화
        GazeTrail.Instance.On = true;  // 시선 경로를 켬
    }

    void Update()
    {
        // 시선 경로의 입자 수를 조정하거나 기타 속성 변경 가능
        GazeTrail.Instance.ParticleCount = 500;  // 입자 수 설정 (0~1000)
    }
}
