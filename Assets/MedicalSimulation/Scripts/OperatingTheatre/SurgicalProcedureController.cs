using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedicalSimulation.OperatingTheatre
{
    /// <summary>
    /// Educational Surgical Procedure Simulation Engine.
    /// Guides the medical student or resident through real surgical stages:
    /// Time-out, Anesthesia, Draping, Incision, Exposure, Dissection, Closure.
    /// Tracks protocol compliance, sterile field violations, and educational milestones.
    /// </summary>
    public class SurgicalProcedureController : MonoBehaviour
    {
        public enum ProcedureStage
        {
            PreopTimeOut,
            AnesthesiaInduction,
            AntisepticPrepAndDraping,
            SkinIncision,
            HemostasisAndExposure,
            OrganInspectionAndResection,
            CountAndClosure,
            ProcedureComplete
        }

        [System.Serializable]
        public struct StageData
        {
            public ProcedureStage stage;
            public string title;
            [TextArea(2, 4)] public string clinicalObjective;
            public string requiredInstrument;
            public string clinicalHint;
        }

        [Header("Procedure Setup")]
        [SerializeField] private string procedureName = "Exploratory Laparotomy & Hepatic Resection";
        [SerializeField] private ProcedureStage currentStage = ProcedureStage.PreopTimeOut;
        [SerializeField] private List<StageData> stages = new List<StageData>();

        [Header("Performance")]
        [SerializeField] private int currentScore = 100;
        [SerializeField] private int errorCount = 0;

        public event Action<ProcedureStage, StageData> OnStageAdvanced;
        public event Action<int> OnScoreUpdated;

        public string ProcedureName => procedureName;
        public ProcedureStage CurrentStage => currentStage;
        public int Score => currentScore;
        public int Errors => errorCount;

        void Awake()
        {
            InitializeDefaultStages();
        }

        private void InitializeDefaultStages()
        {
            if (stages.Count > 0) return;

            stages.Add(new StageData {
                stage = ProcedureStage.PreopTimeOut,
                title = "1. WHO Surgical Safety Time-Out",
                clinicalObjective = "Confirm patient identity, surgical procedure, allergy history, and prophylactic antibiotics.",
                requiredInstrument = "Checklist / Verbal Confirmation",
                clinicalHint = "Lead surgeon, nurse, and anesthesiologist must pause and agree before incision."
            });

            stages.Add(new StageData {
                stage = ProcedureStage.AnesthesiaInduction,
                title = "2. General Anesthesia & Airway Verification",
                clinicalObjective = "Verify endotracheal tube placement, muscle relaxation, and stable hemodynamic parameters.",
                requiredInstrument = "Anesthesia Monitor",
                clinicalHint = "Ensure SpO2 > 98% and blood pressure is controlled before positioning."
            });

            stages.Add(new StageData {
                stage = ProcedureStage.AntisepticPrepAndDraping,
                title = "3. Antiseptic Prep & Sterile Draping",
                clinicalObjective = "Prep abdominal skin with chlorhexidine-alcohol; apply sterile drapes leaving laparotomy aperture.",
                requiredInstrument = "Surgical Drape Clamp",
                clinicalHint = "Drape borders must maintain a 30cm sterile field boundary."
            });

            stages.Add(new StageData {
                stage = ProcedureStage.SkinIncision,
                title = "4. Midline Laparotomy Skin Incision",
                clinicalObjective = "Execute a clean, controlled midline incision through epidermis, dermis, and subcutaneous fat.",
                requiredInstrument = "Scalpel #10",
                clinicalHint = "Apply tension with non-dominant hand; keep scalpel blade perpendicular to the tissue."
            });

            stages.Add(new StageData {
                stage = ProcedureStage.HemostasisAndExposure,
                title = "5. Hemostasis & Surgical Exposure",
                clinicalObjective = "Coagulate superficial bleeding vessels and position abdominal self-retaining retractors.",
                requiredInstrument = "Bovie Electrocautery & Retractor",
                clinicalHint = "Use suction to clear the surgical field before dividing the linea alba."
            });

            stages.Add(new StageData {
                stage = ProcedureStage.OrganInspectionAndResection,
                title = "6. Visceral Organ Inspection & Target Resection",
                clinicalObjective = "Inspect liver lobes, gallbladder, and visceral vessels. Execute anatomical wedge resection.",
                requiredInstrument = "Metzenbaum Scissors & Forceps",
                clinicalHint = "Identify the anatomical landmarks and hepatic portal triad before proceeding."
            });

            stages.Add(new StageData {
                stage = ProcedureStage.CountAndClosure,
                title = "7. Sponge Count & Multi-Layer Closure",
                clinicalObjective = "Complete surgical sponge, needle, and instrument counts. Suture fascia and close skin.",
                requiredInstrument = "Needle Holder & Suture",
                clinicalHint = "A correct instrument count must be confirmed by the scrub nurse before closure."
            });
        }

        public StageData GetCurrentStageData()
        {
            foreach (var s in stages)
            {
                if (s.stage == currentStage) return s;
            }
            return new StageData {
                stage = currentStage,
                title = currentStage.ToString(),
                clinicalObjective = "Complete surgical protocol.",
                requiredInstrument = "Standard",
                clinicalHint = ""
            };
        }

        public void AdvanceToNextStage()
        {
            int nextIdx = (int)currentStage + 1;
            if (nextIdx <= (int)ProcedureStage.CountAndClosure)
            {
                currentStage = (ProcedureStage)nextIdx;
            }
            else
            {
                currentStage = ProcedureStage.ProcedureComplete;
            }

            OnStageAdvanced?.Invoke(currentStage, GetCurrentStageData());
        }

        public void RecordClinicalError(string reason, int penalty = 5)
        {
            errorCount++;
            currentScore = Mathf.Max(0, currentScore - penalty);
            OnScoreUpdated?.Invoke(currentScore);
            Debug.LogWarning($"[SurgicalProcedureController] Clinical Error: {reason} (-{penalty} pts). Score: {currentScore}");
        }
    }
}
