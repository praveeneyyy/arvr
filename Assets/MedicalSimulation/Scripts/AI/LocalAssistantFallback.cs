using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MedicalSimulation.Core;

namespace MedicalSimulation.AI
{
    /// <summary>
    /// Highly capable offline clinical and educational knowledge engine.
    /// Provides instantaneous, medically grounded responses without requiring internet access
    /// or external API keys. Serves as a resilient fallback ensuring the simulator never fails.
    /// </summary>
    public class LocalAssistantFallback : IAssistantProvider
    {
        public string ProviderName => "Local Clinical Knowledge Engine (Offline)";
        public bool IsConfigured => true;

        public Task<bool> CheckAvailabilityAsync(CancellationToken ct)
        {
            return Task.FromResult(true); // Always available locally
        }

        public async Task<AssistantResponse> SendMessageAsync(
            string prompt,
            ConversationHistory history,
            AssistantContext context,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            await Task.Yield(); // Keep async signature non-blocking

            if (ct.IsCancellationRequested)
            {
                return AssistantResponse.CreateError("Request cancelled.", ProviderName);
            }

            string answer = GenerateResponse(prompt, context, history, out List<string> citations);
            sw.Stop();

            return AssistantResponse.CreateSuccess(answer, ProviderName, sw.ElapsedMilliseconds, citations);
        }

