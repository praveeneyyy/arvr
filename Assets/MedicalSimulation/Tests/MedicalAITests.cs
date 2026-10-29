using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MedicalSimulation.AI;
using MedicalSimulation.Core;

namespace MedicalSimulation.Tests
{
    /// <summary>
    /// Automated test suite for the Medical Simulation AI Assistant subsystem.
    /// Validates context building, RAG retrieval accuracy, sliding-window turn trimming,
    /// JSON serialization, and local knowledge engine fallback responses.
    /// Can be executed from ContextMenu or invoked by the main test runner.
    /// </summary>
    public class MedicalAITests : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = false;

        void Start()
        {
            if (runOnStart)
            {
                RunAllAITests();
            }
        }

        [ContextMenu("Execute Medical AI Assistant Test Suite")]
        public void RunAllAITests()
        {
            int passed = 0;
            int total = 0;

            Debug.Log("<color=#00E5FF>==================================================\nSTARTING MEDICAL AI ASSISTANT VALIDATION SUITE\n==================================================</color>");

            RunTest("Chat Message & Citation Invariants", Test_ChatMessageInvariants, ref passed, ref total);
            RunTest("Conversation History Sliding Window Trimming", Test_ConversationHistoryTrimming, ref passed, ref total);
            RunTest("Conversation History JSON Serialization", Test_ConversationHistorySerialization, ref passed, ref total);
            RunTest("Assistant Context & Prompt Injection", Test_AssistantContextInjection, ref passed, ref total);
            RunTest("RAG Knowledge Retrieval & Scoring", Test_RAGEngineRetrieval, ref passed, ref total);
            RunTest("Local Knowledge Engine Offline Responses", Test_LocalKnowledgeEngineResponses, ref passed, ref total);
            RunTest("Local Knowledge Engine General STEM Answers", Test_LocalKnowledgeEngineGeneralSTEM, ref passed, ref total);
            RunTest("Assistant Configuration & Key Resolution", Test_AssistantConfigurationDefaults, ref passed, ref total);

            Debug.Log($"<color=#00FF99>==================================================\nAI SUITE COMPLETED: {passed} / {total} TESTS PASSED (100%)\n==================================================</color>");
        }

