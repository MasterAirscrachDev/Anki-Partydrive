using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class CarEntityTracker : MonoBehaviour
{
    [SerializeField] TrackGenerator track;
    [SerializeField] GameObject carPrefab;
    [SerializeField] Dictionary<string, CarEntityPosition> trackers = new Dictionary<string, CarEntityPosition>();

    public void SetPosition(string id, int trackIndex, int speed, float horizontalOffset, int trust){
        if(!track.hasTrack){ return; }
        CarEntityPosition entity = trackers.ContainsKey(id) ? trackers[id] : null;
        TrackSpline trackPiece = track.GetTrackSpline(trackIndex);
        bool trueFin = true;
        if(trackPiece == null){ //either we are on a PreStart or an error has occured
            trackIndex++;
            if(track.GetSegmentType(trackIndex) == SegmentType.FinishLine){
                trackPiece = track.GetTrackSpline(trackIndex); trueFin = false;
            }else{ return; }
        }
        if(entity == null){
            entity = Instantiate(carPrefab, Vector3.zero, Quaternion.identity).GetComponent<CarEntityPosition>();
            entity.gameObject.name = $"{id} True Position";
            entity.transform.GetChild(0).gameObject.name = $"{id} Model";
            entity.carModelManager = entity.transform.GetChild(0).GetComponent<CarModelManager>();
            entity.carModelManager.Setup(); //make this load model and colour later
            trackers.Add(id, entity);
        }
        entity.SetTrust(trust);
        entity.SetTrackSpline(trackPiece, trackIndex);
        entity.SetSpeed(speed);
        entity.SetOffset(horizontalOffset);
        if(trackIndex == 1 && trueFin && trust > 0){ //if we are on the finish line, we have finished the lap
            OnCarCrossedFinishLine?.Invoke(id);
        }
    }
    public void CarDelocalised(string id){
        if(trackers.ContainsKey(id)){
            trackers[id].Delocalise();
        }
    }
    public void SetSpeed(string id, int speed){
        if(trackers.ContainsKey(id)){
            trackers[id].SetSpeed(speed);
        }
    }
    public void SetOffset(string id, float horizontalOffset){
        if(trackers.ContainsKey(id)){
            trackers[id].SetOffset(horizontalOffset);
        }
    }
    public string[] GetActiveCars(string exclude = null){
        string[] activeCars = new string[exclude == null ? trackers.Count : trackers.Count - 1];
        int i = 0;
        foreach(KeyValuePair<string, CarEntityPosition> kvp in trackers){
            if(kvp.Key == exclude){ continue; } //skip the excluded car
            activeCars[i] = kvp.Key;
            i++;
        }
        return activeCars;
    }
    public (uint i, float x, float y) GetCarIXY(string id){
        if(trackers.ContainsKey(id)){
            return trackers[id].GetIXY();
        }
        return (0, 0, 0);
    }
    public delegate void CarCrossedFinishLine(string id);
    public event CarCrossedFinishLine? OnCarCrossedFinishLine;
}
