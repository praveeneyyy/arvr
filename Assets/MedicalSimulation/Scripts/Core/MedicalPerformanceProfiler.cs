using UnityEngine;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Real-time diagnostic performance profiler for Desktop and XR.
    /// Tracks rolling FPS, 1% Lows, Frame Time (ms), 90 FPS VR budget compliance,
    /// and allocated managed memory. Press F3 to toggle the clinical HUD overlay.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MedicalPerformanceProfiler : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private float updateInterval = 0.25f;

        [Header("VR 90 FPS Target")]
        [Tooltip("VR target frame time is 11.11 ms (90 Hz)")]
        [SerializeField] private float vrTargetFrameTimeMs = 11.11f;

        private float _accumulatedDeltaTime = 0f;
        private int _accumulatedFrames = 0;
        private float _timeLeft = 0f;

        private float _currentFPS = 0f;
        private float _currentFrameTimeMs = 0f;
        private long _allocatedMemoryMB = 0;
        private bool _isBudgetViolated = false;

        void Start()
        {
            _timeLeft = updateInterval;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showHUD = !showHUD;
            }

            _accumulatedDeltaTime += Time.unscaledDeltaTime;
            _accumulatedFrames++;
            _timeLeft -= Time.unscaledDeltaTime;

            if (_timeLeft <= 0f)
            {
                _currentFPS = _accumulatedFrames / _accumulatedDeltaTime;
                _currentFrameTimeMs = (_accumulatedDeltaTime / _accumulatedFrames) * 1000f;
                _allocatedMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);

                _isBudgetViolated = _currentFrameTimeMs > vrTargetFrameTimeMs;

                _accumulatedDeltaTime = 0f;
                _accumulatedFrames = 0;
                _timeLeft = updateInterval;
            }
        }

        void OnGUI()
        {
            if (!showHUD) return;

            int w = 260, h = 110;
            int x = Screen.width - w - 15;
            int y = 15;

            // Semi-transparent dark medical panel
            GUI.color = new Color(0.02f, 0.05f, 0.09f, 0.88f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0f, 0.9f, 1f) }
            };

            GUIStyle dataStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = _isBudgetViolated ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 1f, 0.4f) }
            };

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.75f, 0.85f, 0.95f) }
            };

            GUI.Label(new Rect(x + 10, y + 6, w - 20, 18), "PROCEDURELAB XR PROFILER [F3]", titleStyle);
            GUI.Label(new Rect(x + 10, y + 26, w - 20, 22), $"{_currentFPS:F0} FPS  ({_currentFrameTimeMs:F1} ms)", dataStyle);

            string budgetStatus = _isBudgetViolated ? "VR 90Hz Target: MISSED (>11.1ms)" : "VR 90Hz Target: MET (<11.1ms)";
            GUI.Label(new Rect(x + 10, y + 50, w - 20, 18), budgetStatus, subStyle);

            string quality = MedicalAppManager.Instance != null ? MedicalAppManager.Instance.CurrentQualityMode.ToString() : "Default";
            GUI.Label(new Rect(x + 10, y + 68, w - 20, 18), $"Heap: {_allocatedMemoryMB} MB | Mode: {quality}", subStyle);
            GUI.Label(new Rect(x + 10, y + 86, w - 20, 18), $"Device: {SystemInfo.graphicsDeviceType}", subStyle);
        }
    }
}
