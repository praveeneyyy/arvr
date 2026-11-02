using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MedicalSimulation.Core;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Master Application Startup & Bootstrap Orchestrator.
    /// Scene 00_Bootstrap entrypoint. Initializes global singletons,
    /// configures frame pacing, and launches into 01_MainMenu.
    /// </summary>
    public class BootstrapController : MonoBehaviour
    {
        [Header("Startup Configuration")]
        [SerializeField] private string targetMainMenuScene = "01_MainMenu";
        [SerializeField] private float minimumSplashHoldTime = 0.5f;

        void Awake()
        {
            // Frame pacing & mobile/desktop sleep settings
            Application.targetFrameRate = 90;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Ensure MedicalAppManager singleton
            if (MedicalAppManager.Instance == null)
            {
                var appGo = new GameObject("MedicalAppManager");
                appGo.AddComponent<MedicalAppManager>();
            }
        }

        IEnumerator Start()
        {
            Debug.Log("[BootstrapController] Initializing Project Atlas runtime services...");
            yield return new WaitForSeconds(minimumSplashHoldTime);

            // Check if 01_MainMenu exists in build settings, fallback to 1 if needed
            string sceneToLoad = targetMainMenuScene;
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (sName.Equals(targetMainMenuScene, System.StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                sceneToLoad = "01_MainMenu";
            }

            Debug.Log($"[BootstrapController] Launching into primary atlas command centre: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
