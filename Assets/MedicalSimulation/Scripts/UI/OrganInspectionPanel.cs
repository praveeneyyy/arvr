using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Core;
using System.Collections.Generic;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Organ inspection panel. Updates its tabs when an OrganInteractable is selected.
    /// All information comes from AnatomyMetadata ScriptableObjects — never invented.
    /// </summary>
    public class OrganInspectionPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI organNameText;
        [SerializeField] private TextMeshProUGUI systemLabel;
        [SerializeField] private Image           organIcon;

        [Header("Tab Buttons")]
        [SerializeField] private Button overviewTabButton;
        [SerializeField] private Button functionsTabButton;
        [SerializeField] private Button anatomyTabButton;
        [SerializeField] private Button clinicalTabButton;

        [Header("Tab Content Areas")]
        [SerializeField] private TextMeshProUGUI contentText;

        [Header("Related Structures")]
        [SerializeField] private TextMeshProUGUI relatedStructuresText;

        [Header("Action Buttons")]
        [SerializeField] private Button focusCameraButton;
        [SerializeField] private Button isolateButton;
        [SerializeField] private Button showNeighboursButton;
        [SerializeField] private Button toggleLabelsButton;
        [SerializeField] private Slider transparencySlider;
        [SerializeField] private Button restoreButton;

        [Header("References")]
        [SerializeField] private AnatomyModelController anatomyController;

        // ── Internal ─────────────────────────────────────────────────────────
        private OrganInteractable _currentOrgan;
        private AnatomyMetadata   _currentMetadata;
        private enum Tab { Overview, Functions, Anatomy, Clinical }
        private Tab _activeTab = Tab.Overview;

        void Awake()
        {
            if (anatomyController == null)
                anatomyController = FindFirstObjectByType<AnatomyModelController>();

            // Wire tabs
            overviewTabButton?.onClick.AddListener(() => ShowTab(Tab.Overview));
            functionsTabButton?.onClick.AddListener(() => ShowTab(Tab.Functions));
            anatomyTabButton?.onClick.AddListener(()  => ShowTab(Tab.Anatomy));
            clinicalTabButton?.onClick.AddListener(()  => ShowTab(Tab.Clinical));

            // Wire actions
            focusCameraButton?.onClick.AddListener(OnFocusCamera);
            isolateButton?.onClick.AddListener(OnIsolate);
            showNeighboursButton?.onClick.AddListener(OnShowNeighbours);
            toggleLabelsButton?.onClick.AddListener(() => anatomyController?.ToggleLabels());
            restoreButton?.onClick.AddListener(OnRestore);
            transparencySlider?.onValueChanged.AddListener(OnTransparencyChanged);

            Hide();
        }

        void OnEnable()
        {
            OrganInteractable.OnOrganSelected   += ShowOrgan;
            OrganInteractable.OnOrganDeselected += Hide;
        }

        void OnDisable()
        {
            OrganInteractable.OnOrganSelected   -= ShowOrgan;
            OrganInteractable.OnOrganDeselected -= Hide;
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        public void ShowOrgan(OrganInteractable organ)
        {
            _currentOrgan    = organ;
            _currentMetadata = organ?.metadata;

            if (_currentMetadata == null)
            {
                Hide();
                return;
            }

            panelRoot?.SetActive(true);
            PopulateHeader();
            ShowTab(_activeTab);
        }

        public void Hide()
        {
            panelRoot?.SetActive(false);
            _currentOrgan = null;
        }

        // ── Tab display ───────────────────────────────────────────────────────

        private void ShowTab(Tab tab)
        {
            _activeTab = tab;
            if (_currentMetadata == null) return;

            string text = tab switch
            {
                Tab.Overview  => _currentMetadata.overview,
                Tab.Functions => _currentMetadata.primaryFunctions,
                Tab.Anatomy   => _currentMetadata.anatomyDetails,
                Tab.Clinical  => _currentMetadata.clinicalInformation,
                _             => string.Empty
            };

            if (contentText != null) contentText.text = text;
        }

        private void PopulateHeader()
        {
            if (_currentMetadata == null) return;

            if (organNameText != null)
                organNameText.text = _currentMetadata.anatomicalName;

            if (systemLabel != null)
                systemLabel.text = _currentMetadata.systemLabel;

            if (organIcon != null && _currentMetadata.icon != null)
            {
                organIcon.sprite = _currentMetadata.icon;
                organIcon.gameObject.SetActive(true);
            }

            if (relatedStructuresText != null)
            {
                var relations = _currentMetadata.relatedStructures;
                relatedStructuresText.text = (relations != null && relations.Length > 0)
                    ? string.Join(", ", relations)
                    : "—";
            }
        }

        // ── Action callbacks ──────────────────────────────────────────────────

        private void OnFocusCamera()
        {
            if (_currentOrgan != null && anatomyController != null)
            {
                var cam = FindFirstObjectByType<AnatomyCameraController>();
                cam?.FocusOn(_currentOrgan.transform);
            }
        }

        private void OnIsolate()
        {
            if (_currentOrgan != null)
                anatomyController?.IsolateOrgan(_currentOrgan);
        }

        private void OnShowNeighbours()
        {
            if (_currentMetadata == null || anatomyController == null) return;
            // Make all non-related organs very transparent
            var allOrgans = anatomyController.GetAllOrgans();
            var relatedSet = new HashSet<string>(_currentMetadata.relatedStructures ?? new string[0]);
            foreach (var o in allOrgans)
            {
                bool isRelated = o == _currentOrgan ||
                                 (o.metadata != null && relatedSet.Contains(o.metadata.anatomicalName));
                o.SetTransparency(isRelated ? 1f : 0.08f);
            }
        }

        private void OnRestore()
        {
            anatomyController?.RestoreFromIsolation();
        }

        private void OnTransparencyChanged(float value)
        {
            // value 0 = fully opaque surroundings, 1 = fully transparent
            anatomyController?.SetSurroundingTransparency(1f - value);
        }
    }
}
