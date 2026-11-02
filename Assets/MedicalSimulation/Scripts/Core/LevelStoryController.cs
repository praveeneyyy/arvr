using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MedicalSimulation.UI;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Story-driven Step-by-Step Clinical Residency Campaign System.
    /// Manages the 6-Act, 18-Step clinical journey and orchestrates 3D modules,
    /// dynamic subsystem inspection, camera focus, and audio cues.
    /// </summary>
    public class LevelStoryController : MonoBehaviour
    {
        public static LevelStoryController Instance { get; private set; }

        [System.Serializable]
        public class ClinicalStep
        {
            public string stepTitle;
            [TextArea(2, 4)] public string narrativeBriefing;
            public string clinicalObjective;
            public string surgicalHint;
            public MedicalComponentType targetComponent;
            public string targetSubsystem;
            public string audioClipName;
            public Vector3 cameraOffset;
        }

        [System.Serializable]
        public class StoryAct
        {
            public int actNumber;
            public string actTitle;
            public string patientProfile;
            public List<ClinicalStep> steps = new List<ClinicalStep>();
        }

        [Header("Clinical Campaign Architecture")]
        [SerializeField] private string campaignTitle = "Charité Surgical Residency: Clinical Case Journey";
        [SerializeField] private List<StoryAct> acts = new List<StoryAct>();

        [Header("Runtime Progression State")]
        public int currentActIndex = 0;
        public int currentStepIndex = 0;
        public bool isStoryModeActive = true;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultCampaign();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            // Sync with workstation UI
            NotifyUIOfCurrentStep();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            NotifyUIOfCurrentStep();
        }

        // =========================================================================
        // 6-ACT, 18-STEP CLINICAL CAMPAIGN DATA
        // =========================================================================
        private void InitializeDefaultCampaign()
        {
            if (acts != null && acts.Count > 0) return;

            acts = new List<StoryAct>
            {
                // ACT 1: Full-Body Anatomy Orientation
                new StoryAct
                {
                    actNumber = 1,
                    actTitle = "Act I: Splanchnic & Musculoskeletal Architecture",
                    patientProfile = "Academic Case: Baseline Anatomical Reference. Charité Surgical Simulation Center.",
                    steps = new List<ClinicalStep>
                    {
                        new ClinicalStep
                        {
                            stepTitle = "Step 1.1: Axial & Appendicular Skeletal Framework",
                            narrativeBriefing = "Welcome to your residency shift, Doctor. Begin by examining the load-bearing osteological framework: cranium, cervical-to-lumbar spine, and cortical ribcage.",
                            clinicalObjective = "Isolate the Skeletal System. Verify vertebral alignment and cortical bone density.",
                            surgicalHint = "Use the Left Anatomy Panel to toggle 'Skeletal' and inspect bone porosity.",
                            targetComponent = MedicalComponentType.FullAnatomy,
                            targetSubsystem = "Skeletal",
                            audioClipName = "lab_ambience",
                            cameraOffset = new Vector3(0.12f, 0.4f, -2.2f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 1.2: Muscular Striations & Aponeuroses",
                            narrativeBriefing = "Examine the dynamic muscular layer. Note the directional fiber striations of the pectoralis, biceps brachii, rectus abdominis, and quadriceps.",
                            clinicalObjective = "Examine the aponeurotic tendinous attachments and muscle fiber architecture.",
                            surgicalHint = "Inspect the glistening ivory tendons anchoring the abdominal wall.",
                            targetComponent = MedicalComponentType.FullAnatomy,
                            targetSubsystem = "Muscular",
                            audioClipName = "ui_confirm",
                            cameraOffset = new Vector3(0.12f, 0.4f, -2.0f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 1.3: Visceral Splanchnic Organs & Cutaway",
                            narrativeBriefing = "Review internal organ placement inside the frosted acrylic cutaway silhouette: bilateral lobed lungs, gastric loop, and retroperitoneal kidneys.",
                            clinicalObjective = "Verify anatomical relationship of internal visceral organs to the ribcage.",
                            surgicalHint = "Observe the frosted acrylic silhouette providing spatial depth without obscuring organs.",
                            targetComponent = MedicalComponentType.FullAnatomy,
                            targetSubsystem = "Integumentary",
                            audioClipName = "ui_select",
                            cameraOffset = new Vector3(0.12f, 0.35f, -1.8f)
                        }
                    }
                },

                // ACT 2: Acute Myocardial Infarction Emergency
                new StoryAct
                {
                    actNumber = 2,
                    actTitle = "Act II: Cardiology Code Blue & Hemodynamics",
                    patientProfile = "Patient: 54-year-old male presenting with acute crushing retrosternal pain. EKG indicates anterior STEMI with ST-elevation in V1-V4.",
                    steps = new List<ClinicalStep>
                    {
                        new ClinicalStep
                        {
                            stepTitle = "Step 2.1: Four-Chamber Isolation & Epicardial Sulci",
                            narrativeBriefing = "CODE BLUE! Emergency in Cardiology. Isolate the four-chamber heart. Inspect ventricular contours, atrial appendages, and the interventricular sulci.",
                            clinicalObjective = "Locate anterior and posterior interventricular sulci bordered by adipose tissue.",
                            surgicalHint = "Notice the creamy pale-yellow epicardial fat pads protecting coronary vessels.",
                            targetComponent = MedicalComponentType.Cardiology,
                            targetSubsystem = "Epicardial Fat Pads",
                            audioClipName = "heartbeat",
                            cameraOffset = new Vector3(0f, 0.2f, -1.9f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 2.2: Coronary Vasculature & LAD Lesion",
                            narrativeBriefing = "Trace the Left Anterior Descending (LAD) coronary artery. Identify the critical stenosis that is starving the anterior myocardial wall.",
                            clinicalObjective = "Trace LAD and RCA arteries against the deep cyanotic venous drainage.",
                            surgicalHint = "The LAD is highlighted in high-gloss arterial crimson with a serous clearcoat.",
                            targetComponent = MedicalComponentType.Cardiology,
                            targetSubsystem = "LAD Coronary Artery",
                            audioClipName = "heartbeat",
                            cameraOffset = new Vector3(0.05f, 0.15f, -1.6f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 2.3: Systolic Perfusion & Cardiac Output",
                            narrativeBriefing = "Evaluate chamber contractility. The left ventricular ejection fraction is depressed at 42%. Prepare for urgent percutaneous coronary intervention (PCI).",
                            clinicalObjective = "Review myocardial Subsurface Scattering and calculate cardiac output (4.2 L/min).",
                            surgicalHint = "Listen to the synchronized ECG monitor and pulsatile valve acoustics.",
                            targetComponent = MedicalComponentType.Cardiology,
                            targetSubsystem = "Left Ventricle",
                            audioClipName = "monitor_ecg_beep",
                            cameraOffset = new Vector3(0f, 0.1f, -1.5f)
                        }
                    }
                },

                // ACT 3: Hepatic Trauma & Biliary System Assessment
                new StoryAct
                {
                    actNumber = 3,
                    actTitle = "Act III: Hepatic Trauma & Biliary Tree Triage",
                    patientProfile = "Patient: 38-year-old female post-motor vehicle collision. Severe right upper quadrant tenderness, guarding, and localized hemoperitoneum.",
                    steps = new List<ClinicalStep>
                    {
                        new ClinicalStep
                        {
                            stepTitle = "Step 3.1: Glisson's Capsule & Parenchyma Inspection",
                            narrativeBriefing = "Perform laparoscopic exploration of the liver. Inspect the smooth, reflective Glisson's peritoneal capsule for lacerations or hematoma.",
                            clinicalObjective = "Inspect right and left liver lobes for parenchymal fracture.",
                            surgicalHint = "Look for the burgundy-brown hepatic sheen and smooth peritoneal reflection.",
                            targetComponent = MedicalComponentType.SoftTissue,
                            targetSubsystem = "Liver Right Lobe",
                            audioClipName = "lab_ambience",
                            cameraOffset = new Vector3(0f, 0.1f, -1.8f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 3.2: 8 Couinaud Segments & Portal Triad",
                            narrativeBriefing = "Delineate functional anatomy according to the 8 Couinaud segments. Isolate the portal triad: hepatic artery, portal vein, and bile duct.",
                            clinicalObjective = "Identify the high-pressure crimson hepatic artery and the low-pressure purple portal vein.",
                            surgicalHint = "The portal vein carries 75% of hepatic inflow; ensure vascular pedicle integrity.",
                            targetComponent = MedicalComponentType.SoftTissue,
                            targetSubsystem = "Portal Triad",
                            audioClipName = "ui_confirm",
                            cameraOffset = new Vector3(0.08f, 0.05f, -1.5f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 3.3: Gallbladder & Biliary Arborization",
                            narrativeBriefing = "Examine the turgid emerald-olive gallbladder and cystic duct. Verify no biliary leakage before completing the trauma staging.",
                            clinicalObjective = "Confirm integrity of the common bile duct and gallbladder mucosal wall.",
                            surgicalHint = "The gallbladder demonstrates natural olive-green translucency with SSS.",
                            targetComponent = MedicalComponentType.SoftTissue,
                            targetSubsystem = "Gallbladder",
                            audioClipName = "ui_select",
                            cameraOffset = new Vector3(0.04f, 0.0f, -1.3f)
                        }
                    }
                },

                // ACT 4: Maxillofacial Surgery & Tooth Extraction
                new StoryAct
                {
                    actNumber = 4,
                    actTitle = "Act IV: Maxillofacial Surgery & Tooth Extraction",
                    patientProfile = "Patient: 26-year-old male with impacted mandibular right third molar (#32) causing severe recurrent pericoronitis and localized trismus.",
                    steps = new List<ClinicalStep>
                    {
                        new ClinicalStep
                        {
                            stepTitle = "Step 4.1: Dental Arch & Dentition Survey",
                            narrativeBriefing = "Review the dental anatomy. Inspect all 32 individual permanent teeth, cortical alveolar bone, and stippled vascular gingiva.",
                            clinicalObjective = "Locate Tooth #32 in the right posterior mandibular quadrant.",
                            surgicalHint = "Each of the 32 teeth has anatomically differentiated crowns and roots.",
                            targetComponent = MedicalComponentType.Dental,
                            targetSubsystem = "Mandible & Maxilla",
                            audioClipName = "dental_scaler",
                            cameraOffset = new Vector3(0f, 0.05f, -0.65f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 4.2: Mandibular Canal & Anesthesia Block",
                            narrativeBriefing = "Assess proximity to the inferior alveolar nerve canal to prevent paresthesia. Administer local anesthesia at the pterygomandibular space.",
                            clinicalObjective = "Confirm deep anesthesia and identify the retromolar surgical landmarks.",
                            surgicalHint = "Verify vascular blanching of the attached gingival margins.",
                            targetComponent = MedicalComponentType.Dental,
                            targetSubsystem = "Gingiva & Gums",
                            audioClipName = "dental_suction",
                            cameraOffset = new Vector3(0.04f, 0.02f, -0.55f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 4.3: Atraumatic Luxation & Root Elevation",
                            narrativeBriefing = "Reflect the mucoperiosteal flap, apply the straight elevator into the periodontal ligament space, and execute atraumatic tooth delivery.",
                            clinicalObjective = "Elevate the roots cleanly without cortical bone fracture.",
                            surgicalHint = "Ensure complete root removal and perform sterile socket debridement.",
                            targetComponent = MedicalComponentType.Dental,
                            targetSubsystem = "Molars (Target #32)",
                            audioClipName = "dental_drill",
                            cameraOffset = new Vector3(0.03f, 0.02f, -0.45f)
                        }
                    }
                },

                // ACT 5: Surgical Metallurgy & Mayo Stand Triage
                new StoryAct
                {
                    actNumber = 5,
                    actTitle = "Act V: Surgical Metallurgy & Mayo Stand Triage",
                    patientProfile = "Perioperative Case: Surgical tray preparation for an open cardiothoracic and vascular procedure in Operating Room 3.",
                    steps = new List<ClinicalStep>
                    {
                        new ClinicalStep
                        {
                            stepTitle = "Step 5.1: AISI 316L Stainless Steel Passivation",
                            narrativeBriefing = "Sterile scrub-in. Inspect the Mayo stand: all instruments are precision-forged AISI 316L stainless steel with 100% metallic response and zero corrosion.",
                            clinicalObjective = "Verify instrument count and inspect surgical steel passivation.",
                            surgicalHint = "Notice the fine anisotropic brushing along instrument shanks.",
                            targetComponent = MedicalComponentType.Procedure,
                            targetSubsystem = "Mayo Instrument Tray",
                            audioClipName = "surgical_instrument_clink",
                            cameraOffset = new Vector3(0f, 0.45f, -0.65f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 5.2: Precision Cutting & Grasping Instruments",
                            narrativeBriefing = "Inspect Scalpel No. 10 with its mirror-polished cutting bevel, atraumatic DeBakey tissue forceps, and curved Metzenbaum dissecting scissors.",
                            clinicalObjective = "Test scissor pivot action and verify blade alignment.",
                            surgicalHint = "Cutting edges feature sharp specular reflections (Roughness 0.05).",
                            targetComponent = MedicalComponentType.Procedure,
                            targetSubsystem = "Scalpel No. 10",
                            audioClipName = "surgical_instrument_clink",
                            cameraOffset = new Vector3(-0.08f, 0.35f, -0.55f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 5.3: Hemostasis, Needle Driver & Electrocautery",
                            narrativeBriefing = "Examine curved Kelly hemostats with box-lock ratchets, tungsten carbide needle driver, and the dual-button electrocautery handpiece.",
                            clinicalObjective = "Confirm electrocautery active electrode tip and clamp ratchet locks.",
                            surgicalHint = "Diamond-knurled finger rings provide non-slip tactile control during surgery.",
                            targetComponent = MedicalComponentType.Procedure,
                            targetSubsystem = "Hemostat Kelly Clamp",
                            audioClipName = "surgical_instrument_clink",
                            cameraOffset = new Vector3(0.08f, 0.35f, -0.55f)
                        }
                    }
                },

                // ACT 6: Virtual Operating Theatre & Residency Graduation
                new StoryAct
                {
                    actNumber = 6,
                    actTitle = "Act VI: Operating Theatre & Residency Graduation",
                    patientProfile = "Operating Theatre Suite: Full surgical team assembled for definitive procedure under general anesthesia with multi-lead monitoring.",
                    steps = new List<ClinicalStep>
                    {
                        new ClinicalStep
                        {
                            stepTitle = "Step 6.1: Sterile Field & Dual 4800K Surgical Lights",
                            narrativeBriefing = "Enter the sterile Operating Suite. Align the dual 4800K LED luminaire reflectors directly over the surgical table for shadowless illumination.",
                            clinicalObjective = "Position luminaires and establish the sterile operating perimeter.",
                            surgicalHint = "4800K neutral daylight provides true color fidelity for tissue differentiation.",
                            targetComponent = MedicalComponentType.OperatingTheatre,
                            targetSubsystem = "Dual 4800K Luminaires",
                            audioClipName = "or_ventilation_ambience",
                            cameraOffset = new Vector3(2.2f, 1.8f, -3.2f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 6.2: Anesthesia Ventilation & Multi-Lead Telemetry",
                            narrativeBriefing = "Review patient vital telemetry: Sinus 72 BPM, SpO2 99%, EtCO2 36 mmHg, Blood Pressure 120/80 mmHg. Anesthesia depth stable.",
                            clinicalObjective = "Verify continuous hemodynamic telemetry and ventilator parameters.",
                            surgicalHint = "Observe the anti-static vinyl floor reflections and illuminated monitor screens.",
                            targetComponent = MedicalComponentType.OperatingTheatre,
                            targetSubsystem = "Patient Vital Monitor",
                            audioClipName = "monitor_ecg_beep",
                            cameraOffset = new Vector3(1.5f, 1.5f, -2.4f)
                        },
                        new ClinicalStep
                        {
                            stepTitle = "Step 6.3: Surgical Completion & Residency Graduation",
                            narrativeBriefing = "Surgical procedure executed with clinical precision! Anatomical closure complete, vital signs stable, and sterile dressing applied. Congratulations Doctor, you have graduated from the Residency Program!",
                            clinicalObjective = "Residency complete. All 6 clinical modules mastered.",
                            surgicalHint = "You can freely revisit any chapter or explore models in sandbox mode.",
                            targetComponent = MedicalComponentType.FullAnatomy,
                            targetSubsystem = "Visceral Overview",
                            audioClipName = "ui_confirm",
                            cameraOffset = new Vector3(0.12f, 0.4f, -2.2f)
                        }
                    }
                }
            };
        }

        // =========================================================================
        // STEP ADVANCEMENT & NOTIFICATION
        // =========================================================================
        public void ApplyStep(int actIdx, int stepIdx)
        {
            if (acts == null || actIdx < 0 || actIdx >= acts.Count) return;
            var act = acts[actIdx];
            if (act.steps == null || stepIdx < 0 || stepIdx >= act.steps.Count) return;

            currentActIndex = actIdx;
            currentStepIndex = stepIdx;

            NotifyUIOfCurrentStep();
        }

        public void AdvanceStep()
        {
            if (acts == null || acts.Count == 0) return;
            var currentAct = acts[currentActIndex];

            if (currentStepIndex + 1 < currentAct.steps.Count)
            {
                ApplyStep(currentActIndex, currentStepIndex + 1);
            }
            else if (currentActIndex + 1 < acts.Count)
            {
                ApplyStep(currentActIndex + 1, 0);
            }
            else
            {
                ApplyStep(0, 0);
            }
        }

        public void NextStep() => AdvanceStep();

        public void PreviousStep()
        {
            if (acts == null || acts.Count == 0) return;

            if (currentStepIndex > 0)
            {
                ApplyStep(currentActIndex, currentStepIndex - 1);
            }
            else if (currentActIndex > 0)
            {
                int prevAct = currentActIndex - 1;
                int lastStep = acts[prevAct].steps.Count - 1;
                ApplyStep(prevAct, lastStep);
            }
        }

        public StoryAct GetCurrentAct()
        {
            if (acts != null && currentActIndex >= 0 && currentActIndex < acts.Count)
                return acts[currentActIndex];
            return null;
        }

        public ClinicalStep GetCurrentStep()
        {
            var act = GetCurrentAct();
            if (act != null && act.steps != null && currentStepIndex >= 0 && currentStepIndex < act.steps.Count)
                return act.steps[currentStepIndex];
            return null;
        }

        public int GetTotalActsCount() => acts != null ? acts.Count : 0;

        private void NotifyUIOfCurrentStep()
        {
            var workstation = MedicalWorkstationUIController.Instance ?? FindFirstObjectByType<MedicalWorkstationUIController>();
            if (workstation != null)
            {
                var act = GetCurrentAct();
                var step = GetCurrentStep();
                if (act != null && step != null)
                {
                    workstation.OnStoryStepChanged(act, step, currentActIndex, currentStepIndex, acts.Count);
                }
            }
        }
    }
}
