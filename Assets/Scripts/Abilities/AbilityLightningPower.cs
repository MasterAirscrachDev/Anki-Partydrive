using System.Collections;
using UnityEngine;

public class AbilityLightningPower : MonoBehaviour
{
    [SerializeField] GameObject lightningCloudPrefab;
    
    public void Setup(CarController[] targets)
    {
        // Spawn a lightning cloud for each target
        foreach (CarController target in targets)
        {
            if (target != null)
            {
                GameObject cloud = Instantiate(lightningCloudPrefab);
                
                // Random horizontal position at high altitude
                Vector3 spawnPos = new Vector3(
                    Random.Range(-20f, 20f),
                    50f,
                    Random.Range(-20f, 20f)
                );
                cloud.transform.position = spawnPos;
                
                // Start the lightning sequence
                StartCoroutine(LightningCloudSequence(cloud, target));
            }
        }
    }

    IEnumerator LightningCloudSequence(GameObject cloud, CarController target)
    {
        LineRenderer lineRenderer = cloud.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        Vector3 startPos = cloud.transform.position;
        Transform targetTransform = SR.cet.GetCarVisualTransform(target.GetID());
        
        // Move to target over 3 seconds
        float moveTime = 2f;
        float elapsed = 0f;
        
        while (elapsed < moveTime && targetTransform != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            
            // Lerp to above the car
            cloud.transform.position = Vector3.Lerp(startPos, targetTransform.position, t);
            
            yield return new WaitForEndOfFrame();
        }
        
        if (targetTransform == null)
        {
            Destroy(cloud);
            yield break;
        }
        
        // Follow the car for 1 second
        float followTime = 1f;
        elapsed = 0f;
        
        while (elapsed < followTime && targetTransform != null)
        {
            elapsed += Time.deltaTime;
            cloud.transform.position = targetTransform.position;
            yield return new WaitForEndOfFrame();
        }
        
        if (targetTransform == null)
        {
            Destroy(cloud);
            yield break;
        }
        
        // Lightning strike!
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, cloud.transform.position);
            lineRenderer.SetPosition(1, targetTransform.position);
        }
        
        // Apply 80% slow for 4 seconds
        target.AddSpeedModifier(-60, true, 4f, "LightningSlow");
        
        // Show lightning for 0.5 seconds
        yield return new WaitForSeconds(0.5f);
        
        // Disperse effect - scale up and fade
        float disperseTime = 0.5f;
        elapsed = 0f;
        Vector3 originalScale = cloud.transform.localScale;
        
        while (elapsed < disperseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disperseTime;
            
            cloud.transform.localScale = originalScale * (1f + t * 2f);
            
            yield return new WaitForEndOfFrame();
        }
        
        // Despawn
        Destroy(cloud);
    }
}
