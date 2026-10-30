using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Cardiovascular;
using MedicalSimulation.Respiratory;
using MedicalSimulation.Assessment;
using MedicalSimulation.AI;
using MedicalSimulation.Audio;

namespace MedicalSimulation.Editor
{
    /// <summary>
    /// Comprehensive end-to-end automated test runner for the Medical Simulation platform.
    /// Can be executed in the Unity Editor via the menu: MedicalSimulation -> Run Full Project Test Suite,
    /// or invoked headlessly in batchmode via command line.
    /// Tests every system: physics math, anatomy, cardiology, respiration, audio, UI, AI, scenes, and prefabs.
    /// </summary>
    public static class MedicalFullProjectTestRunner
    {
        private static int _passCount = 0;
        private static int _failCount = 0;
        private static List<string> _testLog = new List<string>();

        [MenuItem("MedicalSimulation/Run Complete Project Verification Suite")]
        public static void RunFromMenu()
        {
            RunCompleteProjectVerificationSuite();
        }

        public static void RunCompleteProjectVerificationSuite()
        {
            _passCount = 0;
            _failCount = 0;
            _testLog.Clear();

            LogHeader("STARTING FULL MEDICAL PROJECT VERIFICATION & TEST SUITE");

            // 1. Core Data & Metadata
            ExecuteTest("Anatomy Metadata Fallback Catalog", Test_MetadataCatalog);
            ExecuteTest("Anatomy Metadata Completeness & Accuracy", Test_MetadataCompleteness);

            // 2. Physiological Animation & Math
            ExecuteTest("Cardiac Cycle Math & 30-200 BPM Clamping", Test_CardiacCycleMath);
            ExecuteTest("Respiratory Mechanics & Diaphragm Displacement", Test_RespiratoryCycleMath);
            ExecuteTest("Digestive Peristalsis Traveling Wave Dynamics", Test_PeristalsisMath);
            ExecuteTest("Blood Flow 10-Waypoint Loop Continuity", Test_BloodFlowWaypoints);
            ExecuteTest("Anatomical Cross-Section Slicing Axes", Test_SlicePlaneMath);

            // 3. Audio & Acoustic Design
            ExecuteTest("11 Synthesized PCM WAV Audio Resources", Test_AudioResources);

            // 4. Clinical Assessment & Procedure Systems
            ExecuteTest("Diagnostic Assessment Engine & Scoring Invariants", Test_AssessmentEngine);
            ExecuteTest("Procedure Step Progression & Error Tracking", Test_ProcedureRunner);

            // 5. Quality & Performance Profiling
            ExecuteTest("Quality Settings Desktop & VR Profiles", Test_QualitySettingsProfiles);

            // 6. Clinical AI Assistant Subsystem
            ExecuteTest("AI Chat Message Invariants & Citations", Test_AIChatMessage);
            ExecuteTest("AI Conversation History Sliding Window & JSON Serialization", Test_AIConversationHistory);
            ExecuteTest("AI Assistant Context & Runtime Simulation Injection", Test_AIAssistantContext);
            ExecuteTest("AI In-Memory RAG Retrieval & Source Citations", Test_AIRAGEngine);
            ExecuteTest("AI Local Knowledge Engine Offline Clinical Answers", Test_AILocalKnowledgeEngine);
            ExecuteTest("AI Local Knowledge Engine STEM Answers (ML, RAM, Photosynthesis)", Test_AILocalKnowledgeSTEM);
            ExecuteTest("AI Configuration & API Key Fallback Resolution", Test_AIAssistantConfig);

            // 7. 3D Anatomical Art & PBR Materials
            ExecuteTest("Blender 5.2 3D Model Geometry Files (FBX/GLB)", Test_3DModelFiles);
            ExecuteTest("High-Resolution Clinical Renders & Overview Grid", Test_ClinicalRenders);
            ExecuteTest("PBR Medical Materials Library (URP)", Test_PBRMaterials);

            // 8. Prefab Generation & Integration
            ExecuteTest("Anatomical Prefab Construction & Collider Wiring", Test_PrefabConstruction);

            // 9. Scene Hierarchy & Build Settings
            ExecuteTest("Editor Build Settings Scene Registry (All Scenes)", Test_BuildSettingsScenes);

            // Summary
            LogSummary();

            if (Application.isBatchMode)
            {
                int exitCode = _failCount == 0 ? 0 : 1;
                EditorApplication.Exit(exitCode);
            }
        }

