using UnityEngine;

public class SimpleToolPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rightHand; // Assign your right hand controller
    [SerializeField] private Transform leftHand;  // Optional: assign left hand
    
    [Header("Settings")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Mouse0; // Left mouse click
    [SerializeField] private KeyCode releaseKey = KeyCode.R; // Press R to release
    [SerializeField] private bool requireHoverToRelease = false; // If true, must hover over tool to release
    
    private bool isHeld = false;
    private Transform currentHand;
    private Rigidbody rb;
    private Collider col;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        currentHand = rightHand; // Default to right hand
    }

    void Update()
    {
        if (isHeld && currentHand != null)
        {
            // Keep tool attached to hand
            transform.position = currentHand.position;
            transform.rotation = currentHand.rotation;
            
            // Allow releasing without hovering (press R anywhere)
            if (!requireHoverToRelease && Input.GetKeyDown(releaseKey))
            {
                ReleaseTool();
            }
        }
    }

    void OnMouseDown()
    {
        // Called when clicking on this object
        if (!isHeld)
        {
            PickupTool();
        }
    }

    void OnMouseOver()
    {
        // Alternative: pickup on key press while hovering
        if (Input.GetKeyDown(pickupKey) && !isHeld)
        {
            PickupTool();
        }
        
        // Release only if hovering is required
        if (requireHoverToRelease && Input.GetKeyDown(releaseKey) && isHeld)
        {
            ReleaseTool();
        }
    }

    void PickupTool()
    {
        if (currentHand == null)
        {
            Debug.LogError("Hand not assigned!");
            return;
        }

        isHeld = true;
        
        // Parent to hand
        transform.SetParent(currentHand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        // Disable physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        if (col != null)
        {
            col.enabled = false; // Optional: prevents collision while held
        }
        
        Debug.Log($"Picked up {gameObject.name}");
    }

    void ReleaseTool()
    {
        isHeld = false;
        
        // Unparent
        transform.SetParent(null);
        
        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        if (col != null)
        {
            col.enabled = true;
        }
        
        Debug.Log($"Released {gameObject.name}");
    }
    
    // Optional: Switch hands with a key
    void SwitchHand()
    {
        if (leftHand != null)
        {
            currentHand = (currentHand == rightHand) ? leftHand : rightHand;
        }
    }
}