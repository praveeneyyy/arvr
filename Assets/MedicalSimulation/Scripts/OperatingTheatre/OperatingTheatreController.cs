using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.UI;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Core;
using MedicalSimulation.AI;

namespace MedicalSimulation.OperatingTheatre
{
    /// <summary>
    /// Master technical director and orchestrator for the 3D Virtual Operating Theatre.
    /// Manages room setup, surgical team characters, patient position, equipment rigs,
    /// camera navigation modes, and interactive procedure simulation.
    /// </summary>
    public class OperatingTheatreController : MonoBehaviour
    {
        public static OperatingTheatreController Instance { get; private set; }

        public enum ViewpointMode
        {
            SurgeonView,
            PatientCloseUp,
            AnesthesiaStation,
            InstrumentTray,
            TheatreOverview
        }

        [Header("Room & Scene Hierarchy")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private Transform operatingTableRoot;
        [SerializeField] private Transform surgicalTeamRoot;
        [SerializeField] private Transform equipmentRoot;

        [Header("Core Sub-Controllers")]
        [SerializeField] private PatientModelController patientController;
        [SerializeField] private ClinicalMonitorController monitorController;
        [SerializeField] private MedicalEquipmentController equipmentController;
        [SerializeField] private SurgicalProcedureController procedureController;
        [SerializeField] private MedicalAudioController audioController;
        [SerializeField] private AnatomyCameraController cameraController;

        [Header("Camera & Viewpoints")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ViewpointMode currentViewpoint = ViewpointMode.SurgeonView;

        [Header("UI Overlay Containers")]
        [SerializeField] private Canvas theatreCanvas;
        [SerializeField] private TextMeshProUGUI stageTitleText;
        [SerializeField] private TextMeshProUGUI stageObjectiveText;
        [SerializeField] private TextMeshProUGUI stageHintText;
        [SerializeField] private TextMeshProUGUI scoreText;

        // Viewpoint transform targets
        private readonly Dictionary<ViewpointMode, (Vector3 pos, Vector3 rot)> _viewpoints = new()
        {
            { ViewpointMode.SurgeonView,       (new Vector3(0.75f, 1.55f, 0.35f),  new Vector3(32f, -110f, 0f)) },
            { ViewpointMode.PatientCloseUp,    (new Vector3(0.0f, 1.45f, 0.35f),   new Vector3(65f, -90f, 0f)) },
            { ViewpointMode.AnesthesiaStation, (new Vector3(-0.95f, 1.60f, 0.95f), new Vector3(20f, 135f, 0f)) },
            { ViewpointMode.InstrumentTray,    (new Vector3(0.85f, 1.35f, -0.65f), new Vector3(35f, -45f, 0f)) },
            { ViewpointMode.TheatreOverview,   (new Vector3(2.40f, 2.20f, 1.80f),  new Vector3(28f, -145f, 0f)) }
        };

        void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureControllers();
            BuildTheatreHierarchy();
            BuildTheatreHUD();
        }

        void Start()
        {
            SetViewpoint(currentViewpoint);

            if (procedureController != null)
            {
                procedureController.OnStageAdvanced += HandleStageAdvanced;
                procedureController.OnScoreUpdated += HandleScoreUpdated;
                UpdateHUDStageInfo(procedureController.GetCurrentStageData());
            }

            if (patientController != null)
            {
                patientController.OnOrganClicked += HandleOrganClicked;
            }
        }

        private void EnsureControllers()
        {
            if (patientController == null) patientController = GetComponentInChildren<PatientModelController>() ?? gameObject.AddComponent<PatientModelController>();
            if (monitorController == null) monitorController = GetComponentInChildren<ClinicalMonitorController>() ?? gameObject.AddComponent<ClinicalMonitorController>();
            if (equipmentController == null) equipmentController = GetComponentInChildren<MedicalEquipmentController>() ?? gameObject.AddComponent<MedicalEquipmentController>();
            if (procedureController == null) procedureController = GetComponentInChildren<SurgicalProcedureController>() ?? gameObject.AddComponent<SurgicalProcedureController>();
            if (audioController == null) audioController = GetComponentInChildren<MedicalAudioController>() ?? gameObject.AddComponent<MedicalAudioController>();

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null && cameraController == null)
            {
                cameraController = mainCamera.GetComponent<AnatomyCameraController>() ?? mainCamera.gameObject.AddComponent<AnatomyCameraController>();
            }
        }

