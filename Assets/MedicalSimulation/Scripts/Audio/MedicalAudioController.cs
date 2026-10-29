using UnityEngine;
using UnityEngine.Audio;

namespace MedicalSimulation.Audio
{
    /// <summary>
    /// Centralized medical audio controller.
    /// Manages heartbeat, respiration, dental procedure, UI and environment audio.
    /// 
    /// All audio clips must be assigned in the Inspector or loaded from Resources.
    /// No proprietary audio middleware required — uses Unity's built-in AudioSource.
    /// 
    /// SOURCE ATTRIBUTION:
    ///   Assign appropriately licensed clips in the Inspector.
    ///   Recommended free sources: freesound.org (CC0 / CC-BY licenses)
    ///   Record license info for each clip in the project documentation.
    /// </summary>
    public class MedicalAudioController : MonoBehaviour
    {
        public static MedicalAudioController Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mixer;

        [Header("AudioSources")]
        [SerializeField] private AudioSource physiologicalSource;   // looping heartbeat / respiration
        [SerializeField] private AudioSource respirationSource;
        [SerializeField] private AudioSource procedureSource;       // dental / surgical SFX
        [SerializeField] private AudioSource uiSource;              // UI clicks and feedback
        [SerializeField] private AudioSource ambientSource;         // lab ambience

        [Header("Heartbeat Clips")]
        [Tooltip("Single heart-beat sound. Must be appropriately licensed.")]
        [SerializeField] private AudioClip heartbeatClip;

        [Header("Respiration Clips")]
        [SerializeField] private AudioClip inhaleClip;
        [SerializeField] private AudioClip exhaleClip;

        [Header("Dental Procedure Clips")]
        [SerializeField] private AudioClip drillClip;
        [SerializeField] private AudioClip suctionClip;
        [SerializeField] private AudioClip scalerClip;
        [SerializeField] private AudioClip instrumentPlaceClip;
        [SerializeField] private AudioClip procedureCompleteClip;

        [Header("UI Clips")]
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip confirmClip;
        [SerializeField] private AudioClip errorClip;

        [Header("Environment")]
        [SerializeField] private AudioClip labAmbienceClip;

        // ── Heartbeat sync state ──────────────────────────────────────────────
        private Cardiovascular.HeartAnimationController  _heartController;
        private Respiratory.RespiratoryAnimationController _respController;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
        }

        void Start()
        {
            // Auto-find controllers in scene
            _heartController = FindFirstObjectByType<Cardiovascular.HeartAnimationController>();
            _respController  = FindFirstObjectByType<Respiratory.RespiratoryAnimationController>();

            if (_heartController != null)
                _heartController.OnHeartbeat += PlayHeartbeat;

            if (_respController != null)
            {
                _respController.OnInhale += PlayInhale;
                _respController.OnExhale += PlayExhale;
            }

            StartLabAmbience();
        }

        void OnDestroy()
        {
            if (_heartController != null) _heartController.OnHeartbeat -= PlayHeartbeat;
            if (_respController  != null)
            {
                _respController.OnInhale -= PlayInhale;
                _respController.OnExhale -= PlayExhale;
            }
        }

        // ── Heartbeat ─────────────────────────────────────────────────────────

        public void PlayHeartbeat()
        {
            if (heartbeatClip == null || physiologicalSource == null) return;
            // Play one-shot to avoid accumulating overlapping sources
            physiologicalSource.PlayOneShot(heartbeatClip, 0.8f);
        }

        // ── Respiration ───────────────────────────────────────────────────────

        public void PlayInhale()
        {
            if (inhaleClip != null && respirationSource != null)
                respirationSource.PlayOneShot(inhaleClip, 0.5f);
        }

        public void PlayExhale()
        {
            if (exhaleClip != null && respirationSource != null)
                respirationSource.PlayOneShot(exhaleClip, 0.4f);
        }

        // ── Dental procedures ─────────────────────────────────────────────────

        public void PlayDrill()              => PlayProcedure(drillClip,           0.9f);
        public void StopDrill()              { if (procedureSource.clip == drillClip) procedureSource.Stop(); }
        public void PlaySuction()            => PlayProcedure(suctionClip,         0.7f);
        public void PlayScaler()             => PlayProcedure(scalerClip,          0.8f);
        public void PlayInstrumentPlace()    => PlayProcedure(instrumentPlaceClip, 0.6f);
        public void PlayProcedureComplete()  => PlayProcedure(procedureCompleteClip, 1f);

        private void PlayProcedure(AudioClip clip, float vol)
        {
            if (clip == null || procedureSource == null) return;
            procedureSource.PlayOneShot(clip, vol);
        }

        // ── UI audio ─────────────────────────────────────────────────────────

        public void PlaySelect()   => PlayUI(selectClip,      0.5f);
        public void PlayClick()    => PlayUI(buttonClickClip, 0.6f);
        public void PlayConfirm()  => PlayUI(confirmClip,     0.7f);
        public void PlayError()    => PlayUI(errorClip,       0.7f);

        private void PlayUI(AudioClip clip, float vol)
        {
            if (clip == null || uiSource == null) return;
            uiSource.PlayOneShot(clip, vol);
        }

        // ── Ambience ─────────────────────────────────────────────────────────

        public void StartLabAmbience()
        {
            if (labAmbienceClip == null || ambientSource == null) return;
            ambientSource.clip   = labAmbienceClip;
            ambientSource.loop   = true;
            ambientSource.volume = 0.15f;
            ambientSource.Play();
        }

        public void StopLabAmbience() => ambientSource?.Stop();

        // ── Volume controls ───────────────────────────────────────────────────

        public void SetPhysiologicalVolume(float v) => SetMixerGroup("PhysiologicalVolume", v);
        public void SetProcedureVolume(float v)      => SetMixerGroup("ProcedureVolume",     v);
        public void SetUIVolume(float v)             => SetMixerGroup("UIVolume",            v);
        public void SetAmbientVolume(float v)        => SetMixerGroup("AmbientVolume",       v);

        private void SetMixerGroup(string param, float linearVolume)
        {
            if (mixer == null) return;
            float db = linearVolume > 0.0001f ? Mathf.Log10(linearVolume) * 20f : -80f;
            mixer.SetFloat(param, db);
        }

        // ── Auto-create AudioSources if missing ───────────────────────────────

        private void EnsureAudioSources()
        {
            if (physiologicalSource == null)
                physiologicalSource = CreateSource("Physiological", spatialize: false);
            if (respirationSource == null)
                respirationSource   = CreateSource("Respiration",   spatialize: false);
            if (procedureSource == null)
                procedureSource     = CreateSource("Procedure",     spatialize: false);
            if (uiSource == null)
                uiSource            = CreateSource("UI",            spatialize: false);
            if (ambientSource == null)
                ambientSource       = CreateSource("Ambient",       spatialize: false);
        }

        private AudioSource CreateSource(string sourceName, bool spatialize)
        {
            var go = new GameObject($"AudioSource_{sourceName}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = spatialize ? 1f : 0f;
            src.playOnAwake  = false;
            return src;
        }
    }
}
