// MultiLineRendererGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;

// Helper class to define a key pattern
[System.Serializable]
public class KeyPattern
{
    public string name;
    public string keyForRight;
    public string keyForTop;
    public string keyForLeft;
    public string keyForBottom;

    public KeyPattern(string name, string r, string t, string l, string b)
    {
        this.name = name;
        keyForRight = r; keyForTop = t; keyForLeft = l; keyForBottom = b;
    }
}

public class MultiLineRendererGenerator : MonoBehaviour
{
    public static event Action OnLanesRegenerated;

    [Header("References")]
    public Transform ctrlKeyCenter;
    public List<Transform> allAvailableKeys;
    public Material lineMaterialTemplate;

    [Header("UI")] // [추가] UI 관련 참조를 위한 헤더
    public TextMeshProUGUI patternNameText; // [추가] TextMeshPro 오브젝트를 연결할 변수


    [Header("Curve Base Settings")]
    public int curveResolution = 20;
    public float startOffset = 0.3f;

    [Header("Curve Style (P0 Side Based)")]
    [Tooltip("P0가 Top 또는 Right일 때 (안쪽 선) 적용될 높이 계수")]
    public float innerCurveHeightFactor = 0.25f;
    // outerCurveHeightFactor 분리
    [Tooltip("P0가 Left일 때 (바깥쪽 선) 적용될 높이 계수. 3차 베지어 시 핸들 강도에 영향.")]
    public float leftOuterCurveHeightFactor = 1.0f;
    [Tooltip("P0가 Bottom일 때 (바깥쪽 선) 적용될 높이 계수. 3차 베지어 시 핸들 강도에 영향.")]
    public float bottomOuterCurveHeightFactor = 1.0f;


    [Header("P1 Squeeze Heuristic")]
    [Tooltip("인접 키와 각도 차이가 이 값 미만이면 heightFactor 감소 시작")]
    public float neighborSqueezeAngleThreshold = 75f;
    [Tooltip("압착 시 heightFactor 최대 감소율 (1.0=감소없음, 0.5=절반)")]
    [Range(0.3f, 1.0f)]
    public float neighborSqueezeHeightFactorMultiplier = 0.6f;
    [Tooltip("외곽선이 내곽선의 목표 키와 가까울 때 적용될 heightFactor 감소율")]
    [Range(0.1f, 1.0f)]
    public float outerToInnerSqueezeMultiplier = 0.4f;
    [Tooltip("모든 heightFactor가 Clamp될 최소/최대값. 이 최소값은 곡선의 최소 곡률에 영향.")]
    public Vector2 minMaxHeightFactorClamp = new Vector2(0.25f, 1.2f);

    [Header("Cubic Bezier Settings - Left Lanes")]
    [Tooltip("좌측 3차 곡선: P0에서 CP1까지의 기본 이격 거리. LeftOuterCurveHeightFactor와 압착 변조가 곱해짐.")]
    public float leftCubicP0ToCp1Offset = 1.0f;
    [Tooltip("좌측 3차 곡선: P3에서 CP2까지의 기본 이격 거리. LeftOuterCurveHeightFactor와 압착 변조가 곱해짐.")]
    public float leftCubicP3ToCp2Offset = 1.0f;
    [Tooltip("좌측 3차 곡선: CP1의 기본 방향(P0에서 slot 방향)에서 추가적인 각도 오프셋 (도).")]
    public float leftCubicCp1AngleOffset = 0f;
    [Tooltip("좌측 3차 곡선: CP2의 기본 방향(P3에서 CP1 방향)에서 추가적인 각도 오프셋 (도).")]
    public float leftCubicCp2AngleOffset = 0f;

    [Header("Cubic Bezier Settings - Bottom Lanes")]
    [Tooltip("하단 3차 곡선: P0에서 CP1까지의 기본 이격 거리. BottomOuterCurveHeightFactor와 압착 변조가 곱해짐.")]
    public float bottomCubicP0ToCp1Offset = 1.0f;
    [Tooltip("하단 3차 곡선: P3에서 CP2까지의 기본 이격 거리. BottomOuterCurveHeightFactor와 압착 변조가 곱해짐.")]
    public float bottomCubicP3ToCp2Offset = 1.0f;
    [Tooltip("하단 3차 곡선: CP1의 기본 방향(P0에서 slot 방향)에서 추가적인 각도 오프셋 (도).")]
    public float bottomCubicCp1AngleOffset = 0f;
    [Tooltip("하단 3차 곡선: CP2의 기본 방향(P3에서 CP1 방향)에서 추가적인 각도 오프셋 (도).")]
    public float bottomCubicCp2AngleOffset = 0f;


    [Header("Target Keys (Optional - for initial non-pattern state)")]
    public List<Transform> targetKeys;

    [Header("Controls")]
    public KeyCode regenerateKey = KeyCode.R;

    private const int REQUIRED_TARGET_KEYS = 4;
    private enum CtrlSide { Right, Top, Left, Bottom }

