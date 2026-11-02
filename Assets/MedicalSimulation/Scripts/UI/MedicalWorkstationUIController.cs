using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;
using MedicalSimulation.AI;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Master Clinical Medical Workstation UI Controller for ProcedureLab.
    /// Delivers an ultra-clean, minimal, sophisticated medical interface exactly matching
    /// the clinical reference design (Reference Image 1: Anatomy Explorer).
    /// Eliminates all cluttered dashboards, bulky solid sidebars, and overlapping duplicate panels.
    /// The 3D anatomical model occupies 75-80% of the screen.
    /// </summary>
    [ExecuteAlways]
    public class MedicalWorkstationUIController : MonoBehaviour
    {
        public static MedicalWorkstationUIController Instance { get; private set; }

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GraphicRaycaster _raycaster;
        private Camera _cam;

        // UI Panels
        private GameObject _topNavbarGo;
        private GameObject _storyRibbonGo;
        private TextMeshProUGUI _storyBadgeTmp;
        private GameObject _leftAnatomyPanelGo;
        private TextMeshProUGUI _leftPanelTitleTmp;
        private Transform _leftPanelContentRoot;
        private GameObject _rightStructureCardGo;
        private GameObject _viewCubeGo;
        private GameObject _footerBarGo;
        private GameObject _calloutPinsRoot;
        private string _activeSelectedPart = "";
        private Dictionary<MedicalComponentType, (Image bg, TextMeshProUGUI txt)> _navbarTabs = new Dictionary<MedicalComponentType, (Image, TextMeshProUGUI)>();
        private List<(string partKey, Image bg, Image dot, TextMeshProUGUI txt)> _leftPanelItems = new List<(string, Image, Image, TextMeshProUGUI)>();

        // Right Info Card TMP references
        private TextMeshProUGUI _structureTitleTmp;
        private TextMeshProUGUI _structureDescTmp;
        private TextMeshProUGUI _structureVascularTmp;
        private TextMeshProUGUI _structureClinicalTmp;
        private Button _actionSceneBtn;
        private TextMeshProUGUI _actionSceneBtnTmp;
        private string _currentTargetScene = "04_AnatomyLab";

        // Callout pin visibility toggle
        private bool _pinsVisible = true;
        private TextMeshProUGUI _pinsToggleBtnTmp;
        private TextMeshProUGUI _audioToggleBtnTmp;

        // Search field
        private TMP_InputField _searchField;

        // Model roots for toggles
        private Transform _modelRoot;
        private GameObject _skeletalGo;
        private GameObject _muscularGo;
        private GameObject _nervousGo;
        private GameObject _cardioGo;
        private GameObject _respiratoryGo;
        private GameObject _digestiveGo;
        private GameObject _integumentaryGo;

        // Active component tracking
        private MedicalComponentType _activeType = MedicalComponentType.FullAnatomy;

        // Pin definitions
        [System.Serializable]
        public class PinData
        {
            public string label;
            public Vector3 worldOffset;
            public bool isLeftAligned;
            public string structureName;
            public string description;
            public string vascular;
            public string clinical;
            public MedicalComponentType relatedComponent;
            [System.NonSerialized] public GameObject pinGo;
            [System.NonSerialized] public RectTransform lineRt;
            [System.NonSerialized] public RectTransform labelRt;
            [System.NonSerialized] public Image dotImg;
            [System.NonSerialized] public TextMeshProUGUI labelTmp;
        }

        private List<PinData> _pins = new List<PinData>();

        [System.Serializable]
        public class MedicalPartData
        {
            public string partKey;
            public string displayName;
            public string category;
            public string description;
            public string vascular;
            public string clinical;
            public string audioClip;
            public Vector3 cameraOffset;
            public Vector3 cameraLookTarget;
        }

        public List<MedicalPartData> GetPartsForComponent(MedicalComponentType type)
        {
            var list = new List<MedicalPartData>();
            switch (type)
            {
                case MedicalComponentType.FullAnatomy:
                    list.Add(new MedicalPartData {
                        partKey = "Skeletal",
                        displayName = "Axial & Appendicular Skeleton",
                        category = "Skeletal System",
                        description = "Rigid mineralized framework comprising 206 cortical and trabecular bones supporting weight, protecting organs, and anchoring musculature.",
                        vascular = "Nutrient arteries entering through nutrient foramina -> Haversian osteon canals.",
                        clinical = "Cortical bone density normal. Intact spinal column and protective thoracic cage.",
                        audioClip = "ui_confirm",
                        cameraOffset = new Vector3(0.12f, 0.4f, -2.2f),
                        cameraLookTarget = new Vector3(0.12f, 0.35f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Muscular",
                        displayName = "Skeletal Musculature",
                        category = "Muscular System",
                        description = "Dynamic skeletal muscles with directional actin/myosin striation wave maps terminating in glistening ivory aponeurotic tendons.",
                        vascular = "Muscular branches of systemic arterial circulation.",
                        clinical = "Bilateral 5/5 motor power. Symmetrical tone and intact neuromuscular junctions.",
                        audioClip = "ui_confirm",
                        cameraOffset = new Vector3(0.12f, 0.4f, -2.0f),
                        cameraLookTarget = new Vector3(0.12f, 0.35f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Cardiovascular",
                        displayName = "Cardiovascular System",
                        category = "Circulatory System",
                        description = "Central myocardium driving systemic perfusion via ascending aorta and deoxygenated pulmonary return via vena cava.",
                        vascular = "Ascending aorta -> Systemic arterial tree -> Capillary beds -> Systemic veins.",
                        clinical = "Cardiac output 5.0 L/min. Normal mean arterial pressure 93 mmHg.",
                        audioClip = "heartbeat",
                        cameraOffset = new Vector3(0.12f, 0.28f, -1.8f),
                        cameraLookTarget = new Vector3(0.12f, 0.25f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Respiratory",
                        displayName = "Respiratory Tract & Lungs",
                        category = "Respiratory System",
                        description = "Cartilaginous trachea and bilateral lobed pulmonary parenchyma mediating rapid alveolar O2-CO2 gas exchange.",
                        vascular = "Pulmonary arteries (deoxygenated) & bronchial arteries (nutritive systemic).",
                        clinical = "SpO2 99% on room air. Clear vesicular breath sounds bilaterally.",
                        audioClip = "ui_select",
                        cameraOffset = new Vector3(0.12f, 0.35f, -1.8f),
                        cameraLookTarget = new Vector3(0.12f, 0.30f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Digestive",
                        displayName = "Gastrointestinal Tract",
                        category = "Digestive System",
                        description = "Digestive loop encompassing gastric reservoir, small intestinal peristaltic coils, haustral colon, and metabolic liver parenchyma.",
                        vascular = "Celiac trunk, Superior & Inferior Mesenteric Arteries -> Portal Vein.",
                        clinical = "Normoactive bowel sounds. Intact peritoneal barrier with no free air.",
                        audioClip = "ui_select",
                        cameraOffset = new Vector3(0.12f, 0.15f, -1.8f),
                        cameraLookTarget = new Vector3(0.12f, 0.12f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Renal",
                        displayName = "Bilateral Renal System",
                        category = "Urinary System",
                        description = "Bilateral retroperitoneal kidneys containing 1.2 million functional nephrons executing glomerular ultrafiltration and acid-base balance.",
                        vascular = "Renal arteries directly branching from abdominal aorta -> Renal veins into IVC.",
                        clinical = "Estimated GFR: 120 mL/min/1.73m2. Serum creatinine 0.9 mg/dL. Symmetric perfusion.",
                        audioClip = "ui_select",
                        cameraOffset = new Vector3(0.12f, 0.08f, -1.7f),
                        cameraLookTarget = new Vector3(0.12f, 0.05f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Integumentary",
                        displayName = "Frosted Cutaway Silhouette",
                        category = "Integumentary System",
                        description = "Semi-transparent frosted acrylic body boundary (IOR 1.45, Alpha 0.12) maintaining full 3D spatial depth of internal organs.",
                        vascular = "Subdermal capillary plexus and cutaneous thermoregulatory shunts.",
                        clinical = "Intact skin boundary with Fresnel edge highlighting.",
                        audioClip = "ui_select",
                        cameraOffset = new Vector3(0.12f, 0.4f, -2.2f),
                        cameraLookTarget = new Vector3(0.12f, 0.35f, 0f)
                    });
                    break;

                case MedicalComponentType.Cardiology:
                    list.Add(new MedicalPartData {
                        partKey = "Left_Ventricle",
                        displayName = "Left Ventricle",
                        category = "Systemic High-Pressure Pump",
                        description = "Thick muscular myocardial chamber generating systolic ejection pressure across the aortic valve into systemic circulation.",
                        vascular = "Left Anterior Descending (LAD) and Left Circumflex (LCx) coronary arteries.",
                        clinical = "Ejection Fraction 62%, stroke volume 70 mL, peak systolic pressure 120 mmHg.",
                        audioClip = "heartbeat",
                        cameraOffset = new Vector3(0.04f, 0.12f, -1.5f),
                        cameraLookTarget = new Vector3(0f, 0.1f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Right_Ventricle",
                        displayName = "Right Ventricle",
                        category = "Pulmonary Low-Pressure Pump",
                        description = "Anterior crescent-shaped chamber pumping deoxygenated blood across the pulmonary valve into the pulmonary trunk.",
                        vascular = "Right Coronary Artery (RCA) marginal and acute branches.",
                        clinical = "Pulmonary systolic pressure 22 mmHg. Normal right ventricular free wall motion.",
                        audioClip = "heartbeat",
                        cameraOffset = new Vector3(-0.05f, 0.12f, -1.5f),
                        cameraLookTarget = new Vector3(0f, 0.1f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Ascending_Aorta",
                        displayName = "Ascending Aorta",
                        category = "Systemic Outflow Trunk",
                        description = "Large elastic artery emerging from aortic vestibule, providing systolic compliance and coronary ostial perfusion.",
                        vascular = "Direct left ventricular outflow tract; gives rise to right and left coronary ostia.",
                        clinical = "Root diameter 3.1 cm. Normal laminar flow velocity 1.2 m/s.",
                        audioClip = "heartbeat",
                        cameraOffset = new Vector3(0f, 0.28f, -1.6f),
                        cameraLookTarget = new Vector3(0f, 0.18f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Coronary_LAD",
                        displayName = "LAD Coronary Artery",
                        category = "Anterior Myocardial Perfusion",
                        description = "Left Anterior Descending artery traveling in anterior interventricular sulcus supplying apex and septum.",
                        vascular = "Originates from Left Main Coronary Artery -> Diagonal & Septal perforator branches.",
                        clinical = "Primary target in STEMI revascularization. Serous clearcoat high-gloss arterial crimson.",
                        audioClip = "heartbeat",
                        cameraOffset = new Vector3(0.05f, 0.15f, -1.4f),
                        cameraLookTarget = new Vector3(0f, 0.1f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Coronary_RCA",
                        displayName = "RCA Coronary Artery",
                        category = "Right & Inferior Wall Perfusion",
                        description = "Right Coronary Artery coursing in right atrioventricular groove to supply SA and AV conduction nodes.",
                        vascular = "Right aortic sinus -> Acute marginal branches -> Posterior Descending Artery (PDA).",
                        clinical = "Perfuses the primary cardiac pacemaker; patent lumen with normal rhythm.",
                        audioClip = "heartbeat",
                        cameraOffset = new Vector3(-0.06f, 0.15f, -1.4f),
                        cameraLookTarget = new Vector3(0f, 0.1f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Adipose_Fat_Pads",
                        displayName = "Epicardial Adipose Sulci",
                        category = "Protective Cardiac Cushion",
                        description = "Creamy pale-yellow lobulated adipose tissue deposits along anterior and posterior interventricular sulci.",
                        vascular = "Adventitial vasa vasorum arising from adjacent epicardial coronary vessels.",
                        clinical = "Protects coronary arteries from shear stress during continuous systolic contraction.",
                        audioClip = "ui_confirm",
                        cameraOffset = new Vector3(0.04f, 0.10f, -1.4f),
                        cameraLookTarget = new Vector3(0f, 0.1f, 0f)
                    });
                    break;

                case MedicalComponentType.Dental:
                    list.Add(new MedicalPartData {
                        partKey = "Mandible_Maxilla",
                        displayName = "Mandible & Maxilla Bone",
                        category = "Maxillofacial Alveolar Framework",
                        description = "Dense cortical bone and trabecular alveolar sockets anchoring 32 permanent adult teeth.",
                        vascular = "Inferior alveolar artery and posterior superior alveolar arterial branches.",
                        clinical = "Type II cortical bone density. Alveolar ridge intact with zero osteolytic defects.",
                        audioClip = "dental_scaler",
                        cameraOffset = new Vector3(0f, 0.05f, -0.65f),
                        cameraLookTarget = new Vector3(0f, 0f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Gums_Gingiva",
                        displayName = "Attached & Marginal Gingiva",
                        category = "Periodontal Mucosa",
                        description = "Vascular pink keratinized tissue featuring characteristic Voronoi 'orange peel' stippling.",
                        vascular = "Supra-periosteal gingival capillary plexus with rapid microvascular response.",
                        clinical = "Healthy gingival margins: Probing depth < 2.5 mm, no bleeding on probing.",
                        audioClip = "dental_suction",
                        cameraOffset = new Vector3(0f, 0.05f, -0.55f),
                        cameraLookTarget = new Vector3(0f, 0f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Incisors",
                        displayName = "Incisors (#8, #9, #24, #25)",
                        category = "Anterior Cutting Dentition",
                        description = "Single-rooted anterior teeth exhibiting natural incisal edge enamel translucency and amber dentin core.",
                        vascular = "Anterior superior and incisive alveolar arteries; nasopalatine innervation.",
                        clinical = "Intact incisal guidance with zero mobility.",
                        audioClip = "dental_scaler",
                        cameraOffset = new Vector3(0f, 0.04f, -0.50f),
                        cameraLookTarget = new Vector3(0f, 0f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Canines",
                        displayName = "Canines (#6, #11, #22, #27)",
                        category = "Cornerstone Tearing Dentition",
                        description = "Robust conical teeth with the longest roots in the human dentition providing canine protected occlusion.",
                        vascular = "Anterior superior alveolar artery and middle mental nerve branches.",
                        clinical = "Cornerstone of arch integrity; excellent root bone anchorage.",
                        audioClip = "dental_scaler",
                        cameraOffset = new Vector3(0.04f, 0.03f, -0.48f),
                        cameraLookTarget = new Vector3(0f, 0f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Molars",
                        displayName = "Molars (Extraction Target #32)",
                        category = "Posterior Masticatory Units",
                        description = "Multi-rooted posterior grinding teeth. Includes impacted mandibular right third molar (#32).",
                        vascular = "Inferior alveolar neurovascular bundle in proximity to molar root apex.",
                        clinical = "Indication: Atraumatic surgical extraction and socket debridement.",
                        audioClip = "dental_drill",
                        cameraOffset = new Vector3(0.04f, 0.02f, -0.45f),
                        cameraLookTarget = new Vector3(0.02f, 0f, 0f)
                    });
                    break;

                case MedicalComponentType.SoftTissue:
                    list.Add(new MedicalPartData {
                        partKey = "Liver_Right",
                        displayName = "Liver Right Lobe",
                        category = "Hepatic Segments V-VIII",
                        description = "Massive hepatic division enclosed in smooth Glisson's capsule executing glycogen storage and plasma protein synthesis.",
                        vascular = "Right hepatic artery proper and right branch of portal vein.",
                        clinical = "Normal parenchyma; zero subcapsular hematoma or laceration.",
                        audioClip = "lab_ambience",
                        cameraOffset = new Vector3(0.06f, 0.1f, -1.7f),
                        cameraLookTarget = new Vector3(0f, 0.05f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Liver_Left",
                        displayName = "Liver Left Lobe",
                        category = "Hepatic Segments II-IV",
                        description = "Flatter hepatic lobe extending into epigastrium across midline underneath left diaphragmatic dome.",
                        vascular = "Left hepatic artery and umbilical portion of left portal vein.",
                        clinical = "Crisp anterior margins; normal transaminase activity.",
                        audioClip = "lab_ambience",
                        cameraOffset = new Vector3(-0.06f, 0.1f, -1.7f),
                        cameraLookTarget = new Vector3(0f, 0.05f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Gallbladder",
                        displayName = "Gallbladder & Bile Duct",
                        category = "Biliary Reservoir & Drainage",
                        description = "Turgid emerald-olive green organ with translucent Subsurface Scattering storing 50 mL of concentrated bile.",
                        vascular = "Cystic artery directly branching from right hepatic artery.",
                        clinical = "No cholelithiasis (gallstones), thin mucosal wall < 3 mm, patent common bile duct.",
                        audioClip = "ui_select",
                        cameraOffset = new Vector3(0.03f, 0.02f, -1.4f),
                        cameraLookTarget = new Vector3(0.02f, 0.02f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Portal_Triad",
                        displayName = "Hepatic Portal Triad",
                        category = "Critical Splanchnic Pedicle",
                        description = "Hepatoduodenal ligament structure uniting Common Bile Duct, Hepatic Artery, and Portal Vein.",
                        vascular = "Portal Vein (75% flow, 50% O2) & Hepatic Artery (25% flow, 50% O2).",
                        clinical = "Pringle maneuver clamping landmark for hemorrhage control in trauma.",
                        audioClip = "ui_confirm",
                        cameraOffset = new Vector3(0.06f, 0.04f, -1.4f),
                        cameraLookTarget = new Vector3(0.03f, 0.02f, 0f)
                    });
                    break;

                case MedicalComponentType.Procedure:
                    list.Add(new MedicalPartData {
                        partKey = "Mayo_Tray",
                        displayName = "Mayo Instrument Stand",
                        category = "Sterile Surgical Base",
                        description = "Surgical staging platform forged from autoclaved AISI 316L medical stainless steel with raised anti-spill lip.",
                        vascular = "Non-biological sterile platform.",
                        clinical = "Sterilized at 134°C; confirmed zero microbial bioburden.",
                        audioClip = "surgical_instrument_clink",
                        cameraOffset = new Vector3(0f, 0.45f, -0.65f),
                        cameraLookTarget = new Vector3(0f, 0.2f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Scalpel_10",
                        displayName = "Scalpel No. 10",
                        category = "Primary Incision Instrument",
                        description = "Large curved belly blade with mirror-polished cutting bevel (Roughness 0.05) on an ergonomic graduated handle.",
                        vascular = "Medical-grade hardened surgical steel blade.",
                        clinical = "Sharpness rating 10/10 for smooth dermotomy with minimal cell necrosis.",
                        audioClip = "surgical_instrument_clink",
                        cameraOffset = new Vector3(-0.12f, 0.35f, -0.55f),
                        cameraLookTarget = new Vector3(-0.08f, 0.2f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "DeBakey_Forceps",
                        displayName = "DeBakey Tissue Forceps",
                        category = "Atraumatic Grasping",
                        description = "Precision forceps featuring non-slip ribbed thumb pads and fine atraumatic longitudinal micro-serrations.",
                        vascular = "AISI 316L stainless steel with spring tension action.",
                        clinical = "Designed specifically to handle vascular and intestinal tissue without crush trauma.",
                        audioClip = "surgical_instrument_clink",
                        cameraOffset = new Vector3(-0.05f, 0.35f, -0.55f),
                        cameraLookTarget = new Vector3(-0.04f, 0.2f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Metzenbaum_Scissors",
                        displayName = "Metzenbaum Dissecting Scissors",
                        category = "Blunt & Sharp Dissection",
                        description = "Curved surgical scissors with tungsten carbide cutting edges and dual finger rings for dissection.",
                        vascular = "Precision box-lock hinge pivot.",
                        clinical = "Effortless shearing through deep visceral fascia and peritoneal planes.",
                        audioClip = "surgical_instrument_clink",
                        cameraOffset = new Vector3(0.02f, 0.35f, -0.55f),
                        cameraLookTarget = new Vector3(0.01f, 0.2f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Kelly_Clamp",
                        displayName = "Hemostat Kelly Clamp",
                        category = "Vascular Hemostasis Lock",
                        description = "Curved hemostatic forceps with transverse jaw serrations and a multi-position ratchet locking handle.",
                        vascular = "Ratchet tension calibrated for vessel occlusion.",
                        clinical = "Essential for securing bleeders and temporary vessel occlusion.",
                        audioClip = "surgical_instrument_clink",
                        cameraOffset = new Vector3(0.08f, 0.35f, -0.55f),
                        cameraLookTarget = new Vector3(0.05f, 0.2f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Electrocautery",
                        displayName = "Electrosurgical Pencil",
                        category = "RF Cut & Coagulation",
                        description = "Active electrosurgical handpiece with yellow (Cut) and blue (Coag) pushbuttons and an active blade tip.",
                        vascular = "Isolated generator RF circuit connection.",
                        clinical = "Delivers instantaneous hemostasis and rapid tissue division with minimal lateral heat.",
                        audioClip = "surgical_instrument_clink",
                        cameraOffset = new Vector3(0.15f, 0.35f, -0.55f),
                        cameraLookTarget = new Vector3(0.10f, 0.2f, 0f)
                    });
                    break;

                case MedicalComponentType.OperatingTheatre:
                    list.Add(new MedicalPartData {
                        partKey = "Table_Patient",
                        displayName = "Operating Table & Patient",
                        category = "Surgical Field",
                        description = "Motorized carbon-composite operating table with anti-static surgical mattress supporting the draped patient.",
                        vascular = "Patient supine position with bilateral armboards.",
                        clinical = "Sterile surgical perimeter secured with iodophor incise drapes.",
                        audioClip = "or_ventilation_ambience",
                        cameraOffset = new Vector3(0f, 1.2f, -2.8f),
                        cameraLookTarget = new Vector3(0f, 0.8f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Surgical_Lights",
                        displayName = "Dual 4800K LED Luminaires",
                        category = "Shadowless Surgical Lighting",
                        description = "Dual overhead reflector lamp heads producing 160,000 lux of neutral daylight (4800K) with CRI 96.",
                        vascular = "Adjustable optical focal spot matching surgical cavity depth.",
                        clinical = "Eliminates deep cavity shadows for accurate anatomical color perception.",
                        audioClip = "or_ventilation_ambience",
                        cameraOffset = new Vector3(1.0f, 2.0f, -2.8f),
                        cameraLookTarget = new Vector3(0.5f, 1.0f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Vital_Monitor",
                        displayName = "Patient Vitals Monitor",
                        category = "Real-Time Hemodynamic Telemetry",
                        description = "High-resolution bedside display streaming 5-lead ECG rhythm, SpO2 plethysmograph, and invasive blood pressure.",
                        vascular = "Radial arterial line and central venous pressure transducers.",
                        clinical = "HR: 72 BPM | SpO2: 99% | BP: 120/80 mmHg | Patient Hemodynamically Stable.",
                        audioClip = "monitor_ecg_beep",
                        cameraOffset = new Vector3(1.6f, 1.5f, -2.2f),
                        cameraLookTarget = new Vector3(1.0f, 1.2f, 0f)
                    });
                    list.Add(new MedicalPartData {
                        partKey = "Anesthesia_Workstation",
                        displayName = "Anesthesia Ventilator",
                        category = "Airway & Volatile Anesthetic Delivery",
                        description = "Electronic circle breathing circuit ventilator with Sevoflurane vaporizer and in-line CO2 absorber.",
                        vascular = "Endotracheal tube with positive pressure ventilation.",
                        clinical = "Tidal Volume 500 mL, RR 12/min, EtCO2 36 mmHg, MAC 1.1.",
                        audioClip = "or_ventilation_ambience",
                        cameraOffset = new Vector3(2.0f, 1.4f, -2.5f),
                        cameraLookTarget = new Vector3(1.2f, 1.0f, 0f)
                    });
                    break;
            }
            return list;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrapWorkstation()
        {
            var existing = FindFirstObjectByType<MedicalWorkstationUIController>();
            if (existing == null)
            {
                var workstationGo = new GameObject("MedicalWorkstation_MasterController");
                existing = workstationGo.AddComponent<MedicalWorkstationUIController>();
            }
            existing.InitializeWorkstation();
        }

        void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            InitializeWorkstation();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            InitializeWorkstation();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeWorkstation();
        }

        [ContextMenu("Rebuild Workstation Interface")]
        public void InitializeWorkstation()
        {
            _cam = Camera.main;

            // 1. Purge all legacy clutter
            PurgeLegacyUI();

            // 2. Setup 3D Anatomy Rig & 3-Point Lighting
            Ensure3DAnatomyViewportRig();

            // 3. Setup Canvas
            SetupCanvas();

            // 4. Initialize Pin Data
            InitializePinData();

            // 5. Build Pristine Minimal Interface (Matching Reference Image 1)
            BuildTopNavbar();
            BuildStoryWorkflowRibbon();
            BuildLeftAnatomyPanel();
            BuildRightStructureCard();
            BuildViewCube();
            BuildCalloutPinsLayer();
            BuildSubtleFooter();

            // 5b. Ensure Story Campaign Controller is Active
            var story = LevelStoryController.Instance ?? FindFirstObjectByType<LevelStoryController>();
            if (story == null && Application.isPlaying)
            {
                var sGo = new GameObject("ClinicalStoryCampaign_Manager");
                story = sGo.AddComponent<LevelStoryController>();
            }

            // 6. Select Full Anatomy by default
            SelectMedicalComponent(MedicalComponentType.FullAnatomy);
        }

        private void PurgeLegacyUI()
        {
            string[] legacyNames = new string[] {
                "Button  1", "Button 1", "Button 2", "Button 3",
                "Button dental", "Button nose", "Button liver", "Button nasal",
                "Panel", "AI_Launcher_Button", "DashboardOverlay", "StoryLevelCardsScroll",
                "HomeComponentRibbon", "LeftSidebar", "ContextualInspectionPanel",
                "AnatomyExplorer_Overlay", "AnatomyExplorer_OverlayManager"
            };

            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.gameObject == gameObject) continue;
                if (canvas.name == "MedicalWorkstation_Canvas") continue;

                for (int i = 0; i < canvas.transform.childCount; i++)
                {
                    var child = canvas.transform.GetChild(i);
                    foreach (var legacy in legacyNames)
                    {
                        if (child.name.Equals(legacy, System.StringComparison.OrdinalIgnoreCase))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }

                    var textComps = child.GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (var t in textComps)
                    {
                        if (t.text.Contains("WELCOME TO MEDICAL VR") || t.text.Contains("Team 11") || t.text.Contains("Functionalities"))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void Ensure3DAnatomyViewportRig()
        {
            // Remove old procedural blobs
            var oldTorso = GameObject.Find("AnatomicalTorso_Root");
            if (oldTorso != null) oldTorso.SetActive(false);

            if (_cam == null) _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                _cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0.12f, 0.4f, -2.2f);
            }

            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = MedicalUITheme.BackgroundDeep;

            // Camera Controller with proper orbit pivot
            var camCtrl = _cam.GetComponent<AnatomyCameraController>() ?? _cam.gameObject.AddComponent<AnatomyCameraController>();

            // Setup Master 3D Component Viewer
            var viewer = FindFirstObjectByType<Medical3DComponentViewer>();
            if (viewer == null)
            {
                var viewerGo = new GameObject("3D_Medical_Component_MasterRig");
                viewer = viewerGo.AddComponent<Medical3DComponentViewer>();
            }
            viewer.EnsureComponentModels();

            EnsureStudioLighting();
        }

        private void EnsureStudioLighting()
        {
            var rigGo = GameObject.Find("Studio_Clinical_Lighting_Rig");
            if (rigGo == null)
            {
                rigGo = new GameObject("Studio_Clinical_Lighting_Rig");

                // Key Light (Clinical cool illumination)
                var keyGo = new GameObject("Key_Light_Clinical");
                keyGo.transform.SetParent(rigGo.transform, false);
                var keyLight = keyGo.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.98f, 0.99f, 1.0f);
                keyLight.intensity = 1.4f;
                keyGo.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

                // Fill Light (Soft cool ambient fill)
                var fillGo = new GameObject("Fill_Light_Soft");
                fillGo.transform.SetParent(rigGo.transform, false);
                var fillLight = fillGo.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = new Color(0.55f, 0.75f, 0.95f);
                fillLight.intensity = 0.7f;
                fillGo.transform.rotation = Quaternion.Euler(25f, 145f, 0f);

                // Rim Light (Sharp cyan edge contour highlight)
                var rimGo = new GameObject("Rim_Light_Cyan");
                rimGo.transform.SetParent(rigGo.transform, false);
                var rimLight = rimGo.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = new Color(0.0f, 0.85f, 1.0f);
                rimLight.intensity = 1.2f;
                rimGo.transform.rotation = Quaternion.Euler(160f, 10f, 0f);

                var oldLight = GameObject.Find("Directional Light");
                if (oldLight != null && oldLight != keyGo && oldLight != fillGo && oldLight != rimGo)
                {
                    var l = oldLight.GetComponent<Light>();
                    if (l != null) l.intensity = 0.2f;
                }
            }
        }

        private void SetupCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null) _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            _raycaster = GetComponent<GraphicRaycaster>();
            if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();

            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void InitializePinData()
        {
            _pins.Clear();

            _pins.Add(new PinData {
                label = "Cerebrum",
                worldOffset = new Vector3(0f, 0.65f, 0f),
                isLeftAligned = true,
                structureName = "Cerebrum (Telencephalon)",
                description = "Primary cerebral hemispheres governing motor control, somatic sensory processing, and conscious cognition.",
                vascular = "Circle of Willis (Internal Carotid & Vertebral arteries)",
                clinical = "Intact perfusion, intracranial pressure 12 mmHg.",
                relatedComponent = MedicalComponentType.Procedure
            });

            _pins.Add(new PinData {
                label = "Respiratory",
                worldOffset = new Vector3(0f, 0.40f, 0f),
                isLeftAligned = true,
                structureName = "Trachea & Bronchial Tree",
                description = "Cartilaginous airway conducting humidified tidal volume to pulmonary alveoli.",
                vascular = "Inferior thyroid and bronchial arteries",
                clinical = "Clear breath sounds, patent airway.",
                relatedComponent = MedicalComponentType.FullAnatomy
            });

            _pins.Add(new PinData {
                label = "Liver",
                worldOffset = new Vector3(0.08f, 0.15f, 0f),
                isLeftAligned = true,
                structureName = "Hepatic Lobe & Gallbladder",
                description = "Largest internal gland responsible for protein synthesis, detoxification, and bile storage in the emerald gallbladder.",
                vascular = "75% Portal Vein, 25% Hepatic Artery Proper",
                clinical = "Normal INR/PT coagulation, normal transaminases.",
                relatedComponent = MedicalComponentType.SoftTissue
            });

            _pins.Add(new PinData {
                label = "Biceps Brachii",
                worldOffset = new Vector3(0.25f, 0.22f, 0f),
                isLeftAligned = true,
                structureName = "Biceps Brachii (Arm Flexor)",
                description = "Two-headed skeletal muscle flexing the elbow and supinating the forearm.",
                vascular = "Brachial Artery muscular branches",
                clinical = "Motor power 5/5, intact musculocutaneous nerve.",
                relatedComponent = MedicalComponentType.FullAnatomy
            });

            _pins.Add(new PinData {
                label = "Heart",
                worldOffset = new Vector3(-0.03f, 0.26f, 0f),
                isLeftAligned = false,
                structureName = "Heart (Myocardium)",
                description = "Muscular organ driving pulmonary and systemic circulation at 72 BPM sinus rhythm.",
                vascular = "LAD, LCx, and RCA coronary vasculature",
                clinical = "Cardiac output 5.0 L/min, ejection fraction 62%.",
                relatedComponent = MedicalComponentType.Cardiology
            });

            _pins.Add(new PinData {
                label = "Lungs",
                worldOffset = new Vector3(-0.13f, 0.28f, 0f),
                isLeftAligned = false,
                structureName = "Pulmonary Parenchyma",
                description = "Bilateral lung lobes with alveoli for oxygenation and CO2 elimination.",
                vascular = "Pulmonary Arteries & Bronchial Circulation",
                clinical = "SpO2 99%, normal compliance, clear expansion.",
                relatedComponent = MedicalComponentType.FullAnatomy
            });

            _pins.Add(new PinData {
                label = "Kidney",
                worldOffset = new Vector3(-0.10f, 0.08f, 0f),
                isLeftAligned = false,
                structureName = "Structure: Left Kidney",
                description = "Left Kidney is proximal to retroperitoneal space and spleen. Active glomerular filtration and renin secretion.",
                vascular = "Left Renal Artery (Aorta) -> Left Renal Vein (IVC)",
                clinical = "GFR: 125 mL/min. Active cyan vascular perfusion.",
                relatedComponent = MedicalComponentType.Diagnosis
            });

            _pins.Add(new PinData {
                label = "Lymphatic",
                worldOffset = new Vector3(-0.24f, -0.05f, 0f),
                isLeftAligned = false,
                structureName = "Lymphatic Network",
                description = "Channels returning interstitial fluid and lymphocytes into systemic circulation.",
                vascular = "Thoracic duct and right lymphatic trunk",
                clinical = "No lymphadenopathy, normal lymphatic drainage.",
                relatedComponent = MedicalComponentType.FullAnatomy
            });

            _pins.Add(new PinData {
                label = "Femur",
                worldOffset = new Vector3(-0.12f, -0.42f, 0f),
                isLeftAligned = false,
                structureName = "Femur (Cortical Bone)",
                description = "Major weight-bearing bone of the lower extremity articulating with pelvis and tibia.",
                vascular = "Profunda Femoris & Circumflex Arteries",
                clinical = "Intact cortex, normal bone mineral density.",
                relatedComponent = MedicalComponentType.FullAnatomy
            });
        }

        private void FindModelRoots()
        {
            var viewer = Medical3DComponentViewer.Instance ?? FindFirstObjectByType<Medical3DComponentViewer>();
            if (viewer != null)
            {
                var fullAnatomy = viewer.transform.Find("Comp_01_FullAnatomy");
                if (fullAnatomy != null)
                {
                    _modelRoot = fullAnatomy;
                    var explorer = fullAnatomy.Find("Human_Anatomy_Explorer_Root") ?? fullAnatomy;
                    _skeletalGo = explorer.Find("System_Skeletal")?.gameObject;
                    _muscularGo = explorer.Find("System_Muscular")?.gameObject;
                    _nervousGo = explorer.Find("System_Nervous")?.gameObject;
                    _cardioGo = explorer.Find("System_Cardiovascular")?.gameObject;
                    _respiratoryGo = explorer.Find("System_Respiratory")?.gameObject;
                    _digestiveGo = explorer.Find("System_Digestive")?.gameObject;
                    _integumentaryGo = explorer.Find("System_Integumentary")?.gameObject;
                }
            }
        }

        // =========================================================================
        // TOP NAVBAR: SLEEK, COMPACT, UNCLUTTERED (Height: 46px)
        // =========================================================================
        private void BuildTopNavbar()
        {
            if (_topNavbarGo != null)
            {
                if (Application.isPlaying) Destroy(_topNavbarGo);
                else DestroyImmediate(_topNavbarGo);
            }

            _navbarTabs.Clear();

            _topNavbarGo = CreateUIRect("TopNavbar", transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                Vector2.zero, new Vector2(0, 46));
            _topNavbarGo.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.10f, 0.95f);

            var border = CreateUIRect("Border", _topNavbarGo.transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                Vector2.zero, new Vector2(0, 1));
            border.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            // Brand
            var brandGo = CreateUIRect("Brand", _topNavbarGo.transform,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(20, 0), new Vector2(170, 30));
            var brandTmp = brandGo.AddComponent<TextMeshProUGUI>();
            brandTmp.text = "<b><color=#00E5FF>ProcedureLab</color></b> <size=80%><color=#64748B>VR</color></size>";
            brandTmp.fontSize = 14;
            brandTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Module Navigation Tabs (Right next to brand)
            var tabsGo = CreateUIRect("ModuleTabs", _topNavbarGo.transform,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(190, 0), new Vector2(620, 32));
            var hLayout = tabsGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandHeight = true;

            CreateNavbarTab("Anatomy", MedicalComponentType.FullAnatomy, tabsGo.transform);
            CreateNavbarTab("Cardiology", MedicalComponentType.Cardiology, tabsGo.transform);
            CreateNavbarTab("Dental", MedicalComponentType.Dental, tabsGo.transform);
            CreateNavbarTab("Soft Tissue", MedicalComponentType.SoftTissue, tabsGo.transform);
            CreateNavbarTab("Instruments", MedicalComponentType.Procedure, tabsGo.transform);
            CreateNavbarTab("Operating Theatre", MedicalComponentType.OperatingTheatre, tabsGo.transform);

            // Center Search Box (matching Reference Image 1)
            var searchGo = CreateUIRect("SearchBar", _topNavbarGo.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(160, 0), new Vector2(220, 28));
            searchGo.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.17f, 0.9f);

            var sBorder = CreateUIRect("Border", searchGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            sBorder.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            var sTxtGo = CreateUIRect("Text", searchGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var sTmp = sTxtGo.AddComponent<TextMeshProUGUI>();
            sTmp.text = "<color=#64748B>Search anatomy...</color>";
            sTmp.fontSize = 11;
            sTmp.alignment = TextAlignmentOptions.Center;

            // Right Action Buttons
            var rightActionsGo = CreateUIRect("RightActions", _topNavbarGo.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-20, 0), new Vector2(390, 32));
            var rLayout = rightActionsGo.AddComponent<HorizontalLayoutGroup>();
            rLayout.spacing = 8;
            rLayout.childAlignment = TextAnchor.MiddleRight;
            rLayout.childControlWidth = false;
            rLayout.childForceExpandWidth = false;

            // Audio Mute/Unmute Toggle Button
            var audioBtnGo = CreateTopButton("AudioBtn", rightActionsGo.transform, new Vector2(85, 28), () => {
                if (MedicalWorkstationAudioController.Instance != null)
                {
                    MedicalWorkstationAudioController.Instance.ToggleMute();
                    bool muted = MedicalWorkstationAudioController.Instance.isMuted;
                    if (_audioToggleBtnTmp != null)
                        _audioToggleBtnTmp.text = muted ? "<color=#EF4444>Mute</color>" : "<color=#10B981>Audio ON</color>";
                }
            });
            _audioToggleBtnTmp = audioBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            _audioToggleBtnTmp.text = "<color=#10B981>Audio ON</color>";

            // Pins Toggle Button
            var pinsBtnGo = CreateTopButton("PinsBtn", rightActionsGo.transform, new Vector2(90, 28), () => {
                _pinsVisible = !_pinsVisible;
                if (_calloutPinsRoot != null) _calloutPinsRoot.SetActive(_pinsVisible);
                if (_pinsToggleBtnTmp != null)
                    _pinsToggleBtnTmp.text = _pinsVisible ? "<color=#00E5FF>Pins ON</color>" : "<color=#64748B>Pins OFF</color>";
            });
            _pinsToggleBtnTmp = pinsBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            _pinsToggleBtnTmp.text = "<color=#00E5FF>Pins ON</color>";

            // Reset Camera Button
            CreateTopButton("ResetBtn", rightActionsGo.transform, new Vector2(75, 28), () => {
                var camCtrl = _cam != null ? _cam.GetComponent<AnatomyCameraController>() : null;
                if (camCtrl != null) camCtrl.ResetView();
            }, "Reset");

            // AI Assistant Pill
            CreateTopButton("AIBtn", rightActionsGo.transform, new Vector2(110, 28), () => {
                var aiChat = MedicalAIChatUI.EnsureInstance();
                if (aiChat != null) aiChat.TogglePanel();
            }, "AI Assistant", MedicalUITheme.AccentCyan, Color.black);
        }

        private void CreateNavbarTab(string label, MedicalComponentType type, Transform parent)
        {
            var btnGo = CreateUIRect(label, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(type == MedicalComponentType.OperatingTheatre ? 124 : 82, 28));
            var img = btnGo.AddComponent<Image>();
            img.color = _activeType == type ? MedicalUITheme.SurfaceElevated : Color.clear;

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                if (MedicalWorkstationAudioController.Instance != null)
                    MedicalWorkstationAudioController.Instance.PlayUIClick();
                SelectMedicalComponent(type);
            });

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = _activeType == type ? MedicalUITheme.AccentCyan : MedicalUITheme.TextSecondary;

            _navbarTabs[type] = (img, tmp);
        }

        private void UpdateNavbarTabHighlights()
        {
            foreach (var kvp in _navbarTabs)
            {
                bool active = kvp.Key == _activeType;
                if (kvp.Value.bg != null)
                    kvp.Value.bg.color = active ? MedicalUITheme.SurfaceElevated : Color.clear;
                if (kvp.Value.txt != null)
                    kvp.Value.txt.color = active ? MedicalUITheme.AccentCyan : MedicalUITheme.TextSecondary;
            }
        }

        // =========================================================================
        // DOCKED STORY WORKFLOW RIBBON (Directly below Top Navbar at Y: -46, Height: 36px)
        // =========================================================================
        private void BuildStoryWorkflowRibbon()
        {
            if (_storyRibbonGo != null)
            {
                if (Application.isPlaying) Destroy(_storyRibbonGo);
                else DestroyImmediate(_storyRibbonGo);
            }

            _storyRibbonGo = CreateUIRect("StoryWorkflowRibbon", transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -46), new Vector2(0, 36));
            _storyRibbonGo.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.12f, 0.98f);

            var border = CreateUIRect("Border", _storyRibbonGo.transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                Vector2.zero, new Vector2(0, 1));
            border.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            // Left: Story Badge & Step Indicator
            var badgeGo = CreateUIRect("StoryBadge", _storyRibbonGo.transform,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(20, 0), new Vector2(800, 26));
            _storyBadgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
            _storyBadgeTmp.text = "<b><color=#00E5FF>ACT I • STEP 1/3</color></b> <color=#475569>|</color> <color=#F8FAFC>Splanchnic & Musculoskeletal Architecture</color> <size=85%><color=#94A3B8>(Identify major organ systems and skeletal anchors)</color></size>";
            _storyBadgeTmp.fontSize = 11;
            _storyBadgeTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Right: Story Navigation Controls
            var navGo = CreateUIRect("StoryControls", _storyRibbonGo.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-20, 0), new Vector2(260, 26));
            var hLayout = navGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childAlignment = TextAnchor.MiddleRight;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;

            // Prev Step Button
            CreateTopButton("PrevStepBtn", navGo.transform, new Vector2(95, 24), () => {
                var story = LevelStoryController.Instance ?? FindFirstObjectByType<LevelStoryController>();
                story?.PreviousStep();
            }, "◀ Prev Step", MedicalUITheme.SurfaceElevated, MedicalUITheme.TextSecondary);

            // Next Step Button (Highlighted Cyan)
            CreateTopButton("NextStepBtn", navGo.transform, new Vector2(115, 24), () => {
                var story = LevelStoryController.Instance ?? FindFirstObjectByType<LevelStoryController>();
                story?.NextStep();
            }, "Next Step ▶", MedicalUITheme.AccentCyan, Color.black);
        }

        private GameObject CreateTopButton(string name, Transform parent, Vector2 size, UnityEngine.Events.UnityAction action, string label = "", Color? bgColor = null, Color? textColor = null)
        {
            var btnGo = CreateUIRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, size);
            var img = btnGo.AddComponent<Image>();
            img.color = bgColor ?? MedicalUITheme.SurfaceElevated;

            var border = CreateUIRect("Border", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            border.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            var btn = btnGo.AddComponent<Button>();
            if (action != null)
            {
                btn.onClick.AddListener(() => {
                    if (MedicalWorkstationAudioController.Instance != null)
                        MedicalWorkstationAudioController.Instance.PlayUIClick();
                    action.Invoke();
                });
            }

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor ?? MedicalUITheme.TextPrimary;

            return btnGo;
        }

        // =========================================================================
        // LEFT DOCK: DYNAMIC ANATOMY & SUBSYSTEM EXPLORER
        // =========================================================================
        private void BuildLeftAnatomyPanel()
        {
            if (_leftAnatomyPanelGo != null)
            {
                if (Application.isPlaying) Destroy(_leftAnatomyPanelGo);
                else DestroyImmediate(_leftAnatomyPanelGo);
            }

            FindModelRoots();

            _leftAnatomyPanelGo = CreatePanel("LeftAnatomyDock", transform,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -90), new Vector2(250, 560));

            var vLayout = _leftAnatomyPanelGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(14, 14, 14, 14);
            vLayout.spacing = 6;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandHeight = false;

            // Panel Title Header
            var titleRow = CreateUIRect("Header", _leftAnatomyPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 22));
            _leftPanelTitleTmp = titleRow.AddComponent<TextMeshProUGUI>();
            _leftPanelTitleTmp.text = "<b>Anatomical Systems</b>";
            _leftPanelTitleTmp.fontSize = 13.5f;
            _leftPanelTitleTmp.color = Color.white;
            _leftPanelTitleTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var subHeader = CreateUIRect("SubHeader", _leftAnatomyPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 14));
            var subTmp = subHeader.AddComponent<TextMeshProUGUI>();
            subTmp.text = "<color=#64748B>Select sub-part to inspect</color>";
            subTmp.fontSize = 9.5f;

            // Divider
            var div = CreateUIRect("Divider", _leftAnatomyPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
            div.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            // Container for dynamic items
            var contentGo = CreateUIRect("SubsystemContent", _leftAnatomyPanelGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 480));
            var cvLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            cvLayout.spacing = 4;
            cvLayout.childControlWidth = true;
            cvLayout.childForceExpandWidth = true;
            cvLayout.childControlHeight = false;
            cvLayout.childForceExpandHeight = false;
            _leftPanelContentRoot = contentGo.transform;

            RebuildLeftSubsystemPanel(_activeType);
        }

        public void RebuildLeftSubsystemPanel(MedicalComponentType type)
        {
            if (_leftPanelContentRoot == null) return;

            // Clear previous items
            _leftPanelItems.Clear();
            for (int i = _leftPanelContentRoot.childCount - 1; i >= 0; i--)
            {
                var child = _leftPanelContentRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            // Set Title based on component
            if (_leftPanelTitleTmp != null)
            {
                switch (type)
                {
                    case MedicalComponentType.FullAnatomy:
                        _leftPanelTitleTmp.text = "<b>Anatomical Systems</b>";
                        break;
                    case MedicalComponentType.Cardiology:
                        _leftPanelTitleTmp.text = "<b>Cardiology Substructures</b>";
                        break;
                    case MedicalComponentType.Dental:
                        _leftPanelTitleTmp.text = "<b>Dental Substructures</b>";
                        break;
                    case MedicalComponentType.SoftTissue:
                        _leftPanelTitleTmp.text = "<b>Visceral Structures</b>";
                        break;
                    case MedicalComponentType.Procedure:
                        _leftPanelTitleTmp.text = "<b>Surgical Instruments</b>";
                        break;
                    case MedicalComponentType.OperatingTheatre:
                        _leftPanelTitleTmp.text = "<b>OR Suite Equipment</b>";
                        break;
                    default:
                        _leftPanelTitleTmp.text = "<b>Subsystem Explorer</b>";
                        break;
                }
            }

            var parts = GetPartsForComponent(type);
            MedicalPartData autoSelectPart = null;

            foreach (var part in parts)
            {
                var pCapture = part;
                var btnGo = CreateUIRect("Item_" + part.partKey, _leftPanelContentRoot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 36));
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = (part.partKey == _activeSelectedPart) ? new Color(0.08f, 0.22f, 0.32f, 0.95f) : new Color(0.10f, 0.13f, 0.18f, 0.85f);

                var itemBorder = CreateUIRect("Border", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                itemBorder.AddComponent<Image>().color = (part.partKey == _activeSelectedPart) ? MedicalUITheme.AccentCyan : MedicalUITheme.BorderSubtle;

                var hLayout = btnGo.AddComponent<HorizontalLayoutGroup>();
                hLayout.padding = new RectOffset(8, 8, 4, 4);
                hLayout.spacing = 8;
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childControlWidth = false;
                hLayout.childForceExpandWidth = false;
                hLayout.childControlHeight = true;
                hLayout.childForceExpandHeight = true;

                // Status / Accent Dot
                var dotGo = CreateUIRect("Dot", btnGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(7, 7));
                var dotImg = dotGo.AddComponent<Image>();
                dotImg.color = (part.partKey == _activeSelectedPart) ? MedicalUITheme.AccentCyan : new Color(0.35f, 0.42f, 0.52f, 1f);

                // Label Text
                var txtGo = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(190, 28));
                var txt = txtGo.AddComponent<TextMeshProUGUI>();
                txt.text = $"<b>{part.displayName}</b>\n<size=75%><color=#64748B>{part.category}</color></size>";
                txt.fontSize = 10.5f;
                txt.lineSpacing = -15f;
                txt.alignment = TextAlignmentOptions.MidlineLeft;
                txt.color = Color.white;

                var btn = btnGo.AddComponent<Button>();
                btn.onClick.AddListener(() => {
                    InspectSubsystem(pCapture);
                });

                _leftPanelItems.Add((part.partKey, btnImg, dotImg, txt));

                if (part.partKey == _activeSelectedPart)
                {
                    autoSelectPart = part;
                }
            }

            // Opacity Sliders if FullAnatomy
            if (type == MedicalComponentType.FullAnatomy)
            {
                var sDiv = CreateUIRect("SliderDiv", _leftPanelContentRoot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
                sDiv.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

                CreateOpacitySlider("Skeletal Opacity", 1.0f, (val) => AdjustSystemOpacity(_skeletalGo, val), _leftPanelContentRoot);
                CreateOpacitySlider("Muscular Opacity", 1.0f, (val) => AdjustSystemOpacity(_muscularGo, val), _leftPanelContentRoot);
                CreateOpacitySlider("Organ Opacity", 1.0f, (val) => AdjustSystemOpacity(_digestiveGo, val), _leftPanelContentRoot);
            }

            // If active part was not found in this component, auto-select the first part
            if (autoSelectPart == null && parts.Count > 0)
            {
                InspectSubsystem(parts[0]);
            }
            else if (autoSelectPart != null)
            {
                UpdateLeftPanelHighlights();
            }
        }

        public void InspectSubsystem(MedicalPartData part)
        {
            if (part == null) return;
            _activeSelectedPart = part.partKey;

            // Update Right Card Info
            if (_structureTitleTmp != null) _structureTitleTmp.text = $"<b>{part.displayName}</b>";
            if (_structureDescTmp != null) _structureDescTmp.text = part.description;
            if (_structureVascularTmp != null) _structureVascularTmp.text = $"<color=#00E5FF>Vascular Supply / Spec:</color>\n{part.vascular}";
            if (_structureClinicalTmp != null) _structureClinicalTmp.text = $"<color=#2DD4BF>Clinical Telemetry & Relevance:</color>\n{part.clinical}";

            // Camera focus
            if (part.cameraOffset != Vector3.zero)
            {
                SnapCameraTo(part.cameraOffset, part.cameraLookTarget);
            }

            // Audio feedback
            if (MedicalWorkstationAudioController.Instance != null)
            {
                MedicalWorkstationAudioController.Instance.PlayContextClip(part.audioClip);
            }

            UpdateLeftPanelHighlights();
        }

        private void UpdateLeftPanelHighlights()
        {
            foreach (var item in _leftPanelItems)
            {
                bool isSelected = item.partKey == _activeSelectedPart;
                if (item.bg != null)
                    item.bg.color = isSelected ? new Color(0.08f, 0.22f, 0.32f, 0.95f) : new Color(0.10f, 0.13f, 0.18f, 0.85f);
                if (item.dot != null)
                    item.dot.color = isSelected ? MedicalUITheme.AccentCyan : new Color(0.35f, 0.42f, 0.52f, 1f);
            }
        }

        public void OnStoryStepChanged(LevelStoryController.StoryAct act, LevelStoryController.ClinicalStep step, int actIdx, int stepIdx, int totalActs)
        {
            if (_storyBadgeTmp != null)
            {
                _storyBadgeTmp.text = $"<b><color=#00E5FF>ACT {act.actNumber} • STEP {stepIdx + 1}/{act.steps.Count}</color></b> <color=#475569>|</color> <color=#F8FAFC>{step.stepTitle}</color> <size=85%><color=#94A3B8>({step.clinicalObjective})</color></size>";
            }

            if (_activeType != step.targetComponent)
            {
                SelectMedicalComponent(step.targetComponent);
            }

            var parts = GetPartsForComponent(step.targetComponent);
            var targetPart = parts.Find(p => p.partKey.Equals(step.targetSubsystem, System.StringComparison.OrdinalIgnoreCase)) ?? (parts.Count > 0 ? parts[0] : null);
            if (targetPart != null)
            {
                InspectSubsystem(targetPart);
            }

            if (!string.IsNullOrEmpty(step.audioClipName))
            {
                MedicalWorkstationAudioController.Instance?.PlayContextClip(step.audioClipName);
            }
        }

        private void CreateSystemToggle(string label, bool initialVal, UnityEngine.Events.UnityAction<bool> onToggle, Transform parent)
        {
            var rowGo = CreateUIRect("Toggle_" + label, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 24));
            var hLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandHeight = true;
            hLayout.spacing = 8;

            var lblGo = CreateUIRect("Label", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(148, 24));
            var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 11.5f;
            lblTmp.color = MedicalUITheme.TextPrimary;
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Modern Pill Switch
            var switchGo = CreateUIRect("Switch", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(38, 20));
            var switchImg = switchGo.AddComponent<Image>();
            switchImg.color = initialVal ? new Color(0.12f, 0.45f, 0.85f, 1f) : MedicalUITheme.SurfaceElevated;

            var knobGo = CreateUIRect("Knob", switchGo.transform,
                new Vector2(initialVal ? 1 : 0, 0.5f), new Vector2(initialVal ? 1 : 0, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(initialVal ? -10 : 10, 0), new Vector2(14, 14));
            var knobImg = knobGo.AddComponent<Image>();
            knobImg.color = Color.white;

            var btn = switchGo.AddComponent<Button>();
            bool state = initialVal;
            btn.onClick.AddListener(() => {
                state = !state;
                switchImg.color = state ? new Color(0.12f, 0.45f, 0.85f, 1f) : MedicalUITheme.SurfaceElevated;
                var knobRt = knobGo.GetComponent<RectTransform>();
                knobRt.anchorMin = new Vector2(state ? 1 : 0, 0.5f);
                knobRt.anchorMax = new Vector2(state ? 1 : 0, 0.5f);
                knobRt.anchoredPosition = new Vector2(state ? -10 : 10, 0);
                onToggle?.Invoke(state);
            });
        }

        private void CreateOpacitySlider(string label, float initialVal, UnityEngine.Events.UnityAction<float> onSlide, Transform parent)
        {
            var rowGo = CreateUIRect("Slider_" + label.Replace(" ", "_"), parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 30));
            var vLayout = rowGo.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            var lblGo = CreateUIRect("Label", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 13));
            var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 9.5f;
            lblTmp.color = MedicalUITheme.TextSecondary;

            var sliderGo = CreateUIRect("SliderBar", rowGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 10));
            var sliderImg = sliderGo.AddComponent<Image>();
            sliderImg.color = MedicalUITheme.SurfaceElevated;

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0.05f;
            slider.maxValue = 1.0f;
            slider.value = initialVal;

            var fillGo = CreateUIRect("Fill", sliderGo.transform, Vector2.zero, new Vector2(initialVal, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = MedicalUITheme.AccentCyan;
            slider.fillRect = fillGo.GetComponent<RectTransform>();

            slider.onValueChanged.AddListener((v) => {
                onSlide?.Invoke(v);
            });
        }

        private void SetSystemActive(GameObject go, bool val)
        {
            if (go != null) go.SetActive(val);
        }

        private void AdjustSystemOpacity(GameObject go, float alpha)
        {
            if (go == null) return;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                {
                    Color c = r.sharedMaterial.color;
                    c.a = alpha;
                    r.sharedMaterial.color = c;
                }
            }
        }

        // =========================================================================
        // RIGHT DOCK: STRUCTURE INFO CARD (Matching Reference Image 1)
        // =========================================================================
        private void BuildRightStructureCard()
        {
            if (_rightStructureCardGo != null)
            {
                if (Application.isPlaying) Destroy(_rightStructureCardGo);
                else DestroyImmediate(_rightStructureCardGo);
            }

            _rightStructureCardGo = CreatePanel("RightStructureCard", transform,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-24, -90), new Vector2(300, 360));

            var vLayout = _rightStructureCardGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(16, 16, 14, 14);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childControlHeight = false;

            // Header Tag
            var tagGo = CreateUIRect("Tag", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 16));
            var tagTmp = tagGo.AddComponent<TextMeshProUGUI>();
            tagTmp.text = "<size=75%><color=#00E5FF>[ ACTIVE SUBSYSTEM TELEMETRY ]</color></size>";
            tagTmp.fontSize = 9.5f;

            // Structure Title
            var titleGo = CreateUIRect("Title", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 22));
            _structureTitleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            _structureTitleTmp.text = "<b>Structure: Left Kidney</b>";
            _structureTitleTmp.fontSize = 14;
            _structureTitleTmp.color = Color.white;

            // Description
            var descGo = CreateUIRect("Desc", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 72));
            _structureDescTmp = descGo.AddComponent<TextMeshProUGUI>();
            _structureDescTmp.text = "Left Kidney is proximal to retroperitoneal space and spleen. Contains 1.2 million nephrons performing active ultrafiltration and acid-base homeostasis.";
            _structureDescTmp.fontSize = 10;
            _structureDescTmp.color = new Color(0.85f, 0.90f, 0.95f);
            _structureDescTmp.textWrappingMode = TextWrappingModes.Normal;

            // Vascular / Specification
            var vascGo = CreateUIRect("Vascular", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 48));
            _structureVascularTmp = vascGo.AddComponent<TextMeshProUGUI>();
            _structureVascularTmp.text = "<color=#00E5FF>Vascular Supply:</color>\nLeft Renal Artery (Aorta) -> Left Renal Vein (IVC)";
            _structureVascularTmp.fontSize = 9.5f;
            _structureVascularTmp.textWrappingMode = TextWrappingModes.Normal;

            // Clinical Telemetry
            var clinGo = CreateUIRect("Clinical", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 48));
            _structureClinicalTmp = clinGo.AddComponent<TextMeshProUGUI>();
            _structureClinicalTmp.text = "<color=#2DD4BF>Clinical Telemetry:</color>\nGFR: 125 mL/min | Flow: Normal Perfusion";
            _structureClinicalTmp.fontSize = 9.5f;
            _structureClinicalTmp.textWrappingMode = TextWrappingModes.Normal;

            // Divider
            var div = CreateUIRect("Divider", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 1));
            div.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            // Action Buttons Row
            var btnRowGo = CreateUIRect("ActionsRow", _rightStructureCardGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 28));
            var hLayout = btnRowGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.childControlWidth = false;
            hLayout.childForceExpandWidth = false;

            CreateTopButton("FocusBtn", btnRowGo.transform, new Vector2(125, 26), () => {
                var parts = GetPartsForComponent(_activeType);
                var cur = parts.Find(p => p.partKey == _activeSelectedPart);
                if (cur != null && cur.cameraOffset != Vector3.zero)
                {
                    SnapCameraTo(cur.cameraOffset, cur.cameraLookTarget);
                }
            }, "Focus Camera", MedicalUITheme.SurfaceElevated, MedicalUITheme.AccentCyan);

            CreateTopButton("NextStepBtn", btnRowGo.transform, new Vector2(135, 26), () => {
                var story = LevelStoryController.Instance ?? FindFirstObjectByType<LevelStoryController>();
                story?.NextStep();
            }, "Next Case Step ▶", MedicalUITheme.AccentCyan, Color.black);
        }

        // =========================================================================
        // BOTTOM-RIGHT: INTERACTIVE 3D VIEWCUBE
        // =========================================================================
        private void BuildViewCube()
        {
            if (_viewCubeGo != null)
            {
                if (Application.isPlaying) Destroy(_viewCubeGo);
                else DestroyImmediate(_viewCubeGo);
            }

            _viewCubeGo = CreatePanel("InteractiveViewCube", transform,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-24, 45), new Vector2(86, 86));

            var vLayout = _viewCubeGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(6, 6, 6, 6);
            vLayout.spacing = 3;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;

            var titleGo = CreateUIRect("Title", _viewCubeGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 13));
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<size=75%><color=#64748B>VIEWCUBE</color></size>";
            titleTmp.fontSize = 8.5f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            var gridGo = CreateUIRect("Grid", _viewCubeGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 56));
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(35, 25);
            grid.spacing = new Vector2(4, 4);

            CreateViewCubeButton("TOP", () => SnapCameraTo(new Vector3(0.12f, 2.5f, 0f), new Vector3(0.12f, 0.35f, 0f)), gridGo.transform);
            CreateViewCubeButton("FRONT", () => SnapCameraTo(new Vector3(0.12f, 0.4f, -2.2f), new Vector3(0.12f, 0.35f, 0f)), gridGo.transform);
            CreateViewCubeButton("RIGHT", () => SnapCameraTo(new Vector3(2.2f, 0.4f, 0f), new Vector3(0.12f, 0.35f, 0f)), gridGo.transform);
            CreateViewCubeButton("ISO", () => SnapCameraTo(new Vector3(1.6f, 1.2f, -1.8f), new Vector3(0.12f, 0.35f, 0f)), gridGo.transform);
        }

        private void CreateViewCubeButton(string label, UnityEngine.Events.UnityAction action, Transform parent)
        {
            var btnGo = CreateUIRect("Btn_" + label, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            btnGo.AddComponent<Image>().color = MedicalUITheme.SurfaceElevated;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 8.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = MedicalUITheme.TextPrimary;
        }

        private void SnapCameraTo(Vector3 camPos, Vector3 lookTarget)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                _cam.transform.position = camPos;
                _cam.transform.LookAt(lookTarget);
            }
        }

        // =========================================================================
        // CALLOUT PINS WITH LEADER LINES
        // =========================================================================
        private void BuildCalloutPinsLayer()
        {
            if (_calloutPinsRoot != null)
            {
                if (Application.isPlaying) Destroy(_calloutPinsRoot);
                else DestroyImmediate(_calloutPinsRoot);
            }

            _calloutPinsRoot = new GameObject("CalloutPins_Layer");
            _calloutPinsRoot.transform.SetParent(transform, false);
            var rt = _calloutPinsRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            foreach (var pin in _pins)
            {
                var pinGo = new GameObject("Pin_" + pin.label);
                pinGo.transform.SetParent(_calloutPinsRoot.transform, false);
                var pinRt = pinGo.AddComponent<RectTransform>();
                pinRt.sizeDelta = new Vector2(170, 22);
                pin.pinGo = pinGo;

                // Glowing target dot
                var dotGo = CreateUIRect("TargetDot", pinGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8));
                pin.dotImg = dotGo.AddComponent<Image>();
                pin.dotImg.color = pin.label == "Kidney" ? MedicalUITheme.AccentCyan : Color.white;

                // Thin leader line
                var lineGo = CreateUIRect("LeaderLine", pinGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(45, 1f));
                var lineImg = lineGo.AddComponent<Image>();
                lineImg.color = new Color(1f, 1f, 1f, 0.55f);
                pin.lineRt = lineGo.GetComponent<RectTransform>();

                // Text Box Button
                var boxGo = CreateUIRect("TextBox", pinGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 20));
                boxGo.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.85f);

                var boxBorder = CreateUIRect("Border", boxGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                boxBorder.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

                var boxBtn = boxGo.AddComponent<Button>();
                var pinCapture = pin;
                boxBtn.onClick.AddListener(() => {
                    SelectStructure(pinCapture);
                });
                pin.labelRt = boxGo.GetComponent<RectTransform>();

                var txtGo = CreateUIRect("Text", boxGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                pin.labelTmp = txtGo.AddComponent<TextMeshProUGUI>();
                pin.labelTmp.text = pin.label;
                pin.labelTmp.fontSize = 10.5f;
                pin.labelTmp.color = pin.label == "Kidney" ? MedicalUITheme.AccentCyan : Color.white;
                pin.labelTmp.alignment = TextAlignmentOptions.Center;
            }
        }

        void LateUpdate()
        {
            if (_pinsVisible && _calloutPinsRoot != null && _calloutPinsRoot.activeSelf)
            {
                UpdatePinPositions();
            }
        }

        private void UpdatePinPositions()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || _canvas == null) return;

            var viewer = Medical3DComponentViewer.Instance;
            Vector3 modelBase = viewer != null ? viewer.transform.position : Vector3.zero;

            foreach (var pin in _pins)
            {
                if (pin.pinGo == null) continue;

                Vector3 targetWorld = modelBase + pin.worldOffset;
                Vector3 screenPos = _cam.WorldToScreenPoint(targetWorld);

                if (screenPos.z < 0.1f)
                {
                    pin.pinGo.SetActive(false);
                    continue;
                }

                pin.pinGo.SetActive(true);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    screenPos,
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam,
                    out Vector2 localPoint
                );

                var pinRt = pin.pinGo.GetComponent<RectTransform>();
                pinRt.anchoredPosition = localPoint;

                float lineLen = 42f;
                float sign = pin.isLeftAligned ? -1f : 1f;

                if (pin.lineRt != null)
                {
                    pin.lineRt.anchoredPosition = new Vector2(sign * lineLen * 0.5f, 0);
                    pin.lineRt.sizeDelta = new Vector2(lineLen, 1f);
                }

                if (pin.labelRt != null)
                {
                    pin.labelRt.anchoredPosition = new Vector2(sign * (lineLen + 46f), 0);
                }
            }
        }

        public void SelectStructure(PinData pin)
        {
            if (pin == null) return;

            if (_structureTitleTmp != null) _structureTitleTmp.text = $"<b>{pin.structureName}</b>";
            if (_structureDescTmp != null) _structureDescTmp.text = pin.description;
            if (_structureVascularTmp != null) _structureVascularTmp.text = $"<color=#00E5FF>Vascular Supply:</color>\n{pin.vascular}";
            if (_structureClinicalTmp != null) _structureClinicalTmp.text = $"<color=#2DD4BF>Clinical Telemetry:</color>\n{pin.clinical}";

            foreach (var other in _pins)
            {
                if (other.dotImg != null)
                {
                    other.dotImg.color = other == pin ? MedicalUITheme.AccentCyan : Color.white;
                    other.dotImg.rectTransform.sizeDelta = other == pin ? new Vector2(11, 11) : new Vector2(8, 8);
                }
                if (other.labelTmp != null)
                {
                    other.labelTmp.color = other == pin ? MedicalUITheme.AccentCyan : Color.white;
                }
            }
        }

        // =========================================================================
        // BOTTOM FOOTER: SINGLE SUBTLE LINE (Team Members & Controls)
        // =========================================================================
        private void BuildSubtleFooter()
        {
            if (_footerBarGo != null)
            {
                if (Application.isPlaying) Destroy(_footerBarGo);
                else DestroyImmediate(_footerBarGo);
            }

            _footerBarGo = CreateUIRect("SubtleFooter", transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                Vector2.zero, new Vector2(0, 28));
            _footerBarGo.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.10f, 0.95f);

            var border = CreateUIRect("Border", _footerBarGo.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                Vector2.zero, new Vector2(0, 1));
            border.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            var tmp = CreateUIRect("Text", _footerBarGo.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(16, 0), new Vector2(-32, 0)).AddComponent<TextMeshProUGUI>();
            tmp.text = "<b><color=#00E5FF>Team Members:</color></b> <color=#E2E8F0>Sanjith</color> | <color=#E2E8F0>Thrish</color> | <color=#E2E8F0>Praveen</color> | <color=#E2E8F0>Raghav</color> | <color=#E2E8F0>Raghaveshwara Karthikeyan</color> | <color=#E2E8F0>Vaishnav</color>    <color=#64748B>|    Controls: Left Drag: Orbit  |  Right/Shift: Pan  |  Scroll: Zoom  |  Space: Auto-Rotate  |  1-7: Switch Module</color>";
            tmp.fontSize = 10.5f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // =========================================================================
        // COMPONENT SWITCHER: INSTANT & CLEAN
        // =========================================================================
        public void SelectMedicalComponent(MedicalComponentType type, string targetScene = null)
        {
            _activeType = type;

            var viewer = Medical3DComponentViewer.Instance ?? FindFirstObjectByType<Medical3DComponentViewer>();
            if (viewer != null)
            {
                viewer.ShowComponent(type);
            }

            if (!string.IsNullOrEmpty(targetScene))
            {
                _currentTargetScene = targetScene;
            }

            // Ensure Left Subsystems Panel stays ALWAYS ACTIVE and dynamically rebuilds
            if (_leftAnatomyPanelGo != null)
            {
                _leftAnatomyPanelGo.SetActive(true);
            }
            RebuildLeftSubsystemPanel(type);

            // Update Top Navbar Tab Highlights
            UpdateNavbarTabHighlights();

            if (_calloutPinsRoot != null)
            {
                _calloutPinsRoot.SetActive(type == MedicalComponentType.FullAnatomy && _pinsVisible);
            }

            if (type == MedicalComponentType.FullAnatomy && string.IsNullOrEmpty(_activeSelectedPart))
            {
                var pin = _pins.Find(p => p.label == "Kidney");
                if (pin != null) SelectStructure(pin);
            }
        }

        // =========================================================================
        // UI HELPERS
        // =========================================================================
        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = CreateUIRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.07f, 0.09f, 0.12f, 0.92f);

            var border = CreateUIRect("Border", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            border.AddComponent<Image>().color = MedicalUITheme.BorderSubtle;

            return go;
        }

        private GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }
    }

    /// <summary>
    /// Master Clinical Audio Management System for the Medical Workstation.
    /// Provides context-aware procedural soundscapes, dynamic cardiology heartbeat pulsing,
    /// dental scaler whirs, hospital OR ventilation ambience, vital ECG monitor beeps,
    /// and tactile UI button feedback.
    /// </summary>
    public class MedicalWorkstationAudioController : MonoBehaviour
    {
        public static MedicalWorkstationAudioController Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private AudioSource _rhythmSource;
        [SerializeField] private AudioSource _uiSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Master Controls")]
        [Range(0f, 1f)] public float masterVolume = 0.85f;
        public bool isMuted = false;

        // Cached Audio Clips
        private AudioClip _clipHeartbeat;
        private AudioClip _clipDentalScaler;
        private AudioClip _clipDentalDrill;
        private AudioClip _clipDentalSuction;
        private AudioClip _clipLabAmbience;
        private AudioClip _clipOrAmbience;
        private AudioClip _clipEcgBeep;
        private AudioClip _clipInstrumentClink;
        private AudioClip _clipSuctionLoop;
        private AudioClip _clipUiSelect;
        private AudioClip _clipUiConfirm;

        private Coroutine _ecgRoutine;
        private Coroutine _heartbeatRoutine;
        private MedicalComponentType _currentMode = MedicalComponentType.FullAnatomy;

        void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureSources();
            LoadAudioClips();
        }

        void Start()
        {
            SetModeAudio(_currentMode);
        }

        private void EnsureSources()
        {
            if (_ambienceSource == null) _ambienceSource = CreateSource("AmbienceSource", true, 0.3f);
            if (_rhythmSource == null) _rhythmSource = CreateSource("RhythmSource", false, 0.65f);
            if (_uiSource == null) _uiSource = CreateSource("UISource", false, 0.75f);
            if (_sfxSource == null) _sfxSource = CreateSource("SFXSource", false, 0.5f);
        }

        private AudioSource CreateSource(string name, bool loop, float defaultVolume)
        {
            Transform existing = transform.Find(name);
            AudioSource src = null;
            if (existing != null) src = existing.GetComponent<AudioSource>();
            if (src == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(transform, false);
                src = go.AddComponent<AudioSource>();
            }
            src.playOnAwake = false;
            src.loop = loop;
            src.volume = defaultVolume * (isMuted ? 0f : masterVolume);
            src.spatialBlend = 0f; // Clean stereo GUI/HUD sound
            return src;
        }

        private void LoadAudioClips()
        {
            _clipHeartbeat = Resources.Load<AudioClip>("MedicalAudio/heartbeat");
            _clipDentalScaler = Resources.Load<AudioClip>("MedicalAudio/dental_scaler");
            _clipDentalDrill = Resources.Load<AudioClip>("MedicalAudio/dental_drill");
            _clipDentalSuction = Resources.Load<AudioClip>("MedicalAudio/dental_suction");
            _clipLabAmbience = Resources.Load<AudioClip>("MedicalAudio/lab_ambience");
            _clipOrAmbience = Resources.Load<AudioClip>("MedicalAudio/or_ventilation_ambience");
            _clipEcgBeep = Resources.Load<AudioClip>("MedicalAudio/monitor_ecg_beep");
            _clipInstrumentClink = Resources.Load<AudioClip>("MedicalAudio/surgical_instrument_clink");
            _clipSuctionLoop = Resources.Load<AudioClip>("MedicalAudio/surgical_suction_loop");
            _clipUiSelect = Resources.Load<AudioClip>("MedicalAudio/ui_select");
            _clipUiConfirm = Resources.Load<AudioClip>("MedicalAudio/ui_confirm");
        }

        public void SetModeAudio(MedicalComponentType mode)
        {
            _currentMode = mode;
            StopAllCoroutines();

            if (isMuted) return;

            switch (mode)
            {
                case MedicalComponentType.FullAnatomy:
                    PlayAmbience(_clipLabAmbience, 0.25f);
                    break;

                case MedicalComponentType.Cardiology:
                    PlayAmbience(_clipLabAmbience, 0.15f);
                    _heartbeatRoutine = StartCoroutine(HeartbeatRhythmRoutine());
                    break;

                case MedicalComponentType.Dental:
                    PlayAmbience(_clipDentalSuction, 0.18f);
                    if (_sfxSource != null && _clipDentalScaler != null)
                    {
                        _sfxSource.clip = _clipDentalScaler;
                        _sfxSource.loop = true;
                        _sfxSource.volume = 0.22f * masterVolume;
                        _sfxSource.Play();
                    }
                    break;

                case MedicalComponentType.SoftTissue:
                    PlayAmbience(_clipLabAmbience, 0.20f);
                    break;

                case MedicalComponentType.OperatingTheatre:
                    PlayAmbience(_clipOrAmbience, 0.35f);
                    _ecgRoutine = StartCoroutine(EcgMonitorBeepRoutine());
                    break;

                case MedicalComponentType.Procedure:
                    PlayAmbience(_clipSuctionLoop, 0.20f);
                    PlaySFX(_clipInstrumentClink, 0.6f);
                    break;

                default:
                    PlayAmbience(_clipLabAmbience, 0.20f);
                    break;
            }
        }

        private void PlayAmbience(AudioClip clip, float volume)
        {
            if (_ambienceSource == null) return;
            if (clip == null)
            {
                _ambienceSource.Stop();
                return;
            }
            if (_ambienceSource.clip == clip && _ambienceSource.isPlaying) return;

            _ambienceSource.clip = clip;
            _ambienceSource.volume = volume * (isMuted ? 0f : masterVolume);
            _ambienceSource.loop = true;
            _ambienceSource.Play();
        }

        private IEnumerator HeartbeatRhythmRoutine()
        {
            // 72 BPM: ~0.833s per cardiac cycle (Lub-Dub sound)
            while (true)
            {
                if (!isMuted && _rhythmSource != null && _clipHeartbeat != null)
                {
                    _rhythmSource.PlayOneShot(_clipHeartbeat, 0.85f * masterVolume);
                }
                yield return new WaitForSeconds(0.833f);
            }
        }

        private IEnumerator EcgMonitorBeepRoutine()
        {
            // Normal sinus rhythm 75 BPM: 0.80s interval
            while (true)
            {
                if (!isMuted && _rhythmSource != null && _clipEcgBeep != null)
                {
                    _rhythmSource.PlayOneShot(_clipEcgBeep, 0.60f * masterVolume);
                }
                yield return new WaitForSeconds(0.80f);
            }
        }

        public void PlayUIClick()
        {
            if (isMuted || _uiSource == null || _clipUiSelect == null) return;
            _uiSource.PlayOneShot(_clipUiSelect, 0.70f * masterVolume);
        }

        public void PlayContextClip(string clipName)
        {
            if (isMuted || _uiSource == null || string.IsNullOrEmpty(clipName)) return;
            var clip = Resources.Load<AudioClip>($"MedicalAudio/{clipName}");
            if (clip != null)
            {
                _uiSource.PlayOneShot(clip, 0.65f * masterVolume);
            }
        }

        public void PlayUIConfirm()
        {
            if (isMuted || _uiSource == null || _clipUiConfirm == null) return;
            _uiSource.PlayOneShot(_clipUiConfirm, 0.85f * masterVolume);
        }

        public void PlaySFX(AudioClip clip, float vol = 0.6f)
        {
            if (isMuted || _sfxSource == null || clip == null) return;
            _sfxSource.PlayOneShot(clip, vol * masterVolume);
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            if (_ambienceSource != null) _ambienceSource.volume = isMuted ? 0f : 0.3f * masterVolume;
            if (_rhythmSource != null) _rhythmSource.volume = isMuted ? 0f : 0.65f * masterVolume;
            if (_sfxSource != null) _sfxSource.volume = isMuted ? 0f : 0.5f * masterVolume;

            if (!isMuted) SetModeAudio(_currentMode);
            else StopAllCoroutines();
        }

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            if (!isMuted)
            {
                if (_ambienceSource != null) _ambienceSource.volume = 0.3f * masterVolume;
                if (_rhythmSource != null) _rhythmSource.volume = 0.65f * masterVolume;
                if (_sfxSource != null) _sfxSource.volume = 0.5f * masterVolume;
            }
        }
    }
}