        public void BuildTheatreHierarchy()
        {
            // 1. Operating Table & Patient Setup
            if (operatingTableRoot == null)
            {
                var tableGo = new GameObject("OperatingTable_Assembly");
                tableGo.transform.SetParent(transform, false);
                tableGo.transform.localPosition = new Vector3(0, 0.95f, 0.35f);
                operatingTableRoot = tableGo.transform;

                // Load or instantiate patient character
                var patientPrefab = Resources.Load<GameObject>("Characters/Character_Patient_OperatingTable");
                #if UNITY_EDITOR
                if (patientPrefab == null)
                {
                    patientPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MedicalSimulation/Art/Characters/Character_Patient_OperatingTable.fbx");
                }
                #endif
                if (patientPrefab != null)
                {
                    var pInstance = Instantiate(patientPrefab, operatingTableRoot);
                    pInstance.name = "Patient_Supine_Mesh";
                }
            }

            // 2. Surgical Team Characters
            if (surgicalTeamRoot == null)
            {
                var teamGo = new GameObject("Surgical_Team_Characters");
                teamGo.transform.SetParent(transform, false);
                surgicalTeamRoot = teamGo.transform;

                SpawnCharacter("Character_Surgeon_Lead", new Vector3(0.65f, 0, 0.35f), Quaternion.Euler(0, -90f, 0), surgicalTeamRoot);
                SpawnCharacter("Character_Surgeon_Assistant", new Vector3(-0.65f, 0, 0.35f), Quaternion.Euler(0, 90f, 0), surgicalTeamRoot);
                SpawnCharacter("Character_Scrub_Nurse", new Vector3(0.65f, 0, -0.65f), Quaternion.Euler(0, -45f, 0), surgicalTeamRoot);
                SpawnCharacter("Character_Anesthesiologist", new Vector3(-0.75f, 0, 1.10f), Quaternion.Euler(0, 135f, 0), surgicalTeamRoot);
            }

            // 3. Clinical Lighting
            EnsureOperatingRoomLighting();
        }

        private GameObject SpawnCharacter(string name, Vector3 pos, Quaternion rot, Transform parent)
        {
            GameObject prefab = null;
            #if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/MedicalSimulation/Art/Characters/{name}.fbx");
            #endif
            if (prefab == null) prefab = Resources.Load<GameObject>($"Characters/{name}");

            if (prefab != null)
            {
                var inst = Instantiate(prefab, parent);
                inst.name = name;
                inst.transform.localPosition = pos;
                inst.transform.localRotation = rot;
                return inst;
            }
            return null;
        }

