using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Automates the creation and configuration of the dedicated laboratory scenes
    /// (00_Bootstrap, 04_AnatomyLab, 05_DiagnosisLab, 06_ProcedureLab, 07_FinalAssessment)
    /// and registers them into EditorBuildSettings.
    /// </summary>
    public static class MedicalSceneSetupEngine
    {
#if UNITY_EDITOR
        [MenuItem("MedicalSimulation/Rebuild Master Clinical Workstation in Active Scene", false, 1)]
        public static void RebuildMasterClinicalWorkstation()
        {
            var existing = Object.FindFirstObjectByType<UI.MedicalWorkstationUIController>();
            if (existing == null)
            {
                var workstationGo = new GameObject("MedicalWorkstation_MasterController");
                existing = workstationGo.AddComponent<UI.MedicalWorkstationUIController>();
                Undo.RegisterCreatedObjectUndo(workstationGo, "Create Master Clinical Workstation");
            }
            existing.InitializeWorkstation();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("<color=#00E5FF>[MedicalSceneSetupEngine] Master Clinical Workstation successfully initialized in active scene!</color>");
        }

        [MenuItem("MedicalSimulation/Add AI Assistant to Active Scene")]
        public static void AddAIAssistantToActiveScene()
        {
            if (Object.FindFirstObjectByType<AI.MedicalAIOrchestrator>() == null)
            {
                var aiGo = new GameObject("MedicalAI_Assistant");
                aiGo.AddComponent<AI.MedicalAIOrchestrator>();
                aiGo.AddComponent<UI.MedicalAIChatUI>();
                aiGo.AddComponent<UI.MedicalAIChatController>();
                Undo.RegisterCreatedObjectUndo(aiGo, "Add Medical AI Assistant");
                Debug.Log("<color=#00FF99>[MedicalSceneSetupEngine] Added Medical AI Assistant to the active scene.</color>");
            }
            else
            {
                Debug.LogWarning("[MedicalSceneSetupEngine] Medical AI Assistant already exists in this scene.");
            }
        }

        [MenuItem("MedicalSimulation/Setup and Build Dedicated Laboratory Scenes")]
        public static void BuildAllDedicatedScenes()
        {
            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            CreateAnatomyLabScene($"{scenesDir}/04_AnatomyLab.unity");
            CreateDiagnosisLabScene($"{scenesDir}/05_DiagnosisLab.unity");
            CreateProcedureLabScene($"{scenesDir}/06_ProcedureLab.unity");
            CreateAssessmentScene($"{scenesDir}/07_FinalAssessment.unity");

            RegisterScenesInBuildSettings();
            Debug.Log("<color=#00FF99>[MedicalSceneSetupEngine] All dedicated scenes generated and registered in Build Settings!</color>");
        }

        private static void CreateAnatomyLabScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. App Manager
            var appGo = new GameObject("MedicalAppManager");
            appGo.AddComponent<MedicalAppManager>();

            // 2. Audio Controller
            var audioGo = new GameObject("MedicalAudioController");
            audioGo.AddComponent<Audio.MedicalAudioController>();

            // 3. Torso Assembly & Controller
            var anatomyRoot = new GameObject("AnatomyExplorer_Root");
            anatomyRoot.AddComponent<Anatomy.AnatomyModelController>();
            anatomyRoot.AddComponent<Anatomy.AnatomyAssemblyBuilder>();
            anatomyRoot.AddComponent<Anatomy.AnatomicalSlicePlaneController>();

            // 4. Runtime Clinical UI
            var uiGo = new GameObject("MedicalDashboardRuntimeUI");
            uiGo.AddComponent<UI.MedicalDashboardRuntimeUI>();

            // 5. Scene Bootstrap
            var bootGo = new GameObject("SceneBootstrap");
            var boot = bootGo.AddComponent<SceneBootstrap>();

            // 6. AI Assistant Orchestrator & UI
            var aiGo = new GameObject("MedicalAI_Assistant");
            aiGo.AddComponent<AI.MedicalAIOrchestrator>();
            aiGo.AddComponent<UI.MedicalAIChatUI>();
            aiGo.AddComponent<UI.MedicalAIChatController>();

            // Adjust Camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 0.4f, -1.8f);
                cam.transform.LookAt(new Vector3(0, 0.2f, 0));
                cam.backgroundColor = new Color(0.02f, 0.05f, 0.09f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.gameObject.AddComponent<Anatomy.AnatomyCameraController>();
            }

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MedicalSceneSetupEngine] Saved: {path}");
        }

        private static void CreateDiagnosisLabScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var appGo = new GameObject("MedicalAppManager");
            appGo.AddComponent<MedicalAppManager>();

            var audioGo = new GameObject("MedicalAudioController");
            audioGo.AddComponent<Audio.MedicalAudioController>();

            var diagGo = new GameObject("DiagnosisLab_Controller");
            diagGo.AddComponent<Assessment.MedicalAssessmentEngine>();
            diagGo.AddComponent<Assessment.AssessmentResultsView>();

            var bootGo = new GameObject("SceneBootstrap");
            bootGo.AddComponent<SceneBootstrap>();

            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.03f, 0.06f, 0.10f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MedicalSceneSetupEngine] Saved: {path}");
        }

        private static void CreateProcedureLabScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var appGo = new GameObject("MedicalAppManager");
            appGo.AddComponent<MedicalAppManager>();

            var audioGo = new GameObject("MedicalAudioController");
            audioGo.AddComponent<Audio.MedicalAudioController>();

            var procGo = new GameObject("ProcedureLab_Runner");
            procGo.AddComponent<ProcedureRunner>();

            var uiGo = new GameObject("MedicalDashboardRuntimeUI");
            uiGo.AddComponent<UI.MedicalDashboardRuntimeUI>();

            var bootGo = new GameObject("SceneBootstrap");
            bootGo.AddComponent<SceneBootstrap>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MedicalSceneSetupEngine] Saved: {path}");
        }

        private static void CreateAssessmentScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var appGo = new GameObject("MedicalAppManager");
            appGo.AddComponent<MedicalAppManager>();

            var audioGo = new GameObject("MedicalAudioController");
            audioGo.AddComponent<Audio.MedicalAudioController>();

            var assessGo = new GameObject("FinalAssessment_Controller");
            assessGo.AddComponent<Assessment.MedicalAssessmentEngine>();
            assessGo.AddComponent<Assessment.AssessmentResultsView>();

            var bootGo = new GameObject("SceneBootstrap");
            bootGo.AddComponent<SceneBootstrap>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MedicalSceneSetupEngine] Saved: {path}");
        }

        private static void RegisterScenesInBuildSettings()
        {
            var existingScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string[] newScenePaths = {
                "Assets/Scenes/04_AnatomyLab.unity",
                "Assets/Scenes/05_DiagnosisLab.unity",
                "Assets/Scenes/06_ProcedureLab.unity",
                "Assets/Scenes/07_FinalAssessment.unity"
            };

            foreach (var path in newScenePaths)
            {
                if (!existingScenes.Exists(s => s.path == path))
                {
                    existingScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = existingScenes.ToArray();
        }
#endif
    }
}
