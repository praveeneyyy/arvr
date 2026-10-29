using UnityEngine;
using MedicalSimulation.Cardiovascular;
using MedicalSimulation.Respiratory;
using MedicalSimulation.Audio;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Bootstraps the heart scene (heart.unity / heart 1.unity / heart 2.unity).
    /// Finds or creates the necessary controllers, connects audio, and starts animations.
    /// Attach to a persistent root GameObject in each heart/bloodflow scene.
    /// </summary>
    public class HeartSceneBootstrap : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool autoStartHeart      = true;
        [SerializeField] private bool autoStartBloodflow  = true;
        [SerializeField] private float initialHeartRate   = 72f;

        [Header("Back Navigation")]
        [SerializeField] private UnityEngine.UI.Button backButton;

        private HeartAnimationController  _heart;
        private BloodFlowController       _bloodFlow;
        private MedicalAudioController    _audio;

        void Start()
        {
            // Ensure AppManager exists
            if (MedicalAppManager.Instance == null)
                new GameObject("MedicalAppManager").AddComponent<MedicalAppManager>();

            // Find or auto-add controllers
            _heart     = FindFirstObjectByType<HeartAnimationController>();
            _bloodFlow = FindFirstObjectByType<BloodFlowController>();
            _audio     = FindFirstObjectByType<MedicalAudioController>();

            if (_heart == null)
            {
                // Attach to the heart GLB root if present
                var heartGo = GameObject.Find("adult_heart") ??
                              GameObject.Find("Heart")       ??
                              new GameObject("HeartController");
                _heart = heartGo.AddComponent<HeartAnimationController>();
                Debug.Log("[HeartSceneBootstrap] Added HeartAnimationController to: " + heartGo.name);
            }

            _heart.SetHeartRate(initialHeartRate);
            if (autoStartHeart) _heart.Play();

            if (_bloodFlow == null)
            {
                // Look for any BloodFlowPath in the scene and wrap it
                var path = FindFirstObjectByType<BloodVesselPath>();
                if (path != null)
                {
                    _bloodFlow = path.gameObject.AddComponent<BloodFlowController>();
                }
            }
            if (autoStartBloodflow) _bloodFlow?.Play();

            // Connect audio
            if (_audio != null)
            {
                _heart.OnHeartbeat += _audio.PlayHeartbeat;
                Debug.Log("[HeartSceneBootstrap] Audio heartbeat sync connected.");
            }

            // Back button
            backButton?.onClick.AddListener(MedicalAppManager.Instance.GoToMainMenu);

            MedicalAppManager.Instance?.RecordModuleCompleted();
        }

        void OnDestroy()
        {
            if (_heart != null && _audio != null)
                _heart.OnHeartbeat -= _audio.PlayHeartbeat;
        }
    }
}
