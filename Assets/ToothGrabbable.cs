using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToothExtractionKeyboard : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalScale;
    
    [Header("Extraction Settings")]
    [SerializeField] private float extractionDistance = 0.05f;
    [SerializeField] private float extractionSpeed = 0.02f; // Speed of keyboard extraction
    [SerializeField] private float resistanceStrength = 80f;
    [SerializeField] private AnimationCurve resistanceCurve = AnimationCurve.Linear(0, 1, 1, 0.2f);
    
    [Header("Keyboard Controls")]
    [SerializeField] private KeyCode pullKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode pushKey = KeyCode.DownArrow;
    
    [Header("State")]
    [SerializeField] private bool isExtracted = false;
    
    private Vector3 extractionStartPos;
    private Vector3 pullDirection;
    private float currentPullDistance = 0f;
    private bool isBeingGrabbed = false;
    private Quaternion lockedRotation;
    
    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalScale = transform.localScale;
        
        // DISABLE default grab behavior
        grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
        grabInteractable.throwOnDetach = false;
        grabInteractable.useDynamicAttach = false;
        
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        
        rb.isKinematic = true;
        pullDirection = originalParent.up;
        
        Debug.Log("=== TOOTH EXTRACTION CONTROLS ===");
        Debug.Log("1. Hover over tooth and press G (left grip) to grab");
        Debug.Log("2. While holding G, press UP ARROW to pull tooth");
        Debug.Log("3. Press DOWN ARROW to push back");
        Debug.Log("4. Release G to let go");
    }
    
    void OnGrab(SelectEnterEventArgs args)
    {
        isBeingGrabbed = true;
        extractionStartPos = transform.position;
        currentPullDistance = 0f;
        lockedRotation = transform.rotation;
        
        Debug.Log("✓ TOOTH GRABBED! Press UP ARROW to pull, DOWN ARROW to push back");
    }
    
    void OnRelease(SelectExitEventArgs args)
    {
        isBeingGrabbed = false;
        
        if (!isExtracted)
        {
            SnapBackToJaw();
        }
        else
        {
            grabInteractable.trackPosition = true;
            grabInteractable.trackRotation = true;
            grabInteractable.throwOnDetach = true;
            rb.isKinematic = false;
            rb.useGravity = true;
            
            Debug.Log("✓ Tooth extracted and released!");
        }
    }
    
    void Update()
    {
        if (isBeingGrabbed && !isExtracted)
        {
            HandleKeyboardExtraction();
        }
    }
    
    void HandleKeyboardExtraction()
    {
        float pullInput = 0f;
        
        // Keyboard input
        if (Input.GetKey(pullKey))
        {
            pullInput = extractionSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(pushKey))
        {
            pullInput = -extractionSpeed * Time.deltaTime;
        }
        
        if (pullInput != 0)
        {
            currentPullDistance += pullInput;
            currentPullDistance = Mathf.Clamp(currentPullDistance, 0, extractionDistance);
            
            // Calculate resistance
            float progress = currentPullDistance / extractionDistance;
            float resistance = resistanceCurve.Evaluate(progress) * (resistanceStrength / 100f);
            
            // Apply resistance
            float actualMovement = pullInput * (1f - resistance);
            
            // Move tooth
            Vector3 newPos = extractionStartPos + pullDirection * currentPullDistance;
            transform.position = newPos;
            transform.rotation = lockedRotation;
            
            // Log progress
            if (Time.frameCount % 30 == 0) // Every 30 frames
            {
                Debug.Log($"Extraction: {progress * 100:F0}% | Resistance: {resistance * 100:F0}%");
            }
            
            // Check if extracted
            if (progress >= 0.98f)
            {
                ExtractTooth();
            }
        }
    }
    
    void ExtractTooth()
    {
        isExtracted = true;
        
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;
        Vector3 lossyScale = transform.lossyScale;
        
        transform.SetParent(null);
        transform.position = worldPos;
        transform.rotation = worldRot;
        transform.localScale = lossyScale;
        
        Debug.Log("========================================");
        Debug.Log("✓✓✓ TOOTH SUCCESSFULLY EXTRACTED! ✓✓✓");
        Debug.Log("========================================");
    }
    
    void SnapBackToJaw()
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        transform.position = originalParent.TransformPoint(originalLocalPosition);
        transform.rotation = originalParent.rotation * originalLocalRotation;
        
        currentPullDistance = 0f;
        Debug.Log("Tooth snapped back to jaw");
    }
    
    // UI Display
    void OnGUI()
    {
        if (isBeingGrabbed && !isExtracted)
        {
            float progress = (currentPullDistance / extractionDistance) * 100f;
            float resistance = resistanceCurve.Evaluate(currentPullDistance / extractionDistance) * resistanceStrength;
            
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            
            GUI.Label(new Rect(10, 10, 400, 30), $"EXTRACTION PROGRESS: {progress:F0}%", style);
            GUI.Label(new Rect(10, 40, 400, 30), $"RESISTANCE: {resistance:F0}%", style);
            GUI.Label(new Rect(10, 70, 400, 30), "Hold UP ARROW to pull", style);
            
            // Progress bar
            Rect barBg = new Rect(10, 100, 300, 30);
            Rect barFg = new Rect(10, 100, 300 * (progress / 100f), 30);
            
            GUI.color = Color.black;
            GUI.DrawTexture(barBg, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, progress / 100f);
            GUI.DrawTexture(barFg, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (originalParent != null)
        {
            Vector3 startPos = originalParent.TransformPoint(originalLocalPosition);
            Vector3 pullDir = originalParent.up;
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, startPos + pullDir * extractionDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startPos + pullDir * extractionDistance, 0.005f);
        }
        
        if (Application.isPlaying && isBeingGrabbed && !isExtracted)
        {
            float progress = currentPullDistance / extractionDistance;
            Gizmos.color = Color.Lerp(Color.yellow, Color.green, progress);
            Gizmos.DrawLine(extractionStartPos, transform.position);
        }
    }
}