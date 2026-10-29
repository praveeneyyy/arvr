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
    /// Google Gemini API provider implementation.
    /// Handles authentication via API key or environment variable, formats
    /// system instructions and multi-turn contents, and executes queries via UnityWebRequest.
    /// </summary>
    public class GeminiAssistantProvider : IAssistantProvider
    {
        private readonly AssistantConfiguration _config;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

        public string ProviderName => "Google Gemini";
        public bool IsConfigured => !string.IsNullOrEmpty(_config.GetGeminiApiKey());

        public GeminiAssistantProvider(AssistantConfiguration config)
        {
            _config = config ?? AssistantConfiguration.CreateDefault();
        }

        public async Task<bool> CheckAvailabilityAsync(CancellationToken ct)
        {
            string apiKey = _config.GetGeminiApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                return false;
            }

            // Quick network reachability check
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public async Task<AssistantResponse> SendMessageAsync(
            string prompt,
            ConversationHistory history,
            AssistantContext context,
            CancellationToken ct)
        {
            string apiKey = _config.GetGeminiApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                return AssistantResponse.CreateError(
                    "Gemini API key is not configured. Set the GEMINI_API_KEY environment variable or enter it in Assistant Configuration.",
                    ProviderName);
            }

            var stopwatch = Stopwatch.StartNew();
            string model = string.IsNullOrEmpty(_config.geminiModel) ? "gemini-2.5-flash" : _config.geminiModel;
            string url = $"{BaseUrl}/{model}:generateContent?key={apiKey}";

            // Construct payload
            string jsonPayload = BuildGeminiRequestJson(prompt, history, context);

            try
            {
                using (var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    uwr.timeout = _config.timeoutSeconds;

                    var op = uwr.SendWebRequest();

                    while (!op.isDone)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            uwr.Abort();
                            return AssistantResponse.CreateError("Request was cancelled by user.", ProviderName, stopwatch.ElapsedMilliseconds);
                        }
                        await Task.Yield();
                    }

                    stopwatch.Stop();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        string errorDetail = uwr.error;
                        if (uwr.downloadHandler != null && !string.IsNullOrEmpty(uwr.downloadHandler.text))
                        {
                            errorDetail += " - " + uwr.downloadHandler.text;
                        }
                        return AssistantResponse.CreateError($"Gemini HTTP Error ({uwr.responseCode}): {errorDetail}", ProviderName, stopwatch.ElapsedMilliseconds);
                    }

                    string responseText = uwr.downloadHandler.text;
                    string generatedAnswer = ParseGeminiResponse(responseText);

                    if (string.IsNullOrEmpty(generatedAnswer))
                    {
                        return AssistantResponse.CreateError("Received empty or unparseable response from Gemini API.", ProviderName, stopwatch.ElapsedMilliseconds);
                    }

                    return AssistantResponse.CreateSuccess(generatedAnswer, $"{ProviderName} ({model})", stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return AssistantResponse.CreateError($"Exception querying Gemini: {ex.Message}", ProviderName, stopwatch.ElapsedMilliseconds);
            }
        }

        private string BuildGeminiRequestJson(string currentPrompt, ConversationHistory history, AssistantContext context)
        {
            // System instruction
            string systemInstruction = context != null ? context.BuildSystemInstruction() : "You are a medical simulation assistant.";
            string runtimeContext = context != null ? context.BuildRuntimeContextString() : string.Empty;

            var sb = new StringBuilder();
            sb.Append("{");

            // System Instruction
            sb.Append("\"systemInstruction\":{\"parts\":[{\"text\":");
            sb.Append(EscapeJsonString(systemInstruction));
            sb.Append("}]},");

            // Contents array
            sb.Append("\"contents\":[");
            bool firstContent = true;

            // Prior history (up to recent 6 turns to keep context fast and focused)
            if (history != null && history.Count > 0)
            {
                var recent = history.GetRecentMessages(6);
                for (int i = 0; i < recent.Count; i++)
                {
                    var msg = recent[i];
                    if (msg.Role == MessageRole.System) continue;

                    if (!firstContent) sb.Append(",");
                    firstContent = false;

                    string role = msg.Role == MessageRole.User ? "user" : "model";
                    sb.Append("{\"role\":\"").Append(role).Append("\",\"parts\":[{\"text\":");
                    sb.Append(EscapeJsonString(msg.Content));
                    sb.Append("}]}");
                }
            }

            // Current user prompt with injected runtime context
            if (!firstContent) sb.Append(",");
            string fullPrompt = currentPrompt;
            if (!string.IsNullOrEmpty(runtimeContext))
            {
                fullPrompt = runtimeContext + "\n\nUser Question:\n" + currentPrompt;
            }

            sb.Append("{\"role\":\"user\",\"parts\":[{\"text\":");
            sb.Append(EscapeJsonString(fullPrompt));
            sb.Append("}]}");

            sb.Append("],");

            // Generation Config
            sb.Append("\"generationConfig\":{");
            sb.Append("\"temperature\":").Append(_config.temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(",");
            sb.Append("\"maxOutputTokens\":").Append(_config.maxOutputTokens);
            sb.Append("}");

            sb.Append("}");
            return sb.ToString();
        }

        private string ParseGeminiResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                // Attempt JsonUtility parsing first
                var geminiRes = JsonUtility.FromJson<GeminiRawResponse>(json);
                if (geminiRes != null && geminiRes.candidates != null && geminiRes.candidates.Length > 0)
                {
                    var candidate = geminiRes.candidates[0];
                    if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                    {
                        var sb = new StringBuilder();
                        for (int i = 0; i < candidate.content.parts.Length; i++)
                        {
                            sb.Append(candidate.content.parts[i].text);
                        }
                        return sb.ToString().Trim();
                    }
                }
            }
            catch
            {
                // Fallback to regex/string extraction if structure has variants
            }

            // Fallback text parser
            int textKeyIndex = json.IndexOf("\"text\":", StringComparison.OrdinalIgnoreCase);
            if (textKeyIndex >= 0)
            {
                int startQuote = json.IndexOf('"', textKeyIndex + 7);
                if (startQuote >= 0)
                {
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
                }
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
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append($"\\u{(int)c:x4}");
                        }
                        else
                        {
                            sb.Append(c);
                        }
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

        [Serializable]
        private class GeminiRawPart { public string text; }

        [Serializable]
        private class GeminiRawContent { public string role; public GeminiRawPart[] parts; }

        [Serializable]
        private class GeminiRawCandidate { public GeminiRawContent content; public string finishReason; }

        [Serializable]
        private class GeminiRawResponse { public GeminiRawCandidate[] candidates; }
    }
}