    private static readonly Dictionary<CtrlSide, float> P0_SLOT_ANGLES_DEG = new Dictionary<CtrlSide, float>
    { { CtrlSide.Right, 0f }, { CtrlSide.Top, 90f }, { CtrlSide.Left, 180f }, { CtrlSide.Bottom, 270f } };
    private static readonly List<CtrlSide> P0_SLOT_PROCESSING_ORDER = new List<CtrlSide> { CtrlSide.Right, CtrlSide.Top, CtrlSide.Left, CtrlSide.Bottom };

    private class LaneGenerationData
    {
        public int initialSortIndex; public Transform targetKey; public Vector2 P0_startPoint;
        public Vector2 P3_endPoint; public float actualAngleRad; public CtrlSide assignedP0Side;
        public bool isCubic; public Vector2 CP1_cubic; public Vector2 CP2_cubic;
        public Vector2 P1_quadratic; public float finalHeightFactor;
    }
    private struct ProcessedKeyInfo
    {
        public Transform transform; public Vector2 worldPos; public float angleRad;
    }

    private List<Transform> selectedTargetKeysForDrawing = new List<Transform>();
    private List<LaneGenerationData> laneGenDataList = new List<LaneGenerationData>();
    private List<KeyPattern> definedPatterns = new List<KeyPattern>();
    private int currentPatternIndex = -1;
    private bool isInPatternMode = false;
    private Dictionary<string, Transform> availableKeyTransformsMap = new Dictionary<string, Transform>();

    private Vector2 RotateVector(Vector2 v, float degrees)
    { /* 이전과 동일 */
        if (degrees == 0) return v; float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians); float cos = Mathf.Cos(radians);
        float tx = v.x; float ty = v.y;
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    void Awake()
    { /* 이전과 동일 */
        if (allAvailableKeys != null) { foreach (Transform keyTransform in allAvailableKeys) { if (keyTransform != null) { if (!availableKeyTransformsMap.ContainsKey(keyTransform.name)) { availableKeyTransformsMap.Add(keyTransform.name, keyTransform); } else { Debug.LogWarning($"[MLRG] Duplicate key name '{keyTransform.name}' in allAvailableKeys. Using the first one encountered.", this); } } } }
        else { Debug.LogError("[MLRG] allAvailableKeys list is not assigned!", this); }
        PopulateDefinedPatterns();
    }
    void Start() { 
        ProcessLaneGeneration();
        UpdatePatternNameUI();
    }
    void Update()
    { /* 이전과 동일 */
        if (Input.GetKeyDown(regenerateKey)) {
            isInPatternMode = true;
            if (SquareMoverManager.Instance != null)
            {
                SquareMoverManager.Instance.ClearAllMovers();
            }

            if (definedPatterns.Count > 0)
            {
                currentPatternIndex++; if (currentPatternIndex >= definedPatterns.Count) { currentPatternIndex = 0; }
                Debug.Log($"[MLRG] Applying pattern: {definedPatterns[currentPatternIndex].name} (Index: {currentPatternIndex})", this);
            }
            else { currentPatternIndex = -1; Debug.LogWarning("[MLRG] Regenerate key pressed, but no patterns are defined.", this); } 
            ProcessLaneGeneration();
            UpdatePatternNameUI();
        }

    }
    // [추가] 패턴 이름을 UI에 표시하는 함수
    void UpdatePatternNameUI()
    {
        if (patternNameText == null) return; // UI 텍스트가 할당되지 않았으면 실행하지 않음

        if (isInPatternMode && currentPatternIndex >= 0 && currentPatternIndex < definedPatterns.Count)
        {
            string currentPatternName = definedPatterns[currentPatternIndex].name;
            patternNameText.text = "유형 이름: " + currentPatternName;
        }
        else
        {
            // 패턴 모드가 아니거나, 유효한 패턴이 없을 경우 기본 텍스트 표시
            patternNameText.text = "유형 이름: 없음";
        }
    }

