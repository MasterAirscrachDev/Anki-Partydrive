using UnityEngine;

public class TruckTrailer : MonoBehaviour
{
    [SerializeField] Transform parentTruck;
    [SerializeField] float rotationSensitivity = 3f;
    
    Vector3 hitchOffset;
    Vector3 previousPosition;
    float accumulatedRotation;
    
    void Start()
    {
        parentTruck = transform.parent;
        // Preserve the hitch offset before unparenting
        hitchOffset = transform.localPosition;
        transform.parent = null;
        previousPosition = transform.position;
    }

    void Update()
    {
        if (parentTruck == null)
        {
            Destroy(gameObject, 0.1f);
            return;
        }

        // Move to parent position with preserved hitch offset
        Vector3 targetPosition = parentTruck.position + parentTruck.TransformVector(hitchOffset);
        transform.position = targetPosition;
        
        // Calculate movement direction and apply granular rotation
        Vector3 movementDirection = transform.position - previousPosition;
        float distanceTraveled = movementDirection.magnitude;
        
        if (distanceTraveled > 0.001f)
        {
            // Calculate the angle change based on movement direction
            Vector3 normalizedMovement = movementDirection / distanceTraveled;
            Vector3 currentForward = transform.forward;
            
            // Get the signed angle between current forward and movement direction
            float angleChange = Vector3.SignedAngle(currentForward, normalizedMovement, Vector3.up);
            
            // Accumulate rotation based on distance traveled for granular effect
            accumulatedRotation += angleChange * distanceTraveled * rotationSensitivity;
            
            // Apply the accumulated rotation
            transform.rotation = Quaternion.AngleAxis(accumulatedRotation, Vector3.up);
        }
        
        previousPosition = transform.position;
    }
}
