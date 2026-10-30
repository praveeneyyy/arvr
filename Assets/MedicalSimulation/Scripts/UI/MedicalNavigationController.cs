using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Global application navigation shell.
    /// Provides collapsible sidebar, top status navbar, breadcrumbs, and instant switching
    /// between Anatomy Explorer, Diagnosis Lab, Procedure Lab, Cardiology, and Assessment.
    /// Ensures 65-75% screen space remains fully dedicated to the 3D viewport.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class MedicalNavigationController : MonoBehaviour
    {
        public static MedicalNavigationController Instance { get; private set; }

        private RectTransform _sidebarPanel;
        private RectTransform _topNavbar;
        private TextMeshProUGUI _breadcrumbText;
        private TextMeshProUGUI _sessionStatusText;
        private Button _collapseSidebarBtn;
        private bool _isSidebarCollapsed = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildGlobalShellUI();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateBreadcrumb(scene.name);
        }

        private void BuildGlobalShellUI()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70; // Behind AI chat and Modal dialogs, above 3D world

            var scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // ---------------------------------------------------------
            // 1. TOP NAVBAR (Height: 52px, across top)
            // ---------------------------------------------------------
            var topNavGo = CreateUI("TopNavbar", transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, MedicalUITheme.TopNavbarHeight));
            _topNavbar = topNavGo.GetComponent<RectTransform>();
            var topNavImg = topNavGo.AddComponent<Image>();
            topNavImg.color = MedicalUITheme.SurfaceSecondary;

            // Brand Logo & Product Name
            var brandGo = CreateUI("Brand", topNavGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(260, 36));
            var brandText = brandGo.AddComponent<TextMeshProUGUI>();
            brandText.text = "<b><color=#00E5FF>ProcedureLab</color></b> <size=85%><color=#A0AAB5>| Medical Simulation</color></size>";
            brandText.fontSize = 15;
            brandText.alignment = TextAlignmentOptions.MidlineLeft;
            brandText.color = MedicalUITheme.TextPrimary;

            // Breadcrumbs
            var breadcrumbGo = CreateUI("Breadcrumb", topNavGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(400, 30));
            _breadcrumbText = breadcrumbGo.AddComponent<TextMeshProUGUI>();
            _breadcrumbText.fontSize = 13;
            _breadcrumbText.alignment = TextAlignmentOptions.MidlineLeft;
            _breadcrumbText.color = MedicalUITheme.TextSecondary;
            UpdateBreadcrumb(SceneManager.GetActiveScene().name);

            // Simulation Status / Quality Badge
            var statusGo = CreateUI("StatusBadge", topNavGo.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(280, 32));
            _sessionStatusText = statusGo.AddComponent<TextMeshProUGUI>();
            _sessionStatusText.text = "<color=#2ECC71>● SYSTEM ONLINE</color>  <size=80%><color=#707E8C>| URP Lit 60FPS</color></size>";
            _sessionStatusText.fontSize = 12;
            _sessionStatusText.alignment = TextAlignmentOptions.MidlineRight;
            _sessionStatusText.color = MedicalUITheme.TextSecondary;

            // ---------------------------------------------------------
            // 2. COMPACT DOCKED SIDEBAR (Left: 240px wide)
            // ---------------------------------------------------------
            var sidebarGo = CreateUI("LeftSidebar", transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(0, -MedicalUITheme.TopNavbarHeight / 2f), new Vector2(MedicalUITheme.SidebarWidth, -MedicalUITheme.TopNavbarHeight));
            _sidebarPanel = sidebarGo.GetComponent<RectTransform>();
            var sidebarImg = sidebarGo.AddComponent<Image>();
            sidebarImg.color = MedicalUITheme.SurfaceSecondary;

            // Nav List Layout
            var navContainer = CreateUI("NavItems", sidebarGo.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(-16, -30));
            var vlg = navContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Navigation Entries (Workstation Modules)
            CreateNavButton("Dashboard HQ", "1", navContainer.transform);
            CreateNavButton("Anatomy Explorer", "04_AnatomyLab", navContainer.transform);
            CreateNavButton("Cardiology & Hemodynamics", "heart", navContainer.transform);
            CreateNavButton("Dental Surgical Lab", "Teeth", navContainer.transform);
            CreateNavButton("Diagnosis & Pathology", "05_DiagnosisLab", navContainer.transform);
            CreateNavButton("Procedure Suite", "06_ProcedureLab", navContainer.transform);
            CreateNavButton("Clinical Assessment", "07_FinalAssessment", navContainer.transform);

            // Collapse Sidebar Toggle (Bottom of Sidebar)
            var toggleGo = CreateUI("ToggleSidebar", sidebarGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(-20, 36));
            toggleGo.AddComponent<Image>().color = MedicalUITheme.SurfaceElevated;
            _collapseSidebarBtn = toggleGo.AddComponent<Button>();
            _collapseSidebarBtn.onClick.AddListener(ToggleSidebar);
            var toggleText = CreateUI("Txt", toggleGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            toggleText.text = "<size=85%><color=#00E5FF><<</color> COLLAPSE SIDEBAR</size>";
            toggleText.fontSize = 11;
            toggleText.alignment = TextAlignmentOptions.Center;
            toggleText.color = MedicalUITheme.TextSecondary;
        }

        private void CreateNavButton(string label, string targetScene, Transform parent)
        {
            var btnGo = CreateUI($"NavBtn_{targetScene}", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 42));
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = MedicalUITheme.SurfaceElevated;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = MedicalUITheme.SurfaceElevated;
            colors.highlightedColor = MedicalUITheme.SurfaceHover;
            colors.pressedColor = MedicalUITheme.SurfacePressed;
            btn.colors = colors;

            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[MedicalNavigationController] Loading module: {targetScene}");
                SceneManager.LoadScene(targetScene);
            });

            var textGo = CreateUI("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16, 0), new Vector2(-20, 0));
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = MedicalUITheme.TextPrimary;
        }

        public void ToggleSidebar()
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            if (_sidebarPanel != null)
            {
                float targetW = _isSidebarCollapsed ? MedicalUITheme.SidebarCollapsedWidth : MedicalUITheme.SidebarWidth;
                _sidebarPanel.sizeDelta = new Vector2(targetW, _sidebarPanel.sizeDelta.y);
            }
        }

        public void SetSidebarVisible(bool visible)
        {
            if (_sidebarPanel != null)
            {
                _sidebarPanel.gameObject.SetActive(visible);
            }
        }

        private void UpdateBreadcrumb(string sceneName)
        {
            if (_breadcrumbText == null) return;
            string readableName = sceneName switch
            {
                "1" => "Workstation HQ / Main Dashboard",
                "04_AnatomyLab" => "Workstation / Anatomy Explorer (Full Body 3D)",
                "heart" => "Workstation / Cardiology & Hemodynamics",
                "Teeth" => "Workstation / Dental Surgical Simulation",
                "05_DiagnosisLab" => "Workstation / Clinical Diagnosis & Pathology",
                "06_ProcedureLab" => "Workstation / Surgical Procedure Lab",
                "07_FinalAssessment" => "Workstation / Residency Clinical Assessment",
                _ => $"Workstation / {sceneName}"
            };
            _breadcrumbText.text = $"<color=#707E8C>Module:</color> <color=#FFFFFF>{readableName}</color>";
        }

        private GameObject CreateUI(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }
    }
}
