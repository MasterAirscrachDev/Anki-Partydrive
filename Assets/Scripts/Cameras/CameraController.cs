using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // the target the camera can match position and rotation to;
    public bool smoothFollow = true; // whether the camera smoothly follows the target or snaps to it
    public float followSpeed = 5f; // the speed at which the camera follows the
    public float rotationSpeed = 5f; // the speed at which the camera rotates to look at the target
    public float fovSpeed = 5f; // the speed at which the camera changes its field of view
    Quaternion targetRotation; // desired rotation (when no target)
    Vector3 targetPosition; // desired position (when no target)
    float targetFOV; // desired field of view (when no target)
    Camera cam;
    bool isOrthographic = false;
    const float lerpSlow = 0.2f;
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) { cam = Camera.main; }
        targetRotation = transform.rotation;
        targetPosition = transform.position;
        targetFOV = cam.fieldOfView;
    }
    void LateUpdate()
    {
        if (target != null)
        {
            // Follow target position
            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(transform.position, target.position, followSpeed * lerpSlow * Time.deltaTime);
                //rotate to match target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationSpeed * lerpSlow * Time.deltaTime);
            }
            else
            {
                transform.position = target.position;
                transform.rotation = target.rotation;
            }
        }
        else
        {
            // No target - smoothly return to default position and rotation
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * lerpSlow * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * lerpSlow * Time.deltaTime);
        }
        if(isOrthographic) {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetFOV, fovSpeed * lerpSlow * Time.deltaTime);
        }
        else {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * lerpSlow * Time.deltaTime);
        }
    }
    public void SetTarget(Transform newTarget, float followSpeed = 5f, float rotationSpeed = 5f, bool smoothFollow = true)
    {
        this.smoothFollow = smoothFollow;
        target = newTarget;
        this.followSpeed = followSpeed;
        this.rotationSpeed = rotationSpeed;
    }
    public void SetFOV(float newFOV, bool isOrthographic = false, float fovSpeed = 5f)
    {
        targetFOV = newFOV;
        if(isOrthographic && !this.isOrthographic) {
            cam.orthographic = true;
            float currentFOV = cam.fieldOfView;
            cam.orthographicSize = currentFOV / 3.5f;
            //Debug.Log($"Switching to orthographic mode. Current FOV: {currentFOV}, setting orthographic size to: {cam.orthographicSize}");
            this.isOrthographic = true;
        }
        else if(!isOrthographic && this.isOrthographic) {
            cam.orthographic = false;
            this.isOrthographic = false;
        }
        this.fovSpeed = fovSpeed;
        //Debug.Log($"Setting FOV to {newFOV}({targetFOV}) (orthographic: {isOrthographic}) with speed {fovSpeed}");
    }
    public void SetTargetPosition(Transform newTargetPosition, float followSpeed = 5f)
    {
        target = null;
        targetPosition = newTargetPosition.position;
        targetRotation = newTargetPosition.rotation;
        this.followSpeed = followSpeed;
    }
    public void SetTargetPosition(Vector3 newPosition, Quaternion newRotation, float followSpeed = 5f)
    {
        target = null;
        targetPosition = newPosition;
        targetRotation = newRotation;
        this.followSpeed = followSpeed;
    }
}
