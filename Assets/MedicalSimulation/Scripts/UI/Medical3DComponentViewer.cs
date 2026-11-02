using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Master 3D Medical Component Viewer.
    /// Manages high-fidelity 3D anatomical models, dental structures, hepatic tissue,
    /// cardiology suites, and virtual operating theatre inside the main viewport.
    /// Runs seamlessly in Edit Mode [ExecuteAlways] and Play Mode.
    /// </summary>
    [ExecuteAlways]
    public class Medical3DComponentViewer : MonoBehaviour
    {
        public static Medical3DComponentViewer Instance { get; private set; }

        [Header("Active Component")]
        [SerializeField] private MedicalComponentType _activeComponent = MedicalComponentType.FullAnatomy;
        public MedicalComponentType ActiveComponent => _activeComponent;

        [Header("Display Settings")]
        public float autoRotateSpeed = 10f;
        public bool autoRotate = true;

        // Child component roots
        private GameObject _torsoGo;
        private GameObject _heartGo;
        private GameObject _dentalGo;
        private GameObject _digestiveGo;
        private GameObject _diagnosisGo;
        private GameObject _procedureGo;
        private GameObject _orGo;

        // Pulse timer for cardiology
        private float _pulseTimer = 0f;
        private Vector3 _heartBaseScale = Vector3.one;

        void Awake()
        {
            Instance = this;
            if (Application.isPlaying && MedicalWorkstationAudioController.Instance == null)
            {
                var audioGo = new GameObject("MedicalWorkstationAudioController");
                audioGo.AddComponent<MedicalWorkstationAudioController>();
            }
            EnsureComponentModels();
            ShowComponent(_activeComponent);
        }

        void OnEnable()
        {
            Instance = this;
            EnsureComponentModels();
            ShowComponent(_activeComponent);
        }

        void Update()
        {
            if (autoRotate)
            {
                float rot = autoRotateSpeed * (Application.isPlaying ? Time.deltaTime : 0.016f);
                transform.Rotate(0f, rot, 0f, Space.World);
            }

            // Cardiology realistic systolic/diastolic pulsing deformation
            if (_activeComponent == MedicalComponentType.Cardiology && _heartGo != null && _heartGo.activeSelf)
            {
                _pulseTimer += (Application.isPlaying ? Time.deltaTime : 0.016f) * 1.2f;
                float beat = Mathf.Sin(_pulseTimer * Mathf.PI * 2f);
                float pulse = 1.0f + Mathf.Max(0f, beat) * 0.05f;
                _heartGo.transform.localScale = _heartBaseScale * pulse;
            }

            // Keyboard shortcuts (1 - 7)
            if (Application.isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ShowComponent(MedicalComponentType.FullAnatomy);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) ShowComponent(MedicalComponentType.Cardiology);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) ShowComponent(MedicalComponentType.Dental);
                else if (Input.GetKeyDown(KeyCode.Alpha4)) ShowComponent(MedicalComponentType.SoftTissue);
                else if (Input.GetKeyDown(KeyCode.Alpha5)) ShowComponent(MedicalComponentType.Diagnosis);
                else if (Input.GetKeyDown(KeyCode.Alpha6)) ShowComponent(MedicalComponentType.Procedure);
                else if (Input.GetKeyDown(KeyCode.Alpha7)) ShowComponent(MedicalComponentType.OperatingTheatre);
            }
        }

        public void ShowComponent(MedicalComponentType type)
        {
            _activeComponent = type;
            EnsureComponentModels();

            // Trigger clinical procedural soundscapes and audio cues
            if (Application.isPlaying && MedicalWorkstationAudioController.Instance != null)
            {
                MedicalWorkstationAudioController.Instance.SetModeAudio(type);
                MedicalWorkstationAudioController.Instance.PlayUIConfirm();
            }

            if (_torsoGo != null) _torsoGo.SetActive(type == MedicalComponentType.FullAnatomy);
            if (_heartGo != null) _heartGo.SetActive(type == MedicalComponentType.Cardiology);
            if (_dentalGo != null) _dentalGo.SetActive(type == MedicalComponentType.Dental);
            if (_digestiveGo != null) _digestiveGo.SetActive(type == MedicalComponentType.SoftTissue);
            if (_diagnosisGo != null) _diagnosisGo.SetActive(type == MedicalComponentType.Diagnosis);
            if (_procedureGo != null) _procedureGo.SetActive(type == MedicalComponentType.Procedure);
            if (_orGo != null) _orGo.SetActive(type == MedicalComponentType.OperatingTheatre);

            // Set optimal viewing heights
            switch (type)
            {
                case MedicalComponentType.FullAnatomy:
                    transform.position = new Vector3(0.12f, 0.0f, 0f);
                    break;
                case MedicalComponentType.Cardiology:
                    transform.position = new Vector3(0.12f, 0.05f, 0f);
                    break;
                case MedicalComponentType.Dental:
                    transform.position = new Vector3(0.12f, 0.05f, 0f);
                    break;
                case MedicalComponentType.SoftTissue:
                    transform.position = new Vector3(0.12f, 0.05f, 0f);
                    break;
                case MedicalComponentType.Diagnosis:
                    transform.position = new Vector3(0.12f, 0.05f, 0f);
                    break;
                case MedicalComponentType.Procedure:
                    transform.position = new Vector3(0.12f, 0.05f, 0f);
                    break;
                case MedicalComponentType.OperatingTheatre:
                    transform.position = new Vector3(0.12f, -0.2f, 0f);
                    break;
            }
        }

        public void EnsureComponentModels()
        {
            if (_torsoGo == null)
            {
                _torsoGo = InstantiateOrFindModel("Human_Torso_Anatomy_Full", "Comp_01_FullAnatomy");
                FitModelInViewport(_torsoGo, 1.85f, new Vector3(0, 0.25f, 0));
            }

            if (_heartGo == null)
            {
                _heartGo = InstantiateOrFindModel("Heart_Detailed", "Comp_02_Cardiology");
                FitModelInViewport(_heartGo, 1.35f, new Vector3(0, 0.35f, 0));
                if (_heartGo != null) _heartBaseScale = _heartGo.transform.localScale;
            }

            if (_dentalGo == null)
            {
                _dentalGo = InstantiateOrFindModel("Dental_Mandible_Teeth", "Comp_03_Dental");
                FitModelInViewport(_dentalGo, 1.35f, new Vector3(0, 0.35f, 0));
            }

            if (_digestiveGo == null)
            {
                _digestiveGo = InstantiateOrFindModel("Digestive_System", "Comp_04_SoftTissue");
                FitModelInViewport(_digestiveGo, 1.45f, new Vector3(0, 0.35f, 0));
            }

            if (_diagnosisGo == null)
            {
                _diagnosisGo = InstantiateOrFindModel("Cardiovascular_System", "Comp_05_Diagnosis");
                FitModelInViewport(_diagnosisGo, 1.45f, new Vector3(0, 0.35f, 0));
            }

            if (_procedureGo == null)
            {
                _procedureGo = InstantiateOrFindModel("Surgical_Instruments_Tray", "Comp_06_Procedure");
                FitModelInViewport(_procedureGo, 1.35f, new Vector3(0, 0.35f, 0));
            }

            if (_orGo == null)
            {
                _orGo = InstantiateOrFindModel("Operating_Theatre_Suite", "Comp_07_OperatingTheatre");
                FitModelInViewport(_orGo, 2.8f, new Vector3(0, -0.1f, 0));
            }

            ApplyClinicalMaterials();
        }

        [ContextMenu("Force Reload High-Poly Clinical Models")]
        public void ForceReloadModels()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            _torsoGo = null;
            _heartGo = null;
            _dentalGo = null;
            _digestiveGo = null;
            _diagnosisGo = null;
            _procedureGo = null;
            _orGo = null;

            EnsureComponentModels();
            ShowComponent(_activeComponent);
        }

        private GameObject InstantiateOrFindModel(string modelName, string childName)
        {
            Transform existing = transform.Find(childName);
            if (existing != null)
            {
                // Purge dummy fallbacks and outdated models without clinical scans
                bool isDummy = existing.Find("Cylinder") != null || 
                               (existing.GetComponentInChildren<MeshFilter>() != null && 
                                existing.GetComponentInChildren<MeshFilter>().sharedMesh != null &&
                                existing.GetComponentInChildren<MeshFilter>().sharedMesh.name.Contains("Cylinder"));

                bool isOutdatedTorso = modelName == "Human_Torso_Anatomy_Full" &&
                                       existing.Find("System_Skeletal/Spine") == null &&
                                       existing.Find("System_Skeletal/Spine_Vertebral_Column") == null &&
                                       existing.Find("Spine") == null &&
                                       existing.Find("Spine_Vertebral_Column") == null;

                if (!isDummy && !isOutdatedTorso)
                {
                    return existing.gameObject;
                }

                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            GameObject prefab = Resources.Load<GameObject>("Models/" + modelName);
            #if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Resources/Models/{modelName}.fbx")
                      ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Art/Anatomy/{modelName}.fbx")
                      ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Art/Organs/{modelName}.fbx")
                      ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Art/OperatingTheatre/{modelName}.fbx")
                      ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Art/Skeleton/{modelName}.fbx")
                      ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Art/Vessels/{modelName}.fbx");
            }
            #endif

            GameObject instance = null;

            if (prefab != null)
            {
                instance = Instantiate(prefab, transform);
                instance.name = childName;
            }
            else
            {
                instance = new GameObject(childName);
                instance.transform.SetParent(transform, false);
            }

            return instance;
        }

        private void FitModelInViewport(GameObject go, float targetSize, Vector3 localOffset)
        {
            if (go == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDim > 0.001f)
            {
                float scaleMultiplier = targetSize / maxDim;
                go.transform.localScale = Vector3.one * scaleMultiplier;

                // Center mesh bounds
                Bounds scaledBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    scaledBounds.Encapsulate(renderers[i].bounds);
                }

                Vector3 delta = go.transform.position - scaledBounds.center;
                go.transform.localPosition = delta + localOffset;
            }
        }

        private void ApplyClinicalMaterials()
        {
            var boneMat = Resources.Load<Material>("Materials/Mat_Bone_Cortical");
            var liverMat = Resources.Load<Material>("Materials/Mat_Liver_Hepatic");

            #if UNITY_EDITOR
            if (boneMat == null) boneMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/MedicalSimulation/Materials/Mat_Bone_Cortical.mat");
            if (liverMat == null) liverMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/MedicalSimulation/Materials/Mat_Liver_Hepatic.mat");
            #endif

            // Enhance dental jaw materials
            if (_dentalGo != null && boneMat != null)
            {
                foreach (var r in _dentalGo.GetComponentsInChildren<Renderer>())
                {
                    if (r.sharedMaterial == null || r.sharedMaterial.name.Contains("Default"))
                    {
                        r.sharedMaterial = boneMat;
                    }
                }
            }

            // Enhance liver soft tissue material
            if (_digestiveGo != null && liverMat != null)
            {
                foreach (var r in _digestiveGo.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = liverMat;
                }
            }
        }
    }
}
