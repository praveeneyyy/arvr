using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Core;

namespace MedicalSimulation.XR
{
    /// <summary>
    /// XR interaction adapter for organ selection.
    /// Listens to XR ray-cast hover/select events on OrganInteractable objects
    /// and delegates to the same OrganInteractable.Select() / Deselect() API
    /// used by the desktop mouse path.
    /// 
    /// Attach to the XR Ray Interactor GameObject (or a parent).
    /// The XRRayInteractor in the scene already provides raycasting;
    /// this script maps its events to our medical interaction model.
    /// </summary>
    public class MedicalXRInteractionAdapter : MonoBehaviour
    {
        [Header("XR Interactor (auto-found if null)")]
        [SerializeField] private XRRayInteractor rayInteractor;

        [Header("World-space UI panels to reposition near the organ")]
        [SerializeField] private Transform inspectionPanelRoot;
        [SerializeField] private float     panelOffset = 0.25f;

        void Start()
        {
            if (rayInteractor == null)
                rayInteractor = GetComponent<XRRayInteractor>();
            if (rayInteractor == null)
                rayInteractor = FindFirstObjectByType<XRRayInteractor>();

            if (rayInteractor != null)
            {
                rayInteractor.selectEntered.AddListener(OnXRSelectEntered);
                rayInteractor.selectExited.AddListener(OnXRSelectExited);
                rayInteractor.hoverEntered.AddListener(OnXRHoverEntered);
            }
            else
            {
                Debug.LogWarning("[MedicalXRInteractionAdapter] No XRRayInteractor found. " +
                                 "XR organ selection will not function.");
            }
        }

        void OnDestroy()
        {
            if (rayInteractor != null)
            {
                rayInteractor.selectEntered.RemoveListener(OnXRSelectEntered);
                rayInteractor.selectExited.RemoveListener(OnXRSelectExited);
                rayInteractor.hoverEntered.RemoveListener(OnXRHoverEntered);
            }
        }

        private void OnXRSelectEntered(SelectEnterEventArgs args)
        {
            var organ = args.interactableObject?.transform?.GetComponent<OrganInteractable>();
            if (organ != null)
            {
                organ.OnXRInteract();
                PositionInspectionPanel(organ.transform.position);
            }
        }

        private void OnXRSelectExited(SelectExitEventArgs args)
        {
            var organ = args.interactableObject?.transform?.GetComponent<OrganInteractable>();
            organ?.Deselect();
        }

        private void OnXRHoverEntered(HoverEnterEventArgs args)
        {
            // Haptic feedback stub — wire to your controller haptics if available
            // e.g. XRBaseController.SendHapticImpulse(0.1f, 0.05f);
        }

        private void PositionInspectionPanel(Vector3 organWorldPos)
        {
            if (inspectionPanelRoot == null) return;
            // Place the panel slightly to the right of the organ, facing the camera/HMD
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 dir   = (cam.transform.position - organWorldPos).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                inspectionPanelRoot.position = organWorldPos + right * panelOffset;
                inspectionPanelRoot.LookAt(cam.transform);
                inspectionPanelRoot.Rotate(0, 180f, 0);
            }
        }
    }
}
