using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using static OverdriveServer.NetStructures;
public class TrackGenerator : MonoBehaviour
{
    [SerializeField] TrackCamera trackCamera;
    [SerializeField] Material validConfirmedMat;
    [SerializeField] PostProcessProfile post;
    [SerializeField] Segment[] segments;
    [SerializeField] GameObject[] trackPrefabs, scannningPrefabs;
    [SerializeField] List<GameObject> trackPieces;
    public bool hasTrack = false;
    int lastSegmentCount = 0;
    public static TrackGenerator track;

    [ContextMenu("Generate Track From Segments")]
    public void TEST_GenerateTrackFromSegments(){
        if(segments == null || segments.Length == 0){ return; } //if there are no segments, do nothing  
        //set all segments to validated
        for(int i = 0; i < segments.Length; i++){
            segments[i].validated = true;
        }
        //generate the track
        Generate(segments, true);
        Debug.Log($"Track validated with {segments.Length} segments, {trackPieces.Count} track pieces");
    }

    void Awake(){
        if(track == null){ track = this; }else{ DestroyImmediate(this); }
    }
    int LoopIndex(int index){
        if(index < 0){ return segments.Length + index; }
        if(index >= segments.Length){ return index - segments.Length; }
        return index;
    }

    public TrackSpline GetTrackSpline(int index){
        if(index >= trackPieces.Count){ index-= trackPieces.Count; }
        if(trackPieces[index] == null){ return null; }
        return trackPieces[index].GetComponent<TrackSpline>();
    }
    public SegmentType GetSegmentType(int index){ index = LoopIndex(index); return segments[index].type; }
    public bool GetSegmentReversed(int index){ index = LoopIndex(index);  return segments[index].flipped; }
    public Segment[] GetTrackPieces(){
        return segments;
    }
    public int GetTrackLength(){
        return segments.Length;
    }

    public void Generate(Segment[] segments, bool validated){
        hasTrack = false;
        this.segments = segments;
        try{
            if(validated){
                StartCoroutine(OnFinalGenerate());
                //Debug.Log("Track validated, generating final track...");
                return;
            }
            if(segments == null || segments.Length == 0){ return; } //if there are no segments, do nothing  
            GenerateTrackObjects(lastSegmentCount != segments.Length, false);
            lastSegmentCount = segments.Length;
            hasTrack = validated;
            if(validated){
                Debug.Log($"Track validated with {segments.Length} segments, {trackPieces.Count} track pieces");
                OnTrackValidated?.Invoke(segments);
            }
        }
        catch(System.Exception e){
            Debug.LogError($"Error generating track: " + e);
            return;
        }
        //calculate the center and size of the track
        Vector3 center = Vector3.zero;
        for(int i = 0; i < trackPieces.Count; i++){
            if(trackPieces[i] == null){ continue; }
            center += trackPieces[i].transform.position;
        }
        center /= trackPieces.Count;
        Vector2 size = new Vector2(0, 0);
        for(int i = 0; i < trackPieces.Count; i++){
            if(trackPieces[i] == null){ continue; }
            Vector2 pos = new Vector2(trackPieces[i].transform.position.x, trackPieces[i].transform.position.z);
            if(pos.x > size.x){ size.x = pos.x; }
            if(pos.y > size.y){ size.y = pos.y; }
        }
        size.x -= center.x; size.y -= center.y;
        size.x *= 2; size.y *= 2;
        trackCamera.TrackUpdated(center, size);
    }
    IEnumerator OnFinalGenerate(){
        Bloom b = post.GetSetting<Bloom>();
        //over 0.5s spike bloom intensity to 300
        float time = 0;
        float duration = 0.5f;
        float start = b.intensity.value;
        while(time < duration){
            time += Time.deltaTime;
            b.intensity.value = Mathf.Lerp(start, 200, time / duration);
            yield return new WaitForEndOfFrame();
        }
        GenerateTrackObjects(false, true);
        time = 0;
        //over 0.5s reduce bloom intensity to 1
        start = b.intensity.value;
        while(time < duration){
            time += Time.deltaTime;
            b.intensity.value = Mathf.Lerp(start, 1, time / duration);
            yield return new WaitForEndOfFrame();
        }
        OnTrackValidated?.Invoke(segments);
        hasTrack = true;
        lastSegmentCount = segments.Length;
    }