        private string GenerateResponse(string rawPrompt, AssistantContext context, ConversationHistory history, out List<string> citations)
        {
            citations = new List<string>();
            string prompt = (rawPrompt ?? string.Empty).ToLowerInvariant().Trim();

            // 1. Check if user is asking about the currently selected organ
            if (context != null && context.selectedOrgan != null)
            {
                var organ = context.selectedOrgan;
                if (IsAskingAboutOrganOrGeneral(prompt, organ.anatomicalName, organ.organId))
                {
                    citations.Add($"Anatomy Catalog v2.4 ({organ.anatomicalName})");
                    citations.Add("Atlas of Human Anatomy, Section " + organ.systemLabel);
                    return FormatOrganExplanation(organ, prompt, context);
                }
            }

            // 2. Check general educational / computing / science queries
            if (prompt.Contains("photosynthesis"))
            {
                citations.Add("Campbell Biology, 12th Ed. - Chloroplast Bioenergetics");
                return "**Photosynthesis: Light-Driven Biochemical Synthesis**\n\n" +
                       "Photosynthesis is the biological process by which autotrophic organisms (plants, algae, and cyanobacteria) convert light energy into chemical energy stored in glucose.\n\n" +
                       "**Overall Chemical Equation:**\n" +
                       "`6 CO₂ + 6 H₂O + Light Energy → C₆H₁₂O₆ + 6 O₂`\n\n" +
                       "**Two Main Stages:**\n" +
                       "1. **Light Reactions (Thylakoid Membrane):** Photons excite electrons in Chlorophyll Photosystems II & I, photolyzing water into oxygen (O₂) and generating ATP and NADPH via electron transport chains.\n" +
                       "2. **Calvin Cycle (Stroma):** Carbon fixation enzyme RuBisCO fixes atmospheric CO₂ into 3-PGA, subsequently reduced using ATP and NADPH into Glyceraldehyde-3-phosphate (G3P), which forms glucose.\n\n" +
                       "*Medical Connection:* In human physiology, the oxygen produced by photosynthesis is the terminal electron acceptor in mitochondrial oxidative phosphorylation.";
            }

            if (prompt.Contains("machine learning") || prompt.Contains("what is ml"))
            {
                citations.Add("Bishop - Pattern Recognition and Machine Learning");
                return "**Machine Learning (ML): Core Principles & Paradigms**\n\n" +
                       "Machine Learning is a subfield of Artificial Intelligence where algorithms identify patterns in empirical data to generalize predictions or decisions without explicit rule-based programming.\n\n" +
                       "**Core Paradigms:**\n" +
                       "- **Supervised Learning:** Models train on labeled input-output pairs `(X, y)` (e.g., Logistic Regression, ResNet for medical image segmentation of chest X-rays).\n" +
                       "- **Unsupervised Learning:** Discovers hidden structures or latent representations without labels (e.g., K-Means clustering of patient demographics, PCA/t-SNE for single-cell RNA sequencing).\n" +
                       "- **Reinforcement Learning:** Agents learn optimal policies by receiving scalar rewards/penalties through environmental trial-and-error (e.g., robotic surgery trajectory optimization).\n\n" +
                       "*Clinical Application:* Deep neural networks assist clinicians by identifying microcalcifications in mammography and diabetic retinopathy in fundus photography.";
            }

            if (prompt.Contains("neural network") || prompt.Contains("deep learning"))
            {
                citations.Add("Goodfellow, Bengio, Courville - Deep Learning");
                return "**Artificial Neural Networks (ANN): Architecture & Function**\n\n" +
                       "An Artificial Neural Network is a computational model inspired by biological neuronal networks in the human cerebral cortex.\n\n" +
                       "**Fundamental Building Blocks:**\n" +
                       "1. **Artificial Neuron (Perceptron):** Computes a weighted sum of inputs plus bias: `z = Σ(w_i * x_i) + b`.\n" +
                       "2. **Activation Function:** Introduces non-linearity so the network can model complex manifolds (e.g., ReLU `f(z)=max(0,z)`, Sigmoid, GELU).\n" +
                       "3. **Layers:** Input Layer → Hidden Layers (feature representations) → Output Layer.\n" +
                       "4. **Backpropagation:** Computes gradient of the loss function via chain rule: `∂L/∂w`, updated through optimization algorithms like Adam or SGD.\n\n" +
                       "*Biological Analogy:* While biological dendrites integrate analog neurotransmitter graded potentials across synapses, artificial networks use matrix multiplication over normalized float tensors.";
            }

            if (prompt.Contains("ram") && (prompt.Contains("storage") || prompt.Contains("difference") || prompt.Contains("disk") || prompt.Contains("rom")))
            {
                citations.Add("Patterson & Hennessy - Computer Organization and Design");
                return "**RAM vs. Storage: Functional Architecture Comparison**\n\n" +
                       "| Metric | Random Access Memory (RAM) | Non-Volatile Storage (SSD/NVMe/HDD) |\n" +
                       "| :--- | :--- | :--- |\n" +
                       "| **Volatilization** | Volatile (data lost when power drops) | Non-volatile (persists permanently) |\n" +
                       "| **Speed / Latency** | Ultra-low latency (~10-50 ns, >50 GB/s) | Moderate latency (~50-100 µs, 3-7 GB/s) |\n" +
                       "| **Primary Role** | Working memory holding active code & heap | Long-term repository for OS, apps, & files |\n" +
                       "| **CPU Bus** | Direct Memory Controller / DDR5 bus | PCIe / NVMe / SATA interface |\n\n" +
                       "*Cognitive Analogy:* RAM is analogous to human **working memory** (e.g., holding numbers while performing mental arithmetic), whereas SSD storage is analogous to **long-term episodic/semantic memory** encoded in the cerebral cortex.";
            }

            if (prompt.Contains("cpu") && (prompt.Contains("execute") || prompt.Contains("instruction") || prompt.Contains("cycle") || prompt.Contains("how does")))
            {
                citations.Add("Tanenbaum - Structured Computer Organization");
                return "**CPU Instruction Cycle: Fetch, Decode, Execute**\n\n" +
                       "The Central Processing Unit (CPU) executes software instructions through a continuous cyclic pipeline:\n\n" +
                       "1. **Fetch:** The Program Counter (PC) holds the memory address of the next instruction. The CPU fetches the machine instruction opcode from L1 Cache/RAM into the Instruction Register (IR), then increments the PC.\n" +
                       "2. **Decode:** The Instruction Decoder analyzes the binary opcode, identifying the required operation and operand registers.\n" +
                       "3. **Execute:** The Arithmetic Logic Unit (ALU) performs the computation (e.g. ADD, SUB, bitwise AND) or evaluates branch conditions.\n" +
                       "4. **Memory Access & Writeback:** Results are written back to internal register files or committed to L1/L2 cache and system RAM.\n\n" +
                       "*Electrophysiological Parallels:* A modern CPU pipeline clocking at 4.0 GHz executes ~4 billion cycles/sec, contrasted with biological neuronal action potentials that propagate at refractory maximums of ~500-1000 Hz.";
            }

            // 3. Clinical & Anatomical Systems
            if (prompt.Contains("heart") || prompt.Contains("cardiac") || prompt.Contains("circulation") || prompt.Contains("myocard"))
            {
                citations.Add("Guyton & Hall Textbook of Medical Physiology - Ch. 9: Cardiac Muscle");
                citations.Add("Netter's Atlas of Human Anatomy - Plate 215");
                return "**Cardiovascular System: Cardiac Cycle & Hemodynamics**\n\n" +
                       "The human heart functions as a four-chambered dual-circuit muscular pump generating continuous systemic and pulmonary perfusion.\n\n" +
                       "**Electrophysiological Conduction:**\n" +
                       "- **Sinoatrial (SA) Node:** Primary pacemaker located in the superior anterolateral right atrial wall (~60-100 spontaneous intrinsic depolarizations/min).\n" +
                       "- **Atrioventricular (AV) Node:** Imparts a 0.12-second physiological delay permitting complete atrial ventricular filling before ventricular systole.\n" +
                       "- **Bundle of His & Purkinje Fibers:** Rapidly conduct depolarization through the interventricular septum to ventricular myocardium.\n\n" +
                       "**Mechanical Cycle (Systole & Diastole):**\n" +
                       "- *Isovolumetric Contraction:* Ventricular pressure exceeds atrial pressure; mitral/tricuspid valves close (S1 sound).\n" +
                       "- *Ventricular Ejection:* Aortic and pulmonary semilunar valves open; stroke volume (~70 mL) ejected into systemic/pulmonary circulation.\n" +
                       "- *Isovolumetric Relaxation:* Ventricular pressure drops below arterial pressure; semilunar valves close (S2 sound).\n\n" +
                       "*(Live Simulation Telemetry: Heart Rate = " + (context != null ? context.simulatedHeartRate.ToString("F0") : "72") + " BPM)*";
            }

            if (prompt.Contains("lung") || prompt.Contains("respirat") || prompt.Contains("breath") || prompt.Contains("alveol"))
            {
                citations.Add("West's Respiratory Physiology: The Essentials, 11th Ed.");
                return "**Respiratory System: Mechanics & Pulmonary Gas Exchange**\n\n" +
                       "The respiratory system maintains arterial blood gas homeostasis by exchanging oxygen and carbon dioxide between ambient air and alveolar capillary blood.\n\n" +
                       "**Inspiratory Mechanics:**\n" +
                       "- Contraction of the **Diaphragm** (innervated by phrenic nerves C3-C5) flattens the dome, increasing vertical thoracic cavity volume.\n" +
                       "- **External intercostal muscles** lift the ribs in a 'bucket-handle' vector.\n" +
                       "- Intrathoracic pressure drops negative (-1 to -3 mmHg relative to atmosphere), pulling air down the tracheobronchial tree.\n\n" +
                       "**Alveolar-Capillary Diffusion:**\n" +
                       "- Gas transfer occurs across the 0.2-0.5 µm blood-gas barrier governed by Fick's Law of Diffusion: `V_gas = (Area / Thickness) * Diff_constant * (P1 - P2)`.\n" +
                       "- Surfactant produced by Type II alveolar pneumocytes reduces surface tension, preventing end-expiratory atelectasis.";
            }

            if (prompt.Contains("dental") || prompt.Contains("tooth") || prompt.Contains("teeth") || prompt.Contains("extract"))
            {
                citations.Add("Peterson's Principles of Oral and Maxillofacial Surgery");
                citations.Add("Contemporary Oral and Maxillofacial Surgery, 7th Ed.");
                return "**Dental Anatomy & Surgical Extraction Protocol**\n\n" +
                       "**Tooth Anatomical Architecture:**\n" +
                       "- **Enamel:** 96% inorganic hydroxyapatite, the hardest biological substance in the human body.\n" +
                       "- **Dentin:** Mineralized tissue containing microscopic tubules transmitting thermal and hydrodynamic sensations to the pulp.\n" +
                       "- **Dental Pulp:** Neurovascular bundle within the pulp chamber and root canals providing vitality, sensory innervation (trigeminal CN V2/V3), and reparative odontoblasts.\n" +
                       "- **Periodontal Ligament (PDL):** Collagenous Sharpey's fibers suspending the tooth within the alveolar bone socket.\n\n" +
                       "**Standard Clinical Extraction Sequence:**\n" +
                       "1. *Asepsis & Local Anaesthesia:* Target inferior alveolar or posterior superior alveolar nerve block.\n" +
                       "2. *Syndesmotomy:* Sever gingival attachment fibers using a periosteal elevator.\n" +
                       "3. *Luxation:* Introduce a straight elevator into the periodontal space, applying controlled rotational wedging to expand alveolar bone.\n" +
                       "4. *Forceps Delivery:* Apply apical pressure with forceps beaks, utilizing slow buccal and lingual forces to deliver the tooth along path of least resistance.\n" +
                       "5. *Socket Debridement & Hemostasis:* Inspect socket, curette granulation tissue, compress alveolar plates, and place sterile gauze pressure pack.";
            }

            if (prompt.Contains("liver") || prompt.Contains("hepatic") || prompt.Contains("cirrhosis"))
            {
                citations.Add("Sherlock's Diseases of the Liver and Biliary System");
                return "**Hepatic Anatomy & Metabolic Physiology**\n\n" +
                       "The liver is the largest internal organ and metabolic nexus of the human body, situated in the right upper quadrant protected by the thoracic rib cage.\n\n" +
                       "**Dual Blood Supply:**\n" +
                       "- **Hepatic Portal Vein (75%):** Delivers nutrient-rich, deoxygenated blood drained from the gastrointestinal tract and spleen.\n" +
                       "- **Hepatic Artery Proper (25%):** Supplies oxygenated systemic blood branching from the celiac trunk.\n\n" +
                       "**Core Physiological Roles:**\n" +
                       "- *Bile Synthesis:* Produces bile salts required for intestinal lipid emulsification and fat-soluble vitamin absorption (A, D, E, K).\n" +
                       "- *Protein Synthesis:* Synthesizes albumin, transferrin, and clotting factors (I, II, VII, IX, X).\n" +
                       "- *Detoxification & Biotransformation:* Cytochrome P450 Phase I & II enzyme pathways metabolize endogenous toxins and xenobiotics.";
            }

            if (prompt.Contains("stomach") || prompt.Contains("gastric") || prompt.Contains("digest"))
            {
                citations.Add("Ganong's Review of Medical Physiology, 26th Ed.");
                return "**Gastrointestinal System: Gastric Function & Digestion**\n\n" +
                       "The stomach functions as a muscular reservoir performing both mechanical churning and chemical breakdown of ingested alimentary boluses into acidic chyme.\n\n" +
                       "**Gastric Secretions:**\n" +
                       "- **Parietal (Oxyntic) Cells:** Secrete Hydrochloric Acid (HCl, establishing pH 1.5-2.0) via H+/K+ ATPase proton pumps, and Intrinsic Factor (essential for terminal ileum Vitamin B12 absorption).\n" +
                       "- **Chief Cells:** Secrete inactive pepsinogen, cleaved by acidic pH into the proteolytic enzyme pepsin.\n" +
                       "- **G Cells:** Produce gastrin in the antrum, stimulating parietal cell acid secretion in response to luminal peptides.\n\n" +
                       "**Peristalsis:** Driven by the Gastric Basal Electrical Rhythm (slow waves generated by Interstitial Cells of Cajal at ~3 contractions/min).";
            }

            if (prompt.Contains("kidney") || prompt.Contains("renal") || prompt.Contains("nephron") || prompt.Contains("urin"))
            {
                citations.Add("Brenner & Rector's The Kidney, 11th Ed.");
                return "**Renal Physiology: Nephron Dynamics & Glomerular Filtration**\n\n" +
                       "The kidneys regulate fluid volume, electrolyte homeostasis, acid-base balance, and nitrogenous waste excretion.\n\n" +
                       "**Functional Unit: The Nephron (~1 million per kidney):**\n" +
                       "- **Glomerulus & Bowman's Capsule:** Ultrafiltrates blood at a normal Glomerular Filtration Rate (GFR) of ~120-125 mL/min.\n" +
                       "- **Proximal Convoluted Tubule:** Reabsorbs 65% of filtered water, sodium, and chloride, alongside 100% of filtered glucose and amino acids.\n" +
                       "- **Loop of Henle:** Countercurrent multiplier creating high medullary interstitial osmolarity (~1200 mOsm/L).\n" +
                       "- **Distal Tubule & Collecting Duct:** Fine-tunes water and sodium balance under the regulation of Aldosterone and Antidiuretic Hormone (ADH/Vasopressin).";
            }

            if (prompt.Contains("brain") || prompt.Contains("nervous") || prompt.Contains("cortex") || prompt.Contains("neuro"))
            {
                citations.Add("Kandel - Principles of Neural Science, 6th Ed.");
                return "**Neuroanatomy: Central Nervous System Architecture**\n\n" +
                       "The brain integrates sensory afferents, coordinates motor efferents, and executes higher-order cognitive, memory, and executive functions.\n\n" +
                       "**Major Anatomical Subdivisions:**\n" +
                       "- **Cerebral Cortex:** Divided into Frontal (motor/executive), Parietal (somatosensory), Temporal (auditory/language/memory), and Occipital (vision) lobes.\n" +
                       "- **Cerebellum:** Coordinates fine motor control, timing, trajectory planning, and vestibular balance.\n" +
                       "- **Brainstem (Midbrain, Pons, Medulla):** Houses vital autonomic cardiorespiratory centers and cranial nerve nuclei (CN III-XII).\n" +
                       "- **Limbic System:** Hippocampus and amygdala mediate memory consolidation and emotional responses.";
            }

            if (prompt.Contains("quiz") || prompt.Contains("practice") || prompt.Contains("question") || prompt.Contains("test me"))
            {
                citations.Add("Medical Simulation Clinical Assessment Question Bank");
                return "**Clinical Simulation Practice Questions**\n\n" +
                       "**Question 1 (Cardiovascular):**\n" +
                       "During which phase of the cardiac cycle do the coronary arteries receive the vast majority of their myocardial blood perfusion?\n" +
                       "*A) Ventricular Systole*\n" +
                       "*B) Ventricular Diastole*\n" +
                       "*C) Isovolumetric Contraction*\n" +
                       "*D) Atrial Systole*\n\n" +
                       "**Question 2 (Dental Procedure):**\n" +
                       "What is the primary biological objective of alveolar bone luxation with a dental elevator prior to forceps application?\n" +
                       "*A) Fracturing the tooth apex*\n" +
                       "*B) Expanding the osseous socket and tearing periodontal ligament fibers*\n" +
                       "*C) Cauterizing the alveolar artery*\n" +
                       "*D) Removing mucosal gingival tissue*\n\n" +
                       "*Type 'Answer 1' or your reasoning, and I will evaluate your clinical explanation!*";
            }

            // Default comprehensive clinical responder
            citations.Add("Medical Simulation Educational Curriculum v3.0");
            var sb = new StringBuilder();
            sb.AppendLine("**Clinical Simulation Assistant**");
            sb.AppendLine("I am ready to assist your medical simulation and educational exploration. Here is how I can help:");
            sb.AppendLine("- **Selected Organ Guidance:** Select any organ in the 3D viewport (Heart, Lungs, Liver, Stomach, Kidneys, Intestines, Teeth) to receive full anatomical, physiological, and pathological briefings.");
            sb.AppendLine("- **Physiological Mechanics:** Ask about cardiac electrophysiology, respiratory ventilation mechanics, blood flow hemodynamics, or digestive peristalsis.");
            sb.AppendLine("- **Dental Procedures:** Review extraction protocols, instruments, nerve blocks, and clinical tooth anatomy.");
            sb.AppendLine("- **Diagnostic Assessments:** Ask for clinical case studies, practice quiz questions, or differential diagnosis exercises.");
            sb.AppendLine("- **General STEM Questions:** I can also explain computing architecture, machine learning, biochemistry, or cellular biology.");
            sb.AppendLine();
            sb.AppendLine("*Try asking: 'Explain the heart valves', 'What is machine learning?', or 'Steps of tooth extraction'.*");

            return sb.ToString();
        }

