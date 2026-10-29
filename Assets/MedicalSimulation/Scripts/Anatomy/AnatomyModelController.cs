using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MedicalSimulation.Core;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Central manager for the anatomy explorer scene.
    /// Controls system visibility, isolation, exploded view, and transparency.
    /// </summary>
    public class AnatomyModelController : MonoBehaviour
    {
        [Header("Root of the anatomy model hierarchy")]
        [SerializeField] private Transform anatomyRoot;

        [Header("Camera Controller Reference")]
        [SerializeField] private AnatomyCameraController cameraController;

        // ── Runtime state ─────────────────────────────────────────────────────
        private List<OrganInteractable> _allOrgans = new();
        private OrganInteractable       _selectedOrgan;
        private bool                    _explodedView;
        private bool                    _labelsVisible = true;

        // Per-organ original positions (for exploded view)
        private Dictionary<OrganInteractable, Vector3> _originalPositions = new();
        [SerializeField] private float explodedScale = 1.4f;

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<OrganInteractable> OnSelectionChanged;

        void Awake()
        {
            if (anatomyRoot == null) anatomyRoot = transform;
            RefreshOrganList();
        }

        void OnEnable()
        {
            OrganInteractable.OnOrganSelected   += HandleOrganSelected;
            OrganInteractable.OnOrganDeselected += HandleOrganDeselected;
        }

        void OnDisable()
        {
            OrganInteractable.OnOrganSelected   -= HandleOrganSelected;
            OrganInteractable.OnOrganDeselected -= HandleOrganDeselected;
        }

        // ── Initialization ────────────────────────────────────────────────────

        public void RefreshOrganList()
        {
            _allOrgans.Clear();
            _originalPositions.Clear();
            _allOrgans.AddRange(anatomyRoot.GetComponentsInChildren<OrganInteractable>(true));
            foreach (var o in _allOrgans)
                _originalPositions[o] = o.transform.position;

            Debug.Log($"[AnatomyModelController] Found {_allOrgans.Count} selectable organs.");
        }

        // ── System visibility ─────────────────────────────────────────────────

        public void ShowSystem(AnatomicalSystem system)
        {
            foreach (var o in _allOrgans)
            {
                bool show = system == AnatomicalSystem.None || o.metadata == null || o.metadata.system == system;
                o.SetVisible(show);
                o.SetTransparency(1f);
            }
        }

        public void ShowAllSystems()
        {
            foreach (var o in _allOrgans)
            {
                o.SetVisible(true);
                o.SetTransparency(1f);
            }
        }

        // ── Selection ─────────────────────────────────────────────────────────

        private void HandleOrganSelected(OrganInteractable organ)
        {
            // Deselect previous
            if (_selectedOrgan != null && _selectedOrgan != organ)
                _selectedOrgan.Deselect();

            _selectedOrgan = organ;

            // Frame camera on selected organ
            if (cameraController != null)
                cameraController.FocusOn(organ.transform);

            OnSelectionChanged?.Invoke(organ);
        }

        private void HandleOrganDeselected()
        {
            _selectedOrgan = null;
            OnSelectionChanged?.Invoke(null);
        }

        public void DeselectCurrent()
        {
            _selectedOrgan?.Deselect();
        }

        // ── Isolation ─────────────────────────────────────────────────────────

        public void IsolateOrgan(OrganInteractable target)
        {
            if (target == null) return;
            foreach (var o in _allOrgans)
            {
                if (o == target)
                {
                    o.SetVisible(true);
                    o.SetTransparency(1f);
                }
                else
                {
                    o.SetTransparency(0.05f);
                }
            }
        }

        public void IsolateSelectedOrgan()  => IsolateOrgan(_selectedOrgan);
        public void RestoreFromIsolation()  => ShowAllSystems();

        // ── Transparency ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets transparency of all organs except the selected one.
        /// alpha 0 = invisible, 1 = opaque.
        /// </summary>
        public void SetSurroundingTransparency(float alpha)
        {
            foreach (var o in _allOrgans)
            {
                if (o != _selectedOrgan)
                    o.SetTransparency(alpha);
            }
        }

        // ── Exploded view ─────────────────────────────────────────────────────

        public void SetExplodedView(bool exploded)
        {
            _explodedView = exploded;
            Vector3 centre = GetModelCentre();

            foreach (var o in _allOrgans)
            {
                if (!_originalPositions.TryGetValue(o, out Vector3 orig)) continue;
                Vector3 target = exploded
                    ? centre + (orig - centre) * explodedScale
                    : orig;
                o.transform.position = target;
            }
        }

        public void ToggleExplodedView() => SetExplodedView(!_explodedView);

        // ── Labels ────────────────────────────────────────────────────────────

        public void SetLabelsVisible(bool visible)
        {
            _labelsVisible = visible;
            // Labels are driven by OrganLabel components attached to organs
            var labels = anatomyRoot.GetComponentsInChildren<OrganLabel>(true);
            foreach (var l in labels) l.gameObject.SetActive(visible);
        }

        public void ToggleLabels() => SetLabelsVisible(!_labelsVisible);

        // ── Camera ────────────────────────────────────────────────────────────

        public void ResetView()
        {
            DeselectCurrent();
            RestoreFromIsolation();
            SetExplodedView(false);
            cameraController?.ResetView();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector3 GetModelCentre()
        {
            if (_allOrgans.Count == 0) return anatomyRoot.position;
            Vector3 sum = Vector3.zero;
            foreach (var o in _allOrgans) sum += o.transform.position;
            return sum / _allOrgans.Count;
        }

        public OrganInteractable GetOrganById(string organId)
            => _allOrgans.FirstOrDefault(o => o.metadata != null && o.metadata.organId == organId);

        public List<OrganInteractable> GetAllOrgans() => _allOrgans;
    }
}
