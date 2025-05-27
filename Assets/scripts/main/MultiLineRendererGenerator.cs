// MultiLineRendererGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class MultiLineRendererGenerator : MonoBehaviour
{
    public static event Action OnLanesRegenerated;

    [Header("References")]
    public Transform ctrlKeyCenter;
    public List<Transform> allAvailableKeys;
    public Material lineMaterialTemplate;

    [Header("Curve Base Settings")]
    public int curveResolution = 20;
    public float startOffset = 0.3f;

    [Header("Curve Style (P0 Side Based)")]
    [Tooltip("P0가 Top 또는 Right일 때 (안쪽 선) 적용될 높이 계수")]
    public float innerCurveHeightFactor = 0.25f; // 이전: innerCurveHeightFactor
    [Tooltip("P0가 Left 또는 Bottom일 때 (바깥쪽 선) 적용될 높이 계수")]
    public float outerCurveHeightFactor = 0.5f; // 이전: outerCurveHeightFactor

    [Header("P1 Squeeze Heuristic")]
    [Tooltip("인접 키와 각도 차이가 이 값 미만이면 heightFactor 감소 시작")]
    public float neighborSqueezeAngleThreshold = 75f;
    [Tooltip("압착 시 heightFactor 최대 감소율 (1.0=감소없음, 0.5=절반)")]
    [Range(0.3f, 1.0f)]
    public float neighborSqueezeHeightFactorMultiplier = 0.6f;
    [Tooltip("모든 heightFactor가 Clamp될 최소/최대값")]
    public Vector2 minMaxHeightFactorClamp = new Vector2(0.1f, 0.7f);

    [Header("Target Keys (Optional)")]
    public List<Transform> targetKeys;

    [Header("Controls")]
    public KeyCode regenerateKey = KeyCode.R;

    private const int REQUIRED_TARGET_KEYS = 4;
    private enum CtrlSide { Right, Top, Left, Bottom }

    // P0 슬롯의 기준 각도 (Right=0, Top=90, Left=180, Bottom=270 degrees)
    private static readonly Dictionary<CtrlSide, float> P0_SLOT_ANGLES_DEG = new Dictionary<CtrlSide, float>
    {
        { CtrlSide.Right, 0f }, { CtrlSide.Top, 90f }, { CtrlSide.Left, 180f }, { CtrlSide.Bottom, 270f }
    };

    // 내부 처리용 데이터 구조
    private class LaneGenerationData
    {
        public int initialSortIndex; // 초기 각도 정렬 인덱스 (색상 등 일관성 유지용)
        public Transform targetKey;
        public Vector2 P2_endPoint;
        public float actualAngleRad;    // 키의 실제 각도 (ctrlKeyCenter 기준)
        public CtrlSide assignedP0Side; // 이 레인에 할당된 P0 오프셋 방향 (Right, Top, Left, Bottom)
        public Vector2 P0_startPoint;
        public Vector2 P1_controlPoint; // 최종 계산된 P1
        public float finalHeightFactor;  // 최종 적용된 heightFactor
    }

    private List<Transform> selectedTargetKeysForDrawing = new List<Transform>();

    void Start() { ProcessLaneGeneration(false); }
    void Update() { if (Input.GetKeyDown(regenerateKey)) ProcessLaneGeneration(true); }

    void ProcessLaneGeneration(bool forceRandom)
    {
        // 1. 기존 라인 삭제 및 선택된 키 초기화
        foreach (Transform child in transform) if (child.gameObject.name.StartsWith("Curve_")) Destroy(child.gameObject);
        selectedTargetKeysForDrawing.Clear();

        // 2. 대상 키 선택
        bool useInspector = !forceRandom && targetKeys != null && targetKeys.Count == REQUIRED_TARGET_KEYS;
        if (useInspector) selectedTargetKeysForDrawing.AddRange(targetKeys);
        else SelectRandomTargetKeys();

        // 3. 유효성 검사
        if (selectedTargetKeysForDrawing.Count != REQUIRED_TARGET_KEYS) { Debug.LogError($"[MLRG] 대상 키 {REQUIRED_TARGET_KEYS}개 필요 (현재: {selectedTargetKeysForDrawing.Count})."); OnLanesRegenerated?.Invoke(); return; }
        if (ctrlKeyCenter == null) { Debug.LogError("[MLRG] ctrlKeyCenter 할당 필요."); OnLanesRegenerated?.Invoke(); return; }

        Vector2 ctrlPos = ctrlKeyCenter.position;

        // 4. 모든 키의 기본 정보 (실제 각도 포함) 계산 및 각도순 정렬 (P1 휴리스틱 및 색상 할당에 사용)
        var initialKeyInfosSortedByAngle = selectedTargetKeysForDrawing
            .Select((t, index) => {
                if (t == null) return null;
                Vector2 p = t.position; Vector2 d = p - ctrlPos; float a = Mathf.Atan2(d.y, d.x); if (a < 0) a += 2 * Mathf.PI; // 0 ~ 2PI
                return new { transform = t, worldPos = p, angleRad = a, originalSelectionOrder = index };
            })
            .Where(k => k != null && k.transform != null).OrderBy(k => k.angleRad).ToList();

        if (initialKeyInfosSortedByAngle.Count != REQUIRED_TARGET_KEYS) { Debug.LogError($"[MLRG] 정렬 후 유효 키 부족."); OnLanesRegenerated?.Invoke(); return; }

        // 5. P0 시작점 할당 개선: 각 키의 실제 각도와 가장 유사한 P0 슬롯(R,T,L,B)에 중복 없이 배정
        List<LaneGenerationData> laneGenDataList = new List<LaneGenerationData>();
        bool[] p0SlotIsUsed = new bool[4]; // Right, Top, Left, Bottom 순서대로 사용 여부 마킹
        List<CtrlSide> p0SlotOrderReference = new List<CtrlSide> { CtrlSide.Right, CtrlSide.Top, CtrlSide.Left, CtrlSide.Bottom };

        // 각도순으로 정렬된 키들에 대해 P0 슬롯 할당
        foreach (var keyInfo in initialKeyInfosSortedByAngle)
        {
            float bestAngularDifference = float.MaxValue;
            CtrlSide bestMatchingSlot = p0SlotOrderReference[0]; // 기본값
            int bestSlotInternalIndex = -1;

            // 사용 가능한 P0 슬롯 중에서 현재 키의 각도와 가장 유사한 슬롯 찾기
            for (int slotIndex = 0; slotIndex < p0SlotOrderReference.Count; slotIndex++)
            {
                if (!p0SlotIsUsed[slotIndex]) // 아직 사용되지 않은 슬롯만 고려
                {
                    CtrlSide currentCandidateSlot = p0SlotOrderReference[slotIndex];
                    float keyAngleDegrees = keyInfo.angleRad * Mathf.Rad2Deg;
                    float slotNominalAngleDegrees = P0_SLOT_ANGLES_DEG[currentCandidateSlot];
                    float angularDiff = Mathf.Abs(Mathf.DeltaAngle(keyAngleDegrees, slotNominalAngleDegrees));

                    if (angularDiff < bestAngularDifference)
                    {
                        bestAngularDifference = angularDiff;
                        bestMatchingSlot = currentCandidateSlot;
                        bestSlotInternalIndex = slotIndex;
                    }
                }
            }

            if (bestSlotInternalIndex != -1) p0SlotIsUsed[bestSlotInternalIndex] = true; // 선택된 슬롯 사용됨으로 마킹

            laneGenDataList.Add(new LaneGenerationData
            {
                initialSortIndex = initialKeyInfosSortedByAngle.IndexOf(keyInfo),
                targetKey = keyInfo.transform,
                P2_endPoint = keyInfo.worldPos,
                actualAngleRad = keyInfo.angleRad,
                assignedP0Side = bestMatchingSlot, // 계산된 최적의 P0 Side 할당
                P0_startPoint = GetStartPoint(ctrlPos, bestMatchingSlot, startOffset)
            });
        }
        // 이제 laneGenDataList는 각 키에 대해 P0 정보가 할당된 상태. initialKeyInfosSortedByAngle 순서를 따름.

        // 6. P1 제어점 계산 (새로운 "안쪽/바깥쪽" 스타일 규칙 및 "압착" 휴리스틱 적용)
        for (int i = 0; i < laneGenDataList.Count; i++)
        {
            LaneGenerationData currentLane = laneGenDataList[i];

            // 6a. 기본 heightFactor 결정 (할당된 P0 Side 기반)
            float baseHeightFactor;
            if (currentLane.assignedP0Side == CtrlSide.Top || currentLane.assignedP0Side == CtrlSide.Right)
            {
                baseHeightFactor = this.innerCurveHeightFactor; // Top 또는 Right에서 시작하면 "안쪽 선" 스타일
            }
            else // Left 또는 Bottom에서 시작
            {
                baseHeightFactor = this.outerCurveHeightFactor; // Left 또는 Bottom에서 시작하면 "바깥쪽 선" 스타일
            }

            // 6b. P1 휴리스틱: 인접 키와의 "압착" 정도에 따라 heightFactor 추가 조정
            float squeezeMultiplier = 1.0f;
            // initialKeyInfosSortedByAngle는 각도순 정렬되어 있으므로, 이를 기준으로 이전/다음 키를 찾음
            // currentLane.initialSortIndex는 initialKeyInfosSortedByAngle에서의 인덱스임
            var prevKeyAngleInfo = initialKeyInfosSortedByAngle[(currentLane.initialSortIndex - 1 + REQUIRED_TARGET_KEYS) % REQUIRED_TARGET_KEYS];
            var nextKeyAngleInfo = initialKeyInfosSortedByAngle[(currentLane.initialSortIndex + 1) % REQUIRED_TARGET_KEYS];

            float angleToPrevDeg = Mathf.Abs(Mathf.DeltaAngle(currentLane.actualAngleRad * Mathf.Rad2Deg, prevKeyAngleInfo.angleRad * Mathf.Rad2Deg));
            float angleToNextDeg = Mathf.Abs(Mathf.DeltaAngle(currentLane.actualAngleRad * Mathf.Rad2Deg, nextKeyAngleInfo.angleRad * Mathf.Rad2Deg));
            float minNeighborAngleSep = Mathf.Min(angleToPrevDeg, angleToNextDeg);

            if (minNeighborAngleSep < this.neighborSqueezeAngleThreshold)
            {
                squeezeMultiplier = Mathf.Lerp(this.neighborSqueezeHeightFactorMultiplier, 1.0f, minNeighborAngleSep / this.neighborSqueezeAngleThreshold);
            }

            currentLane.finalHeightFactor = baseHeightFactor * squeezeMultiplier;
            currentLane.finalHeightFactor = Mathf.Clamp(currentLane.finalHeightFactor, minMaxHeightFactorClamp.x, minMaxHeightFactorClamp.y);

            // 6c. 최종 P1 계산
            currentLane.P1_controlPoint = CalculateP1ForLane(
                currentLane.P0_startPoint, currentLane.P2_endPoint, ctrlPos,
                currentLane.assignedP0Side, currentLane.finalHeightFactor, startOffset
            );
        }

        // 7. 최종 곡선 그리기
        foreach (var laneData in laneGenDataList)
        {
            // 색상 인덱스는 initialSortIndex (초기 각도 정렬 순서)를 사용
            DrawQuadraticBezier(laneData.initialSortIndex, laneData.P0_startPoint, laneData.P1_controlPoint, laneData.P2_endPoint, laneData.targetKey.name);
        }

        OnLanesRegenerated?.Invoke();
    }

    // P1 계산 함수 (이전과 거의 동일, startOffsetForMidpointCheck 파라미터 전달)
    Vector2 CalculateP1ForLane(Vector2 p0, Vector2 p2, Vector2 ctrlPos, CtrlSide assignedSideForP0, float actualHeightFactor, float startOffsetForMidpointCheck)
    {
        Vector2 p0p2Midpoint = (p0 + p2) * 0.5f;
        Vector2 vecP0ToP2 = p2 - p0;
        float distP0P2 = vecP0ToP2.magnitude;

        if (distP0P2 < Mathf.Epsilon) return p0p2Midpoint;

        Vector2 perpendicularNormal = new Vector2(-vecP0ToP2.y, vecP0ToP2.x).normalized;
        Vector2 vecCtrlToP0P2Midpoint = p0p2Midpoint - ctrlPos;

        // P0P2 중간점이 컨트롤 키 중심과 매우 가까울 때의 법선 방향 결정 로직
        if (vecCtrlToP0P2Midpoint.sqrMagnitude < (startOffsetForMidpointCheck * 0.01f) * (startOffsetForMidpointCheck * 0.01f))
        {
            Vector2 sideBasedOutwardDirection;
            // 할당된 P0 슬롯의 정규화된 방향 벡터 사용
            float slotAngleRad = P0_SLOT_ANGLES_DEG[assignedSideForP0] * Mathf.Deg2Rad;
            sideBasedOutwardDirection = new Vector2(Mathf.Cos(slotAngleRad), Mathf.Sin(slotAngleRad)).normalized;
            // perpendicularNormal이 이 방향과 같은 쪽을 가리키도록 조정
            if (Vector2.Dot(perpendicularNormal, sideBasedOutwardDirection) < 0) perpendicularNormal = -perpendicularNormal;
        }
        else
        {
            // 일반적인 경우, P0P2 중간점이 컨트롤 키 중심에서 멀리 있을 때
            if (Vector2.Dot(perpendicularNormal, vecCtrlToP0P2Midpoint) < 0) perpendicularNormal = -perpendicularNormal;
        }

        float controlPointOffsetDistance = actualHeightFactor * distP0P2;
        return p0p2Midpoint + perpendicularNormal * controlPointOffsetDistance;
    }

    void SelectRandomTargetKeys()
    {
        selectedTargetKeysForDrawing.Clear();
        if (allAvailableKeys == null || allAvailableKeys.Count < REQUIRED_TARGET_KEYS)
        {
            Debug.LogError($"[SelectRandomTargetKeys] 랜덤 선택 불가: allAvailableKeys가 null이거나 {REQUIRED_TARGET_KEYS}개 미만입니다. (현재 {(allAvailableKeys != null ? allAvailableKeys.Count : 0)}개). Inspector의 allAvailableKeys를 확인하세요.");
            return;
        }
        List<Transform> validKeys = allAvailableKeys.Where(t => t != null).ToList();
        if (validKeys.Count < REQUIRED_TARGET_KEYS)
        {
            Debug.LogError($"[SelectRandomTargetKeys] 랜덤 선택 불가: 유효한 (null이 아닌) 키가 allAvailableKeys에 {REQUIRED_TARGET_KEYS}개 미만입니다. (현재 {validKeys.Count}개).");
            return;
        }
        List<Transform> tempList = new List<Transform>(validKeys);
        for (int i = 0; i < REQUIRED_TARGET_KEYS; i++)
        {
            if (tempList.Count == 0) break;
            int randomIndex = UnityEngine.Random.Range(0, tempList.Count);
            selectedTargetKeysForDrawing.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }
    }

    Vector2 GetStartPoint(Vector2 center, CtrlSide side, float offsetDistance)
    {
        switch (side)
        {
            case CtrlSide.Left: return center + Vector2.left * offsetDistance;
            case CtrlSide.Top: return center + Vector2.up * offsetDistance;
            case CtrlSide.Right: return center + Vector2.right * offsetDistance;
            case CtrlSide.Bottom: return center + Vector2.down * offsetDistance;
            default: return center;
        }
    }

    void DrawQuadraticBezier(int colorIndex, Vector2 start, Vector2 control, Vector2 end, string targetKeyName)
    {
        GameObject lineObj = new GameObject("Curve_" + targetKeyName + "_" + colorIndex);
        lineObj.transform.parent = this.transform;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = curveResolution;
        lr.widthMultiplier = 0.05f; // 기본 선 두께
        lr.sortingOrder = 1;

        if (lineMaterialTemplate != null)
        {
            Material matInstance = new Material(lineMaterialTemplate);
            Color color = Color.HSVToRGB(colorIndex / (float)REQUIRED_TARGET_KEYS, 1f, 1f);
            matInstance.color = color;
            lr.material = matInstance;
            lr.startColor = color;
            lr.endColor = color;
        }
        else
        {
            Color color = Color.HSVToRGB(colorIndex / (float)REQUIRED_TARGET_KEYS, 1f, 1f);
            lr.startColor = color;
            lr.endColor = color;
        }

        ColorLaneInfo cli = lineObj.AddComponent<ColorLaneInfo>();
        // cli.positions는 ColorLaneInfo.Awake에서 new List<Vector3>()로 초기화 되므로 Clear 불필요
        // 또는 ColorLaneInfo에서 public List<Vector3> positions = new List<Vector3>(); 로 선언 시 바로 사용 가능

        for (int k = 0; k < curveResolution; k++) // 루프 변수 i 대신 k 사용 (바깥 루프와 구분)
        {
            float t = k / (float)(curveResolution - 1);
            Vector2 pointOnCurve = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
            Vector3 point3D = new Vector3(pointOnCurve.x, pointOnCurve.y, 0f);
            lr.SetPosition(k, point3D);
            cli.positions.Add(point3D);
        }
    }
}