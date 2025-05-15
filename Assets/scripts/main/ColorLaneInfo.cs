// ColorLaneInfo.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorLaneInfo : MonoBehaviour
{
    public List<Vector3> positions = new List<Vector3>();
    [HideInInspector] public LineRenderer lineRenderer;

    private Coroutine blinkCoroutine;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>
    /// 라인을 깜빡이며 하이라이트 처리 (3회 반복)
    /// </summary>
    public void Highlight(bool isOn)
    {
        if (lineRenderer == null) return;

        if (isOn)
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            blinkCoroutine = StartCoroutine(BlinkLine());
        }
    }

    private IEnumerator BlinkLine()
    {
        float originalWidth = lineRenderer.widthMultiplier;

        for (int i = 0; i < 3; i++)
        {
            lineRenderer.widthMultiplier = originalWidth * 2f;
            yield return new WaitForSeconds(0.2f);
            lineRenderer.widthMultiplier = originalWidth;
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 이 Color Lane의 경로 좌표 반환
    /// </summary>
    public List<Vector3> GetWorldPoints()
    {
        return positions;
    }
}
