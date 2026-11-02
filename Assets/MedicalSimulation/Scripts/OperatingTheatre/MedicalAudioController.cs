using System.Collections;
using UnityEngine;

namespace MedicalSimulation.OperatingTheatre
{
    /// <summary>
    /// Immersive audio management system for the Operating Theatre.
    /// Provides spatial 3D audio for ventilation ambience, monitor ECG pulse beeps,
    /// clinical alarm chimes, suction units, and surgical instrument handling.
    /// </summary>
    public class MedicalAudioController : MonoBehaviour
    {
        public static MedicalAudioController Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip ecgBeepClip;
        [SerializeField] private AudioClip monitorAlarmClip;
        [SerializeField] private AudioClip ventilationAmbienceClip;
        [SerializeField] private AudioClip surgicalSuctionClip;
        [SerializeField] private AudioClip instrumentClinkClip;

        [Header("Sources")]
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource ecgMonitorSource;
        [SerializeField] private AudioSource alarmSource;
        [SerializeField] private AudioSource suctionSource;
        [SerializeField] private AudioSource effectsSource;

        [Header("Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float monitorVolume = 0.70f;
        [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.35f;

        private ClinicalMonitorController _monitor;

        void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureAudioSources();
            LoadDefaultAudioClips();
        }

        void Start()
        {
            _monitor = FindFirstObjectByType<ClinicalMonitorController>();
            if (_monitor != null)
            {
                _monitor.OnHeartbeatPulse += HandleHeartbeatPulse;
                _monitor.OnAlarmStateChanged += HandleAlarmStateChanged;
            }

            StartAmbience();
        }

        void OnDestroy()
        {
            if (_monitor != null)
            {
                _monitor.OnHeartbeatPulse -= HandleHeartbeatPulse;
                _monitor.OnAlarmStateChanged -= HandleAlarmStateChanged;
            }
        }

        private void EnsureAudioSources()
        {
            if (ambienceSource == null) ambienceSource = CreateSource("AudioSource_Ambience", true, ambienceVolume);
            if (ecgMonitorSource == null) ecgMonitorSource = CreateSource("AudioSource_ECG_Pulse", false, monitorVolume);
            if (alarmSource == null) alarmSource = CreateSource("AudioSource_Monitor_Alarm", true, monitorVolume * 0.9f);
            if (suctionSource == null) suctionSource = CreateSource("AudioSource_Suction", true, 0.25f);
            if (effectsSource == null) effectsSource = CreateSource("AudioSource_Effects", false, 0.75f);
        }

        private AudioSource CreateSource(string name, bool loop, float vol)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.loop = loop;
            src.volume = vol * masterVolume;
            src.spatialBlend = 0.4f; // Semi-spatial
            return src;
        }

        private void LoadDefaultAudioClips()
        {
            if (ecgBeepClip == null) ecgBeepClip = Resources.Load<AudioClip>("MedicalAudio/monitor_ecg_beep");
            if (ventilationAmbienceClip == null) ventilationAmbienceClip = Resources.Load<AudioClip>("MedicalAudio/or_ventilation_ambience") ?? Resources.Load<AudioClip>("MedicalAudio/lab_ambience");
            if (instrumentClinkClip == null) instrumentClinkClip = Resources.Load<AudioClip>("MedicalAudio/instrument_place");

            // Also check direct asset paths if in Editor
            #if UNITY_EDITOR
            if (ecgBeepClip == null) ecgBeepClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MedicalSimulation/Audio/OR/monitor_ecg_beep.wav");
            if (monitorAlarmClip == null) monitorAlarmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MedicalSimulation/Audio/OR/monitor_alarm_alert.wav");
            if (ventilationAmbienceClip == null) ventilationAmbienceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MedicalSimulation/Audio/OR/or_ventilation_ambience.wav");
            if (surgicalSuctionClip == null) surgicalSuctionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MedicalSimulation/Audio/OR/surgical_suction_loop.wav");
            if (instrumentClinkClip == null) instrumentClinkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/MedicalSimulation/Audio/OR/surgical_instrument_clink.wav");
            #endif
        }

        public void StartAmbience()
        {
            if (ambienceSource != null && ventilationAmbienceClip != null)
            {
                ambienceSource.clip = ventilationAmbienceClip;
                ambienceSource.Play();
            }
        }

        private void HandleHeartbeatPulse()
        {
            if (ecgMonitorSource != null && ecgBeepClip != null)
            {
                ecgMonitorSource.PlayOneShot(ecgBeepClip, monitorVolume * masterVolume);
            }
        }

        private void HandleAlarmStateChanged(bool active)
        {
            if (alarmSource == null) return;

            if (active && monitorAlarmClip != null)
            {
                alarmSource.clip = monitorAlarmClip;
                if (!alarmSource.isPlaying) alarmSource.Play();
            }
            else
            {
                alarmSource.Stop();
            }
        }

        public void PlayInstrumentSound()
        {
            if (effectsSource != null && instrumentClinkClip != null)
            {
                effectsSource.PlayOneShot(instrumentClinkClip, 0.6f * masterVolume);
            }
        }

        public void SetSuctionActive(bool active)
        {
            if (suctionSource == null) return;
            if (active && surgicalSuctionClip != null)
            {
                suctionSource.clip = surgicalSuctionClip;
                if (!suctionSource.isPlaying) suctionSource.Play();
            }
            else
            {
                suctionSource.Stop();
            }
        }
    }
}