    void PopulateDefinedPatterns()
    { /* 이전과 동일 (사용자 제공 패턴 목록 사용) */
        definedPatterns.Clear();
        definedPatterns.Add(new KeyPattern("I 유형 1", "D", "S", "A", "F"));
        definedPatterns.Add(new KeyPattern("I 유형 2 - 첫번째 라인일 경우.", "C", "Z", "X", "V"));
        definedPatterns.Add(new KeyPattern("O 유형 1 (꽉찬 2X2 사각형)", "A", "W", "Q", "S"));
        definedPatterns.Add(new KeyPattern("O 유형 2", "Z", "W", "Q", "X"));
        definedPatterns.Add(new KeyPattern("O 유형 3", "Z", "E", "Q", "C"));
        definedPatterns.Add(new KeyPattern("O 유형 좌우반전 1", "S", "A", "W", "E"));
        definedPatterns.Add(new KeyPattern("O 유형 좌우반전 2", "R", "Z", "W", "C"));
        definedPatterns.Add(new KeyPattern("O 유형 좌우반전 3", "Z", "E", "W", "X"));
        definedPatterns.Add(new KeyPattern("L 유형 1", "Z", "A", "Q", "X"));
        definedPatterns.Add(new KeyPattern("L 유형 2", "X", "Z", "D", "C"));
        definedPatterns.Add(new KeyPattern("L 유형 3", "Z", "S", "A", "D"));
        definedPatterns.Add(new KeyPattern("L 유형 4", "S", "W", "Q", "X"));
        definedPatterns.Add(new KeyPattern("J 유형 1", "Z", "S", "W", "X"));
        definedPatterns.Add(new KeyPattern("J 유형 2", "X", "Z", "A", "C"));
        definedPatterns.Add(new KeyPattern("J 유형 3", "D", "S", "A", "C"));
        definedPatterns.Add(new KeyPattern("J 유형 4", "A", "W", "Q", "Z"));
        definedPatterns.Add(new KeyPattern("S 유형 1", "X", "Z", "D", "F"));
        definedPatterns.Add(new KeyPattern("S 유형 2", "S", "A", "Q", "X"));
        definedPatterns.Add(new KeyPattern("S 유형 3", "D", "S", "Q", "C"));
        definedPatterns.Add(new KeyPattern("Z 유형 1", "C", "S", "A", "V"));
        definedPatterns.Add(new KeyPattern("Z 유형 2", "C", "W", "Q", "V"));
        definedPatterns.Add(new KeyPattern("Z 유형 3", "S", "A", "W", "Z"));
        definedPatterns.Add(new KeyPattern("Z 유형 4", "S", "A", "E", "Z"));
        definedPatterns.Add(new KeyPattern("T 유형 1", "D", "S", "A", "X"));
        definedPatterns.Add(new KeyPattern("T 반대 유형", "X", "Z", "S", "C"));
        definedPatterns.Add(new KeyPattern("ㅓ, ㅏ 유형 1", "S", "A", "Q", "Z"));
        definedPatterns.Add(new KeyPattern("ㅓ, ㅏ 유형 2", "S", "A", "E", "X"));
        definedPatterns.Add(new KeyPattern("ㅓ, ㅏ 유형 3", "Z", "S", "W", "D"));
        definedPatterns.Add(new KeyPattern("ㅓ, ㅏ 유형 4", "S", "A", "W", "X"));
    }
    Transform GetKeyTransformByName(string keyName)
    { /* 이전과 동일 */
        if (string.IsNullOrEmpty(keyName)) return null; if (availableKeyTransformsMap.TryGetValue(keyName, out Transform foundTransform)) { return foundTransform; }
        Debug.LogWarning($"[MLRG] Key Transform for '{keyName}' not found in availableKeyTransformsMap.", this); return null;
    }

