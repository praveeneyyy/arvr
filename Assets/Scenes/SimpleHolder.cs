using UnityEngine;

public class SimpleHolder : MonoBehaviour
{
    [Tooltip("The part of the rig that should hold the object (e.g., the Hand bone)")]
    public Transform holdPoint;

    [Tooltip("The object to pick up (e.g., your Blocking Cube)")]
    public Transform objectToHold;

    [Tooltip("Press this key to Grab/Drop (for testing on PC)")]
    public KeyCode grabKey = KeyCode.Space;

    private bool isHolding = false;
    private Transform originalParent;
    private Rigidbody objectRb;
    private bool wasKinematic;

    void Start()
    {
        if (objectToHold != null)
        {
            objectRb = objectToHold.GetComponent<Rigidbody>();
            originalParent = objectToHold.parent;
            if (objectRb != null) wasKinematic = objectRb.isKinematic;
        }
    }

    void Update()
    {
        // 1. Trigger the Grab/Drop (You can replace this with VR Input later)
        if (Input.GetKeyDown(grabKey)) 
        {
            if (isHolding) Drop();
            else Grab();
        }
    }

    public void Grab()
    {
        if (objectToHold == null || holdPoint == null) return;

        isHolding = true;

        // 1. Disable Physics so it doesn't jitter or fall
        if (objectRb != null) objectRb.isKinematic = true;

        // 2. Snap position to the hand
        objectToHold.position = holdPoint.position;
        objectToHold.rotation = holdPoint.rotation;

        // 3. Parent it (This makes it move with the rig)
        objectToHold.SetParent(holdPoint);
    }

    public void Drop()
    {
        if (objectToHold == null) return;

        isHolding = false;

        // 1. Un-parent so it stops following
        objectToHold.SetParent(originalParent);

        // 2. Re-enable physics so it falls
        if (objectRb != null) objectRb.isKinematic = wasKinematic;
    }
}