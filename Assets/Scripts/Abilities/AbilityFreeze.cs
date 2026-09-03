using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityFreeze : MonoBehaviour
{
    // 1 is full track width, 0.35 is regular car width
    float freezeDuration = 3f; // How long the freeze effect lasts on cars
    TrackCoordinate currentCoord;
    AbilityController abilityController;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(AbilityController ab, float size = 0.35f, float duration = 4f, int speed = 500, float freezeDuration = 3f)
    {
        abilityController = ab;
        transform.localScale = Vector3.zero; // Start with zero scale for the grow effect
        StartCoroutine(LerpToScale(size, 0.4f)); // Grow to target size over 0.5 seconds
        currentCoord = SR.track.WorldspaceToTrackCoordinate(transform.position);
        if(currentCoord == null)
        {
            // Clone: GetCarTrackCoordinate hands back the car's *live* coordinate object (it's a
            // class, not a struct). Using it directly would drive the wall at whatever speed the
            // car happens to be doing — and corrupt the car's own position, since both would be
            // calling Progress() on the same instance.
            currentCoord = SR.cet.GetCarTrackCoordinate(ab.GetCarController().GetID())?.Clone();
            if(currentCoord == null)
            {
                Debug.LogError("AbilityFreeze: Failed to get track coordinate for ability position and car position!");
                currentCoord = new TrackCoordinate(0, 0, 0);
            }
        }
        currentCoord.speed = speed; // How fast the freeze effect moves along the track
        this.freezeDuration = freezeDuration;
        Destroy(gameObject, duration); // Destroy the freeze object after the duration expires
    }
    public void OverrideOffset()
    {
        currentCoord.offset = 0f; // Center the freeze effect on the track
        freezeDuration = 5f; //lazy so im just using this function to trigger overdrive behavior
    }

    IEnumerator LerpToScale(float target, float duration)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 targetScale = new Vector3(target, target, target);
        float elapsed = 0f;

        while (elapsed < duration) {
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.localScale = targetScale; // Ensure it ends at the exact target scale
    }

    // Update is called once per frame
    void Update()
    {
        if(currentCoord.idx == 0 && !SR.track.IsMat()){
            currentCoord.idx = 1;
            currentCoord.progression = 0;
        } //index 0 doesnt exist for non-mat tracks, so skip it to avoid weird behavior

        // We should never be positioned on a splineless segment: index 0 (PreFinishLine) is skipped
        // above and everything else has a spline. If we land on one, the index has gone somewhere it
        // shouldn't — GetProgress would divide by a zero track width and hand back NaN from here on,
        // so fail loudly instead of limping along on a poisoned coordinate.
        if(SR.track.GetTrackSpline(currentCoord.idx) == null){
            Debug.LogError($"AbilityFreeze: no spline at track index {currentCoord.idx} ({SR.track.GetSegmentType(currentCoord.idx)}) - aborting ability");
            Destroy(gameObject);
            return;
        }

        float step = TrackPathSolver.GetProgress(SR.track.GetSegmentType(currentCoord.idx), SR.track.GetSegmentID(currentCoord.idx), SR.track.GetSegmentReversed(currentCoord.idx), currentCoord, Time.deltaTime);

        // Use the += operator rather than Progress(): it carries the leftover distance into the
        // next segment and wraps the index correctly. Progress() clamps at 1 and never resets,
        // which left the old code incrementing idx on every single frame once the wall had
        // finished its first segment.
        currentCoord += step;

        Vector3 newPosition = SR.track.TrackCoordinateToWorldspace(currentCoord);
        transform.LookAt(newPosition); // Orient towards the direction of movement
        transform.Rotate(0, 180, 0); // Rotate to lay flat on the track
        transform.position = newPosition;


        float size = transform.localScale.x * 0.45f; // Assuming the scale's x component represents the width of the freeze effect
        //use CMS cube check
        List<CarController> cars = SR.cms.CubeCheckControllers(transform.position, transform.forward, size); // Check for cars within the radius of the freeze effect
        DoHitLogic(cars);
    }
    void DoHitLogic(List<CarController> cars)
    {
        foreach (CarController car in cars)
        {
            if(car == abilityController.GetCarController()) continue; // Skip the car that spawned the freeze effect
            car.SetStatusEffect(CarStatus.Frozen, freezeDuration); // Freeze the car for 3 seconds
            car.UseEnergy(5f);
            abilityController?.ReportDamage(5f); // Report damage back to the ability owner
            //Debug.Log($"Car {car.GetID()} hit by Ice Blast, frozen for 3 seconds and took 5 damage!");
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
