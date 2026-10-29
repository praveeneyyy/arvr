using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Central orchestrator for the Medical Simulation AI Assistant.
    /// Manages multi-provider cascading (Gemini -> Local LLM -> Offline Clinical Fallback),
    /// live simulation context injection, RAG grounding, voice input, and TTS readouts.
    /// </summary>
    public class MedicalAIOrchestrator : MonoBehaviour
    {
        public static MedicalAIOrchestrator Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private AssistantConfiguration configuration;

        [Header("Subsystems")]
        [SerializeField] private MedicalVoiceInput voiceInput;
        [SerializeField] private MedicalSpeechOutput speechOutput;

        [Header("Telemetry & Context")]
        [SerializeField] private AssistantContext liveContext = new AssistantContext();

        public AssistantConfiguration Configuration => configuration;
        public AssistantContext LiveContext => liveContext;
        public ConversationHistory History { get; private set; }
        public MedicalRAGEngine RAGEngine { get; private set; }
        public MedicalVoiceInput VoiceInput => voiceInput;
        public MedicalSpeechOutput SpeechOutput => speechOutput;

        public bool IsGenerating { get; private set; }
        public string ActiveProviderName { get; private set; } = "Initializing...";

        // Events
        public event Action<string> OnRequestStarted;
        public event Action<AssistantResponse> OnResponseReceived;
        public event Action<string> OnRequestFailed;
        public event Action<string> OnStatusChanged;
        public event Action<AnatomyMetadata> OnActiveOrganChanged;

        private IAssistantProvider _geminiProvider;
        private IAssistantProvider _openAIProvider;
        private IAssistantProvider _offlineFallbackProvider;

        private CancellationTokenSource _currentCts;
        private string _lastUserQuery;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeOrchestrator();
        }

        void OnEnable()
        {
            OrganInteractable.OnOrganSelected += HandleOrganSelected;
            OrganInteractable.OnOrganDeselected += HandleOrganDeselected;
            ProcedureRunner.OnStepChanged += HandleProcedureStepChanged;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDisable()
        {
            OrganInteractable.OnOrganSelected -= HandleOrganSelected;
            OrganInteractable.OnOrganDeselected -= HandleOrganDeselected;
            ProcedureRunner.OnStepChanged -= HandleProcedureStepChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void InitializeOrchestrator()
        {
            if (configuration == null)
            {
                configuration = AssistantConfiguration.CreateDefault();
            }

            History = new ConversationHistory(maxTurnLimit: 24);
            RAGEngine = new MedicalRAGEngine();
            RAGEngine.Initialize();

            // Setup providers
            _geminiProvider = new GeminiAssistantProvider(configuration);
            _openAIProvider = new OpenAICompatibleProvider(configuration);
            _offlineFallbackProvider = new LocalAssistantFallback();

            // Wire Voice Input if present
            if (voiceInput == null)
            {
                voiceInput = GetComponentInChildren<MedicalVoiceInput>();
                if (voiceInput == null)
                {
                    var voiceObj = new GameObject("MedicalVoiceInput");
                    voiceObj.transform.SetParent(transform);
                    voiceInput = voiceObj.AddComponent<MedicalVoiceInput>();
                }
            }
            voiceInput.OnSpeechRecorded += HandleSpeechRecorded;

            // Wire Speech Output if present
            if (speechOutput == null)
            {
                speechOutput = GetComponentInChildren<MedicalSpeechOutput>();
                if (speechOutput == null)
                {
                    var speechObj = new GameObject("MedicalSpeechOutput");
                    speechObj.transform.SetParent(transform);
                    speechOutput = speechObj.AddComponent<MedicalSpeechOutput>();
                }
            }

            UpdateActiveProviderName();
            liveContext.activeSceneName = SceneManager.GetActiveScene().name;
        }

        private void UpdateActiveProviderName()
        {
            switch (configuration.primaryProvider)
            {
                case AIProviderType.Gemini:
                    ActiveProviderName = _geminiProvider.IsConfigured
                        ? $"Gemini ({configuration.geminiModel})"
                        : "Gemini (No Key - Cascade to Local)";
                    break;
                case AIProviderType.OpenAICompatible:
                    ActiveProviderName = $"Local LLM ({configuration.openAIModel})";
                    break;
                case AIProviderType.LocalOffline:
                    ActiveProviderName = "Offline Clinical Engine";
                    break;
            }
            OnStatusChanged?.Invoke(ActiveProviderName);
        }

        public async void SubmitQuery(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt) || IsGenerating) return;

            _lastUserQuery = prompt;
            IsGenerating = true;
            _currentCts?.Dispose();
            _currentCts = new CancellationTokenSource();

            // Record user turn in history
            History.AddTurn(MessageRole.User, prompt, liveContext.selectedOrgan?.organId);
            OnRequestStarted?.Invoke(prompt);

            // Optional RAG Grounding
            string augmentedPrompt = prompt;
            List<string> ragCitations = new List<string>();

            if (configuration.enableRAG && RAGEngine != null)
            {
                string ragContext = RAGEngine.BuildAugmentationString(prompt, 2, out ragCitations);
                if (!string.IsNullOrEmpty(ragContext))
                {
                    augmentedPrompt = ragContext + prompt;
                }
            }

            AssistantResponse response = null;

            // Provider selection with automatic cascading
            try
            {
                if (configuration.primaryProvider == AIProviderType.Gemini && _geminiProvider.IsConfigured)
                {
                    OnStatusChanged?.Invoke($"Querying {configuration.geminiModel}...");
                    response = await _geminiProvider.SendMessageAsync(augmentedPrompt, History, liveContext, _currentCts.Token);
                }
                else if (configuration.primaryProvider == AIProviderType.OpenAICompatible && _openAIProvider.IsConfigured)
                {
                    OnStatusChanged?.Invoke($"Querying local {configuration.openAIModel}...");
                    response = await _openAIProvider.SendMessageAsync(augmentedPrompt, History, liveContext, _currentCts.Token);
                }

                // If primary failed or was skipped, cascade to offline knowledge engine
                if ((response == null || !response.Success) && configuration.autoFallbackCascade)
                {
                    string reason = response != null ? response.ErrorMessage : "Provider unavailable";
                    Debug.Log($"[MedicalAIOrchestrator] Cascading to local knowledge engine ({reason})");
                    OnStatusChanged?.Invoke("Resolving via Local Knowledge Engine...");
                    response = await _offlineFallbackProvider.SendMessageAsync(prompt, History, liveContext, _currentCts.Token);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MedicalAIOrchestrator] Execution error: {ex.Message}");
                response = AssistantResponse.CreateError($"Execution failure: {ex.Message}", "Orchestrator");
            }
            finally
            {
                IsGenerating = false;
                UpdateActiveProviderName();
            }

            if (response != null && response.Success)
            {
                // Merge RAG citations
                if (ragCitations != null && ragCitations.Count > 0)
                {
                    foreach (var c in ragCitations)
                    {
                        if (!response.Citations.Contains(c))
                        {
                            response.Citations.Add(c);
                        }
                    }
                }

                History.AddTurn(MessageRole.Assistant, response.Content, liveContext.selectedOrgan?.organId, response.Citations);
                OnResponseReceived?.Invoke(response);

                // Speak response if TTS is active
                if (configuration.enableTTS && speechOutput != null)
                {
                    speechOutput.Speak(response.Content);
                }
            }
            else
            {
                string err = response != null ? response.ErrorMessage : "Unknown failure";
                OnRequestFailed?.Invoke(err);
            }
        }

        public void StopGeneration()
        {
            if (_currentCts != null && !_currentCts.IsCancellationRequested)
            {
                _currentCts.Cancel();
            }

            if (speechOutput != null && speechOutput.IsSpeaking)
            {
                speechOutput.Stop();
            }

            IsGenerating = false;
            OnStatusChanged?.Invoke("Generation stopped.");
        }

        public void RetryLastQuery()
        {
            if (!string.IsNullOrEmpty(_lastUserQuery))
            {
                SubmitQuery(_lastUserQuery);
            }
        }

        public void ClearConversation()
        {
            StopGeneration();
            History?.Clear();
            OnStatusChanged?.Invoke("Conversation cleared.");
        }

        public void SwitchProvider(AIProviderType newProvider)
        {
            configuration.primaryProvider = newProvider;
            UpdateActiveProviderName();
        }

        // Live Event Handlers
        private void HandleOrganSelected(OrganInteractable organ)
        {
            if (organ == null) return;
            var meta = organ.metadata ?? AnatomyMetadata.GetFallback(organ.gameObject.name);
            liveContext.SetSelectedOrgan(meta);
            OnActiveOrganChanged?.Invoke(meta);
            Debug.Log($"[MedicalAIOrchestrator] Live Context Updated: Selected {meta.anatomicalName}");
        }

        private void HandleOrganDeselected(OrganInteractable organ)
        {
            liveContext.ClearSelectedOrgan();
            OnActiveOrganChanged?.Invoke(null);
        }

        private void HandleProcedureStepChanged(int stepIndex, string stepTitle)
        {
            liveContext.SetProcedureContext("Clinical Procedure", $"Step {stepIndex + 1}: {stepTitle}");
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            liveContext.activeSceneName = scene.name;
        }

        private void HandleSpeechRecorded(AudioClip clip, float duration)
        {
            // If real speech is recorded, prompt the assistant with the active context question
            if (liveContext.HasSelectedOrgan)
            {
                SubmitQuery($"Explain the anatomy, function, and pathology of the {liveContext.selectedOrgan.anatomicalName}.");
            }
            else
            {
                SubmitQuery("Provide an overview of the cardiovascular and respiratory systems.");
            }
        }
    }
}
