using UnityEngine;
using static OverdriveServer.NetStructures;

public class CarEntityPosition : MonoBehaviour
{
    Material ourMaterial;
    public CarModelManager carModelManager;
    TrackSpline trackSpline;
    SegmentType currentSegmentType = SegmentType.Straight;
    bool isSegmentReversed = false;
    int speed = 0, shift = 0;
    TrackCoordinate trackpos = new TrackCoordinate(0, 0, 0);
    Vector3 lastPosition;
    TrackGenerator track;
    bool showOnTrack = true; 
    public bool wasDelocalisedThisLap = false;
    readonly bool SHOW_ANYWAY = false; //Debugging to show hitbox in editor
    void Start()
    {
        track = TrackGenerator.track;
        //if not the editor, destroy the mesh renderer
        if(!Application.isEditor || !SHOW_ANYWAY){
            GetComponent<MeshRenderer>().enabled = false;
            if(!Application.isEditor){ Destroy(GetComponent<MeshFilter>()); }
        }else{
            ourMaterial = GetComponent<MeshRenderer>().material; //get the material of the car
            if(ourMaterial == null){ Debug.LogError("No material found on car entity"); }
        }
    }

    // Update is called once per frame
    void Update()
    {
        trackpos.Progress(GetProgress(Time.deltaTime)); //get the progress of the car on the track
        if(trackSpline != null){
            Vector3 targetPos = trackSpline.GetPoint(trackpos);
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
        if(trackpos.progression >= 1 && shift < 2){
            trackpos.progression = 0; //reset the progression
            shift++;
            trackSpline = track.GetTrackSpline(trackpos.idx + shift);
            if(trackSpline == null){trackSpline = track.GetTrackSpline(trackpos.idx + ++shift); }
            UpdateTrackSpline(trackSpline, trackpos.idx + shift); //updates cached values for movement prediction
            SetMat(false);
        }
    }
    /// <summary>
    /// Set the track spline for the car entity. idx must be trusted by server
    /// </summary>
    public void SetTrackSpline(TrackSpline trackSpline, int idx){
        if(idx != trackpos.idx){ //only update if the track spline is different
            UpdateTrackSpline(trackSpline, idx);
            trackpos.SetIdx(idx);
            shift = 0;
        }
    }
    /// <summary>
    /// Update the track spline for the car entity. idx may not be trusted
    /// </summary>
    void UpdateTrackSpline(TrackSpline trackSpline, int idx){
        this.trackSpline = trackSpline;
        currentSegmentType = track.GetSegmentType(idx);
        isSegmentReversed = track.GetSegmentReversed(idx);
    }
    public void SetOffset(float offset){
        trackpos.offset = offset; //change this to smooth when changed
    }
    public void SetSpeed(int speed){
        this.speed = speed;
    }
    public void SetTrust(CarTrust trust){
        bool isTrusted = trust == CarTrust.Trusted;
        SetMat(isTrusted);
        carModelManager.ShowTrustedModel(isTrusted);
        if(isTrusted){ //certainly on track
            showOnTrack = true;
        }
        else if(trust == CarTrust.Delocalized){ //lost or delocalised
            showOnTrack = false; 
        }
    }
    void SetMat(bool trusted){
        if(ourMaterial != null){
            //if trusted and shift == 0 set colour to solid blue
            Color c = new Color(0, 0.3f, 1, 0.6f); //blue
            if(!trusted){
                c = shift == 0 ? new Color(1, 0.5f, 0, 0.4f) : new Color(0.4f, 0.6f, 0, 0.4f); //red or orange
            }
            ourMaterial.color = c;
        }
    }
    public void Delocalise(){ //start a timer to despawn the car model
        trackSpline = null;
        speed = 0;
        shift = 0;
        showOnTrack = false;
        wasDelocalisedThisLap = true;
        transform.position = transform.position + new Vector3(0, -10, 0); //reset position to the last known position
    }
    public bool IsDelocalised(){
        return trackSpline == null;
    }
    public TrackCoordinate GetTrackCoordinate(){
        return trackpos;
    }

    float GetProgress(float deltaTime){
        //pre start = 340mm
        //start = 220mm
        //straight = 560mm
        //turn inside = 280mm
        //turn outside = 640mm
        int distanceMM = 560; //default distance for straight track
        if(currentSegmentType == SegmentType.Turn){
            //offset is between -72.25 and 72.25, so we can use this to determine the distance
            float offset = trackpos.offset / 72.25f; //scale offset to -1 to 1
            if(isSegmentReversed){ offset = -offset;  } //reverse the offset if the segment is reversed
            distanceMM = (int)Mathf.Lerp(280, 640, offset); //scale distance to 280 to 640
        }
        distanceMM = Mathf.RoundToInt(distanceMM * 1.1f); //tolerance
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
public class TrackCoordinate{
    public int idx;
    public float offset, progression;
    public TrackCoordinate(int segmentIdx, float offset, float progression){
        this.idx = segmentIdx;
        this.offset = offset;
        this.progression = progression;
    }
    public void Progress(float scaledDistance){
        progression += scaledDistance;
        if(progression > 1){ progression = 1; }
    }
    public void SetIdx(int idx){
        this.idx = idx;
        progression = 0;
    }
}
