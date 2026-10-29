using UnityEngine;

namespace MedicalSimulation.Core
{
    /// <summary>
    /// ScriptableObject that stores metadata for a single anatomical structure.
    /// Assign one per organ/region in the Project window.
    /// </summary>
    [CreateAssetMenu(fileName = "AnatomyData_New", menuName = "MedicalSimulation/Anatomy Metadata")]
    public class AnatomyMetadata : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Stable identifier used in code. e.g. organ.heart")]
        public string organId;

        [Tooltip("Human-readable anatomical name.")]
        public string anatomicalName;

        [Header("Classification")]
        public AnatomicalSystem system;
        public string systemLabel;

        [Header("Location")]
        [TextArea(1, 2)]
        public string anatomicalLocation;

        [Header("Educational Content")]
        [TextArea(2, 5)]
        public string overview;

        [TextArea(2, 5)]
        public string primaryFunctions;

        [TextArea(2, 5)]
        public string anatomyDetails;

        [TextArea(2, 5)]
        public string clinicalInformation;

        [Header("Relations")]
        [Tooltip("Names of directly related structures.")]
        public string[] relatedStructures;

        [Header("Interaction")]
        public bool isSelectable = true;
        public bool supportsIsolation = true;
        public bool supportsTransparency = true;

        [Header("Visual")]
        public Color highlightColor = new Color(0.2f, 0.9f, 0.9f, 0.8f);
        public Sprite icon;
    }

    public enum AnatomicalSystem
    {
        None,
        Skeletal,
        Muscular,
        Cardiovascular,
        Respiratory,
        Digestive,
        Urinary,
        Nervous,
        Endocrine,
        Lymphatic,
        Reproductive,
        Integumentary
    }
}
