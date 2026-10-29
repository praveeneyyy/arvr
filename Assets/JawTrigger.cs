using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JawControllerOpen : MonoBehaviour
{
    [SerializeField] private Transform lowerJaw;
    [SerializeField] private Transform hingePoint;
    [SerializeField] private Vector3 rotationAxis = new Vector3(1, 0, 0);
    [SerializeField] private float maxOpenAngle = 25f;
    [SerializeField] private float openSpeed = 5f;
    
    [Header("Assign Controller Here")]
    [SerializeField] private ActionBasedController leftController;
    [SerializeField] private ActionBasedController rightController;
    
    private float currentAngle = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        originalPosition = lowerJaw.position;
        originalRotation = lowerJaw.rotation;
    }
    
    void Update()
    {
        bool shouldOpen = false;
        
        // Check left controller grip
        if (leftController != null)
        {
            float leftGrip = leftController.selectAction.action.ReadValue<float>();
            if (leftGrip > 0.1f) shouldOpen = true;
        }
        
        // Check right controller grip
        if (rightController != null)
        {
            float rightGrip = rightController.selectAction.action.ReadValue<float>();
            if (rightGrip > 0.1f) shouldOpen = true;
        }
        
        // Keyboard fallback
        if (Input.GetKey(KeyCode.O)) shouldOpen = true;
        
        float targetAngle = shouldOpen ? maxOpenAngle : 0f;
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * openSpeed);
        
        RotateAroundHinge(currentAngle);
    }
    
    void RotateAroundHinge(float angle)
    {
        if (hingePoint != null)
        {
            lowerJaw.position = originalPosition;
            lowerJaw.rotation = originalRotation;
            lowerJaw.RotateAround(hingePoint.position, hingePoint.TransformDirection(rotationAxis), angle);
        }
        else
        {
            lowerJaw.localRotation = Quaternion.Euler(rotationAxis * angle);
        }
    }
    
    void OnDrawGizmos()
    {
        if (hingePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hingePoint.position, 0.01f);
        }
    }
}