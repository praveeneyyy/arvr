using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.UI;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Anatomy Explorer Overlay UI matching the exact medical reference interface.
    /// Features:
    /// 1. Left Dock: "Anatomy Explorer" layer toggles (Skeletal, Muscular, Nervous,
    ///    Cardiovascular, Respiratory, Digestive, Lymphatic, Integumentary) + Opacity Sliders.
    /// 2. 3D Viewport Callout Pins with Leader Lines pointing directly to 3D anatomical structures:
    ///    (Cerebrum, Respiratory, Heart, Lungs, Liver, Biceps Brachii, Kidney, Lymphatic, Femur).
    /// 3. Top-Right Structure Info Card (Structure: Left Kidney, clinical overview, vascular supply, etc.).
    /// 4. Bottom-Right Interactive 3D ViewCube (Top, Front, Right, Isometric snapping).
    /// 5. Full Controls Guide HUD explaining mouse, keyboard, and interaction controls.
    /// </summary>
    [ExecuteAlways]
    public class AnatomyExplorerOverlayUI : MonoBehaviour
    {
        public static AnatomyExplorerOverlayUI Instance { get; private set; }

        private Canvas _canvas;
        private Camera _cam;

        // UI Panels
        private GameObject _overlayRoot;
        private GameObject _leftPanelGo;
        private GameObject _rightInfoPanelGo;
        private GameObject _viewCubeGo;
        private GameObject _calloutPinsRoot;
        private GameObject _controlsGuideGo;

        // Right Info Card TMP refs
        private TextMeshProUGUI _structureTitleTmp;
        private TextMeshProUGUI _structureDescTmp;
        private TextMeshProUGUI _structureVascularTmp;
        private TextMeshProUGUI _structureClinicalTmp;

        // Model roots for toggles
        private Transform _modelRoot;
        private GameObject _skeletalGo;
        private GameObject _muscularGo;
        private GameObject _nervousGo;
        private GameObject _cardioGo;
        private GameObject _respiratoryGo;
        private GameObject _digestiveGo;
        private GameObject _integumentaryGo;

        // Callout pin definitions
        [System.Serializable]
        public class CalloutPin
        {
            public string label;
            public Vector3 worldOffset;
            public bool isLeftAligned;
            public string structureName;
            public string description;
            public string vascular;
            public string clinical;
            [System.NonSerialized] public GameObject pinGo;
            [System.NonSerialized] public RectTransform lineRt;
            [System.NonSerialized] public RectTransform labelRt;
            [System.NonSerialized] public Image dotImg;
        }

        private List<CalloutPin> _pins = new List<CalloutPin>();

        void Awake()
        {
            Instance = this;
            _cam = Camera.main;
            EnsureCanvas();
            InitializePinsData();
            BuildOverlay();
        }

        void OnEnable()
        {
            Instance = this;
            _cam = Camera.main;
            EnsureCanvas();
            InitializePinsData();
            BuildOverlay();
        }

        private void EnsureCanvas()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                var workstation = FindFirstObjectByType<MedicalWorkstationUIController>();
                if (workstation != null)
                {
                    _canvas = workstation.GetComponent<Canvas>();
                    transform.SetParent(workstation.transform, false);
                }
            }
        }

        private void InitializePinsData()
        {
            _pins.Clear();

            _pins.Add(new CalloutPin {
                label = "Cerebrum",
                worldOffset = new Vector3(0f, 0.65f, 0f),
                isLeftAligned = true,
                structureName = "Cerebrum (Telencephalon)",
                description = "Primary cerebral cortex and basal ganglia governing voluntary motor function, somatosensory integration, and cognitive cognition.",
                vascular = "Circle of Willis (Internal Carotid & Vertebral arteries)",
                clinical = "Cerebral perfusion pressure maintained at 60-70 mmHg. Intact blood-brain barrier."
            });

            _pins.Add(new CalloutPin {
                label = "Respiratory",
                worldOffset = new Vector3(0f, 0.40f, 0f),
                isLeftAligned = true,
                structureName = "Trachea & Upper Airway",
                description = "Cartilaginous trachea and mainstem bronchi conducting humidified airflow to pulmonary alveoli.",
                vascular = "Inferior thyroid and bronchial arteries",
                clinical = "Patent airway. Tracheostomy landmark 2nd-3rd tracheal rings."
            });

            _pins.Add(new CalloutPin {
                label = "Liver",
                worldOffset = new Vector3(0.08f, 0.15f, 0f),
                isLeftAligned = true,
                structureName = "Hepatic Lobes & Gallbladder",
                description = "Right & left hepatic lobes responsible for metabolic detoxification, glycogen synthesis, and bile secretion into the emerald gallbladder.",
                vascular = "Dual supply: 75% Portal Vein, 25% Hepatic Artery Proper",
                clinical = "Normal INR/PT coagulation profile. Resection margins clearly demarcated."
            });

            _pins.Add(new CalloutPin {
                label = "Biceps Brachii",
                worldOffset = new Vector3(0.25f, 0.22f, 0f),
                isLeftAligned = true,
                structureName = "Biceps Brachii Muscle",
                description = "Two-headed skeletal muscle crossing glenohumeral and elbow joints. Primary forearm supinator and elbow flexor.",
                vascular = "Muscular branches of the Brachial Artery",
                clinical = "Musculocutaneous nerve (C5-C6) innervation intact. Strength 5/5."
            });

            _pins.Add(new CalloutPin {
                label = "Heart",
                worldOffset = new Vector3(-0.03f, 0.26f, 0f),
                isLeftAligned = false,
                structureName = "Heart (Myocardium)",
                description = "Four-chambered muscular organ powering systemic and pulmonary circulation with coordinated 72 BPM electrical conduction.",
                vascular = "Left Anterior Descending (LAD) & Right Coronary Artery (RCA)",
                clinical = "Sinus rhythm, ejection fraction 62%, normal cardiac output 5.0 L/min."
            });

            _pins.Add(new CalloutPin {
                label = "Lungs",
                worldOffset = new Vector3(-0.13f, 0.28f, 0f),
                isLeftAligned = false,
                structureName = "Lungs (Pulmonary Parenchyma)",
                description = "Bilateral alveolar parenchyma facilitating gas exchange of oxygen and carbon dioxide across the respiratory membrane.",
                vascular = "Pulmonary Arteries (deoxygenated) & Bronchial Circulation",
                clinical = "Bilateral vesicular breath sounds, SpO2 99% on room air."
            });

            _pins.Add(new CalloutPin {
                label = "Kidney",
                worldOffset = new Vector3(-0.10f, 0.08f, 0f),
                isLeftAligned = false,
                structureName = "Structure: Left Kidney",
                description = "Left Kidney is proximal to retroperitoneal space and spleen. Contains 1.2 million nephrons performing active ultrafiltration and acid-base homeostasis.",
                vascular = "Left Renal Artery (from Abdominal Aorta) & Left Renal Vein (to IVC)",
                clinical = "Glomerular Filtration Rate (GFR): 125 mL/min. Active cyan vascular glow."
            });

            _pins.Add(new CalloutPin {
                label = "Lymphatic",
                worldOffset = new Vector3(-0.24f, -0.05f, 0f),
                isLeftAligned = false,
                structureName = "Lymphatic System & Nodes",
                description = "Network of lymphatic channels draining interstitial fluid, filtering pathogens through axillary nodes and returning lymph to venous angles.",
                vascular = "Right Lymphatic Duct & Thoracic Duct draining into Subclavian veins",
                clinical = "No lymphadenopathy. Immune surveillance fully functional."
            });

            _pins.Add(new CalloutPin {
                label = "Femur",
                worldOffset = new Vector3(-0.12f, -0.42f, 0f),
                isLeftAligned = false,
                structureName = "Femur (Cortical Bone)",
                description = "Longest and strongest bone in the human body, articulating proximally with the acetabulum and distally with patella and tibia.",
                vascular = "Profunda Femoris & Medial/Lateral Circumflex Femoral Arteries",
                clinical = "Intact trabecular bone density. No fracture or cortical defect."
            });
        }

        [ContextMenu("Rebuild Anatomy Overlay")]
        public void BuildOverlay()
        {
            if (_overlayRoot != null)
            {
                if (Application.isPlaying) Destroy(_overlayRoot);
                else DestroyImmediate(_overlayRoot);
            }

            var master = FindFirstObjectByType<MedicalWorkstationUIController>();
            if (master != null)
            {
                // Master controller handles all unified UI - avoid duplicate overlays
                return;
            }

            if (_canvas == null) EnsureCanvas();
            if (_canvas == null) return;

            FindModelRoots();

            _overlayRoot = new GameObject("AnatomyExplorer_Overlay");
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            var rt = _overlayRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            BuildLeftAnatomyPanel(_overlayRoot.transform);
            BuildRightStructureInfoCard(_overlayRoot.transform);
            BuildInteractiveViewCube(_overlayRoot.transform);
            BuildCalloutPins(_overlayRoot.transform);
            BuildControlsGuideHUD(_overlayRoot.transform);

            // Default selection: Left Kidney (matching reference image 1)
            SelectStructureByLabel("Kidney");
        }

        private void FindModelRoots()
        {
            var viewer = Medical3DComponentViewer.Instance ?? FindFirstObjectByType<Medical3DComponentViewer>();
            if (viewer != null)
            {
                var fullAnatomy = viewer.transform.Find("Comp_01_FullAnatomy");
                if (fullAnatomy != null)
                {
                    _modelRoot = fullAnatomy;
                    var explorer = fullAnatomy.Find("Human_Anatomy_Explorer_Root") ?? fullAnatomy;
                    _skeletalGo = explorer.Find("System_Skeletal")?.gameObject;
                    _muscularGo = explorer.Find("System_Muscular")?.gameObject;
                    _nervousGo = explorer.Find("System_Nervous")?.gameObject;
                    _cardioGo = explorer.Find("System_Cardiovascular")?.gameObject;
                    _respiratoryGo = explorer.Find("System_Respiratory")?.gameObject;
                    _digestiveGo = explorer.Find("System_Digestive")?.gameObject;
                    _integumentaryGo = explorer.Find("System_Integumentary")?.gameObject;
                }
            }
        }

        private void BuildLeftAnatomyPanel(Transform parent)
        {
            // Positioned on the left side, matching reference image 1
            _leftPanelGo = CreatePanel("AnatomyExplorer_LeftPanel", parent,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(250, -65), new Vector2(240, 520));

            var vLayout = _leftPanelGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(14, 14, 12, 12);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandHeight = false;

            // Panel Title
            var titleRow = CreateUIRect("Header", _leftPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 26));
            var titleTmp = titleRow.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>Anatomy Explorer</b>";
            titleTmp.fontSize = 15;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // System Toggles
            CreateSystemToggle("Skeletal", true, (val) => SetSystemActive(_skeletalGo, val), _leftPanelGo.transform);
            CreateSystemToggle("Muscular", true, (val) => SetSystemActive(_muscularGo, val), _leftPanelGo.transform);
            CreateSystemToggle("Nervous", true, (val) => SetSystemActive(_nervousGo, val), _leftPanelGo.transform);
            CreateSystemToggle("Cardiovascular", true, (val) => SetSystemActive(_cardioGo, val), _leftPanelGo.transform);
            CreateSystemToggle("Respiratory", true, (val) => SetSystemActive(_respiratoryGo, val), _leftPanelGo.transform);
            CreateSystemToggle("Digestive", true, (val) => SetSystemActive(_digestiveGo, val), _leftPanelGo.transform);
            CreateSystemToggle("Lymphatic", true, (val) => {
                // Lymphatic toggles peripheral neurovascular
                if (_cardioGo != null)
                {
                    foreach (Transform t in _cardioGo.transform)
                    {
                        if (t.name.Contains("Intercostal") || t.name.Contains("Femoral"))
                            t.gameObject.SetActive(val);
                    }
                }
            }, _leftPanelGo.transform);
            CreateSystemToggle("Integumentary", true, (val) => SetSystemActive(_integumentaryGo, val), _leftPanelGo.transform);

            // Divider
            var div = CreateUIRect("Divider", _leftPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
            div.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            // 3 Opacity Sliders matching reference image 1
            CreateOpacitySlider("Skeletal Opacity", 1.0f, (val) => AdjustSystemOpacity(_skeletalGo, val), _leftPanelGo.transform);
            CreateOpacitySlider("Muscular Opacity", 1.0f, (val) => AdjustSystemOpacity(_muscularGo, val), _leftPanelGo.transform);
            CreateOpacitySlider("Organ Opacity", 1.0f, (val) => AdjustSystemOpacity(_digestiveGo, val), _leftPanelGo.transform);
        }

        private void CreateSystemToggle(string label, bool initialVal, UnityEngine.Events.UnityAction<bool> onToggle, Transform parent)
        {
            var rowGo = CreateUIRect("Toggle_" + label, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 26));
            var hLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandHeight = true;
            hLayout.spacing = 8;

            // Label
            var lblGo = CreateUIRect("Label", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(150, 26));
            var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 12;
            lblTmp.color = MedicalUITheme.TextPrimary;
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Pill Switch
            var switchGo = CreateUIRect("Switch", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(44, 22));
            var switchImg = switchGo.AddComponent<Image>();
            switchImg.color = initialVal ? new Color(0.12f, 0.45f, 0.85f, 1f) : MedicalUITheme.SurfaceElevated;

            var knobGo = CreateUIRect("Knob", switchGo.transform, new Vector2(initialVal ? 1 : 0, 0.5f), new Vector2(initialVal ? 1 : 0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(initialVal ? -11 : 11, 0), new Vector2(16, 16));
            var knobImg = knobGo.AddComponent<Image>();
            knobImg.color = Color.white;

            var btn = switchGo.AddComponent<Button>();
            bool state = initialVal;
            btn.onClick.AddListener(() => {
                state = !state;
                switchImg.color = state ? new Color(0.12f, 0.45f, 0.85f, 1f) : MedicalUITheme.SurfaceElevated;
                var knobRt = knobGo.GetComponent<RectTransform>();
                knobRt.anchorMin = new Vector2(state ? 1 : 0, 0.5f);
                knobRt.anchorMax = new Vector2(state ? 1 : 0, 0.5f);
                knobRt.anchoredPosition = new Vector2(state ? -11 : 11, 0);
                onToggle?.Invoke(state);
            });
        }

        private void CreateOpacitySlider(string label, float initialVal, UnityEngine.Events.UnityAction<float> onSlide, Transform parent)
        {
            var rowGo = CreateUIRect("Slider_" + label.Replace(" ", "_"), parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 32));
            var vLayout = rowGo.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            var lblGo = CreateUIRect("Label", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 14));
            var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 10;
            lblTmp.color = MedicalUITheme.TextSecondary;

            var sliderGo = CreateUIRect("SliderBar", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 12));
            var sliderImg = sliderGo.AddComponent<Image>();
            sliderImg.color = MedicalUITheme.SurfaceElevated;

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0.05f;
            slider.maxValue = 1.0f;
            slider.value = initialVal;

            var fillGo = CreateUIRect("Fill", sliderGo.transform, Vector2.zero, new Vector2(initialVal, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = MedicalUITheme.AccentCyan;
            slider.fillRect = fillGo.GetComponent<RectTransform>();

            slider.onValueChanged.AddListener((v) => {
                onSlide?.Invoke(v);
            });
        }

        private void SetSystemActive(GameObject go, bool val)
        {
            if (go != null) go.SetActive(val);
        }

        private void AdjustSystemOpacity(GameObject go, float alpha)
        {
            if (go == null) return;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                {
                    Color c = r.sharedMaterial.color;
                    c.a = alpha;
                    r.sharedMaterial.color = c;
                }
            }
        }

        private void BuildRightStructureInfoCard(Transform parent)
        {
            // Positioned at top-right, matching reference image 1
            _rightInfoPanelGo = CreatePanel("StructureInfoCard", parent,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, -65), new Vector2(300, 240));

            var vLayout = _rightInfoPanelGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(16, 16, 14, 14);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            // Structure Header
            var titleGo = CreateUIRect("Title", _rightInfoPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 22));
            _structureTitleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            _structureTitleTmp.text = "<b>Structure: Left Kidney</b>";
            _structureTitleTmp.fontSize = 14;
            _structureTitleTmp.color = Color.white;

            // "Info" subheader
            var infoSub = CreateUIRect("InfoSub", _rightInfoPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 16));
            var infoTmp = infoSub.AddComponent<TextMeshProUGUI>();
            infoTmp.text = "<color=#94A3B8>Info</color>";
            infoTmp.fontSize = 11;

            // Description body matching reference image 1
            var descGo = CreateUIRect("Desc", _rightInfoPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 75));
            _structureDescTmp = descGo.AddComponent<TextMeshProUGUI>();
            _structureDescTmp.text = "Left Kidney is proximal to retroperitoneal space. Performs metabolic waste filtration, electrolyte homeostasis, and renin endocrine secretion.";
            _structureDescTmp.fontSize = 10;
            _structureDescTmp.color = new Color(0.85f, 0.90f, 0.95f);
            _structureDescTmp.textWrappingMode = TextWrappingModes.Normal;

            // Vascular details
            var vascGo = CreateUIRect("Vascular", _rightInfoPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 36));
            _structureVascularTmp = vascGo.AddComponent<TextMeshProUGUI>();
            _structureVascularTmp.text = "<color=#00E5FF>Vascular Supply:</color>\nLeft Renal Artery (Aorta) → Left Renal Vein (IVC)";
            _structureVascularTmp.fontSize = 9.5f;

            // Clinical metrics
            var clinGo = CreateUIRect("Clinical", _rightInfoPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 36));
            _structureClinicalTmp = clinGo.AddComponent<TextMeshProUGUI>();
            _structureClinicalTmp.text = "<color=#2DD4BF>Clinical Telemetry:</color>\nGFR: 125 mL/min | Flow: Normal Perfusion";
            _structureClinicalTmp.fontSize = 9.5f;
        }

        private void BuildInteractiveViewCube(Transform parent)
        {
            // Positioned at bottom-right corner, matching reference image 1
            _viewCubeGo = CreatePanel("InteractiveViewCube", parent,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-30, 80), new Vector2(90, 90));

            var vLayout = _viewCubeGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(6, 6, 6, 6);
            vLayout.spacing = 4;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;

            var titleGo = CreateUIRect("Title", _viewCubeGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 14));
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<size=80%><color=#64748B>VIEWCUBE</color></size>";
            titleTmp.fontSize = 9;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Grid of views
            var gridGo = CreateUIRect("Grid", _viewCubeGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 60));
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(36, 26);
            grid.spacing = new Vector2(4, 4);

            CreateViewCubeButton("TOP", () => SnapCameraTo(new Vector3(0, 2.5f, 0), Vector3.zero), gridGo.transform);
            CreateViewCubeButton("FRONT", () => SnapCameraTo(new Vector3(0.12f, 0.4f, -2.2f), new Vector3(0.12f, 0.3f, 0)), gridGo.transform);
            CreateViewCubeButton("RIGHT", () => SnapCameraTo(new Vector3(2.2f, 0.4f, 0), new Vector3(0, 0.3f, 0)), gridGo.transform);
            CreateViewCubeButton("ISO", () => SnapCameraTo(new Vector3(1.6f, 1.2f, -1.8f), new Vector3(0.12f, 0.3f, 0)), gridGo.transform);
        }

        private void CreateViewCubeButton(string label, UnityEngine.Events.UnityAction action, Transform parent)
        {
            var btnGo = CreateUIRect("Btn_" + label, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            btnGo.AddComponent<Image>().color = MedicalUITheme.SurfaceElevated;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 9;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MedicalUITheme.TextPrimary;
        }

        private void SnapCameraTo(Vector3 camPos, Vector3 lookTarget)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                _cam.transform.position = camPos;
                _cam.transform.LookAt(lookTarget);
            }
        }

        private void BuildCalloutPins(Transform parent)
        {
            _calloutPinsRoot = new GameObject("CalloutPins_Layer");
            _calloutPinsRoot.transform.SetParent(parent, false);
            var rt = _calloutPinsRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            foreach (var pin in _pins)
            {
                // Create pin container
                var pinGo = new GameObject("Pin_" + pin.label);
                pinGo.transform.SetParent(_calloutPinsRoot.transform, false);
                var pinRt = pinGo.AddComponent<RectTransform>();
                pinRt.sizeDelta = new Vector2(180, 24);
                pin.pinGo = pinGo;

                // Glowing target dot
                var dotGo = CreateUIRect("TargetDot", pinGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8));
                pin.dotImg = dotGo.AddComponent<Image>();
                pin.dotImg.color = pin.label == "Kidney" ? MedicalUITheme.AccentCyan : Color.white;

                // Leader Line
                var lineGo = CreateUIRect("LeaderLine", pinGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 1.5f));
                var lineImg = lineGo.AddComponent<Image>();
                lineImg.color = new Color(1f, 1f, 1f, 0.65f);
                pin.lineRt = lineGo.GetComponent<RectTransform>();

                // Text Box with Button
                var boxGo = CreateUIRect("TextBox", pinGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 22));
                var boxImg = boxGo.AddComponent<Image>();
                boxImg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
                var boxBtn = boxGo.AddComponent<Button>();
                var pinCapture = pin;
                boxBtn.onClick.AddListener(() => {
                    SelectStructure(pinCapture);
                });
                pin.labelRt = boxGo.GetComponent<RectTransform>();

                var txtGo = CreateUIRect("Text", boxGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var tmp = txtGo.AddComponent<TextMeshProUGUI>();
                tmp.text = pin.label;
                tmp.fontSize = 11;
                tmp.color = pin.label == "Kidney" ? MedicalUITheme.AccentCyan : Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private void BuildControlsGuideHUD(Transform parent)
        {
            // Discreet controls guide badge at top center
            _controlsGuideGo = CreatePanel("ControlsGuideHUD", parent,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -65), new Vector2(480, 28));

            var tmp = _controlsGuideGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "<size=80%><color=#64748B>CONTROLS:</color></size> <color=#94A3B8>Left Drag: Orbit | Right/Shift Drag: Pan | Scroll: Zoom | Space: Auto-Rotate | R: Reset View</color>";
            tmp.fontSize = 10;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void LateUpdate()
        {
            UpdatePinPositions();
        }

        private void UpdatePinPositions()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || _canvas == null) return;

            var viewer = Medical3DComponentViewer.Instance;
            Vector3 modelBase = viewer != null ? viewer.transform.position : Vector3.zero;

            foreach (var pin in _pins)
            {
                if (pin.pinGo == null) continue;

                Vector3 targetWorld = modelBase + pin.worldOffset;
                Vector3 screenPos = _cam.WorldToScreenPoint(targetWorld);

                if (screenPos.z < 0.1f)
                {
                    pin.pinGo.SetActive(false);
                    continue;
                }

                pin.pinGo.SetActive(true);

                // Convert screen pos to Canvas local pos
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    screenPos,
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam,
                    out Vector2 localPoint
                );

                var pinRt = pin.pinGo.GetComponent<RectTransform>();
                pinRt.anchoredPosition = localPoint;

                // Layout leader line and text box
                float lineLen = 45f;
                float sign = pin.isLeftAligned ? -1f : 1f;

                if (pin.lineRt != null)
                {
                    pin.lineRt.anchoredPosition = new Vector2(sign * lineLen * 0.5f, 0);
                    pin.lineRt.sizeDelta = new Vector2(lineLen, 1.5f);
                }

                if (pin.labelRt != null)
                {
                    pin.labelRt.anchoredPosition = new Vector2(sign * (lineLen + 50f), 0);
                }
            }
        }

        public void SelectStructureByLabel(string label)
        {
            var p = _pins.Find(x => x.label.Equals(label, System.StringComparison.OrdinalIgnoreCase));
            if (p != null) SelectStructure(p);
        }

        public void SelectStructure(CalloutPin pin)
        {
            if (pin == null) return;

            if (_structureTitleTmp != null) _structureTitleTmp.text = $"<b>{pin.structureName}</b>";
            if (_structureDescTmp != null) _structureDescTmp.text = pin.description;
            if (_structureVascularTmp != null) _structureVascularTmp.text = $"<color=#00E5FF>Vascular Supply:</color>\n{pin.vascular}";
            if (_structureClinicalTmp != null) _structureClinicalTmp.text = $"<color=#2DD4BF>Clinical Telemetry:</color>\n{pin.clinical}";

            // Highlight target dot
            foreach (var other in _pins)
            {
                if (other.dotImg != null)
                {
                    other.dotImg.color = other == pin ? MedicalUITheme.AccentCyan : Color.white;
                    other.dotImg.rectTransform.sizeDelta = other == pin ? new Vector2(12, 12) : new Vector2(8, 8);
                }
            }
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = CreateUIRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.07f, 0.09f, 0.13f, 0.92f);

            // Subtle border
            var border = CreateUIRect("Border", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var borderImg = border.AddComponent<Image>();
            borderImg.color = MedicalUITheme.BorderSubtle;

            return go;
        }

        private GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }
    }
}
