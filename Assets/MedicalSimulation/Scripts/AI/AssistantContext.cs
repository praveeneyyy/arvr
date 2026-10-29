using System;
using System.Text;
using UnityEngine;
using MedicalSimulation.Core;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Encapsulates the live runtime context of the medical simulation, including
    /// currently selected anatomical structures, active procedure steps, and physiological telemetry.
    /// Injects structured context into AI prompts and provides clinical education guardrails.
    /// </summary>
    [Serializable]
    public class AssistantContext
    {
        public string activeSceneName;
        public AnatomyMetadata selectedOrgan;
        public string currentProcedureName;
        public string currentProcedureStep;
        public float simulatedHeartRate = 72f;
        public float simulatedRespirationRate = 16f;
        public float simulatedBloodPressureSystolic = 120f;
        public float simulatedBloodPressureDiastolic = 80f;
        public bool isVREnabled;

        public bool HasSelectedOrgan => selectedOrgan != null;

        public void SetSelectedOrgan(AnatomyMetadata organ)
        {
            selectedOrgan = organ;
        }

        public void ClearSelectedOrgan()
        {
            selectedOrgan = null;
        }

        public void SetProcedureContext(string procedureName, string stepDescription)
        {
            currentProcedureName = procedureName;
            currentProcedureStep = stepDescription;
        }

        /// <summary>
        /// Generates the base system prompt containing medical education guidelines,
        /// safety guardrails, and instructions for answering both clinical and general questions.
        /// </summary>
        public string BuildSystemInstruction()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the Clinical AI Assistant embedded within an advanced Medical AR/VR Simulation Platform.");
            sb.AppendLine("Your primary role is to serve as an expert medical educator, anatomy tutor, clinical simulation guide, and knowledgeable STEM instructor.");
            sb.AppendLine();
            sb.AppendLine("CORE OPERATIONAL GUIDELINES:");
            sb.AppendLine("1. MEDICAL ACCURACY: Ground anatomy, physiology, and procedural explanations in verified medical consensus (e.g. Gray's Anatomy, standard medical curricula).");
            sb.AppendLine("2. DUAL-MODE KNOWLEDGE: You answer both specialized medical/anatomical queries AND general educational, scientific, and computing questions (e.g. Machine Learning, photosynthesis, RAM vs storage, physics). Never refuse general questions; answer them with clarity and pedagogical precision.");
            sb.AppendLine("3. CLINICAL CONTEXT AWARENESS: When an anatomical structure is actively inspected in the simulation, ground your explanation in that specific structure's functions, relations, and clinical relevance.");
            sb.AppendLine("4. EDUCATIONAL SAFETY DISCLAIMER: This platform is designed exclusively for medical simulation, education, and anatomical exploration. Clarify when appropriate that explanations are for educational and training purposes, not real-world patient diagnosis or medical prescription.");
            sb.AppendLine("5. STRUCTURED FORMATTING: Use clean, concise sections, bullet points, and highlight key anatomical structures or scientific concepts for readability in VR and desktop viewports.");

            return sb.ToString();
        }

        /// <summary>
        /// Formats the live runtime simulation state (active organ, physiology, active procedure)
        /// into a structured context string to be appended to the user prompt or system turn.
        /// </summary>
        public string BuildRuntimeContextString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[LIVE MEDICAL SIMULATION RUNTIME CONTEXT]");

            if (!string.IsNullOrEmpty(activeSceneName))
            {
                sb.AppendLine($"- Active Environment: {activeSceneName}");
            }

            // Physiology telemetry
            sb.AppendLine($"- Patient Physiology: Heart Rate = {simulatedHeartRate:F0} BPM, Respiration = {simulatedRespirationRate:F0} Br/min, BP = {simulatedBloodPressureSystolic:F0}/{simulatedBloodPressureDiastolic:F0} mmHg");

            // Selected anatomical structure
            if (selectedOrgan != null)
            {
                sb.AppendLine($"- Currently Inspected Organ: {selectedOrgan.anatomicalName} (ID: {selectedOrgan.organId})");
                sb.AppendLine($"  * System: {selectedOrgan.systemLabel} ({selectedOrgan.system})");
                sb.AppendLine($"  * Anatomical Location: {selectedOrgan.anatomicalLocation}");
                if (!string.IsNullOrEmpty(selectedOrgan.primaryFunctions))
                    sb.AppendLine($"  * Primary Functions: {selectedOrgan.primaryFunctions}");
                if (!string.IsNullOrEmpty(selectedOrgan.anatomyDetails))
                    sb.AppendLine($"  * Anatomical Details: {selectedOrgan.anatomyDetails}");
                if (!string.IsNullOrEmpty(selectedOrgan.clinicalInformation))
                    sb.AppendLine($"  * Clinical Significance / Pathology: {selectedOrgan.clinicalInformation}");
                if (selectedOrgan.relatedStructures != null && selectedOrgan.relatedStructures.Length > 0)
                    sb.AppendLine($"  * Adjacent Structures: {string.Join(", ", selectedOrgan.relatedStructures)}");
            }
            else
            {
                sb.AppendLine("- Currently Inspected Organ: None (Full body / systemic overview)");
            }

            // Active procedure step
            if (!string.IsNullOrEmpty(currentProcedureName))
            {
                sb.AppendLine($"- Active Clinical Procedure: {currentProcedureName}");
                if (!string.IsNullOrEmpty(currentProcedureStep))
                {
                    sb.AppendLine($"  * Current Step: {currentProcedureStep}");
                }
            }

            sb.AppendLine("[END RUNTIME CONTEXT]");
            return sb.ToString();
        }
    }
}
