// ColorLaneInfo.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorLaneInfo : MonoBehaviour
{
    public List<Vector3> positions = new List<Vector3>();
    [HideInInspector] public LineRenderer lineRenderer;

    private Coroutine blinkCoroutine;
    private float initialWidthMultiplier = -1f; // LineRenderer의 초기 너비 저장용

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError($"[ColorLaneInfo] LineRenderer component not found on {gameObject.name}.");
        }
        else
        {
            // LineRenderer가 존재하면 초기 너비 저장
            initialWidthMultiplier = lineRenderer.widthMultiplier;
        }

        // ColorLaneManager에 자신을 등록
        if (ColorLaneManager.Instance != null)
        {
            ColorLaneManager.Instance.RegisterLane(this);
        }
        else
        {
            Debug.LogWarning($"[ColorLaneInfo] ColorLaneManager.Instance not found during Awake for {gameObject.name}. Lane will not be registered. Ensure ColorLaneManager is active and its Awake runs first (check Script Execution Order if issues persist).");
        }
    }

    void OnDestroy()
    {
        // ColorLaneManager에서 자신을 해제
        if (ColorLaneManager.Instance != null)
        {
            ColorLaneManager.Instance.UnregisterLane(this);
        }

        // 실행 중인 코루틴이 있다면 중지
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    /// <summary>
    /// 라인을 깜빡이며 하이라이트 처리 (3회 반복)
    /// </summary>
    public void Highlight(bool isOn)
    {
        if (lineRenderer == null)
        {
            // Debug.LogWarning($"[ColorLaneInfo] Highlight called on {gameObject.name}, but LineRenderer is null.");
            return;
        }
        // initialWidthMultiplier가 설정되지 않았다면 (Awake에서 lineRenderer가 없었을 경우 등) 다시 시도
        if (initialWidthMultiplier < 0 && lineRenderer != null)
        {
            initialWidthMultiplier = lineRenderer.widthMultiplier;
        }


        if (isOn)
        {
            if (blinkCoroutine != null) // 이미 실행 중인 깜빡임이 있다면 중지
            {
                StopCoroutine(blinkCoroutine);
                if (initialWidthMultiplier >= 0) lineRenderer.widthMultiplier = initialWidthMultiplier; // 원래 두께로 복원
            }
            if (initialWidthMultiplier >= 0) // 유효한 초기 너비가 있을 때만 깜빡임 시작
            {
                blinkCoroutine = StartCoroutine(BlinkLine());
            }
            else
            {
                // Debug.LogWarning($"[ColorLaneInfo] Cannot start highlight on {gameObject.name} due to invalid initialWidthMultiplier.");
            }
        }
        else // 하이라이트 끄기 (깜빡임 즉시 중지 및 원래 상태로)
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            if (initialWidthMultiplier >= 0) lineRenderer.widthMultiplier = initialWidthMultiplier; // 원래 두께로 복원
        }
    }

    private IEnumerator BlinkLine()
    {
        // initialWidthMultiplier는 Awake에서 설정되거나 Highlight에서 재시도되어야 함
        if (initialWidthMultiplier < 0)
        {
            // Debug.LogError($"[ColorLaneInfo] BlinkLine cannot execute on {gameObject.name} because initialWidthMultiplier is not set.");
            yield break; // 초기 너비 없이는 실행 불가
        }

        for (int i = 0; i < 3; i++)
        {
            lineRenderer.widthMultiplier = initialWidthMultiplier * 2f;
            yield return new WaitForSeconds(0.2f);
            lineRenderer.widthMultiplier = initialWidthMultiplier;
            yield return new WaitForSeconds(0.2f);
        }
        blinkCoroutine = null; // 코루틴 완료 후 null로 설정
    }

    /// <summary>
    /// 이 Color Lane의 경로 좌표 반환 (월드 좌표)
    /// </summary>
    public List<Vector3> GetWorldPoints()
    {
        return positions;
    }
}