using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{
    [SerializeField] bool test;
    [SerializeField] TrackCamera trackCamera;
    [SerializeField] Segment[] segments;
    [SerializeField] GameObject[] trackPrefabs;
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
        int oldHeight = 0, heightDiff;
        for(int i = 0; i < segments.Length; i++){
            GameObject track = null;
            Quaternion rot = Quaternion.LookRotation(forward);
            heightDiff = segments[i].elevation - oldHeight;
            if(segments[i].type == TrackType.Finish){
                track = Instantiate(trackPrefabs[0], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == TrackType.Straight){
                int useIndex = Mathf.Abs(heightDiff) == 2 ? 4 : 1;
                track = Instantiate(trackPrefabs[useIndex], pos, rot, transform);
                pos += forward;
                if(heightDiff == 2){
                    track.transform.Rotate(0, 180, 0);
                    pos += Vector3.up * 0.2f;
                }
                else if(heightDiff == -2){
                    pos += Vector3.down * 0.2f;
                    track.transform.Translate(0, -0.2f, 0);
                }
            } if(segments[i].type == TrackType.Poweup){
                track = Instantiate(trackPrefabs[2], pos, rot, transform);
                if(segments[i].flipped){
                    track.transform.localScale = new Vector3(-1, 1, 1);
                }
                pos += forward;
            } if(segments[i].type == TrackType.CurveLeft || segments[i].type == TrackType.CurveRight){
                int useIndex = Mathf.Abs(heightDiff) == 0 ? 3 : (Mathf.Abs(heightDiff) == 2 ? 5 : 6);
                track = Instantiate(trackPrefabs[useIndex], pos, rot, transform);
                bool right = segments[i].type == TrackType.CurveRight;
                //if curve right set scale to x -1
                track.transform.localScale = new Vector3(right ? -1 : 1, 1, 1);
                //pos += forward;
                //rotate forward vector
                forward = Quaternion.Euler(0, right ? 90 : -90, 0) * forward;
                pos += forward;
                if(heightDiff == 2){
                    pos += Vector3.up * 0.2f;
                }
                else if(heightDiff == -2){
                    pos += Vector3.down * 0.2f;
                    track.transform.Translate(0, -0.2f, 0);
                    track.transform.Rotate(0, -90, 0);
                    track.transform.localScale = new Vector3(right ? -1 : 1, 1, right ? 1 : -1);
                }
                else if(heightDiff == 1){
                    pos += Vector3.up * 0.1f;
                }
                else if(heightDiff == -1){
                    pos += Vector3.down * 0.1f;
                    track.transform.Translate(0, -0.1f, 0);
                    track.transform.Rotate(0, 90, 0);
                    track.transform.localScale = new Vector3(-1, 1, right ? -1 : 1);
                }
            }
            if(track != null){
                trackPieces.Add(track);
                track.name = $"{i} ({segments[i].type})";
            }
            Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
            lastPos = pos;
            oldHeight = segments[i].elevation;
        }
    }
    public void Generate(Segment[] segments){
        this.segments = segments;
        GenerateTrackPls();
        trackCamera.TrackUpdated();
    }
}
[System.Serializable]
public enum TrackType
{
    Straight,
    CurveLeft,
    CurveRight,
    Jump,
    CrissCross,
    Finish,
    Poweup,
    Unknown
}
[System.Serializable]
public class Segment
{
    public TrackType type;
    public int elevation;
    public bool addPowerups, flipped;
    public Segment(TrackType type, int elevation, bool addPowerups, bool flipped){
        this.type = type;
        this.elevation = elevation;
        this.addPowerups = addPowerups;
        this.flipped = flipped;
    }
}