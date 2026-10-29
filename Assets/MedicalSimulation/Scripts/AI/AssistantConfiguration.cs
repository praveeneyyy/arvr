using System;
using UnityEngine;

namespace MedicalSimulation.AI
{
    public enum AIProviderType
    {
        Gemini,
        OpenAICompatible,
        LocalOffline
    }

    /// <summary>
    /// Configuration asset for the Medical Simulation AI Assistant.
    /// Supports cloud providers (Gemini), local OpenAI-compatible backends (Ollama/LM Studio),
    /// and built-in offline clinical fallback with automatic cascading.
    /// </summary>
    [CreateAssetMenu(fileName = "AIAssistantConfig", menuName = "MedicalSimulation/AI Assistant Configuration")]
    public class AssistantConfiguration : ScriptableObject
    {
        [Header("Active Provider")]
        [Tooltip("Primary LLM provider to query.")]
        public AIProviderType primaryProvider = AIProviderType.Gemini;

        [Tooltip("Whether to automatically cascade to local/offline fallback if primary provider fails or has no key.")]
        public bool autoFallbackCascade = true;

        [Header("Google Gemini Settings")]
        [Tooltip("Gemini model to query (e.g. gemini-2.5-flash, gemini-1.5-flash).")]
        public string geminiModel = "gemini-2.5-flash";

        [Tooltip("Environment variable name to read Gemini API key from if not explicitly provided.")]
        public string geminiEnvVarName = "GEMINI_API_KEY";

        [Tooltip("Optional direct key override for development (DO NOT commit keys to public repositories).")]
        [SerializeField] private string geminiApiKeyOverride = "";

        [Header("OpenAI-Compatible / Local Ollama Settings")]
        [Tooltip("Chat completions endpoint URL (e.g. http://localhost:11434/v1/chat/completions for Ollama).")]
        public string openAIEndpoint = "http://localhost:11434/v1/chat/completions";

        [Tooltip("Model name for Ollama / OpenAI (e.g. llama3, mistral, gpt-4o-mini).")]
        public string openAIModel = "llama3";

        [Tooltip("Authorization token if connecting to authenticated endpoint (leave 'ollama' for local Ollama).")]
        public string openAIApiKey = "ollama";

        [Header("Inference Parameters")]
        [Range(0f, 1f)]
        public float temperature = 0.4f;

        [Range(64, 4096)]
        public int maxOutputTokens = 1024;

        [Range(3, 60)]
        public int timeoutSeconds = 15;

        [Header("Augmentation & Multimodal")]
        [Tooltip("Enable Retrieval-Augmented Generation (RAG) over in-app anatomical metadata.")]
        public bool enableRAG = true;

        [Tooltip("Enable Text-To-Speech audio readout for assistant responses.")]
        public bool enableTTS = true;

        [Tooltip("Enable Microphone speech input.")]
        public bool enableVoiceInput = true;

        /// <summary>
        /// Retrieves the Gemini API key, prioritizing the override if populated,
        /// then falling back to the operating system environment variable.
        /// </summary>
        public string GetGeminiApiKey()
        {
            if (!string.IsNullOrEmpty(geminiApiKeyOverride))
            {
                return geminiApiKeyOverride.Trim();
            }

            if (!string.IsNullOrEmpty(geminiEnvVarName))
            {
                string envVal = Environment.GetEnvironmentVariable(geminiEnvVarName);
                if (!string.IsNullOrEmpty(envVal))
                {
                    return envVal.Trim();
                }
            }

            return string.Empty;
        }

        public void SetGeminiApiKeyOverride(string key)
        {
            geminiApiKeyOverride = key;
        }

        public static AssistantConfiguration CreateDefault()
        {
            var config = CreateInstance<AssistantConfiguration>();
            config.primaryProvider = AIProviderType.Gemini;
            config.geminiModel = "gemini-2.5-flash";
            config.geminiEnvVarName = "GEMINI_API_KEY";
            config.openAIEndpoint = "http://localhost:11434/v1/chat/completions";
            config.openAIModel = "llama3";
            config.openAIApiKey = "ollama";
            config.temperature = 0.4f;
            config.maxOutputTokens = 1024;
            config.timeoutSeconds = 15;
            config.enableRAG = true;
            config.enableTTS = true;
            config.enableVoiceInput = true;
            return config;
        }
    }
}