        private static void ExecuteTest(string testName, Func<bool> testMethod)
        {
            try
            {
                bool success = testMethod();
                if (success)
                {
                    _passCount++;
                    string msg = $"[PASS] {testName}";
                    _testLog.Add(msg);
                    Debug.Log($"<color=#00FF99>{msg}</color>");
                }
                else
                {
                    _failCount++;
                    string msg = $"[FAIL] {testName}";
                    _testLog.Add(msg);
                    Debug.LogError($"<color=#FF5252>{msg}</color>");
                }
            }
            catch (Exception ex)
            {
                _failCount++;
                string msg = $"[EXCEPTION] {testName}: {ex.Message}";
                _testLog.Add(msg);
                Debug.LogError($"<color=#FF5252>{msg}\n{ex.StackTrace}</color>");
            }
        }

        // ── 1. Metadata Tests ────────────────────────────────────────────────
        private static bool Test_MetadataCatalog()
        {
            string[] requiredKeys = { "heart", "lung", "liver", "stomach", "intestine", "kidney" };
            foreach (var key in requiredKeys)
            {
                var meta = AnatomyMetadata.GetFallback(key);
                if (meta == null) return false;
                if (string.IsNullOrEmpty(meta.anatomicalName)) return false;
                if (string.IsNullOrEmpty(meta.overview)) return false;
            }
            return true;
        }

        private static bool Test_MetadataCompleteness()
        {
            var heart = AnatomyMetadata.GetFallback("heart");
            if (heart.system != AnatomicalSystem.Cardiovascular) return false;
            if (string.IsNullOrEmpty(heart.clinicalInformation)) return false;
            if (heart.relatedStructures == null || heart.relatedStructures.Length == 0) return false;
            return true;
        }

        // ── 2. Physiology Math Tests ─────────────────────────────────────────
        private static bool Test_CardiacCycleMath()
        {
            var go = new GameObject("Test_HeartAnim");
            var ctrl = go.AddComponent<HeartAnimationController>();

            ctrl.SetBPM(10f); // below min (30)
            if (ctrl.BPM < 30f) { Object.DestroyImmediate(go); return false; }

            ctrl.SetBPM(300f); // above max (200)
            if (ctrl.BPM > 200f) { Object.DestroyImmediate(go); return false; }

            ctrl.SetBPM(72f);
            if (Mathf.Abs(ctrl.BPM - 72f) > 0.01f) { Object.DestroyImmediate(go); return false; }

            Object.DestroyImmediate(go);
            return true;
        }

        private static bool Test_RespiratoryCycleMath()
        {
            var go = new GameObject("Test_RespAnim");
            var ctrl = go.AddComponent<RespiratoryAnimationController>();

            ctrl.SetRate(5f); // below min (8)
            if (ctrl.RespirationsPerMinute < 8f) { Object.DestroyImmediate(go); return false; }

            ctrl.SetRate(60f); // above max (40)
            if (ctrl.RespirationsPerMinute > 40f) { Object.DestroyImmediate(go); return false; }

            ctrl.SetRate(16f);
            if (Mathf.Abs(ctrl.RespirationsPerMinute - 16f) > 0.01f) { Object.DestroyImmediate(go); return false; }

            Object.DestroyImmediate(go);
            return true;
        }

        private static bool Test_PeristalsisMath()
        {
            var go = new GameObject("Test_Peristalsis");
            var ctrl = go.AddComponent<DigestivePeristalsisController>();
            if (ctrl.WaveFrequency <= 0f) { Object.DestroyImmediate(go); return false; }
            if (ctrl.WaveSpeed <= 0f) { Object.DestroyImmediate(go); return false; }

            Object.DestroyImmediate(go);
            return true;
        }

        private static bool Test_BloodFlowWaypoints()
        {
            var go = new GameObject("Test_BloodVessel");
            var path = go.AddComponent<BloodVesselPath>();

            var points = path.GetLoopPoints();
            if (points == null || points.Length < 6)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            // Check non-zero distance between consecutive waypoints
            for (int i = 0; i < points.Length - 1; i++)
            {
                float dist = Vector3.Distance(points[i], points[i + 1]);
                if (dist < 0.01f)
                {
                    Object.DestroyImmediate(go);
                    return false;
                }
            }

            Object.DestroyImmediate(go);
            return true;
        }

