using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToothHighlight : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Renderer toothRenderer;
    private Color originalColor;
    
    [SerializeField] private Color highlightColor = Color.yellow;
    
    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        toothRenderer = GetComponent<Renderer>();
        originalColor = toothRenderer.material.color;
        
        grabInteractable.hoverEntered.AddListener(OnHoverEnter);
        grabInteractable.hoverExited.AddListener(OnHoverExit);
    }
    
    void OnHoverEnter(HoverEnterEventArgs args)
    {
        toothRenderer.material.color = highlightColor;
    }
    
    void OnHoverExit(HoverExitEventArgs args)
    {
        toothRenderer.material.color = originalColor;
    }
}