        private void EnsureOperatingRoomLighting()
        {
            var existingLights = GetComponentsInChildren<Light>();
            if (existingLights.Length == 0)
            {
                // Main Surgical Overhead Dual Spotlights
                var lamp1Go = new GameObject("Surgical_Overhead_Spot_01");
                lamp1Go.transform.SetParent(transform, false);
                lamp1Go.transform.position = new Vector3(0.2f, 2.7f, 0.35f);
                lamp1Go.transform.rotation = Quaternion.Euler(85f, -15f, 0);
                var l1 = lamp1Go.AddComponent<Light>();
                l1.type = LightType.Spot;
                l1.color = new Color(0.96f, 0.98f, 1.0f);
                l1.intensity = 2.4f;
                l1.spotAngle = 42f;
                l1.range = 6.0f;
                l1.shadows = LightShadows.Soft;

                var lamp2Go = new GameObject("Surgical_Overhead_Spot_02");
                lamp2Go.transform.SetParent(transform, false);
                lamp2Go.transform.position = new Vector3(-0.2f, 2.65f, 0.40f);
                lamp2Go.transform.rotation = Quaternion.Euler(82f, 20f, 0);
                var l2 = lamp2Go.AddComponent<Light>();
                l2.type = LightType.Spot;
                l2.color = new Color(0.94f, 0.97f, 1.0f);
                l2.intensity = 1.8f;
                l2.spotAngle = 46f;
                l2.range = 6.0f;
                l2.shadows = LightShadows.Soft;

                // Room Ambient Fill Light
                var fillGo = new GameObject("OR_Room_Ambient_Fill");
                fillGo.transform.SetParent(transform, false);
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = new Color(0.72f, 0.85f, 0.95f);
                fill.intensity = 0.65f;
                fillGo.transform.rotation = Quaternion.Euler(45f, 130f, 0);
            }
        }

        // =========================================================================
        // VIEWPOINT NAVIGATION
        // =========================================================================

        public void SetViewpoint(ViewpointMode mode)
        {
            currentViewpoint = mode;
            if (_viewpoints.TryGetValue(mode, out var vp))
            {
                if (mainCamera != null)
                {
                    mainCamera.transform.position = vp.pos;
                    mainCamera.transform.rotation = Quaternion.Euler(vp.rot);
                }
                if (cameraController != null)
                {
                    cameraController.SetOrbitTarget(new Vector3(0, 0.95f, 0.35f));
                }
            }
        }

        // =========================================================================
        // THEATRE HUD OVERLAY
        // =========================================================================

        private void BuildTheatreHUD()
        {
            theatreCanvas = GetComponentInChildren<Canvas>();
            if (theatreCanvas == null)
            {
                var canvasGo = new GameObject("OperatingTheatre_HUDCanvas");
                canvasGo.transform.SetParent(transform, false);
                theatreCanvas = canvasGo.AddComponent<Canvas>();
                theatreCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                theatreCanvas.sortingOrder = 55;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Top Status Bar: Patient Case & Active Procedure
            var topBar = CreateUIRect("TopStatusBar", theatreCanvas.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 52));
            topBar.AddComponent<Image>().color = MedicalUITheme.SurfaceSecondary;

            var titleTmp = CreateUIRect("Title", topBar.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(400, 36)).AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#00E5FF>VIRTUAL OPERATING THEATRE</color></b> <size=80%><color=#94A3B8>| OR Suite 4</color></size>";
            titleTmp.fontSize = 15;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = MedicalUITheme.TextPrimary;

            scoreText = CreateUIRect("Score", topBar.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(260, 32)).AddComponent<TextMeshProUGUI>();
            scoreText.text = "<color=#14B8A6>SCORE: 100/100</color>  <size=80%><color=#64748B>| PROTOCOL PASS</color></size>";
            scoreText.fontSize = 13;
            scoreText.alignment = TextAlignmentOptions.MidlineRight;

            // Left Panel: Procedural Milestones & Objectives
            var leftPanel = CreateUIRect("ProcedurePanel", theatreCanvas.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(340, 480));
            leftPanel.AddComponent<Image>().color = MedicalUITheme.SurfaceSecondary;
            var vLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(16, 16, 16, 16);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;

            stageTitleText = CreateUIRect("StageTitle", leftPanel.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 30)).AddComponent<TextMeshProUGUI>();
            stageTitleText.fontSize = 14;
            stageTitleText.color = MedicalUITheme.AccentCyan;

            stageObjectiveText = CreateUIRect("StageObjective", leftPanel.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 80)).AddComponent<TextMeshProUGUI>();
            stageObjectiveText.fontSize = 11;
            stageObjectiveText.color = MedicalUITheme.TextPrimary;

