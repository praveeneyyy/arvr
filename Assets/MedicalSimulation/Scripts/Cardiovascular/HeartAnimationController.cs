using UnityEngine;
using System;

namespace MedicalSimulation.Cardiovascular
{
    /// <summary>
    /// Drives a smooth cardiac animation on a heart model.
    /// Supports blend-shape deformation (preferred) or uniform scale pulse (fallback).
    /// Heart rate is a simulation parameter, not a real patient measurement.
    /// </summary>
    public class HeartAnimationController : MonoBehaviour
    {
        // ── Settings ──────────────────────────────────────────────────────────
        [Header("Heart Rate (simulation)")]
        [Range(30, 200)]
        [SerializeField] private float heartRateBPM = 72f;

        [Header("Blend Shape (preferred)")]
        [SerializeField] private SkinnedMeshRenderer heartRenderer;
        [Tooltip("Index of the contraction blend shape (0–100).")]
        [SerializeField] private int contractionBlendShapeIndex = 0;
        [Range(0f, 100f)]
        [SerializeField] private float contractionAmount = 30f;

        [Header("Scale Pulse Fallback")]
        [Tooltip("Used when no SkinnedMeshRenderer / blend shape is available.")]
        [SerializeField] private Transform heartRoot;
        [Range(0f, 0.15f)]
        [SerializeField] private float scalePulseAmount = 0.08f;

        [Header("Playback")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float speedMultiplier = 1f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fires once per systolic contraction peak. Useful for heartbeat audio sync.</summary>
        public event Action OnHeartbeat;

        // ── Internal ─────────────────────────────────────────────────────────
        private bool  _playing;
        private float _phase;         // 0–1 within one cardiac cycle
        private bool  _beatFired;
        private float _targetBPM;
        private float _currentBPM;

        // Systolic curve: fast contraction, slower relaxation
        private static readonly AnimationCurve _contractionCurve = new AnimationCurve(
            new Keyframe(0f,   0f,  0f,  8f),
            new Keyframe(0.12f, 1f,  0f,  0f),   // peak systole
            new Keyframe(0.40f, 0f,  0f,  0f),   // end diastole
            new Keyframe(1f,   0f,  0f,  0f)
        );

        void Start()
        {
            _targetBPM  = heartRateBPM;
            _currentBPM = heartRateBPM;
            if (playOnStart) Play();
        }

        void Update()
        {
            if (!_playing) return;

            // Smoothly transition to target BPM
            _currentBPM = Mathf.MoveTowards(_currentBPM, _targetBPM, 10f * Time.deltaTime);

            float cycleTime = 60f / Mathf.Max(1f, _currentBPM);
            _phase += (Time.deltaTime * speedMultiplier) / cycleTime;

            if (_phase >= 1f)
            {
                _phase -= 1f;
                _beatFired = false;
            }

            float t = _contractionCurve.Evaluate(_phase);

            // Blend shape path
            if (heartRenderer != null && heartRenderer.sharedMesh != null &&
                heartRenderer.sharedMesh.blendShapeCount > contractionBlendShapeIndex)
            {
                heartRenderer.SetBlendShapeWeight(contractionBlendShapeIndex, t * contractionAmount);
            }
            // Scale fallback path
            else if (heartRoot != null)
            {
                float s = 1f + t * scalePulseAmount;
                heartRoot.localScale = new Vector3(s, s, s);
            }

            // Fire heartbeat event at ~peak systole
            if (_phase > 0.09f && _phase < 0.15f && !_beatFired)
            {
                _beatFired = true;
                OnHeartbeat?.Invoke();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Play()  => _playing = true;
        public void Pause() => _playing = false;

        public void SetHeartRate(float bpm)
        {
            _targetBPM    = Mathf.Clamp(bpm, 30f, 200f);
            heartRateBPM  = _targetBPM;
        }

        public float GetHeartRate()    => _currentBPM;
        public bool  IsPlaying()       => _playing;
        public float GetCyclePhase()   => _phase;
    }
}
