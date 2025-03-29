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
        if(trackPiece == null){ //either we are on a PreStart or an error has occured
            trackIndex++;
            if(track.GetTrackPieceType(trackIndex) == TrackPieceType.FinishLine){
                trackPiece = track.GetTrackPiece(trackIndex);
            }else{ return; }
        }
        if(entity == null){
            CarEntityPosition carModel = Instantiate(carPrefab, Vector3.zero, Quaternion.identity).GetComponent<CarEntityPosition>();
            carModel.gameObject.name = $"{id} True Position";
            carModel.transform.GetChild(0).gameObject.name = $"{id} Model";
            carModel.carModelManager = carModel.transform.GetChild(0).GetComponent<CarModelManager>();
            trackers.Add(id, carModel);
        }
        entity.SetTrustedPosition(positionTrusted);
        entity.SetTrackSpline(trackPiece, trackIndex);
        entity.SetSpeedAndOffset(speed, horizontalOffset);
    }
    public void CarDelocalised(string id){
        if(trackers.ContainsKey(id)){
            trackers[id].Delocalise();
        }
    }
    public void SetSpeedAndLane(string id, int speed, float horizontalOffset){
        if(trackers.ContainsKey(id)){
            trackers[id].SetSpeedAndOffset(speed, horizontalOffset);
        }
    }
}
