using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEntityTracker : MonoBehaviour
{
    [SerializeField] TrackGenerator track;
    [SerializeField] GameObject carPrefab;
    [SerializeField] Dictionary<string, CarEntityPosition> trackers = new Dictionary<string, CarEntityPosition>();

    public void SetPosition(string id, int trackIndex, int speed, float horizontalOffset, bool positionTrusted){
        if(!track.hasTrack){ return; }
        CarEntityPosition entity = trackers.ContainsKey(id) ? trackers[id] : null;
        TrackSpline trackPiece = track.GetTrackPiece(trackIndex);
        bool trueFin = true;
        if(trackPiece == null){ //either we are on a PreStart or an error has occured
            trackIndex++;
            if(track.GetTrackPieceType(trackIndex) == TrackPieceType.FinishLine){
                trackPiece = track.GetTrackPiece(trackIndex); trueFin = false;
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
        entity.SetTrustedPosition(positionTrusted);
        entity.SetTrackSpline(trackPiece, trackIndex);
        entity.SetSpeed(speed);
        entity.SetOffset(horizontalOffset);
        if(trackIndex == 1 && trueFin && positionTrusted){ //if we are on the finish line, we have finished the lap
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
    public string[] GetActiveCars(){
        string[] activeCars = new string[trackers.Count];
        int i = 0;
        foreach(KeyValuePair<string, CarEntityPosition> kvp in trackers){
            activeCars[i] = kvp.Key;
            i++;
        }
        return activeCars;
    }
    public delegate void CarCrossedFinishLine(string id);
    public event CarCrossedFinishLine? OnCarCrossedFinishLine;
}
