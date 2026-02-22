using UnityEngine;
using System.Collections;

public class AbilityRecharger : MonoBehaviour
{
    [SerializeField] float energyPerTick = 10f;
    [SerializeField] float tickInterval = 1f;
    [SerializeField] float totalDuration = 5f;
    
    string userID;
    CarController userCar;
    
    public void Setup(CarController user)
    {
        userCar = user;
        userID = user.GetID();
        StartCoroutine(RechargeLoop());
    }

    void Update()
    {
        // Follow the user's visual position
        if(string.IsNullOrEmpty(userID)) return;
        
        Transform userTransform = SR.cet.GetCarVisualTransform(userID);
        if(userTransform != null)
        {
            transform.position = userTransform.position;
            transform.rotation = userTransform.rotation;
        }
        else
        {
            // User no longer exists, destroy self
            Destroy(gameObject);
        }
    }
    
    IEnumerator RechargeLoop()
    {
        float elapsed = 0f;
        
        while(elapsed < totalDuration)
        {
            if(userCar != null)
            {
                userCar.ChargeEnergy(energyPerTick);
            }
            else
            {
                // User car no longer exists
                Destroy(gameObject);
                yield break;
            }
            
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
        
        // Duration complete, destroy self
        Destroy(gameObject);
    }
}
