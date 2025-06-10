using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CueLogger : MonoBehaviour
{
    private string logPath;

    [SerializeField] private GameObject targetKeyObject; // 타겟 키 오브젝트 (예: A, B 등)
    private string targetSymbol; // ex: "A"

    private void Awake()
    {
        logPath = Application.dataPath + "/cue_log_filtered.csv";
        targetSymbol = targetKeyObject.name; // 예: "A"

        if (!File.Exists(logPath))
        {
            File.WriteAllText(logPath, "trial_id,laneName,frechet,speedDiff,isBest,isTarget,isMatch\n");
        }
    }

    public void LogTrial(int trialId, string targetLane, string bestLane,
        List<string> laneNames, List<float> frechets, List<float> speedDiffs)
    {
        using (StreamWriter sw = File.AppendText(logPath))
        {
            for (int i = 0; i < laneNames.Count; i++)
            {
                // laneName이 ex. "Curve_A"라면 마지막 글자가 Symbol
                string laneSymbol = laneNames[i].Substring(laneNames[i].Length - 1);

                if (laneSymbol != targetSymbol)
                    continue; // target이 아닌 레인은 무시

                bool isBest = laneNames[i] == bestLane;
                string line = $"{trialId},{laneNames[i]},{frechets[i]},{speedDiffs[i]},{isBest},{targetSymbol},{laneSymbol == targetSymbol}";
                sw.WriteLine(line);
            }
        }

        Debug.Log($"✅ Trial {trialId} (타겟 {targetSymbol}) 로그 저장 완료");
    }
}
