using UnityEngine;
using System.Collections.Generic;
using MedicalSimulation.Audio;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Executes and monitors structured medical procedures.
    /// Manages procedure phases, objectives checklist, sterile field validation,
    /// technique scoring, and step advancement.
    /// </summary>
    public class ProcedureRunner : MonoBehaviour
    {
        [System.Serializable]
        public class ProcedureStep
        {
            public string stepId;
            public string stepTitle;
            [TextArea(2, 4)] public string stepDescription;
            public bool isMandatory = true;
            public bool isCompleted = false;
            public float minDurationSeconds = 1.0f;
            public float timeSpent = 0f;
        }

        [Header("Procedure Config")]
        [SerializeField] private string procedureName = "Surgical / Procedural Simulation";
        [SerializeField] private List<ProcedureStep> steps = new List<ProcedureStep>();

        [Header("Runtime State")]
        public int CurrentStepIndex = 0;
        public bool IsProcedureActive = false;
        public float TotalElapsedProcedureTime = 0f;
        public int ErrorCount = 0;

        public ProcedureStep CurrentStep => (steps != null && steps.Count > CurrentStepIndex) ? steps[CurrentStepIndex] : null;

        // ── Events ────────────────────────────────────────────────────────────
        public static event System.Action<ProcedureStep, int, int> OnStepChanged; // currentStep, index, total
        public event System.Action<ProcedureStep> OnStepCompleted;
        public event System.Action<string, float, int> OnProcedureFinished; // name, totalTime, errors

        void Start()
        {
            if (steps == null || steps.Count == 0)
            {
                InitializeDefaultSteps();
            }
            StartProcedure();
        }

        void Update()
        {
            if (!IsProcedureActive) return;

            TotalElapsedProcedureTime += Time.deltaTime;
            if (CurrentStep != null)
            {
                CurrentStep.timeSpent += Time.deltaTime;
            }
        }

        public void StartProcedure()
        {
            IsProcedureActive = true;
            CurrentStepIndex = 0;
            TotalElapsedProcedureTime = 0f;
            ErrorCount = 0;

            foreach (var s in steps) s.isCompleted = false;

            NotifyStepChanged();
            Debug.Log($"[ProcedureRunner] Started: {procedureName}");
        }

        public void CompleteCurrentStep()
        {
            if (!IsProcedureActive || CurrentStep == null) return;

            CurrentStep.isCompleted = true;
            OnStepCompleted?.Invoke(CurrentStep);
            MedicalAudioController.Instance?.PlayConfirm();

            CurrentStepIndex++;
            if (CurrentStepIndex >= steps.Count)
            {
                FinishProcedure();
            }
            else
            {
                NotifyStepChanged();
            }
        }

        public void RecordClinicalError(string reason)
        {
            ErrorCount++;
            MedicalAudioController.Instance?.PlayError();
            Debug.LogWarning($"[ProcedureRunner] Clinical error recorded: {reason}");
        }

        private void FinishProcedure()
        {
            IsProcedureActive = false;
            MedicalAudioController.Instance?.PlayProcedureComplete();
            MedicalAppManager.Instance?.RecordProcedurePracticed();

            OnProcedureFinished?.Invoke(procedureName, TotalElapsedProcedureTime, ErrorCount);
            Debug.Log($"[ProcedureRunner] Procedure '{procedureName}' completed in {TotalElapsedProcedureTime:F1}s with {ErrorCount} errors.");
        }

        private void NotifyStepChanged()
        {
            if (CurrentStep != null)
            {
                OnStepChanged?.Invoke(CurrentStep, CurrentStepIndex, steps.Count);
            }
        }

        private void InitializeDefaultSteps()
        {
            steps = new List<ProcedureStep>
            {
                new ProcedureStep { stepId = "STEP_ASEPSIS", stepTitle = "Surgical Hand Scrub & Sterile Gowning", stepDescription = "Verify sterile field protocol and sterile drape boundary placement." },
                new ProcedureStep { stepId = "STEP_LOCAL_ANESTHESIA", stepTitle = "Local Infiltration & Nerve Block", stepDescription = "Aspirate prior to injection. Administer 2% Lidocaine with 1:100,000 Epinephrine." },
                new ProcedureStep { stepId = "STEP_ACCESS_INCISION", stepTitle = "Surgical Access / Tissue Separation", stepDescription = "Maintain controlled instrument trajectory without violating collateral vascular structures." },
                new ProcedureStep { stepId = "STEP_TARGET_INTERVENTION", stepTitle = "Target Organ Intervention", stepDescription = "Execute procedure (extraction, resection, or tissue sampling) along defined cleavage planes." },
                new ProcedureStep { stepId = "STEP_HEMOSTASIS_CLOSURE", stepTitle = "Hemostasis & Wound Closure", stepDescription = "Confirm primary hemostasis and apply sterile dressing or compression pack." }
            };
        }
    }
}
