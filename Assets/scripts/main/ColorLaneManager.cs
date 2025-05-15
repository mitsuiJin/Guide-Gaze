// ColorLaneManager.cs

using System.Collections.Generic;
using UnityEngine;

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
#if UNITY_2023_1_OR_NEWER
            lanes = new List<ColorLaneInfo>(FindObjectsByType<ColorLaneInfo>(FindObjectsSortMode.None));
#else
            lanes = new List<ColorLaneInfo>(FindObjectsOfType<ColorLaneInfo>());
#endif
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
