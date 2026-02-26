using System.Collections;
using UnityEngine;

public class AbilityCone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float lifetime = 30f;
    [SerializeField] float hitCheckDistance = 0.15f; // Distance behind the cone to check for hits
    [SerializeField] float hitCheckWidth = 0.08f; // Width of hitcheck area
    [SerializeField] float damage = 15f;
    [SerializeField] int slowPercent = -30; // Negative for slowdown
    [SerializeField] float slowDuration = 2f;
    [SerializeField] float horizontalKnockback = 0.5f;
    [SerializeField] float knockUpForce = 4f;
    [SerializeField] float knockUpTorque = 10f;
    
    CarController owner;
    bool isHit = false;
    float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    public void Setup(CarController ownerController)
    {
        owner = ownerController;
    }

    void Update()
    {
        if (isHit) return;
        
        // Check if lifetime exceeded
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        CheckForCollision();
    }

    void CheckForCollision()
    {
        // Calculate check position behind the cone
        Vector3 checkCenter = transform.position - transform.forward * (hitCheckDistance / 2f);
        
        // Use cube check similar to hazard zones
        var hits = SR.cms.CubeCheckControllers(checkCenter, -transform.forward, hitCheckWidth);
        
        foreach (CarController hit in hits)
        {
            if (hit == owner) continue; // Don't hit the owner
            
            // Apply damage and slow
            ApplyEffect(hit);
            
            // Trigger visual effect and destroy
            TriggerHitEffect();
            return; // Only one hit needed
        }
    }

    void ApplyEffect(CarController target)
    {
        // Apply damage
        target.UseEnergy(damage);
        
        // Apply slow
        target.AddSpeedModifier(slowPercent, true, slowDuration, "TrafficCone");
        
        // Report damage to owner for stats
        AbilityController abilityController = GetComponent<AbilityController>();
        abilityController?.ReportDamage(damage);
    }

    void TriggerHitEffect()
    {
        isHit = true;
        GameObject coneVisual = transform.GetChild(0).gameObject; // Assuming the visual is the first child
        
        Destroy(gameObject, 3f); // Destroyafter 3 seconds
        SR.sfx.PlaySFX(SFXEvent.TrafficConeHit);
        // Use manual animation instead of physics for consistent behavior
        StartCoroutine(AnimateConeVisual(coneVisual.transform));
    }
    
    IEnumerator AnimateConeVisual(Transform cone)
    {
        Vector3 startPos = cone.position;
        //upwards force plus some random horizontal for visual interest - will be affected by gravity in the coroutine
        Vector3 velocity = Vector3.up * knockUpForce + new Vector3(Random.Range(-horizontalKnockback, horizontalKnockback), 0, Random.Range(-horizontalKnockback, horizontalKnockback)) * (knockUpForce / 2f);
        Vector3 angularVelocity = Random.insideUnitSphere * knockUpTorque * 50f; // degrees per second
        float gravity = 9.81f; // Use our own gravity constant
        Vector3 lastPosition = startPos;
        
        // Skip first frame to avoid initial spike in deltaTime from object creation
        yield return new WaitForEndOfFrame();
        
        while(gameObject != null)
        {
            // Cap deltaTime to prevent jumps from frame hitches (max ~60fps equivalent)
            float dt = Mathf.Min(Time.deltaTime, 0.016f) / 2;
            
            // Apply gravity to velocity
            velocity += Vector3.down * gravity * dt;
            
            // Move cone
            cone.position += velocity * dt;
            
            // Rotate cone
            cone.Rotate(angularVelocity * dt);
            
            // Debug visualization
            Debug.DrawLine(lastPosition, cone.position, Color.Lerp(Color.red, Color.green, velocity.magnitude / 10f), 8f);
            lastPosition = cone.position;
            
            yield return new WaitForEndOfFrame();
        }
    }
    
    // Legacy coroutine removed - now using AnimateConeVisual
    //command to test hit effect from context menu
    [ContextMenu("Test Hit Effect")]
    void TestHitEffect()
    {
        TriggerHitEffect();
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the hit check area
        Gizmos.color = Color.red;
        Vector3 checkCenter = transform.position - transform.forward * (hitCheckDistance / 2f);
        Gizmos.matrix = Matrix4x4.TRS(checkCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(hitCheckWidth, hitCheckWidth, hitCheckDistance));
    }
}
