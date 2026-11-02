using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Master Executive Controller for Project Atlas Main Menu & Startup Experience.
    /// Orchestrates the 3D clinical presentation stage, central anatomical viewport,
    /// 6-module navigation deck, context telemetry card, and cinematic startup sequence.
    /// Can be dropped into any scene or bootstrapped dynamically at runtime.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        [Header("Subsystems")]
        public ModelPresentationController presentationController;
        public ModelRotationController rotationController;
        public ModulePreviewController previewController;
        public ModuleNavigationController navigationController;
        public StartupSequenceController startupController;
        public SceneTransitionController transitionController;

        [Header("UI Canvas Hierarchy")]
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GraphicRaycaster _raycaster;

        private GameObject _topNavbarGo;
        private GameObject _leftModuleDeckGo;
        private GameObject _rightContextPanelGo;
        private GameObject _splashOverlayGo;
        private GameObject _transitionOverlayGo;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildCompleteMainMenuExperience();
        }

        [ContextMenu("Rebuild Main Menu Experience")]
        public void BuildCompleteMainMenuExperience()
        {
            // 0. Purge any legacy placeholder buttons if loaded in legacy scene
            PurgeLegacyClutter();

            // 1. Configure Canvas
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            _scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            _raycaster = gameObject.GetComponent<GraphicRaycaster>() ?? gameObject.AddComponent<GraphicRaycaster>();

            // 2. Setup 3D Clinical Studio Rig (Camera, Pedestal, Lighting)
            Ensure3DStudioRig();

            // 3. Build Top Navigation Bar
            BuildTopNavbar();

            // 4. Build Left Module Navigation Deck
            BuildLeftModuleDeck();

            // 5. Build Right Context Telemetry Panel
            BuildRightContextPanel();

            // 6. Build Cinematic Startup Overlay (Stage A & B)
            BuildStartupOverlay();

            // 7. Build Scene Transition Fader
            BuildTransitionOverlay();

            // 8. Initialize Modules in Navigation Controller
            if (navigationController != null)
            {
                navigationController.InitializeModuleList();
            }
        }

        private void PurgeLegacyClutter()
        {
            string[] legacyNames = new string[] {
                "Button  1", "Button 1", "Button 2", "Button 3",
                "Button dental", "Button nose", "Button liver", "Button nasal",
                "Panel", "Button heart", "Button 4", "Button 5"
            };

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.gameObject == gameObject) continue;
                for (int i = 0; i < canvas.transform.childCount; i++)
                {
                    var child = canvas.transform.GetChild(i);
                    foreach (var legacy in legacyNames)
                    {
                        if (child.name.Equals(legacy, System.StringComparison.OrdinalIgnoreCase))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void Ensure3DStudioRig()
        {
            // Find or create Main Viewport Camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f); // Deep clinical charcoal
            cam.transform.position = new Vector3(0.08f, 0.42f, -2.15f);
            cam.transform.LookAt(new Vector3(0.08f, 0.35f, 0f));

            // Studio Presentation Rig
            var studioGo = GameObject.Find("Clinical_Presentation_Studio");
            if (studioGo == null)
            {
                studioGo = new GameObject("Clinical_Presentation_Studio");
            }

            presentationController = studioGo.GetComponent<ModelPresentationController>() ?? studioGo.AddComponent<ModelPresentationController>();
            presentationController.EnsureStudioRig();

            // Preview Controller
            previewController = studioGo.GetComponent<ModulePreviewController>() ?? studioGo.AddComponent<ModulePreviewController>();

            // Rotation Controller attached to Camera
            rotationController = cam.GetComponent<ModelRotationController>() ?? cam.gameObject.AddComponent<ModelRotationController>();
        }

        private void BuildTopNavbar()
        {
            if (_topNavbarGo != null)
            {
                if (Application.isPlaying) Destroy(_topNavbarGo);
                else DestroyImmediate(_topNavbarGo);
            }

            _topNavbarGo = CreateUIRect("TopNavbar", transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                Vector2.zero, new Vector2(0, 52));
            _topNavbarGo.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.98f);

            var border = CreateUIRect("Border", _topNavbarGo.transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                Vector2.zero, new Vector2(0, 1));
            border.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.30f, 0.8f);

            // Brand & Title
            var brandGo = CreateUIRect("Brand", _topNavbarGo.transform,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(24, 0), new Vector2(340, 36));
            var brandTmp = brandGo.AddComponent<TextMeshProUGUI>();
            brandTmp.text = "<b><color=#00E5FF>PROJECT ATLAS</color></b> <size=75%><color=#64748B>|</color> <color=#94A3B8>3D MEDICAL ATLAS</color></size>";
            brandTmp.fontSize = 15;
            brandTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Right Action Controls (Home, Explore, Settings, Exit)
            var rightActionsGo = CreateUIRect("RightActions", _topNavbarGo.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-24, 0), new Vector2(440, 32));
            var hLayout = rightActionsGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleRight;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;

            CreateNavButton("Home", rightActionsGo.transform, () => {
                if (navigationController != null) navigationController.SelectModule(0);
            });

            CreateNavButton("Explore", rightActionsGo.transform, () => {
                var meta = ModuleAssetRegistry.GetModuleByIndex(0);
                transitionController?.TransitionToScene(meta.targetSceneName);
            });

            CreateNavButton("Settings", rightActionsGo.transform, () => {
                Debug.Log("[MainMenu] Clinical Quality Settings Toggled");
            });

            CreateNavButton("Exit", rightActionsGo.transform, () => {
                Debug.Log("[MainMenu] Exiting Application...");
                Application.Quit();
            }, new Color(0.85f, 0.22f, 0.28f, 0.25f), new Color(1f, 0.45f, 0.5f, 1f));
        }

        private void BuildLeftModuleDeck()
        {
            if (_leftModuleDeckGo != null)
            {
                if (Application.isPlaying) Destroy(_leftModuleDeckGo);
                else DestroyImmediate(_leftModuleDeckGo);
            }

            _leftModuleDeckGo = CreateUIRect("LeftModuleDeck", transform,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(24, -16), new Vector2(310, -104));
            _leftModuleDeckGo.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f, 0.92f);

            var border = CreateUIRect("Border", _leftModuleDeckGo.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            border.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.30f, 0.6f);

            var vLayout = _leftModuleDeckGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(16, 16, 16, 16);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandHeight = false;

            // Deck Header
            var headerGo = CreateUIRect("Header", _leftModuleDeckGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 26));
            var hTmp = headerGo.AddComponent<TextMeshProUGUI>();
            hTmp.text = "<size=75%><color=#00E5FF>[ CLINICAL SIMULATION MODULES ]</color></size>";
            hTmp.fontSize = 11;

            var subHeaderGo = CreateUIRect("SubHeader", _leftModuleDeckGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 18));
            var subTmp = subHeaderGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "<color=#94A3B8>Select module to preview 3D anatomy</color>";
            subTmp.fontSize = 9.5f;

            var div = CreateUIRect("Divider", _leftModuleDeckGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
            div.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.30f, 0.6f);

            // Scroll / Content Root for items
            var listRootGo = CreateUIRect("ListRoot", _leftModuleDeckGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 390));
            var lvLayout = listRootGo.AddComponent<VerticalLayoutGroup>();
            lvLayout.spacing = 6;
            lvLayout.childControlWidth = true;
            lvLayout.childForceExpandWidth = true;
            lvLayout.childControlHeight = false;
            lvLayout.childForceExpandHeight = false;

            // Attach ModuleNavigationController
            navigationController = gameObject.GetComponent<ModuleNavigationController>() ?? gameObject.AddComponent<ModuleNavigationController>();
            // Expose list root to navigation controller via reflection or public method
            var field = typeof(ModuleNavigationController).GetField("moduleListRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(navigationController, listRootGo.transform);
        }

        private void BuildRightContextPanel()
        {
            if (_rightContextPanelGo != null)
            {
                if (Application.isPlaying) Destroy(_rightContextPanelGo);
                else DestroyImmediate(_rightContextPanelGo);
            }

            _rightContextPanelGo = CreateUIRect("RightContextPanel", transform,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-24, -16), new Vector2(340, -104));
            _rightContextPanelGo.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f, 0.94f);

            var border = CreateUIRect("Border", _rightContextPanelGo.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            border.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.30f, 0.6f);

            var vLayout = _rightContextPanelGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(18, 18, 16, 16);
            vLayout.spacing = 10;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            // Category tag
            var tagGo = CreateUIRect("Tag", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 16));
            var tagTmp = tagGo.AddComponent<TextMeshProUGUI>();
            tagTmp.text = "<size=75%><color=#00E5FF>[ ACTIVE MODULE TELEMETRY ]</color></size>";
            tagTmp.fontSize = 9.5f;

            // Title
            var titleGo = CreateUIRect("Title", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 24));
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>FULL ANATOMY EXPLORER</b>";
            titleTmp.fontSize = 15;
            titleTmp.color = Color.white;

            // Subtitle
            var subGo = CreateUIRect("Subtitle", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 18));
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "<color=#94A3B8>Explore the complete human anatomical system.</color>";
            subTmp.fontSize = 10.5f;

            var div1 = CreateUIRect("Div1", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
            div1.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.30f, 0.6f);

            // Description
            var descGo = CreateUIRect("Desc", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 75));
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "Comprehensive full-body anatomical atlas featuring balanced half-muscular and half-skeletal cutaway, visceral internal organs, and cardiovascular network.";
            descTmp.fontSize = 10.5f;
            descTmp.color = new Color(0.85f, 0.88f, 0.92f);
            descTmp.textWrappingMode = TextWrappingModes.Normal;

            // Capabilities
            var capGo = CreateUIRect("Capabilities", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 95));
            var capTmp = capGo.AddComponent<TextMeshProUGUI>();
            capTmp.text = "<color=#00E5FF>Clinical Capabilities:</color>\n• 8 Independent Anatomical Systems\n• Real-Time Depth Layer Slicing\n• Independent Organ Opacity Sliders";
            capTmp.fontSize = 10f;
            capTmp.textWrappingMode = TextWrappingModes.Normal;

            // Specs
            var specGo = CreateUIRect("Specs", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 36));
            var specTmp = specGo.AddComponent<TextMeshProUGUI>();
            specTmp.text = "<size=80%><color=#64748B>Model: Human_Torso_Anatomy_Full.fbx | PBR Lit</color></size>";
            specTmp.fontSize = 9.5f;

            var div2 = CreateUIRect("Div2", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
            div2.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.30f, 0.6f);

            // Primary Action Button (e.g. "EXPLORE ANATOMY")
            var actBtnGo = CreateUIRect("PrimaryActionBtn", _rightContextPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 42));
            var actBtnImg = actBtnGo.AddComponent<Image>();
            actBtnImg.color = new Color(0f, 0.898f, 1f, 1f); // Medical Cyan

            var actBtn = actBtnGo.AddComponent<Button>();
            var actTxtGo = CreateUIRect("Text", actBtnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var actTxt = actTxtGo.AddComponent<TextMeshProUGUI>();
            actTxt.text = "<b>EXPLORE ANATOMY ▶</b>";
            actTxt.fontSize = 12.5f;
            actTxt.alignment = TextAlignmentOptions.Center;
            actTxt.color = Color.black;

            // Wire to Navigation Controller via reflection
            SetPrivateField(navigationController, "contextTitleText", titleTmp);
            SetPrivateField(navigationController, "contextSubtitleText", subTmp);
            SetPrivateField(navigationController, "contextDescriptionText", descTmp);
            SetPrivateField(navigationController, "contextCapabilitiesText", capTmp);
            SetPrivateField(navigationController, "contextSpecsText", specTmp);
            SetPrivateField(navigationController, "contextActionButton", actBtn);
            SetPrivateField(navigationController, "contextActionButtonText", actTxt);
        }

        private void BuildStartupOverlay()
        {
            if (_splashOverlayGo != null)
            {
                if (Application.isPlaying) Destroy(_splashOverlayGo);
                else DestroyImmediate(_splashOverlayGo);
            }

            _splashOverlayGo = CreateUIRect("StartupSplash_Overlay", transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _splashOverlayGo.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 1.0f); // Solid pitch

            var cg = _splashOverlayGo.AddComponent<CanvasGroup>();

            // Center Branding Box
            var centerBoxGo = CreateUIRect("CenterBox", _splashOverlayGo.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20), new Vector2(560, 260));

            var vLayout = centerBoxGo.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            var titleGo = CreateUIRect("BrandTitle", centerBoxGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 36));
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#00E5FF>PROJECT ATLAS</color></b>";
            titleTmp.fontSize = 28;
            titleTmp.alignment = TextAlignmentOptions.Center;

            var subTitleGo = CreateUIRect("SubTitle", centerBoxGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 20));
            var subTmp = subTitleGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "<color=#94A3B8>ADVANCED 3D MEDICAL ATLAS & SURGICAL SIMULATION</color>";
            subTmp.fontSize = 11.5f;
            subTmp.alignment = TextAlignmentOptions.Center;

            // ECG Scanning Line Bar
            var ecgBarGo = CreateUIRect("ECGBar", centerBoxGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 32));
            var ecgBarImg = ecgBarGo.AddComponent<Image>();
            ecgBarImg.color = new Color(0.06f, 0.09f, 0.12f, 0.8f);

            var ecgSweepGo = CreateUIRect("ECGSweepDot", ecgBarGo.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
            var ecgDotImg = ecgSweepGo.AddComponent<Image>();
            ecgDotImg.color = new Color(0f, 0.898f, 1f, 1f);

            // Progress Slider
            var sliderGo = CreateUIRect("ProgressBar", centerBoxGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 8));
            sliderGo.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 1f);
            var slider = sliderGo.AddComponent<Slider>();

            var fillGo = CreateUIRect("Fill", sliderGo.transform, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0f, 0.898f, 1f, 1f);
            slider.fillRect = fillGo.GetComponent<RectTransform>();

            // Status Readout
            var statusGo = CreateUIRect("StatusReadout", centerBoxGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 22));
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "CALIBRATING UNIVERSAL RENDER PIPELINE SHADERS...";
            statusTmp.fontSize = 10.5f;
            statusTmp.color = new Color(0.55f, 0.65f, 0.75f);
            statusTmp.alignment = TextAlignmentOptions.Center;

            // Skip Button at bottom right
            var skipGo = CreateUIRect("SkipBtn", _splashOverlayGo.transform,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-32, 28), new Vector2(110, 28));
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.10f, 0.14f, 0.18f, 0.85f);
            var skipBtn = skipGo.AddComponent<Button>();

            var skipTxt = CreateUIRect("Text", skipGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            skipTxt.text = "<size=80%><color=#94A3B8>SKIP INTRO [ESC]</color></size>";
            skipTxt.fontSize = 9.5f;
            skipTxt.alignment = TextAlignmentOptions.Center;

            // Wire Startup Controller
            startupController = gameObject.GetComponent<StartupSequenceController>() ?? gameObject.AddComponent<StartupSequenceController>();
            SetPrivateField(startupController, "splashCanvasGroup", cg);
            SetPrivateField(startupController, "ecgLineTransform", ecgSweepGo.GetComponent<RectTransform>());
            SetPrivateField(startupController, "progressBar", slider);
            SetPrivateField(startupController, "statusReadoutText", statusTmp);
            SetPrivateField(startupController, "skipButton", skipBtn);
            SetPrivateField(startupController, "presentationController", presentationController);
        }

        private void BuildTransitionOverlay()
        {
            if (_transitionOverlayGo != null)
            {
                if (Application.isPlaying) Destroy(_transitionOverlayGo);
                else DestroyImmediate(_transitionOverlayGo);
            }

            _transitionOverlayGo = CreateUIRect("SceneTransition_Overlay", transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _transitionOverlayGo.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 1.0f);

            var cg = _transitionOverlayGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            _transitionOverlayGo.SetActive(false);

            transitionController = gameObject.GetComponent<SceneTransitionController>() ?? gameObject.AddComponent<SceneTransitionController>();
            SetPrivateField(transitionController, "faderCanvasGroup", cg);
        }

        private void CreateNavButton(string label, Transform parent, UnityEngine.Events.UnityAction action, Color? bgColor = null, Color? textColor = null)
        {
            var btnGo = CreateUIRect($"Btn_{label}", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(85, 28));
            var img = btnGo.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.09f, 0.12f, 0.16f, 0.9f);

            var border = CreateUIRect("Border", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            border.AddComponent<Image>().color = new Color(0.18f, 0.24f, 0.32f, 0.6f);

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor ?? new Color(0.85f, 0.88f, 0.92f, 1f);
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(target, value);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrapMainMenuIfRequested()
        {
            var activeScene = SceneManager.GetActiveScene().name;
            if (activeScene.Equals("01_MainMenu", StringComparison.OrdinalIgnoreCase) ||
                activeScene.Equals("00_Bootstrap", StringComparison.OrdinalIgnoreCase) ||
                activeScene.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                var existing = FindFirstObjectByType<MainMenuController>();
                if (existing == null)
                {
                    var mmGo = new GameObject("ProjectAtlas_MainMenu_Master");
                    existing = mmGo.AddComponent<MainMenuController>();
                }
            }
        }
    }
}
