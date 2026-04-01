using UnityEngine;
using System.Collections;

/// <summary>
/// Visual effect that follows a disabled car for the duration of the disable.
/// Automatically destroys itself when the disable ends or if the car no longer exists.
/// </summary>
public class AbilityDisabled : MonoBehaviour
{
    float duration = 3.5f;
    
    string userID;
    float elapsed = 0f;
    
    public void Setup(AbilityController ab, float duration = 3.5f)
    {
        this.duration = duration;
        userID = ab.GetCarController().GetID();
        StartCoroutine(DisableTimer());
    }

    void Update()
    {
        // Follow the user's visual position
        if(string.IsNullOrEmpty(userID))
        {
            Destroy(gameObject);
            return;
        }
        
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
    
    IEnumerator DisableTimer()
    {
        yield return new WaitForSeconds(duration);
        
        // Duration complete, destroy self
        Destroy(gameObject);
    }
}
