using UnityEngine;
using UnityEngine.InputSystem;
using MedicalSimulation.AI;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Runtime input controller for the AI Assistant.
    /// Handles keyboard hotkeys (e.g., C / Backquote to toggle chat),
    /// VR controller primary button triggers, and programmatic shortcuts.
    /// </summary>
    public class MedicalAIChatController : MonoBehaviour
    {
        [Header("Hotkeys")]
        [SerializeField] private KeyCode toggleChatKey = KeyCode.C;
        [SerializeField] private KeyCode secondaryToggleKey = KeyCode.BackQuote;

        [Header("References")]
        [SerializeField] private MedicalAIChatUI chatUI;

        void Start()
        {
            if (chatUI == null)
            {
                chatUI = MedicalAIChatUI.EnsureInstance();
            }
        }

        void Update()
        {
            // Legacy input fallback
            if (Input.GetKeyDown(toggleChatKey) || Input.GetKeyDown(secondaryToggleKey))
            {
                ToggleChat();
            }

            // New Input System check if present
            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.backquoteKey.wasPressedThisFrame)
                {
                    ToggleChat();
                }
            }
        }

        public void ToggleChat()
        {
            if (chatUI == null)
            {
                chatUI = MedicalAIChatUI.EnsureInstance();
            }

            chatUI.TogglePanel();
        }

        public void AskAboutCurrentOrgan()
        {
            if (chatUI == null) chatUI = MedicalAIChatUI.EnsureInstance();
            chatUI.SubmitQuickPrompt("Explain the anatomical structures, function, and clinical risks of the currently selected organ.");
        }
    }
}
