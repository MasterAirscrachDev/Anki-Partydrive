using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CarEntityPosition : MonoBehaviour
{
    [SerializeField] float speedTuning = 0.0019f;
    [SerializeField] Material solidMaterial, transparentMaterial;
    public CarModelManager carModelManager;
    TrackSpline trackSpline;
    int speed = 0, segmentIdx = 0, shift = 0;
    float trackPieceProgression = 0, trackPieceLength = 0, horizontalOffset = 0;
    Vector3 lastPosition;
    TrackGenerator track;
    bool showOnTrack = true;
    void Start()
    {
        track = TrackGenerator.track;
        //if not the editor, destroy the mesh renderer
        if(!Application.isEditor || true){
            GetComponent<MeshRenderer>().enabled = false;
            Destroy(GetComponent<MeshFilter>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        trackPieceProgression += ((speedTuning * speed) * trackPieceLength) * Time.deltaTime;
        if(trackSpline != null){
            Vector3 targetPos = trackSpline.GetPoint(trackPieceProgression, horizontalOffset);
            if(!showOnTrack){
                targetPos.y -= 20; //hide the car
            }
            transform.position = targetPos;
            if(Vector3.Distance(transform.position, lastPosition) > 0.01f){ //should fix weirdness when stopping
                transform.LookAt(lastPosition);
                lastPosition = transform.position;
                transform.Rotate(0, 180, 0);
            }
        }
        if(trackPieceProgression >= 1 && shift > 2){
            trackPieceProgression = 0;
            shift++;
            trackSpline = track.GetTrackSpline(segmentIdx + shift);
            if(trackSpline == null){trackSpline = track.GetTrackSpline(segmentIdx + ++shift); }
        }
    }
    public void SetTrackSpline(TrackSpline trackSpline, int idx){
        if(idx != this.segmentIdx){ //only update if the track spline is different
            this.segmentIdx = idx;
            this.trackSpline = trackSpline;
            trackPieceProgression = 0;
            shift = 0;
            trackPieceLength = trackSpline.GetLength(horizontalOffset);
        }
    }
    public void SetOffset(float offset){
        this.horizontalOffset = offset; //change this to smooth when changed
    }
    public void SetSpeed(int speed){
        this.speed = speed;
    }
    public void SetTrust(int trust){
        GetComponent<MeshRenderer>().material = (trust > 1) ? solidMaterial : transparentMaterial;
        carModelManager.SetHolo(trust < 2);
        if(trust == 2){ //certainly on track
            showOnTrack = true;
        }
        else if(trust == 0){ //lost or delocalised
            showOnTrack = false; 
        }
    }
    public void Delocalise(){
        trackPieceProgression = 0;
        trackPieceLength = 0;
        trackSpline = null;
        speed = 0;
        shift = 0;
        showOnTrack = false;
        transform.position = transform.position + new Vector3(0, -10, 0); //reset position to the last known position
    }
    public bool IsDelocalised(){
        return trackSpline == null;
    }
    public (uint i, float x, float y) GetIXY(){
        return ((uint)segmentIdx, horizontalOffset, trackPieceProgression);
    }
}
