using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drop-in replacement for the old GoToScene script found in Assets/Scenes/.
/// Works with both the existing scene paths and MedicalAppManager.
/// </summary>
public class GoToScene : MonoBehaviour
{
    [Tooltip("Scene name (e.g. 'heart') or build index as a string (e.g. '2')")]
    public string sceneName;

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GoToScene] sceneName is not set.");
            return;
        }

        // Try by name via MedicalAppManager first
        if (MedicalSimulation.Core.MedicalAppManager.Instance != null)
        {
            MedicalSimulation.Core.MedicalAppManager.Instance.LoadSceneByName(sceneName);
            return;
        }

        // Fallback: direct SceneManager load
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int index)
    {
        if (MedicalSimulation.Core.MedicalAppManager.Instance != null)
            MedicalSimulation.Core.MedicalAppManager.Instance.LoadScene(index);
        else
            SceneManager.LoadScene(index);
    }
}
