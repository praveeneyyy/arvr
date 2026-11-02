using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Master 3D Presentation Studio Environment for Project Atlas.
    /// Manages the circular medical pedestal, 3-point clinical lighting rig,
    /// contact shadow disc, and cinematic reveal illumination fades.
    /// </summary>
    public class ModelPresentationController : MonoBehaviour
    {
        [Header("Studio Lighting Rig")]
        [SerializeField] private Light keyLight;
        [SerializeField] private Light rimLight;
        [SerializeField] private Light fillLight;

        [Header("Pedestal & Ground Rig")]
        [SerializeField] private GameObject pedestalStage;
        [SerializeField] private GameObject contactShadowPlane;

        [Header("Target Intensities")]
        public float keyIntensity = 1.35f;
        public float rimIntensity = 1.65f;
        public float fillIntensity = 0.55f;

        private float _currentReveal = 1.0f;

        void Awake()
        {
            EnsureStudioRig();
        }

        public void EnsureStudioRig()
        {
            // Create Key Light if absent
            if (keyLight == null)
            {
                var kGo = new GameObject("Studio_KeyLight");
                kGo.transform.SetParent(transform);
                keyLight = kGo.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.96f, 0.98f, 1.0f); // 5000K crisp white
                keyLight.intensity = keyIntensity;
                kGo.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
            }

            // Create Rim Light (Cyan / Ice-Blue highlight)
            if (rimLight == null)
            {
                var rGo = new GameObject("Studio_RimLight");
                rGo.transform.SetParent(transform);
                rimLight = rGo.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = new Color(0f, 0.85f, 1.0f); // Restrained medical cyan
                rimLight.intensity = rimIntensity;
                rGo.transform.rotation = Quaternion.Euler(-25f, 145f, 0f);
            }

            // Create Fill Light
            if (fillLight == null)
            {
                var fGo = new GameObject("Studio_FillLight");
                fGo.transform.SetParent(transform);
                fillLight = fGo.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = new Color(0.72f, 0.78f, 0.88f); // Soft ambient fill
                fillLight.intensity = fillIntensity;
                fGo.transform.rotation = Quaternion.Euler(20f, 60f, 0f);
            }

            // Create Circular Clinical Pedestal
            if (pedestalStage == null)
            {
                pedestalStage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pedestalStage.name = "Clinical_Pedestal_Stage";
                pedestalStage.transform.SetParent(transform);
                pedestalStage.transform.position = new Vector3(0f, -0.05f, 0f);
                pedestalStage.transform.localScale = new Vector3(3.4f, 0.04f, 3.4f);

                var coll = pedestalStage.GetComponent<Collider>();
                if (coll != null) Destroy(coll);

                var rend = pedestalStage.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.04f, 0.05f, 0.07f, 1f); // Deep graphite
                mat.SetFloat("_Smoothness", 0.82f); // Reflective clinic finish
                mat.SetFloat("_Metallic", 0.15f);
                rend.sharedMaterial = mat;
            }

            // Create Contact Shadow Disc
            if (contactShadowPlane == null)
            {
                contactShadowPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                contactShadowPlane.name = "Pedestal_ContactShadow";
                contactShadowPlane.transform.SetParent(transform);
                contactShadowPlane.transform.position = new Vector3(0f, -0.029f, 0f);
                contactShadowPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                contactShadowPlane.transform.localScale = new Vector3(3.1f, 3.1f, 1f);

                var coll = contactShadowPlane.GetComponent<Collider>();
                if (coll != null) Destroy(coll);

                var rend = contactShadowPlane.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.015f, 0.02f, 0.03f, 0.92f);
                rend.sharedMaterial = mat;
            }
        }

        public void SetRevealProgress(float progress)
        {
            _currentReveal = Mathf.Clamp01(progress);

            if (keyLight != null) keyLight.intensity = keyIntensity * _currentReveal;
            if (rimLight != null) rimLight.intensity = rimIntensity * _currentReveal;
            if (fillLight != null) fillLight.intensity = fillIntensity * _currentReveal;
        }
    }
}
