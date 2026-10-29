using UnityEngine;
using MedicalSimulation.Core;

namespace MedicalSimulation.Data
{
    /// <summary>
    /// Editor utility that creates the default AnatomyMetadata ScriptableObject
    /// assets for the major organs. Run from the GameObject context menu in the Editor.
    /// 
    /// Note: ScriptableObject assets must be created in the Unity Editor via
    ///   Assets → Create → MedicalSimulation → Anatomy Metadata,
    /// or via the context menu below.
    /// 
    /// This class generates them programmatically so the project
    /// has useful defaults immediately.
    /// </summary>
    public class AnatomyMetadataFactory : MonoBehaviour
    {
#if UNITY_EDITOR
        [ContextMenu("Create Default Anatomy Metadata Assets")]
        public void CreateDefaultAssets()
        {
            CreateOrgan("heart",          "Heart",            AnatomicalSystem.Cardiovascular,
                "Cardiovascular System",
                "Located in the mediastinum of the thoracic cavity, slightly left of the midline.",
                "The heart is a muscular organ roughly the size of a fist. It functions as the pump of the cardiovascular system, maintaining blood pressure and circulation throughout the body.",
                "Pumps oxygenated blood to the body via the systemic circulation and deoxygenated blood to the lungs via the pulmonary circulation. Beats approximately 60–100 times per minute at rest.",
                "The heart has four chambers: right atrium, right ventricle, left atrium, and left ventricle. It is surrounded by the pericardial sac and supplied by the coronary arteries.",
                "Coronary artery disease (CAD), myocardial infarction (heart attack), and heart failure are among the most common and serious medical conditions affecting the heart.",
                new[] { "Aorta", "Superior Vena Cava", "Pulmonary Arteries", "Pulmonary Veins" });

            CreateOrgan("left_lung",      "Left Lung",        AnatomicalSystem.Respiratory,
                "Respiratory System",
                "Left hemithorax, separated from the right lung by the mediastinum.",
                "The left lung is slightly smaller than the right to accommodate the heart's position. It is divided into two lobes: the superior and inferior lobes, separated by the oblique fissure.",
                "Gas exchange: delivers oxygen to the blood and removes carbon dioxide. Works in coordination with the right lung and diaphragm during respiration.",
                "The left lung has two lobes, while the right has three. It contains the lingula, a tongue-shaped projection of the superior lobe.",
                "Pneumonia, lung cancer, pneumothorax, and pulmonary embolism are key conditions affecting the lungs.",
                new[] { "Right Lung", "Diaphragm", "Trachea", "Heart" });

            CreateOrgan("right_lung",     "Right Lung",       AnatomicalSystem.Respiratory,
                "Respiratory System",
                "Right hemithorax.",
                "The right lung is slightly larger than the left and has three lobes: superior, middle, and inferior.",
                "Gas exchange. Larger lung volume than the left lung.",
                "Three lobes divided by the horizontal and oblique fissures. Supplied by the right main bronchus.",
                "Same primary conditions as the left lung: pneumonia, cancer, pneumothorax.",
                new[] { "Left Lung", "Diaphragm", "Trachea", "Liver" });

            CreateOrgan("liver",          "Liver",            AnatomicalSystem.Digestive,
                "Digestive System",
                "Right upper quadrant of the abdomen, beneath the right diaphragm.",
                "The liver is the largest solid organ in the body. It plays a central role in metabolism, detoxification, and digestion.",
                "Produces bile, metabolises carbohydrates, fats and proteins, detoxifies blood, stores glycogen and vitamins, produces clotting factors.",
                "Divided into right and left lobes. Receives a dual blood supply: the portal vein (nutrient-rich) and the hepatic artery (oxygenated).",
                "Cirrhosis, hepatitis, non-alcoholic fatty liver disease (NAFLD), and liver cancer are major hepatic conditions.",
                new[] { "Gallbladder", "Stomach", "Pancreas", "Right Kidney" });

            CreateOrgan("stomach",        "Stomach",          AnatomicalSystem.Digestive,
                "Digestive System",
                "Left upper quadrant of the abdomen, between the oesophagus and small intestine.",
                "A muscular, hollow organ that receives food from the oesophagus and begins the process of digestion.",
                "Stores food, mixes it with digestive acids and enzymes, and gradually releases chyme into the small intestine.",
                "Has four regions: cardia, fundus, body, and pylorus. The gastric mucosa secretes hydrochloric acid and pepsinogen.",
                "Peptic ulcers, gastritis, gastric cancer, and GERD are common stomach conditions.",
                new[] { "Oesophagus", "Small Intestine", "Liver", "Pancreas" });

            CreateOrgan("left_kidney",    "Left Kidney",      AnatomicalSystem.Urinary,
                "Urinary System",
                "Retroperitoneal, left posterior abdominal wall, at the T12–L3 vertebral level.",
                "Bean-shaped organ about 10–12 cm long. Filters blood to produce urine.",
                "Filters ~180 L of blood per day. Regulates fluid balance, blood pressure, and electrolytes. Produces erythropoietin and activates vitamin D.",
                "Has an outer cortex and inner medulla. Contains approximately 1 million nephrons each.",
                "Kidney stones, chronic kidney disease (CKD), and renal cancer are key conditions.",
                new[] { "Right Kidney", "Ureter", "Adrenal Gland", "Aorta" });

            CreateOrgan("right_kidney",   "Right Kidney",     AnatomicalSystem.Urinary,
                "Urinary System",
                "Retroperitoneal, right posterior abdominal wall, slightly lower than the left kidney due to the liver.",
                "Bean-shaped organ about 10–12 cm long. Filters blood to produce urine.",
                "Same functions as the left kidney.",
                "Slightly lower position than the left kidney. Connected to the inferior vena cava via the right renal vein.",
                "Kidney stones, CKD, renal cell carcinoma.",
                new[] { "Left Kidney", "Ureter", "Liver", "Inferior Vena Cava" });

            CreateOrgan("gallbladder",    "Gallbladder",      AnatomicalSystem.Digestive,
                "Digestive System",
                "Inferior surface of the right hepatic lobe.",
                "A small pear-shaped organ that stores bile produced by the liver.",
                "Concentrates and stores bile, releasing it into the duodenum in response to fat intake.",
                "Connects to the common bile duct via the cystic duct.",
                "Gallstones (cholelithiasis), cholecystitis, and gallbladder cancer.",
                new[] { "Liver", "Pancreas", "Duodenum" });

            CreateOrgan("pancreas",       "Pancreas",         AnatomicalSystem.Digestive,
                "Digestive System",
                "Retroperitoneal, behind the stomach.",
                "Elongated gland with both endocrine and exocrine functions.",
                "Exocrine: secretes digestive enzymes into the duodenum. Endocrine: produces insulin and glucagon to regulate blood sugar.",
                "Has head, neck, body, and tail regions. The tail is adjacent to the spleen.",
                "Pancreatitis, pancreatic cancer, and diabetes mellitus.",
                new[] { "Stomach", "Liver", "Duodenum", "Spleen" });

            CreateOrgan("spleen",         "Spleen",           AnatomicalSystem.Lymphatic,
                "Lymphatic System",
                "Left upper quadrant of the abdomen, between the diaphragm and stomach.",
                "The largest lymphatic organ in the body.",
                "Filters blood, recycles iron from aged red blood cells, produces lymphocytes, and stores platelets.",
                "Has a red pulp (blood filtration) and white pulp (immune function).",
                "Splenomegaly, splenic rupture, and hypersplenism.",
                new[] { "Stomach", "Pancreas", "Left Kidney" });

            CreateOrgan("small_intestine","Small Intestine",  AnatomicalSystem.Digestive,
                "Digestive System",
                "Central abdomen, connecting stomach to large intestine.",
                "The longest segment of the digestive tract, approximately 6–7 metres.",
                "Primary site of nutrient absorption. Digests carbohydrates, proteins, and fats.",
                "Divided into duodenum, jejunum, and ileum. The mucosa has villi and microvilli to maximise surface area.",
                "Crohn's disease, coeliac disease, and small bowel obstruction.",
                new[] { "Stomach", "Large Intestine", "Pancreas", "Liver" });

            CreateOrgan("large_intestine","Large Intestine",  AnatomicalSystem.Digestive,
                "Digestive System",
                "Peripheral abdomen, framing the small intestine.",
                "Approximately 1.5 metres long. Frames the small intestine.",
                "Absorbs water and electrolytes. Forms and stores faeces. Hosts colonic microbiota.",
                "Divided into caecum, ascending, transverse, descending, and sigmoid colon, then rectum.",
                "Colorectal cancer, diverticulitis, inflammatory bowel disease (IBD), and constipation.",
                new[] { "Small Intestine", "Rectum", "Appendix" });

            Debug.Log("[AnatomyMetadataFactory] Created default organ metadata assets in Assets/MedicalSimulation/Data/");
        }

        private void CreateOrgan(string id, string name, AnatomicalSystem system,
            string systemLabel, string location,
            string overview, string functions, string anatomy, string clinical,
            string[] related)
        {
            var asset          = ScriptableObject.CreateInstance<AnatomyMetadata>();
            asset.organId      = $"organ.{id}";
            asset.anatomicalName = name;
            asset.system       = system;
            asset.systemLabel  = systemLabel;
            asset.anatomicalLocation = location;
            asset.overview     = overview;
            asset.primaryFunctions   = functions;
            asset.anatomyDetails     = anatomy;
            asset.clinicalInformation = clinical;
            asset.relatedStructures  = related;
            asset.highlightColor = new Color(0.1f, 0.85f, 0.85f, 0.85f);

            string dir  = "Assets/MedicalSimulation/Data/Organs";
            if (!UnityEditor.AssetDatabase.IsValidFolder(dir))
                UnityEditor.AssetDatabase.CreateFolder("Assets/MedicalSimulation/Data", "Organs");

            string path = $"{dir}/AnatomyData_{id}.asset";
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
        }
#endif
    }
}
