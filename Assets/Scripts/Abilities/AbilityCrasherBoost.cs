using UnityEngine;
using System.Collections;

public class AbilityCrasherBoost : MonoBehaviour
{
    Transform targetTransform;
    string targetID;
    [SerializeField] float travelSpeed = 15f;
    bool hasHit = false;
    
    public void Setup(Transform target, string targetCarID)
    {
        targetTransform = target;
        targetID = targetCarID;
    }
    
    void Update()
    {
        if(targetTransform == null)
        {
            return;
        }
        if(hasHit)
        {
            transform.position = targetTransform.position;
            return;
        }
        
        // Move towards target
        Vector3 direction = (targetTransform.position - transform.position).normalized;
        transform.position += direction * travelSpeed * Time.deltaTime;
        
        // Check if close enough to hit
        if(Vector3.Distance(transform.position, targetTransform.position) < 1f)
        {
            OnHitTarget();
        }
    }
    
    void OnHitTarget()
    {
        hasHit = true;
        
        // Get the target car controller
        CarController targetCar = SR.cms.GetController(targetID);
        if(targetCar != null)
        {
            // Apply 500 boost for 2.5s
            targetCar.AddSpeedModifier(500, false, 2.5f, "CrasherBoost");
            
            // Then apply 80% slow for 3s after the boost ends
            StartCoroutine(ApplySlowAfterDelay(targetCar, 2.5f));
        }
        
        // Destroy the projectile
        Destroy(gameObject);
    }
    
    IEnumerator ApplySlowAfterDelay(CarController targetCar, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if(targetCar != null)
        {
            // Apply 80% slow (negative 80% modifier)
            targetCar.AddSpeedModifier(-80, true, 3f, "CrasherSlow");
        }
    }
}
