using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.Anatomy;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Discreet bottom 3D manipulation toolbar for the Anatomy Explorer.
    /// Provides one-click camera reset, orbit/pan toggle, focus selection,
    /// exploded view, slice plane, anatomical labels, and fullscreen viewport toggle.
    /// Follows the refined dark medical workstation design system.
    /// </summary>
    public class ViewportToolbar : MonoBehaviour
    {
        [Header("Toolbar References")]
        [SerializeField] private AnatomyModelController anatomyController;
        [SerializeField] private AnatomyCameraController cameraController;
        [SerializeField] private AnatomicalSlicePlaneController slicePlaneController;

        private Button _resetCamBtn;
        private Button _orbitPanToggleBtn;
        private Button _focusBtn;
        private Button _explodedViewBtn;
        private Button _slicePlaneBtn;
        private Button _labelsToggleBtn;
        private Button _fullscreenBtn;

        private TextMeshProUGUI _orbitPanText;
        private TextMeshProUGUI _explodedText;
        private TextMeshProUGUI _slicePlaneText;
        private TextMeshProUGUI _labelsText;
        private TextMeshProUGUI _fullscreenText;

        private bool _isPanMode = false;
        private bool _isExploded = false;
        private bool _isSliceActive = false;
        private bool _areLabelsVisible = true;
        private bool _isFullscreen = false;

        public static ViewportToolbar AttachTo(Transform parent, AnatomyModelController anatomyCtrl = null, AnatomyCameraController camCtrl = null)
        {
            var existing = parent.GetComponentInChildren<ViewportToolbar>();
            if (existing != null) return existing;

            var toolbarGo = new GameObject("ViewportToolbar_BottomDock");
            toolbarGo.transform.SetParent(parent, false);
            var tb = toolbarGo.AddComponent<ViewportToolbar>();
            tb.anatomyController = anatomyCtrl ?? Object.FindFirstObjectByType<AnatomyModelController>();
            tb.cameraController = camCtrl ?? Object.FindFirstObjectByType<AnatomyCameraController>();
            tb.slicePlaneController = Object.FindFirstObjectByType<AnatomicalSlicePlaneController>();
            tb.BuildToolbarUI();
            return tb;
        }

        void Awake()
        {
            if (anatomyController == null) anatomyController = Object.FindFirstObjectByType<AnatomyModelController>();
            if (cameraController == null) cameraController = Object.FindFirstObjectByType<AnatomyCameraController>();
            if (slicePlaneController == null) slicePlaneController = Object.FindFirstObjectByType<AnatomicalSlicePlaneController>();

            if (transform.childCount == 0)
            {
                BuildToolbarUI();
            }
        }

        public void BuildToolbarUI()
        {
            var rt = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(760f, MedicalUITheme.BottomToolbarHeight);

            var img = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceElevated;

            var layout = gameObject.GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(12, 12, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // 1. Reset Camera
            _resetCamBtn = CreateToolbarButton("ResetCam", "Reset Cam", () => {
                if (cameraController != null) cameraController.ResetCamera();
            });

            // 2. Orbit / Pan Toggle
            _orbitPanToggleBtn = CreateToolbarButton("OrbitPan", "Mode: Orbit", () => {
                _isPanMode = !_isPanMode;
                _orbitPanText.text = _isPanMode ? "Mode: <color=#00E5FF>Pan</color>" : "Mode: Orbit";
            });
            _orbitPanText = _orbitPanToggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // 3. Focus Selection
            _focusBtn = CreateToolbarButton("Focus", "Focus Organ", () => {
                if (anatomyController != null && cameraController != null)
                {
                    var selected = anatomyController.GetSelectedOrgan();
                    if (selected != null) cameraController.FocusOn(selected.transform);
                    else cameraController.ResetCamera();
                }
            });

            // 4. Exploded View Toggle
            _explodedViewBtn = CreateToolbarButton("Exploded", "Exploded View", () => {
                if (anatomyController != null)
                {
                    anatomyController.ToggleExplodedView();
                    _isExploded = !_isExploded;
                    _explodedText.text = _isExploded ? "<color=#00E5FF>Exploded: ON</color>" : "Exploded View";
                }
            });
            _explodedText = _explodedViewBtn.GetComponentInChildren<TextMeshProUGUI>();

            // 5. Slice Plane Toggle
            _slicePlaneBtn = CreateToolbarButton("SlicePlane", "Slice Plane", () => {
                if (slicePlaneController != null)
                {
                    _isSliceActive = !_isSliceActive;
                    slicePlaneController.gameObject.SetActive(_isSliceActive);
                    _slicePlaneText.text = _isSliceActive ? "<color=#00E5FF>Slice: ON</color>" : "Slice Plane";
                }
            });
            _slicePlaneText = _slicePlaneBtn.GetComponentInChildren<TextMeshProUGUI>();

            // 6. Labels Toggle
            _labelsToggleBtn = CreateToolbarButton("Labels", "Labels: ON", () => {
                if (anatomyController != null)
                {
                    anatomyController.ToggleLabels();
                    _areLabelsVisible = !_areLabelsVisible;
                    _labelsText.text = _areLabelsVisible ? "Labels: ON" : "<color=#94A3B8>Labels: OFF</color>";
                }
            });
            _labelsText = _labelsToggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // 7. Fullscreen Viewport Toggle
            _fullscreenBtn = CreateToolbarButton("Fullscreen", "Full Viewport", () => {
                _isFullscreen = !_isFullscreen;
                _fullscreenText.text = _isFullscreen ? "<color=#00E5FF>Standard</color>" : "Full Viewport";
                var nav = Object.FindFirstObjectByType<MedicalNavigationController>();
                if (nav != null) nav.ToggleSidebar();
            });
            _fullscreenText = _fullscreenBtn.GetComponentInChildren<TextMeshProUGUI>();
        }

        private Button CreateToolbarButton(string name, string label, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(transform, false);

            var img = btnGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceSecondary;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = MedicalUITheme.SurfaceSecondary;
            colors.highlightedColor = MedicalUITheme.SurfaceHover;
            colors.pressedColor = MedicalUITheme.SurfacePressed;
            colors.selectedColor = MedicalUITheme.SurfaceSecondary;
            btn.colors = colors;
            btn.onClick.AddListener(action);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var rt = txtGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MedicalUITheme.TextPrimary;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            return btn;
        }
    }
}
