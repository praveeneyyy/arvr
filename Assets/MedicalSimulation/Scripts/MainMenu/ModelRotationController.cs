using UnityEngine;
using UnityEngine.EventSystems;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Interactive 3D Model Viewport Controller.
    /// Supports smooth orbit with inertia, scroll zoom, pan, double-click reset,
    /// and subtle idle turntable rotation when untouched.
    /// </summary>
    public class ModelRotationController : MonoBehaviour
    {
        [Header("Target & Camera References")]
        [SerializeField] private Transform targetModelRoot;
        [SerializeField] private Camera viewportCamera;

        [Header("Orbit Settings")]
        public float orbitSpeedX = 140f;
        public float orbitSpeedY = 120f;
        public float minPitch = -35f;
        public float maxPitch = 60f;
        public float orbitDamping = 8f;

        [Header("Zoom Settings")]
        public float zoomSpeed = 2.5f;
        public float minDistance = 0.5f;
        public float maxDistance = 4.5f;
        public float zoomDamping = 8f;

        [Header("Pan Settings")]
        public float panSpeed = 0.003f;

        [Header("Idle Turntable")]
        public bool enableIdleRotation = true;
        public float idleRotationSpeed = 8f;
        public float idleTimeout = 2.5f;

        private float _yaw = 0f;
        private float _pitch = 10f;
        private float _targetYaw = 0f;
        private float _targetPitch = 10f;
        private float _distance = 2.0f;
        private float _targetDistance = 2.0f;
        private Vector3 _panOffset = Vector3.zero;
        private Vector3 _targetPanOffset = Vector3.zero;

        private Vector3 _defaultPosition;
        private Quaternion _defaultRotation;
        private float _defaultFOV = 36f;

        private Vector3 _lastMousePos;
        private float _lastInteractionTime;
        private float _lastClickTime;
        private const float DoubleClickThreshold = 0.3f;

        void Awake()
        {
            if (viewportCamera == null) viewportCamera = Camera.main;
            if (viewportCamera != null)
            {
                _defaultPosition = viewportCamera.transform.position;
                _defaultRotation = viewportCamera.transform.rotation;
                _defaultFOV = viewportCamera.fieldOfView;
            }
            _lastInteractionTime = Time.time;
        }

        public void SetTarget(Transform modelRoot)
        {
            targetModelRoot = modelRoot;
            ResetView();
        }

        public void SetCameraFraming(Vector3 cameraPos, Vector3 lookTarget, float fov)
        {
            if (viewportCamera == null) viewportCamera = Camera.main;
            if (viewportCamera == null) return;

            _defaultPosition = cameraPos;
            _defaultFOV = fov;
            _panOffset = Vector3.zero;
            _targetPanOffset = Vector3.zero;

            Vector3 toCam = cameraPos - lookTarget;
            _targetDistance = toCam.magnitude;
            _distance = _targetDistance;

            _targetYaw = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
            _targetPitch = Mathf.Asin(toCam.y / Mathf.Max(0.001f, _distance)) * Mathf.Rad2Deg;

            _yaw = _targetYaw;
            _pitch = _targetPitch;

            viewportCamera.fieldOfView = fov;
            _lastInteractionTime = Time.time;
        }

        public void ResetView()
        {
            _targetPanOffset = Vector3.zero;
            _targetYaw = 0f;
            _targetPitch = 8f;
            _targetDistance = Mathf.Clamp(_distance, minDistance, maxDistance);
            _lastInteractionTime = Time.time;
        }

        void Update()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private void HandleInput()
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // Double Click to Reset
            if (Input.GetMouseButtonDown(0) && !isOverUI)
            {
                if (Time.time - _lastClickTime < DoubleClickThreshold)
                {
                    ResetView();
                }
                _lastClickTime = Time.time;
            }

            // Left Drag to Orbit
            if (Input.GetMouseButtonDown(0) && !isOverUI)
            {
                _lastMousePos = Input.mousePosition;
                _lastInteractionTime = Time.time;
            }
            else if (Input.GetMouseButton(0) && !isOverUI)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                _targetYaw += delta.x * orbitSpeedX * 0.01f;
                _targetPitch -= delta.y * orbitSpeedY * 0.01f;
                _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
                _lastMousePos = Input.mousePosition;
                _lastInteractionTime = Time.time;
            }

            // Middle Click or Shift + Right Drag to Pan
            if ((Input.GetMouseButton(2) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))) && !isOverUI)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                if (viewportCamera != null)
                {
                    Vector3 right = viewportCamera.transform.right;
                    Vector3 up = viewportCamera.transform.up;
                    _targetPanOffset -= (right * delta.x + up * delta.y) * panSpeed * (_distance * 0.5f);
                }
                _lastMousePos = Input.mousePosition;
                _lastInteractionTime = Time.time;
            }
            else if (Input.GetMouseButtonDown(2) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1)))
            {
                _lastMousePos = Input.mousePosition;
            }

            // Scroll Zoom
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !isOverUI)
            {
                _targetDistance -= scroll * zoomSpeed * 0.2f;
                _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
                _lastInteractionTime = Time.time;
            }

            // Idle Turntable when untouched
            if (enableIdleRotation && (Time.time - _lastInteractionTime > idleTimeout))
            {
                _targetYaw += idleRotationSpeed * Time.deltaTime;
            }
        }

        private void UpdateCameraTransform()
        {
            if (viewportCamera == null) return;

            float dt = Time.deltaTime;
            _yaw = Mathf.Lerp(_yaw, _targetYaw, dt * orbitDamping);
            _pitch = Mathf.Lerp(_pitch, _targetPitch, dt * orbitDamping);
            _distance = Mathf.Lerp(_distance, _targetDistance, dt * zoomDamping);
            _panOffset = Vector3.Lerp(_panOffset, _targetPanOffset, dt * orbitDamping);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 center = (targetModelRoot != null ? targetModelRoot.position : Vector3.zero) + _panOffset;
            Vector3 camPos = center - (rot * Vector3.forward * _distance);

            viewportCamera.transform.position = camPos;
            viewportCamera.transform.rotation = rot;
        }
    }
}
