using UnityEngine;
using System.Collections.Generic;

namespace MedicalSimulation.Cardiovascular
{
    /// <summary>
    /// Manages particle-based blood flow along an ordered set of waypoints.
    /// Extends and replaces the original BloodFlowPath with:
    ///  - VesselType enum (arterial / venous / pulmonary)
    ///  - Speed and density controls
    ///  - Heartbeat burst support
    ///  - Allocation-free particle steering (array cached)
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BloodVesselPath : MonoBehaviour
    {
        public enum VesselTypeEnum { Arterial, Venous, Pulmonary }

        [Header("Vessel Classification")]
        public VesselTypeEnum VesselType = VesselTypeEnum.Arterial;

        [Header("Waypoints (ordered)")]
        public List<Transform> waypoints = new();

        [Header("Flow Settings")]
        [Range(0.5f, 10f)] public float baseFlowSpeed   = 3f;
        [Range(0.05f, 1f)] public float veinWidth        = 0.3f;
        [Range(0f, 1f)]    public float turbulence       = 0.3f;
        [Range(0.5f, 10f)] public float steeringStrength = 5f;

        private float _speedMultiplier = 1f;
        private float _density         = 1f;

        private ParticleSystem   _ps;
        private ParticleSystem.Particle[] _particles;

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _particles = new ParticleSystem.Particle[_ps.main.maxParticles];
        }

        void Start()
        {
            SetupParticleColors();
        }

        void LateUpdate()
        {
            if (waypoints.Count < 2 || _ps == null) return;

            int count = _ps.GetParticles(_particles);
            float lifetime = _ps.main.startLifetime.constant;
            if (lifetime <= 0) return;

            for (int i = 0; i < count; i++)
            {
                float age        = lifetime - _particles[i].remainingLifetime;
                float norm       = Mathf.Clamp01(age / lifetime);
                float pathProg   = norm * (waypoints.Count - 1);
                int   segStart   = Mathf.Min(Mathf.FloorToInt(pathProg), waypoints.Count - 2);
                float segT       = pathProg - segStart;

                Vector3 target   = Vector3.Lerp(waypoints[segStart].position,
                                                waypoints[segStart + 1].position, segT);

                // Randomized vessel-width offset seeded per-particle
                uint seed    = _particles[i].randomSeed;
                float ox     = ((seed % 100) / 100f * 2f - 1f) * veinWidth;
                float oy     = ((seed % 137) / 137f * 2f - 1f) * veinWidth;
                Vector3 wiggle = new Vector3(
                    Mathf.Sin(Time.time * 2.1f + i) * turbulence * 0.08f,
                    Mathf.Cos(Time.time * 1.7f + i) * turbulence * 0.08f,
                    0f);

                Vector3 finalTarget = target + new Vector3(ox, oy, 0) + wiggle;
                float   speed       = baseFlowSpeed * _speedMultiplier;
                Vector3 desired     = (finalTarget - _particles[i].position).normalized * speed;
                _particles[i].velocity = Vector3.Lerp(_particles[i].velocity, desired,
                                                      Time.deltaTime * steeringStrength);
            }

            _ps.SetParticles(_particles, count);
        }

        // ── Color by vessel type ──────────────────────────────────────────────

        private void SetupParticleColors()
        {
            var main = _ps.main;
            switch (VesselType)
            {
                case VesselTypeEnum.Arterial:
                    main.startColor = new Color(0.85f, 0.1f, 0.1f, 0.9f); // bright red
                    break;
                case VesselTypeEnum.Venous:
                    main.startColor = new Color(0.45f, 0.05f, 0.2f, 0.9f); // dark maroon
                    break;
                case VesselTypeEnum.Pulmonary:
                    main.startColor = new Color(0.7f, 0.3f, 0.6f, 0.9f);  // pink-purple
                    break;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartFlow()
        {
            if (!_ps.isPlaying) _ps.Play();
        }

        public void StopFlow()
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void SetSpeedMultiplier(float mult)
        {
            _speedMultiplier = Mathf.Clamp(mult, 0.1f, 5f);
        }

        public void SetDensity(float density)
        {
            _density = Mathf.Clamp01(density);
            var emission = _ps.emission;
            // Scale emission rate proportionally (read the original rate from main.maxParticles)
            emission.rateOverTime = density * (_ps.main.maxParticles / _ps.main.startLifetime.constant);
        }

        public void SetVisible(bool visible)
        {
            _ps.gameObject.SetActive(visible);
        }

        /// <summary>Called by BloodFlowController on each heartbeat for optional burst emit.</summary>
        public void OnHeartbeat()
        {
            // Burst 3 particles with each beat for arterial pulse effect
            if (VesselType == VesselTypeEnum.Arterial)
                _ps.Emit(3);
        }
    }
}
