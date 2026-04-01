using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityFreeze : MonoBehaviour
{
    // 1 is full track width, 0.35 is regular car width
    float speed = 0.3f; // How fast the freeze effect moves along the track
    TrackCoordinate currentCoord;
    AbilityController abilityController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(AbilityController ab, float size = 0.35f, float duration = 4f, float speed = 0.3f)
    {
        abilityController = ab;
        transform.localScale = Vector3.zero; // Start with zero scale for the grow effect
        StartCoroutine(LerpToScale(size, 0.4f)); // Grow to target size over 0.5 seconds
        currentCoord = SR.track.WorldspaceToTrackCoordinate(transform.position);
        this.speed = speed;
        Destroy(gameObject, duration); // Destroy the freeze object after the duration expires
    }

    IEnumerator LerpToScale(float target, float duration)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 targetScale = new Vector3(target, target, target);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale; // Ensure it ends at the exact target scale
    }

    // Update is called once per frame
    void Update()
    {
        currentCoord = currentCoord + (speed * Time.deltaTime); // Move along the track based on speed and time
        Vector3 newPosition = SR.track.TrackCoordinateToWorldspace(currentCoord);
        transform.LookAt(newPosition); // Orient towards the direction of movement
        transform.Rotate(0, 180, 0); // Rotate to lay flat on the track
        transform.position = newPosition;


        float size = transform.localScale.x * 0.45f; // Assuming the scale's x component represents the width of the freeze effect
        //use CMS cube check
        List<CarController> cars = SR.cms.CubeCheckControllers(transform.position, transform.forward, size); // Check for cars within the radius of the freeze effect
        
    }
    void DoHitLogic(List<CarController> cars)
    {
        foreach (CarController car in cars)
        {
            if(car == abilityController.GetCarController()) continue; // Skip the car that spawned the freeze effect
            // Apply freeze effect to the hit car
            car.AddSpeedModifier(-100, true, 3f, "Freeze");
            car.UseEnergy(5f);
            abilityController?.ReportDamage(5f); // Report damage back to the ability owner
            Debug.Log($"Car {car.GetID()} hit by Ice Blast, frozen for 3 seconds and took 5 damage!");
        }
    }
    void OnDrawGizmos()
    {
        // Visualize the freeze effect area in the editor
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f); // Light blue with some transparency
        float size = transform.localScale.x * 0.45f; // Assuming the scale's x component represents the width of the freeze effect
        Gizmos.DrawWireCube(transform.position, new Vector3(size,size,size)); // Draw a wire cube representing the freeze area
    }
}
