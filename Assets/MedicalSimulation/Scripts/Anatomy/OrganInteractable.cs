using UnityEngine;
using System.Collections.Generic;
using System;
using MedicalSimulation.Core;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Attaches to each selectable organ mesh. Stores its metadata reference
    /// and manages selection highlighting without permanently modifying materials.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OrganInteractable : MonoBehaviour
    {
        [Header("Metadata")]
        public AnatomyMetadata metadata;

        [Header("Highlight Settings")]
        [SerializeField] private Color selectionEmission = new Color(0f, 0.8f, 0.8f);
        [SerializeField] private Color hoverEmission     = new Color(0f, 0.4f, 0.4f);

        // ── Events ───────────────────────────────────────────────────────────
        public static event Action<OrganInteractable> OnOrganSelected;
        public static event Action<OrganInteractable> OnOrganHovered;
        public static event Action                    OnOrganDeselected;

        // ── Internal ─────────────────────────────────────────────────────────
        private Renderer[]  _renderers;
        private Material[]  _originalMaterials;
        private Material[]  _runtimeMaterials;
        private bool        _isSelected;
        private bool        _isHovered;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            // Cache originals and create per-instance runtime copies
            var origList    = new List<Material>();
            var runtimeList = new List<Material>();
            foreach (var r in _renderers)
            {
                origList.AddRange(r.sharedMaterials);
                var copies = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < r.sharedMaterials.Length; i++)
                    copies[i] = new Material(r.sharedMaterials[i]);
                r.materials = copies;
                runtimeList.AddRange(copies);
            }
            _originalMaterials = origList.ToArray();
            _runtimeMaterials  = runtimeList.ToArray();
        }

        // ── Mouse interaction (desktop) ───────────────────────────────────────

        void OnMouseEnter()
        {
            if (!_isSelected) SetHoverHighlight(true);
        }

        void OnMouseExit()
        {
            if (!_isSelected) SetHoverHighlight(false);
        }

        void OnMouseDown()
        {
            Select();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Select()
        {
            if (_isSelected) return;
            _isSelected = true;
            SetSelectionHighlight(true);
            SetHoverHighlight(false);
            OnOrganSelected?.Invoke(this);
        }

        public void Deselect()
        {
            if (!_isSelected) return;
            _isSelected = false;
            SetSelectionHighlight(false);
            OnOrganDeselected?.Invoke();
        }

        /// <summary>
        /// XR ray/direct-interaction entry point.
        /// </summary>
        public void OnXRInteract() => Select();

        // ── Transparency (for layer/isolation control) ────────────────────────

        public void SetTransparency(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            foreach (var mat in _runtimeMaterials)
            {
                if (mat == null) continue;
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;

                // Switch URP surface type based on alpha
                bool transparent = alpha < 0.99f;
                mat.SetFloat("_Surface", transparent ? 1f : 0f);
                mat.SetInt("_SrcBlend",  transparent ? (int)UnityEngine.Rendering.BlendMode.SrcAlpha : (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend",  transparent ? (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite",    transparent ? 0 : 1);
                mat.renderQueue = transparent ? 3000 : 2000;
            }
        }

        public void SetVisible(bool visible)
        {
            foreach (var r in _renderers)
                if (r != null) r.enabled = visible;
        }

        // ── Highlight helpers ─────────────────────────────────────────────────

        private void SetSelectionHighlight(bool on)
        {
            foreach (var mat in _runtimeMaterials)
            {
                if (mat == null) continue;
                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", selectionEmission);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        private void SetHoverHighlight(bool on)
        {
            _isHovered = on;
            foreach (var mat in _runtimeMaterials)
            {
                if (mat == null) continue;
                if (on)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", hoverEmission);
                    OnOrganHovered?.Invoke(this);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        void OnDestroy()
        {
            // Clean up runtime material instances
            foreach (var mat in _runtimeMaterials)
                if (mat != null) Destroy(mat);
        }
    }
}
