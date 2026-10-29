using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JawController : MonoBehaviour
{
    [SerializeField] private Transform lowerJaw;
    [SerializeField] private float maxRotation = 30f;
    [SerializeField] private float openSpeed = 2f;
    
    private float currentRotation = 0f;
    private bool isOpen = false;
    
    void Update()
    {
        // Toggle with a button or automatically
        float targetRotation = isOpen ? maxRotation : 0f;
        currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * openSpeed);
        
        // Rotate around X axis (adjust axis as needed for your model)
        lowerJaw.localRotation = Quaternion.Euler(currentRotation, 0, 0);
    }
    
    public void ToggleJaw()
    {
        isOpen = !isOpen;
    }
    
    public void OpenJaw()
    {
        isOpen = true;
    }
    
    public void CloseJaw()
    {
        isOpen = false;
    }
}