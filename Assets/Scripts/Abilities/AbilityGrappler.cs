using UnityEngine;
using System.Collections;

public class AbilityGrappler : MonoBehaviour
{
    Transform targetTransform;
    string targetID;
    string userID;
    [SerializeField] LineRenderer grapplerLine;
    bool abilityActive = false;
    
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
        if(targetTransform == null || !abilityActive)
        {
            Destroy(gameObject);
            return;
        }
        
        // Check if user has overtaken the target - if so, end the ability early
        if(HasUserOvertakenTarget())
        {
            EndAbilityEarly();
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
    
    bool HasUserOvertakenTarget()
    {
        // Get track coordinates for both cars
        TrackCoordinate userCoord = SR.cet.GetCarTrackCoordinate(userID);
        TrackCoordinate targetCoord = SR.cet.GetCarTrackCoordinate(targetID);
        
        if(userCoord == null || targetCoord == null) return false;
        
        // Compare positions - user is ahead if their combined position value is higher
        double userPosition = (userCoord.idx * 1000.0) + userCoord.progression;
        double targetPosition = (targetCoord.idx * 1000.0) + targetCoord.progression;
        
        return userPosition > targetPosition;
    }
    
    void EndAbilityEarly()
    {
        abilityActive = false;
        
        // Remove the speed modifiers early
        CarController userCar = SR.cms.GetController(userID);
        CarController targetCar = SR.cms.GetController(targetID);
        
        if(userCar != null)
        {
            userCar.AddSpeedModifier(0, true, 0f, "GrapplerBoost"); // Override with 0 duration
        }
        if(targetCar != null)
        {
            targetCar.AddSpeedModifier(0, true, 0f, "GrapplerSlow"); // Override with 0 duration
        }
        
        Destroy(gameObject);
    }
    
    void OnHitTarget()
    {
        // Get both car controllers
        CarController userCar = SR.cms.GetController(userID);
        CarController targetCar = SR.cms.GetController(targetID);
        
        if(userCar != null && targetCar != null)
        {
            abilityActive = true;
            
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
        abilityActive = false;
        Destroy(gameObject);
    }
}
