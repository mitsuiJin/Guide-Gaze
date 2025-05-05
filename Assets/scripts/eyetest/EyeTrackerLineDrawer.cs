using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;

public class EyeTrackerLineDrawer : MonoBehaviour
{
    private IEyeTracker eyeTracker;
    private Queue<Vector2> gazeQueue = new Queue<Vector2>();
    private LineRenderer lineRenderer;
    private List<Vector3> worldPositions = new List<Vector3>();

    IEnumerator Start()
    {
        var trackers = EyeTrackingOperations.FindAllEyeTrackers();
        if (trackers.Count == 0)
        {
            Debug.LogError("❌ Eye Tracker 연결 안됨");
            yield break;
        }

        eyeTracker = trackers[0];
        Debug.Log("🔵 Eye Tracker 연결됨: " + eyeTracker.SerialNumber);

        // LineRenderer 설정
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = Color.white;
        lineRenderer.widthMultiplier = 0.05f;
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        lineRenderer.positionCount = 0;

        // Gaze 이벤트 연결
        eyeTracker.GazeDataReceived += OnGazeDataReceived;

        yield return new WaitForSeconds(30); // 필요 시 시간 제거 가능

        eyeTracker.GazeDataReceived -= OnGazeDataReceived;
    }

    private void OnGazeDataReceived(object sender, GazeDataEventArgs e)
    {
        Debug.Log("📡 GazeDataReceived 호출됨");

        var gazePoint = e.LeftEye.GazePoint;

        Debug.Log($"👁️ Gaze Validity: {gazePoint.Validity}");

        if (gazePoint.Validity == Validity.Valid)
        {
            var normPos = gazePoint.PositionOnDisplayArea;
            Debug.Log($"✅ Gaze 좌표: {normPos.X}, {normPos.Y}");

            lock (gazeQueue)
            {
                gazeQueue.Enqueue(new Vector2((float)normPos.X, (float)normPos.Y));
            }
        }
        else
        {
            Debug.Log("❌ Gaze 데이터 유효하지 않음");
        }
    }

    private void Update()
    {
        Debug.Log("🟡 Update 실행 중, Queue 크기: " + gazeQueue.Count);

        while (gazeQueue.Count > 0)
        {
            Vector2 gaze = gazeQueue.Dequeue();

            float x = gaze.x * Screen.width;
            float y = gaze.y * Screen.height;

            Vector3 screenPoint = new Vector3(x, y, 5f); // 카메라 앞쪽
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPoint);

            Debug.Log($"📌 world pos: {worldPoint}");

            worldPositions.Add(worldPoint);
            lineRenderer.positionCount = worldPositions.Count;
            lineRenderer.SetPositions(worldPositions.ToArray());
        }
    }

    private void OnDestroy()
    {
        if (eyeTracker != null)
        {
            eyeTracker.GazeDataReceived -= OnGazeDataReceived;
        }
    }
}
