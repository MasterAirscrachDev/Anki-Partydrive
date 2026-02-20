using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCamera : MonoBehaviour
{
    [SerializeField] Vector2 xzOffset = new Vector2(0, 0);
    [SerializeField][Range(-5, 5)] float zoomOffset = 0f;
    float camSize = 10f;
    void OnEnable(){
        if(SR.cameraController != null){
            SR.cameraController.SetTarget(transform);
        }
    }
    public void TrackUpdated(Vector3 center, Vector2 size, float? overrideRotation = null){
        if(!Application.isPlaying){ return; }
        try
        {
            // Set target position from the center parameter
            Vector3 targetPos = new Vector3(center.x, 5, center.z);
            
            // Calculate camera size based on the track bounds
            float maxDist = Mathf.Max(size.x, size.y);
            
            // Scale fixNum with track size (logarithmic scaling) to prevent excessive zoom on large tracks while still providing a boost for small tracks
            float fixNum = Mathf.Log(maxDist + 1, 2) * 0.6f; // Adjust the multiplier (0.5f) to control the strength of the fix
            camSize = Mathf.Max(1.5f, (maxDist / 2.3f) - 0.25f) + fixNum;
            
            float targetRotation = 0;
            // If track is taller than it is wide, rotate the camera 90 degrees
            if(size.x < size.y){
                targetRotation = 90;
                targetPos += new Vector3(xzOffset.y, 0, -xzOffset.x);
            }
            else{
                targetRotation = 0;
                targetPos += new Vector3(xzOffset.x, 0, xzOffset.y);
            }
            if(overrideRotation.HasValue){
                targetRotation = overrideRotation.Value;
            }
            
            // Offset camera to the right by 10% of the longer side to compensate for left UI
            // This shifts the visible track rightward on screen, away from UI
            float uiCompensation = maxDist * 0.10f;
            if(targetRotation == 90){
                // Camera facing along Z, left is -Z, so shift +Z to move content right on screen
                targetPos += new Vector3(0, 0, uiCompensation);
            }
            else{
                // Camera facing along X, left is -X, so shift +X to move content right on screen
                targetPos += new Vector3(uiCompensation, 0, 0);
            }

            transform.position = targetPos;
            transform.rotation = Quaternion.Euler(70, targetRotation - 3, 0);
            PullCameraToTrack();
        }catch(System.Exception ex)
        {
            Debug.LogError($"Error in TrackCamera.TrackUpdated: {ex.Message}");
            return;
        }
        
        
    }
    public void PullCameraToTrack(){
        SR.cameraController.SetTarget(transform);
        SR.cameraController.SetFOV(camSize + zoomOffset, true);
    }
}
