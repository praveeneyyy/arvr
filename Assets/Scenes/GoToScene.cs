using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToScene : MonoBehaviour
{
    // Load scene by name
    public void GoToSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Optional: Quit the game
    public void QuitApp()
    {
        Debug.Log("Quit App!");
        Application.Quit();
    }
}
