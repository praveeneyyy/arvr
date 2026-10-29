using UnityEngine;
using System.Collections.Generic;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Simulates educational peristaltic wave contractions through the digestive tract.
    /// Drives smooth periodic traveling contractile rings through alimentary structures
    /// (Esophagus, Stomach, Small Intestine, Colon) without destructive mesh mutations.
    /// </summary>
    public class DigestivePeristalsisController : MonoBehaviour
    {
        [System.Serializable]
        public class DigestiveSegment
        {
            public string segmentName;
            public Transform segmentTransform;
            [Tooltip("Local direction along which the peristaltic wave travels (normalized)")]
            public Vector3 travelDirection = Vector3.down;
            public float waveFrequency = 1.2f;
            public float waveSpeed = 1.8f;
            public float contractionAmplitude = 0.08f;
            [HideInInspector] public Vector3 originalScale;
            [HideInInspector] public Vector3 originalLocalPosition;
        }

        [Header("Digestive Segments")]
        [SerializeField] private List<DigestiveSegment> segments = new List<DigestiveSegment>();

        [Header("Global Settings")]
        [SerializeField] private bool playOnStart = true;
        [Range(0.2f, 3.0f)]
        [SerializeField] private float speedMultiplier = 1.0f;
        [Range(0.01f, 0.25f)]
        [SerializeField] private float globalContractionDepth = 0.07f;

        private bool _isPlaying = false;
        private float _waveTimer = 0f;

        void Awake()
        {
            InitializeSegments();
        }

        void Start()
        {
            if (playOnStart) Play();
        }

        void Update()
        {
            if (!_isPlaying) return;

            _waveTimer += Time.deltaTime * speedMultiplier;

            foreach (var seg in segments)
            {
                if (seg.segmentTransform == null) continue;

                // Traveling contraction wave: sin(t * speed - spatial phase)
                float phase = _waveTimer * seg.waveSpeed * seg.waveFrequency;
                float contractionFactor = Mathf.Sin(phase);

                // Modulate radial scale perpendicular to travel direction
                // For a wave traveling along Y (down), scale X and Z
                float radialScale = 1.0f - (Mathf.Abs(contractionFactor) * globalContractionDepth);

                Vector3 currentScale = seg.originalScale;
                if (Mathf.Abs(seg.travelDirection.y) > 0.5f)
                {
                    currentScale.x *= radialScale;
                    currentScale.z *= radialScale;
                }
                else if (Mathf.Abs(seg.travelDirection.x) > 0.5f)
                {
                    currentScale.y *= radialScale;
                    currentScale.z *= radialScale;
                }
                else
                {
                    currentScale.x *= radialScale;
                    currentScale.y *= radialScale;
                }

                seg.segmentTransform.localScale = currentScale;
            }
        }

        public void Play()
        {
            _isPlaying = true;
        }

        public void Pause()
        {
            _isPlaying = false;
            ResetToOriginal();
        }

        public void SetSpeed(float speed)
        {
            speedMultiplier = Mathf.Clamp(speed, 0.1f, 5.0f);
        }

        public void SetContractionDepth(float depth)
        {
            globalContractionDepth = Mathf.Clamp(depth, 0.01f, 0.3f);
        }

        private void InitializeSegments()
        {
            if (segments == null || segments.Count == 0)
            {
                // Auto-detect stomach and intestines in children
                var stomach = transform.Find("organ.stomach") ?? transform.Find("Stomach");
                if (stomach != null)
                {
                    segments.Add(new DigestiveSegment
                    {
                        segmentName = "Stomach Antrum",
                        segmentTransform = stomach,
                        travelDirection = new Vector3(1f, -1f, 0).normalized,
                        waveFrequency = 0.8f,
                        waveSpeed = 1.2f,
                        contractionAmplitude = 0.06f
                    });
                }

                var intestine = transform.Find("organ.small_intestine") ?? transform.Find("SmallIntestine");
                if (intestine != null)
                {
                    segments.Add(new DigestiveSegment
                    {
                        segmentName = "Small Intestine",
                        segmentTransform = intestine,
                        travelDirection = Vector3.down,
                        waveFrequency = 1.4f,
                        waveSpeed = 2.0f,
                        contractionAmplitude = 0.09f
                    });
                }
            }

            foreach (var seg in segments)
            {
                if (seg.segmentTransform != null)
                {
                    seg.originalScale = seg.segmentTransform.localScale;
                    seg.originalLocalPosition = seg.segmentTransform.localPosition;
                }
            }
        }

        private void ResetToOriginal()
        {
            foreach (var seg in segments)
            {
                if (seg.segmentTransform != null)
                {
                    seg.segmentTransform.localScale = seg.originalScale;
                    seg.segmentTransform.localPosition = seg.originalLocalPosition;
                }
            }
        }
    }
}
