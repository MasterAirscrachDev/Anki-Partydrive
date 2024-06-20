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
        //get the average position of all track pieces
        Vector3 pos = Vector3.zero;
        for(int i = 0; i < trackManager.childCount; i++){
            pos += trackManager.GetChild(i).position;
        }
        targetPos = pos / trackManager.childCount;
        targetPos.y = 10;
        //the diagonal bounds of the track
        float maxDist = 0;
        for(int i = 0; i < trackManager.childCount; i++){
            float dist = Vector3.Distance(trackManager.GetChild(i).position, targetPos);
            if(dist > maxDist){
                maxDist = dist;
            }
        }
        camSize = Mathf.Max(2, maxDist / 2);
    }
}