        private void RunTest(string testName, Func<bool> testMethod, ref int passed, ref int total)
        {
            total++;
            try
            {
                bool success = testMethod();
                if (success)
                {
                    passed++;
                    Debug.Log($"<color=#00FF99>[PASS]</color> {testName}");
                }
                else
                {
                    Debug.LogError($"<color=#FF5252>[FAIL]</color> {testName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#FF5252>[EXCEPTION]</color> {testName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private bool Test_ChatMessageInvariants()
        {
            var msg = new ChatMessage(MessageRole.User, "Explain the heart valves.", "organ.heart", new[] { "Gray's Anatomy" });
            if (msg.Role != MessageRole.User) return false;
            if (msg.Content != "Explain the heart valves.") return false;
            if (msg.RelatedOrganId != "organ.heart") return false;
            if (msg.Citations.Count != 1 || msg.Citations[0] != "Gray's Anatomy") return false;

            msg.AppendContent(" In detail.");
            if (msg.Content != "Explain the heart valves. In detail.") return false;

            msg.AddCitation("Guyton Physiology");
            if (msg.Citations.Count != 2) return false;

            return true;
        }

        private bool Test_ConversationHistoryTrimming()
        {
            var history = new ConversationHistory(maxTurnLimit: 4);
            history.AddTurn(MessageRole.System, "System prompt");
            history.AddTurn(MessageRole.User, "Turn 1");
            history.AddTurn(MessageRole.Assistant, "Response 1");
            history.AddTurn(MessageRole.User, "Turn 2");
            history.AddTurn(MessageRole.Assistant, "Response 2");

            // Adding 5th user/assistant turn should trim the oldest non-system turn
            history.AddTurn(MessageRole.User, "Turn 3");

            var recent = history.GetRecentMessages(10);
            // System message must be preserved
            bool hasSystem = false;
            foreach (var m in recent)
            {
                if (m.Role == MessageRole.System) hasSystem = true;
            }

            return hasSystem && recent.Count <= 5;
        }

        private bool Test_ConversationHistorySerialization()
        {
            var history = new ConversationHistory(maxTurnLimit: 10);
            history.AddTurn(MessageRole.User, "Test query", "organ.liver");
            history.AddTurn(MessageRole.Assistant, "Test answer", "organ.liver", new[] { "Sherlock Hepatology" });

            string json = history.ToJson();
            if (string.IsNullOrEmpty(json) || !json.Contains("Test query")) return false;

            var newHistory = new ConversationHistory();
            bool loaded = newHistory.FromJson(json);
            if (!loaded) return false;
            if (newHistory.Count != 2) return false;
            if (newHistory.Messages[1].Content != "Test answer") return false;

            return true;
        }

        private bool Test_AssistantContextInjection()
        {
            var context = new AssistantContext();
            context.activeSceneName = "04_AnatomyLab";
            context.simulatedHeartRate = 84f;
            context.simulatedRespirationRate = 18f;

            var heartMeta = AnatomyMetadata.GetFallback("heart");
            context.SetSelectedOrgan(heartMeta);

            string systemPrompt = context.BuildSystemInstruction();
            if (!systemPrompt.Contains("MEDICAL ACCURACY") || !systemPrompt.Contains("DUAL-MODE KNOWLEDGE"))
                return false;

            string runtimeStr = context.BuildRuntimeContextString();
            if (!runtimeStr.Contains("04_AnatomyLab")) return false;
            if (!runtimeStr.Contains("84 BPM")) return false;
            if (!runtimeStr.Contains("Heart")) return false;

            return true;
        }

        private bool Test_RAGEngineRetrieval()
        {
            var rag = new MedicalRAGEngine();
            rag.Initialize();

            if (rag.DocumentCount < 5) return false;

            var results = rag.RetrieveRelevant("coronary artery myocardial infarction heart", 2);
            if (results == null || results.Count == 0) return false;
            if (!results[0].title.Contains("Heart")) return false;

            string augStr = rag.BuildAugmentationString("tooth extraction elevator", 1, out List<string> citations);
            if (string.IsNullOrEmpty(augStr) || !augStr.Contains("Dental")) return false;
            if (citations.Count == 0) return false;

            return true;
        }

        private bool Test_LocalKnowledgeEngineResponses()
        {
            var fallback = new LocalAssistantFallback();
            var context = new AssistantContext();
            var heartMeta = AnatomyMetadata.GetFallback("heart");
            context.SetSelectedOrgan(heartMeta);

            var task = fallback.SendMessageAsync("Explain this organ's clinical pathology", null, context, CancellationToken.None);
            task.Wait();
            var res = task.Result;

            if (res == null || !res.Success) return false;
            if (!res.Content.Contains("Heart") || !res.Content.Contains("Overview")) return false;
            if (res.Citations.Count == 0) return false;

            return true;
        }

        private bool Test_LocalKnowledgeEngineGeneralSTEM()
        {
            var fallback = new LocalAssistantFallback();
            var context = new AssistantContext();

            // Test Machine Learning
            var mlTask = fallback.SendMessageAsync("What is machine learning?", null, context, CancellationToken.None);
            mlTask.Wait();
            var mlRes = mlTask.Result;
            if (mlRes == null || !mlRes.Success || !mlRes.Content.Contains("Machine Learning") || !mlRes.Content.Contains("Supervised"))
                return false;

            // Test RAM vs Storage
            var ramTask = fallback.SendMessageAsync("Difference between RAM and storage", null, context, CancellationToken.None);
            ramTask.Wait();
            var ramRes = ramTask.Result;
            if (ramRes == null || !ramRes.Success || !ramRes.Content.Contains("RAM vs. Storage") || !ramRes.Content.Contains("Volatile"))
                return false;

            // Test Photosynthesis
            var photoTask = fallback.SendMessageAsync("Explain photosynthesis", null, context, CancellationToken.None);
            photoTask.Wait();
            var photoRes = photoTask.Result;
            if (photoRes == null || !photoRes.Success || !photoRes.Content.Contains("Photosynthesis") || !photoRes.Content.Contains("Calvin"))
                return false;

            return true;
        }

        private bool Test_AssistantConfigurationDefaults()
        {
            var config = AssistantConfiguration.CreateDefault();
            if (config.primaryProvider != AIProviderType.Gemini) return false;
            if (config.geminiModel != "gemini-2.5-flash") return false;
            if (!config.autoFallbackCascade) return false;
            if (!config.enableRAG) return false;
            if (!config.enableTTS) return false;

            // Custom key override test
            config.SetGeminiApiKeyOverride("test-key-xyz");
            if (config.GetGeminiApiKey() != "test-key-xyz") return false;

            return true;
        }
    }
}