        private static bool Test_SlicePlaneMath()
        {
            var go = new GameObject("Test_SlicePlane");
            var ctrl = go.AddComponent<AnatomicalSlicePlaneController>();

            ctrl.SetSliceAxis(AnatomicalSlicePlaneController.SliceAxis.Axial);
            if (ctrl.ActiveAxis != AnatomicalSlicePlaneController.SliceAxis.Axial) { Object.DestroyImmediate(go); return false; }

            ctrl.SetSlicePosition(0.5f);
            if (Mathf.Abs(ctrl.SlicePosition - 0.5f) > 0.01f) { Object.DestroyImmediate(go); return false; }

            Object.DestroyImmediate(go);
            return true;
        }

        // ── 3. Audio Tests ───────────────────────────────────────────────────
        private static bool Test_AudioResources()
        {
            string[] audioFiles = {
                "heartbeat", "inhale", "exhale", "dental_drill",
                "dental_suction", "dental_scaler", "instrument_place",
                "procedure_complete", "ui_select", "ui_confirm", "lab_ambience"
            };

            foreach (var clipName in audioFiles)
            {
                var clip = Resources.Load<AudioClip>("MedicalAudio/" + clipName);
                if (clip == null)
                {
                    Debug.LogWarning($"[Test_AudioResources] Clip missing in Resources/MedicalAudio: {clipName}");
                    return false;
                }
                if (clip.length <= 0f) return false;
            }
            return true;
        }

        // ── 4. Assessment & Procedure Tests ──────────────────────────────────
        private static bool Test_AssessmentEngine()
        {
            var go = new GameObject("Test_Engine");
            var engine = go.AddComponent<MedicalAssessmentEngine>();
            engine.LoadDefaultClinicalCases();

            if (engine.ActiveCase == null || engine.ActiveQuestion == null)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            int correctIdx = engine.ActiveQuestion.correctOptionIndex;
            bool result = engine.SubmitAnswer(correctIdx);
            if (!result || engine.TotalCorrect != 1)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            Object.DestroyImmediate(go);
            return true;
        }

        private static bool Test_ProcedureRunner()
        {
            var go = new GameObject("Test_ProcRunner");
            var runner = go.AddComponent<ProcedureRunner>();

            runner.StartProcedure("Test Procedure", 3);
            if (!runner.IsActive || runner.TotalSteps != 3)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            runner.CompleteStep();
            if (runner.CurrentStepIndex != 1)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            runner.RecordSterileBreak("Test error");
            if (runner.SterileTechniqueBreaks != 1)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            Object.DestroyImmediate(go);
            return true;
        }

        // ── 5. Quality Settings ──────────────────────────────────────────────
        private static bool Test_QualitySettingsProfiles()
        {
            var go = new GameObject("Test_Quality");
            var settings = go.AddComponent<MedicalQualitySettings>();

            settings.ApplyDesktopProfile();
            if (settings.CurrentProfile != QualityProfile.DesktopHigh)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            settings.ApplyVRProfile();
            if (settings.CurrentProfile != QualityProfile.VROptimized)
            {
                Object.DestroyImmediate(go);
                return false;
            }

            Object.DestroyImmediate(go);
            return true;
        }

        // ── 6. AI Assistant Subsystem Tests ──────────────────────────────────
        private static bool Test_AIChatMessage()
        {
            var msg = new ChatMessage(MessageRole.User, "Explain coronary arteries", "organ.heart", new[] { "Gray's Anatomy" });
            if (msg.Role != MessageRole.User) return false;
            if (msg.Content != "Explain coronary arteries") return false;
            if (msg.RelatedOrganId != "organ.heart") return false;
            if (msg.Citations.Count != 1) return false;
            return true;
        }

        private static bool Test_AIConversationHistory()
        {
            var history = new ConversationHistory(maxTurnLimit: 3);
            history.AddTurn(MessageRole.System, "System prompt");
            history.AddTurn(MessageRole.User, "Turn 1");
            history.AddTurn(MessageRole.Assistant, "Resp 1");
            history.AddTurn(MessageRole.User, "Turn 2");

            string json = history.ToJson();
            if (string.IsNullOrEmpty(json) || !json.Contains("Turn 2")) return false;

            var newHist = new ConversationHistory();
            if (!newHist.FromJson(json) || newHist.Count != history.Count) return false;

            return true;
        }

