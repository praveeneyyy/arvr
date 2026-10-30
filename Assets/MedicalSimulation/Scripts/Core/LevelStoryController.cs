using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Story-driven Campaign & Level progression system.
    /// Turns the medical AR/VR simulator into an interactive, gamified medical journey
    /// with objectives, mission briefings, clinical alerts, and level advancement.
    /// </summary>
    public class LevelStoryController : MonoBehaviour
    {
        public static LevelStoryController Instance { get; private set; }

        [System.Serializable]
        public class StoryLevel
        {
            public int levelNumber;
            public string levelName;
            public string sceneName;
            [TextArea(2, 4)] public string briefing;
            public string primaryObjective;
            public string hint;
            public bool isUnlocked;
            public bool isCompleted;
            public int targetScore;
        }

        [Header("Campaign Narrative")]
        [SerializeField] private string campaignTitle = "Operation Caduceus: Clinical Medical Residency";
        [SerializeField] private List<StoryLevel> levels = new List<StoryLevel>();

        [Header("Runtime State")]
        public int currentLevelIndex = 0;
        private Canvas storyCanvas;
        private GameObject briefingPanel;
        private TextMeshProUGUI levelTitleText;
        private TextMeshProUGUI briefingBodyText;
        private TextMeshProUGUI objectiveText;
        private TextMeshProUGUI hintText;
        private Button advanceButton;
        private Button toggleBriefingButton;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultCampaign();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            DetectCurrentSceneLevel();
            BuildHUDOverlay();
            ShowMissionBriefing(currentLevelIndex);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void InitializeDefaultCampaign()
        {
            if (levels != null && levels.Count > 0) return;

            levels = new List<StoryLevel>
            {
                new StoryLevel
                {
                    levelNumber = 1,
                    levelName = "Level 1: Full-Body Anatomy Orientation",
                    sceneName = "04_AnatomyLab",
                    briefing = "Welcome to the Medical Simulation Lab, Doctor. Your clinical residency begins here. Inspect the 3D human torso, isolate vital physiological systems (Skeletal, Cardiovascular, Digestive, Respiratory), and test the multi-planar cross-section slice tool.",
                    primaryObjective = "Inspect and select at least 3 major anatomical organs. Ask the Clinical AI Assistant about their physiological functions.",
                    hint = "Click on any organ to inspect. Press 'Space' to toggle Exploded View, or press 'H' to consult your AI assistant.",
                    isUnlocked = true,
                    targetScore = 100
                },
                new StoryLevel
                {
                    levelNumber = 2,
                    levelName = "Level 2: Cardiac Electrophysiology & Hemodynamics",
                    sceneName = "heart",
                    briefing = "Emergency in Cardiology! A patient presents with sinus tachycardia and valve irregularities. Examine the 4-chamber animated heart, adjust heart rate telemetry, and observe systemic blood flow.",
                    primaryObjective = "Analyze systolic and diastolic deformation curves. Query the AI on valve mechanics and stroke volume.",
                    hint = "Look at the pulsating myocardium. Ask the AI: 'Explain the heart valves and cardiac cycle'.",
                    isUnlocked = true,
                    targetScore = 100
                },
                new StoryLevel
                {
                    levelNumber = 3,
                    levelName = "Level 3: Dental Surgical Lab & Tooth Extraction",
                    sceneName = "Teeth",
                    briefing = "The patient requires surgical intervention on an impacted lower molar. Maintain sterility, identify alveolar landmarks, administer local anesthesia, and execute the atraumatic extraction.",
                    primaryObjective = "Complete the 5-step extraction procedure while preventing cortical plate fracture and nerve impingement.",
                    hint = "Use your surgical instruments. Check the Dental HUD metrics for tooth mobility and root elevation.",
                    isUnlocked = true,
                    targetScore = 100
                },
                new StoryLevel
                {
                    levelNumber = 4,
                    levelName = "Level 4: Diagnostic Pathology & Final Assessment",
                    sceneName = "05_DiagnosisLab",
                    briefing = "Final Board Examination. You are presented with complex physiological anomalies. Review vital signs, examine pathology notes with your AI assistant, and finalize your differential diagnosis.",
                    primaryObjective = "Review the clinical case study, achieve a passing diagnostic score (>80%), and finish your residency.",
                    hint = "Ask the AI assistant for a 'Practice Quiz' to review high-yield diagnostic questions.",
                    isUnlocked = true,
                    targetScore = 100
                }
            };
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DetectCurrentSceneLevel();
            BuildHUDOverlay();
            ShowMissionBriefing(currentLevelIndex);
        }

        private void DetectCurrentSceneLevel()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].sceneName == currentScene)
                {
                    currentLevelIndex = i;
                    return;
                }
            }
        }

        private void BuildHUDOverlay()
        {
            // Create Screen Overlay Canvas for Story HUD
            var canvasGo = new GameObject("LevelStory_HUD_Canvas");
            canvasGo.transform.SetParent(transform, false);

            storyCanvas = canvasGo.AddComponent<Canvas>();
            storyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            storyCanvas.sortingOrder = 80;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Floating Top Banner: Mission Badge
            var bannerGo = CreateUIElement("MissionBadge", canvasGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25, -20), new Vector2(360, 48));
            var bannerImg = bannerGo.AddComponent<Image>();
            bannerImg.color = new Color(0.04f, 0.12f, 0.22f, 0.95f);
            var bannerBtn = bannerGo.AddComponent<Button>();
            bannerBtn.onClick.AddListener(() => ToggleBriefing());

            var bannerTextGo = CreateUIElement("Text", bannerGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bannerTmp = bannerTextGo.AddComponent<TextMeshProUGUI>();
            string currentName = (levels != null && currentLevelIndex < levels.Count) ? levels[currentLevelIndex].levelName : "Clinical Simulation Mode";
            bannerTmp.text = $"<color=#00E5FF><b>MISSION BRIEFING:</b></color> <size=85%>{currentName}</size>";
            bannerTmp.fontSize = 13;
            bannerTmp.alignment = TextAlignmentOptions.Center;
            bannerTmp.color = Color.white;

            // Mission Briefing Dialog Window (Center Modal)
            briefingPanel = CreateUIElement("MissionBriefingDialog", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 380));
            var diagImg = briefingPanel.AddComponent<Image>();
            diagImg.color = new Color(0.03f, 0.07f, 0.13f, 0.98f);

            // Dialog Header
            var dHeader = CreateUIElement("Header", briefingPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(0, 50));
            dHeader.AddComponent<Image>().color = new Color(0.06f, 0.14f, 0.25f, 1.0f);

            var titleGo = CreateUIElement("TitleText", dHeader.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(16, 0), new Vector2(-32, 0));
            levelTitleText = titleGo.AddComponent<TextMeshProUGUI>();
            levelTitleText.fontSize = 17;
            levelTitleText.fontStyle = FontStyles.Bold;
            levelTitleText.color = new Color(0.0f, 0.9f, 1.0f);
            levelTitleText.alignment = TextAlignmentOptions.Left;

            // Dialog Content Layout
            var contentArea = CreateUIElement("ContentArea", briefingPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(-40, -110));
            var vlg = contentArea.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Briefing Text
            var bTextGo = CreateUIElement("BriefingText", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 80));
            briefingBodyText = bTextGo.AddComponent<TextMeshProUGUI>();
            briefingBodyText.fontSize = 13;
            briefingBodyText.color = new Color(0.85f, 0.92f, 0.98f);

            // Objective Box
            var objBox = CreateUIElement("ObjectiveBox", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 55));
            objBox.AddComponent<Image>().color = new Color(0.06f, 0.18f, 0.25f, 0.9f);
            var objTextGo = CreateUIElement("ObjText", objBox.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(-20, 0));
            objectiveText = objTextGo.AddComponent<TextMeshProUGUI>();
            objectiveText.fontSize = 12;
            objectiveText.color = new Color(0.2f, 1.0f, 0.6f);

            // Hint Text
            var hintBox = CreateUIElement("HintBox", contentArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 36));
            hintBox.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.20f, 0.8f);
            var hTextGo = CreateUIElement("HText", hintBox.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(-20, 0));
            hintText = hTextGo.AddComponent<TextMeshProUGUI>();
            hintText.fontSize = 11;
            hintText.color = new Color(1.0f, 0.85f, 0.4f);

            // Button Bar (Bottom)
            var btnBar = CreateUIElement("ButtonBar", briefingPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 25), new Vector2(-40, 44));
            var hlg = btnBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;

            // Start Mission Button
            var startBtnGo = CreateUIElement("StartBtn", btnBar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            startBtnGo.AddComponent<Image>().color = new Color(0.0f, 0.75f, 0.9f);
            var startBtn = startBtnGo.AddComponent<Button>();
            startBtn.onClick.AddListener(() => briefingPanel.SetActive(false));
            var sTxt = CreateUIElement("Txt", startBtnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            sTxt.text = "<b>BEGIN OPERATION</b>";
            sTxt.fontSize = 13;
            sTxt.alignment = TextAlignmentOptions.Center;
            sTxt.color = new Color(0.02f, 0.05f, 0.1f);

            // Next Level Button
            var nextBtnGo = CreateUIElement("NextBtn", btnBar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            nextBtnGo.AddComponent<Image>().color = new Color(0.12f, 0.28f, 0.40f);
            advanceButton = nextBtnGo.AddComponent<Button>();
            advanceButton.onClick.AddListener(AdvanceToNextLevel);
            var nTxt = CreateUIElement("Txt", nextBtnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            nTxt.text = "<b>NEXT MISSION -></b>";
            nTxt.fontSize = 13;
            nTxt.alignment = TextAlignmentOptions.Center;
            nTxt.color = Color.white;

            // Main Menu Button
            var menuBtnGo = CreateUIElement("MenuBtn", btnBar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            menuBtnGo.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.15f);
            var menuBtn = menuBtnGo.AddComponent<Button>();
            menuBtn.onClick.AddListener(() => SceneManager.LoadScene("1"));
            var mTxt = CreateUIElement("Txt", menuBtnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            mTxt.text = "<b>HQ / MENU</b>";
            mTxt.fontSize = 13;
            mTxt.alignment = TextAlignmentOptions.Center;
            mTxt.color = new Color(1.0f, 0.7f, 0.7f);
        }

        public void ShowMissionBriefing(int levelIndex)
        {
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Count) return;
            var level = levels[levelIndex];

            if (levelTitleText != null) levelTitleText.text = $"MISSION {level.levelNumber}: {level.levelName.ToUpper()}";
            if (briefingBodyText != null) briefingBodyText.text = level.briefing;
            if (objectiveText != null) objectiveText.text = $"<b>PRIMARY OBJECTIVE:</b> {level.primaryObjective}";
            if (hintText != null) hintText.text = $"<b>TACTICAL HINT:</b> {level.hint}";

            if (briefingPanel != null) briefingPanel.SetActive(true);
        }

        public void ToggleBriefing()
        {
            if (briefingPanel != null)
            {
                briefingPanel.SetActive(!briefingPanel.activeSelf);
            }
        }

        public void AdvanceToNextLevel()
        {
            int nextIndex = (currentLevelIndex + 1) % levels.Count;
            currentLevelIndex = nextIndex;
            string targetScene = levels[nextIndex].sceneName;
            Debug.Log($"[LevelStoryController] Advancing to Story Level {nextIndex + 1}: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }

        private GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
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