    void GenerateTrackObjects(bool animateLastSegment, bool fullyValidated){
        for(int i = 0; i < transform.childCount; i++){
            if(Application.isPlaying){ Destroy(transform.GetChild(i).gameObject); }
            else{ DestroyImmediate(transform.GetChild(i).gameObject); }
        }
        trackPieces = new List<GameObject>();
        Vector3 pos = Vector3.zero;
        Vector3 lastPos = Vector3.zero;
        Vector3 forward = Vector3.forward;
        for(int i = 0; i < segments.Length; i++){
            GameObject track = null;
            Quaternion rot = Quaternion.LookRotation(forward);
            bool useFullTrack = segments[i].validated && fullyValidated;

            //round position to 1 decimal place
            pos = new Vector3(Mathf.Round(pos.x * 10) / 10, Mathf.Round(pos.y * 10) / 10, Mathf.Round(pos.z * 10) / 10);
            if(segments[i].type == SegmentType.FinishLine){
                track = Instantiate(useFullTrack ? trackPrefabs[0] : scannningPrefabs[0], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == SegmentType.Straight){
                track = Instantiate(useFullTrack ? trackPrefabs[1] : scannningPrefabs[1], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == SegmentType.FnFSpecial){
                track = Instantiate(useFullTrack ? trackPrefabs[2] : scannningPrefabs[2], pos, rot, transform);
                if(segments[i].flipped){ track.transform.localScale = new Vector3(-1, 1, 1); }
                pos += forward;
            } if(segments[i].type == SegmentType.Turn){
                track = Instantiate(useFullTrack ? trackPrefabs[3] : scannningPrefabs[3], pos, rot, transform);
                track.transform.localScale = new Vector3(segments[i].flipped ? 1 : -1, 1, 1);
                forward = Quaternion.Euler(0, segments[i].flipped ? 90 : -90, 0) * forward;
                pos += forward;
            } if(segments[i].type == SegmentType.CrissCross){
                bool hasCrissCross = false;
                for(int j = 0; j < i; j++){
                    if(segments[j].type == SegmentType.CrissCross && segments[j].X == segments[i].X && segments[j].Y == segments[i].Y){
                        hasCrissCross = true;
                        trackPieces[j].name = $"{i} {trackPieces[j].name}";
                        if(segments[j].validated){
                            track = Instantiate(trackPrefabs[10], pos, rot, transform); //invisible criss cross (for splines)
                        }
                        break;
                    }
                }
                if(!hasCrissCross){ track = Instantiate(useFullTrack ? trackPrefabs[8] : scannningPrefabs[4], pos, rot, transform); }
                pos += forward;
            } if(segments[i].type == SegmentType.JumpRamp){
                track = Instantiate(useFullTrack ? trackPrefabs[9] : scannningPrefabs[5], pos, rot, transform);
                pos += forward * 2;
            }

            trackPieces.Add(track);
            if(track != null){
                track.name = $"{i} ({segments[i].type})";
                if(segments[i].validated && fullyValidated){
                    if(i == 1){
                        track.GetComponent<TrackSpline>().flipped = segments[i].flipped;
                    }else if (i > 1){
                        if(trackPieces[i] == null){ continue; }
                        try{
                            int offset = 1;
                            if(trackPieces[i - 1] == null){ offset = 2; }
                            if(trackPieces[i - offset] == null){ Debug.Log($"Track {i} was null"); continue; }
                            Vector3 lastTrackEndLink = trackPieces[i - offset].GetComponent<TrackSpline>().GetEndLinkPoint();
                            Vector3 lastTrackStartLink = trackPieces[i].GetComponent<TrackSpline>().GetStartLinkPoint();
                            Debug.DrawLine(lastTrackEndLink, lastTrackStartLink, Color.red, 400);
                            float linkDist = Vector3.Distance(lastTrackEndLink, lastTrackStartLink);
                            if(linkDist > 0.01f){
                                track.GetComponent<TrackSpline>().flipped = true;
                            }
                        }
                        catch(System.Exception e){
                            Debug.LogError(e);
                        }
                        
                    }
                }
                if(!fullyValidated && track != null && segments[i].validated){
                    track.GetComponent<MeshRenderer>().material = validConfirmedMat;
                }
            }
            Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
            lastPos = pos;
        }
        if(animateLastSegment && trackPieces.Count > 0 ){
            if(trackPieces[trackPieces.Count - 1] != null){ 
                trackPieces[trackPieces.Count - 1].AddComponent<SegmentSpawnAnimator>();
            }
        }
    }
    
    public delegate void OnVerifyTrack(Segment[] segments);
    public event OnVerifyTrack? OnTrackValidated;

    public static (bool, Segment?) EvaluateMatch(Segment A, Segment B){
        if(A == null || B == null){ return (false, null); } //if either piece is null, we can't match them
        else if(A.internalID != 0 && B.internalID != 0){ //if both pieces are not fallbacks
            return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
        }else if(A.internalID == 0 && B.internalID == 0){ //if both pieces are fallbacks
            return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
        }
        else{ //A or B is a fallback, check if we can match them
            for(int i = 0; i < 2; i++){ 
                Segment C = A; //copy A to C
                Segment D = B; //copy B to D
                if(i == 1){ C = B; D = A; } //swap C and D
                if(C.internalID == 0 && D.internalID != 0){ //if C is a fallback and D is not
                    if(C.type == D.type){ 
                        if(C.validated){ D.validated = true; } //if C is validated, set D to validated
                        return (true, D); 
                    } //if the type is the same, its probably the same piece (return the one with the ID)
                    else{
                        if(C.type == SegmentType.Straight && D.type == SegmentType.CrissCross) { 
                            if (C.validated){ D.validated = true; } //if C is validated, set D to validated
                            return (true, D);
                        } //crisscross is a straight piece
                        else if(C.type == SegmentType.Straight && D.type == SegmentType.FnFSpecial) { 
                            if (C.validated){ D.validated = true; } //if C is validated, set D to validated
                            return (true, D); 
                        } //FnF is a straight piece
                    }
                }
            }
        }
        return (false, null); //no match found (should not happen)
    }

    public TrackCoordinate WorldspaceToTrackCoordinate(Vector3 worldPos){
        //check if the position is within 0.5m of any track piece
        float closestDist = Mathf.Infinity;
        TrackSpline closestSpline = null;
        int bestIndex = 0;
        for(int i = 0; i < trackPieces.Count; i++){
            if(trackPieces[i] == null){ continue; }
            float dist = Vector3.Distance(worldPos, trackPieces[i].transform.position);
            if(dist < 0.51f && dist < closestDist){
                closestDist = dist;
                closestSpline = trackPieces[i].GetComponent<TrackSpline>();
                bestIndex = i;
            }
        }
        if(closestSpline != null){
            TrackCoordinate c = closestSpline.GetTrackCoordinate(worldPos);
            c.idx = bestIndex; //set the index of the track piece
            Vector3 pointOnTrack = closestSpline.GetPoint(c.progression, c.offset);

            Debug.DrawLine(worldPos, pointOnTrack, Color.red, 5); //draw a line from the world position to the track position

            return c;//get the track coordinate from the world position
        }
        return null;
    }
}
[System.Serializable]
public class Segment{
    public SegmentType type;
    public int internalID;
    public readonly bool flipped;
    public int up, down, elevation, X, Y;
    public Segment(SegmentType type, int id, bool flipped){
        this.type = type;
        this.flipped = flipped;
        internalID = id;
        up = 0; down = 0;
    }
    public bool validated = false;
    public void SetUpDown(int up, int down){ this.up = up; this.down = down; }
    public override bool Equals(object? obj) { // Check for null and compare run-time types.
        if (obj == null || !GetType().Equals(obj.GetType())) { return false; }
        else {
            Segment p = (Segment)obj;
            return (type == p.type) && (flipped == p.flipped);
        }
    }
    public static bool operator ==(Segment? a, Segment? b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a is null || b is null) { return false; }
        return a.Equals(b);
    }
    public static bool operator !=(Segment? a, Segment? b) { return !(a == b); }
    public override string ToString() { return $"({type}|id:{internalID}|flipped:{flipped})"; }
}