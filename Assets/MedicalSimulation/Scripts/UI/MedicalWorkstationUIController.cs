using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;
using MedicalSimulation.AI;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Master UI/UX Workstation Controller for ProcedureLab.
    /// Completely replaces generic/sci-fi UI with a premium, minimal, sophisticated
    /// clinical medical visualization workstation interface.
    /// Works seamlessly at Edit Time ([ExecuteAlways]) and Runtime.
    /// </summary>
    [ExecuteAlways]
    public class MedicalWorkstationUIController : MonoBehaviour
    {
        public static MedicalWorkstationUIController Instance { get; private set; }

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GraphicRaycaster _raycaster;

        // UI Components
        private GameObject _topNavbarGo;
        private GameObject _sidebarGo;
        private GameObject _dashboardOverlayGo;
        private GameObject _anatomySelectorGo;
        private GameObject _inspectionPanelGo;
        private GameObject _bottomToolbarGo;
        private GameObject _teamMembersGo;
        private GameObject _aiPillBtnGo;

        private bool _isSidebarCollapsed = false;

        void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            InitializeWorkstation();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeWorkstation();
        }

        [ContextMenu("Rebuild Workstation Interface")]
        public void InitializeWorkstation()
        {
            // 1. Purge legacy obsolete UI
            PurgeLegacyUI();

            // 2. Setup 3D Anatomy & Studio Lighting Rig in Scene
            Ensure3DAnatomyViewportRig();

            // 3. Ensure Canvas
            SetupCanvas();

            // 4. Build Core Workstation Elements
            BuildTopNavbar();
            BuildLeftSidebar();
            BuildSceneSpecificUI();
            BuildAIPillLauncher();
        }

        private void PurgeLegacyUI()
        {
            // Look for old canvases or game objects that carried legacy buttons or raw text
            string[] legacyNames = new string[] {
                "Button  1", "Button 1", "Button 2", "Button 3",
                "Button dental", "Button nose", "Button liver", "Button nasal",
                "Panel", "AI_Launcher_Button"
            };

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.gameObject == gameObject) continue;
                if (canvas.name == "MedicalWorkstation_Canvas") continue;

                // Scan children
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

                    // Check for old text
                    var tmp = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null && (tmp.text.Contains("WELCOME TO MEDICAL VR") || tmp.text.Contains("Team 11") || tmp.text.Contains("Functionalities")))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void Ensure3DAnatomyViewportRig()
        {
            // Check if Main Camera exists
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0, 0.4f, -2.4f);
            }

            // Set camera background to clinical charcoal
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MedicalUITheme.BackgroundDeep;

            // Ensure smooth camera controller
            var camCtrl = cam.GetComponent<AnatomyCameraController>() ?? cam.gameObject.AddComponent<AnatomyCameraController>();

            // Check if 3D anatomy exists in scene
            var existingAnatomy = FindFirstObjectByType<AnatomyModelController>();
            if (existingAnatomy == null)
            {
                var anatomyRootGo = new GameObject("3D_Human_Anatomy_MasterRig");
                anatomyRootGo.transform.position = new Vector3(0.15f, -0.4f, 0f);
                anatomyRootGo.transform.localScale = Vector3.one * 1.0f;

                existingAnatomy = anatomyRootGo.AddComponent<AnatomyModelController>();
                var assemblyBuilder = anatomyRootGo.AddComponent<AnatomyAssemblyBuilder>();
                anatomyRootGo.AddComponent<AnatomicalSlicePlaneController>();

                assemblyBuilder.BuildAnatomicalModel();
            }

            // Ensure Studio Clinical Lighting (Key, Fill, Rim)
            EnsureStudioLighting();
        }

        private void EnsureStudioLighting()
        {
            var existingLight = FindFirstObjectByType<Light>();
            if (existingLight == null)
            {
                // Key Light
                var keyLightGo = new GameObject("Key_Light_Clinical");
                var keyLight = keyLightGo.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.95f, 0.98f, 1.0f);
                keyLight.intensity = 1.1f;
                keyLightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

                // Fill Light
                var fillLightGo = new GameObject("Fill_Light_Soft");
                var fillLight = fillLightGo.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = new Color(0.6f, 0.75f, 0.9f);
                fillLight.intensity = 0.5f;
                fillLightGo.transform.rotation = Quaternion.Euler(20f, 140f, 0f);

                // Rim Light
                var rimLightGo = new GameObject("Rim_Light_Cyan");
                var rimLight = rimLightGo.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = new Color(0.0f, 0.8f, 0.95f);
                rimLight.intensity = 0.6f;
                rimLightGo.transform.rotation = Quaternion.Euler(-25f, -160f, 0f);
            }
        }

        private void SetupCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null) _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            _raycaster = GetComponent<GraphicRaycaster>();
            if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        // =========================================================================
        // TOP NAVBAR
        // =========================================================================

        private void BuildTopNavbar()
        {
            if (_topNavbarGo != null)
            {
                if (Application.isPlaying) Destroy(_topNavbarGo);
                else DestroyImmediate(_topNavbarGo);
            }

            _topNavbarGo = CreateUIRect("TopNavbar", transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, MedicalUITheme.TopNavbarHeight));
            var bg = _topNavbarGo.AddComponent<Image>();
            bg.color = MedicalUITheme.SurfaceSecondary;

            // Brand Logo & Title
            var brandGo = CreateUIRect("BrandLogo", _topNavbarGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(300, 36));
            var brandTmp = brandGo.AddComponent<TextMeshProUGUI>();
            brandTmp.text = "<b><color=#00E5FF>ProcedureLab</color></b> <size=80%><color=#94A3B8>| Clinical Medical Workstation</color></size>";
            brandTmp.fontSize = 15;
            brandTmp.alignment = TextAlignmentOptions.MidlineLeft;
            brandTmp.color = MedicalUITheme.TextPrimary;

            // Breadcrumbs
            string activeScene = SceneManager.GetActiveScene().name;
            var breadcrumbGo = CreateUIRect("Breadcrumb", _topNavbarGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(340, 0), new Vector2(450, 30));
            var breadcrumbTmp = breadcrumbGo.AddComponent<TextMeshProUGUI>();
            breadcrumbTmp.text = $"<color=#64748B>Workspace /</color> <color=#00E5FF>{FormatSceneName(activeScene)}</color>";
            breadcrumbTmp.fontSize = 13;
            breadcrumbTmp.alignment = TextAlignmentOptions.MidlineLeft;
            breadcrumbTmp.color = MedicalUITheme.TextSecondary;

            // Simulation Status Badge
            var statusGo = CreateUIRect("StatusBadge", _topNavbarGo.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(360, 32));
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "<color=#14B8A6>● SYSTEM OPERATIONAL</color> <size=80%><color=#64748B>| 60 FPS URP 6</color></size>";
            statusTmp.fontSize = 12;
            statusTmp.alignment = TextAlignmentOptions.MidlineRight;
            statusTmp.color = MedicalUITheme.TextSecondary;
        }

        // =========================================================================
        // LEFT NAVIGATION SIDEBAR
        // =========================================================================

        private void BuildLeftSidebar()
        {
            if (_sidebarGo != null)
            {
                if (Application.isPlaying) Destroy(_sidebarGo);
                else DestroyImmediate(_sidebarGo);
            }

            _sidebarGo = CreateUIRect("LeftSidebar", transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(0, -MedicalUITheme.TopNavbarHeight / 2), new Vector2(MedicalUITheme.SidebarWidth, -MedicalUITheme.TopNavbarHeight));
            var bg = _sidebarGo.AddComponent<Image>();
            bg.color = MedicalUITheme.SurfaceSecondary;

            var layout = _sidebarGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 20, 20);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Sidebar Header
            var sectionLabelGo = CreateUIRect("SectionLabel", _sidebarGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 24));
            var sectionTmp = sectionLabelGo.AddComponent<TextMeshProUGUI>();
            sectionTmp.text = "<size=75%><color=#64748B>CLINICAL MODULES</color></size>";
            sectionTmp.fontSize = 12;
            sectionTmp.alignment = TextAlignmentOptions.Left;

            // Navigation Entries
            CreateSidebarButton("01  Dashboard", "1", _sidebarGo.transform);
            CreateSidebarButton("02  Anatomy Explorer", "04_AnatomyLab", _sidebarGo.transform);
            CreateSidebarButton("03  Cardiology Suite", "heart", _sidebarGo.transform);
            CreateSidebarButton("04  Dental Procedure", "Teeth", _sidebarGo.transform);
            CreateSidebarButton("05  Soft Tissue Surgery", "Liver Cut", _sidebarGo.transform);
            CreateSidebarButton("06  Diagnosis Lab", "05_DiagnosisLab", _sidebarGo.transform);
            CreateSidebarButton("07  Procedure Lab", "06_ProcedureLab", _sidebarGo.transform);
            CreateSidebarButton("08  Final Assessment", "07_FinalAssessment", _sidebarGo.transform);

            // Divider
            var divGo = CreateUIRect("Divider", _sidebarGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 2));
            divGo.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            // AI Assistant Toggle Button
            var aiBtnGo = CreateSidebarButton("AI Assistant [HUD]", null, _sidebarGo.transform, () => {
                var aiChat = MedicalAIChatUI.EnsureInstance();
                if (aiChat != null) aiChat.TogglePanel();
            });

            // Collapse Sidebar Button
            CreateSidebarButton("Collapse Sidebar", null, _sidebarGo.transform, ToggleSidebar);
        }

        private GameObject CreateSidebarButton(string label, string targetScene, Transform parent, UnityEngine.Events.UnityAction customAction = null)
        {
            var btnGo = CreateUIRect(label.Replace(" ", "_"), parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 42));
            var img = btnGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceElevated;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = MedicalUITheme.SurfaceElevated;
            colors.highlightedColor = MedicalUITheme.SurfaceHover;
            colors.pressedColor = MedicalUITheme.SurfacePressed;
            colors.selectedColor = MedicalUITheme.SurfaceElevated;
            btn.colors = colors;

            if (customAction != null)
            {
                btn.onClick.AddListener(customAction);
            }
            else if (!string.IsNullOrEmpty(targetScene))
            {
                btn.onClick.AddListener(() => {
                    SceneManager.LoadScene(targetScene);
                });
            }

            var txtGo = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(14, 0, 0, 0);
            tmp.color = MedicalUITheme.TextPrimary;

            // Highlight if active scene
            string curScene = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(targetScene) && curScene.Equals(targetScene, System.StringComparison.OrdinalIgnoreCase))
            {
                img.color = MedicalUITheme.SurfaceHover;
                tmp.color = MedicalUITheme.AccentCyan;
            }

            return btnGo;
        }

        private void ToggleSidebar()
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            var rt = _sidebarGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(_isSidebarCollapsed ? MedicalUITheme.SidebarCollapsedWidth : MedicalUITheme.SidebarWidth, -MedicalUITheme.TopNavbarHeight);

            // Hide text when collapsed
            foreach (var tmp in _sidebarGo.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (tmp.gameObject.name == "Text")
                {
                    tmp.enabled = !_isSidebarCollapsed;
                }
            }
        }

        // =========================================================================
        // SCENE SPECIFIC WORKSPACE (DASHBOARD vs ANATOMY EXPLORER)
        // =========================================================================

        private void BuildSceneSpecificUI()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            // Cleanup previous views
            if (_dashboardOverlayGo != null)
            {
                if (Application.isPlaying) Destroy(_dashboardOverlayGo);
                else DestroyImmediate(_dashboardOverlayGo);
            }
            if (_anatomySelectorGo != null)
            {
                if (Application.isPlaying) Destroy(_anatomySelectorGo);
                else DestroyImmediate(_anatomySelectorGo);
            }
            if (_inspectionPanelGo != null)
            {
                if (Application.isPlaying) Destroy(_inspectionPanelGo);
                else DestroyImmediate(_inspectionPanelGo);
            }
            if (_bottomToolbarGo != null)
            {
                if (Application.isPlaying) Destroy(_bottomToolbarGo);
                else DestroyImmediate(_bottomToolbarGo);
            }
            if (_teamMembersGo != null)
            {
                if (Application.isPlaying) Destroy(_teamMembersGo);
                else DestroyImmediate(_teamMembersGo);
            }

            if (sceneName == "1" || sceneName == "1 1" || sceneName == "1 2" || sceneName.Contains("1"))
            {
                BuildDashboardOverlay();
                BuildTeamMembersFooter();
            }
            else if (sceneName == "04_AnatomyLab")
            {
                BuildAnatomyExplorerHUD();
            }
            else
            {
                // Specialized Clinical Lab (Cardiology, Dental, Procedure, etc.)
                BuildSpecializedLabHUD(sceneName);
            }
        }

        // =========================================================================
        // DASHBOARD OVERLAY (Main Menu)
        // =========================================================================

        private void BuildDashboardOverlay()
        {
            _dashboardOverlayGo = CreateUIRect("DashboardOverlay", transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _dashboardOverlayGo.AddComponent<CanvasGroup>().blocksRaycasts = true;

            // Hero Typography (Top Left of workspace)
            var heroGo = CreateUIRect("HeroSection", _dashboardOverlayGo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(MedicalUITheme.SidebarWidth + 40, -MedicalUITheme.TopNavbarHeight - 30), new Vector2(580, 180));
            var heroTitle = CreateUIRect("Title", heroGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 48)).AddComponent<TextMeshProUGUI>();
            heroTitle.text = "<b><color=#FFFFFF>ProcedureLab</color></b>";
            heroTitle.fontSize = 32;
            heroTitle.alignment = TextAlignmentOptions.Left;

            var heroSub = CreateUIRect("Subtitle", heroGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, -48), new Vector2(0, 32)).AddComponent<TextMeshProUGUI>();
            heroSub.text = "<color=#94A3B8>Medical Visualization & AR/VR Clinical Simulation</color>";
            heroSub.fontSize = 15;
            heroSub.alignment = TextAlignmentOptions.Left;

            // Hero CTA Buttons
            var btnContainer = CreateUIRect("ActionButtons", heroGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 10), new Vector2(0, 46));
            var hLayout = btnContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 14;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = true;

            CreateActionButton("Explore 3D Anatomy", MedicalUITheme.AccentCyan, Color.black, btnContainer.transform, () => {
                SceneManager.LoadScene("04_AnatomyLab");
            });

            CreateActionButton("Continue Clinical Residency", MedicalUITheme.SurfaceElevated, MedicalUITheme.TextPrimary, btnContainer.transform, () => {
                SceneManager.LoadScene("heart");
            });

            // Story Campaign Level Cards (Bottom tray)
            BuildCampaignLevelCards(_dashboardOverlayGo.transform);
        }

        private void BuildCampaignLevelCards(Transform parent)
        {
            var trayGo = CreateUIRect("StoryLevelCardsTray", parent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(MedicalUITheme.SidebarWidth / 2, 70), new Vector2(-MedicalUITheme.SidebarWidth - 60, 120));
            var hLayout = trayGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 12;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = true;

            CreateLevelCard("LEVEL 1", "Anatomy Explorer", "04_AnatomyLab", "Full human torso & internal organ systems", trayGo.transform);
            CreateLevelCard("LEVEL 2", "Cardiology Suite", "heart", "Hemodynamics & cardiac cycle", trayGo.transform);
            CreateLevelCard("LEVEL 3", "Dental Surgery", "Teeth", "Maxillofacial tooth extraction", trayGo.transform);
            CreateLevelCard("LEVEL 4", "Soft Tissue Resection", "Liver Cut", "Hepatic surgical cutting", trayGo.transform);
            CreateLevelCard("LEVEL 5", "Clinical Diagnosis", "05_DiagnosisLab", "Patient pathology & vitals", trayGo.transform);
            CreateLevelCard("LEVEL 6", "Procedure Suite", "06_ProcedureLab", "Surgical instrumentation", trayGo.transform);
        }

        private void CreateLevelCard(string levelCode, string title, string sceneName, string description, Transform parent)
        {
            var cardGo = CreateUIRect(title.Replace(" ", "_"), parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 110));
            var img = cardGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceSecondary;

            var btn = cardGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = MedicalUITheme.SurfaceSecondary;
            colors.highlightedColor = MedicalUITheme.SurfaceHover;
            colors.pressedColor = MedicalUITheme.SurfacePressed;
            btn.colors = colors;
            btn.onClick.AddListener(() => SceneManager.LoadScene(sceneName));

            var vLayout = cardGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(12, 12, 10, 10);
            vLayout.spacing = 4;

            var codeTmp = CreateUIRect("Code", cardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 16)).AddComponent<TextMeshProUGUI>();
            codeTmp.text = $"<color=#00E5FF><b>{levelCode}</b></color>";
            codeTmp.fontSize = 10;

            var titleTmp = CreateUIRect("Title", cardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 20)).AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"<b>{title}</b>";
            titleTmp.fontSize = 12;
            titleTmp.color = MedicalUITheme.TextPrimary;

            var descTmp = CreateUIRect("Desc", cardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 32)).AddComponent<TextMeshProUGUI>();
            descTmp.text = description;
            descTmp.fontSize = 9;
            descTmp.color = MedicalUITheme.TextSecondary;
        }

        private void BuildTeamMembersFooter()
        {
            _teamMembersGo = CreateUIRect("TeamMembersFooter", transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(MedicalUITheme.SidebarWidth / 2, 16), new Vector2(1000, 36));
            var img = _teamMembersGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceSecondary;

            var tmp = CreateUIRect("Text", _teamMembersGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = "<b><color=#00E5FF>Team Members:</color></b>  <color=#F1F5F9>Sanjith</color>  |  <color=#F1F5F9>Thrish</color>  |  <color=#F1F5F9>Praveen</color>  |  <color=#F1F5F9>Raghav</color>  |  <color=#F1F5F9>Raghaveshwara Karthikeyan</color>  |  <color=#F1F5F9>Vaishnav</color>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MedicalUITheme.TextSecondary;
        }

        // =========================================================================
        // ANATOMY EXPLORER HUD (04_AnatomyLab)
        // =========================================================================

        private void BuildAnatomyExplorerHUD()
        {
            // 1. Left Anatomical Systems Selector (Docked inside or beside sidebar)
            BuildSystemsSelector();

            // 2. Right Contextual Organ Inspection Panel
            BuildOrganInspectionHUD();

            // 3. Bottom 3D Manipulation Toolbar
            _bottomToolbarGo = ViewportToolbar.AttachTo(transform).gameObject;
        }

        private void BuildSystemsSelector()
        {
            _anatomySelectorGo = CreateUIRect("AnatomySystemsSelector", transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(MedicalUITheme.SidebarWidth + 16, 0), new Vector2(170, 380));
            var img = _anatomySelectorGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceSecondary;

            var vLayout = _anatomySelectorGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 12, 12);
            vLayout.spacing = 6;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;

            var titleTmp = CreateUIRect("Header", _anatomySelectorGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 20)).AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<size=75%><color=#64748B>ANATOMY SYSTEMS</color></size>";
            titleTmp.fontSize = 11;

            var anatomyCtrl = FindFirstObjectByType<AnatomyModelController>();

            CreateSystemBtn("Full Body", AnatomicalSystem.None, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Skeletal", AnatomicalSystem.Skeletal, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Muscular", AnatomicalSystem.Muscular, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Circulatory", AnatomicalSystem.Cardiovascular, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Respiratory", AnatomicalSystem.Respiratory, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Digestive", AnatomicalSystem.Digestive, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Urinary", AnatomicalSystem.Urinary, _anatomySelectorGo.transform, anatomyCtrl);
            CreateSystemBtn("Nervous", AnatomicalSystem.Nervous, _anatomySelectorGo.transform, anatomyCtrl);
        }

        private void CreateSystemBtn(string label, AnatomicalSystem system, Transform parent, AnatomyModelController ctrl)
        {
            var btnGo = CreateUIRect(label, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 32));
            var img = btnGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceElevated;

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                if (ctrl != null)
                {
                    if (system == AnatomicalSystem.None) ctrl.ShowAllSystems();
                    else ctrl.ShowSystem(system);
                }
            });

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MedicalUITheme.TextPrimary;
        }

        private void BuildOrganInspectionHUD()
        {
            _inspectionPanelGo = CreateUIRect("ContextualInspectionPanel", transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-20, -MedicalUITheme.TopNavbarHeight / 2), new Vector2(MedicalUITheme.InspectionPanelWidth, -MedicalUITheme.TopNavbarHeight - 40));
            var img = _inspectionPanelGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceSecondary;

            var panelScript = _inspectionPanelGo.GetComponent<OrganInspectionPanel>() ?? _inspectionPanelGo.AddComponent<OrganInspectionPanel>();
        }

        private void BuildSpecializedLabHUD(string sceneName)
        {
            var hudGo = CreateUIRect("LabHUD_" + sceneName, transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -MedicalUITheme.TopNavbarHeight - 20), new Vector2(340, 180));
            var img = hudGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceSecondary;

            var vLayout = hudGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(16, 16, 14, 14);
            vLayout.spacing = 8;

            var titleTmp = CreateUIRect("Title", hudGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 24)).AddComponent<TextMeshProUGUI>();
            titleTmp.text = $"<color=#00E5FF><b>{FormatSceneName(sceneName)}</b></color>";
            titleTmp.fontSize = 14;

            var descTmp = CreateUIRect("Desc", hudGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 50)).AddComponent<TextMeshProUGUI>();
            descTmp.text = "Interactive surgical simulation mode. Use mouse orbit/zoom to inspect tissue and instruments.";
            descTmp.fontSize = 11;
            descTmp.color = MedicalUITheme.TextSecondary;

            CreateActionButton("Return to Dashboard", MedicalUITheme.SurfaceElevated, MedicalUITheme.TextPrimary, hudGo.transform, () => {
                SceneManager.LoadScene("1");
            });
        }

        private void BuildAIPillLauncher()
        {
            if (_aiPillBtnGo != null)
            {
                if (Application.isPlaying) Destroy(_aiPillBtnGo);
                else DestroyImmediate(_aiPillBtnGo);
            }

            _aiPillBtnGo = CreateUIRect("ClinicalAI_PillLauncher", transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 20), new Vector2(190, 42));
            var img = _aiPillBtnGo.AddComponent<Image>();
            img.color = MedicalUITheme.SurfaceElevated;

            var btn = _aiPillBtnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                var aiChat = MedicalAIChatUI.EnsureInstance();
                if (aiChat != null) aiChat.TogglePanel();
            });

            var tmp = CreateUIRect("Text", _aiPillBtnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = "<b><color=#00E5FF>◉</color> AI ASSISTANT</b>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MedicalUITheme.TextPrimary;
        }

        private GameObject CreateActionButton(string label, Color bgColor, Color textColor, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var btnGo = CreateUIRect(label.Replace(" ", "_"), parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 42));
            var img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;

            return btnGo;
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

        private string FormatSceneName(string raw)
        {
            if (raw == "1" || raw == "1 1" || raw == "1 2") return "Dashboard // Clinical Overview";
            if (raw == "04_AnatomyLab") return "Anatomy Explorer // Full Torso";
            if (raw == "05_DiagnosisLab") return "Diagnosis Lab // Case Studies";
            if (raw == "06_ProcedureLab") return "Procedure Lab // Surgical Suite";
            if (raw == "07_FinalAssessment") return "Assessment // Board Exam";
            if (raw == "heart") return "Cardiology // Hemodynamics";
            if (raw == "Teeth") return "Dental Lab // Oral Surgery";
            if (raw == "Liver Cut") return "Soft Tissue // Hepatic Resection";
            return raw;
        }
    }
}
