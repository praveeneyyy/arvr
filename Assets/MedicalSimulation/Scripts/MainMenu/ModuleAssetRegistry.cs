using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Metadata definition for each medical atlas module in the Project Atlas command centre.
    /// Strictly maintains the exact 6-module clinical ordering defined by Creative Direction.
    /// </summary>
    [System.Serializable]
    public class ModuleMetadata
    {
        public int moduleIndex; // 0 to 5
        public string moduleNumber; // "01", "02", etc.
        public string displayName;
        public string subtitle;
        public string description;
        public string capabilities;
        public string technicalSpecs;
        public string primaryActionLabel;
        public string targetSceneName;
        public string modelAssetName;
        public Vector3 previewCameraOffset;
        public Vector3 previewLookTarget;
        public float previewCameraFOV;
        public string audioCue;
        public Color accentColor;
    }

    /// <summary>
    /// Master repository of all 6 medical modules in exact non-negotiable sequence.
    /// </summary>
    public static class ModuleAssetRegistry
    {
        private static List<ModuleMetadata> _modules;

        public static List<ModuleMetadata> GetAllModules()
        {
            if (_modules == null || _modules.Count == 0)
            {
                InitializeRegistry();
            }
            return _modules;
        }

        public static ModuleMetadata GetModuleByIndex(int index)
        {
            var list = GetAllModules();
            if (index >= 0 && index < list.Count)
                return list[index];
            return list[0];
        }

        private static void InitializeRegistry()
        {
            _modules = new List<ModuleMetadata>
            {
                // MODULE 01 — FULL ANATOMY EXPLORER
                new ModuleMetadata
                {
                    moduleIndex = 0,
                    moduleNumber = "01",
                    displayName = "FULL ANATOMY EXPLORER",
                    subtitle = "Explore the complete human anatomical system.",
                    description = "Comprehensive full-body anatomical atlas featuring balanced half-muscular and half-skeletal cutaway, visceral internal organs, cardiovascular network, respiratory tract, and peripheral innervation.",
                    capabilities = "• 8 Independent Anatomical Systems\n• Real-Time Depth Layer Slicing\n• Independent Organ Opacity Sliders\n• Dynamic Callout Pins & Telemetry\n• 3D Interactive Orbit & Subsystem Index",
                    technicalSpecs = "Model: Human_Torso_Anatomy_Full.fbx | PBR Lit | AgX High Contrast | 4K Cortical & Muscular Striation Maps",
                    primaryActionLabel = "EXPLORE ANATOMY",
                    targetSceneName = "04_AnatomyLab",
                    modelAssetName = "Human_Torso_Anatomy_Full",
                    previewCameraOffset = new Vector3(0.08f, 0.42f, -2.15f),
                    previewLookTarget = new Vector3(0.08f, 0.35f, 0f),
                    previewCameraFOV = 36f,
                    audioCue = "ui_confirm",
                    accentColor = new Color(0f, 0.898f, 1f, 1f) // Medical Cyan
                },

                // MODULE 02 — CARDIOLOGY SUITE
                new ModuleMetadata
                {
                    moduleIndex = 1,
                    moduleNumber = "02",
                    displayName = "CARDIOLOGY SUITE",
                    subtitle = "Explore cardiac anatomy and cardiovascular function.",
                    description = "High-precision 4-chamber human heart model with anatomically mapped Left Anterior Descending (LAD) and Right Coronary Artery (RCA) vasculature, ascending aorta, pulmonary trunk, and epicardial adipose tissue sulci.",
                    capabilities = "• 4-Chamber Morphological Exploration\n• Coronary Vessel Isolation & Flow\n• Dynamic 72 BPM Systolic/Diastolic Pulsing\n• Live Arterial Pressure & Cardiac Output\n• Epicardial Fat & Pericardial Layering",
                    technicalSpecs = "Model: Heart_Detailed.fbx | Subsurface Scattering 0.25 | LAD/RCA Plaque Mapping | Dynamic Shader Pulsing",
                    primaryActionLabel = "EXPLORE HEART",
                    targetSceneName = "heart",
                    modelAssetName = "Heart_Detailed",
                    previewCameraOffset = new Vector3(0f, 0.38f, -1.25f),
                    previewLookTarget = new Vector3(0f, 0.35f, 0f),
                    previewCameraFOV = 34f,
                    audioCue = "heartbeat_deep_pulse",
                    accentColor = new Color(0.94f, 0.28f, 0.35f, 1f) // Cardiac Crimson
                },

                // MODULE 03 — DENTAL SURGERY
                new ModuleMetadata
                {
                    moduleIndex = 2,
                    moduleNumber = "03",
                    displayName = "DENTAL SURGERY",
                    subtitle = "Explore maxillofacial anatomy and dental procedures.",
                    description = "Detailed human mandible and maxillary structures displaying individual dentition, roots, neurovascular alveolar canals, gingival margins, and periodontal ligament attachment zones.",
                    capabilities = "• Mandibular & Maxillary Cortical Bone\n• Individual Tooth Grasp & Extraction Sim\n• Inferior Alveolar Nerve Tracing\n• Specular Enamel & Gingival Shaders\n• Real-Time Luxation & Force Vectors",
                    technicalSpecs = "Model: Dental_Mandible_Teeth.fbx | Dental Enamel Roughness 0.08 | Translucent Gingival SSS | Root Morphology",
                    primaryActionLabel = "ENTER DENTAL LAB",
                    targetSceneName = "Teeth",
                    modelAssetName = "Dental_Mandible_Teeth",
                    previewCameraOffset = new Vector3(0f, 0.38f, -1.20f),
                    previewLookTarget = new Vector3(0f, 0.35f, 0f),
                    previewCameraFOV = 32f,
                    audioCue = "dental_scaler_whir",
                    accentColor = new Color(0.18f, 0.83f, 0.75f, 1f) // Clinical Teal
                },

                // MODULE 04 — HEPATIC AND SOFT TISSUE SURGERY
                new ModuleMetadata
                {
                    moduleIndex = 3,
                    moduleNumber = "04",
                    displayName = "HEPATIC AND SOFT TISSUE SURGERY",
                    subtitle = "Explore hepatic anatomy and soft tissue surgical procedures.",
                    description = "Surgical-grade hepatic system featuring right and left liver lobes, gallbladder, cystic and common bile ducts, and the hepatic portal triad (portal vein, hepatic artery proper, bile duct).",
                    capabilities = "• Hepatic Lobular Anatomy & Resection\n• Cholecystectomy & Gallbladder Dissection\n• Pringle Maneuver Clamping Landmarks\n• Dual Vascular Inflow Modeling\n• Visceral Glisson's Capsule Sheen",
                    technicalSpecs = "Model: Digestive_System.fbx | Visceral Wet Specular | Glisson Capsule Fresnel | Emerald Bile Translucency",
                    primaryActionLabel = "EXPLORE HEPATIC LAB",
                    targetSceneName = "Liver Cut",
                    modelAssetName = "Digestive_System",
                    previewCameraOffset = new Vector3(0f, 0.38f, -1.35f),
                    previewLookTarget = new Vector3(0f, 0.35f, 0f),
                    previewCameraFOV = 34f,
                    audioCue = "ui_confirm",
                    accentColor = new Color(0.96f, 0.62f, 0.18f, 1f) // Hepatic Amber
                },

                // MODULE 05 — SURGICAL PROCEDURE SUITE
                new ModuleMetadata
                {
                    moduleIndex = 4,
                    moduleNumber = "05",
                    displayName = "SURGICAL PROCEDURE SUITE",
                    subtitle = "Explore surgical instruments and procedural techniques.",
                    description = "Autoclaved AISI 316L medical stainless-steel surgical instruments arranged on an ergonomic sterile Mayo tray: Scalpel No. 10, DeBakey Forceps, Metzenbaum Scissors, Hemostat Kelly Clamps, and Electrosurgical RF Pencil.",
                    capabilities = "• Precision AISI 316L Stainless Steel\n• Sharp Dermotomy (Scalpel No. 10)\n• Atraumatic Vascular Grasping (DeBakey)\n• Deep Fascial Dissection (Metzenbaum)\n• Dual RF Cut / Coagulation Pencil",
                    technicalSpecs = "Model: Surgical_Instruments_Tray.fbx | Brushed Steel Anisotropy | Micro-Serrations | 134°C Autoclave Spec",
                    primaryActionLabel = "ENTER PROCEDURE LAB",
                    targetSceneName = "06_ProcedureLab",
                    modelAssetName = "Surgical_Instruments_Tray",
                    previewCameraOffset = new Vector3(0f, 0.40f, -0.95f),
                    previewLookTarget = new Vector3(0f, 0.35f, 0f),
                    previewCameraFOV = 32f,
                    audioCue = "surgical_instrument_clink",
                    accentColor = new Color(0.65f, 0.78f, 0.95f, 1f) // Surgical Steel
                },

                // MODULE 06 — VIRTUAL OPERATING THEATRE
                new ModuleMetadata
                {
                    moduleIndex = 5,
                    moduleNumber = "06",
                    displayName = "VIRTUAL OPERATING THEATRE",
                    subtitle = "Experience an immersive clinical operating environment.",
                    description = "Turnkey virtual operating room with motorized carbon-composite surgical table, draped patient, dual 160,000 lux 4800K LED luminaires, anesthesia workstation ventilator, and real-time hemodynamic telemetry monitor.",
                    capabilities = "• Full 3D Surgical Suite Exploration\n• 5-Lead ECG & Invasive Arterial Telemetry\n• Dual Shadowless LED Light Positioning\n• Positive HEPA Laminar Air Flow\n• Multidisciplinary Surgical Team Layout",
                    technicalSpecs = "Model: Operating_Theatre_Suite.fbx | 160,000 Lux CRI 96 | Live Waveform Canvas | Sevoflurane Circuit",
                    primaryActionLabel = "ENTER OPERATING THEATRE",
                    targetSceneName = "08_VirtualOperatingTheatre",
                    modelAssetName = "Operating_Theatre_Suite",
                    previewCameraOffset = new Vector3(0f, 0.85f, -2.95f),
                    previewLookTarget = new Vector3(0f, 0.40f, 0f),
                    previewCameraFOV = 42f,
                    audioCue = "or_ventilation_ambience",
                    accentColor = new Color(0.2f, 0.85f, 0.45f, 1f) // Clinical Emerald
                }
            };
        }
    }
}
