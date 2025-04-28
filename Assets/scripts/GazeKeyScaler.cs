using UnityEngine;
using Tobii.Research.Unity;

public class GazeKeyScaler : MonoBehaviour
{
    public float gazeScale = 0.8f;          // 응시 시 줄어들 크기
    public float returnSpeed = 5f;          // 원래 크기로 돌아오는 속도
    private Vector3 originalScale;          // 원래 크기 저장
    private bool isGazed = false;           // 현재 응시 상태 여부

    private EyeTracker eyeTracker;          // EyeTracker 객체

    void Start()
    {
        originalScale = transform.localScale;  // 객체의 원래 크기 저장
        eyeTracker = EyeTracker.Instance;      // EyeTracker 인스턴스 가져오기

        if (eyeTracker == null)
        {
            Debug.LogError("EyeTracker instance is not found.");
        }
    }

    void Update()
    {
        if (eyeTracker == null) return;  // EyeTracker가 없으면 처리 중단

        // GazeData 데이터를 가져오기
        var gazeData = eyeTracker.LatestGazeData;

        if (gazeData != null)
        {
            // 시선 좌표 추출 (좌측 눈 데이터 사용)
            float gazeX = gazeData.Left.GazePointOnDisplayArea.x;
            float gazeY = gazeData.Left.GazePointOnDisplayArea.y;

            // 디버그: 시선 좌표 확인
            Debug.Log($"Gaze Coordinates: X = {gazeX}, Y = {gazeY}");

            // 시선 좌표를 화면 좌표로 변환
            Vector3 screenPos = new Vector3(gazeX * Screen.width, (1 - gazeY) * Screen.height, 0);
            Ray ray = Camera.main.ScreenPointToRay(screenPos);  // 시선의 레이 캐스트 생성
            RaycastHit hit;

            // 레이가 이 오브젝트를 가리키는지 확인
            if (Physics.Raycast(ray, out hit))
            {
                isGazed = hit.transform == transform;
                // 디버그: Raycast가 정확히 오브젝트와 충돌했는지 확인
                Debug.Log($"Raycast Hit: {hit.transform.name}");
            }
            else
            {
                isGazed = false;
                // 디버그: Raycast가 오브젝트에 충돌하지 않았을 경우
                Debug.Log("Raycast did not hit the object.");
            }
        }
        else
        {
            isGazed = false;  // 유효하지 않은 시선 데이터일 경우
        }

        // 크기 조정
        Vector3 targetScale = isGazed ? originalScale * gazeScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);
    }
}
