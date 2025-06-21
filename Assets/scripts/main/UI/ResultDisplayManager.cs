// ResultDisplayManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class ResultDisplayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI[] frechetDistanceTexts;
    [SerializeField] private TextMeshProUGUI[] speedSimilarityTexts;
    [SerializeField] private TextMeshProUGUI finalResultText;

    private void OnEnable()
    {
        // MultiLineRendererGenerator가 차선 생성을 마쳤다는 신호를 받습니다.
        MultiLineRendererGenerator.OnLanesRegenerated += HandleLanesRegenerated;
        
        // LaneMatcher가 점수 계산을 마쳤다는 신호를 받습니다.
        LaneMatcher.OnComparisonComplete += UpdateScores;
    }

    private void OnDisable()
    {
        MultiLineRendererGenerator.OnLanesRegenerated -= HandleLanesRegenerated;
        LaneMatcher.OnComparisonComplete -= UpdateScores;
    }

    private void Start()
    {
        ClearAllTexts();
    }

    /// <summary>
    /// 차선이 새로 생성되었다는 신호를 받으면, 바로 처리하지 않고 코루틴을 실행합니다.
    /// </summary>
    private void HandleLanesRegenerated()
    {
        // 이전에 실행 중이던 코루틴이 있다면 중지시켜 중복 실행을 방지합니다.
        StopAllCoroutines();
        StartCoroutine(PrepareUIAfterFrameDelay());
    }

    /// <summary>
    /// [핵심] 한 프레임 뒤에 UI를 준비하여, 데이터가 밀리는 문제를 해결합니다.
    /// </summary>
    private IEnumerator PrepareUIAfterFrameDelay()
    {
        // 현재 프레임의 모든 작업(특히 이전 오브젝트 파괴)이 끝날 때까지 기다립니다.
        yield return new WaitForEndOfFrame();

        // 이제 데이터가 깨끗해졌으므로, UI 업데이트를 시작합니다.
        List<ColorLaneInfo> allLanes = ColorLaneManager.Instance.GetAllColorLanes();
        
        if (allLanes == null) 
        {
            ClearAllTexts();
            yield break;
        }

        // 키 이름만 먼저 UI에 표시합니다. (순서는 보장되지 않지만, 밀리지는 않습니다)
        for (int i = 0; i < frechetDistanceTexts.Length; i++)
        {
            if (i < allLanes.Count)
            {
                frechetDistanceTexts[i].text = $"{allLanes[i].keyName} 프레셰 거리: -";
            }
            else
            {
                frechetDistanceTexts[i].text = "";
            }
        }

        for (int i = 0; i < speedSimilarityTexts.Length; i++)
        {
            if (i < allLanes.Count)
            {
                speedSimilarityTexts[i].text = $"{allLanes[i].keyName} 속도 유사도: -";
            }
            else
            {
                speedSimilarityTexts[i].text = "";
            }
        }
        
        finalResultText.text = "▶ 결과 대기 중...";
    }

    /// <summary>
    /// 계산 완료 후 점수만 업데이트합니다. 순서가 뒤섞여도 이름으로 찾아갑니다.
    /// </summary>
    private void UpdateScores(List<LaneResultData> allResults, string bestMatchKeyName)
    {
        if (allResults == null) return;
        
        // 점수를 업데이트하기 전에, 현재 표시된 키 이름들과 일치하는지 확인하며 채워넣습니다.
        foreach (var result in allResults)
        {
            // 이름이 같은 프레셰 텍스트를 찾아서 점수를 업데이트합니다.
            TextMeshProUGUI frechetText = frechetDistanceTexts.FirstOrDefault(t => t.text.StartsWith(result.keyName));
            if (frechetText != null)
            {
                frechetText.text = $"{result.keyName} 프레셰 거리: {result.normFD:F3}";
            }

            // 이름이 같은 속도 텍스트를 찾아서 점수를 업데이트합니다.
            TextMeshProUGUI speedText = speedSimilarityTexts.FirstOrDefault(t => t.text.StartsWith(result.keyName));
            if (speedText != null)
            {
                speedText.text = $"{result.keyName} 속도 유사도: {result.speedSim:F3}";
            }
        }

        // 최종 결과 텍스트 업데이트
        if (!string.IsNullOrEmpty(bestMatchKeyName))
        {
            finalResultText.text = $"▶ 최종 선택: {bestMatchKeyName}";
        }
        else
        {
            finalResultText.text = "▶ 최종 선택: 없음";
        }
    }

    private void ClearAllTexts()
    {
        foreach (var txt in frechetDistanceTexts) { if (txt != null) txt.text = ""; }
        foreach (var txt in speedSimilarityTexts) { if (txt != null) txt.text = ""; }
        if (finalResultText != null) finalResultText.text = "▶ 결과 대기 중...";
    }
}