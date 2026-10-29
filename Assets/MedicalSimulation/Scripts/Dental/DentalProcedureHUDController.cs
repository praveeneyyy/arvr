using UnityEngine;
using MedicalSimulation.Audio;
using System.Collections;

namespace MedicalSimulation.Dental
{
    /// <summary>
    /// Upgraded dental procedure HUD controller.
    /// Integrates with the existing ToothGrabbable / JawController / ToothHighlight
    /// scripts and adds:
    ///  - Contextual audio triggers
    ///  - Step progress tracking
    ///  - Instrument selection feedback
    ///  - Assessment recording through MedicalAppManager
    /// 
    /// Attach to the HUD Canvas root in the Teeth.unity scene.
    /// </summary>
    public class DentalProcedureHUDController : MonoBehaviour
    {
        public enum DentalInstrument
        {
            None,
            Mirror,
            Probe,
            Explorer,
            Scaler,
            Drill,
            Forceps,
            Suction
        }

        [Header("Procedure State")]
        [SerializeField] private string[] procedureSteps = new string[]
        {
            "Examine the tooth with the probe.",
            "Apply topical anaesthetic.",
            "Administer local anaesthetic.",
            "Scale and clean the tooth surface.",
            "Assess the extraction site.",
            "Apply the forceps correctly.",
            "Perform controlled luxation.",
            "Extract the tooth.",
            "Clean the socket.",
            "Complete the procedure."
        };

        [Header("UI References")]
        [SerializeField] private TMPro.TextMeshProUGUI stepText;
        [SerializeField] private TMPro.TextMeshProUGUI progressText;
        [SerializeField] private TMPro.TextMeshProUGUI instrumentNameText;
        [SerializeField] private UnityEngine.UI.Slider progressSlider;
        [SerializeField] private UnityEngine.UI.Button nextStepButton;
        [SerializeField] private UnityEngine.UI.Button prevStepButton;
        [SerializeField] private UnityEngine.UI.Button resetProcedureButton;

        [Header("Instrument Buttons")]
        [SerializeField] private UnityEngine.UI.Button mirrorButton;
        [SerializeField] private UnityEngine.UI.Button probeButton;
        [SerializeField] private UnityEngine.UI.Button scalerButton;
        [SerializeField] private UnityEngine.UI.Button drillButton;
        [SerializeField] private UnityEngine.UI.Button forcepsButton;
        [SerializeField] private UnityEngine.UI.Button suctionButton;

        // ── Internal ─────────────────────────────────────────────────────────
        private int              _currentStep = 0;
        private DentalInstrument _activeInstrument = DentalInstrument.None;
        private bool             _procedureComplete;

        void Start()
        {
            // Wire step navigation
            nextStepButton?.onClick.AddListener(AdvanceStep);
            prevStepButton?.onClick.AddListener(RetreatStep);
            resetProcedureButton?.onClick.AddListener(ResetProcedure);

            // Wire instrument selection
            mirrorButton?.onClick.AddListener(()  => SelectInstrument(DentalInstrument.Mirror));
            probeButton?.onClick.AddListener(()   => SelectInstrument(DentalInstrument.Probe));
            scalerButton?.onClick.AddListener(()  => SelectInstrument(DentalInstrument.Scaler));
            drillButton?.onClick.AddListener(()   => SelectInstrument(DentalInstrument.Drill));
            forcepsButton?.onClick.AddListener(()  => SelectInstrument(DentalInstrument.Forceps));
            suctionButton?.onClick.AddListener(() => SelectInstrument(DentalInstrument.Suction));

            RefreshStepUI();
        }

        // ── Step control ──────────────────────────────────────────────────────

        public void AdvanceStep()
        {
            if (_currentStep < procedureSteps.Length - 1)
            {
                _currentStep++;
                MedicalAudioController.Instance?.PlayConfirm();
                RefreshStepUI();
            }
            else if (!_procedureComplete)
            {
                CompleteProcedure();
            }
        }

        public void RetreatStep()
        {
            if (_currentStep > 0)
            {
                _currentStep--;
                MedicalAudioController.Instance?.PlayClick();
                RefreshStepUI();
            }
        }

        public void ResetProcedure()
        {
            _currentStep        = 0;
            _procedureComplete  = false;
            _activeInstrument   = DentalInstrument.None;
            RefreshStepUI();
        }

        private void CompleteProcedure()
        {
            _procedureComplete = true;
            MedicalAudioController.Instance?.PlayProcedureComplete();
            Core.MedicalAppManager.Instance?.RecordProcedurePracticed();

            if (stepText != null) stepText.text = "✓ Procedure Complete!";
        }

        // ── Instrument selection ──────────────────────────────────────────────

        public void SelectInstrument(DentalInstrument instrument)
        {
            _activeInstrument = instrument;
            MedicalAudioController.Instance?.PlaySelect();
            TriggerInstrumentAudio(instrument);

            if (instrumentNameText != null)
                instrumentNameText.text = instrument.ToString();
        }

        private void TriggerInstrumentAudio(DentalInstrument instrument)
        {
            var audio = MedicalAudioController.Instance;
            if (audio == null) return;

            switch (instrument)
            {
                case DentalInstrument.Drill:   audio.PlayDrill();            break;
                case DentalInstrument.Scaler:  audio.PlayScaler();           break;
                case DentalInstrument.Suction: audio.PlaySuction();          break;
                default:                       audio.PlayInstrumentPlace();  break;
            }
        }

        // ── UI refresh ────────────────────────────────────────────────────────

        private void RefreshStepUI()
        {
            if (stepText != null && _currentStep < procedureSteps.Length)
                stepText.text = procedureSteps[_currentStep];

            if (progressText != null)
                progressText.text = $"Step {_currentStep + 1} / {procedureSteps.Length}";

            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
                progressSlider.maxValue = procedureSteps.Length - 1;
                progressSlider.value    = _currentStep;
            }
        }

        // ── External event hooks (called by ToothGrabbable etc.) ──────────────

        /// <summary>Call from ToothGrabbable when tooth extraction completes.</summary>
        public void OnToothExtracted()
        {
            MedicalAudioController.Instance?.PlayProcedureComplete();
            AdvanceStep();
        }

        /// <summary>Call from JawController when jaw opens/closes.</summary>
        public void OnJawStateChanged(bool isOpen)
        {
            MedicalAudioController.Instance?.PlayInstrumentPlace();
        }
    }
}
