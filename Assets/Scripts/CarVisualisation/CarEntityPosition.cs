using System.Collections;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class CarEntityPosition : MonoBehaviour
{
    Material ourMaterial;
    public CarModelManager carModelManager;
    string id = "";
    TrackSpline trackSpline;
    SegmentType currentSegmentType = SegmentType.Straight;
    bool isSegmentReversed = false;
    [SerializeField] int shift = 0;
    int despawnTimer = 50;
    [SerializeField] TrackCoordinate trackpos;
    Vector3 lastPosition;
    TrackGenerator track;
    bool showOnTrack = true, despawnCancelled = false, despawnTimerRunning = false;
    public bool wasDelocalisedThisLap = true; //true until set to false
    readonly bool SHOW_ANYWAY = false; //Debugging to show hitbox in editor
    int carModel = 0; // Store car model for truck length calculations
    public void Setup(string id, int model) {
        this.id = id; //set the id of the car entity
        track = TrackGenerator.track;
        carModel = model;
        float backDistance = 0.2f; // Default for normal cars
        // Check if this is a supertruck (models 15, 16, 17 = Freewheel, x52, x52Ice)
        if(carModel == 15 || carModel == 16 || carModel == 17) { backDistance *= 2.5f; } // 2.5x length for trucks
        trackpos = new TrackCoordinate(0, 0, 0, 22, backDistance);
        
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
    void Update() {
        trackpos.Progress(TrackPathSolver.GetProgress(currentSegmentType,isSegmentReversed, trackpos, Time.deltaTime)); //get the progress of the car on the track
        if(trackSpline != null){
            // Calculate predictive position - show cars further ahead based on speed
            TrackCoordinate displayPos = trackpos.Clone();
            Vector3 targetPos;
            // Add 50% of a segment progression scaled by car speed (higher speed = more prediction)
            if(trackpos.speed > 0)
            {
                float speedFactor = Mathf.Clamp01(trackpos.speed / 500.0f); // Normalize speed (500mm/s as reference)
                float predictiveProgress = 0.5f * speedFactor; // 50% segment scaled by speed
                displayPos += predictiveProgress;
                targetPos = trackSpline.GetPoint(displayPos);
            }
            else
            {
                // No speed, use exact position
                targetPos = trackSpline.GetPoint(trackpos);
            }
            if(!showOnTrack){
                targetPos.y -= 20; //hide the car
            }
            transform.position = targetPos;
            
            if(Vector3.Distance(transform.position, lastPosition) > 0.01f){ //should fix weirdness when stopping
                transform.LookAt(lastPosition);
                lastPosition = transform.position;
                transform.Rotate(0, 180, 0);
            }
        }else if(!showOnTrack){ //if we are not on the track, hide the car
            transform.position = new Vector3(0, -10, 0); //hide the car
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
        }
        shift = 0;
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
        StartCoroutine(SetSpeedDelayed(speed)); //set the speed after a delay (account for ble latency + sim lag)
    }
    IEnumerator SetSpeedDelayed(int speed){
        yield return new WaitForSeconds(0.1f); //wait for 0.1 seconds
        trackpos.speed = speed; //set the speed in the track coordinate
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
        trackpos.speed = 0;
        shift = 0;
        showOnTrack = false;
        wasDelocalisedThisLap = true;

        despawnCancelled = false; //reset the despawn cancelled flag
        despawnTimer = 50; //reset the despawn timer
        StartCoroutine(TimeoutCar()); //start the despawn timer
    }
    IEnumerator TimeoutCar(){
        if(despawnTimerRunning){ yield break; } //if the despawn timer is already running, stop it
        despawnTimerRunning = true; //set the despawn timer running flag
        while(despawnTimer > 0){
            yield return new WaitForSeconds(0.1f);
            despawnTimer--;
            if(despawnCancelled){  break; } //if the despawn is cancelled, stop the timer
        }
        if(despawnTimer <= 0 && !despawnCancelled){ FindObjectOfType<CarEntityTracker>().RemoveTracker(id); } //destroy the car entity
        despawnTimerRunning = false; //reset the despawn timer running flag
    }
    public bool IsDelocalised(){
        return trackSpline == null;
    }
    public TrackCoordinate GetTrackCoordinate(){
        return trackpos;
    }
}
[System.Serializable]
public class TrackCoordinate
{
    public readonly int SIDE_DISTANCE = 22; //Half the width of this object (22mm is half the width of the car)
    public readonly float BACK_DISTANCE = 0.15f; //Distance that we need to start avoidance from the back of the car (20% of a segment) (will need to be adjusted for supertrucks)
    public int idx, speed;
    public float offset, progression;
    public TrackCoordinate(int segmentIdx, float offset, float progression, int sideDistance = 22, float backDistance = 0.2f)
    {
        this.idx = segmentIdx;
        this.offset = offset;
        this.progression = progression;
        this.SIDE_DISTANCE = sideDistance;
        this.BACK_DISTANCE = backDistance;
    }
    public void Progress(float scaledDistance)
    {
        progression += scaledDistance;
        if (progression > 1) { progression = 1; }
    }
    public void SetIdx(int idx)
    {
        this.idx = idx;
        progression = 0;
    }
    public float DistanceX(TrackCoordinate other)
    {
        return Mathf.Abs(offset - other.offset);
    }
    public float DistanceY(TrackCoordinate other)
    {
        float ourDist = idx + progression;
        float otherDist = other.idx + other.progression;

        return Mathf.Abs(ourDist - otherDist);
    }
    public bool IsAhead(TrackCoordinate other)
    {
        //check if we are ahead of the other car
        if (idx == other.idx)
        {
            return progression > other.progression;
        }
        int trackLength = TrackGenerator.track.GetTrackLength();
        //check if we are ahead of the other car (in the future 50% of the track)
        int distanceAB = (other.idx - idx + trackLength) % trackLength;
        int distanceBA = (idx - other.idx + trackLength) % trackLength;

        return distanceAB < distanceBA;
    }
    public bool IntersectsOffset(TrackCoordinate other)
    {
        int XSpacingSum = SIDE_DISTANCE + other.SIDE_DISTANCE; //sum of the side distances
        //check if we are within the side distance of the other car
        return DistanceX(other) < XSpacingSum; //check if we are within the side distance of the other car
    }
    //for shorthand incrementing progression
    public static TrackCoordinate operator +(TrackCoordinate a, float b)
    {
        a.progression += b;
        if (a.progression > 1)
        {
            a.idx++;
            a.progression -= 1;
        }
        return a;
    }

    public void DebugRender(TrackSpline spline, Color color, float duration = 0.1f)
    {
        Vector3 frontLeft = spline.GetPoint(progression, offset - SIDE_DISTANCE);
        Vector3 frontRight = spline.GetPoint(progression, offset + SIDE_DISTANCE);
        Vector3 backLeft = spline.GetPoint(progression - BACK_DISTANCE, offset - SIDE_DISTANCE);
        Vector3 backRight = spline.GetPoint(progression - BACK_DISTANCE, offset + SIDE_DISTANCE);

        Debug.DrawLine(frontLeft, frontRight, color, duration);
        Debug.DrawLine(backLeft, backRight, color, duration);
        Debug.DrawLine(frontLeft, backLeft, color, duration);
        Debug.DrawLine(frontRight, backRight, color, duration);
        Debug.DrawLine(frontRight, frontLeft + Vector3.up * 0.05f, color, duration);
        Debug.DrawLine(backLeft, backRight + Vector3.up * 0.05f, color, duration);
        Debug.DrawLine(backRight, frontRight + Vector3.up * 0.05f, color, duration);
        Debug.DrawLine(frontLeft, backLeft + Vector3.up * 0.05f, color, duration);

        Debug.DrawLine(frontLeft + Vector3.up * 0.05f, frontRight + Vector3.up * 0.05f, color, duration);
        Debug.DrawLine(backLeft + Vector3.up * 0.05f, backRight + Vector3.up * 0.05f, color, duration);
        Debug.DrawLine(frontLeft + Vector3.up * 0.05f, backLeft + Vector3.up * 0.05f, color, duration);
        Debug.DrawLine(frontRight + Vector3.up * 0.05f, backRight + Vector3.up * 0.05f, color, duration);
    }
    public TrackCoordinate Clone()
    {
        return new TrackCoordinate(idx, offset, progression, SIDE_DISTANCE, BACK_DISTANCE);
    }
}