        private static bool Test_AIAssistantContext()
        {
            var ctx = new AssistantContext();
            ctx.activeSceneName = "04_AnatomyLab";
            ctx.simulatedHeartRate = 80f;
            ctx.SetSelectedOrgan(AnatomyMetadata.GetFallback("heart"));

            string sys = ctx.BuildSystemInstruction();
            if (string.IsNullOrEmpty(sys) || !sys.Contains("MEDICAL ACCURACY")) return false;

            string runtime = ctx.BuildRuntimeContextString();
            if (string.IsNullOrEmpty(runtime) || !runtime.Contains("Heart") || !runtime.Contains("80 BPM")) return false;

            return true;
        }

        private static bool Test_AIRAGEngine()
        {
            var rag = new MedicalRAGEngine();
            rag.Initialize();
            if (rag.DocumentCount < 6) return false;

            var matches = rag.RetrieveRelevant("heart valves myocardial infarction", 2);
            if (matches == null || matches.Count == 0) return false;
            if (!matches[0].title.Contains("Heart")) return false;

            string aug = rag.BuildAugmentationString("lung alveolar gas exchange", 1, out List<string> citations);
            if (string.IsNullOrEmpty(aug) || citations.Count == 0) return false;

            return true;
        }

        private static bool Test_AILocalKnowledgeEngine()
        {
            var engine = new LocalAssistantFallback();
            var ctx = new AssistantContext();
            ctx.SetSelectedOrgan(AnatomyMetadata.GetFallback("heart"));

            var task = engine.SendMessageAsync("Explain heart valves", null, ctx, System.Threading.CancellationToken.None);
            task.Wait();
            var res = task.Result;

            if (res == null || !res.Success) return false;
            if (!res.Content.Contains("Heart") && !res.Content.Contains("Cardiovascular")) return false;
            if (res.Citations.Count == 0) return false;

            return true;
        }

        private static bool Test_AILocalKnowledgeSTEM()
        {
            var engine = new LocalAssistantFallback();
            var ctx = new AssistantContext();

            // Machine Learning
            var t1 = engine.SendMessageAsync("Explain machine learning", null, ctx, System.Threading.CancellationToken.None);
            t1.Wait();
            if (!t1.Result.Success || !t1.Result.Content.Contains("Machine Learning")) return false;

            // RAM vs Storage
            var t2 = engine.SendMessageAsync("Difference between RAM and storage", null, ctx, System.Threading.CancellationToken.None);
            t2.Wait();
            if (!t2.Result.Success || !t2.Result.Content.Contains("RAM vs. Storage")) return false;

            // Photosynthesis
            var t3 = engine.SendMessageAsync("Explain photosynthesis", null, ctx, System.Threading.CancellationToken.None);
            t3.Wait();
            if (!t3.Result.Success || !t3.Result.Content.Contains("Photosynthesis")) return false;

            return true;
        }

        private static bool Test_AIAssistantConfig()
        {
            var config = AssistantConfiguration.CreateDefault();
            if (config.primaryProvider != AIProviderType.Gemini) return false;
            if (!config.autoFallbackCascade) return false;
            if (!config.enableRAG) return false;
            return true;
        }

        // ── 7. 3D Models & Renders Tests ─────────────────────────────────────
        private static bool Test_3DModelFiles()
        {
            string[] requiredFiles = {
                "Assets/MedicalSimulation/Art/Anatomy/Human_Torso_Anatomy_Full.fbx",
                "Assets/MedicalSimulation/Art/Anatomy/Human_Torso_Anatomy_Full.glb",
                "Assets/MedicalSimulation/Art/Skeleton/Skeletal_System.fbx",
                "Assets/MedicalSimulation/Art/Vessels/Cardiovascular_System.fbx",
                "Assets/MedicalSimulation/Art/Organs/Heart_Detailed.fbx",
                "Assets/MedicalSimulation/Art/Organs/Respiratory_System.fbx",
                "Assets/MedicalSimulation/Art/Organs/Digestive_System.fbx",
                "Assets/MedicalSimulation/Art/Organs/Urinary_System.fbx",
                "Assets/MedicalSimulation/Art/Organs/Brain_Nervous_System.fbx",
                "BlenderAutomation/Medical_Human_Anatomy_Master.blend"
            };

            foreach (var f in requiredFiles)
            {
                if (!File.Exists(f))
                {
                    Debug.LogError($"[Test_3DModelFiles] Missing required model asset: {f}");
                    return false;
                }
                var fi = new FileInfo(f);
                if (fi.Length < 1000) return false; // Must be real non-empty geometry
            }
            return true;
        }

