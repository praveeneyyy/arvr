using UnityEngine;
using System.Collections.Generic;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Controls multi-planar anatomical cross-section visualization.
    /// Simulates educational CT/MRI planar slicing along Axial, Sagittal, and Coronal axes
    /// using progressive slice transparency and visual plane guides.
    /// Note: Labeled as an educational anatomical slice view, not diagnostic imaging.
    /// </summary>
    public class AnatomicalSlicePlaneController : MonoBehaviour
    {
        public enum SlicePlaneAxis { Axial, Sagittal, Coronal }

        [Header("Slice Plane Configuration")]
        [SerializeField] private SlicePlaneAxis currentAxis = SlicePlaneAxis.Axial;
        [Range(-1.0f, 1.0f)]
        [SerializeField] private float slicePosition = 0.0f;
        [SerializeField] private float sliceThickness = 0.35f;

        [Header("Visual Plane Indicator")]
        [SerializeField] private GameObject sliceIndicatorPlane;
        [SerializeField] private bool showIndicatorPlane = true;

        [Header("Target Model (auto-found if null)")]
        [SerializeField] private AnatomyModelController anatomyController;

        private List<OrganInteractable> _organs = new List<OrganInteractable>();
        private bool _isSlicingActive = false;

        void Start()
        {
            anatomyController ??= FindFirstObjectByType<AnatomyModelController>();
            if (anatomyController != null)
            {
                _organs = anatomyController.GetAllOrgans();
            }

            if (sliceIndicatorPlane == null)
            {
                CreateIndicatorPlane();
            }

            UpdateSlicePlane();
        }

        void Update()
        {
            if (_isSlicingActive)
            {
                UpdateSlicePlane();
            }
        }

        public void SetSlicingActive(bool active)
        {
            _isSlicingActive = active;
            if (sliceIndicatorPlane != null)
            {
                sliceIndicatorPlane.SetActive(active && showIndicatorPlane);
            }

            if (!active)
            {
                // Restore all organs to full opacity
                anatomyController?.ShowAllSystems();
            }
            else
            {
                UpdateSlicePlane();
            }
        }

        public void SetSliceAxis(SlicePlaneAxis axis)
        {
            currentAxis = axis;
            UpdateSlicePlane();
        }

        public void SetSlicePosition(float normalizedPosition)
        {
            slicePosition = Mathf.Clamp(normalizedPosition, -1.0f, 1.0f);
            UpdateSlicePlane();
        }

        public void SetSliceThickness(float thickness)
        {
            sliceThickness = Mathf.Max(0.05f, thickness);
            UpdateSlicePlane();
        }

        private void UpdateSlicePlane()
        {
            if (anatomyController == null) return;
            if (_organs.Count == 0) _organs = anatomyController.GetAllOrgans();

            Vector3 planeNormal = currentAxis switch
            {
                SlicePlaneAxis.Axial => Vector3.up,       // Superior-Inferior (Horizontal)
                SlicePlaneAxis.Sagittal => Vector3.right,  // Left-Right
                SlicePlaneAxis.Coronal => Vector3.forward, // Anterior-Posterior
                _ => Vector3.up
            };

            Vector3 planeCenter = transform.position + (planeNormal * slicePosition);

            // Update Indicator Plane
            if (sliceIndicatorPlane != null)
            {
                sliceIndicatorPlane.transform.position = planeCenter;
                sliceIndicatorPlane.transform.up = planeNormal;
            }

            // Adjust transparency of organs according to their distance from the slice plane
            foreach (var organ in _organs)
            {
                if (organ == null) continue;

                Vector3 toOrgan = organ.transform.position - planeCenter;
                float planeDistance = Mathf.Abs(Vector3.Dot(toOrgan, planeNormal));

                if (planeDistance <= sliceThickness)
                {
                    // In-plane focus: high opacity
                    float t = 1.0f - (planeDistance / sliceThickness);
                    organ.SetTransparency(Mathf.Lerp(0.35f, 1.0f, t));
                }
                else
                {
                    // Distant from current slice: subtle ghosting
                    organ.SetTransparency(0.04f);
                }
            }
        }

        private void CreateIndicatorPlane()
        {
            sliceIndicatorPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sliceIndicatorPlane.name = "MedicalSliceIndicator_Plane";
            sliceIndicatorPlane.transform.SetParent(transform, false);
            sliceIndicatorPlane.transform.localScale = Vector3.one * 1.5f;

            // Destroy collider so it doesn't intercept clicks
            var col = sliceIndicatorPlane.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = sliceIndicatorPlane.GetComponent<Renderer>();
            var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(s);
            mat.color = new Color(0.0f, 0.9f, 1.0f, 0.18f);

            // Enable transparency
            mat.SetFloat("_Surface", 1.0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3050;

            r.material = mat;
            sliceIndicatorPlane.SetActive(_isSlicingActive && showIndicatorPlane);
        }
    }
}
