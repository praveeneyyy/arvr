using UnityEngine;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// ScriptableObject that stores metadata for a single anatomical structure.
    /// Assign one per organ/region in the Project window.
    /// </summary>
    [CreateAssetMenu(fileName = "AnatomyData_New", menuName = "MedicalSimulation/Anatomy Metadata")]
    public class AnatomyMetadata : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Stable identifier used in code. e.g. organ.heart")]
        public string organId;

        [Tooltip("Human-readable anatomical name.")]
        public string anatomicalName;

        [Header("Classification")]
        public AnatomicalSystem system;
        public string systemLabel;

        [Header("Location")]
        [TextArea(1, 2)]
        public string anatomicalLocation;

        [Header("Educational Content")]
        [TextArea(2, 5)]
        public string overview;

        [TextArea(2, 5)]
        public string primaryFunctions;

        [TextArea(2, 5)]
        public string anatomyDetails;

        [TextArea(2, 5)]
        public string clinicalInformation;

        [Header("Relations")]
        [Tooltip("Names of directly related structures.")]
        public string[] relatedStructures;

        [Header("Interaction")]
        public bool isSelectable = true;
        public bool supportsIsolation = true;
        public bool supportsTransparency = true;

        [Header("Visual")]
        public Color highlightColor = new Color(0.2f, 0.9f, 0.9f, 0.8f);
        public Sprite icon;

        private static System.Collections.Generic.Dictionary<string, AnatomyMetadata> _cachedFallbacks;

        public static AnatomyMetadata GetFallback(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId)) return null;
            if (_cachedFallbacks == null)
            {
                _cachedFallbacks = new System.Collections.Generic.Dictionary<string, AnatomyMetadata>(System.StringComparer.OrdinalIgnoreCase);
                PopulateDefaultCatalog();
            }

            string cleanKey = nameOrId.ToLowerInvariant().Replace("organ.", "").Replace("vh_m_", "").Replace("(clone)", "").Trim();
            foreach (var kvp in _cachedFallbacks)
            {
                if (cleanKey.Contains(kvp.Key) || kvp.Key.Contains(cleanKey))
                    return kvp.Value;
            }

            // Generic fallback
            var generic = CreateInstance<AnatomyMetadata>();
            generic.organId = "organ." + cleanKey;
            generic.anatomicalName = char.ToUpper(cleanKey[0]) + cleanKey.Substring(1);
            generic.system = AnatomicalSystem.None;
            generic.systemLabel = "General Anatomy";
            generic.overview = $"Anatomical structure: {generic.anatomicalName}. Part of the human anatomical hierarchy.";
            generic.primaryFunctions = "Provides physiological or structural support within the human body.";
            generic.anatomyDetails = "Contains specialized tissue architecture adapted for its functional role.";
            generic.clinicalInformation = "Monitored during anatomical surveys and comprehensive medical evaluations.";
            return generic;
        }

        private static void PopulateDefaultCatalog()
        {
            Register("heart", "Heart", AnatomicalSystem.Cardiovascular, "Cardiovascular System",
                "Mediastinum of the thoracic cavity, posterior to the sternum.",
                "The heart is a four-chambered muscular organ that circulates blood through systemic and pulmonary circuits.",
                "Pumps deoxygenated blood to lungs and oxygenated blood to systemic tissues; generates systemic arterial pressure.",
                "Composed of epicardium, myocardium, and endocardium. Four chambers: right/left atria and right/left ventricles.",
                "Coronary artery disease, myocardial infarction, arrhythmias, and valvular stenosis.",
                new[] { "Aorta", "Superior Vena Cava", "Pulmonary Trunk", "Left Lung", "Right Lung" });

            Register("liver", "Liver", AnatomicalSystem.Digestive, "Digestive / Metabolic System",
                "Right upper quadrant of the abdomen, inferior to the diaphragm.",
                "The largest internal organ, responsible for essential metabolic, detoxification, and secretory functions.",
                "Produces bile for lipid digestion, metabolizes carbohydrates and amino acids, synthesizes albumin and clotting factors.",
                "Divided into anatomical right, left, caudate, and quadrate lobes; microarchitecture defined by hepatic lobules.",
                "Hepatitis, hepatic cirrhosis, hepatocellular carcinoma, and non-alcoholic fatty liver disease (NAFLD).",
                new[] { "Gallbladder", "Inferior Vena Cava", "Stomach", "Duodenum", "Diaphragm" });

            Register("lung", "Lungs", AnatomicalSystem.Respiratory, "Respiratory System",
                "Thoracic cavity on either side of the mediastinum.",
                "Paired respiratory organs responsible for continuous hematosis (gas exchange).",
                "Enables passive and active diffusion of O2 into pulmonary capillaries and elimination of CO2.",
                "Right lung has three lobes (superior, middle, inferior); left lung has two lobes and a cardiac notch.",
                "Pneumonia, chronic obstructive pulmonary disease (COPD), asthma, and pulmonary thromboembolism.",
                new[] { "Trachea", "Main Bronchi", "Heart", "Diaphragm", "Pulmonary Arteries" });

            Register("stomach", "Stomach", AnatomicalSystem.Digestive, "Digestive System",
                "Left hypochondriac and epigastric regions of the abdomen.",
                "J-shaped muscular dilatation of the alimentary canal between esophagus and duodenum.",
                "Mechanical churning and enzymatic chemical digestion via gastric acid and pepsinogen.",
                "Comprises cardia, fundus, corpus (body), antrum, and pylorus; inner lining has prominent rugae.",
                "Peptic ulcer disease, gastroesophageal reflux disease (GERD), gastritis, and adenocarcinoma.",
                new[] { "Esophagus", "Duodenum", "Pancreas", "Spleen", "Liver" });

            Register("kidney", "Kidney", AnatomicalSystem.Urinary, "Urinary System",
                "Retroperitoneal abdominal wall at T12–L3 vertebral levels.",
                "Paired bean-shaped organs that filter blood plasma to produce urine and regulate homeostasis.",
                "Excretion of metabolic wastes (urea, creatinine), regulation of extracellular fluid volume, acid-base balance.",
                "Composed of outer renal cortex, renal medulla with renal pyramids, renal pelvis, and nephrons.",
                "Chronic kidney disease (CKD), acute renal failure, nephrolithiasis (kidney stones), and renal cell carcinoma.",
                new[] { "Ureter", "Renal Artery", "Renal Vein", "Adrenal Gland", "Aorta" });

            Register("teeth", "Dentition & Teeth", AnatomicalSystem.Digestive, "Digestive / Stomatognathic",
                "Oral cavity embedded in the alveolar processes of maxilla and mandible.",
                "Calcified structures anchored in alveolar sockets dedicated to mastication, phonetics, and facial aesthetics.",
                "Mechanical mastication (incising, tearing, grinding food bolus); speech articulation; occlusal stability.",
                "Composed of anatomical crown, neck, and root; layers include enamel, dentin, cementum, and dental pulp.",
                "Dental caries, pulpitis, chronic periodontitis, impaction, and dental trauma.",
                new[] { "Mandible", "Maxilla", "Gingiva", "Periodontal Ligament", "Dental Nerve" });

            Register("vessel", "Blood Vessels & Circulation", AnatomicalSystem.Cardiovascular, "Cardiovascular System",
                "Extensive closed vascular network traversing the entire body.",
                "Dynamic conduits delivering nutrient-rich oxygenated blood and returning deoxygenated blood.",
                "High-pressure conduit distribution, microvascular exchange, capacitance storage, and systemic peripheral resistance regulation.",
                "Three tunics: tunica intima (endothelium), tunica media (smooth muscle/elastin), and tunica adventitia.",
                "Atherosclerosis, systemic hypertension, aortic aneurysm, thrombosis, and deep vein thrombosis.",
                new[] { "Heart", "Aorta", "Vena Cava", "Capillary Bed" });
        }

        private static void Register(string key, string name, AnatomicalSystem system, string systemLabel,
            string location, string overview, string functions, string anatomy, string clinical, string[] related)
        {
            var meta = CreateInstance<AnatomyMetadata>();
            meta.organId = "organ." + key;
            meta.anatomicalName = name;
            meta.system = system;
            meta.systemLabel = systemLabel;
            meta.anatomicalLocation = location;
            meta.overview = overview;
            meta.primaryFunctions = functions;
            meta.anatomyDetails = anatomy;
            meta.clinicalInformation = clinical;
            meta.relatedStructures = related;
            meta.highlightColor = new Color(0.12f, 0.88f, 0.92f, 0.85f);
            _cachedFallbacks[key] = meta;
        }
    }

    public enum AnatomicalSystem
    {
        None,
        Skeletal,
        Muscular,
        Cardiovascular,
        Respiratory,
        Digestive,
        Urinary,
        Nervous,
        Endocrine,
        Lymphatic,
        Reproductive,
        Integumentary
    }
}
