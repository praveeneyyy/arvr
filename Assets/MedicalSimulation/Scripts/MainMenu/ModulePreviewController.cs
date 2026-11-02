using System.Collections.Generic;
using UnityEngine;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Master 3D Viewport Preview Controller.
    /// Loads, standardizes, caches, and animates the 6 medical 3D assets on the clinical presentation stage.
    /// Features unique idle animations: 72 BPM cardiac pulsing, subtle breathing, and precision turntable rotation.
    /// </summary>
    public class ModulePreviewController : MonoBehaviour
    {
        [Header("Stage Root")]
        [SerializeField] private Transform modelStageRoot;
        [SerializeField] private ModelRotationController rotationController;

        private readonly Dictionary<int, GameObject> _cachedModels = new Dictionary<int, GameObject>();
        private int _currentActiveIndex = -1;

        // Cardiology pulsing animation state
        private float _heartPulseTimer = 0f;
        private Vector3 _heartBaseScale = Vector3.one;

        // Torso subtle breathing state
        private float _breathingTimer = 0f;
        private Vector3 _torsoBaseScale = Vector3.one;

        void Awake()
        {
            if (modelStageRoot == null) modelStageRoot = transform;
            if (rotationController == null) rotationController = FindFirstObjectByType<ModelRotationController>();
        }

        public void DisplayModulePreview(int moduleIndex)
        {
            var meta = ModuleAssetRegistry.GetModuleByIndex(moduleIndex);
            if (meta == null) return;

            // Hide previous active model
            if (_cachedModels.TryGetValue(_currentActiveIndex, out var prevModel) && prevModel != null)
            {
                prevModel.SetActive(false);
            }

            _currentActiveIndex = moduleIndex;

            // Load or fetch cached model
            if (!_cachedModels.TryGetValue(moduleIndex, out var activeModel) || activeModel == null)
            {
                activeModel = LoadAndNormalizeModel(meta);
                if (activeModel != null)
                {
                    _cachedModels[moduleIndex] = activeModel;
                }
            }

            if (activeModel != null)
            {
                activeModel.SetActive(true);

                // Re-center rotation and set camera framing
                if (rotationController != null)
                {
                    rotationController.SetTarget(activeModel.transform);
                    rotationController.SetCameraFraming(meta.previewCameraOffset, meta.previewLookTarget, meta.previewCameraFOV);
                }
            }
        }

        private GameObject LoadAndNormalizeModel(ModuleMetadata meta)
        {
            // 1. Try loading from Resources/Models
            GameObject prefab = Resources.Load<GameObject>($"Models/{meta.modelAssetName}");

            // 2. Fallback to direct asset database paths
            if (prefab == null)
            {
                string[] paths = new string[] {
                    $"MedicalSimulation/Art/Anatomy/{meta.modelAssetName}",
                    $"MedicalSimulation/Art/Organs/{meta.modelAssetName}",
                    $"MedicalSimulation/Art/OperatingTheatre/{meta.modelAssetName}",
                    $"MedicalSimulation/Resources/Models/{meta.modelAssetName}",
                    meta.modelAssetName
                };
                foreach (var p in paths)
                {
                    prefab = Resources.Load<GameObject>(p);
                    if (prefab != null) break;
                }
            }

            GameObject instance = null;
            if (prefab != null)
            {
                instance = Instantiate(prefab, modelStageRoot);
                instance.name = $"Preview_{meta.moduleNumber}_{meta.modelAssetName}";
            }
            else
            {
                // Honest fallback preview if FBX is not placed in Resources
                instance = CreateHonestAnatomicalFallback(meta);
            }

            // Standardize scale, bounds, and pedestal placement
            FitModelToStage(instance, meta.moduleIndex);
            ApplyClinicalMaterials(instance, meta);

            return instance;
        }

        private void FitModelToStage(GameObject go, int moduleIndex)
        {
            if (go == null) return;

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // Per-module specific scaling and centering
            switch (moduleIndex)
            {
                case 0: // Full Anatomy
                    NormalizeBounds(go, 1.80f, new Vector3(0.08f, 0.05f, 0f));
                    _torsoBaseScale = go.transform.localScale;
                    break;
                case 1: // Cardiology
                    NormalizeBounds(go, 1.25f, new Vector3(0f, 0.15f, 0f));
                    _heartBaseScale = go.transform.localScale;
                    break;
                case 2: // Dental
                    NormalizeBounds(go, 1.20f, new Vector3(0f, 0.18f, 0f));
                    break;
                case 3: // Hepatic
                    NormalizeBounds(go, 1.30f, new Vector3(0f, 0.15f, 0f));
                    break;
                case 4: // Surgical Instruments
                    NormalizeBounds(go, 1.15f, new Vector3(0f, 0.15f, 0f));
                    break;
                case 5: // Operating Theatre
                    NormalizeBounds(go, 2.75f, new Vector3(0f, 0.02f, 0f));
                    break;
            }
        }

        private void NormalizeBounds(GameObject go, float targetHeight, Vector3 offset)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            float currentHeight = b.size.y;
            if (currentHeight > 0.001f)
            {
                float scaleFactor = targetHeight / currentHeight;
                go.transform.localScale = Vector3.one * scaleFactor;
            }

            // Re-calculate after scale to adjust base position to sit on pedestal
            Bounds bScaled = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bScaled.Encapsulate(renderers[i].bounds);
            }

            float bottomY = bScaled.min.y;
            go.transform.position = new Vector3(offset.x, offset.y - bottomY, offset.z);
        }

        private void ApplyClinicalMaterials(GameObject go, ModuleMetadata meta)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null || mats[m].name.Contains("Default-Material"))
                    {
                        var clinicalMat = new Material(shader);
                        clinicalMat.name = $"ClinicalMat_{meta.displayName}";
                        clinicalMat.color = meta.accentColor * 0.85f + new Color(0.15f, 0.15f, 0.15f, 1f);
                        clinicalMat.SetFloat("_Smoothness", 0.72f);
                        clinicalMat.SetFloat("_Metallic", 0.08f);
                        mats[m] = clinicalMat;
                    }
                }
                r.sharedMaterials = mats;
            }
        }

        private GameObject CreateHonestAnatomicalFallback(ModuleMetadata meta)
        {
            var fallback = new GameObject($"Fallback_{meta.displayName}");
            fallback.transform.SetParent(modelStageRoot);

            var prim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prim.transform.SetParent(fallback.transform);
            prim.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
            prim.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            var rend = prim.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = meta.accentColor;
            rend.sharedMaterial = mat;

            return fallback;
        }

        void Update()
        {
            // Module 02 Cardiology: Dynamic 72 BPM systolic/diastolic pulsing deformation
            if (_currentActiveIndex == 1 && _cachedModels.TryGetValue(1, out var heart) && heart != null && heart.activeSelf)
            {
                _heartPulseTimer += Time.deltaTime * 1.2f; // 72 BPM
                float wave = Mathf.Sin(_heartPulseTimer * Mathf.PI * 2f);
                float pulse = 1.0f + Mathf.Max(0f, wave) * 0.045f;
                heart.transform.localScale = _heartBaseScale * pulse;
            }

            // Module 01 Full Anatomy: Subtle organic respiration cycle
            if (_currentActiveIndex == 0 && _cachedModels.TryGetValue(0, out var torso) && torso != null && torso.activeSelf)
            {
                _breathingTimer += Time.deltaTime * 0.35f; // 14 breaths/min
                float breath = Mathf.Sin(_breathingTimer * Mathf.PI * 2f);
                float breathFactor = 1.0f + breath * 0.012f;
                torso.transform.localScale = new Vector3(_torsoBaseScale.x * breathFactor, _torsoBaseScale.y, _torsoBaseScale.z * breathFactor);
            }
        }
    }
}
