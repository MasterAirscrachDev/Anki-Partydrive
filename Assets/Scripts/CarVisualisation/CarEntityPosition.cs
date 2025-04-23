using UnityEngine;
using static OverdriveServer.NetStructures;

public class CarEntityPosition : MonoBehaviour
{
    [SerializeField] Material solidMaterial, transparentMaterial;
    public CarModelManager carModelManager;
    TrackSpline trackSpline;
    int speed = 0, segmentIdx = 0, shift = 0;
    float trackPieceProgression = 0, horizontalOffset = 0;
    Vector3 lastPosition;
    TrackGenerator track;
    bool showOnTrack = true;
    void Start()
    {
        track = TrackGenerator.track;
        //if not the editor, destroy the mesh renderer
        if(!Application.isEditor){
            GetComponent<MeshRenderer>().enabled = false;
            if(!Application.isEditor){ Destroy(GetComponent<MeshFilter>()); }
        }
    }

    // Update is called once per frame
    void Update()
    {
        trackPieceProgression += GetProgress(Time.deltaTime); //get the progress of the car on the track
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
        if(trackPieceProgression >= 1 && shift < 2){
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
        }
    }
    public void SetOffset(float offset){
        this.horizontalOffset = offset; //change this to smooth when changed
    }
    public void SetSpeed(int speed){
        this.speed = speed;
    }
    public void SetTrust(CarTrust trust){
        bool isTrusted = trust == CarTrust.Trusted;
        GetComponent<MeshRenderer>().material = isTrusted ? solidMaterial : transparentMaterial;
        carModelManager.ShowTrustedModel(isTrusted);
        if(isTrusted){ //certainly on track
            showOnTrack = true;
        }
        else if(trust == CarTrust.Delocalized){ //lost or delocalised
            showOnTrack = false; 
        }
    }
    public void Delocalise(){
        trackPieceProgression = 0;
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

    float GetProgress(float deltaTime){
        //pre start = 340mm
        //start = 220mm
        //straight = 560mm
        //turn inside = 280mm
        //turn outside = 640mm
        int distanceMM = 560; //default distance for straight track
        if(TrackGenerator.track.GetSegmentType(segmentIdx) == SegmentType.Turn){
            //offset is between -65 and 65, so we can use this to determine the distance
            float offset = horizontalOffset / 65f; //scale offset to -1 to 1
            if(TrackGenerator.track.GetSegmentReversed(segmentIdx)){ offset = -offset;  } //reverse the offset if the segment is reversed
            distanceMM = (int)Mathf.Lerp(280, 640, offset); //scale distance to 280 to 640
        }
        //Debug.Log($"Distance: {distanceMM}mm, Offset: {horizontalOffset}, Segment: {segmentIdx}");
        // } else if(TrackGenerator.track.GetSegmentType(segmentIdx) == SegmentType.PreFinishLine){
        //     distanceMM = 340; //pre start distance
        // } else if(TrackGenerator.track.GetSegmentType(segmentIdx) == SegmentType.FinishLine){
        //     distanceMM = 220; //start distance
        // }
        //speed is in mm/s  
        return ((float)speed / distanceMM) * deltaTime;
    }
}
