using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Connects to any OpenAI-compatible API endpoint, including local offline Ollama
    /// instances (e.g. http://localhost:11434/v1/chat/completions), LM Studio, vLLM, or OpenAI.
    /// </summary>
    public class OpenAICompatibleProvider : IAssistantProvider
    {
        private readonly AssistantConfiguration _config;

        public string ProviderName => "OpenAI / Local Ollama";
        public bool IsConfigured => !string.IsNullOrEmpty(_config.openAIEndpoint);

        public OpenAICompatibleProvider(AssistantConfiguration config)
        {
            _config = config ?? AssistantConfiguration.CreateDefault();
        }

        public async Task<bool> CheckAvailabilityAsync(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_config.openAIEndpoint)) return false;

            // Ping the endpoint with GET or HEAD
            try
            {
                string healthUrl = _config.openAIEndpoint.Replace("/chat/completions", "");
                if (string.IsNullOrEmpty(healthUrl)) healthUrl = _config.openAIEndpoint;

                using (var uwr = UnityWebRequest.Get(healthUrl))
                {
                    uwr.timeout = 2;
                    var op = uwr.SendWebRequest();
                    while (!op.isDone)
                    {
                        if (ct.IsCancellationRequested) return false;
                        await Task.Yield();
                    }
                    return uwr.result == UnityWebRequest.Result.Success || uwr.responseCode > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<AssistantResponse> SendMessageAsync(
            string prompt,
            ConversationHistory history,
            AssistantContext context,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_config.openAIEndpoint))
            {
                return AssistantResponse.CreateError("OpenAI/Ollama endpoint URL is not configured.", ProviderName);
            }

            var stopwatch = Stopwatch.StartNew();
            string jsonPayload = BuildOpenAIRequestJson(prompt, history, context);

            try
            {
                using (var uwr = new UnityWebRequest(_config.openAIEndpoint, UnityWebRequest.kHttpVerbPOST))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Content-Type", "application/json");

                    string authKey = string.IsNullOrEmpty(_config.openAIApiKey) ? "ollama" : _config.openAIApiKey;
                    uwr.SetRequestHeader("Authorization", "Bearer " + authKey);
                    uwr.timeout = _config.timeoutSeconds;

                    var op = uwr.SendWebRequest();
                    while (!op.isDone)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            uwr.Abort();
                            return AssistantResponse.CreateError("Request cancelled.", ProviderName, stopwatch.ElapsedMilliseconds);
                        }
                        await Task.Yield();
                    }

                    stopwatch.Stop();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        return AssistantResponse.CreateError($"Ollama/OpenAI Error ({uwr.responseCode}): {uwr.error}", ProviderName, stopwatch.ElapsedMilliseconds);
                    }

                    string responseText = uwr.downloadHandler.text;
                    string answer = ParseOpenAIResponse(responseText);

                    if (string.IsNullOrEmpty(answer))
                    {
                        return AssistantResponse.CreateError("Received empty or unparseable response from local LLM.", ProviderName, stopwatch.ElapsedMilliseconds);
                    }

                    return AssistantResponse.CreateSuccess(answer, $"{ProviderName} ({_config.openAIModel})", stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return AssistantResponse.CreateError($"Exception querying Ollama/OpenAI: {ex.Message}", ProviderName, stopwatch.ElapsedMilliseconds);
            }
        }

        private string BuildOpenAIRequestJson(string currentPrompt, ConversationHistory history, AssistantContext context)
        {
            var sb = new StringBuilder();
            sb.Append("{");

            // Model
            string model = string.IsNullOrEmpty(_config.openAIModel) ? "llama3" : _config.openAIModel;
            sb.Append("\"model\":\"").Append(model).Append("\",");

            // Messages array
            sb.Append("\"messages\":[");
            bool firstMsg = true;

            // System prompt
            string systemPrompt = context != null ? context.BuildSystemInstruction() : "You are a medical simulation assistant.";
            sb.Append("{\"role\":\"system\",\"content\":").Append(EscapeJsonString(systemPrompt)).Append("}");
            firstMsg = false;

            // Prior history
            if (history != null && history.Count > 0)
            {
                var recent = history.GetRecentMessages(6);
                for (int i = 0; i < recent.Count; i++)
                {
                    var msg = recent[i];
                    if (msg.Role == MessageRole.System) continue;

                    sb.Append(",");
                    string role = msg.Role == MessageRole.User ? "user" : "assistant";
                    sb.Append("{\"role\":\"").Append(role).Append("\",\"content\":");
                    sb.Append(EscapeJsonString(msg.Content)).Append("}");
                }
            }

            // Current prompt with runtime context
            sb.Append(",");
            string runtimeContext = context != null ? context.BuildRuntimeContextString() : string.Empty;
            string fullPrompt = currentPrompt;
            if (!string.IsNullOrEmpty(runtimeContext))
            {
                fullPrompt = runtimeContext + "\n\nUser Question:\n" + currentPrompt;
            }

            sb.Append("{\"role\":\"user\",\"content\":").Append(EscapeJsonString(fullPrompt)).Append("}");

            sb.Append("],");

            // Parameters
            sb.Append("\"temperature\":").Append(_config.temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(",");
            sb.Append("\"max_tokens\":").Append(_config.maxOutputTokens);

            sb.Append("}");
            return sb.ToString();
        }

        private string ParseOpenAIResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            // Look for "content": "..." inside "choices"
            int choicesIdx = json.IndexOf("\"choices\"", StringComparison.OrdinalIgnoreCase);
            if (choicesIdx < 0) return null;

            int contentIdx = json.IndexOf("\"content\":", choicesIdx, StringComparison.OrdinalIgnoreCase);
            if (contentIdx < 0) return null;

            int startQuote = json.IndexOf('"', contentIdx + 10);
            if (startQuote < 0) return null;

            int endQuote = -1;
            bool escaped = false;
            for (int i = startQuote + 1; i < json.Length; i++)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                if (json[i] == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (json[i] == '"')
                {
                    endQuote = i;
                    break;
                }
            }

            if (endQuote > startQuote)
            {
                string raw = json.Substring(startQuote + 1, endQuote - startQuote - 1);
                return UnescapeJsonString(raw).Trim();
            }

            return null;
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            var sb = new StringBuilder(s.Length + 10);
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append($"\\u{(int)c:x4}");
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string UnescapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
        }
    }
}
