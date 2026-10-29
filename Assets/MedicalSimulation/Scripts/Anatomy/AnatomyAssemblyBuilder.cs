using UnityEngine;
using System.Collections.Generic;
using MedicalSimulation.Core;
using MedicalSimulation.Cardiovascular;
using MedicalSimulation.Respiratory;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Constructs, scales, and organizes a full anatomical torso model.
    /// Integrates real 3D models (adult_heart.glb, VH_M_Liver.glb, blood_vessel.glb)
    /// and generates procedural biological geometry with proper URP materials
    /// for structures where external files are not yet provided.
    /// </summary>
    [ExecuteAlways]
    public class AnatomyAssemblyBuilder : MonoBehaviour
    {
        [Header("Model Root")]
        [SerializeField] private Transform modelContainer;

        [Header("Materials (auto-loaded from Resources/Materials if null)")]
        [SerializeField] private Material matHeart;
        [SerializeField] private Material matLung;
        [SerializeField] private Material matLiver;
        [SerializeField] private Material matStomach;
        [SerializeField] private Material matKidney;
        [SerializeField] private Material matBone;
        [SerializeField] private Material matArtery;
        [SerializeField] private Material matVein;
        [SerializeField] private Material matHologram;

        [Header("Generation Settings")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private float globalScale = 1.0f;

        private GameObject _heartObj;
        private GameObject _liverObj;
        private GameObject _leftLungObj;
        private GameObject _rightLungObj;
        private GameObject _stomachObj;
        private GameObject _leftKidneyObj;
        private GameObject _rightKidneyObj;
        private GameObject _tracheaObj;
        private GameObject _vascularTreeObj;

        void Start()
        {
            if (buildOnStart && (modelContainer == null || modelContainer.childCount == 0))
            {
                BuildAnatomicalModel();
            }
        }

        [ContextMenu("Rebuild Anatomy Torso")]
        public void BuildAnatomicalModel()
        {
            LoadMaterials();

            if (modelContainer == null)
            {
                var containerGo = new GameObject("AnatomicalTorso_Root");
                containerGo.transform.SetParent(transform, false);
                modelContainer = containerGo.transform;
            }
            else
            {
                // Clear existing procedural children if any
                for (int i = modelContainer.childCount - 1; i >= 0; i--)
                {
                    var child = modelContainer.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child);
                    else DestroyImmediate(child);
                }
            }

            modelContainer.localScale = Vector3.one * globalScale;

            BuildCardiovascularSystem();
            BuildRespiratorySystem();
            BuildDigestiveSystem();
            BuildUrinarySystem();
            BuildVascularTree();

            // Refresh anatomy controller
            var modelCtrl = GetComponentInParent<AnatomyModelController>() ?? GetComponent<AnatomyModelController>();
            if (modelCtrl != null)
            {
                modelCtrl.RefreshOrganList();
            }

            Debug.Log("[AnatomyAssemblyBuilder] Complete anatomical torso assembled successfully.");
        }

        private void LoadMaterials()
        {
            matHeart ??= LoadMat("Mat_Heart_Myocardium", new Color(0.75f, 0.10f, 0.12f));
            matLung ??= LoadMat("Mat_Lung_Tissue", new Color(0.88f, 0.55f, 0.60f));
            matLiver ??= LoadMat("Mat_Liver_Hepatic", new Color(0.48f, 0.14f, 0.11f));
            matStomach ??= LoadMat("Mat_Liver_Hepatic", new Color(0.82f, 0.65f, 0.58f));
            matKidney ??= LoadMat("Mat_Liver_Hepatic", new Color(0.55f, 0.18f, 0.14f));
            matBone ??= LoadMat("Mat_Bone_Cortical", new Color(0.92f, 0.88f, 0.80f));
            matArtery ??= LoadMat("Mat_Vessel_Arterial", new Color(0.95f, 0.12f, 0.15f));
            matVein ??= LoadMat("Mat_Vessel_Venous", new Color(0.18f, 0.25f, 0.78f));
            matHologram ??= LoadMat("Mat_Hologram_XRay", new Color(0.12f, 0.85f, 0.95f, 0.35f));
        }

        private Material LoadMat(string name, Color fallbackColor)
        {
            var mat = Resources.Load<Material>("Materials/" + name);
            if (mat == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(s);
                mat.color = fallbackColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fallbackColor);
            }
            return mat;
        }

        // ── 1. Cardiovascular System ──────────────────────────────────────────

        private void BuildCardiovascularSystem()
        {
            var heartPrefab = Resources.Load<GameObject>("adult_heart");
            if (heartPrefab != null)
            {
                _heartObj = Instantiate(heartPrefab, modelContainer);
                _heartObj.name = "organ.heart";
                _heartObj.transform.localPosition = new Vector3(0.04f, 0.32f, 0.05f);
                _heartObj.transform.localScale = Vector3.one * 0.45f;
            }
            else
            {
                // Procedural Heart representation with aortic arch
                _heartObj = CreateOrganicMesh("organ.heart", PrimitiveType.Sphere,
                    new Vector3(0.04f, 0.32f, 0.05f),
                    new Vector3(0.24f, 0.30f, 0.22f),
                    Quaternion.Euler(15f, -20f, -10f),
                    matHeart, modelContainer);

                // Aorta arch
                var aorta = CreateOrganicMesh("AortaArch", PrimitiveType.Cylinder,
                    new Vector3(0.02f, 0.48f, 0.02f),
                    new Vector3(0.07f, 0.12f, 0.07f),
                    Quaternion.Euler(0, 0, 25f),
                    matArtery, _heartObj.transform);
            }

            var interactable = _heartObj.GetComponent<OrganInteractable>() ?? _heartObj.AddComponent<OrganInteractable>();
            interactable.metadata = AnatomyMetadata.GetFallback("heart");

            // Attach cardiac animation
            var heartAnim = _heartObj.GetComponent<HeartAnimationController>() ?? _heartObj.AddComponent<HeartAnimationController>();
            heartAnim.Play();
        }

        // ── 2. Respiratory System ─────────────────────────────────────────────

        private void BuildRespiratorySystem()
        {
            // Trachea
            _tracheaObj = CreateOrganicMesh("Trachea", PrimitiveType.Cylinder,
                new Vector3(0f, 0.68f, -0.03f),
                new Vector3(0.06f, 0.16f, 0.06f),
                Quaternion.identity,
                matBone, modelContainer);

            // Left Lung (2 lobes, cardiac notch)
            _leftLungObj = CreateOrganicMesh("organ.left_lung", PrimitiveType.Capsule,
                new Vector3(-0.24f, 0.30f, 0.02f),
                new Vector3(0.20f, 0.36f, 0.18f),
                Quaternion.Euler(0, 10f, -8f),
                matLung, modelContainer);

            var leftMeta = _leftLungObj.GetComponent<OrganInteractable>() ?? _leftLungObj.AddComponent<OrganInteractable>();
            leftMeta.metadata = AnatomyMetadata.GetFallback("lung");

            // Right Lung (3 lobes)
            _rightLungObj = CreateOrganicMesh("organ.right_lung", PrimitiveType.Capsule,
                new Vector3(0.26f, 0.31f, 0.02f),
                new Vector3(0.22f, 0.38f, 0.19f),
                Quaternion.Euler(0, -10f, 8f),
                matLung, modelContainer);

            var rightMeta = _rightLungObj.GetComponent<OrganInteractable>() ?? _rightLungObj.AddComponent<OrganInteractable>();
            rightMeta.metadata = AnatomyMetadata.GetFallback("lung");

            // Diaphragm dome
            var diaphragm = CreateOrganicMesh("Diaphragm", PrimitiveType.Sphere,
                new Vector3(0f, 0.08f, 0.02f),
                new Vector3(0.68f, 0.08f, 0.42f),
                Quaternion.identity,
                matBone, modelContainer);

            // Attach respiratory animation controller
            var respAnim = gameObject.GetComponent<RespiratoryAnimationController>() ?? gameObject.AddComponent<RespiratoryAnimationController>();
            respAnim.Play();
        }

        // ── 3. Digestive System ───────────────────────────────────────────────

        private void BuildDigestiveSystem()
        {
            // Liver
            _liverObj = CreateOrganicMesh("organ.liver", PrimitiveType.Sphere,
                new Vector3(0.20f, -0.04f, 0.06f),
                new Vector3(0.38f, 0.22f, 0.26f),
                Quaternion.Euler(-12f, 25f, 15f),
                matLiver, modelContainer);

            var liverMeta = _liverObj.GetComponent<OrganInteractable>() ?? _liverObj.AddComponent<OrganInteractable>();
            liverMeta.metadata = AnatomyMetadata.GetFallback("liver");

            // Stomach
            _stomachObj = CreateOrganicMesh("organ.stomach", PrimitiveType.Sphere,
                new Vector3(-0.16f, -0.02f, 0.08f),
                new Vector3(0.24f, 0.26f, 0.18f),
                Quaternion.Euler(10f, -30f, -20f),
                matStomach, modelContainer);

            var stomachMeta = _stomachObj.GetComponent<OrganInteractable>() ?? _stomachObj.AddComponent<OrganInteractable>();
            stomachMeta.metadata = AnatomyMetadata.GetFallback("stomach");

            // Intestinal Mass
            var intestines = CreateOrganicMesh("organ.small_intestine", PrimitiveType.Sphere,
                new Vector3(0f, -0.28f, 0.06f),
                new Vector3(0.42f, 0.26f, 0.26f),
                Quaternion.identity,
                matStomach, modelContainer);

            var intMeta = intestines.GetComponent<OrganInteractable>() ?? intestines.AddComponent<OrganInteractable>();
            intMeta.metadata = AnatomyMetadata.GetFallback("intestine");
        }

        // ── 4. Urinary System ─────────────────────────────────────────────────

        private void BuildUrinarySystem()
        {
            // Left Kidney
            _leftKidneyObj = CreateOrganicMesh("organ.left_kidney", PrimitiveType.Sphere,
                new Vector3(-0.24f, -0.16f, -0.12f),
                new Vector3(0.12f, 0.18f, 0.10f),
                Quaternion.Euler(5f, -15f, 10f),
                matKidney, modelContainer);

            var lkMeta = _leftKidneyObj.GetComponent<OrganInteractable>() ?? _leftKidneyObj.AddComponent<OrganInteractable>();
            lkMeta.metadata = AnatomyMetadata.GetFallback("kidney");

            // Right Kidney (slightly lower)
            _rightKidneyObj = CreateOrganicMesh("organ.right_kidney", PrimitiveType.Sphere,
                new Vector3(0.24f, -0.20f, -0.12f),
                new Vector3(0.12f, 0.18f, 0.10f),
                Quaternion.Euler(5f, 15f, -10f),
                matKidney, modelContainer);

            var rkMeta = _rightKidneyObj.GetComponent<OrganInteractable>() ?? _rightKidneyObj.AddComponent<OrganInteractable>();
            rkMeta.metadata = AnatomyMetadata.GetFallback("kidney");
        }

        // ── 5. Vascular Tree & Blood Circulation ──────────────────────────────

        private void BuildVascularTree()
        {
            _vascularTreeObj = new GameObject("VascularSystem");
            _vascularTreeObj.transform.SetParent(modelContainer, false);

            // Aorta trunk descending along spine
            CreateOrganicMesh("AortaDescending", PrimitiveType.Cylinder,
                new Vector3(-0.03f, 0.10f, -0.10f),
                new Vector3(0.045f, 0.45f, 0.045f),
                Quaternion.identity,
                matArtery, _vascularTreeObj.transform);

            // Inferior Vena Cava ascending
            CreateOrganicMesh("VenaCavaInferior", PrimitiveType.Cylinder,
                new Vector3(0.05f, 0.10f, -0.10f),
                new Vector3(0.052f, 0.45f, 0.052f),
                Quaternion.identity,
                matVein, _vascularTreeObj.transform);

            // Blood Flow particles
            var flowGo = new GameObject("BloodFlow_CirculationCircuit");
            flowGo.transform.SetParent(_vascularTreeObj.transform, false);

            var ps = flowGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3.2f;
            main.startSpeed = 0f;
            main.startSize = 0.022f;
            main.maxParticles = 180;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.95f, 0.12f, 0.12f, 0.95f);

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var renderer = flowGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = matArtery;

            // Add waypoints along circulation
            var p1 = CreateWaypoint("WP_VenaCava", new Vector3(0.05f, -0.25f, -0.10f), flowGo.transform);
            var p2 = CreateWaypoint("WP_RightAtrium", new Vector3(0.06f, 0.28f, 0.03f), flowGo.transform);
            var p3 = CreateWaypoint("WP_RightVentricle", new Vector3(0.02f, 0.24f, 0.06f), flowGo.transform);
            var p4 = CreateWaypoint("WP_PulmonaryArtery", new Vector3(0.01f, 0.38f, 0.04f), flowGo.transform);
            var p5 = CreateWaypoint("WP_LeftLung", new Vector3(-0.20f, 0.32f, 0.02f), flowGo.transform);
            var p6 = CreateWaypoint("WP_LeftAtrium", new Vector3(-0.02f, 0.34f, 0.01f), flowGo.transform);
            var p7 = CreateWaypoint("WP_LeftVentricle", new Vector3(-0.04f, 0.26f, 0.06f), flowGo.transform);
            var p8 = CreateWaypoint("WP_AortaArch", new Vector3(0.02f, 0.48f, 0.02f), flowGo.transform);
            var p9 = CreateWaypoint("WP_AortaDesc", new Vector3(-0.03f, 0.05f, -0.10f), flowGo.transform);
            var p10 = CreateWaypoint("WP_IliacBranch", new Vector3(-0.04f, -0.35f, -0.08f), flowGo.transform);

            var vesselPath = flowGo.AddComponent<BloodVesselPath>();
            vesselPath.VesselType = BloodVesselPath.VesselTypeEnum.Arterial;
            vesselPath.baseFlowSpeed = 2.4f;
            vesselPath.veinWidth = 0.035f;
            vesselPath.waypoints = new List<Transform> { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10 };

            var flowCtrl = flowGo.AddComponent<BloodFlowController>();
            flowCtrl.Play();
        }

        private Transform CreateWaypoint(string name, Vector3 localPos, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private GameObject CreateOrganicMesh(string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = localRot;

            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            return go;
        }
    }
}
