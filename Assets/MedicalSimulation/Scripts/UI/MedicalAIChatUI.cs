using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.AI;
using MedicalSimulation.Core;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Clinical Dark-themed UI for the Medical Simulation AI Assistant.
    /// Supports desktop overlay canvas and VR world-space interaction.
    /// Provides conversation history, live organ context pills, quick prompt chips,
    /// voice dictation meters, and audio controls.
    /// </summary>
    public class MedicalAIChatUI : MonoBehaviour
    {
        public static MedicalAIChatUI Instance { get; private set; }

        [Header("Main Containers")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private Transform messageContainer;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Controls")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button copyLastButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button micButton;
        [SerializeField] private Button ttsToggleButton;
        [SerializeField] private Button closeButton;

        [Header("Status & Context Badges")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image statusIndicatorDot;
        [SerializeField] private TextMeshProUGUI activeContextPillText;
        [SerializeField] private GameObject loadingSpinner;
        [SerializeField] private TextMeshProUGUI micLevelText;

        [Header("Quick Prompt Chips")]
        [SerializeField] private Transform chipsContainer;

        private MedicalAIOrchestrator _orchestrator;
        private bool _isOpen = false;
        private string _lastAssistantResponseText = "";

        public bool IsOpen => _isOpen;

        public static MedicalAIChatUI EnsureInstance()
        {
            if (Instance != null) return Instance;

            var existing = FindFirstObjectByType<MedicalAIChatUI>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            var go = new GameObject("MedicalAIChatUI_System");
            var ui = go.AddComponent<MedicalAIChatUI>();
            ui.BuildRuntimeUI();
            return ui;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (chatPanel == null)
            {
                BuildRuntimeUI();
            }
        }

        void Start()
        {
            _orchestrator = MedicalAIOrchestrator.Instance;
            if (_orchestrator == null)
            {
                var orchGo = new GameObject("MedicalAIOrchestrator_Auto");
                _orchestrator = orchGo.AddComponent<MedicalAIOrchestrator>();
            }

            BindOrchestratorEvents();
            BindButtonActions();
            UpdateContextPill(null);

            // Hide chat panel by default until toggled
            SetPanelVisible(false);
        }

        void OnDestroy()
        {
            if (_orchestrator != null)
            {
                _orchestrator.OnRequestStarted -= HandleRequestStarted;
                _orchestrator.OnResponseReceived -= HandleResponseReceived;
                _orchestrator.OnRequestFailed -= HandleRequestFailed;
                _orchestrator.OnStatusChanged -= HandleStatusChanged;
                _orchestrator.OnActiveOrganChanged -= HandleActiveOrganChanged;
            }
        }

        private void BindOrchestratorEvents()
        {
            if (_orchestrator == null) return;

            _orchestrator.OnRequestStarted += HandleRequestStarted;
            _orchestrator.OnResponseReceived += HandleResponseReceived;
            _orchestrator.OnRequestFailed += HandleRequestFailed;
            _orchestrator.OnStatusChanged += HandleStatusChanged;
            _orchestrator.OnActiveOrganChanged += HandleActiveOrganChanged;

            if (_orchestrator.VoiceInput != null)
            {
                _orchestrator.VoiceInput.OnRecordingStateChanged += HandleRecordingStateChanged;
                _orchestrator.VoiceInput.OnAudioLevelChanged += HandleAudioLevelChanged;
            }

            UpdateStatusPill(_orchestrator.ActiveProviderName, true);
        }

        private void BindButtonActions()
        {
            sendButton?.onClick.AddListener(OnSendClicked);
            stopButton?.onClick.AddListener(OnStopClicked);
            clearButton?.onClick.AddListener(OnClearClicked);
            copyLastButton?.onClick.AddListener(OnCopyLastClicked);
            retryButton?.onClick.AddListener(OnRetryClicked);
            micButton?.onClick.AddListener(OnMicClicked);
            ttsToggleButton?.onClick.AddListener(OnTTSToggleClicked);
            closeButton?.onClick.AddListener(() => SetPanelVisible(false));

            inputField?.onSubmit.AddListener(text => OnSendClicked());
        }

        public void TogglePanel()
        {
            SetPanelVisible(!_isOpen);
        }

        public void SetPanelVisible(bool visible)
        {
            _isOpen = visible;
            if (chatPanel != null)
            {
                chatPanel.SetActive(visible);
            }

            if (visible && inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        private void OnSendClicked()
        {
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;
            string query = inputField.text.Trim();
            inputField.text = string.Empty;

            AddMessageBubble(MessageRole.User, query);
            _orchestrator.SubmitQuery(query);
        }

        private void OnStopClicked()
        {
            _orchestrator?.StopGeneration();
            SetLoadingState(false);
        }

        private void OnClearClicked()
        {
            _orchestrator?.ClearConversation();
            ClearMessageContainer();
            AddMessageBubble(MessageRole.Assistant, "**Conversation cleared.** Ready for medical questions or anatomical exploration.");
        }

        private void OnCopyLastClicked()
        {
            if (!string.IsNullOrEmpty(_lastAssistantResponseText))
            {
                GUIUtility.systemCopyBuffer = _lastAssistantResponseText;
                UpdateStatusPill("Copied to clipboard!", true);
            }
        }

        private void OnRetryClicked()
        {
            _orchestrator?.RetryLastQuery();
        }

        private void OnMicClicked()
        {
            _orchestrator?.VoiceInput?.ToggleRecording();
        }

        private void OnTTSToggleClicked()
        {
            if (_orchestrator != null && _orchestrator.Configuration != null)
            {
                _orchestrator.Configuration.enableTTS = !_orchestrator.Configuration.enableTTS;
                string state = _orchestrator.Configuration.enableTTS ? "TTS Enabled" : "TTS Muted";
                UpdateStatusPill(state, true);
            }
        }

        public void SubmitQuickPrompt(string prompt)
        {
            SetPanelVisible(true);
            if (inputField != null) inputField.text = string.Empty;
            AddMessageBubble(MessageRole.User, prompt);
            _orchestrator?.SubmitQuery(prompt);
        }

        // Orchestrator Event Handlers
        private void HandleRequestStarted(string query)
        {
            SetLoadingState(true);
            UpdateStatusPill("Thinking...", true);
        }

        private void HandleResponseReceived(AssistantResponse response)
        {
            SetLoadingState(false);
            _lastAssistantResponseText = response.Content;
            AddMessageBubble(MessageRole.Assistant, response.Content, response.Citations);
            UpdateStatusPill($"{response.ProviderUsed} ({response.LatencyMs}ms)", true);
        }

        private void HandleRequestFailed(string error)
        {
            SetLoadingState(false);
            AddMessageBubble(MessageRole.Assistant, $"<color=#FF5252><b>Error:</b> {error}</color>\n*Click [↻] to retry or ask another question.*");
            UpdateStatusPill("Request Failed", false);
        }

        private void HandleStatusChanged(string status)
        {
            UpdateStatusPill(status, true);
        }

        private void HandleActiveOrganChanged(AnatomyMetadata organ)
        {
            UpdateContextPill(organ);
        }

        private void HandleRecordingStateChanged(bool recording)
        {
            if (micLevelText != null)
            {
                micLevelText.text = recording ? "REC..." : "";
                micLevelText.color = recording ? new Color(1f, 0.2f, 0.2f) : Color.white;
            }
        }

        private void HandleAudioLevelChanged(float level)
        {
            if (micLevelText != null && _orchestrator != null && _orchestrator.VoiceInput.IsRecording)
            {
                int bars = Mathf.RoundToInt(level * 10f);
                micLevelText.text = "REC " + new string('|', Mathf.Clamp(bars, 1, 8));
            }
        }

        private void SetLoadingState(bool loading)
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(loading);
            if (sendButton != null) sendButton.interactable = !loading;
            if (stopButton != null) stopButton.interactable = loading;
        }

        private void UpdateStatusPill(string text, bool healthy)
        {
            if (statusText != null) statusText.text = text;
            if (statusIndicatorDot != null)
            {
                statusIndicatorDot.color = healthy ? new Color(0.0f, 0.9f, 0.6f) : new Color(0.9f, 0.3f, 0.3f);
            }
        }

        private void UpdateContextPill(AnatomyMetadata organ)
        {
            if (activeContextPillText == null) return;

            if (organ != null)
            {
                activeContextPillText.text = $"<color=#00E5FF>◉ Active Context:</color> {organ.anatomicalName} ({organ.systemLabel})";
            }
            else
            {
                activeContextPillText.text = "<color=#888888>◉ Context:</color> Full Body Overview";
            }
        }

        public void AddMessageBubble(MessageRole role, string text, IReadOnlyList<string> citations = null)
        {
            if (messageContainer == null) return;

            var bubbleGo = new GameObject($"Msg_{role}_{DateTime.UtcNow.Ticks}");
            bubbleGo.transform.SetParent(messageContainer, false);

            var layout = bubbleGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fitter = bubbleGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var img = bubbleGo.AddComponent<Image>();
            // Dark navy theme with role differentiation
            if (role == MessageRole.User)
            {
                img.color = new Color(0.04f, 0.20f, 0.28f, 0.95f); // Deep Cyan-Slate
            }
            else
            {
                img.color = new Color(0.06f, 0.10f, 0.18f, 0.95f); // Deep Navy
            }

            // Text component
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(bubbleGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.richText = true;
            tmp.enableWordWrapping = true;
            tmp.color = Color.white;

            string header = role == MessageRole.User ? "<color=#00E5FF><b>You</b></color>\n" : "<color=#00FF99><b>Clinical AI Assistant</b></color>\n";
            string body = text;

            if (citations != null && citations.Count > 0)
            {
                body += "\n\n<size=11><color=#80DEEA><b>Sources & Citations:</b>\n- " + string.Join("\n- ", citations) + "</color></size>";
            }

            tmp.text = header + body;

            StartCoroutine(ScrollToBottomRoutine());
        }

        private void ClearMessageContainer()
        {
            if (messageContainer == null) return;
            for (int i = messageContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(messageContainer.GetChild(i).gameObject);
            }
        }

        private IEnumerator ScrollToBottomRoutine()
        {
            yield return null;
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0f, 0f);
            }
        }

        /// <summary>
        /// Programmatically creates the complete clinical UI hierarchy for desktop and VR.
        /// </summary>
        public void BuildRuntimeUI()
        {
            // Canvas Setup
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // Top-level UI

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Launcher Button (Bottom-Right) - Sleek Clinical Pill
            var launcherGo = CreateUIElement("AI_Launcher_Button", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 20), new Vector2(190, 42));
            var launcherImg = launcherGo.AddComponent<Image>();
            launcherImg.color = MedicalUITheme.SurfaceElevated;
            var launcherBtn = launcherGo.AddComponent<Button>();
            var colors = launcherBtn.colors;
            colors.normalColor = MedicalUITheme.SurfaceElevated;
            colors.highlightedColor = MedicalUITheme.SurfaceHover;
            colors.pressedColor = MedicalUITheme.SurfacePressed;
            launcherBtn.colors = colors;
            launcherBtn.onClick.AddListener(TogglePanel);

            var launcherTxtGo = CreateUIElement("Text", launcherGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var launcherTmp = launcherTxtGo.AddComponent<TextMeshProUGUI>();
            launcherTmp.text = "<b><color=#00E5FF>◉</color> AI ASSISTANT</b>";
            launcherTmp.fontSize = 12;
            launcherTmp.alignment = TextAlignmentOptions.Center;
            launcherTmp.color = MedicalUITheme.TextPrimary;

            // Main Chat Panel (Right Docked: 520px wide, fills height with margin)
            chatPanel = CreateUIElement("ChatPanel", transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-280, 0), new Vector2(520, -60));
            var panelImg = chatPanel.AddComponent<Image>();
            panelImg.color = new Color(0.04f, 0.07f, 0.12f, 0.96f); // Dark medical slate

            // Header Bar
            var headerGo = CreateUIElement("Header", chatPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(0, 60));
            var headerImg = headerGo.AddComponent<Image>();
            headerImg.color = new Color(0.07f, 0.12f, 0.20f, 1.0f);

            // Title & Status
            var titleGo = CreateUIElement("Title", headerGo.transform, new Vector2(0f, 0.5f), new Vector2(0.7f, 1f), new Vector2(0f, 0.5f), new Vector2(16, 0), new Vector2(0, 0));
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<color=#00E5FF><b>CLINICAL AI ASSISTANT</b></color>";
            titleTmp.fontSize = 16;
            titleTmp.alignment = TextAlignmentOptions.Left;

            var statusGo = CreateUIElement("Status", headerGo.transform, new Vector2(0f, 0f), new Vector2(0.7f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16, 0), new Vector2(0, 0));
            statusText = statusGo.AddComponent<TextMeshProUGUI>();
            statusText.text = "Initializing Clinical Engine...";
            statusText.fontSize = 11;
            statusText.color = new Color(0.6f, 0.8f, 0.9f);

            // Close Button
            var closeGo = CreateUIElement("CloseBtn", headerGo.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-24, 0), new Vector2(36, 36));
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeButton = closeGo.AddComponent<Button>();
            var closeTxt = CreateUIElement("X", closeGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.fontSize = 16;

            // Context Pill Bar
            var contextBar = CreateUIElement("ContextBar", chatPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -78), new Vector2(-20, 28));
            var contextImg = contextBar.AddComponent<Image>();
            contextImg.color = new Color(0.08f, 0.16f, 0.25f, 0.85f);
            var pillTextGo = CreateUIElement("Text", contextBar.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(-16, 0));
            activeContextPillText = pillTextGo.AddComponent<TextMeshProUGUI>();
            activeContextPillText.fontSize = 12;
            activeContextPillText.alignment = TextAlignmentOptions.Left;

            // Quick Prompt Chips
            chipsContainer = CreateUIElement("ChipsContainer", chatPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -115), new Vector2(-20, 32)).transform;
            var chipsLayout = chipsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipsLayout.spacing = 6;
            chipsLayout.childControlWidth = true;
            chipsLayout.childForceExpandWidth = true;

            CreateChip("Physiology", "Explain the physiological mechanics of this organ.", chipsContainer);
            CreateChip("Pathology", "What are the common diseases, risks, and clinical pathology for this organ?", chipsContainer);
            CreateChip("Procedures", "What clinical or surgical procedures are performed here?", chipsContainer);
            CreateChip("Practice Quiz", "Generate practice quiz questions on this topic.", chipsContainer);

            // Message Scroll View
            var scrollGo = CreateUIElement("MessageScrollView", chatPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(-20, -230));
            scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            var viewportGo = CreateUIElement("Viewport", scrollGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewportGo.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();

            var contentGo = CreateUIElement("Content", viewportGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 8, 8);
            contentLayout.spacing = 8;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentGo.GetComponent<RectTransform>();
            messageContainer = contentGo.transform;

            // Input Toolbar (Bottom)
            var inputBarGo = CreateUIElement("InputBar", chatPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(-20, 52));
            var inputBarImg = inputBarGo.AddComponent<Image>();
            inputBarImg.color = new Color(0.06f, 0.10f, 0.16f, 1.0f);

            // Input Field
            var inputFieldGo = CreateUIElement("InputField", inputBarGo.transform, new Vector2(0f, 0f), new Vector2(0.72f, 1f), new Vector2(0f, 0.5f), new Vector2(6, 0), new Vector2(-12, -8));
            var ifImg = inputFieldGo.AddComponent<Image>();
            ifImg.color = new Color(0.09f, 0.15f, 0.22f, 1.0f);

            inputField = inputFieldGo.AddComponent<TMP_InputField>();
            var textCompGo = CreateUIElement("Text", inputFieldGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(-16, 0));
            var textComp = textCompGo.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 13;
            textComp.color = Color.white;
            inputField.textComponent = textComp;

            var placeholderGo = CreateUIElement("Placeholder", inputFieldGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(-16, 0));
            var placeholderComp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderComp.text = "Ask a medical or general question...";
            placeholderComp.fontSize = 13;
            placeholderComp.color = new Color(0.5f, 0.6f, 0.7f);
            inputField.placeholder = placeholderComp;

            // Send Button
            var sendGo = CreateUIElement("SendBtn", inputBarGo.transform, new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(40, 40));
            sendGo.AddComponent<Image>().color = new Color(0.0f, 0.75f, 0.9f);
            sendButton = sendGo.AddComponent<Button>();
            var sendTxt = CreateUIElement("Txt", sendGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            sendTxt.text = "SEND"; sendTxt.fontSize = 11; sendTxt.fontStyle = FontStyles.Bold;
            sendTxt.fontSize = 16;
            sendTxt.alignment = TextAlignmentOptions.Center;
            sendTxt.color = Color.black;

            // Mic Dictation Button
            var micGo = CreateUIElement("MicBtn", inputBarGo.transform, new Vector2(0.86f, 0.5f), new Vector2(0.86f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(38, 40));
            micGo.AddComponent<Image>().color = new Color(0.12f, 0.22f, 0.32f);
            micButton = micGo.AddComponent<Button>();
            var micTxt = CreateUIElement("Txt", micGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            micTxt.text = "MIC"; micTxt.fontSize = 11; micTxt.fontStyle = FontStyles.Bold;
            micTxt.fontSize = 16;
            micTxt.alignment = TextAlignmentOptions.Center;

            // Clear Button
            var clearGo = CreateUIElement("ClearBtn", inputBarGo.transform, new Vector2(0.96f, 0.5f), new Vector2(0.96f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6, 0), new Vector2(38, 40));
            clearGo.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.15f);
            clearButton = clearGo.AddComponent<Button>();
            var clearTxt = CreateUIElement("Txt", clearGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            clearTxt.text = "CLR"; clearTxt.fontSize = 11; clearTxt.fontStyle = FontStyles.Bold;
            clearTxt.fontSize = 15;
            clearTxt.alignment = TextAlignmentOptions.Center;

            // Initial welcome bubble
            AddMessageBubble(MessageRole.Assistant, "**Clinical AI Assistant Initialized**\nI am grounded in human anatomy, physiology, and surgical procedures. You can ask medical questions, explore inspected organs, or ask general STEM questions like machine learning or photosynthesis.");
        }

        private void CreateChip(string label, string prompt, Transform parent)
        {
            var chipGo = CreateUIElement($"Chip_{label}", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            chipGo.AddComponent<Image>().color = new Color(0.10f, 0.20f, 0.30f, 0.9f);
            var btn = chipGo.AddComponent<Button>();
            btn.onClick.AddListener(() => SubmitQuickPrompt(prompt));

            var txtGo = CreateUIElement("Text", chipGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.9f, 1.0f);
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
