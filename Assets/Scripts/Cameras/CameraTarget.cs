using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    [SerializeField] bool trackOnSetActive = true; // whether to set the camera target to this object when it becomes active
    [SerializeField] bool makeTarget = false; // whether to set the camera target to this object at the start of the scene
    [SerializeField] float followSpeed = 5f; // the speed at which the camera follows this target
    [SerializeField] float rotationSpeed = 5f; // the speed at which the camera rotates to match this target
    [SerializeField] float fovSpeed = 5f; // the speed at which the camera changes its field of view when moving to this target 
    [SerializeField] float targetFOV = 60f; // the field of view the camera should have when focused on this target

    void OnEnable()
    {
        if (trackOnSetActive && SR.cameraController != null)
        {
            if (makeTarget) { SR.cameraController.SetTarget(transform, followSpeed, rotationSpeed); }
            else { SR.cameraController.SetTargetPosition(transform, followSpeed); }
            
            SR.cameraController.SetFOV(targetFOV, false, fovSpeed);
        }
    }
}
