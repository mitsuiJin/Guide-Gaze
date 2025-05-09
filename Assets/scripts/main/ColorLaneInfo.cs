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

        for (int i = 0; i < 3; i++) // 🔁 3번 깜빡이기
        {
            lineRenderer.widthMultiplier = originalWidth * 2f;
            yield return new WaitForSeconds(0.2f);
            lineRenderer.widthMultiplier = originalWidth;
            yield return new WaitForSeconds(0.2f);
        }
    }

    public List<Vector3> GetWorldPoints()
    {
        return positions;
    }
}
