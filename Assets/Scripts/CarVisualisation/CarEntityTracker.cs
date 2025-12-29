using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;

//This script manages all cars that exist in the scene, creating and destroying them as needed
public class CarEntityTracker : MonoBehaviour
{
    [SerializeField] TrackGenerator track;
    [SerializeField] GameObject carPrefab;
    [SerializeField] Dictionary<string, CarEntityPosition> trackers = new Dictionary<string, CarEntityPosition>();

    public void SetPosition(string id, int trackIndex, int speed, float horizontalOffset, CarTrust trust){
        if(!track.hasTrack){ return; } //if no track or we are on the finish line, do nothing
        CarEntityPosition entity = trackers.ContainsKey(id) ? trackers[id] : null;

        //special cases for starting lines (prefinishline doesnt have a spline, its part of finishline)
        //jumpLanding doesn't have a spline either, stay on jump ramp
        SegmentType segmentType = track.GetSegmentType(trackIndex);
        TrackSpline trackPiece;
        if(segmentType == SegmentType.PreFinishLine){ //if we are on the prefinish line, we need to get the next piece of track
            trackPiece = track.GetTrackSpline(trackIndex + 1); //get the next piece of track
        } else if(segmentType == SegmentType.JumpLanding){ //if we are on the jump landing, we need to stay on the jump ramp
            trackPiece = track.GetTrackSpline(trackIndex - 1); //get the previous piece of track
        }else{
            trackPiece = track.GetTrackSpline(trackIndex); //get the current piece of track
        }
        if(entity == null){ entity = AddTracker(id); } //create a new tracker if it doesn't exist
        if(trackPiece != null && segmentType != SegmentType.FinishLine){ entity.SetTrackSpline(trackPiece, trackIndex); } //should always be true, but just in case
        entity.SetTrust(trust);
        entity.SetSpeed(speed);
        entity.SetOffset(horizontalOffset);
        if(trackIndex == 1 && trust == CarTrust.Trusted){ //if we are on the finish line, we have finished the lap
            OnCarCrossedFinishLine?.Invoke(id, entity.wasDelocalisedThisLap);
            entity.wasDelocalisedThisLap = false; //reset the delocalised flag for the next lap
        }
    }
    CarEntityPosition AddTracker(string id){
        if(trackers.ContainsKey(id)){ return trackers[id]; } //return the existing tracker (should never happen)
        CarEntityPosition entity = Instantiate(carPrefab, Vector3.zero, Quaternion.identity).GetComponent<CarEntityPosition>();
        int model = (int)CarInteraface.io.GetCarFromID(id).modelName;
        entity.Setup(id, model); //setup the car entity
        entity.gameObject.name = $"{id} True Position";
        entity.transform.GetChild(0).gameObject.name = $"{id} Model";
        entity.carModelManager = entity.transform.GetChild(0).GetComponent<CarModelManager>();

        //check if there is a CarController with this ID to get the colour from
        CarController cc = CMS.cms.GetController(id);
        Color color = cc != null ? cc.GetPlayerColor() : Color.clear;

        entity.carModelManager.Setup(model, color); //make this load colour later
        trackers.Add(id, entity);
        UpdateAIOpponentLocations(); //update the AI opponent locations
        return entity;
    }
    public void UpdateAIOpponentLocations(){
        //for every AI controller, call SetOpponentLocations with all TrackCoordinates except its own
        AIController[] aiControllers = FindObjectsOfType<AIController>();
        foreach(AIController ai in aiControllers){
            ai.SetOpponentLocations(trackers.Where(x => x.Key != ai.GetID()).Select(x => x.Value.GetTrackCoordinate()).ToArray());
            if(trackers.ContainsKey(ai.GetID())){ //AIs can be added before they are tracked
                ai.SetOurLocation(trackers[ai.GetID()].GetTrackCoordinate()); //set our location to the AI controller
            }
        }
    }
    
    public void RemoveTracker(string id){
        if(trackers.ContainsKey(id)){
            Destroy(trackers[id].gameObject);
            trackers.Remove(id);
            UpdateAIOpponentLocations(); //update the AI opponent locations
        }
    }
    
    public void ClearAllCars(){
        Debug.Log("Clearing all car entities for track scan");
        foreach(var tracker in trackers.Values){
            if(tracker != null && tracker.gameObject != null){
                Destroy(tracker.gameObject);
            }
        }
        trackers.Clear();
        UpdateAIOpponentLocations(); //update the AI opponent locations
    }
    public void CarDelocalised(string id){ if(trackers.ContainsKey(id)){ trackers[id].Delocalise(); } }
    public void SetSpeed(string id, int speed){ if(trackers.ContainsKey(id)){ trackers[id].SetSpeed(speed); } }
    public void SetOffset(string id, float horizontalOffset){ if(trackers.ContainsKey(id)){ trackers[id].SetOffset(horizontalOffset); } }
    public string[] GetActiveCars(string exclude = null){
        if(trackers.Count == 0){ return new string[0]; } //no cars to show
        string[] activeCars = new string[exclude == null ? trackers.Count : trackers.Count - 1];
        int i = 0;
        foreach(KeyValuePair<string, CarEntityPosition> kvp in trackers){
            if(kvp.Key == exclude){ continue; } //skip the excluded car
            activeCars[i] = kvp.Key;
            i++;
        }
        return activeCars;
    }
    public TrackCoordinate GetCarTrackCoordinate(string id){
        if(trackers.ContainsKey(id)){
            return trackers[id].GetTrackCoordinate();
        }
        return null; //car not found
    }
    public void SetCarColorByID(string id, Color color){
        if(trackers.ContainsKey(id)){
            trackers[id].carModelManager.SetColour(color);
        }
    }
    public Vector3 GetCarVisualPosition(string id){
        if(trackers.ContainsKey(id)){
            return trackers[id].GetVisualPosition();
        }
        return Vector3.zero; //car not found
    }
    public delegate void CarCrossedFinishLine(string id, bool trusted);
    public event CarCrossedFinishLine? OnCarCrossedFinishLine;
}