    void ProcessLaneGeneration()
    {
        foreach (Transform child in transform) { if (child.gameObject.name.StartsWith("Curve_")) Destroy(child.gameObject); }
        laneGenDataList.Clear();
        if (ctrlKeyCenter == null) { Debug.LogError("[MLRG] ctrlKeyCenter not assigned.", this); OnLanesRegenerated?.Invoke(); return; }
        Vector2 ctrlPos = ctrlKeyCenter.position;
        List<ProcessedKeyInfo> actualTargetKeysSortedForSqueeze = new List<ProcessedKeyInfo>();

        if (isInPatternMode && currentPatternIndex >= 0 && currentPatternIndex < definedPatterns.Count)
        {
            KeyPattern currentActualPattern = definedPatterns[currentPatternIndex];
            Transform keyR = GetKeyTransformByName(currentActualPattern.keyForRight); Transform keyT = GetKeyTransformByName(currentActualPattern.keyForTop); Transform keyL = GetKeyTransformByName(currentActualPattern.keyForLeft); Transform keyB = GetKeyTransformByName(currentActualPattern.keyForBottom);
            List<ProcessedKeyInfo> patternResolvedKeys = new List<ProcessedKeyInfo>();
            Action<Transform, string> AddToResolvedPatternKeys = (keyTransform, keyNameForSide) => { if (keyTransform != null) { Vector2 p = keyTransform.position; Vector2 d = p - ctrlPos; float a = Mathf.Atan2(d.y, d.x); if (a < 0) a += 2 * Mathf.PI; patternResolvedKeys.Add(new ProcessedKeyInfo { transform = keyTransform, worldPos = p, angleRad = a }); } else { Debug.LogError($"[MLRG] Target key for pattern '{currentActualPattern.name}' (side key name: '{keyNameForSide}') is null or not found.", this); } };
            AddToResolvedPatternKeys(keyR, currentActualPattern.keyForRight); AddToResolvedPatternKeys(keyT, currentActualPattern.keyForTop); AddToResolvedPatternKeys(keyL, currentActualPattern.keyForLeft); AddToResolvedPatternKeys(keyB, currentActualPattern.keyForBottom);
            actualTargetKeysSortedForSqueeze = patternResolvedKeys.OrderBy(k => k.angleRad).ToList();
            int p0SlotColorIndex = 0;
            foreach (CtrlSide p0Side in P0_SLOT_PROCESSING_ORDER)
            {
                Transform targetKeyForThisP0Slot = null;
                switch (p0Side) { case CtrlSide.Right: targetKeyForThisP0Slot = keyR; break; case CtrlSide.Top: targetKeyForThisP0Slot = keyT; break; case CtrlSide.Left: targetKeyForThisP0Slot = keyL; break; case CtrlSide.Bottom: targetKeyForThisP0Slot = keyB; break; }
                if (targetKeyForThisP0Slot == null) { p0SlotColorIndex++; continue; }
                Vector2 p_endPoint = targetKeyForThisP0Slot.position; Vector2 dirToP_EndPoint = p_endPoint - ctrlPos; float angleOfP_EndPointRad = Mathf.Atan2(dirToP_EndPoint.y, dirToP_EndPoint.x); if (angleOfP_EndPointRad < 0) angleOfP_EndPointRad += 2 * Mathf.PI;
                laneGenDataList.Add(new LaneGenerationData { initialSortIndex = p0SlotColorIndex, targetKey = targetKeyForThisP0Slot, P0_startPoint = GetStartPoint(ctrlPos, p0Side, startOffset), P3_endPoint = p_endPoint, actualAngleRad = angleOfP_EndPointRad, assignedP0Side = p0Side, isCubic = (p0Side == CtrlSide.Left || p0Side == CtrlSide.Bottom) });
                p0SlotColorIndex++;
            }
        }
        else
        { // Original Mode
            selectedTargetKeysForDrawing.Clear(); bool useInspector = targetKeys != null && targetKeys.Count == REQUIRED_TARGET_KEYS && targetKeys.All(t => t != null);
            if (useInspector) { selectedTargetKeysForDrawing.AddRange(targetKeys); } else { SelectRandomTargetKeys(); }
            if (selectedTargetKeysForDrawing.Count != REQUIRED_TARGET_KEYS) { Debug.LogError($"[MLRG] Original mode: Need {REQUIRED_TARGET_KEYS} target keys, found {selectedTargetKeysForDrawing.Count}.", this); OnLanesRegenerated?.Invoke(); return; }
            var tempInitialKeyInfos = selectedTargetKeysForDrawing.Select(t => { if (t == null) return new ProcessedKeyInfo { transform = null }; Vector2 p = t.position; Vector2 d = p - ctrlPos; float a = Mathf.Atan2(d.y, d.x); if (a < 0) a += 2 * Mathf.PI; return new ProcessedKeyInfo { transform = t, worldPos = p, angleRad = a }; }).Where(k => k.transform != null).OrderBy(k => k.angleRad).ToList();
            if (tempInitialKeyInfos.Count != REQUIRED_TARGET_KEYS) { Debug.LogError($"[MLRG] Original mode: After processing, not enough valid keys ({tempInitialKeyInfos.Count}).", this); OnLanesRegenerated?.Invoke(); return; }
            actualTargetKeysSortedForSqueeze = tempInitialKeyInfos; bool[] p0SlotIsUsed = new bool[P0_SLOT_PROCESSING_ORDER.Count];
            foreach (var keyInfo in actualTargetKeysSortedForSqueeze)
            {
                float bestAngularDifference = float.MaxValue; CtrlSide bestMatchingSlot = P0_SLOT_PROCESSING_ORDER[0]; int bestSlotInternalIndex = -1;
                for (int slotIdx = 0; slotIdx < P0_SLOT_PROCESSING_ORDER.Count; slotIdx++) { if (!p0SlotIsUsed[slotIdx]) { CtrlSide currentCandidateSlot = P0_SLOT_PROCESSING_ORDER[slotIdx]; float keyAngleDegrees = keyInfo.angleRad * Mathf.Rad2Deg; float slotNominalAngleDegrees = P0_SLOT_ANGLES_DEG[currentCandidateSlot]; float angularDiff = Mathf.Abs(Mathf.DeltaAngle(keyAngleDegrees, slotNominalAngleDegrees)); if (angularDiff < bestAngularDifference) { bestAngularDifference = angularDiff; bestMatchingSlot = currentCandidateSlot; bestSlotInternalIndex = slotIdx; } } }
                if (bestSlotInternalIndex != -1) p0SlotIsUsed[bestSlotInternalIndex] = true;
                laneGenDataList.Add(new LaneGenerationData { initialSortIndex = actualTargetKeysSortedForSqueeze.IndexOf(keyInfo), targetKey = keyInfo.transform, P0_startPoint = GetStartPoint(ctrlPos, bestMatchingSlot, startOffset), P3_endPoint = keyInfo.worldPos, actualAngleRad = keyInfo.angleRad, assignedP0Side = bestMatchingSlot, isCubic = (bestMatchingSlot == CtrlSide.Left || bestMatchingSlot == CtrlSide.Bottom) });
            }
        }

        if (laneGenDataList.Count == 0) { OnLanesRegenerated?.Invoke(); return; }

        for (int i = 0; i < laneGenDataList.Count; i++)
        {
            LaneGenerationData currentLane = laneGenDataList[i]; if (currentLane.targetKey == null) continue;

            // baseHeightFactor 결정 시 분리된 OuterCurveHeightFactor 사용
            float baseHeightFactor;
            if (currentLane.assignedP0Side == CtrlSide.Top || currentLane.assignedP0Side == CtrlSide.Right)
            {
                baseHeightFactor = this.innerCurveHeightFactor;
            }
            else if (currentLane.assignedP0Side == CtrlSide.Left)
            {
                baseHeightFactor = this.leftOuterCurveHeightFactor;
            }
            else
            { // Bottom
                baseHeightFactor = this.bottomOuterCurveHeightFactor;
            }

            float squeezeMultiplier = 1.0f;
            if (actualTargetKeysSortedForSqueeze.Count == REQUIRED_TARGET_KEYS)
            {
                int angularSortIndexOfCurrentKey = -1; for (int k = 0; k < actualTargetKeysSortedForSqueeze.Count; ++k) { if (actualTargetKeysSortedForSqueeze[k].transform == currentLane.targetKey) { angularSortIndexOfCurrentKey = k; break; } }
                if (angularSortIndexOfCurrentKey != -1)
                {
                    var prevKeyTargetInfo = actualTargetKeysSortedForSqueeze[(angularSortIndexOfCurrentKey - 1 + REQUIRED_TARGET_KEYS) % REQUIRED_TARGET_KEYS]; var nextKeyTargetInfo = actualTargetKeysSortedForSqueeze[(angularSortIndexOfCurrentKey + 1) % REQUIRED_TARGET_KEYS];
                    float angleToPrevDeg = Mathf.Abs(Mathf.DeltaAngle(currentLane.actualAngleRad * Mathf.Rad2Deg, prevKeyTargetInfo.angleRad * Mathf.Rad2Deg)); float angleToNextDeg = Mathf.Abs(Mathf.DeltaAngle(currentLane.actualAngleRad * Mathf.Rad2Deg, nextKeyTargetInfo.angleRad * Mathf.Rad2Deg));
                    bool currentIsOuter = (currentLane.assignedP0Side == CtrlSide.Left || currentLane.assignedP0Side == CtrlSide.Bottom);
                    Func<Transform, bool> IsTargetOfInnerLaneP0 = (targetTx) => { foreach (var ld in laneGenDataList) { if (ld.targetKey == targetTx) { return (ld.assignedP0Side == CtrlSide.Top || ld.assignedP0Side == CtrlSide.Right); } } return false; };
                    if (angleToPrevDeg < this.neighborSqueezeAngleThreshold) { float actualSqueezeFactorForPrev = this.neighborSqueezeHeightFactorMultiplier; if (currentIsOuter && prevKeyTargetInfo.transform != null && IsTargetOfInnerLaneP0(prevKeyTargetInfo.transform)) { actualSqueezeFactorForPrev = this.outerToInnerSqueezeMultiplier; } float prevSqueeze = Mathf.Lerp(actualSqueezeFactorForPrev, 1.0f, angleToPrevDeg / this.neighborSqueezeAngleThreshold); squeezeMultiplier = Mathf.Min(squeezeMultiplier, prevSqueeze); }
                    if (angleToNextDeg < this.neighborSqueezeAngleThreshold) { float actualSqueezeFactorForNext = this.neighborSqueezeHeightFactorMultiplier; if (currentIsOuter && nextKeyTargetInfo.transform != null && IsTargetOfInnerLaneP0(nextKeyTargetInfo.transform)) { actualSqueezeFactorForNext = this.outerToInnerSqueezeMultiplier; } float nextSqueeze = Mathf.Lerp(actualSqueezeFactorForNext, 1.0f, angleToNextDeg / this.neighborSqueezeAngleThreshold); squeezeMultiplier = Mathf.Min(squeezeMultiplier, nextSqueeze); }
                }
            }
            currentLane.finalHeightFactor = baseHeightFactor * squeezeMultiplier;
            currentLane.finalHeightFactor = Mathf.Clamp(currentLane.finalHeightFactor, minMaxHeightFactorClamp.x, minMaxHeightFactorClamp.y);
        }

        for (int i = 0; i < laneGenDataList.Count; i++)
        {
            LaneGenerationData currentLane = laneGenDataList[i];
            if (currentLane.targetKey == null) continue;

            if (currentLane.isCubic)
            {
                Vector2 p0 = currentLane.P0_startPoint;
                Vector2 p3 = currentLane.P3_endPoint;
                float distP0P3 = Vector2.Distance(p0, p3);

                if (distP0P3 < Mathf.Epsilon)
                {
                    currentLane.CP1_cubic = p0;
                    currentLane.CP2_cubic = p3;
                }
                else
                {
                    Vector2 slotOutwardDir = Vector2.zero;
                    float currentOuterCurveHeightFactor = 1.0f;
                    float currentCubicP0ToCp1Offset = 1.0f;
                    float currentCubicP3ToCp2Offset = 1.0f;
                    float currentCubicCp1AngleOffset = 0f;
                    float currentCubicCp2AngleOffset = 0f;

                    if (currentLane.assignedP0Side == CtrlSide.Left)
                    {
                        slotOutwardDir = Vector2.left;
                        currentOuterCurveHeightFactor = this.leftOuterCurveHeightFactor;
                        currentCubicP0ToCp1Offset = this.leftCubicP0ToCp1Offset;
                        currentCubicP3ToCp2Offset = this.leftCubicP3ToCp2Offset;
                        currentCubicCp1AngleOffset = this.leftCubicCp1AngleOffset;
                        currentCubicCp2AngleOffset = this.leftCubicCp2AngleOffset;
                    }
                    else if (currentLane.assignedP0Side == CtrlSide.Bottom)
                    {
                        slotOutwardDir = Vector2.down;
                        currentOuterCurveHeightFactor = this.bottomOuterCurveHeightFactor;
                        currentCubicP0ToCp1Offset = this.bottomCubicP0ToCp1Offset;
                        currentCubicP3ToCp2Offset = this.bottomCubicP3ToCp2Offset;
                        currentCubicCp1AngleOffset = this.bottomCubicCp1AngleOffset;
                        currentCubicCp2AngleOffset = this.bottomCubicCp2AngleOffset;
                    }

                    float squeezeModulation = Mathf.InverseLerp(minMaxHeightFactorClamp.x, minMaxHeightFactorClamp.y, currentLane.finalHeightFactor);
                    squeezeModulation = Mathf.Lerp(0.3f, 1.0f, squeezeModulation);

                    // CP1 계산
                    float cp1_actual_offset = currentCubicP0ToCp1Offset * currentOuterCurveHeightFactor * squeezeModulation;
                    Vector2 cp1BaseDirection = RotateVector(slotOutwardDir, currentCubicCp1AngleOffset);
                    currentLane.CP1_cubic = p0 + cp1BaseDirection * cp1_actual_offset;

                    // CP2 계산
                    Vector2 incomingTangentP3_baseDirection;
                    if (Vector2.Distance(p3, currentLane.CP1_cubic) < 0.01f)
                    {
                        if (distP0P3 < 0.01f) { incomingTangentP3_baseDirection = -slotOutwardDir; }
                        else { incomingTangentP3_baseDirection = (p3 - p0).normalized; }
                    }
                    else
                    {
                        incomingTangentP3_baseDirection = (p3 - currentLane.CP1_cubic).normalized;
                    }
                    Vector2 cp2FinalIncomingTangent = RotateVector(incomingTangentP3_baseDirection, currentCubicCp2AngleOffset);
                    float cp2_actual_offset = currentCubicP3ToCp2Offset * currentOuterCurveHeightFactor * squeezeModulation;
                    currentLane.CP2_cubic = p3 - cp2FinalIncomingTangent * cp2_actual_offset;
                }
            }
            else
            { // Quadratic
                bool p1BaseAtP0ForThisQuadratic = false;
                bool forceSlotOutwardForThisQuad = false;
                if (!isInPatternMode && (currentLane.assignedP0Side == CtrlSide.Left || currentLane.assignedP0Side == CtrlSide.Bottom))
                {
                    p1BaseAtP0ForThisQuadratic = true;
                    forceSlotOutwardForThisQuad = true;
                }
                currentLane.P1_quadratic = CalculateQuadraticP1Internal(currentLane.P0_startPoint, currentLane.P3_endPoint, ctrlPos, currentLane.assignedP0Side, currentLane.finalHeightFactor, startOffset, forceSlotOutwardForThisQuad, p1BaseAtP0ForThisQuadratic);
            }
        }

        foreach (var laneData in laneGenDataList)
        {
            if (laneData.targetKey != null)
            {
                if (laneData.isCubic) { DrawCubicBezier(laneData.initialSortIndex, laneData.P0_startPoint, laneData.CP1_cubic, laneData.CP2_cubic, laneData.P3_endPoint, laneData.targetKey.name); }
                else { DrawQuadraticBezier(laneData.initialSortIndex, laneData.P0_startPoint, laneData.P1_quadratic, laneData.P3_endPoint, laneData.targetKey.name); }
            }
        }
        OnLanesRegenerated?.Invoke();
    }

