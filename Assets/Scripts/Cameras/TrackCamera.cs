using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TrackCamera : MonoBehaviour
{
    [SerializeField] Vector2 xzOffset = new Vector2(0, 0);
    [SerializeField] float zoomOffset = 0f;
    bool overrideActive = false;
    [SerializeField] Transform overrideTransform;
    float camSize = 10f;
    
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateSpeed = 60f;
    [SerializeField] float zoomSpeed = 3f;
    PlayerController editingPlayer;
    void OnEnable(){
        if(SR.cameraController != null){
            PullCameraToTrack();
        }
    }
    //called whenever the track has changed and we need to update our position, rotation, and zoom to fit the new track
    public void TrackUpdated(Vector3 center, Vector2 size, float? overrideRotation = null){
        if(!Application.isPlaying){ return; }
        try
        {
            // Set target position from the center parameter
            Vector3 targetPos = new Vector3(center.x, 5, center.z);

            //log the ratio A:B of the track size, to determine if it's more horizontal or vertical
            if(size.x == 0 || size.y == 0){
                //Debug.LogWarning($"Track size has zero dimension: {size}. Defaulting ratio to 1.");
            }
            float ratio = size.x / size.y;
            if(size.y > size.x){
                ratio = size.y / size.x;
            }
            //Debug.Log($"Track size: {size}, Ratio (A:B): {ratio}");

            
            // Calculate camera size based on the track bounds
            float maxDist = Mathf.Max(size.x, size.y);
            
            // Scale fixNum with track size (logarithmic scaling) to prevent excessive zoom on large tracks while still providing a boost for small tracks
            float fixNum = Mathf.Log(maxDist + 1, 2) * 0.6f; // Adjust the multiplier (0.5f) to control the strength of the fix
            camSize = Mathf.Max(1.5f, (maxDist / 2.3f) - 0.25f) + fixNum;
            if(ratio > 1.3f)
            {
                camSize -= 0.5f; // If the track is significantly wider than it is tall, reduce camera size to keep it closer to the track
            }
            
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
            
            // Offset camera to the right by 30% of the longer side to compensate for left UI
            // This shifts the visible track rightward on screen, away from UI
            float uiCompensation = maxDist * 0.30f;
            if(targetRotation == 90){
                // Camera facing along Z, left is -Z, so shift +Z to move content right on screen
                targetPos += new Vector3(0, 0, uiCompensation);
            }
            else{
                // Camera facing along X, left is -X, so shift +X to move content right on screen
                targetPos += new Vector3(-uiCompensation, 0, 0);
            }
            transform.position = targetPos;
            transform.rotation = Quaternion.Euler(70, targetRotation - 3, 0);
            PullCameraToTrack();
            overrideActive = false; // Reset override after applying track update
        }catch(System.Exception ex)
        {
            Debug.LogError($"Error in TrackCamera.TrackUpdated: {ex.Message}");
            return;
        }
        
        
    }
    public void PullCameraToTrack(){
        if(overrideActive && overrideTransform != null){
            SR.cameraController.SetTarget(overrideTransform.GetChild(0));
        }else{
            SR.cameraController.SetTarget(transform);
        }
        SR.cameraController.SetFOV(camSize + zoomOffset, true);
    }
    
    public void EditOverride(){
        // Check if we have a valid track (camSize is set by TrackUpdated)
        if(!SR.track.hasTrack){ return; }
        if(overrideTransform == null){ return; }
        
        // Find player 1
        var players = FindObjectsOfType<PlayerController>();
        if(players.Length == 0){ return; }
        editingPlayer = players[0];
        
        // Put player 1 into camera control mode
        editingPlayer.SetInputMode(PlayerController.PlayerInputMode.CameraControl);
        overrideActive = true;
        SR.cameraController.SetTarget(overrideTransform.GetChild(0), 20, 20); //make it snappy while editing
        SR.cameraController.SetFOV(camSize + zoomOffset, true, 20); //snappy FOV change as well
    }
    
    public void ClearOverride(){
        overrideActive = false;
        // Reset override transform position and rotation to default
        overrideTransform.localPosition = Vector3.zero;
        overrideTransform.rotation = Quaternion.identity;
        PullCameraToTrack();

    }
    
    void Update(){
        if(!overrideActive || editingPlayer == null || overrideTransform == null){ return; }
        
        // Read camera inputs from player 1
        Vector2 move = editingPlayer.cameraMove.ReadValue<Vector2>();
        Vector2 shift = editingPlayer.cameraShift.ReadValue<Vector2>();
        bool exit = editingPlayer.cameraExit.WasPressedThisFrame();
        
        // Move override transform relative to its own forward/right directions
        Vector3 moveDir = (overrideTransform.right * move.x + overrideTransform.forward * move.y) * moveSpeed * Time.deltaTime;
        moveDir.y = 0f;
        overrideTransform.position += moveDir;
        
        // Rotate override transform (shift left/right)
        overrideTransform.Rotate(Vector3.up, shift.x * rotateSpeed * Time.deltaTime, Space.Self);
        
        // Zoom in/out (shift up/down)
        zoomOffset -= shift.y * zoomSpeed * Time.deltaTime;
        zoomOffset = Mathf.Clamp(zoomOffset, -20f, 20f); // Prevent zooming in too close or too far
        
        // Apply updated FOV
        SR.cameraController.SetFOV(camSize + zoomOffset, true);
        
        // Exit override editing mode
        if(exit){
            editingPlayer.SetInputMode(PlayerController.PlayerInputMode.Menu);
            editingPlayer = null;
        }
    }
}
