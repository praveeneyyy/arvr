using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MedicalSimulation.OperatingTheatre
{
    /// <summary>
    /// Clinical patient vital signs monitoring system for the Operating Theatre.
    /// Simulates real-time ECG waveforms (Lead II P-Q-R-S-T), SpO2 photoplethysmogram,
    /// invasive arterial blood pressure (ABP), respiratory rate (RR), and core body temperature.
    /// Provides synchronized heartbeat pulse events for spatial audio beeps and physiological crisis states.
    /// </summary>
    public class ClinicalMonitorController : MonoBehaviour
    {
        public static ClinicalMonitorController Instance { get; private set; }

        public enum PhysiologicalState
        {
            NormalSinusRhythm,
            Tachycardia,
            Bradycardia,
            HypotensionCrisis,
            Hypoxemia,
            VentricularFibrillation,
            Asystole
        }

        [Header("Vital Signs")]
        [Range(30, 220)] [SerializeField] private float heartRate = 72f;
        [SerializeField] private int systolicBP = 120;
        [SerializeField] private int diastolicBP = 80;
        [Range(60, 100)] [SerializeField] private float oxygenSaturation = 99f;
        [Range(6, 40)]   [SerializeField] private float respiratoryRate = 14f;
        [SerializeField] private float coreTemperature = 36.8f;
        [SerializeField] private float endTidalCO2 = 38f;

        [Header("State")]
        [SerializeField] private PhysiologicalState currentState = PhysiologicalState.NormalSinusRhythm;

        [Header("Waveform Display")]
        [SerializeField] private RawImage ecgDisplayImage;
        [SerializeField] private int textureWidth = 320;
        [SerializeField] private int textureHeight = 120;
        [SerializeField] private Color ecgGreenColor = new Color(0.12f, 1.0f, 0.32f, 1.0f);
        [SerializeField] private Color gridColor = new Color(0.04f, 0.18f, 0.10f, 0.85f);
        [SerializeField] private Color bgColor = new Color(0.01f, 0.04f, 0.03f, 1.0f);

        [Header("Text Displays (Optional UI references)")]
        [SerializeField] private TextMeshProUGUI hrText;
        [SerializeField] private TextMeshProUGUI bpText;
        [SerializeField] private TextMeshProUGUI spo2Text;
        [SerializeField] private TextMeshProUGUI rrText;
        [SerializeField] private TextMeshProUGUI tempText;
        [SerializeField] private TextMeshProUGUI alarmStatusText;

        // Waveform runtime buffer
        private Texture2D _ecgTexture;
        private Color[] _pixelBuffer;
        private float _waveSweepPos = 0f;
        private float _lastWaveY = 0.5f;
        private float _ecgPhase = 0f;
        private float _pulseTimer = 0f;
        private bool _alarmActive = false;

        // Events
        public event Action OnHeartbeatPulse;
        public event Action<bool> OnAlarmStateChanged;
        public event Action<PhysiologicalState> OnStateChanged;

        public float HeartRate => heartRate;
        public int SystolicBP => systolicBP;
        public int DiastolicBP => diastolicBP;
        public float SpO2 => oxygenSaturation;
        public float RespiratoryRate => respiratoryRate;
        public float Temperature => coreTemperature;
        public PhysiologicalState CurrentState => currentState;
        public bool IsAlarmActive => _alarmActive;

        void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeWaveformTexture();
        }

        void Start()
        {
            ApplyPhysiologicalPreset(currentState);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateCardiacCycle(dt);
            UpdateWaveform(dt);
            UpdateUIReadouts();
        }

        private void InitializeWaveformTexture()
        {
            _ecgTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            _ecgTexture.filterMode = FilterMode.Bilinear;
            _pixelBuffer = new Color[textureWidth * textureHeight];

            // Clear to background with subtle green medical grid
            ClearTextureBuffer();
            _ecgTexture.SetPixels(_pixelBuffer);
            _ecgTexture.Apply();

            if (ecgDisplayImage != null)
            {
                ecgDisplayImage.texture = _ecgTexture;
            }
        }

        private void ClearTextureBuffer()
        {
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    bool isGrid = (x % 20 == 0) || (y % 20 == 0);
                    _pixelBuffer[y * textureWidth + x] = isGrid ? gridColor : bgColor;
                }
            }
        }

        private void UpdateCardiacCycle(float dt)
        {
            if (currentState == PhysiologicalState.Asystole)
            {
                _alarmActive = true;
                return;
            }

            float cardiacPeriod = 60f / Mathf.Max(20f, heartRate);
            _ecgPhase += (dt / cardiacPeriod);

            _pulseTimer += dt;
            if (_pulseTimer >= cardiacPeriod)
            {
                _pulseTimer -= cardiacPeriod;
                _ecgPhase = 0f;
                OnHeartbeatPulse?.Invoke();
            }

            // Check clinical alarms
            bool shouldAlarm = (heartRate > 130f || heartRate < 45f || oxygenSaturation < 90f || systolicBP < 85 || currentState == PhysiologicalState.VentricularFibrillation);
            if (shouldAlarm != _alarmActive)
            {
                _alarmActive = shouldAlarm;
                OnAlarmStateChanged?.Invoke(_alarmActive);
            }
        }

        private void UpdateWaveform(float dt)
        {
            if (_ecgTexture == null) return;

            // Sweep speed: 1.5 screen widths per second
            float sweepSpeed = textureWidth * 0.75f;
            int prevX = Mathf.FloorToInt(_waveSweepPos);
            _waveSweepPos = (_waveSweepPos + sweepSpeed * dt) % textureWidth;
            int currX = Mathf.FloorToInt(_waveSweepPos);

            // Compute normalized ECG voltage Y at this phase (0..1)
            float normVoltage = CalculateECGVoltage(_ecgPhase);
            float targetY = 0.5f + normVoltage * 0.42f;

            // Draw line on texture between prevX and currX
            DrawWaveSegment(prevX, _lastWaveY, currX, targetY);
            _lastWaveY = targetY;

            _ecgTexture.SetPixels(_pixelBuffer);
            _ecgTexture.Apply();
        }

        private float CalculateECGVoltage(float phase)
        {
            if (currentState == PhysiologicalState.Asystole)
            {
                return UnityEngine.Random.Range(-0.01f, 0.01f); // Flatline noise
            }

            if (currentState == PhysiologicalState.VentricularFibrillation)
            {
                return Mathf.Sin(Time.time * 24f) * 0.35f + Mathf.Sin(Time.time * 41f) * 0.25f + UnityEngine.Random.Range(-0.08f, 0.08f);
            }

            // Lead II Normal P-Q-R-S-T morphology
            // P wave: 0.12 - 0.20
            // Q wave: 0.26 - 0.28
            // R peak: 0.30 - 0.34
            // S wave: 0.36 - 0.40
            // T wave: 0.55 - 0.72
            if (phase >= 0.12f && phase <= 0.20f)
            {
                float pT = (phase - 0.12f) / 0.08f;
                return Mathf.Sin(pT * Mathf.PI) * 0.15f;
            }
            if (phase > 0.26f && phase <= 0.29f)
            {
                float qT = (phase - 0.26f) / 0.03f;
                return -Mathf.Sin(qT * Mathf.PI) * 0.12f;
            }
            if (phase > 0.29f && phase <= 0.35f)
            {
                float rT = (phase - 0.29f) / 0.06f;
                return Mathf.Sin(rT * Mathf.PI) * 0.95f; // Sharp R peak
            }
            if (phase > 0.35f && phase <= 0.40f)
            {
                float sT = (phase - 0.35f) / 0.05f;
                return -Mathf.Sin(sT * Mathf.PI) * 0.25f; // S dip
            }
            if (phase >= 0.52f && phase <= 0.72f)
            {
                float tT = (phase - 0.52f) / 0.20f;
                return Mathf.Sin(tT * Mathf.PI) * 0.28f; // Repolarization T wave
            }

            return UnityEngine.Random.Range(-0.015f, 0.015f); // Baseline isoelectric noise
        }

        private void DrawWaveSegment(int x0, float y0, int x1, float y1)
        {
            // Clear ahead of sweep (eraser bar 8 pixels wide)
            for (int clearX = x1; clearX < x1 + 12; clearX++)
            {
                int wrapX = clearX % textureWidth;
                for (int y = 0; y < textureHeight; y++)
                {
                    bool isGrid = (wrapX % 20 == 0) || (y % 20 == 0);
                    _pixelBuffer[y * textureWidth + wrapX] = isGrid ? gridColor : bgColor;
                }
            }

            // Draw line
            int py0 = Mathf.Clamp(Mathf.RoundToInt(y0 * textureHeight), 0, textureHeight - 1);
            int py1 = Mathf.Clamp(Mathf.RoundToInt(y1 * textureHeight), 0, textureHeight - 1);

            int x = x1 % textureWidth;
            int minY = Mathf.Min(py0, py1);
            int maxY = Mathf.Max(py0, py1);

            for (int y = minY; y <= maxY; y++)
            {
                _pixelBuffer[y * textureWidth + x] = ecgGreenColor;
                // Soft glow pixel
                if (x + 1 < textureWidth) _pixelBuffer[y * textureWidth + (x + 1)] = ecgGreenColor * 0.5f;
            }
        }

        private void UpdateUIReadouts()
        {
            if (hrText != null)
            {
                hrText.text = currentState == PhysiologicalState.Asystole ? "---" : Mathf.RoundToInt(heartRate).ToString();
                hrText.color = (heartRate > 120 || heartRate < 50) ? new Color(1f, 0.25f, 0.25f) : ecgGreenColor;
            }
            if (bpText != null)
            {
                bpText.text = $"{systolicBP}/{diastolicBP}";
                bpText.color = systolicBP < 90 ? new Color(1f, 0.3f, 0.2f) : new Color(0.9f, 0.25f, 0.25f);
            }
            if (spo2Text != null)
            {
                spo2Text.text = $"{Mathf.RoundToInt(oxygenSaturation)}%";
                spo2Text.color = oxygenSaturation < 92 ? new Color(1f, 0.7f, 0.1f) : new Color(0.0f, 0.85f, 0.95f);
            }
            if (rrText != null)
            {
                rrText.text = Mathf.RoundToInt(respiratoryRate).ToString();
                rrText.color = new Color(0.95f, 0.85f, 0.2f);
            }
            if (tempText != null)
            {
                tempText.text = $"{coreTemperature:F1}°C";
                tempText.color = Color.white;
            }
            if (alarmStatusText != null)
            {
                if (_alarmActive)
                {
                    alarmStatusText.text = "<color=#FF2222><b>ALARM: " + currentState.ToString().ToUpper() + "</b></color>";
                }
                else
                {
                    alarmStatusText.text = "<color=#14B8A6>STABLE // NORMAL SINUS RHYTHM</color>";
                }
            }
        }

        public void ApplyPhysiologicalPreset(PhysiologicalState state)
        {
            currentState = state;
            switch (state)
            {
                case PhysiologicalState.NormalSinusRhythm:
                    heartRate = 72f;
                    systolicBP = 120;
                    diastolicBP = 80;
                    oxygenSaturation = 99f;
                    respiratoryRate = 14f;
                    coreTemperature = 36.8f;
                    _alarmActive = false;
                    break;
                case PhysiologicalState.Tachycardia:
                    heartRate = 145f;
                    systolicBP = 135;
                    diastolicBP = 88;
                    oxygenSaturation = 98f;
                    respiratoryRate = 22f;
                    break;
                case PhysiologicalState.Bradycardia:
                    heartRate = 42f;
                    systolicBP = 100;
                    diastolicBP = 65;
                    oxygenSaturation = 96f;
                    respiratoryRate = 10f;
                    break;
                case PhysiologicalState.HypotensionCrisis:
                    heartRate = 115f;
                    systolicBP = 75;
                    diastolicBP = 45;
                    oxygenSaturation = 93f;
                    respiratoryRate = 26f;
                    break;
                case PhysiologicalState.Hypoxemia:
                    heartRate = 110f;
                    systolicBP = 118;
                    diastolicBP = 76;
                    oxygenSaturation = 82f;
                    respiratoryRate = 28f;
                    break;
                case PhysiologicalState.VentricularFibrillation:
                    heartRate = 210f;
                    systolicBP = 50;
                    diastolicBP = 25;
                    oxygenSaturation = 74f;
                    respiratoryRate = 8f;
                    break;
                case PhysiologicalState.Asystole:
                    heartRate = 0f;
                    systolicBP = 0;
                    diastolicBP = 0;
                    oxygenSaturation = 50f;
                    respiratoryRate = 0f;
                    break;
            }

            OnStateChanged?.Invoke(currentState);
        }

        public void SetHeartRate(float bpm) => heartRate = Mathf.Clamp(bpm, 20f, 240f);
        public void SetOxygenSaturation(float pct) => oxygenSaturation = Mathf.Clamp(pct, 40f, 100f);
        public void SetBloodPressure(int sys, int dia) { systolicBP = sys; diastolicBP = dia; }
    }
}
