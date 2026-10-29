using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Manages multi-turn conversation history for the AI assistant with
    /// sliding-window context limits and JSON session persistence.
    /// </summary>
    [Serializable]
    public class ConversationHistory
    {
        [SerializeField] private List<ChatMessage> messages = new List<ChatMessage>();
        [SerializeField] private int maxTurnLimit = 20;

        public IReadOnlyList<ChatMessage> Messages => messages;
        public int Count => messages.Count;
        public int MaxTurnLimit
        {
            get => maxTurnLimit;
            set => maxTurnLimit = Mathf.Clamp(value, 2, 100);
        }

        public event Action<ChatMessage> OnMessageAdded;
        public event Action OnConversationCleared;

        public ConversationHistory(int maxTurnLimit = 20)
        {
            this.maxTurnLimit = Mathf.Clamp(maxTurnLimit, 2, 100);
        }

        public void AddMessage(ChatMessage message)
        {
            if (message == null) return;
            messages.Add(message);
            TrimExcessTurns();
            OnMessageAdded?.Invoke(message);
        }

        public void AddTurn(MessageRole role, string content, string relatedOrganId = null, IEnumerable<string> citations = null)
        {
            var msg = new ChatMessage(role, content, relatedOrganId, citations);
            AddMessage(msg);
        }

        public void Clear()
        {
            messages.Clear();
            OnConversationCleared?.Invoke();
        }

        private void TrimExcessTurns()
        {
            // Keep system messages if present, trim oldest user/assistant pairs
            int userAssistantCount = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Role == MessageRole.User || messages[i].Role == MessageRole.Assistant)
                {
                    userAssistantCount++;
                }
            }

            while (userAssistantCount > maxTurnLimit && messages.Count > 0)
            {
                int removeIndex = -1;
                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i].Role != MessageRole.System)
                    {
                        removeIndex = i;
                        break;
                    }
                }

                if (removeIndex >= 0)
                {
                    messages.RemoveAt(removeIndex);
                    userAssistantCount--;
                }
                else
                {
                    break;
                }
            }
        }

        public List<ChatMessage> GetRecentMessages(int count)
        {
            int start = Mathf.Max(0, messages.Count - count);
            var result = new List<ChatMessage>(count);
            for (int i = start; i < messages.Count; i++)
            {
                result.Add(messages[i]);
            }
            return result;
        }

        [Serializable]
        private class SerializationWrapper
        {
            public List<ChatMessage> items;
        }

        public string ToJson()
        {
            var wrapper = new SerializationWrapper { items = messages };
            return JsonUtility.ToJson(wrapper, true);
        }

        public bool FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
                if (wrapper != null && wrapper.items != null)
                {
                    messages = wrapper.items;
                    TrimExcessTurns();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ConversationHistory] Failed to deserialize conversation: {ex.Message}");
            }
            return false;
        }
    }
}
