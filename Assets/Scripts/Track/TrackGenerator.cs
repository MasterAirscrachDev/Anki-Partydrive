using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{
    [SerializeField] TrackCamera trackCamera;
    [SerializeField] TrackPiece[] segments;
    [SerializeField] GameObject[] trackPrefabs, scannningPrefabs;
    [SerializeField] List<GameObject> trackPieces;
    public bool hasTrack = false;

    public TrackSpline GetTrackPiece(int index){
        if(trackPieces[index] == null){ return null; }
        if(index >= trackPieces.Count){ index-= trackPieces.Count; }
        return trackPieces[index].GetComponent<TrackSpline>();
    }
    public TrackPieceType GetTrackPieceType(int index){
        return segments[index].type;
    }
    public TrackPiece[] GetTrackPieces(){
        return segments;
    }
    void GenerateTrackPls(){
        for(int i = 0; i < transform.childCount; i++){
            if(Application.isPlaying){
                Destroy(transform.GetChild(i).gameObject);
            }
            else{
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        trackPieces = new List<GameObject>();
        Vector3 pos = Vector3.zero;
        Vector3 lastPos = Vector3.zero;
        Vector3 forward = Vector3.forward;
        for(int i = 0; i < segments.Length; i++){
            GameObject track = null;
            Quaternion rot = Quaternion.LookRotation(forward);
            //round position to 1 decimal place
            pos = new Vector3(Mathf.Round(pos.x * 10) / 10, Mathf.Round(pos.y * 10) / 10, Mathf.Round(pos.z * 10) / 10);
            if(segments[i].type == TrackPieceType.FinishLine){
                track = Instantiate(segments[i].validated ? trackPrefabs[0] : scannningPrefabs[0], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == TrackPieceType.Straight){
                //if the abs of height diff is 2, use the 4th prefab, if height diff is -1 use the 7th prefab otherwise use the 1st prefab
                int prefIndex = 1;
                int heightDiff = 0;
                //if(segments[i].up == 255 && segments[i].down == 255){ prefIndex = 7; }
                //else if(segments[i].up == 255 || segments[i].down == 255){ prefIndex = 4; heightDiff = segments[i].up == 255 ? 2 : -2;}
                track = Instantiate(segments[i].validated ? trackPrefabs[prefIndex] : scannningPrefabs[1], pos, rot, transform);
                pos += forward;
                // if(heightDiff == 2){
                //     track.transform.Rotate(0, 180, 0);
                //     pos += Vector3.up * 0.2f;
                // }
                // else if(heightDiff == -2){
                //     pos += Vector3.down * 0.2f;
                //     track.transform.Translate(0, -0.2f, 0);
                // }
            } if(segments[i].type == TrackPieceType.FnFSpecial){
                track = Instantiate(segments[i].validated ? trackPrefabs[2] : scannningPrefabs[2], pos, rot, transform);
                if(segments[i].flipped){
                    track.transform.localScale = new Vector3(-1, 1, 1);
                }
                pos += forward;
            } if(segments[i].type == TrackPieceType.Turn){
                //int useIndex = Mathf.Abs(heightDiff) == 0 ? 3 : (Mathf.Abs(heightDiff) == 2 ? 5 : 6);
                int useIndex = 3;
                track = Instantiate(segments[i].validated ? trackPrefabs[useIndex] : scannningPrefabs[3], pos, rot, transform);
                //if curve right set scale to x -1
                track.transform.localScale = new Vector3(segments[i].flipped ? -1 : 1, 1, 1);
                //track.GetComponent<TrackSpline>().flipped = segments[i].flipped;
                //pos += forward;
                //rotate forward vector
                forward = Quaternion.Euler(0, segments[i].flipped ? 90 : -90, 0) * forward;
                pos += forward;
                // if(heightDiff == 2){
                //     pos += Vector3.up * 0.2f;
                // }
                // else if(heightDiff == -2){
                //     pos += Vector3.down * 0.2f;
                //     track.transform.Translate(0, -0.2f, 0);
                //     track.transform.Rotate(0, -90, 0);
                //     track.transform.localScale = new Vector3(segments[i].flipped ? 1 : -1, 1, segments[i].flipped ? 1 : -1);
                // }
                // else if(heightDiff == 1){
                //     pos += Vector3.up * 0.1f;
                // }
                // else if(heightDiff == -1){
                //     pos += Vector3.down * 0.1f;
                //     track.transform.Translate(0, -0.1f, 0);
                //     track.transform.Rotate(0, 90, 0);
                //     track.transform.localScale = new Vector3(-1, 1, segments[i].flipped ? -1 : 1);
                // }
            } if(segments[i].type == TrackPieceType.CrissCross){
                //do we already have a criss cross at this location?
                bool hasCrissCross = false;
                for(int j = 0; j < i; j++){
                    if(segments[j].type == TrackPieceType.CrissCross && segments[j].X == segments[i].X && segments[j].Y == segments[i].Y){
                        hasCrissCross = true;
                        trackPieces[j].name = $"{i} {trackPieces[j].name}";
                        if(segments[j].validated){
                            track = Instantiate(trackPrefabs[10], pos, rot, transform); //invisible criss cross (for splines)
                        }
                        break;
                    }
                }
                if(!hasCrissCross){
                    track = Instantiate(segments[i].validated ? trackPrefabs[8] : scannningPrefabs[4], pos, rot, transform);
                }
                pos += forward;
            } if(segments[i].type == TrackPieceType.JumpRamp){
                track = Instantiate(segments[i].validated ? trackPrefabs[9] : scannningPrefabs[5], pos, rot, transform);
                pos += forward * 2;
            }

            trackPieces.Add(track);
            if(track != null){
                track.name = $"{i} ({segments[i].type})";
                if(segments[i].validated){
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
            }
            Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
            lastPos = pos;
        }
    }
    public void Generate(TrackPiece[] segments, bool validated){
        this.segments = segments;
        try{
            GenerateTrackPls();
            hasTrack = validated;

            if(validated){
                Debug.Log($"Track validated with {segments.Length} segments, {trackPieces.Count} track pieces");
                OnTrackValidated?.Invoke(segments);
            }
            //Debug.Log($"Track generated with {segments.Length} segments, {trackPieces.Count} track pieces, validated: {validated}");
        }
        catch(System.Exception e){
            Debug.LogError(e);
        }
        trackCamera.TrackUpdated();
    }
    public delegate void OnVerifyTrack(TrackPiece[] segments);
    public event OnVerifyTrack? OnTrackValidated;

    public static (bool, TrackPiece?) EvaluateMatch(TrackPiece A, TrackPiece B){
        if(A == null || B == null){ return (false, null); } //if either piece is null, we can't match them
        else if(A.internalID != 0 && B.internalID != 0){ //if both pieces are not fallbacks
            return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
        }else if(A.internalID == 0 && B.internalID == 0){ //if both pieces are fallbacks
            return ((A.type == B.type) && (A.flipped == B.flipped), null); //match if the type and flipped state are the same
        }
        else{ //A or B is a fallback, check if we can match them
            for(int i = 0; i < 2; i++){ 
                TrackPiece C = A; //copy A to C
                TrackPiece D = B; //copy B to D
                if(i == 1){ C = B; D = A; } //swap C and D
                if(C.internalID == 0 && D.internalID != 0){ //if C is a fallback and D is not
                    if(C.type == D.type){ 
                        if(C.validated){ D.validated = true; } //if C is validated, set D to validated
                        return (true, D); 
                    } //if the type is the same, its probably the same piece (return the one with the ID)
                    else{
                        if(C.type == TrackPieceType.Straight && D.type == TrackPieceType.CrissCross) { 
                            if (C.validated){ D.validated = true; } //if C is validated, set D to validated
                            return (true, D);
                        } //crisscross is a straight piece
                        else if(C.type == TrackPieceType.Straight && D.type == TrackPieceType.FnFSpecial) { 
                            if (C.validated){ D.validated = true; } //if C is validated, set D to validated
                            return (true, D); 
                        } //FnF is a straight piece
                    }
                }
            }
        }
        return (false, null); //no match found (should not happen)
    }
}

[System.Serializable]
public class TrackPiece{
    public readonly TrackPieceType type;
    public readonly int internalID;
    public readonly bool flipped;
    public int up, down, elevation, X, Y;
    public TrackPiece(TrackPieceType type, int id, bool flipped){
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
            TrackPiece p = (TrackPiece)obj;
            return (type == p.type) && (flipped == p.flipped);
        }
    }
    public static bool operator ==(TrackPiece? a, TrackPiece? b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a is null || b is null) { return false; }
        return a.Equals(b);
    }
    public static bool operator !=(TrackPiece? a, TrackPiece? b) { return !(a == b); }
    public override string ToString() { return $"({type}|id:{internalID}|flipped:{flipped})"; }
}
[System.Serializable]
public enum TrackPieceType{
    Unknown, Straight, Turn, PreFinishLine, FinishLine, FnFSpecial, CrissCross, JumpRamp, JumpLanding
}