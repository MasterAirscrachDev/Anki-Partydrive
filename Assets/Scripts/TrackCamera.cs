using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCamera : MonoBehaviour
{
    [SerializeField] Transform trackManager;
    Vector3 targetPos = new Vector3(0, 10, 0);
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
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, camSize, Time.deltaTime * 2);
    }
    public void TrackUpdated(){
        if(!Application.isPlaying){ return; }
        //get the average position of all track pieces
        Vector3 pos = Vector3.zero;
        for(int i = 0; i < trackManager.childCount; i++){
            pos += trackManager.GetChild(i).position;
        }
        targetPos = pos / trackManager.childCount;
        targetPos.y = 10;
        //the diagonal bounds of the track
        float maxDist = 0;
        float heighestX = 0, heighestZ = 0, lowestX = 0, lowestZ = 0;
        for(int i = 0; i < trackManager.childCount; i++){
            float dist = Vector3.Distance(trackManager.GetChild(i).position, targetPos);
            if(dist > maxDist){
                maxDist = dist;
            }
            if(trackManager.GetChild(i).position.x > heighestX){
                heighestX = trackManager.GetChild(i).position.x;
            }
            if(trackManager.GetChild(i).position.x < lowestX){
                lowestX = trackManager.GetChild(i).position.x;
            }
            if(trackManager.GetChild(i).position.z > heighestZ){
                heighestZ = trackManager.GetChild(i).position.z;
            }
            if(trackManager.GetChild(i).position.z < lowestZ){
                lowestZ = trackManager.GetChild(i).position.z;
            }
        }
        //if its taller than it is wide, rotate the camera 90 degrees
        if(heighestX - lowestX < heighestZ - lowestZ){
            camSize = Mathf.Max(2, (heighestZ - lowestZ) / 2.3f);
            cam.transform.rotation = Quaternion.Euler(90, 90, 0);
        }
        else{
            camSize = Mathf.Max(2, maxDist / 2.3f);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }
}
