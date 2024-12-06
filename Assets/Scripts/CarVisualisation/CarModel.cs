using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarModel : MonoBehaviour
{
    [SerializeField] float speedTuning = 0.01f;
    [SerializeField] Material solidMaterial, transparentMaterial;
    TrackSpline trackSpline;
    int speed = 0;
    float trackPieceProgression = 0, trackPieceLength = 0;
    float horizontalOffset = 0;
    Vector3 lastPosition;

    int timout;
    // Start is called before the first frame update
    void Start()
    {
        timout = 5;
        StartCoroutine(Timeout());
    }
    IEnumerator Timeout(){
        while(timout > 0){
            yield return new WaitForSeconds(1);
            timout--;
        }
        FindObjectOfType<CarEntityTracker>().RemoveEntity(gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        trackPieceProgression += ((speedTuning * speed) * trackPieceLength) * Time.deltaTime;
        if(trackSpline != null){
            transform.position = trackSpline.GetPoint(trackPieceProgression, horizontalOffset);
            transform.LookAt(lastPosition);
            lastPosition = transform.position;
            transform.Rotate(0, 180, 0);
        }
    }
    public void SetTrackSpline(TrackSpline trackSpline){
        timout = 5;
        if(trackSpline != this.trackSpline){ //only update if the track spline is different
            this.trackSpline = trackSpline;
            trackPieceProgression = 0;
            trackPieceLength = trackSpline.GetLength(horizontalOffset);
        }
    }
    public void SetSpeedAndOffset(int speed, float offset){
        this.speed = speed;
        this.horizontalOffset = offset;
    }
    public void SetTrustedPosition(bool trusted){
        GetComponent<MeshRenderer>().material = trusted ? solidMaterial : transparentMaterial;
    }

}
