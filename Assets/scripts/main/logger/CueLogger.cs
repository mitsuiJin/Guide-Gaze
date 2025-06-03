using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CueLogger : MonoBehaviour
{
    private string logPath;
    [SerializeField] private GameObject targetKeyObject;

    private void Awake()
    {
        logPath = Application.dataPath + "/cue_log.csv";
        if (!File.Exists(logPath))
        {
            File.WriteAllText(logPath, "trialId,laneName,frechet,speedDiff,isBest,isTarget,isMatch\n");
        }
    }

    public void LogTrial(int trialId, string targetLane, string bestLane,
        List<string> laneNames, List<float> frechets, List<float> speedDiffs)
    {
        using (StreamWriter sw = File.AppendText(logPath))
        {
            for (int i = 0; i < laneNames.Count; i++)
            {
                bool isBest = laneNames[i] == bestLane;

                // 'Curve_A' → 'A' 추출
                string laneAlpha = laneNames[i].Length > 0 ? laneNames[i][^1].ToString() : "";
                bool isTargetMatch = laneAlpha == targetLane;

                string line = $"{trialId},{laneNames[i]},{frechets[i]},{speedDiffs[i]},{isBest},{targetLane},{isTargetMatch}";
                sw.WriteLine(line);
            }
        }

        Debug.Log($"✅ Trial {trialId} 로그 저장 완료");
    }
}