    Vector2 CalculateQuadraticP1Internal(Vector2 p0, Vector2 p2_end, Vector2 ctrlPos,
                                     CtrlSide assignedSideForP0, float actualHeightFactor,
                                     float currentStartOffset,
                                     bool forceSlotOutwardNormalForAssignedSide,
                                     bool setP1BaseAtP0)
    {
        Vector2 vecP0ToP2 = p2_end - p0;
        float distP0P2 = vecP0ToP2.magnitude;
        if (distP0P2 < Mathf.Epsilon) return Vector2.Lerp(p0, p2_end, 0.5f);

        Vector2 perpendicularNormal = new Vector2(-vecP0ToP2.y, vecP0ToP2.x).normalized;
        Vector2 referenceDirectionForNormal;
        Vector2 p0p2Midpoint = Vector2.Lerp(p0, p2_end, 0.5f);

        bool useEffectiveSlotOutwardNormal = false;
        if (forceSlotOutwardNormalForAssignedSide && (assignedSideForP0 == CtrlSide.Left || assignedSideForP0 == CtrlSide.Bottom))
        { useEffectiveSlotOutwardNormal = true; }

        if (useEffectiveSlotOutwardNormal)
        {
            float slotAngleRad = P0_SLOT_ANGLES_DEG[assignedSideForP0] * Mathf.Deg2Rad;
            referenceDirectionForNormal = new Vector2(Mathf.Cos(slotAngleRad), Mathf.Sin(slotAngleRad)).normalized;
        }
        else
        {
            Vector2 vecCtrlToP0P2Midpoint = p0p2Midpoint - ctrlPos;
            if (vecCtrlToP0P2Midpoint.sqrMagnitude < (currentStartOffset * 0.01f) * (currentStartOffset * 0.01f))
            {
                float slotAngleRad = P0_SLOT_ANGLES_DEG[assignedSideForP0] * Mathf.Deg2Rad;
                referenceDirectionForNormal = new Vector2(Mathf.Cos(slotAngleRad), Mathf.Sin(slotAngleRad)).normalized;
            }
            else
            {
                if (vecCtrlToP0P2Midpoint.sqrMagnitude > Mathf.Epsilon) { referenceDirectionForNormal = vecCtrlToP0P2Midpoint.normalized; }
                else { float slotAngleRad = P0_SLOT_ANGLES_DEG[assignedSideForP0] * Mathf.Deg2Rad; referenceDirectionForNormal = new Vector2(Mathf.Cos(slotAngleRad), Mathf.Sin(slotAngleRad)).normalized; }
            }
        }
        if (Vector2.Dot(perpendicularNormal, referenceDirectionForNormal) < 0) { perpendicularNormal = -perpendicularNormal; }

        Vector2 p1BasePoint = setP1BaseAtP0 ? p0 : p0p2Midpoint;
        float controlPointOffsetDistance = actualHeightFactor * distP0P2;
        return p1BasePoint + perpendicularNormal * controlPointOffsetDistance;
    }
    void SelectRandomTargetKeys()
    { /* 이전과 동일 */
        selectedTargetKeysForDrawing.Clear(); if (allAvailableKeys == null || allAvailableKeys.Count < REQUIRED_TARGET_KEYS) { Debug.LogError($"[SelectRandomTargetKeys] Random selection not possible: allAvailableKeys is null or has fewer than {REQUIRED_TARGET_KEYS} elements.", this); return; }
        List<Transform> validKeys = allAvailableKeys.Where(t => t != null).ToList(); if (validKeys.Count < REQUIRED_TARGET_KEYS) { Debug.LogError($"[SelectRandomTargetKeys] Random selection not possible: Fewer than {REQUIRED_TARGET_KEYS} non-null keys in allAvailableKeys.", this); return; }
        List<Transform> tempList = new List<Transform>(validKeys); for (int i = 0; i < REQUIRED_TARGET_KEYS; i++) { if (tempList.Count == 0) break; int randomIndex = UnityEngine.Random.Range(0, tempList.Count); selectedTargetKeysForDrawing.Add(tempList[randomIndex]); tempList.RemoveAt(randomIndex); }
    }
    Vector2 GetStartPoint(Vector2 center, CtrlSide side, float offsetDist)
    { /* 이전과 동일 */
        switch (side) { case CtrlSide.Left: return center + Vector2.left * offsetDist; case CtrlSide.Top: return center + Vector2.up * offsetDist; case CtrlSide.Right: return center + Vector2.right * offsetDist; case CtrlSide.Bottom: return center + Vector2.down * offsetDist; default: return center; }
    }
    void DrawQuadraticBezier(int colorIndex, Vector2 p0, Vector2 p1, Vector2 p2, string targetKeyName)
    { /* 이전과 동일 */
        GameObject lineObj = new GameObject("Curve_Quad_" + targetKeyName + "_" + colorIndex);
        lineObj.transform.parent = this.transform;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = curveResolution; lr.widthMultiplier = 0.1f; lr.sortingOrder = 1;
        float hue = colorIndex / (float)REQUIRED_TARGET_KEYS;
        Color colorVal = Color.HSVToRGB(hue, 1f, 1f);
        if (lineMaterialTemplate != null)
        {
            Material matInstance = new Material(lineMaterialTemplate);
            matInstance.color = colorVal; lr.material = matInstance;
        }
        lr.startColor = colorVal;
        lr.endColor = colorVal;
        ColorLaneInfo cli = lineObj.GetComponent<ColorLaneInfo>();
        if (cli == null)
            cli = lineObj.AddComponent<ColorLaneInfo>();

        cli.slotIndex = colorIndex;

        if (cli.positions == null)
            cli.positions = new List<Vector3>();
        else
            cli.positions.Clear();
        for (int k = 0; k < curveResolution; k++)
        {
            float t = k / (float)(curveResolution - 1);
            Vector2 pointOnCurve = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
            Vector3 point3D = new Vector3(pointOnCurve.x, pointOnCurve.y, 0f);
            lr.SetPosition(k, point3D);
            if (cli.positions != null)
                cli.positions.Add(point3D);
        }
    }
    void DrawCubicBezier(int colorIndex, Vector2 p0, Vector2 cp1, Vector2 cp2, Vector2 p3, string targetKeyName)
    { /* 이전과 동일 */
        GameObject lineObj = new GameObject("Curve_Cubic_" + targetKeyName + "_" + colorIndex);
        lineObj.transform.parent = this.transform;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.1f;
        lr.sortingOrder = 1;
        float hue = colorIndex / (float)REQUIRED_TARGET_KEYS;
        Color colorVal = Color.HSVToRGB(hue, 1f, 1f);
        if (lineMaterialTemplate != null)
        {
            Material matInstance = new Material(lineMaterialTemplate);
            matInstance.color = colorVal; lr.material = matInstance;
        }
        lr.startColor = colorVal;
        lr.endColor = colorVal;
        ColorLaneInfo cli = lineObj.GetComponent<ColorLaneInfo>();
        if (cli == null)
            cli = lineObj.AddComponent<ColorLaneInfo>();

        cli.slotIndex = colorIndex;

        if (cli.positions == null)
            cli.positions = new List<Vector3>();
        else
            cli.positions.Clear();
            
        for (int k = 0; k < curveResolution; k++)
        {
            float t = k / (float)(curveResolution - 1);
            float omt = 1f - t;
            float omt2 = omt * omt;
            float t2 = t * t;
            Vector2 pointOnCurve = omt * omt2 * p0 + 3f * omt2 * t * cp1 + 3f * omt * t2 * cp2 + t * t2 * p3;
            Vector3 point3D = new Vector3(pointOnCurve.x, pointOnCurve.y, 0f);
            lr.SetPosition(k, point3D);
            if (cli.positions != null)
                cli.positions.Add(point3D);
        }
    }

}

