using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Smooth Cinematic Scene Transition Controller.
    /// Provides dark clinical cross-fades, asynchronous scene loading, and safe routing fallbacks.
    /// </summary>
    public class SceneTransitionController : MonoBehaviour
    {
        public static SceneTransitionController Instance { get; private set; }

        [Header("Transition Overlay")]
        [SerializeField] private CanvasGroup faderCanvasGroup;
        [SerializeField] private Slider loadProgressSlider;
        [SerializeField] private TextMeshProUGUI transitionStatusText;

        public float fadeDuration = 0.55f;
        private bool _isTransitioning = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public void TransitionToScene(string sceneName, Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionRoutine(sceneName, onComplete));
        }

        private IEnumerator TransitionRoutine(string sceneName, Action onComplete)
        {
            _isTransitioning = true;

            if (faderCanvasGroup != null)
            {
                faderCanvasGroup.gameObject.SetActive(true);
                faderCanvasGroup.blocksRaycasts = true;
            }

            if (transitionStatusText != null)
            {
                transitionStatusText.text = $"LOADING CLINICAL SUITE: {sceneName.ToUpper()}...";
            }

            // Fade Out to Black
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (faderCanvasGroup != null)
                {
                    faderCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                }
                yield return null;
            }

            // Verify scene in build settings
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (sName.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    sceneExists = true;
                    break;
                }
            }

            string targetScene = sceneExists ? sceneName : "04_AnatomyLab";

            // Load Scene Async
            var asyncOp = SceneManager.LoadSceneAsync(targetScene);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
                while (asyncOp.progress < 0.9f)
                {
                    if (loadProgressSlider != null)
                    {
                        loadProgressSlider.value = asyncOp.progress;
                    }
                    yield return null;
                }

                if (loadProgressSlider != null) loadProgressSlider.value = 1.0f;
                yield return new WaitForSeconds(0.15f);
                asyncOp.allowSceneActivation = true;
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }

            onComplete?.Invoke();
            _isTransitioning = false;
        }
    }
}
