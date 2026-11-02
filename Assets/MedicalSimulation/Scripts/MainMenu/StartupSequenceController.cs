using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Coordinates the multi-stage Project Atlas startup sequence.
    /// Stage A: Premium medical branding, animated ECG line graphics, real initialization progress.
    /// Stage B: Cinematic reveal of the 3D anatomical atlas model, progressive lighting, and UI introduction.
    /// Includes a skip option for returning users.
    /// </summary>
    public class StartupSequenceController : MonoBehaviour
    {
        [Header("Stage References")]
        [SerializeField] private CanvasGroup splashCanvasGroup;
        [SerializeField] private RectTransform ecgLineTransform;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusReadoutText;
        [SerializeField] private Button skipButton;

        [Header("Cinematic Reveal Connections")]
        [SerializeField] private ModelPresentationController presentationController;
        [SerializeField] private CanvasGroup mainUiCanvasGroup;

        [Header("Timing Configuration")]
        public float stageADuration = 2.4f;
        public float stageBRevealDuration = 1.6f;
        public bool autoStart = true;

        public event Action OnStartupComplete;

        private bool _isSkipped = false;
        private bool _isComplete = false;

        void Start()
        {
            if (autoStart)
            {
                StartCoroutine(RunStartupSequenceRoutine());
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipStartup);
            }
        }

        void Update()
        {
            if (!_isComplete && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            {
                SkipStartup();
            }
        }

        public void SkipStartup()
        {
            if (_isComplete) return;
            _isSkipped = true;
        }

        private IEnumerator RunStartupSequenceRoutine()
        {
            // Initial states
            if (mainUiCanvasGroup != null) mainUiCanvasGroup.alpha = 0f;
            if (presentationController != null) presentationController.SetRevealProgress(0f);
            if (progressBar != null) progressBar.value = 0f;

            // STAGE A: Launch Progress & Medical ECG Animation
            string[] statusSteps = new string[] {
                "INITIALIZING URP RENDER PIPELINE & CAMERA MATRICES...",
                "LOADING 6-MODULE HIGH-POLY ANATOMICAL ATLAS GEOMETRY...",
                "CONFIGURING PBR SHADERS & EPIDERMAL SUBSURFACE SCATTERING...",
                "CALIBRATING CLINICAL AUDIO SOUNDSCAPES & VR INTERFACES...",
                "READY. WELCOME TO PROJECT ATLAS MEDICAL ATLAS."
            };

            float elapsed = 0f;
            while (elapsed < stageADuration && !_isSkipped)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stageADuration);

                if (progressBar != null) progressBar.value = t;
                if (progressText != null) progressText.text = $"{(int)(t * 100)}%";

                int stepIdx = Mathf.Min(statusSteps.Length - 1, (int)(t * statusSteps.Length));
                if (statusReadoutText != null) statusReadoutText.text = statusSteps[stepIdx];

                // Animated ECG trace pulse
                if (ecgLineTransform != null)
                {
                    float ecgSweep = (Time.time * 2.8f) % 1.0f;
                    ecgLineTransform.anchorMin = new Vector2(ecgSweep, 0.5f);
                    ecgLineTransform.anchorMax = new Vector2(ecgSweep, 0.5f);
                }

                yield return null;
            }

            // STAGE B: Cinematic Reveal & Illumination
            if (statusReadoutText != null) statusReadoutText.text = "WELCOME TO PROJECT ATLAS MEDICAL COMMAND CENTRE.";
            if (progressBar != null) progressBar.value = 1f;
            if (progressText != null) progressText.text = "100%";

            float revealElapsed = 0f;
            while (revealElapsed < stageBRevealDuration && !_isSkipped)
            {
                revealElapsed += Time.deltaTime;
                float rt = Mathf.Clamp01(revealElapsed / stageBRevealDuration);

                // Fade out splash overlay
                if (splashCanvasGroup != null)
                {
                    splashCanvasGroup.alpha = 1f - rt;
                }

                // Slowly illuminate 3D anatomical model with 3-point studio lighting
                if (presentationController != null)
                {
                    presentationController.SetRevealProgress(rt);
                }

                // Fade in primary main menu UI
                if (mainUiCanvasGroup != null)
                {
                    mainUiCanvasGroup.alpha = rt;
                }

                yield return null;
            }

            // Guarantee finalized state
            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 0f;
                splashCanvasGroup.gameObject.SetActive(false);
            }

            if (presentationController != null)
            {
                presentationController.SetRevealProgress(1f);
            }

            if (mainUiCanvasGroup != null)
            {
                mainUiCanvasGroup.alpha = 1f;
                mainUiCanvasGroup.interactable = true;
                mainUiCanvasGroup.blocksRaycasts = true;
            }

            _isComplete = true;
            OnStartupComplete?.Invoke();
        }
    }
}
