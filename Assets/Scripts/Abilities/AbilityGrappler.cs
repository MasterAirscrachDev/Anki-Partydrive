using UnityEngine;
using System.Collections;

public class AbilityGrappler : MonoBehaviour
{
    Transform targetTransform;
    string targetID;
    string userID;
    [SerializeField] LineRenderer grapplerLine;
    
    public void Setup(Transform target, string targetCarID, string userCarID)
    {
        targetTransform = target;
        targetID = targetCarID;
        userID = userCarID;
        
        // Setup line renderer if it exists
        if(grapplerLine != null)
        {
            grapplerLine.positionCount = 2;
        }
        OnHitTarget();
    }
    void Update()
    {
        if(targetTransform == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Update line renderer to connect user car to target car
        if(grapplerLine != null)
        {
            Vector3 userPos = SR.cet.GetCarVisualPosition(userID);
            Vector3 targetPos = targetTransform.position;
            
            grapplerLine.SetPosition(0, userPos);
            grapplerLine.SetPosition(1, targetPos);
        }
    }
    
    void OnHitTarget()
    {
        
        // Get both car controllers
        CarController userCar = SR.cms.GetController(userID);
        CarController targetCar = SR.cms.GetController(targetID);
        
        if(userCar != null && targetCar != null)
        {
            // Apply 20% boost to user for 8s
            userCar.AddSpeedModifier(20, true, 8f, "GrapplerBoost");
            
            // Apply 20% reduction to target for 8s
            targetCar.AddSpeedModifier(-20, true, 8f, "GrapplerSlow");
            
            // Destroy the grappler after the effect duration
            StartCoroutine(DestroyAfterDuration(8f));
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
