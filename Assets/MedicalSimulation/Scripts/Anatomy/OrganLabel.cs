using UnityEngine;
using TMPro;

namespace MedicalSimulation.Anatomy
{
    /// <summary>
    /// Simple world-space label that floats above an organ.
    /// Driven by AnatomyModelController visibility toggle.
    /// </summary>
    public class OrganLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshPro   tmp;
        [SerializeField] private Transform     anchor;
        [SerializeField] private Vector3       offset = new Vector3(0, 0.12f, 0);
        [SerializeField] private float         scaleDistanceFactor = 0.1f;

        private Camera _cam;

        void Awake()
        {
            if (tmp == null) tmp = GetComponentInChildren<TextMeshPro>();
            _cam = Camera.main;

            if (anchor != null)
                transform.position = anchor.position + offset;
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // Always face the camera
            transform.LookAt(_cam.transform);
            transform.Rotate(0, 180f, 0);

            // Scale so it reads at a consistent apparent size
            float dist = Vector3.Distance(transform.position, _cam.transform.position);
            float s    = Mathf.Max(0.01f, dist * scaleDistanceFactor);
            transform.localScale = Vector3.one * s;
        }

        public void SetText(string text)
        {
            if (tmp != null) tmp.text = text;
        }
    }
}
