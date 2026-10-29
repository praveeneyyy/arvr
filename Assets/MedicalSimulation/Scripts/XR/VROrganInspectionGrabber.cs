using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using MedicalSimulation.Anatomy;
using MedicalSimulation.Audio;

namespace MedicalSimulation.XR
{
    /// <summary>
    /// Enables direct hand / controller grabbing of anatomical organs in VR.
    /// When grabbed, the organ isolates, triggers haptic feedback, and positions
    /// a world-space clinical inspection HUD directly in the user's view.
    /// When released, smoothly interpolates back to its anatomical socket.
    /// </summary>
    [RequireComponent(typeof(OrganInteractable))]
    public class VROrganInspectionGrabber : MonoBehaviour
    {
        [Header("Return to Body Settings")]
        [SerializeField] private bool snapBackOnRelease = true;
        [SerializeField] private float returnSpeed = 4.5f;

        [Header("Haptics")]
        [SerializeField] private float grabHapticIntensity = 0.45f;
        [SerializeField] private float grabHapticDuration = 0.08f;

        private XRGrabInteractable _grabInteractable;
        private OrganInteractable _organInteractable;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Transform _originalParent;
        private bool _isGrabbed = false;
        private bool _isReturning = false;

        void Awake()
        {
            _organInteractable = GetComponent<OrganInteractable>();
            _originalParent = transform.parent;
            _originalLocalPosition = transform.localPosition;
            _originalLocalRotation = transform.localRotation;

            _grabInteractable = GetComponent<XRGrabInteractable>() ?? gameObject.AddComponent<XRGrabInteractable>();
            _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grabInteractable.smoothPosition = true;
            _grabInteractable.smoothRotation = true;

            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }

        void OnDestroy()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                _grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        void Update()
        {
            if (_isReturning && !_isGrabbed)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, _originalLocalPosition, Time.deltaTime * returnSpeed);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, _originalLocalRotation, Time.deltaTime * returnSpeed);

                if (Vector3.Distance(transform.localPosition, _originalLocalPosition) < 0.005f)
                {
                    transform.localPosition = _originalLocalPosition;
                    transform.localRotation = _originalLocalRotation;
                    _isReturning = false;
                }
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _isGrabbed = true;
            _isReturning = false;

            _organInteractable?.Select();
            MedicalAudioController.Instance?.PlaySelect();

            // Trigger controller haptics if supported
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
            {
                inputInteractor.SendHapticImpulse(grabHapticIntensity, grabHapticDuration);
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            _isGrabbed = false;
            if (snapBackOnRelease)
            {
                if (_originalParent != null)
                {
                    transform.SetParent(_originalParent);
                }
                _isReturning = true;
            }
        }

        public void ResetToAnatomicalPosition()
        {
            _isReturning = false;
            _isGrabbed = false;
            if (_originalParent != null) transform.SetParent(_originalParent);
            transform.localPosition = _originalLocalPosition;
            transform.localRotation = _originalLocalRotation;
        }
    }
}
