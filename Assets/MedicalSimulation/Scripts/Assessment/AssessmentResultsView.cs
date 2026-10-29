using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.Core;
using MedicalSimulation.Audio;

namespace MedicalSimulation.Assessment
{
    /// <summary>
    /// Displays and drives the diagnostic clinical assessment interface.
    /// Connects directly to MedicalAssessmentEngine and MedicalAppManager.
    /// Can bind to pre-existing Canvas elements or auto-generate on start.
    /// </summary>
    public class AssessmentResultsView : MonoBehaviour
    {
        [Header("Engine Reference")]
        [SerializeField] private MedicalAssessmentEngine assessmentEngine;

        [Header("UI Containers")]
        [SerializeField] private GameObject caseContainer;
        [SerializeField] private GameObject scorecardContainer;

        [Header("Case Details")]
        [SerializeField] private TextMeshProUGUI caseTitleText;
        [SerializeField] private TextMeshProUGUI vitalsText;
        [SerializeField] private TextMeshProUGUI vignetteText;

        [Header("Question & Options")]
        [SerializeField] private TextMeshProUGUI questionPromptText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TextMeshProUGUI[] optionTexts;

        [Header("Feedback Panel")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI feedbackBannerText;
        [SerializeField] private TextMeshProUGUI feedbackRationaleText;
        [SerializeField] private Button nextQuestionButton;

        [Header("Final Scorecard")]
        [SerializeField] private TextMeshProUGUI finalScorePercentageText;
        [SerializeField] private TextMeshProUGUI finalScoreFractionText;
        [SerializeField] private TextMeshProUGUI clinicalRecommendationText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnHomeButton;

        private bool _hasAnsweredCurrent = false;

        void Awake()
        {
            if (assessmentEngine == null)
            {
                assessmentEngine = GetComponent<MedicalAssessmentEngine>() ?? gameObject.AddComponent<MedicalAssessmentEngine>();
            }

            if (caseContainer == null)
            {
                BuildAssessmentUI();
            }
        }

        void OnEnable()
        {
            if (assessmentEngine != null)
            {
                assessmentEngine.OnQuestionPresented += DisplayQuestion;
                assessmentEngine.OnAnswerEvaluated += DisplayFeedback;
                assessmentEngine.OnAssessmentComplete += DisplayScorecard;
            }

            nextQuestionButton?.onClick.AddListener(OnNextClicked);
            restartButton?.onClick.AddListener(OnRestartClicked);
            returnHomeButton?.onClick.AddListener(() => MedicalAppManager.Instance?.GoToMainMenu());
        }

        void OnDisable()
        {
            if (assessmentEngine != null)
            {
                assessmentEngine.OnQuestionPresented -= DisplayQuestion;
                assessmentEngine.OnAnswerEvaluated -= DisplayFeedback;
                assessmentEngine.OnAssessmentComplete -= DisplayScorecard;
            }
        }

        // ── Question Presentation ─────────────────────────────────────────────

        public void DisplayQuestion(MedicalAssessmentEngine.ClinicalCase cCase, MedicalAssessmentEngine.DiagnosticQuestion question)
        {
            _hasAnsweredCurrent = false;
            caseContainer?.SetActive(true);
            scorecardContainer?.SetActive(false);
            feedbackPanel?.SetActive(false);

            if (caseTitleText != null) caseTitleText.text = $"<b>CASE STUDY:</b> <color=#00E5FF>{cCase.title.ToUpper()}</color>";
            if (vitalsText != null) vitalsText.text = $"<color=#00FF99>Vitals: </color>{cCase.patientVitals}";
            if (vignetteText != null) vignetteText.text = cCase.clinicalPresentation;
            if (questionPromptText != null) questionPromptText.text = question.questionText;

            // Setup options
            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    int capturedIndex = i;
                    if (i < question.options.Length)
                    {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        if (optionTexts != null && i < optionTexts.Length)
                        {
                            optionTexts[i].text = $"<b>{(char)('A' + i)}.</b> {question.options[i]}";
                        }

                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionSelected(capturedIndex));
                    }
                    else
                    {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnOptionSelected(int index)
        {
            if (_hasAnsweredCurrent) return;
            _hasAnsweredCurrent = true;

            // Lock all buttons
            if (optionButtons != null)
            {
                foreach (var b in optionButtons) b.interactable = false;
            }

            assessmentEngine.SubmitAnswer(index);
        }

        // ── Feedback Display ──────────────────────────────────────────────────

        public void DisplayFeedback(bool isCorrect, string rationale)
        {
            feedbackPanel?.SetActive(true);

            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = isCorrect
                    ? "<color=#00FF66>✓ CORRECT DIAGNOSTIC REASONING</color>"
                    : "<color=#FF5555>✗ CLINICALLY INCORRECT SELECTION</color>";
            }

            if (feedbackRationaleText != null)
            {
                feedbackRationaleText.text = $"<b>Clinical Teaching Point:</b>\n{rationale}";
            }
        }

        private void OnNextClicked()
        {
            MedicalAudioController.Instance?.PlayClick();
            assessmentEngine.NextQuestion();
        }

        // ── Final Scorecard ───────────────────────────────────────────────────

        public void DisplayScorecard(float scorePercentage, int correct, int total)
        {
            caseContainer?.SetActive(false);
            feedbackPanel?.SetActive(false);
            scorecardContainer?.SetActive(true);

            float pct = scorePercentage * 100f;
            if (finalScorePercentageText != null)
                finalScorePercentageText.text = $"{pct:F0}%";

            if (finalScoreFractionText != null)
                finalScoreFractionText.text = $"Diagnoses Correct: <b>{correct} / {total}</b>";

            if (clinicalRecommendationText != null)
            {
                if (pct >= 85f)
                    clinicalRecommendationText.text = "<b>Mastery Rating: CLINICAL HONORS</b>\nOutstanding mastery of anatomical localization, vascular perfusion territories, and emergency presentation signs.";
                else if (pct >= 60f)
                    clinicalRecommendationText.text = "<b>Mastery Rating: CLINICALLY COMPETENT</b>\nSatisfactory diagnostic reasoning. Recommended: Review coronary artery distribution and dental periapical pathologies.";
                else
                    clinicalRecommendationText.text = "<b>Mastery Rating: REMEDIATION RECOMMENDED</b>\nDiagnostic accuracy is below passing threshold. Re-examine the 3D organ structures and vascular circulation paths in Anatomy Lab.";
            }
        }

        private void OnRestartClicked()
        {
            MedicalAudioController.Instance?.PlayConfirm();
            assessmentEngine.RestartAssessment();
        }

        // ── Procedural UI Fallback Builder ────────────────────────────────────

        private void BuildAssessmentUI()
        {
            var canvasGo = new GameObject("Assessment_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Case Container
            caseContainer = CreatePanel("CaseContainer", canvasGo.transform,
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f),
                new Color(0.04f, 0.08f, 0.14f, 0.95f));

            caseTitleText = CreateText("CaseTitle", caseContainer.transform, "CASE STUDY",
                new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f), 20, TextAlignmentOptions.MidlineLeft, Color.cyan);

            vitalsText = CreateText("Vitals", caseContainer.transform, "Vitals: Normal",
                new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.89f), 14, TextAlignmentOptions.MidlineLeft, Color.green);

