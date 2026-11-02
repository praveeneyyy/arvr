using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MedicalSimulation.MainMenu
{
    /// <summary>
    /// Master Module Navigation & Context Panel Coordinator.
    /// Manages the exact 6-module clinical ordering, selection highlights,
    /// dynamic context panel metadata updates, and primary scene entry actions.
    /// </summary>
    public class ModuleNavigationController : MonoBehaviour
    {
        [Header("Module List Container")]
        [SerializeField] private Transform moduleListRoot;
        [SerializeField] private ModulePreviewController previewController;

        [Header("Right Context Panel References")]
        [SerializeField] private TextMeshProUGUI contextTitleText;
        [SerializeField] private TextMeshProUGUI contextSubtitleText;
        [SerializeField] private TextMeshProUGUI contextDescriptionText;
        [SerializeField] private TextMeshProUGUI contextCapabilitiesText;
        [SerializeField] private TextMeshProUGUI contextSpecsText;
        [SerializeField] private Button contextActionButton;
        [SerializeField] private TextMeshProUGUI contextActionButtonText;

        public event Action<int> OnModuleSelected;

        private readonly List<(int index, Image bg, Image dot, TextMeshProUGUI numText, TextMeshProUGUI titleText)> _menuItems
            = new List<(int, Image, Image, TextMeshProUGUI, TextMeshProUGUI)>();

        private int _selectedModuleIndex = 0;

        void Awake()
        {
            if (previewController == null) previewController = FindFirstObjectByType<ModulePreviewController>();
        }

        public void InitializeModuleList()
        {
            if (moduleListRoot == null) return;

            // Clear existing children
            _menuItems.Clear();
            for (int i = moduleListRoot.childCount - 1; i >= 0; i--)
            {
                var child = moduleListRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var modules = ModuleAssetRegistry.GetAllModules();
            for (int i = 0; i < modules.Count; i++)
            {
                var meta = modules[i];
                int captureIdx = i;

                // Create sleek menu item button
                var itemGo = CreateUIRect($"Item_{meta.moduleNumber}_{meta.displayName}", moduleListRoot,
                    Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 56));

                var btnImg = itemGo.AddComponent<Image>();
                btnImg.color = (i == _selectedModuleIndex) ? new Color(0.08f, 0.18f, 0.28f, 0.95f) : new Color(0.06f, 0.08f, 0.11f, 0.85f);

                var border = CreateUIRect("Border", itemGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var borderImg = border.AddComponent<Image>();
                borderImg.color = (i == _selectedModuleIndex) ? meta.accentColor : new Color(0.18f, 0.24f, 0.32f, 0.6f);

                var hLayout = itemGo.AddComponent<HorizontalLayoutGroup>();
                hLayout.padding = new RectOffset(12, 12, 6, 6);
                hLayout.spacing = 10;
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childControlWidth = false;
                hLayout.childForceExpandWidth = false;
                hLayout.childControlHeight = true;
                hLayout.childForceExpandHeight = true;

                // Left Accent Status Dot
                var dotGo = CreateUIRect("StatusDot", itemGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(8, 8));
                var dotImg = dotGo.AddComponent<Image>();
                dotImg.color = (i == _selectedModuleIndex) ? meta.accentColor : new Color(0.35f, 0.42f, 0.52f, 1f);

                // Module Number (e.g. "01")
                var numGo = CreateUIRect("Number", itemGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(28, 40));
                var numTmp = numGo.AddComponent<TextMeshProUGUI>();
                numTmp.text = $"<b>{meta.moduleNumber}</b>";
                numTmp.fontSize = 12;
                numTmp.color = (i == _selectedModuleIndex) ? meta.accentColor : new Color(0.45f, 0.52f, 0.62f, 1f);
                numTmp.alignment = TextAlignmentOptions.MidlineCenter;

                // Text Column (Title + Subtitle)
                var txtColGo = CreateUIRect("TextCol", itemGo.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(220, 44));
                var titleTmp = txtColGo.AddComponent<TextMeshProUGUI>();
                titleTmp.text = $"<b>{meta.displayName}</b>\n<size=75%><color=#94A3B8>{meta.subtitle}</color></size>";
                titleTmp.fontSize = 11.5f;
                titleTmp.lineSpacing = -12f;
                titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
                titleTmp.color = (i == _selectedModuleIndex) ? Color.white : new Color(0.85f, 0.88f, 0.92f, 1f);

                // Button interaction
                var btn = itemGo.AddComponent<Button>();
                btn.onClick.AddListener(() => {
                    SelectModule(captureIdx);
                });

                _menuItems.Add((i, btnImg, dotImg, numTmp, titleTmp));
            }

            SelectModule(0);
        }

        public void SelectModule(int index)
        {
            _selectedModuleIndex = index;
            var meta = ModuleAssetRegistry.GetModuleByIndex(index);
            if (meta == null) return;

            // Update item highlights
            var modules = ModuleAssetRegistry.GetAllModules();
            for (int i = 0; i < _menuItems.Count; i++)
            {
                bool isSelected = (i == index);
                var item = _menuItems[i];
                var m = modules[i];

                if (item.bg != null)
                {
                    item.bg.color = isSelected ? new Color(0.08f, 0.20f, 0.32f, 0.95f) : new Color(0.06f, 0.08f, 0.11f, 0.85f);
                }
                if (item.dot != null)
                {
                    item.dot.color = isSelected ? m.accentColor : new Color(0.35f, 0.42f, 0.52f, 1f);
                }
                if (item.numText != null)
                {
                    item.numText.color = isSelected ? m.accentColor : new Color(0.45f, 0.52f, 0.62f, 1f);
                }
                if (item.titleText != null)
                {
                    item.titleText.color = isSelected ? Color.white : new Color(0.85f, 0.88f, 0.92f, 1f);
                }
            }

            // Update 3D Viewport preview
            if (previewController != null)
            {
                previewController.DisplayModulePreview(index);
            }

            // Update Right Context Panel
            UpdateContextPanel(meta);

            OnModuleSelected?.Invoke(index);
        }

        private void UpdateContextPanel(ModuleMetadata meta)
        {
            if (contextTitleText != null)
                contextTitleText.text = $"<b>{meta.displayName}</b>";

            if (contextSubtitleText != null)
                contextSubtitleText.text = $"<color=#00E5FF>[ MODULE {meta.moduleNumber} ]</color>  <color=#94A3B8>{meta.subtitle}</color>";

            if (contextDescriptionText != null)
                contextDescriptionText.text = meta.description;

            if (contextCapabilitiesText != null)
                contextCapabilitiesText.text = $"<color=#00E5FF>Clinical Capabilities:</color>\n{meta.capabilities}";

            if (contextSpecsText != null)
                contextSpecsText.text = $"<size=80%><color=#64748B>{meta.technicalSpecs}</color></size>";

            if (contextActionButtonText != null)
                contextActionButtonText.text = $"<b>{meta.primaryActionLabel}</b>";

            if (contextActionButton != null)
            {
                contextActionButton.onClick.RemoveAllListeners();
                contextActionButton.onClick.AddListener(() => {
                    EnterSelectedModule(meta);
                });
            }
        }

        private void EnterSelectedModule(ModuleMetadata meta)
        {
            var transition = SceneTransitionController.Instance ?? FindFirstObjectByType<SceneTransitionController>();
            if (transition != null)
            {
                transition.TransitionToScene(meta.targetSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(meta.targetSceneName);
            }
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
