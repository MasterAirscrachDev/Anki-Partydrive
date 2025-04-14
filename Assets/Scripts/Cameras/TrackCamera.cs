using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCamera : MonoBehaviour
{
    Vector3 targetPos = new Vector3(0, 5, 0);
    [SerializeField] Vector2 xzOffset = new Vector2(0, 0);
    float targetRotation = 0;
    float camSize = 2;
    Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(70, targetRotation - 3, 0), Time.deltaTime * 2);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, camSize, Time.deltaTime * 2);
    }
    public void TrackUpdated(Vector3 center, Vector2 size){
        if(!Application.isPlaying){ return; }
        
        // Set target position from the center parameter
        targetPos = new Vector3(center.x, 5, center.z);
        
        // Calculate camera size based on the track bounds
        float maxDist = Mathf.Max(size.x, size.y);
        
        // If track is taller than it is wide, rotate the camera 90 degrees
        if(size.x < size.y){
            camSize = Mathf.Max(1.5f, (size.y / 2.3f) -0.25f);
            targetRotation = 90;
            targetPos += new Vector3(xzOffset.y, 0, -xzOffset.x);
        }
        else{
            camSize = Mathf.Max(1.5f, (maxDist / 2.3f) - 0.25f);
            targetRotation = 0;
            targetPos += new Vector3(xzOffset.x, 0, xzOffset.y);
        }
    }
}
