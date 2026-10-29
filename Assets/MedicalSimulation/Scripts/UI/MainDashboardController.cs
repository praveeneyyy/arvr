using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.Core;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// Main dashboard controller for the main menu / landing scene (1.unity).
    /// Wires navigation buttons to MedicalAppManager and displays real session progress.
    /// Never invents user statistics.
    /// </summary>
    public class MainDashboardController : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] private Button continueLearningButton;
        [SerializeField] private Button exploreAnatomyButton;
        [SerializeField] private Button anatomyLabButton;
        [SerializeField] private Button procedureLabButton;
        [SerializeField] private Button diagnosisLabButton;
        [SerializeField] private Button arVrModeButton;
        [SerializeField] private Button assessmentButton;
        [SerializeField] private Button settingsButton;

        [Header("Specific Simulation Buttons")]
        [SerializeField] private Button heartSimButton;
        [SerializeField] private Button bloodflowSimButton;
        [SerializeField] private Button teethSimButton;
        [SerializeField] private Button liverCutSimButton;
        [SerializeField] private Button liverGrabSimButton;
        [SerializeField] private Button nasalSimButton;

        [Header("Progress Display (real values only)")]
        [SerializeField] private TextMeshProUGUI modulesCompletedText;
        [SerializeField] private TextMeshProUGUI proceduresPracticedText;
        [SerializeField] private TextMeshProUGUI assessmentsCompletedText;
        [SerializeField] private TextMeshProUGUI averageScoreText;

        [Header("Quality Toggle")]
        [SerializeField] private Button desktopQualityButton;
        [SerializeField] private Button vrQualityButton;

        private MedicalAppManager _app;

        void Start()
        {
            _app = MedicalAppManager.Instance;

            if (_app == null)
            {
                // Spawn the manager if not present (e.g., starting directly from this scene)
                var go = new GameObject("MedicalAppManager");
                go.AddComponent<MedicalAppManager>();
                _app = MedicalAppManager.Instance;
            }

            WireNavigationButtons();
            WireQualityButtons();
            RefreshProgressDisplay();
        }

        private void WireNavigationButtons()
        {
            if (_app == null) return;

            continueLearningButton?.onClick.AddListener(_app.GoToAnatomyLab);
            exploreAnatomyButton?.onClick.AddListener(_app.GoToAnatomyLab);
            anatomyLabButton?.onClick.AddListener(_app.GoToAnatomyLab);
            procedureLabButton?.onClick.AddListener(_app.GoToProcedureLab);
            diagnosisLabButton?.onClick.AddListener(_app.GoToDiagnosisLab);
            arVrModeButton?.onClick.AddListener(_app.GoToAnatomyLab); // Opens anatomy in XR
            assessmentButton?.onClick.AddListener(_app.GoToDiagnosisLab);
            settingsButton?.onClick.AddListener(OnSettings);

            // Specific simulation shortcuts (wired to existing scenes)
            heartSimButton?.onClick.AddListener(_app.GoToHeartScene);
            bloodflowSimButton?.onClick.AddListener(_app.GoToBloodflowScene);
            teethSimButton?.onClick.AddListener(_app.GoToTeethScene);
            liverCutSimButton?.onClick.AddListener(_app.GoToLiverCutScene);
            liverGrabSimButton?.onClick.AddListener(_app.GoToLiverGrabScene);
            nasalSimButton?.onClick.AddListener(_app.GoToNasalScene);
        }

        private void WireQualityButtons()
        {
            desktopQualityButton?.onClick.AddListener(() =>
                _app?.SetQualityMode(MedicalAppManager.QualityMode.Desktop));
            vrQualityButton?.onClick.AddListener(() =>
                _app?.SetQualityMode(MedicalAppManager.QualityMode.VR));
        }

        private void RefreshProgressDisplay()
        {
            if (_app == null) return;

            if (modulesCompletedText    != null)
                modulesCompletedText.text    = _app.ModulesCompleted.ToString();
            if (proceduresPracticedText != null)
                proceduresPracticedText.text = _app.ProceduresPracticed.ToString();
            if (assessmentsCompletedText!= null)
                assessmentsCompletedText.text= _app.AssessmentsCompleted.ToString();
            if (averageScoreText        != null)
                averageScoreText.text        = _app.AssessmentsCompleted > 0
                    ? $"{_app.AverageScore * 100f:F0}%"
                    : "—";
        }

        private void OnSettings()
        {
            // Settings panel toggle — extend with a SettingsPanel reference as needed
            Debug.Log("[MainDashboardController] Settings requested.");
        }
    }
}
