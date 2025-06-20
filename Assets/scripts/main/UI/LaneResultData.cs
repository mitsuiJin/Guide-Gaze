// LaneResultData.cs
// 이 스크립트는 각 차선의 결과 데이터를 담기 위한 구조체입니다.
// 별도의 게임 오브젝트에 붙일 필요가 없습니다.

public struct LaneResultData
{
    public string laneName;
    public string keyName;  // 차선의 고유 키 이름 (laneName과 동일하게 설정)
    public float normFD;    // 정규화된 프레셰 거리 (낮을수록 좋음)
    public float speedSim;  // 속도 유사도 (높을수록 좋음)

    public LaneResultData(string name, string key, float frechet, float speed)
    {
        this.laneName = name;
        this.keyName = key; // keyName은 laneName과 동일하게 설정
        this.normFD = frechet;
        this.speedSim = speed;
    }
}