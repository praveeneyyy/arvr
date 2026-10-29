using UnityEngine;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// Applies platform-appropriate quality settings.
    /// Separate from Unity's built-in Quality Levels to allow
    /// fine-grained control per-feature without overwriting the
    /// entire quality profile.
    /// </summary>
    public static class MedicalQualitySettings
    {
        public static void Apply(MedicalAppManager.QualityMode mode)
        {
            switch (mode)
            {
                case MedicalAppManager.QualityMode.Desktop:
                    ApplyDesktop();
                    break;
                case MedicalAppManager.QualityMode.VR:
                    ApplyVR();
                    break;
            }
        }

        private static void ApplyDesktop()
        {
            QualitySettings.shadows              = ShadowQuality.All;
            QualitySettings.shadowResolution     = ShadowResolution.High;
            QualitySettings.antiAliasing         = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.masterTextureLimit   = 0; // Full resolution
            QualitySettings.maximumLODLevel      = 0;
            QualitySettings.particleRaycastBudget = 256;
            Application.targetFrameRate          = 60;
            Debug.Log("[MedicalQualitySettings] Desktop profile applied.");
        }

        private static void ApplyVR()
        {
            // For VR, reduce shadow cost and texture overhead.
            // Keep AA at 2× (most HMDs use their own multi-sampling).
            QualitySettings.shadows              = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution     = ShadowResolution.Medium;
            QualitySettings.antiAliasing         = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.masterTextureLimit   = 0;
            QualitySettings.maximumLODLevel      = 0;
            QualitySettings.particleRaycastBudget = 64;
            Application.targetFrameRate          = 90; // Standard VR target
            Debug.Log("[MedicalQualitySettings] VR profile applied.");
        }
    }
}
