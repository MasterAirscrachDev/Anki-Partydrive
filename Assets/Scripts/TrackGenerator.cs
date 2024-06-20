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
        for(int i = 0; i < segments.Length; i++){
            GameObject track = null;
            Quaternion rot = Quaternion.identity;
            rot = Quaternion.LookRotation(forward);
            if(segments[i].type == TrackType.Finish){
                track = Instantiate(trackPrefabs[0], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == TrackType.Straight){
                track = Instantiate(trackPrefabs[1], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == TrackType.Poweup){
                track = Instantiate(trackPrefabs[2], pos, rot, transform);
                pos += forward;
            } if(segments[i].type == TrackType.CurveLeft || segments[i].type == TrackType.CurveRight){
                track = Instantiate(trackPrefabs[3], pos, rot, transform);
                bool right = segments[i].type == TrackType.CurveRight;
                //if curve right set scale to x -1
                track.transform.localScale = new Vector3(right ? -1 : 1, 1, 1);
                //pos += forward;
                //rotate forward vector
                forward = Quaternion.Euler(0, right ? 90 : -90, 0) * forward;

                pos += forward;
            }
            if(track != null){
                trackPieces.Add(track);
            }
            Debug.DrawRay(lastPos + (Vector3.up * 0.1f), forward * 0.5f, Color.blue, 5);
            lastPos = pos;
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
}