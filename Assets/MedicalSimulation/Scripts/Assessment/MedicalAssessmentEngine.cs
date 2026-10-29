using UnityEngine;
using System.Collections.Generic;
using MedicalSimulation.Core;
using MedicalSimulation.Audio;

namespace MedicalSimulation.Assessment
{
    /// <summary>
    /// Clinical diagnosis and educational assessment engine.
    /// Manages case presentation, diagnostic reasoning questions,
    /// clinical feedback, scoring, and progress tracking.
    /// </summary>
    public class MedicalAssessmentEngine : MonoBehaviour
    {
        [System.Serializable]
        public class DiagnosticQuestion
        {
            [TextArea(2, 4)] public string questionText;
            public string[] options;
            public int correctOptionIndex;
            [TextArea(2, 4)] public string clinicalRationale;
            public string relatedOrganId;
        }

        [System.Serializable]
        public class ClinicalCase
        {
            public string caseId;
            public string title;
            public string patientVitals;
            [TextArea(3, 6)] public string clinicalPresentation;
            public DiagnosticQuestion[] questions;
        }

        [Header("Cases")]
        [SerializeField] private List<ClinicalCase> cases = new List<ClinicalCase>();

        // ── Runtime State ─────────────────────────────────────────────────────
        public int CurrentCaseIndex { get; private set; } = 0;
        public int CurrentQuestionIndex { get; private set; } = 0;
        public int TotalAnswered { get; private set; } = 0;
        public int TotalCorrect { get; private set; } = 0;
        public bool IsQuizComplete { get; private set; } = false;

        public ClinicalCase ActiveCase => (cases != null && cases.Count > CurrentCaseIndex) ? cases[CurrentCaseIndex] : null;
        public DiagnosticQuestion ActiveQuestion => (ActiveCase != null && ActiveCase.questions != null && ActiveCase.questions.Length > CurrentQuestionIndex) ? ActiveCase.questions[CurrentQuestionIndex] : null;

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<ClinicalCase, DiagnosticQuestion> OnQuestionPresented;
        public event System.Action<bool, string> OnAnswerEvaluated; // isCorrect, rationale
        public event System.Action<float, int, int> OnAssessmentComplete; // scorePercentage, correct, total

        void Awake()
        {
            if (cases == null || cases.Count == 0)
            {
                LoadDefaultClinicalCases();
            }
        }

        void Start()
        {
            PresentCurrentQuestion();
        }

