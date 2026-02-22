using UnityEngine;
using System.Collections;

public class AbilityCrasherBoost : MonoBehaviour
{
    Transform targetTransform;
    string targetID;
    [SerializeField] float travelSpeed = 15f;
    [SerializeField] float boostAmount = 500f;
    [SerializeField] float boostDuration = 2.5f;
    [SerializeField] float slowPercent = 80f;
    [SerializeField] float slowDuration = 3f;
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
            Destroy(gameObject);
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
            // Apply boost and then slow - both modifiers are applied immediately
            // The slow modifier includes a delay built into time (negative time means delayed start)
            targetCar.AddSpeedModifier((int)boostAmount, false, boostDuration, "CrasherBoost");
            
            // Start coroutine to apply slow after boost ends, then destroy
            StartCoroutine(ApplySlowThenDestroy(targetCar));
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    IEnumerator ApplySlowThenDestroy(CarController targetCar)
    {
        // Wait for boost to finish
        yield return new WaitForSeconds(boostDuration);
        
        if(targetCar != null)
        {
            // Apply slow (negative percentage modifier)
            targetCar.AddSpeedModifier((int)-slowPercent, true, slowDuration, "CrasherSlow");
            
            // Spawn slow particle effect at target car
            SR.gas?.SpawnCrasherSlowParticle(targetTransform.position);
        }
        
        // Wait for slow to finish before destroying
        yield return new WaitForSeconds(slowDuration);
        Destroy(gameObject);
    }
}
