using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Singleton application manager. Persists across scene loads.
    /// Manages scene navigation, session progress, and quality settings.
    /// </summary>
    public class MedicalAppManager : MonoBehaviour
    {
        public static MedicalAppManager Instance { get; private set; }

        [Header("Scene Indices (must match Build Settings order)")]
        [SerializeField] private int bootstrapSceneIndex = 0;
        [SerializeField] private int mainMenuSceneIndex = 1;
        [SerializeField] private int storyIntroSceneIndex = 2;
        [SerializeField] private int dentalClinicSceneIndex = 3;
        [SerializeField] private int anatomyLabSceneIndex = 4;
        [SerializeField] private int diagnosisLabSceneIndex = 5;
        [SerializeField] private int procedureLabSceneIndex = 6;
        [SerializeField] private int finalAssessmentSceneIndex = 7;
        [SerializeField] private int operatingTheatreSceneIndex = 8;
        [SerializeField] private int legacyMenuSceneIndex = 9;
        [SerializeField] private int heartSceneIndex = 10;
        [SerializeField] private int bloodflowSceneIndex = 11;
        [SerializeField] private int teethSceneIndex = 12;
        [SerializeField] private int liverCutSceneIndex = 13;
        [SerializeField] private int liverGrabSceneIndex = 14;
        [SerializeField] private int nasalSceneIndex = 15;

        // ── Progress tracking ────────────────────────────────────────────────
        public int ModulesCompleted    { get; private set; }
        public int ProceduresPracticed { get; private set; }
        public int AssessmentsCompleted{ get; private set; }
        public float AverageScore      { get; private set; }

        private float _totalScore;
        private int   _scoreCount;

        // ── Events ───────────────────────────────────────────────────────────
        public event Action<string> OnSceneTransitionRequested;

        // ── Quality ──────────────────────────────────────────────────────────
        public enum QualityMode { Desktop, VR }
        public QualityMode CurrentQualityMode { get; private set; } = QualityMode.Desktop;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        // ── Scene navigation ─────────────────────────────────────────────────

        public void GoToBootstrap()         => LoadScene(bootstrapSceneIndex);
        public void GoToMainMenu()          => LoadScene(mainMenuSceneIndex);
        public void GoToStoryIntro()        => LoadScene(storyIntroSceneIndex);
        public void GoToDentalClinic()      => LoadScene(dentalClinicSceneIndex);
        public void GoToAnatomyLab()        => LoadScene(anatomyLabSceneIndex);
        public void GoToDiagnosisLab()      => LoadScene(diagnosisLabSceneIndex);
        public void GoToProcedureLab()      => LoadScene(procedureLabSceneIndex);
        public void GoToFinalAssessment()   => LoadScene(finalAssessmentSceneIndex);
        public void GoToOperatingTheatre()  => LoadScene(operatingTheatreSceneIndex);
        public void GoToHeartScene()        => LoadScene(heartSceneIndex);
        public void GoToBloodflowScene()    => LoadScene(bloodflowSceneIndex);
        public void GoToTeethScene()        => LoadScene(teethSceneIndex);
        public void GoToLiverCutScene()     => LoadScene(liverCutSceneIndex);
        public void GoToLiverGrabScene()    => LoadScene(liverGrabSceneIndex);
        public void GoToNasalScene()        => LoadScene(nasalSceneIndex);

        public void LoadScene(int index)
        {
            if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning($"[MedicalAppManager] Scene index {index} is out of range.");
                return;
            }
            OnSceneTransitionRequested?.Invoke(SceneManager.GetSceneByBuildIndex(index).name);
            SceneManager.LoadScene(index);
        }

        public void LoadSceneByName(string sceneName)
        {
            OnSceneTransitionRequested?.Invoke(sceneName);
            SceneManager.LoadScene(sceneName);
        }

        // ── Progress ─────────────────────────────────────────────────────────

        public void RecordModuleCompleted()
        {
            ModulesCompleted++;
            SaveProgress();
        }

        public void RecordProcedurePracticed()
        {
            ProceduresPracticed++;
            SaveProgress();
        }

        public void RecordAssessmentCompleted(float score)
        {
            AssessmentsCompleted++;
            _totalScore += Mathf.Clamp01(score);
            _scoreCount++;
            AverageScore = _scoreCount > 0 ? _totalScore / _scoreCount : 0f;
            SaveProgress();
        }

        // ── Quality settings ─────────────────────────────────────────────────

        public void SetQualityMode(QualityMode mode)
        {
            CurrentQualityMode = mode;
            MedicalQualitySettings.Apply(mode);
            Debug.Log($"[MedicalAppManager] Quality mode set to: {mode}");
        }

        // ── Persistence (PlayerPrefs) ─────────────────────────────────────────

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("ModulesCompleted",     ModulesCompleted);
            PlayerPrefs.SetInt("ProceduresPracticed",  ProceduresPracticed);
            PlayerPrefs.SetInt("AssessmentsCompleted", AssessmentsCompleted);
            PlayerPrefs.SetFloat("TotalScore",         _totalScore);
            PlayerPrefs.SetInt("ScoreCount",           _scoreCount);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            ModulesCompleted     = PlayerPrefs.GetInt("ModulesCompleted",     0);
            ProceduresPracticed  = PlayerPrefs.GetInt("ProceduresPracticed",  0);
            AssessmentsCompleted = PlayerPrefs.GetInt("AssessmentsCompleted", 0);
            _totalScore          = PlayerPrefs.GetFloat("TotalScore",         0f);
            _scoreCount          = PlayerPrefs.GetInt("ScoreCount",           0);
            AverageScore = _scoreCount > 0 ? _totalScore / _scoreCount : 0f;
        }

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            ModulesCompleted = ProceduresPracticed = AssessmentsCompleted = _scoreCount = 0;
            _totalScore = AverageScore = 0f;
            SaveProgress();
        }
    }
}
