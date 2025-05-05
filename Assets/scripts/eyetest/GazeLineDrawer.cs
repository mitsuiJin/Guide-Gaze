using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;

public class TobiiGazeLineDrawer : MonoBehaviour
{
    private IEyeTracker _eyeTracker;
    private Queue<GazeDataEventArgs> _gazeQueue = new Queue<GazeDataEventArgs>();
    private LineRenderer _lineRenderer;
    private List<Vector3> _gazePoints = new List<Vector3>();

    void Start()
    {
        // Eye Tracker 찾기
        var trackers = EyeTrackingOperations.FindAllEyeTrackers();
        if (trackers.Count == 0)
        {
            Debug.LogError("Tobii Eye Tracker를 찾을 수 없습니다.");
            return;
        }

        _eyeTracker = trackers[0];
        _eyeTracker.GazeDataReceived += OnGazeDataReceived;

        // LineRenderer 설정
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.widthMultiplier = 0.02f;
        _lineRenderer.startColor = Color.green;
        _lineRenderer.endColor = Color.red;
    }

    void Update()
    {
        while (_gazeQueue.Count > 0)
        {
            var gazeData = _gazeQueue.Dequeue();
            var point = gazeData.LeftEye.GazePoint.PositionOnDisplayArea;

            // 유효한 값인지 확인
            if (!double.IsNaN(point.X) && !double.IsNaN(point.Y))
            {
                float x = (float)point.X * Screen.width;
                float y = (float)point.Y * Screen.height;

                Vector3 screenPoint = new Vector3(x, y, 1f);  // z: 카메라 거리
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPoint);

                _gazePoints.Add(worldPoint);
                _lineRenderer.positionCount = _gazePoints.Count;
                _lineRenderer.SetPositions(_gazePoints.ToArray());
            }
        }
    }

    private void OnGazeDataReceived(object sender, GazeDataEventArgs e)
    {
        lock (_gazeQueue)
        {
            _gazeQueue.Enqueue(e);
        }
    }

    private void OnDestroy()
    {
        if (_eyeTracker != null)
        {
            _eyeTracker.GazeDataReceived -= OnGazeDataReceived;
        }
    }
}
