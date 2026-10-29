using UnityEngine;
using System.Collections.Generic;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Core;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MedicalSimulation.XR
{
    /// <summary>
    /// Handles AR surface plane detection, model placement, pinch-to-scale,
    /// touch-twist rotation, and AR touch organ selection.
    /// Includes seamless Editor/Desktop simulation fallback when no physical AR device is connected.
    /// </summary>
    public class ARMedicalSurfacePlacement : MonoBehaviour
    {
        [Header("Target Model to Place")]
        [SerializeField] private GameObject anatomicalModelPrefab;
        [SerializeField] private Transform placedModelInstance;

        [Header("Placement Reticle")]
        [SerializeField] private GameObject placementReticle;
        [SerializeField] private bool lockPlacementAfterFirstAnchor = true;

        [Header("Scaling & Manipulation Limits")]
        [SerializeField] private float minScale = 0.15f;
        [SerializeField] private float maxScale = 2.5f;
        [SerializeField] private float rotationSpeed = 0.4f;

        [Header("AR Foundation References (optional / auto-found)")]
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARPlaneManager arPlaneManager;

        private bool _isModelPlaced = false;
        private Pose _placementPose;
        private bool _placementPoseIsValid = false;
        private Camera _arCamera;

        // Touch tracking
        private float _initialPinchDistance;
        private Vector3 _initialScale;

        void Awake()
        {
            _arCamera = Camera.main;

            arRaycastManager ??= FindFirstObjectByType<ARRaycastManager>();
            arPlaneManager ??= FindFirstObjectByType<ARPlaneManager>();
        }

        void Update()
        {
            UpdatePlacementPose();
            UpdatePlacementReticle();

            // Handle touch interactions (Mobile AR)
            if (Input.touchCount > 0)
            {
                HandleTouchInteractions();
            }
            // Handle desktop / editor fallback
            else if (Application.isEditor || !Application.isMobilePlatform)
            {
                HandleDesktopFallback();
            }
        }

        private void UpdatePlacementPose()
        {
            _placementPoseIsValid = false;

            if (arRaycastManager != null)
            {
                var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                var hits = new List<ARRaycastHit>();
                if (arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
                {
                    _placementPoseIsValid = true;
                    _placementPose = hits[0].pose;

                    // Face towards camera
                    var cameraForward = _arCamera.transform.forward;
                    var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                    _placementPose.rotation = Quaternion.LookRotation(cameraBearing);
                    return;
                }
            }

            // Editor / Desktop Fallback: Physics raycast against ground plane
            var ray = _arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                _placementPoseIsValid = true;
                _placementPose.position = hit.point;
                var cameraForward = _arCamera.transform.forward;
                var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                _placementPose.rotation = Quaternion.LookRotation(cameraBearing);
            }
        }

        private void UpdatePlacementReticle()
        {
            if (placementReticle == null) return;

            if (_placementPoseIsValid && (!_isModelPlaced || !lockPlacementAfterFirstAnchor))
            {
                placementReticle.SetActive(true);
                placementReticle.transform.SetPositionAndRotation(_placementPose.position, _placementPose.rotation);
            }
            else
            {
                placementReticle.SetActive(false);
            }
        }

        private void HandleTouchInteractions()
        {
            // 1 Finger: Place model or select organ
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    // Check if an organ was tapped
                    Ray ray = _arCamera.ScreenPointToRay(touch.position);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        var organ = hit.transform.GetComponent<OrganInteractable>();
                        if (organ != null)
                        {
                            organ.Select();
                            return;
                        }
                    }

                    // Otherwise, place / anchor the model if reticle valid
                    if ((!_isModelPlaced || !lockPlacementAfterFirstAnchor) && _placementPoseIsValid)
                    {
                        PlaceModel(_placementPose.position, _placementPose.rotation);
                    }
                }
                else if (touch.phase == TouchPhase.Moved && _isModelPlaced)
                {
                    // Single finger drag to rotate
                    if (placedModelInstance != null)
                    {
                        placedModelInstance.Rotate(Vector3.up, -touch.deltaPosition.x * rotationSpeed, Space.World);
                    }
                }
            }
            // 2 Fingers: Pinch to scale
            else if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
                {
                    _initialPinchDistance = Vector2.Distance(t0.position, t1.position);
                    if (placedModelInstance != null) _initialScale = placedModelInstance.localScale;
                }
                else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
                {
                    float currentDistance = Vector2.Distance(t0.position, t1.position);
                    if (_initialPinchDistance > 0.01f && placedModelInstance != null)
                    {
                        float scaleFactor = currentDistance / _initialPinchDistance;
                        Vector3 targetScale = _initialScale * scaleFactor;
                        float clampedX = Mathf.Clamp(targetScale.x, minScale, maxScale);
                        placedModelInstance.localScale = new Vector3(clampedX, clampedX, clampedX);
                    }
                }
            }
        }

        private void HandleDesktopFallback()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _arCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var organ = hit.transform.GetComponent<OrganInteractable>();
                    if (organ != null)
                    {
                        organ.Select();
                        return;
                    }
                }

                if ((!_isModelPlaced || !lockPlacementAfterFirstAnchor) && _placementPoseIsValid)
                {
                    PlaceModel(_placementPose.position, _placementPose.rotation);
                }
            }

            // Desktop scrollwheel to scale
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f && placedModelInstance != null)
            {
                float newScale = Mathf.Clamp(placedModelInstance.localScale.x + scroll * 0.5f, minScale, maxScale);
                placedModelInstance.localScale = Vector3.one * newScale;
            }
        }

        public void PlaceModel(Vector3 position, Quaternion rotation)
        {
            if (placedModelInstance == null)
            {
                if (anatomicalModelPrefab != null)
                {
                    placedModelInstance = Instantiate(anatomicalModelPrefab, position, rotation).transform;
                }
                else
                {
                    // Instantiate an AnatomyAssemblyBuilder directly
                    var go = new GameObject("AR_AnatomicalTorso");
                    go.transform.SetPositionAndRotation(position, rotation);
                    go.AddComponent<AnatomyAssemblyBuilder>();
                    placedModelInstance = go.transform;
                }
            }
            else
            {
                placedModelInstance.SetPositionAndRotation(position, rotation);
            }

            _isModelPlaced = true;
            Debug.Log($"[ARMedicalSurfacePlacement] Anatomical model placed at: {position}");
        }

        public void ResetPlacement()
        {
            _isModelPlaced = false;
            if (placedModelInstance != null)
            {
                placedModelInstance.position = new Vector3(0, -100f, 0); // Hide until re-placed
            }
        }

        public void SetLockPlacement(bool locked)
        {
            lockPlacementAfterFirstAnchor = locked;
        }
    }
}
