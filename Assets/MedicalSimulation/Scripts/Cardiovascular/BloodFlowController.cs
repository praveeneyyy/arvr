using UnityEngine;
using System.Collections.Generic;

namespace MedicalSimulation.Cardiovascular
{
    /// <summary>
    /// Enhanced blood-flow controller. Manages one or more BloodVesselPath
    /// instances and a shared particle pool. Extends the existing BloodFlowPath
    /// approach while preserving its public API shape.
    /// 
    /// Educational circulation path:
    ///   Venae cavae → R. atrium → R. ventricle → Pulmonary arteries
    ///   → Lungs → Pulmonary veins → L. atrium → L. ventricle
    ///   → Aorta → Systemic circulation
    /// </summary>
    public class BloodFlowController : MonoBehaviour
    {
        [Header("Vessel Paths")]
        [SerializeField] private BloodVesselPath[] arterialPaths;
        [SerializeField] private BloodVesselPath[] venousPaths;
        [SerializeField] private BloodVesselPath[] pulmonaryPaths;

        [Header("Global Settings")]
        [SerializeField] private bool playOnStart = true;
        [Range(0.1f, 5f)]
        [SerializeField] private float flowSpeedMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float particleDensity = 1f;

        [Header("Heart Sync (optional)")]
        [SerializeField] private HeartAnimationController heartController;

        private bool _playing;

        void Start()
        {
            // Build full circuit if no paths are assigned in the Inspector:
            // just activate any BloodVesselPath children
            if ((arterialPaths == null || arterialPaths.Length == 0) &&
                (venousPaths   == null || venousPaths.Length   == 0) &&
                (pulmonaryPaths== null || pulmonaryPaths.Length == 0))
            {
                var all = GetComponentsInChildren<BloodVesselPath>(true);
                List<BloodVesselPath> art = new(), ven = new(), pul = new();
                foreach (var p in all)
                {
                    switch (p.VesselType)
                    {
                        case BloodVesselPath.VesselTypeEnum.Arterial:   art.Add(p); break;
                        case BloodVesselPath.VesselTypeEnum.Venous:     ven.Add(p); break;
                        case BloodVesselPath.VesselTypeEnum.Pulmonary:  pul.Add(p); break;
                    }
                }
                arterialPaths  = art.ToArray();
                venousPaths    = ven.ToArray();
                pulmonaryPaths = pul.ToArray();
            }

            if (heartController != null)
                heartController.OnHeartbeat += OnHeartbeat;

            if (playOnStart) Play();
        }

        void OnDestroy()
        {
            if (heartController != null)
                heartController.OnHeartbeat -= OnHeartbeat;
        }

        // ── Heartbeat sync ────────────────────────────────────────────────────

        private void OnHeartbeat()
        {
            // Optionally burst-emit particles on each beat
            // (implementation in BloodVesselPath.OnHeartbeat)
            foreach (var p in arterialPaths)  p?.OnHeartbeat();
            foreach (var p in pulmonaryPaths) p?.OnHeartbeat();
        }

        // ── Playback ──────────────────────────────────────────────────────────

        public void Play()
        {
            _playing = true;
            SetAllPaths(active: true);
        }

        public void Pause()
        {
            _playing = false;
            SetAllPaths(active: false);
        }

        public void SetFlowSpeed(float multiplier)
        {
            flowSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
            foreach (var p in AllPaths()) p.SetSpeedMultiplier(flowSpeedMultiplier);
        }

        public void SetParticleDensity(float density)
        {
            particleDensity = Mathf.Clamp01(density);
            foreach (var p in AllPaths()) p.SetDensity(particleDensity);
        }

        // ── Visibility ────────────────────────────────────────────────────────

        public void ShowArterialOnly()
        {
            SetPathsVisible(arterialPaths,  true);
            SetPathsVisible(venousPaths,    false);
            SetPathsVisible(pulmonaryPaths, false);
        }

        public void ShowVenousOnly()
        {
            SetPathsVisible(arterialPaths,  false);
            SetPathsVisible(venousPaths,    true);
            SetPathsVisible(pulmonaryPaths, false);
        }

        public void ShowPulmonaryOnly()
        {
            SetPathsVisible(arterialPaths,  false);
            SetPathsVisible(venousPaths,    false);
            SetPathsVisible(pulmonaryPaths, true);
        }

        public void ShowAllCirculation()
        {
            foreach (var p in AllPaths()) p.SetVisible(true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetAllPaths(bool active)
        {
            foreach (var p in AllPaths())
            {
                if (active) p.StartFlow();
                else         p.StopFlow();
            }
        }

        private void SetPathsVisible(BloodVesselPath[] paths, bool visible)
        {
            if (paths == null) return;
            foreach (var p in paths) p?.SetVisible(visible);
        }

        private IEnumerable<BloodVesselPath> AllPaths()
        {
            if (arterialPaths  != null) foreach (var p in arterialPaths)  if (p) yield return p;
            if (venousPaths    != null) foreach (var p in venousPaths)    if (p) yield return p;
            if (pulmonaryPaths != null) foreach (var p in pulmonaryPaths) if (p) yield return p;
        }
    }
}