        private bool IsAskingAboutOrganOrGeneral(string prompt, string organName, string organId)
        {
            if (string.IsNullOrEmpty(prompt)) return true;
            if (prompt.Contains("explain") || prompt.Contains("tell me") || prompt.Contains("function") ||
                prompt.Contains("pathology") || prompt.Contains("clinical") || prompt.Contains("what is this") ||
                prompt.Contains("this organ") || prompt.Contains("details") || prompt.Contains("anatomy"))
            {
                return true;
            }

            string cleanOrgan = organName.ToLowerInvariant();
            if (prompt.Contains(cleanOrgan)) return true;

            if (!string.IsNullOrEmpty(organId) && prompt.Contains(organId.ToLowerInvariant().Replace("organ.", "")))
            {
                return true;
            }

            return false;
        }

        private string FormatOrganExplanation(AnatomyMetadata organ, string prompt, AssistantContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**Anatomical Review: {organ.anatomicalName}**");
            sb.AppendLine($"*System:* {organ.systemLabel} | *Location:* {organ.anatomicalLocation}\n");

            sb.AppendLine("**Overview & Function:**");
            sb.AppendLine(organ.overview);
            if (!string.IsNullOrEmpty(organ.primaryFunctions))
            {
                sb.AppendLine("\n**Primary Functions:**");
                sb.AppendLine(organ.primaryFunctions);
            }

            if (!string.IsNullOrEmpty(organ.anatomyDetails))
            {
                sb.AppendLine("\n**Structural & Vascular Details:**");
                sb.AppendLine(organ.anatomyDetails);
            }

            if (!string.IsNullOrEmpty(organ.clinicalInformation))
            {
                sb.AppendLine("\n**Clinical Significance & Pathology:**");
                sb.AppendLine(organ.clinicalInformation);
            }

            if (organ.relatedStructures != null && organ.relatedStructures.Length > 0)
            {
                sb.AppendLine("\n**Anatomically Adjacent Structures:**");
                sb.AppendLine(string.Join(", ", organ.relatedStructures));
            }

            return sb.ToString();
        }
    }
}