            vignetteText = CreateText("Vignette", caseContainer.transform, "Patient clinical presentation...",
                new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.80f), 14, TextAlignmentOptions.TopLeft, Color.white);

            questionPromptText = CreateText("Prompt", caseContainer.transform, "Question prompt...",
                new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.60f), 16, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.85f, 0.2f));

            // Options Buttons
            optionButtons = new Button[4];
            optionTexts = new TextMeshProUGUI[4];
            float y = 0.40f;
            for (int i = 0; i < 4; i++)
            {
                var btnGo = CreateButtonObj($"Option_{i}", caseContainer.transform,
                    new Vector2(0.04f, y - 0.07f), new Vector2(0.96f, y), out optionTexts[i]);
                optionButtons[i] = btnGo.GetComponent<Button>();
                y -= 0.085f;
            }

            // Feedback panel
            feedbackPanel = CreatePanel("FeedbackPanel", caseContainer.transform,
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.18f),
                new Color(0.06f, 0.14f, 0.20f, 0.98f));

            feedbackBannerText = CreateText("Banner", feedbackPanel.transform, "CORRECT",
                new Vector2(0.03f, 0.65f), new Vector2(0.70f, 0.95f), 15, TextAlignmentOptions.MidlineLeft, Color.white);

            feedbackRationaleText = CreateText("Rationale", feedbackPanel.transform, "Rationale details...",
                new Vector2(0.03f, 0.05f), new Vector2(0.75f, 0.60f), 12, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.95f, 1f));

            var nextBtnGo = CreateButtonObj("NextBtn", feedbackPanel.transform,
                new Vector2(0.78f, 0.15f), new Vector2(0.96f, 0.85f), out var nextTxt);
            nextTxt.text = "Next Case →";
            nextQuestionButton = nextBtnGo.GetComponent<Button>();

            // Scorecard Container
            scorecardContainer = CreatePanel("ScorecardContainer", canvasGo.transform,
                new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.80f),
                new Color(0.04f, 0.09f, 0.16f, 0.98f));
            scorecardContainer.SetActive(false);

            CreateText("ScoreTitle", scorecardContainer.transform, "<b>ASSESSMENT SUMMARY</b>",
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f), 22, TextAlignmentOptions.Center, Color.cyan);

            finalScorePercentageText = CreateText("Pct", scorecardContainer.transform, "100%",
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f), 48, TextAlignmentOptions.Center, Color.white);

            finalScoreFractionText = CreateText("Frac", scorecardContainer.transform, "Diagnoses: 4 / 4",
                new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.58f), 18, TextAlignmentOptions.Center, new Color(0.7f, 0.9f, 1f));

            clinicalRecommendationText = CreateText("Rec", scorecardContainer.transform, "Clinical Recommendations...",
                new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.45f), 14, TextAlignmentOptions.Center, Color.white);

            var restartBtn = CreateButtonObj("RestartBtn", scorecardContainer.transform,
                new Vector2(0.15f, 0.05f), new Vector2(0.45f, 0.15f), out var rTxt);
            rTxt.text = "Try Again";
            restartButton = restartBtn.GetComponent<Button>();

            var homeBtn = CreateButtonObj("HomeBtn", scorecardContainer.transform,
                new Vector2(0.55f, 0.05f), new Vector2(0.85f, 0.15f), out var hTxt);
            hTxt.text = "Main Menu";
            returnHomeButton = homeBtn.GetComponent<Button>();
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, Vector2 min, Vector2 max, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.alignment = align; tmp.color = color;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private GameObject CreateButtonObj(string name, Transform parent, Vector2 min, Vector2 max, out TextMeshProUGUI textComp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.22f, 0.32f, 0.95f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0f, 0.85f, 0.95f, 1f);
            btn.colors = colors;
            textComp = CreateText("Label", go.transform, name, Vector2.zero, Vector2.one, 14, TextAlignmentOptions.MidlineLeft, Color.white);
            return go;
        }
    }
}