            stageHintText = CreateUIRect("StageHint", leftPanel.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 60)).AddComponent<TextMeshProUGUI>();
            stageHintText.fontSize = 10;
            stageHintText.color = MedicalUITheme.TextSecondary;

            CreateBtn("Advance Surgical Step", MedicalUITheme.AccentCyan, Color.black, leftPanel.transform, () => {
                procedureController?.AdvanceToNextStage();
            });

            // Bottom Toolbar: Viewpoints & Anatomical Layers
            BuildBottomControlDock(theatreCanvas.transform);
        }

        private void BuildBottomControlDock(Transform parent)
        {
            var dockGo = CreateUIRect("BottomTheatreDock", parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(860, 48));
            dockGo.AddComponent<Image>().color = MedicalUITheme.SurfaceElevated;
            var hLayout = dockGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 6, 6);
            hLayout.spacing = 8;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = true;

            // Viewpoint Buttons
            CreateBtn("Surgeon View", MedicalUITheme.SurfaceSecondary, MedicalUITheme.TextPrimary, dockGo.transform, () => SetViewpoint(ViewpointMode.SurgeonView));
            CreateBtn("Patient Close-Up", MedicalUITheme.SurfaceSecondary, MedicalUITheme.TextPrimary, dockGo.transform, () => SetViewpoint(ViewpointMode.PatientCloseUp));
            CreateBtn("Anesthesia Station", MedicalUITheme.SurfaceSecondary, MedicalUITheme.TextPrimary, dockGo.transform, () => SetViewpoint(ViewpointMode.AnesthesiaStation));
            CreateBtn("Instrument Tray", MedicalUITheme.SurfaceSecondary, MedicalUITheme.TextPrimary, dockGo.transform, () => SetViewpoint(ViewpointMode.InstrumentTray));
            CreateBtn("OR Overview", MedicalUITheme.SurfaceSecondary, MedicalUITheme.TextPrimary, dockGo.transform, () => SetViewpoint(ViewpointMode.TheatreOverview));

            // Anatomy Layer Toggle Button
            CreateBtn("Anatomy Layers", MedicalUITheme.AccentTeal, Color.black, dockGo.transform, CycleAnatomyLayer);
        }

        private int _layerIndex = 0;
        private void CycleAnatomyLayer()
        {
            if (patientController == null) return;
            var modes = (PatientModelController.AnatomicalViewMode[])Enum.GetValues(typeof(PatientModelController.AnatomicalViewMode));
            _layerIndex = (_layerIndex + 1) % modes.Length;
            patientController.SetViewMode(modes[_layerIndex]);
        }

        private void HandleStageAdvanced(SurgicalProcedureController.ProcedureStage stage, SurgicalProcedureController.StageData data)
        {
            UpdateHUDStageInfo(data);
        }

        private void UpdateHUDStageInfo(SurgicalProcedureController.StageData data)
        {
            if (stageTitleText != null) stageTitleText.text = $"<b>{data.title}</b>";
            if (stageObjectiveText != null) stageObjectiveText.text = $"<b>Objective:</b> {data.clinicalObjective}";
            if (stageHintText != null) stageHintText.text = $"<i>Clinical Protocol:</i> {data.clinicalHint}";
        }

        private void HandleScoreUpdated(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"<color=#14B8A6>SCORE: {newScore}/100</color>";
            }
        }

        private void HandleOrganClicked(OrganInteractable organ)
        {
            if (organ == null) return;
            // Ask AI or display organ metadata
            Debug.Log($"[OperatingTheatre] Selected organ: {organ.name}");
        }

        private GameObject CreateBtn(string label, Color bg, Color txtColor, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var btnGo = CreateUIRect(label.Replace(" ", "_"), parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 36));
            btnGo.AddComponent<Image>().color = bg;
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var tmp = CreateUIRect("Text", btnGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = txtColor;
            return btnGo;
        }

        private GameObject CreateUIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }
    }
}