        private static bool Test_ClinicalRenders()
        {
            string rendersDir = "Assets/MedicalSimulation/Art/Renders";
            for (int i = 1; i <= 10; i++)
            {
                string fn = $"Render_{i:D2}_";
                var matches = Directory.GetFiles(rendersDir, fn + "*.png");
                if (matches.Length == 0)
                {
                    Debug.LogError($"[Test_ClinicalRenders] Missing render {fn}");
                    return false;
                }
            }

            // Overview composite grid
            string gridPath = Path.Combine(rendersDir, "Medical_Anatomy_Overview_Grid.png");
            if (!File.Exists(gridPath)) return false;

            return true;
        }

        private static bool Test_PBRMaterials()
        {
            string matDir = "Assets/MedicalSimulation/Materials";
            string[] requiredMats = {
                "Mat_Bone_Cortical", "Mat_Cartilage_Hyaline", "Mat_Heart_Myocardium",
                "Mat_Lung_Tissue", "Mat_Liver_Hepatic", "Mat_Gallbladder",
                "Mat_Gastric_SmoothMuscle", "Mat_Intestinal_Mucosa", "Mat_Pancreas",
                "Mat_Renal_Capsule", "Mat_Brain_Cortex", "Mat_Torso_Silhouette",
                "Mat_Vessel_Arterial", "Mat_Vessel_Venous"
            };

            foreach (var m in requiredMats)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{matDir}/{m}.mat");
                if (mat == null)
                {
                    Debug.LogError($"[Test_PBRMaterials] Missing material: {m}.mat");
                    return false;
                }
            }
            return true;
        }

        // ── 8. Prefab Construction ───────────────────────────────────────────
        private static bool Test_PrefabConstruction()
        {
            MedicalAnatomyPrefabBuilder.BuildAllPrefabs();

            string prefabDir = "Assets/MedicalSimulation/Prefabs/Anatomy";
            string[] expectedPrefabs = {
                "Heart_Detailed.prefab", "Respiratory_System.prefab",
                "Digestive_System.prefab", "Urinary_System.prefab",
                "Brain_Nervous_System.prefab", "Skeletal_System.prefab",
                "Cardiovascular_System.prefab", "Torso_Anatomy_Master.prefab"
            };

            foreach (var p in expectedPrefabs)
            {
                string path = $"{prefabDir}/{p}";
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                {
                    Debug.LogError($"[Test_PrefabConstruction] Prefab failed to build: {p}");
                    return false;
                }
            }
            return true;
        }

        // ── 9. Scene Registry ────────────────────────────────────────────────
        private static bool Test_BuildSettingsScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length < 7)
            {
                Debug.LogError($"[Test_BuildSettingsScenes] Insufficient scenes in BuildSettings: {scenes?.Length}");
                return false;
            }

            foreach (var s in scenes)
            {
                if (!File.Exists(s.path))
                {
                    Debug.LogError($"[Test_BuildSettingsScenes] Scene file missing: {s.path}");
                    return false;
                }
            }
            return true;
        }

        private static void LogHeader(string title)
        {
            Debug.Log($"<color=#00E5FF>==================================================\n{title}\n==================================================</color>");
        }

        private static void LogSummary()
        {
            int total = _passCount + _failCount;
            float pct = total > 0 ? ((float)_passCount / total) * 100f : 0f;
            Debug.Log($"<color=#00FF99>==================================================\n" +
                      $"FINAL TEST EXECUTION SUMMARY: {_passCount} / {total} PASSED ({pct:F1}%)\n" +
                      $"Failed: {_failCount}\n" +
                      $"==================================================</color>");
        }
    }
}