        public void LoadDefaultClinicalCases()
        {
            cases = new List<ClinicalCase>
            {
                // Case 1: Cardiology
                new ClinicalCase
                {
                    caseId = "CASE_CARDIO_01",
                    title = "Acute Chest Pain with Diaphoresis",
                    patientVitals = "BP: 154/92 mmHg | HR: 104 bpm (tachycardia) | SpO2: 95% on room air | Temp: 37.1°C",
                    clinicalPresentation = "A 58-year-old male presents to the Emergency Department with severe, retrosternal crushing chest pressure radiating to the left jaw and left arm. Symptoms began 45 minutes ago during moderate exertion and are accompanied by diaphoresis and nausea. ECG demonstrates 3mm ST-segment elevation in leads V1–V4.",
                    questions = new[]
                    {
                        new DiagnosticQuestion
                        {
                            questionText = "Which anatomical vascular structure is most likely occluded in this patient?",
                            options = new[]
                            {
                                "Right Coronary Artery (RCA)",
                                "Left Anterior Descending Artery (LAD)",
                                "Left Circumflex Artery (LCx)",
                                "Pulmonary Trunk"
                            },
                            correctOptionIndex = 1,
                            clinicalRationale = "ST-segment elevation in leads V1–V4 corresponds to the anteroseptal wall of the left ventricle, which is supplied by the Left Anterior Descending (LAD) coronary artery.",
                            relatedOrganId = "organ.heart"
                        },
                        new DiagnosticQuestion
                        {
                            questionText = "What immediate cellular pathophysiology occurs downstream of this vascular obstruction?",
                            options = new[]
                            {
                                "Loss of ATP synthesis, anaerobic glycolysis, and ischemic myocardial injury",
                                "Bacterial colonization of the endocardial surface",
                                "Excessive pulmonary surfactant accumulation",
                                "Hypertrophy of the Sinoatrial (SA) node"
                            },
                            correctOptionIndex = 0,
                            clinicalRationale = "Interruption of coronary arterial flow causes rapid cessation of aerobic oxidative phosphorylation, intracellular ATP depletion, and lactic acid accumulation leading to ischemic contracture and potential necrosis.",
                            relatedOrganId = "organ.heart"
                        }
                    }
                },
                // Case 2: Pulmonology
                new ClinicalCase
                {
                    caseId = "CASE_RESP_02",
                    title = "Sudden Dyspnea in a Young Adult",
                    patientVitals = "BP: 122/76 mmHg | HR: 110 bpm | RR: 28 /min (tachypnea) | SpO2: 91% on room air",
                    clinicalPresentation = "A 22-year-old tall, slender male presents with acute, sudden-onset right-sided pleuritic chest pain and breathlessness. Physical examination reveals decreased chest expansion on the right, hyperresonance to percussion, and absent vesicular breath sounds over the right hemithorax.",
                    questions = new[]
                    {
                        new DiagnosticQuestion
                        {
                            questionText = "What is the primary anatomical diagnosis based on these clinical findings?",
                            options = new[]
                            {
                                "Left Lobar Pneumonia",
                                "Right-sided Spontaneous Pneumothorax",
                                "Acute Pericarditis",
                                "Foreign Body Aspiration into Left Bronchus"
                            },
                            correctOptionIndex = 1,
                            clinicalRationale = "Sudden pleuritic pain, unilateral hyperresonance, and absent breath sounds in a tall slender young male are classic hallmarks of spontaneous pneumothorax due to rupture of subpleural apical blebs.",
                            relatedOrganId = "organ.right_lung"
                        }
                    }
                },
                // Case 3: Dental / Stomatology
                new ClinicalCase
                {
                    caseId = "CASE_DENTAL_03",
                    title = "Severe Nocturnal Mandibular Pain",
                    patientVitals = "BP: 128/82 mmHg | HR: 76 bpm | Temp: 37.4°C",
                    clinicalPresentation = "A 42-year-old female presents with severe, throbbing pain localized to the mandibular right quadrant. Pain worsens at night and is provoked by hot drinks, lingering for over 45 seconds after stimulus removal. Examination reveals deep occlusal caries on tooth #30 and acute tenderness to vertical percussion.",
                    questions = new[]
                    {
                        new DiagnosticQuestion
                        {
                            questionText = "What is the definitive pulpal and periapical diagnosis?",
                            options = new[]
                            {
                                "Reversible Pulpitis with normal apical tissues",
                                "Symptomatic Irreversible Pulpitis with Symptomatic Apical Periodontitis",
                                "Gingivitis without pulpal involvement",
                                "Enamel hypoplasia"
                            },
                            correctOptionIndex = 1,
                            clinicalRationale = "Prolonged lingering thermal pain (>30 seconds) indicates irreversible pulpal inflammation, while tenderness to percussion indicates inflammatory extension into the apical periodontal ligament.",
                            relatedOrganId = "organ.teeth"
                        }
                    }
                }
            };
        }

        // ── Answering Questions ───────────────────────────────────────────────

        public bool SubmitAnswer(int selectedOptionIndex)
        {
            if (ActiveQuestion == null || IsQuizComplete) return false;

            bool isCorrect = (selectedOptionIndex == ActiveQuestion.correctOptionIndex);
            TotalAnswered++;
            if (isCorrect) TotalCorrect++;

            // Audio cues
            if (isCorrect) MedicalAudioController.Instance?.PlayConfirm();
            else MedicalAudioController.Instance?.PlayError();

            OnAnswerEvaluated?.Invoke(isCorrect, ActiveQuestion.clinicalRationale);

            return isCorrect;
        }

        public void NextQuestion()
        {
            if (ActiveCase == null) return;

            CurrentQuestionIndex++;
            if (CurrentQuestionIndex >= ActiveCase.questions.Length)
            {
                // Advance to next case
                CurrentQuestionIndex = 0;
                CurrentCaseIndex++;

                if (CurrentCaseIndex >= cases.Count)
                {
                    CompleteAssessment();
                    return;
                }
            }

            PresentCurrentQuestion();
        }

        private void PresentCurrentQuestion()
        {
            if (ActiveCase != null && ActiveQuestion != null)
            {
                OnQuestionPresented?.Invoke(ActiveCase, ActiveQuestion);
            }
        }

        private void CompleteAssessment()
        {
            IsQuizComplete = true;
            float score = TotalAnswered > 0 ? (float)TotalCorrect / TotalAnswered : 0f;

            // Record to persistent AppManager
            MedicalAppManager.Instance?.RecordAssessmentCompleted(score);
            MedicalAudioController.Instance?.PlayProcedureComplete();

            OnAssessmentComplete?.Invoke(score, TotalCorrect, TotalAnswered);
            Debug.Log($"[MedicalAssessmentEngine] Assessment finished. Score: {score * 100f:F1}% ({TotalCorrect}/{TotalAnswered})");
        }

        public void RestartAssessment()
        {
            CurrentCaseIndex = 0;
            CurrentQuestionIndex = 0;
            TotalAnswered = 0;
            TotalCorrect = 0;
            IsQuizComplete = false;
            PresentCurrentQuestion();
        }
    }
}
