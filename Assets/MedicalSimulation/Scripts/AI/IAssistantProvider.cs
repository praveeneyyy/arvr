using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Outcome of an AI assistant request, containing the response text,
    /// clinical source citations, latency metrics, and error information.
    /// </summary>
    public class AssistantResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Citations { get; set; } = new List<string>();
        public string ProviderUsed { get; set; }
        public long LatencyMs { get; set; }

        public static AssistantResponse CreateSuccess(string content, string provider, long latencyMs, IEnumerable<string> citations = null)
        {
            var res = new AssistantResponse
            {
                Success = true,
                Content = content ?? string.Empty,
                ProviderUsed = provider,
                LatencyMs = latencyMs
            };
            if (citations != null)
            {
                res.Citations.AddRange(citations);
            }
            return res;
        }

        public static AssistantResponse CreateError(string errorMessage, string provider, long latencyMs = 0)
        {
            return new AssistantResponse
            {
                Success = false,
                ErrorMessage = errorMessage ?? "Unknown error occurred.",
                Content = string.Empty,
                ProviderUsed = provider,
                LatencyMs = latencyMs
            };
        }
    }

    /// <summary>
    /// Contract implemented by all LLM inference providers (Gemini, Local Ollama,
    /// and offline rule-based clinical engine).
    /// </summary>
    public interface IAssistantProvider
    {
        string ProviderName { get; }
        bool IsConfigured { get; }
        Task<bool> CheckAvailabilityAsync(CancellationToken ct);
        Task<AssistantResponse> SendMessageAsync(string prompt, ConversationHistory history, AssistantContext context, CancellationToken ct);
    }
}
