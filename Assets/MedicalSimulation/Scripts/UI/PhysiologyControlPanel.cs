using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.Cardiovascular;
using MedicalSimulation.Respiratory;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// HUD panel for the physiology simulation controls.
    /// Connects slider/button controls to HeartAnimationController and
    /// RespiratoryAnimationController. Shows real simulation values only.
    /// </summary>
    public class PhysiologyControlPanel : MonoBehaviour
    {
        [Header("Heart Controls")]
        [SerializeField] private Slider  heartRateSlider;
        [SerializeField] private TextMeshProUGUI heartRateLabel;
        [SerializeField] private Button  heartPlayPauseButton;
        [SerializeField] private TextMeshProUGUI heartPlayPauseLabel;

        [Header("Respiratory Controls")]
        [SerializeField] private Slider  respRateSlider;
        [SerializeField] private TextMeshProUGUI respRateLabel;
        [SerializeField] private Button  respPlayPauseButton;
        [SerializeField] private TextMeshProUGUI respPlayPauseLabel;

        [Header("Blood Flow Controls")]
        [SerializeField] private Toggle showArterialToggle;
        [SerializeField] private Toggle showVenousToggle;
        [SerializeField] private Toggle showPulmonaryToggle;
        [SerializeField] private Slider  flowSpeedSlider;

        [Header("Runtime References (auto-found if null)")]
        [SerializeField] private HeartAnimationController      heartController;
        [SerializeField] private RespiratoryAnimationController respController;
        [SerializeField] private BloodFlowController            bloodFlowController;

        void Start()
        {
            heartController    ??= FindFirstObjectByType<HeartAnimationController>();
            respController     ??= FindFirstObjectByType<RespiratoryAnimationController>();
            bloodFlowController??= FindFirstObjectByType<BloodFlowController>();

            WireHeartControls();
            WireRespControls();
            WireBloodFlowControls();
        }

        // ── Heart ─────────────────────────────────────────────────────────────

        private void WireHeartControls()
        {
            if (heartRateSlider != null)
            {
                heartRateSlider.minValue = 30f;
                heartRateSlider.maxValue = 180f;
                if (heartController != null) heartRateSlider.value = heartController.GetHeartRate();
                heartRateSlider.onValueChanged.AddListener(bpm =>
                {
                    heartController?.SetHeartRate(bpm);
                    if (heartRateLabel != null) heartRateLabel.text = $"{bpm:F0} BPM";
                });
            }

            heartPlayPauseButton?.onClick.AddListener(() =>
            {
                if (heartController == null) return;
                if (heartController.IsPlaying()) { heartController.Pause(); if (heartPlayPauseLabel) heartPlayPauseLabel.text = "▶ Resume"; }
                else                             { heartController.Play();  if (heartPlayPauseLabel) heartPlayPauseLabel.text = "⏸ Pause";  }
            });
        }

        // ── Respiratory ───────────────────────────────────────────────────────

        private void WireRespControls()
        {
            if (respRateSlider != null)
            {
                respRateSlider.minValue = 6f;
                respRateSlider.maxValue = 30f;
                if (respController != null) respRateSlider.value = respController.GetRespiratoryRate();
                respRateSlider.onValueChanged.AddListener(rpm =>
                {
                    respController?.SetRespiratoryRate(rpm);
                    if (respRateLabel != null) respRateLabel.text = $"{rpm:F0} /min";
                });
            }

            respPlayPauseButton?.onClick.AddListener(() =>
            {
                if (respController == null) return;
                if (respController.IsPlaying()) { respController.Pause(); if (respPlayPauseLabel) respPlayPauseLabel.text = "▶ Resume"; }
                else                            { respController.Play();  if (respPlayPauseLabel) respPlayPauseLabel.text = "⏸ Pause";  }
            });
        }

        // ── Blood Flow ────────────────────────────────────────────────────────

        private void WireBloodFlowControls()
        {
            showArterialToggle?.onValueChanged.AddListener(_ => UpdateBloodFlowVisibility());
            showVenousToggle?.onValueChanged.AddListener(_ => UpdateBloodFlowVisibility());
            showPulmonaryToggle?.onValueChanged.AddListener(_ => UpdateBloodFlowVisibility());

            if (flowSpeedSlider != null)
            {
                flowSpeedSlider.minValue = 0.2f;
                flowSpeedSlider.maxValue = 3f;
                flowSpeedSlider.value    = 1f;
                flowSpeedSlider.onValueChanged.AddListener(v => bloodFlowController?.SetFlowSpeed(v));
            }
        }

        private void UpdateBloodFlowVisibility()
        {
            if (bloodFlowController == null) return;
            bool art = showArterialToggle?.isOn  ?? true;
            bool ven = showVenousToggle?.isOn    ?? true;
            bool pul = showPulmonaryToggle?.isOn ?? true;

            if (art && ven && pul)          bloodFlowController.ShowAllCirculation();
            else if (art && !ven && !pul)   bloodFlowController.ShowArterialOnly();
            else if (!art && ven && !pul)   bloodFlowController.ShowVenousOnly();
            else if (!art && !ven && pul)   bloodFlowController.ShowPulmonaryOnly();
            else                            bloodFlowController.ShowAllCirculation();
        }

        void Update()
        {
            // Keep live readouts accurate
            if (heartRateLabel != null && heartController != null)
                heartRateLabel.text = $"{heartController.GetHeartRate():F0} BPM";
            if (respRateLabel  != null && respController  != null)
                respRateLabel.text  = $"{respController.GetRespiratoryRate():F0} /min";
        }
    }
}
