using UnityEngine;
using MedicalSimulation.Core;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Per-scene initializer. Ensures MedicalAppManager exists and applies
    /// the current quality mode. Attach to a persistent GameObject in each scene.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Scene identity")]
        [SerializeField] private string sceneName = "Scene";

        [Header("Auto-apply quality on scene load")]
        [SerializeField] private bool applyQualityOnStart = true;

        void Awake()
        {
            // Ensure the app manager exists even when starting from this scene in the Editor
            if (MedicalAppManager.Instance == null)
            {
                var go = new GameObject("MedicalAppManager");
                go.AddComponent<MedicalAppManager>();
            }
        }

        void Start()
        {
            if (applyQualityOnStart && MedicalAppManager.Instance != null)
                MedicalQualitySettings.Apply(MedicalAppManager.Instance.CurrentQualityMode);

            Debug.Log($"[SceneBootstrap] Scene '{sceneName}' ready.");
        }

        public void GoToScene(string name) => MedicalAppManager.Instance?.LoadSceneByName(name);
        public void GoToMainMenu()         => MedicalAppManager.Instance?.GoToMainMenu();
    }
}
