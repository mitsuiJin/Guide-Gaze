// ColorLaneManager.cs
using System.Collections.Generic;
using UnityEngine;

public class ColorLaneManager : MonoBehaviour
{
    public static ColorLaneManager Instance { get; private set; }

    // 이 리스트는 RegisterLane/UnregisterLane을 통해 관리됩니다.
    private List<ColorLaneInfo> activeLanes = new List<ColorLaneInfo>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // activeLanes 리스트 초기화
        if (activeLanes == null)
        {
            activeLanes = new List<ColorLaneInfo>();
        }
    }

    void Start()
    {
        // 이 부분은 선택적입니다.
        // 만약 씬에 미리 배치된 ColorLaneInfo 객체들이 있고,
        // 그것들이 Awake에서 RegisterLane을 호출하기 전에 Manager가 참조해야 할 경우 등에 대비한 코드입니다.
        // 보통은 ColorLaneInfo의 Awake에서 RegisterLane을 호출하므로, 이 Find 로직은 중복이거나 필요 없을 수 있습니다.
        if (activeLanes.Count == 0)
        {
            // Debug.Log("[ColorLaneManager] Start: activeLanes is empty. Trying to find existing lanes in scene.");
            PopulateLanesFromSceneFallback();
        }
        else
        {
            // Debug.Log($"[ColorLaneManager] Start: {activeLanes.Count} lanes already registered (likely via ColorLaneInfo.Awake).");
        }
    }

    // 씬에서 ColorLaneInfo 객체를 찾아 리스트에 추가하는 폴백(fallback) 함수
    private void PopulateLanesFromSceneFallback()
    {
        // 기존 등록된 것과 중복될 수 있으므로, 이 함수는 주의해서 사용하거나,
        // RegisterLane에서 중복을 확실히 방지해야 합니다. (현재는 방지됨)
        ColorLaneInfo[] foundLanes;
#if UNITY_2023_1_OR_NEWER
        foundLanes = FindObjectsByType<ColorLaneInfo>(FindObjectsSortMode.None);
#else
        foundLanes = FindObjectsOfType<ColorLaneInfo>();
#endif
        // 이미 등록된 것은 추가하지 않도록 합니다.
        foreach (var foundLane in foundLanes)
        {
            if (!activeLanes.Contains(foundLane))
            {
                activeLanes.Add(foundLane);
            }
        }
        Debug.Log($"[ColorLaneManager] PopulateLanesFromSceneFallback: {foundLanes.Length} ColorLaneInfo objects found in scene. Total active lanes: {activeLanes.Count}");
    }

    /// <summary>
    /// ColorLaneInfo 객체를 관리 목록에 등록합니다.
    /// </summary>
    public void RegisterLane(ColorLaneInfo lane)
    {
        if (lane != null && !activeLanes.Contains(lane))
        {
            activeLanes.Add(lane);
            // Debug.Log($"[ColorLaneManager] Lane registered: {lane.gameObject.name}. Total active lanes: {activeLanes.Count}");
        }
    }

    /// <summary>
    /// ColorLaneInfo 객체를 관리 목록에서 제거합니다.
    /// </summary>
    public void UnregisterLane(ColorLaneInfo lane)
    {
        if (lane != null && activeLanes.Contains(lane))
        {
            activeLanes.Remove(lane);
            // Debug.Log($"[ColorLaneManager] Lane unregistered: {lane.gameObject.name}. Total active lanes: {activeLanes.Count}");
        }
    }

    /// <summary>
    /// 현재 활성화된 모든 ColorLaneInfo 객체 목록을 반환합니다.
    /// 외부에서 리스트를 직접 수정하는 것을 방지하기 위해 복사본을 반환하는 것이 더 안전합니다.
    /// </summary>
    public List<ColorLaneInfo> GetAllColorLanes()
    {
        return new List<ColorLaneInfo>(activeLanes); // 복사본 반환
    }

    /// <summary>
    /// 등록된 모든 레인을 강제로 초기화합니다. (선택적 유틸리티 함수)
    /// </summary>
    public void ClearAllRegisteredLanes()
    {
        // Debug.Log($"[ColorLaneManager] Clearing all {activeLanes.Count} registered lanes explicitly.");
        activeLanes.Clear();
    }
}