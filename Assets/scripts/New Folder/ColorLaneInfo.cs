// ColorLaneInfo.cs
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ColorLaneInfo : MonoBehaviour
{
    [Tooltip("해당 Color Lane의 단축키 이름 (예: Ctrl+S)")]
    public string shortcutName;

    [Tooltip("이 레인의 기준 시간 시퀀스 (DTW 비교용)")] // DTW
    public List<float> referenceTimes = new List<float>();
}