using System;
using System.Collections.Generic;
using UnityEngine;
using MedicalSimulation.Core;
using MedicalSimulation.Anatomy;

namespace MedicalSimulation.OperatingTheatre
{
    /// <summary>
    /// Controls the 3D surgical patient on the operating table.
    /// Manages sterile surgical draping, laparotomy aperture exposure,
    /// and transitions between 10 anatomical visualization modes:
    /// External, Surgical Field, Skeletal, Muscular, Visceral Organs,
    /// Cardiovascular, Respiratory, Digestive, Transparency, and Exploded view.
    /// </summary>
    public class PatientModelController : MonoBehaviour
    {
        public enum AnatomicalViewMode
        {
            ExternalDraped,
            SurgicalFieldExposed,
            SkeletalView,
            MuscularView,
            InternalOrgansView,
            CardiovascularView,
            RespiratoryView,
            DigestiveView,
            AnatomicalTransparency,
            ExplodedView
        }

        [Header("Patient Model Components")]
        [SerializeField] private GameObject patientBodyMesh;
        [SerializeField] private GameObject surgicalDrapes;
        [SerializeField] private GameObject surgicalApertureFrame;
        [SerializeField] private Transform internalAnatomyRoot;

        [Header("Current Mode")]
        [SerializeField] private AnatomicalViewMode currentViewMode = AnatomicalViewMode.SurgicalFieldExposed;

        [Header("Materials")]
        [SerializeField] private Material matStandardDrape;
        [SerializeField] private Material matTransparentFresnel;
        [SerializeField] private Material matExposedSkin;

        private AnatomyModelController _anatomyController;
        private AnatomyAssemblyBuilder _anatomyBuilder;
        private OrganInteractable _selectedOrgan;

        public event Action<AnatomicalViewMode> OnViewModeChanged;
        public event Action<OrganInteractable> OnOrganClicked;

        public AnatomicalViewMode CurrentViewMode => currentViewMode;

        void Awake()
        {
            FindOrBuildAnatomy();
        }

        void Start()
        {
            SetViewMode(currentViewMode);
        }

        public void FindOrBuildAnatomy()
        {
            if (internalAnatomyRoot == null)
            {
                var existing = GetComponentInChildren<AnatomyModelController>();
                if (existing != null)
                {
                    internalAnatomyRoot = existing.transform;
                    _anatomyController = existing;
                }
                else
                {
                    var anatomyGo = new GameObject("Patient_InternalAnatomy");
                    anatomyGo.transform.SetParent(transform, false);
                    anatomyGo.transform.localPosition = new Vector3(0, 0.35f, 0.95f);
                    anatomyGo.transform.localRotation = Quaternion.Euler(-90f, 0, 0); // Align with supine patient
                    anatomyGo.transform.localScale = Vector3.one * 0.92f;

                    _anatomyController = anatomyGo.AddComponent<AnatomyModelController>();
                    _anatomyBuilder = anatomyGo.AddComponent<AnatomyAssemblyBuilder>();
                    _anatomyBuilder.BuildAnatomicalModel();
                    internalAnatomyRoot = anatomyGo.transform;
                }
            }
            else
            {
                _anatomyController = internalAnatomyRoot.GetComponent<AnatomyModelController>();
            }

            if (_anatomyController != null)
            {
                _anatomyController.OnSelectionChanged += HandleOrganSelected;
            }
        }

        public void SetViewMode(AnatomicalViewMode mode)
        {
            currentViewMode = mode;

            if (_anatomyController == null) FindOrBuildAnatomy();

            switch (mode)
            {
                case AnatomicalViewMode.ExternalDraped:
                    SetDrapesVisible(true, false);
                    if (internalAnatomyRoot != null) internalAnatomyRoot.gameObject.SetActive(false);
                    break;

                case AnatomicalViewMode.SurgicalFieldExposed:
                    SetDrapesVisible(true, true);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowAllSystems();
                    }
                    break;

                case AnatomicalViewMode.SkeletalView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowSystem(AnatomicalSystem.Skeletal);
                    }
                    break;

                case AnatomicalViewMode.MuscularView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowSystem(AnatomicalSystem.Muscular);
                    }
                    break;

                case AnatomicalViewMode.InternalOrgansView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowAllSystems();
                    }
                    break;

                case AnatomicalViewMode.CardiovascularView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowSystem(AnatomicalSystem.Cardiovascular);
                    }
                    break;

                case AnatomicalViewMode.RespiratoryView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowSystem(AnatomicalSystem.Respiratory);
                    }
                    break;

                case AnatomicalViewMode.DigestiveView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ShowSystem(AnatomicalSystem.Digestive);
                    }
                    break;

                case AnatomicalViewMode.AnatomicalTransparency:
                    SetDrapesVisible(true, true);
                    ApplyTransparencyMode(0.2f);
                    break;

                case AnatomicalViewMode.ExplodedView:
                    SetDrapesVisible(false, false);
                    if (internalAnatomyRoot != null)
                    {
                        internalAnatomyRoot.gameObject.SetActive(true);
                        _anatomyController?.ToggleExplodedView();
                    }
                    break;
            }

            OnViewModeChanged?.Invoke(currentViewMode);
        }

        private void SetDrapesVisible(bool drapesOn, bool apertureOpen)
        {
            if (surgicalDrapes != null) surgicalDrapes.SetActive(drapesOn);
            if (surgicalApertureFrame != null) surgicalApertureFrame.SetActive(apertureOpen);
        }

        private void ApplyTransparencyMode(float alpha)
        {
            if (_anatomyController != null)
            {
                _anatomyController.SetAllTransparency(alpha);
            }
        }

        private void HandleOrganSelected(OrganInteractable organ)
        {
            _selectedOrgan = organ;
            OnOrganClicked?.Invoke(organ);
        }

        public void IsolateSelectedOrgan()
        {
            if (_selectedOrgan != null && _anatomyController != null)
            {
                _anatomyController.IsolateOrgan(_selectedOrgan);
            }
        }

        public void RestoreAllAnatomy()
        {
            if (_anatomyController != null)
            {
                _anatomyController.RestoreAll();
            }
        }
    }
}
