using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{
    [SerializeField] bool test;
    [SerializeField] TrackCamera trackCamera;
    [SerializeField] TrackPiece[] segments;
    [SerializeField] GameObject[] trackPrefabs;
    [SerializeField] GameObject[] scannningPrefabs;
    //
    List<GameObject> trackPieces;
    // Update is called once per frame
    void Update()
    {
        if(test){
            test = false;
            GenerateTrackPls();
        }
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
                if(segments[i].up == 255 && segments[i].down == 255){ prefIndex = 7; }
                else if(segments[i].up == 255 || segments[i].down == 255){ prefIndex = 4;heightDiff = segments[i].up == 255 ? 2 : -2;}
                track = Instantiate(segments[i].validated ? trackPrefabs[prefIndex] : scannningPrefabs[1], pos, rot, transform);
                pos += forward;
                if(heightDiff == 2){
                    track.transform.Rotate(0, 180, 0);
                    pos += Vector3.up * 0.2f;
                }
                else if(heightDiff == -2){
                    pos += Vector3.down * 0.2f;
                    track.transform.Translate(0, -0.2f, 0);
                }
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
            }
            if(track != null){
                trackPieces.Add(track);
                track.name = $"{i} ({segments[i].type})";
            }
            Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
            lastPos = pos;
        }
    }
    public void Generate(TrackPiece[] segments){
        this.segments = segments;
        GenerateTrackPls();
        trackCamera.TrackUpdated();
    }
}
[System.Serializable]
public class TrackPiece{
    public readonly TrackPieceType type;
    public readonly int internalID;
    public readonly bool flipped;
    public readonly int X, Y;
    public int up, down, elevation;
    public bool validated = false;
    public TrackPiece(TrackPieceType type, int id, bool flipped){
        this.type = type;
        this.flipped = flipped;
        internalID = id;
        up = 0; down = 0;
    }
    public void SetUpDown(int up, int down){
        this.up = up; this.down = down;
    }
}
[System.Serializable]
public enum TrackPieceType{
    Unknown, Straight, Turn, PreFinishLine, FinishLine, FnFSpecial, CrissCross, Jump
}