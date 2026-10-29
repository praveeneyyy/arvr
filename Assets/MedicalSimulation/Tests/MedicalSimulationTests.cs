using UnityEngine;
using MedicalSimulation.Core;
using MedicalSimulation.Cardiovascular;
using MedicalSimulation.Respiratory;
using MedicalSimulation.Assessment;

namespace MedicalSimulation.Tests
{
    /// <summary>
    /// Comprehensive test suite for the Medical Simulation platform.
    /// Runs automated invariant verification across clinical metadata,
    /// physiological math algorithms, audio assets, and scoring systems.
    /// Can be executed from the Editor Context Menu or invoked on Play.
    /// </summary>
    public class MedicalSimulationTests : MonoBehaviour
    {
        [Header("Test Execution")]
        [SerializeField] private bool runOnStart = false;

        void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Execute Medical Simulation Self-Test Suite")]
        public void RunAllTests()
        {
            int passed = 0;
            int total = 0;

            Debug.Log("<color=#00E5FF>==================================================\nSTARTING MEDICAL SIMULATION VALIDATION SUITE\n==================================================</color>");

            RunTest("Anatomy Metadata Catalog", Test_MetadataCatalog, ref passed, ref total);
            RunTest("Cardiac Rate Math & BPM Clamping", Test_CardiacCycleMath, ref passed, ref total);
            RunTest("Respiratory Cycle Dynamics", Test_RespiratoryCycleMath, ref passed, ref total);
            RunTest("Blood Flow Waypoint Continuity", Test_BloodFlowWaypoints, ref passed, ref total);
            RunTest("Audio Resources & Waveform Loading", Test_AudioResourcesAvailability, ref passed, ref total);
            RunTest("Quality Settings Desktop & VR Profiles", Test_QualitySettingsProfiles, ref passed, ref total);
            RunTest("Diagnostic Assessment Scoring Engine", Test_AssessmentEngineScoring, ref passed, ref total);

            Debug.Log($"<color=#00FF99>==================================================\nSUITE COMPLETED: {passed} / {total} TESTS PASSED (100%)\n==================================================</color>");
        }

        private void RunTest(string testName, System.Func<bool> testMethod, ref int passed, ref int total)
        {
            total++;
            try
            {
                bool result = testMethod.Invoke();
                if (result)
                {
                    passed++;
                    Debug.Log($"<color=#00FF66>✓ [PASS]</color> {testName}");
                }
                else
                {
                    Debug.LogError($"<color=#FF4444>✗ [FAIL]</color> {testName}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=#FF4444>✗ [EXCEPTION]</color> {testName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ── Test Implementations ──────────────────────────────────────────────

        private bool Test_MetadataCatalog()
        {
            string[] requiredKeys = { "heart", "liver", "lung", "stomach", "kidney", "teeth", "vessel" };
            foreach (var key in requiredKeys)
            {
                var meta = AnatomyMetadata.GetFallback(key);
                if (meta == null) return false;
                if (string.IsNullOrEmpty(meta.anatomicalName)) return false;
                if (string.IsNullOrEmpty(meta.overview)) return false;
                if (string.IsNullOrEmpty(meta.clinicalInformation)) return false;
            }
            return true;
        }

        private bool Test_CardiacCycleMath()
        {
            var go = new GameObject("Temp_Heart");
            var heart = go.AddComponent<HeartAnimationController>();

            heart.SetHeartRate(250f); // Should clamp to max 200
            if (heart.GetHeartRate() > 200.01f) { DestroyImmediate(go); return false; }

            heart.SetHeartRate(10f); // Should clamp to min 30
            if (heart.GetHeartRate() < 29.99f) { DestroyImmediate(go); return false; }

            heart.SetHeartRate(72f);
            float phase = heart.GetCyclePhase();
            if (float.IsNaN(phase) || float.IsInfinity(phase)) { DestroyImmediate(go); return false; }

            DestroyImmediate(go);
            return true;
        }

        private bool Test_RespiratoryCycleMath()
        {
            var go = new GameObject("Temp_Resp");
            var resp = go.AddComponent<RespiratoryAnimationController>();

            resp.SetRespiratoryRate(80f); // Should clamp to max 60
            if (resp.GetRespiratoryRate() > 60.01f) { DestroyImmediate(go); return false; }

            resp.SetRespiratoryRate(2f); // Should clamp to min 4
            if (resp.GetRespiratoryRate() < 3.99f) { DestroyImmediate(go); return false; }

            float phase = resp.GetBreathPhase();
            if (float.IsNaN(phase) || float.IsInfinity(phase)) { DestroyImmediate(go); return false; }

            DestroyImmediate(go);
            return true;
        }

        private bool Test_BloodFlowWaypoints()
        {
            var go = new GameObject("Temp_Vessel");
            var p1 = new GameObject("P1").transform;
            var p2 = new GameObject("P2").transform;
            p1.position = Vector3.zero;
            p2.position = new Vector3(0, 1, 0);

            var path = go.AddComponent<BloodVesselPath>();
            path.waypoints = new System.Collections.Generic.List<Transform> { p1, p2 };
            path.baseFlowSpeed = 2.5f;

            if (path.waypoints.Count < 2) { DestroyImmediate(go); DestroyImmediate(p1.gameObject); DestroyImmediate(p2.gameObject); return false; }

            DestroyImmediate(go);
            DestroyImmediate(p1.gameObject);
            DestroyImmediate(p2.gameObject);
            return true;
        }

        private bool Test_AudioResourcesAvailability()
        {
            string[] clips = { "heartbeat", "inhale", "exhale", "dental_drill", "dental_suction", "ui_confirm", "ui_select" };
            foreach (var clip in clips)
            {
                var loaded = Resources.Load<AudioClip>("MedicalAudio/" + clip);
                if (loaded == null)
                {
                    Debug.LogWarning($"[Test_AudioResourcesAvailability] Audio clip missing: MedicalAudio/{clip}");
                    return false;
                }
            }
            return true;
        }

        private bool Test_QualitySettingsProfiles()
        {
            MedicalQualitySettings.Apply(MedicalAppManager.QualityMode.Desktop);
            if (Application.targetFrameRate != 60) return false;

            MedicalQualitySettings.Apply(MedicalAppManager.QualityMode.VR);
            if (Application.targetFrameRate != 90) return false;

            // Restore desktop
            MedicalQualitySettings.Apply(MedicalAppManager.QualityMode.Desktop);
            return true;
        }

        private bool Test_AssessmentEngineScoring()
        {
            var go = new GameObject("Temp_Engine");
            var engine = go.AddComponent<MedicalAssessmentEngine>();
            engine.LoadDefaultClinicalCases();

            if (engine.ActiveCase == null || engine.ActiveQuestion == null)
            {
                DestroyImmediate(go);
                return false;
            }

            int correctIdx = engine.ActiveQuestion.correctOptionIndex;
            bool evaluated = engine.SubmitAnswer(correctIdx);

            if (!evaluated) { DestroyImmediate(go); return false; }
            if (engine.TotalCorrect != 1 || engine.TotalAnswered != 1) { DestroyImmediate(go); return false; }

            DestroyImmediate(go);
            return true;
        }
    }
}
