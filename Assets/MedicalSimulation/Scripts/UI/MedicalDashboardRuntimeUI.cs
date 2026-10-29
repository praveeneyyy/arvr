using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Cardiovascular;
using MedicalSimulation.Respiratory;
using MedicalSimulation.Audio;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Self-contained runtime medical UI builder and manager.
    /// Injects or enhances the dark clinical interface across all scenes.
    /// Fully responsive across 1080p, 1440p, 4K, and XR viewports.
    /// Uses native uGUI + TextMeshPro with dark navy / medical cyan visual styling.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MedicalDashboardRuntimeUI : MonoBehaviour
    {
        [Header("UI Mode")]
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private bool showPhysiologyControls = true;
        [SerializeField] private bool showSystemFilters = true;

        private Canvas _canvas;
        private RectTransform _rootPanel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _statsText;
        private TextMeshProUGUI _organNameText;
        private TextMeshProUGUI _organDetailsText;
        private Slider _heartRateSlider;
        private Slider _respRateSlider;
        private Slider _flowSpeedSlider;
        private Slider _transparencySlider;

        private AnatomyModelController _anatomyCtrl;
        private HeartAnimationController _heartCtrl;
        private RespiratoryAnimationController _respCtrl;
        private BloodFlowController _flowCtrl;

        void Awake()
        {
            if (autoBuildIfMissing && FindFirstObjectByType<Canvas>() == null)
            {
                BuildClinicalUI();
            }
        }

        void Start()
        {
            _anatomyCtrl = FindFirstObjectByType<AnatomyModelController>();
            _heartCtrl = FindFirstObjectByType<HeartAnimationController>();
            _respCtrl = FindFirstObjectByType<RespiratoryAnimationController>();
            _flowCtrl = FindFirstObjectByType<BloodFlowController>();

            OrganInteractable.OnOrganSelected += HandleOrganSelected;
            OrganInteractable.OnOrganDeselected += HandleOrganDeselected;

            MedicalAIChatUI.EnsureInstance();

            RefreshStatistics();
        }

        void OnDestroy()
        {
            OrganInteractable.OnOrganSelected -= HandleOrganSelected;
            OrganInteractable.OnOrganDeselected -= HandleOrganDeselected;
        }

        private void BuildClinicalUI()
        {
            // 1. Create Canvas
            var canvasGo = new GameObject("MedicalDashboard_Canvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 2. Header Bar
            CreateHeader(canvasGo.transform);

            // 3. Left Panel (Anatomical Systems Selector)
            if (showSystemFilters)
            {
                CreateSystemSelectorPanel(canvasGo.transform);
            }

            // 4. Right Panel (Organ Inspection HUD)
            CreateInspectionPanel(canvasGo.transform);

            // 5. Bottom Panel (Live Physiology Controls)
            if (showPhysiologyControls)
            {
                CreatePhysiologyHUD(canvasGo.transform);
            }

            Debug.Log("[MedicalDashboardRuntimeUI] Clinical UI overlay built successfully.");
        }

        private void CreateHeader(Transform parent)
        {
            var headerGo = CreateUIPanel("HeaderBar", parent,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -60f), new Vector2(0f, 60f),
                new Color(0.04f, 0.08f, 0.14f, 0.92f));

            // Title
            _titleText = CreateUIText("Title", headerGo.transform,
                "<b>PROCEDURELAB</b> <color=#00E5FF>MED-XR SIMULATION PLATFORM</color>",
                new Vector2(0f, 0f), new Vector2(0.45f, 1f),
                new Vector2(25f, 0f), new Vector2(0f, 0f),
                18, TextAlignmentOptions.MidlineLeft, Color.white);

            // Real Session Stats readout
            _statsText = CreateUIText("Stats", headerGo.transform,
                "Modules: 0 | Procedures: 0 | Score: —",
                new Vector2(0.45f, 0f), new Vector2(0.75f, 1f),
                Vector2.zero, Vector2.zero,
                14, TextAlignmentOptions.MidlineCenter, new Color(0.6f, 0.85f, 0.95f));

            // AI Assistant Button
            CreateUIButton("Btn_AIAssistant", headerGo.transform, "<color=#00E5FF>AI ASSISTANT</color>",
                new Vector2(0.76f, 0.15f), new Vector2(0.86f, 0.85f),
                () => MedicalAIChatUI.EnsureInstance().TogglePanel());

            // Home / Menu Button
            CreateUIButton("Btn_Home", headerGo.transform, "Main Menu",
                new Vector2(0.88f, 0.15f), new Vector2(0.98f, 0.85f),
                () => MedicalAppManager.Instance?.GoToMainMenu());
        }

        private void CreateSystemSelectorPanel(Transform parent)
        {
            var panel = CreateUIPanel("SystemSelectorPanel", parent,
                new Vector2(0f, 0.15f), new Vector2(0.18f, 0.88f),
                new Vector2(15f, 0f), new Vector2(0f, 0f),
                new Color(0.05f, 0.09f, 0.16f, 0.88f));

            CreateUIText("Title", panel.transform,
                "<b>ANATOMICAL SYSTEMS</b>",
                new Vector2(0f, 0.92f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                14, TextAlignmentOptions.Center, new Color(0.0f, 0.9f, 0.95f));

            var systems = new (string label, AnatomicalSystem sys)[]
            {
                ("All Systems", AnatomicalSystem.None),
                ("Cardiovascular", AnatomicalSystem.Cardiovascular),
                ("Respiratory", AnatomicalSystem.Respiratory),
                ("Digestive", AnatomicalSystem.Digestive),
                ("Urinary", AnatomicalSystem.Urinary),
                ("Skeletal", AnatomicalSystem.Skeletal)
            };

            float top = 0.88f;
            float step = 0.09f;
            foreach (var item in systems)
            {
                var captured = item.sys;
                CreateUIButton("Btn_" + item.label, panel.transform, item.label,
                    new Vector2(0.08f, top - step + 0.015f), new Vector2(0.92f, top),
                    () =>
                    {
                        MedicalAudioController.Instance?.PlaySelect();
                        if (captured == AnatomicalSystem.None) _anatomyCtrl?.ShowAllSystems();
                        else _anatomyCtrl?.ShowSystem(captured);
                    });
                top -= step;
            }

            // Exploded view toggle button
            CreateUIButton("Btn_ExplodedView", panel.transform, "Toggle Exploded View",
                new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.12f),
                () =>
                {
                    MedicalAudioController.Instance?.PlayClick();
                    _anatomyCtrl?.ToggleExplodedView();
                });
        }

        private void CreateInspectionPanel(Transform parent)
        {
            var panel = CreateUIPanel("OrganInspectionHUD", parent,
                new Vector2(0.78f, 0.20f), new Vector2(0.985f, 0.88f),
                new Vector2(0f, 0f), new Vector2(-15f, 0f),
                new Color(0.05f, 0.09f, 0.16f, 0.92f));

            _organNameText = CreateUIText("OrganName", panel.transform,
                "<color=#00E5FF>SELECT AN ORGAN</color>",
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f),
                Vector2.zero, Vector2.zero,
                16, TextAlignmentOptions.MidlineCenter, Color.white);

            _organDetailsText = CreateUIText("OrganDetails", panel.transform,
                "Click or point to any anatomical structure to inspect its physiological function, vascular supply, and clinical implications.",
                new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.86f),
                Vector2.zero, Vector2.zero,
                13, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 0.95f));

            // Isolate Organ Button
            CreateUIButton("Btn_Isolate", panel.transform, "Isolate Organ",
                new Vector2(0.08f, 0.15f), new Vector2(0.48f, 0.23f),
                () => _anatomyCtrl?.IsolateSelectedOrgan());

            // Reset View Button
            CreateUIButton("Btn_Reset", panel.transform, "Reset View",
                new Vector2(0.52f, 0.15f), new Vector2(0.92f, 0.23f),
                () => _anatomyCtrl?.ResetView());

            // Transparency slider
            CreateUIText("Lbl_Transparency", panel.transform, "Surrounding Transparency",
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.14f),
                Vector2.zero, Vector2.zero, 11, TextAlignmentOptions.MidlineLeft, Color.cyan);

            _transparencySlider = CreateUISlider("Slider_Transparency", panel.transform,
                new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.08f),
                val => _anatomyCtrl?.SetSurroundingTransparency(1f - val));
        }

        private void CreatePhysiologyHUD(Transform parent)
        {
            var panel = CreateUIPanel("PhysiologyHUD", parent,
                new Vector2(0.20f, 0.02f), new Vector2(0.76f, 0.12f),
                Vector2.zero, Vector2.zero,
                new Color(0.04f, 0.08f, 0.14f, 0.90f));

            // Cardiac BPM Slider
            CreateUIText("Lbl_BPM", panel.transform, "Cardiac Rate (BPM)",
                new Vector2(0.03f, 0.55f), new Vector2(0.30f, 0.95f),
                Vector2.zero, Vector2.zero, 12, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.4f, 0.4f));

            _heartRateSlider = CreateUISlider("Slider_HeartRate", panel.transform,
                new Vector2(0.03f, 0.12f), new Vector2(0.30f, 0.50f),
                bpm => _heartCtrl?.SetHeartRate(bpm * 150f + 30f));

            // Respiratory RPM Slider
            CreateUIText("Lbl_RPM", panel.transform, "Respiratory Rate (/min)",
                new Vector2(0.36f, 0.55f), new Vector2(0.63f, 0.95f),
                Vector2.zero, Vector2.zero, 12, TextAlignmentOptions.MidlineLeft, new Color(0.4f, 0.9f, 0.9f));

            _respRateSlider = CreateUISlider("Slider_RespRate", panel.transform,
                new Vector2(0.36f, 0.12f), new Vector2(0.63f, 0.50f),
                rpm => _respCtrl?.SetRespiratoryRate(rpm * 26f + 6f));

            // Blood Flow Speed Slider
            CreateUIText("Lbl_Flow", panel.transform, "Blood Flow Velocity",
                new Vector2(0.69f, 0.55f), new Vector2(0.96f, 0.95f),
                Vector2.zero, Vector2.zero, 12, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.3f, 0.9f));

            _flowSpeedSlider = CreateUISlider("Slider_FlowSpeed", panel.transform,
                new Vector2(0.69f, 0.12f), new Vector2(0.96f, 0.50f),
                spd => _flowCtrl?.SetFlowSpeed(spd * 3f + 0.2f));
        }

        // ── Selection Updates ─────────────────────────────────────────────────

        private void HandleOrganSelected(OrganInteractable organ)
        {
            if (organ == null) return;
            var meta = organ.metadata ?? AnatomyMetadata.GetFallback(organ.gameObject.name);

            if (_organNameText != null)
                _organNameText.text = $"<color=#00E5FF>{meta.anatomicalName.ToUpper()}</color>";

            if (_organDetailsText != null)
            {
                _organDetailsText.text = $"<b>System:</b> {meta.systemLabel}\n" +
                                         $"<b>Location:</b> {meta.anatomicalLocation}\n\n" +
                                         $"<b>Function:</b>\n{meta.primaryFunctions}\n\n" +
                                         $"<b>Clinical Notes:</b>\n{meta.clinicalInformation}";
            }

            MedicalAudioController.Instance?.PlaySelect();
        }

        private void HandleOrganDeselected()
        {
            if (_organNameText != null) _organNameText.text = "<color=#00E5FF>SELECT AN ORGAN</color>";
            if (_organDetailsText != null)
            {
                _organDetailsText.text = "Click or point to any anatomical structure to inspect its physiological function and vascular supply.";
            }
        }

        private void RefreshStatistics()
        {
            var app = MedicalAppManager.Instance;
            if (app != null && _statsText != null)
            {
                string avgStr = app.AssessmentsCompleted > 0 ? $"{app.AverageScore * 100f:F0}%" : "—";
                _statsText.text = $"Modules: <b>{app.ModulesCompleted}</b> | Procedures: <b>{app.ProceduresPracticed}</b> | Mastery: <b>{avgStr}</b>";
            }
        }

        // ── UI Factory Utilities ──────────────────────────────────────────────

        private GameObject CreateUIPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private TextMeshProUGUI CreateUIText(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableWordWrapping = true;

            return tmp;
        }

        private Button CreateUIButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.10f, 0.22f, 0.32f, 0.90f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.0f, 0.85f, 0.95f, 1f);
            colors.pressedColor = new Color(0.0f, 0.55f, 0.70f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            CreateUIText("Text", go.transform, text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 13, TextAlignmentOptions.Center, Color.white);

            return btn;
        }

        private Slider CreateUISlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action<float> onValueChanged)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.35f);
            bgRt.anchorMax = new Vector2(1f, 0.65f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.18f, 0.25f, 0.8f);

            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillRt = fillArea.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0.35f);
            fillRt.anchorMax = new Vector2(1f, 0.65f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillImgRt = fillGo.AddComponent<RectTransform>();
            fillImgRt.anchorMin = Vector2.zero;
            fillImgRt.anchorMax = Vector2.one;
            fillImgRt.offsetMin = Vector2.zero;
            fillImgRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.0f, 0.85f, 0.95f, 0.95f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillImgRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            slider.onValueChanged.AddListener(v => onValueChanged?.Invoke(v));

            return slider;
        }
    }
}
