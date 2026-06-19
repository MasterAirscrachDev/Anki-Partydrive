using UnityEngine;
using System.Collections;

public class AbilityRecharger : MonoBehaviour
{
    [SerializeField] float totalDuration = 5f;
    [SerializeField] float totalCharge = 100f;
    
    string userID;
    CarController userCar;
    Transform userVisualTransform;
    
    float startTime;
    bool isActive;
    float lastChargedAmount;
    
    public void Setup(CarController user)
    {
        userCar = user;
        userID = user.GetID();
        startTime = Time.time;
        isActive = true;
        lastChargedAmount = 0f;
        userVisualTransform = SR.cet.GetCarVisualTransform(userID);
    }

    void Update() {
        // Follow the user's visual position
        if(string.IsNullOrEmpty(userID)) return;
        if(userVisualTransform != null) {
            transform.position = userVisualTransform.position;
            transform.rotation = userVisualTransform.rotation;
            
            // Smoothly charge over duration, maintaining 25 raw energy minimum
            float elapsed = Time.time - startTime;
            if(elapsed < totalDuration && isActive && userCar != null) {
                float progress = Mathf.Clamp01(elapsed / totalDuration);
                
                // Calculate the target charge-so-far, then send only the delta since last frame
                float targetChargedAmount = Mathf.Lerp(0f, totalCharge, progress);
                float delta = targetChargedAmount - lastChargedAmount;
                if(delta > 0f) {
                    userCar.ChargeEnergy(delta);
                    lastChargedAmount = targetChargedAmount;
                }
                
                // Enforce the raw energy floor regardless of curve progress
                if(userCar.GetEnergy() < 25f) {
                    userCar.ChargeEnergy(25f - userCar.GetEnergy());
                }
            } else if (elapsed >= totalDuration) {
                // Duration complete, destroy self
                Destroy(gameObject);
            }
        }
        else { Destroy(gameObject); } // User no longer exists, destroy self
    }
}