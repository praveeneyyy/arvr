using UnityEngine;

namespace MedicalSimulation.UI
{
    public enum MedicalComponentType
    {
        FullAnatomy = 0,
        Cardiology = 1,
        Dental = 2,
        SoftTissue = 3,
        Diagnosis = 4,
        Procedure = 5,
        OperatingTheatre = 6
    }

    /// <summary>
    /// Centralized clinical design system tokens for Medical Simulation AR/VR.
    /// Professional clinical workstation palette, typography scale, metrics, and interaction styles.
    /// Eliminates chaotic sci-fi neon styling in favor of high-contrast, clean medical visualization.
    /// </summary>
    public static class MedicalUITheme
    {
        // -------------------------------------------------------------
        // Refined Dark Medical Workstation Palette
        // -------------------------------------------------------------
        public static readonly Color BackgroundDeep      = new Color(0.063f, 0.078f, 0.098f, 1.0f); // #101419 Deep charcoal workspace
        public static readonly Color SurfaceSecondary    = new Color(0.090f, 0.114f, 0.141f, 0.98f); // #171D24 Main docked panels
        public static readonly Color SurfaceElevated     = new Color(0.125f, 0.157f, 0.196f, 1.0f); // #202832 Cards, toolbars, buttons
        public static readonly Color SurfaceHover        = new Color(0.165f, 0.208f, 0.259f, 1.0f); // #2A3542 Hover state
        public static readonly Color SurfacePressed      = new Color(0.070f, 0.090f, 0.110f, 1.0f); // #12171C Active click state
        public static readonly Color BorderSubtle         = new Color(0.200f, 0.240f, 0.290f, 0.50f); // Restrained border
        public static readonly Color BorderActive         = new Color(0.0f, 0.75f, 0.85f, 0.80f);     // Active focus border

        // -------------------------------------------------------------
        // Functional Medical Accent Colors
        // -------------------------------------------------------------
        public static readonly Color AccentCyan          = new Color(0.0f, 0.82f, 0.92f, 1.0f);      // Soft cyan for selection/nav
        public static readonly Color AccentTeal          = new Color(0.08f, 0.72f, 0.65f, 1.0f);     // Teal for active physiological state
        public static readonly Color AccentCardiovascular= new Color(0.92f, 0.25f, 0.25f, 1.0f);     // Anatomical arterial red
        public static readonly Color AccentVenous        = new Color(0.22f, 0.45f, 0.85f, 1.0f);     // Systemic venous blue
        public static readonly Color AccentWarning       = new Color(0.95f, 0.68f, 0.22f, 1.0f);     // Amber clinical warning only
        public static readonly Color AccentSuccess       = new Color(0.18f, 0.80f, 0.44f, 1.0f);     // Verification/passing green

        // -------------------------------------------------------------
        // Typography Hierarchy
        // -------------------------------------------------------------
        public static readonly Color TextPrimary         = new Color(0.95f, 0.97f, 0.98f, 1.0f);     // High contrast white
        public static readonly Color TextSecondary       = new Color(0.60f, 0.66f, 0.72f, 1.0f);     // Muted grey secondary text
        public static readonly Color TextTertiary        = new Color(0.40f, 0.46f, 0.52f, 1.0f);     // Metadata / captions
        public static readonly Color TextAccent          = new Color(0.0f, 0.85f, 0.95f, 1.0f);      // Highlighted labels

        public const int FontTitleLarge = 22;
        public const int FontTitleMedium = 16;
        public const int FontBody = 13;
        public const int FontCaption = 11;
        public const int FontTiny = 9;

        // -------------------------------------------------------------
        // Ergonomic Spatial Dimensions
        // -------------------------------------------------------------
        public const float SidebarWidth = 240f;
        public const float SidebarCollapsedWidth = 56f;
        public const float TopNavbarHeight = 52f;
        public const float InspectionPanelWidth = 360f;
        public const float BottomToolbarHeight = 44f;
        public const float CornerRadius = 4f; // Minimal, strictly non-sci-fi rounding
    }
}
