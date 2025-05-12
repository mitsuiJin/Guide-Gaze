using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 내 ColorLaneInfo들을 관리하는 싱글톤 매니저
/// </summary>
public class ColorLaneManager : MonoBehaviour
{
    public static ColorLaneManager Instance { get; private set; }
    public List<ColorLaneInfo> lanes = new List<ColorLaneInfo>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (lanes == null || lanes.Count == 0)
        {
            lanes = new List<ColorLaneInfo>(
                FindObjectsByType<ColorLaneInfo>(FindObjectsSortMode.None)
            );
            Debug.Log($"📌 ColorLane 자동 등록 완료: {lanes.Count}개 탐색됨");
        }
        else
        {
            Debug.Log($"📌 ColorLane 수동 등록 상태: {lanes.Count}개");
        }
    }

    public List<ColorLaneInfo> GetAllColorLanes()
    {
        return lanes;
    }
}
