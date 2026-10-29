using UnityEngine;
using System;

namespace MedicalSimulation.Respiratory
{
    /// <summary>
    /// Drives synchronized lung and diaphragm animation.
    /// Respiratory rate is a simulation parameter, not a real patient measurement.
    /// </summary>
    public class RespiratoryAnimationController : MonoBehaviour
    {
        [Header("Respiratory Rate (simulation)")]
        [Range(8, 30)]
        [SerializeField] private float respiratoryRatePerMin = 14f;

        [Header("Lung Transforms")]
        [SerializeField] private Transform leftLung;
        [SerializeField] private Transform rightLung;
        [SerializeField] private Transform diaphragm;

        [Header("Blend Shapes")]
        [SerializeField] private SkinnedMeshRenderer leftLungRenderer;
        [SerializeField] private SkinnedMeshRenderer rightLungRenderer;
        [Tooltip("Blend shape index on the lung that represents full inflation.")]
        [SerializeField] private int inflationBlendShapeIndex = 0;

        [Header("Scale Fallback")]
        [Range(0f, 0.2f)]
        [SerializeField] private float lungInflationScale  = 0.12f;
        [Range(0f, 0.05f)]
        [SerializeField] private float diaphragmDropAmount = 0.025f;

        [Header("Playback")]
        [SerializeField] private bool playOnStart = true;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fires at the start of each inhalation. Used for audio sync.</summary>
        public event Action OnInhale;
        /// <summary>Fires at the start of each exhalation.</summary>
        public event Action OnExhale;

        // ── Internal ─────────────────────────────────────────────────────────
        private bool  _playing;
        private float _phase;         // 0–1 within one breath cycle
        private bool  _inhaleFired;
        private bool  _exhaleFired;
        private float _targetRPM;
        private float _currentRPM;

        // Smooth breath curve: rise for ~40%, hold briefly, fall for ~50%, pause
        private static readonly AnimationCurve _breathCurve = new AnimationCurve(
            new Keyframe(0f,    0f, 0f, 3f),
            new Keyframe(0.38f, 1f, 0f, 0f),  // peak inhalation
            new Keyframe(0.44f, 1f, 0f, 0f),  // brief hold
            new Keyframe(0.85f, 0f, 0f, 0f),  // full exhalation
            new Keyframe(1f,    0f, 0f, 0f)   // end pause
        );

        private Vector3 _leftLungBase, _rightLungBase, _diaphragmBase;

        void Start()
        {
            _targetRPM  = respiratoryRatePerMin;
            _currentRPM = respiratoryRatePerMin;

            if (leftLung   != null) _leftLungBase   = leftLung.localScale;
            if (rightLung  != null) _rightLungBase  = rightLung.localScale;
            if (diaphragm  != null) _diaphragmBase  = diaphragm.localPosition;

            if (playOnStart) Play();
        }

        void Update()
        {
            if (!_playing) return;

            _currentRPM = Mathf.MoveTowards(_currentRPM, _targetRPM, 2f * Time.deltaTime);
            float cycleTime = 60f / Mathf.Max(1f, _currentRPM);
            _phase += Time.deltaTime / cycleTime;

            if (_phase >= 1f)
            {
                _phase -= 1f;
                _inhaleFired = false;
                _exhaleFired = false;
            }

            float t = _breathCurve.Evaluate(_phase);

            ApplyLungInflation(t);
            ApplyDiaphragm(t);
            FireEvents(t);
        }

        private void ApplyLungInflation(float t)
        {
            // Blend shape path
            if (leftLungRenderer != null &&
                leftLungRenderer.sharedMesh != null &&
                leftLungRenderer.sharedMesh.blendShapeCount > inflationBlendShapeIndex)
            {
                leftLungRenderer.SetBlendShapeWeight(inflationBlendShapeIndex,  t * 100f);
                rightLungRenderer?.SetBlendShapeWeight(inflationBlendShapeIndex, t * 100f);
            }
            // Scale fallback
            else
            {
                if (leftLung  != null) leftLung.localScale  = _leftLungBase  * (1f + t * lungInflationScale);
                if (rightLung != null) rightLung.localScale = _rightLungBase * (1f + t * lungInflationScale);
            }
        }

        private void ApplyDiaphragm(float t)
        {
            if (diaphragm != null)
                // Diaphragm descends on inhalation (positive t), rises on exhalation
                diaphragm.localPosition = _diaphragmBase + new Vector3(0, -t * diaphragmDropAmount, 0);
        }

        private void FireEvents(float t)
        {
            if (_phase < 0.05f && !_inhaleFired)
            {
                _inhaleFired = true;
                OnInhale?.Invoke();
            }
            if (_phase > 0.44f && _phase < 0.52f && !_exhaleFired)
            {
                _exhaleFired = true;
                OnExhale?.Invoke();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Play()  => _playing = true;
        public void Pause() => _playing = false;

        public void SetRespiratoryRate(float ratePerMin)
        {
            _targetRPM            = Mathf.Clamp(ratePerMin, 4f, 60f);
            respiratoryRatePerMin = _targetRPM;
        }

        public float GetRespiratoryRate() => _currentRPM;
        public bool  IsPlaying()          => _playing;
        public float GetBreathPhase()     => _phase;
    }
}
