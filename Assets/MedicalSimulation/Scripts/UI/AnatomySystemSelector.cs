using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Core;

namespace MedicalSimulation.UI
{
    /// <summary>
    /// System selector panel for the anatomy explorer.
    /// Sends the selected system to AnatomyModelController.
    /// </summary>
    public class AnatomySystemSelector : MonoBehaviour
    {
        [System.Serializable]
        private struct SystemEntry
        {
            public string       label;
            public AnatomicalSystem system;
            public Button       button;
        }

        [SerializeField] private SystemEntry[] systems;
        [SerializeField] private AnatomyModelController anatomyController;

        [Header("Visual Feedback")]
        [SerializeField] private Color activeColor   = new Color(0.1f, 0.8f, 0.8f);
        [SerializeField] private Color inactiveColor = new Color(0.15f, 0.18f, 0.22f);

        private int _activeIndex = 0;

        void Start()
        {
            if (anatomyController == null)
                anatomyController = FindFirstObjectByType<AnatomyModelController>();

            for (int i = 0; i < systems.Length; i++)
            {
                int idx = i; // capture for lambda
                systems[i].button?.onClick.AddListener(() => SelectSystem(idx));
            }

            SelectSystem(0);
        }

        private void SelectSystem(int index)
        {
            _activeIndex = index;

            for (int i = 0; i < systems.Length; i++)
            {
                var img = systems[i].button?.GetComponent<Image>();
                if (img != null) img.color = (i == _activeIndex) ? activeColor : inactiveColor;
            }

            anatomyController?.ShowSystem(systems[index].system);
        }
    }
}
