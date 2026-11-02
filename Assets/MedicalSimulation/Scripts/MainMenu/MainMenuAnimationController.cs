using UnityEngine;
using UnityEngine.UI;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Motion Design & Micro-Interaction Controller for the Project Atlas Main Menu.
    /// Drives subtle breathing pulse on active accents, smooth button hover scale tweens,
    /// and graceful panel fade transitions.
    /// </summary>
    public class MainMenuAnimationController : MonoBehaviour
    {
        [Header("Glow & Pulse Settings")]
        public Image activeAccentGlow;
        public float pulseFrequency = 1.4f;
        public float minAlpha = 0.45f;
        public float maxAlpha = 0.95f;

        void Update()
        {
            if (activeAccentGlow != null && activeAccentGlow.gameObject.activeInHierarchy)
            {
                float wave = Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
                Color c = activeAccentGlow.color;
                c.a = alpha;
                activeAccentGlow.color = c;
            }
        }
    }
}
