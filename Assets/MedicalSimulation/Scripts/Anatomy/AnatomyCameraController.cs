using UnityEngine;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Smooth orbit / zoom / pan camera for the anatomy explorer.
    /// Supports desktop mouse+keyboard and basic XR focus-on requests.
    /// </summary>
    public class AnatomyCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform pivot;           // orbit centre
        [SerializeField] private Camera    cam;

        [Header("Orbit")]
        [SerializeField] private float orbitSpeed    = 200f;
        [SerializeField] private float minPitch      = -80f;
        [SerializeField] private float maxPitch      =  80f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed     = 5f;
        [SerializeField] private float minDistance   = 0.3f;
        [SerializeField] private float maxDistance   = 8f;

        [Header("Pan")]
        [SerializeField] private float panSpeed      = 0.3f;

        [Header("Smooth")]
        [SerializeField] private float smoothTime    = 0.12f;

        // Default transform values (used for Reset)
        private float   _yaw   = 0f;
        private float   _pitch = 20f;
        private float   _dist  = 3f;
        private Vector3 _panOffset = Vector3.zero;

        private float   _targetYaw;
        private float   _targetPitch;
        private float   _targetDist;
        private Vector3 _targetPanOffset;

        private float   _yawVel, _pitchVel, _distVel;
        private Vector3 _panVel;

        private bool _isDragging;
        private bool _isPanning;
        private Vector3 _lastMousePos;

        void Awake()
        {
            if (cam  == null) cam  = GetComponentInChildren<Camera>();
            if (pivot == null) pivot = transform;

            _targetYaw       = _yaw;
            _targetPitch     = _pitch;
            _targetDist      = _dist;
            _targetPanOffset = _panOffset;
        }

        void Update()
        {
            HandleMouseInput();
            SmoothApply();
        }

        private void HandleMouseInput()
        {
            // Right-drag → orbit
            if (Input.GetMouseButtonDown(1)) { _isDragging = true; _lastMousePos = Input.mousePosition; }
            if (Input.GetMouseButtonUp(1))     _isDragging = false;

            // Middle-drag → pan
            if (Input.GetMouseButtonDown(2)) { _isPanning = true; _lastMousePos = Input.mousePosition; }
            if (Input.GetMouseButtonUp(2))     _isPanning = false;

            if (_isDragging)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                _targetYaw   += delta.x * orbitSpeed * Time.deltaTime;
                _targetPitch -= delta.y * orbitSpeed * Time.deltaTime;
                _targetPitch  = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
                _lastMousePos = Input.mousePosition;
            }

            if (_isPanning)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                Vector3 right = cam.transform.right;
                Vector3 up    = cam.transform.up;
                _targetPanOffset -= (right * delta.x + up * delta.y) * panSpeed * Time.deltaTime;
                _lastMousePos = Input.mousePosition;
            }

            // Scroll → zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _targetDist = Mathf.Clamp(_targetDist - scroll * zoomSpeed, minDistance, maxDistance);
        }

        private void SmoothApply()
        {
            _yaw       = Mathf.SmoothDampAngle(_yaw,       _targetYaw,       ref _yawVel,   smoothTime);
            _pitch     = Mathf.SmoothDampAngle(_pitch,     _targetPitch,     ref _pitchVel, smoothTime);
            _dist      = Mathf.SmoothDamp     (_dist,      _targetDist,      ref _distVel,  smoothTime);
            _panOffset = Vector3.SmoothDamp   (_panOffset, _targetPanOffset, ref _panVel,   smoothTime);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 pos    = pivot.position + _panOffset + rot * new Vector3(0, 0, -_dist);

            transform.position = pos;
            transform.LookAt(pivot.position + _panOffset);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void FocusOn(Transform target)
        {
            if (target == null) return;
            _targetPanOffset = target.position - pivot.position;
            _targetDist      = Mathf.Clamp(_dist * 0.6f, minDistance, maxDistance);
        }

        public void ResetView()
        {
            _targetYaw       = 0f;
            _targetPitch     = 20f;
            _targetDist      = 3f;
            _targetPanOffset = Vector3.zero;
        }

        public void ResetCamera() => ResetView();

        public void SetOrbitTarget(Vector3 worldPosition)
        {
            if (pivot != null) pivot.position = worldPosition;
        }
    }
}
