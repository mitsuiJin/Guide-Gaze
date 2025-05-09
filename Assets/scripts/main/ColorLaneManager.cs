using System.Collections.Generic;
using UnityEngine;

public class ColorLaneManager : MonoBehaviour
{
    // 🔹 싱글톤 인스턴스
    public static ColorLaneManager Instance { get; private set; }

    // 씬 내 ColorLaneInfo들을 자동 등록할 리스트
    public List<ColorLaneInfo> lanes = new List<ColorLaneInfo>();

    void Awake()
    {
        // 🔹 중복 방지 및 인스턴스 등록
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Unity 2023 이상 호환: 더 빠르고 정렬 없는 탐색 방식
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

    /// <summary>
    /// 현재 등록된 모든 ColorLane 반환
    /// </summary>
    public List<ColorLaneInfo> GetAllColorLanes()
    {
        return lanes;
    }
}