/*
// ColorLaneInfo.cs (별도 파일 또는 이 파일 하단에 위치)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ColorLaneInfo : MonoBehaviour {
    public List<Vector3> positions = new List<Vector3>();
    [HideInInspector] public LineRenderer lineRenderer;
    private Coroutine blinkCoroutine;
    private float initialWidthMultiplier = -1f; 
    void Awake() {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) { Debug.LogError($"[ColorLaneInfo] LineRenderer component not found on {gameObject.name}."); }
        else { initialWidthMultiplier = lineRenderer.widthMultiplier; }
        if (ColorLaneManager.Instance != null) { ColorLaneManager.Instance.RegisterLane(this); }
        else { Debug.LogWarning($"[ColorLaneInfo] ColorLaneManager.Instance not found during Awake for {gameObject.name}. Lane will not be registered.");}
    }
    void OnDestroy() {
        if (ColorLaneManager.Instance != null) { ColorLaneManager.Instance.UnregisterLane(this); }
        if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; }
    }
    public void Highlight(bool isOn) {
        if (lineRenderer == null) return;
        if (initialWidthMultiplier < 0 && lineRenderer != null) { initialWidthMultiplier = lineRenderer.widthMultiplier; }
        if (isOn) {
            if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); if (initialWidthMultiplier >= 0) lineRenderer.widthMultiplier = initialWidthMultiplier; }
            if (initialWidthMultiplier >= 0) { blinkCoroutine = StartCoroutine(BlinkLine()); }
        } else {
            if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; }
            if (initialWidthMultiplier >= 0) lineRenderer.widthMultiplier = initialWidthMultiplier;
        }
    }
    private IEnumerator BlinkLine() {
        if (initialWidthMultiplier < 0) yield break;
        for (int i = 0; i < 3; i++) {
            lineRenderer.widthMultiplier = initialWidthMultiplier * 2f; yield return new WaitForSeconds(0.2f);
            lineRenderer.widthMultiplier = initialWidthMultiplier; yield return new WaitForSeconds(0.2f);
        }
        blinkCoroutine = null;
    }
    public List<Vector3> GetWorldPoints() { return positions; }
}
*/