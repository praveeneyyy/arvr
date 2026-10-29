using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedicalSimulation.AI
{
    public enum MessageRole
    {
        System,
        User,
        Assistant,
        Tool
    }

    /// <summary>
    /// Represents a single conversational turn in the Medical Simulation AI Assistant.
    /// Supports multi-turn dialog, citations, timestamps, and metadata tags.
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        [SerializeField] private string id;
        [SerializeField] private MessageRole role;
        [SerializeField] private string content;
        [SerializeField] private long timestampUnix;
        [SerializeField] private List<string> citations = new List<string>();
        [SerializeField] private string relatedOrganId;

        public string Id => id;
        public MessageRole Role => role;
        public string Content => content;
        public long TimestampUnix => timestampUnix;
        public IReadOnlyList<string> Citations => citations;
        public string RelatedOrganId => relatedOrganId;

        public ChatMessage(MessageRole role, string content, string relatedOrganId = null, IEnumerable<string> sourceCitations = null)
        {
            this.id = Guid.NewGuid().ToString("N");
            this.role = role;
            this.content = content ?? string.Empty;
            this.timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.relatedOrganId = relatedOrganId;

            if (sourceCitations != null)
            {
                this.citations.AddRange(sourceCitations);
            }
        }

        public void AppendContent(string chunk)
        {
            if (!string.IsNullOrEmpty(chunk))
            {
                content += chunk;
            }
        }

        public void AddCitation(string citation)
        {
            if (!string.IsNullOrEmpty(citation) && !citations.Contains(citation))
            {
                citations.Add(citation);
            }
        }

        public string GetFormattedTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestampUnix).ToLocalTime().ToString("HH:mm:ss");
        }
    }
}
