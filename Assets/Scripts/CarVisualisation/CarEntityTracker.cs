using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;

//This script manages all cars that exist in the scene, creating and destroying them as needed
public class CarEntityTracker : MonoBehaviour
{
    [SerializeField] GameObject carPrefab;
    [SerializeField] Dictionary<string, CarEntityPosition> trackers = new Dictionary<string, CarEntityPosition>();
    
    // Overtake tracking with debouncing
    [Header("Overtake Tracking")]
    [SerializeField] float overtakeDebounceTime = 0.5f; // Time in seconds before confirming an overtake
    
    // Tracks the last confirmed position for each car (1 = first place, 2 = second, etc.)
    private Dictionary<string, int> confirmedPositions = new Dictionary<string, int>();
    // Tracks pending position changes: key is carID, value is (newPosition, timeWhenPositionChanged)
    private Dictionary<string, (int newPosition, float changeTime)> pendingPositionChanges = new Dictionary<string, (int, float)>();
    // Tracks the current unconfirmed positions for comparison
    private Dictionary<string, int> currentPositions = new Dictionary<string, int>();

    public void SetPosition(string id, int trackIndex, int speed, float horizontalOffset, CarTrust trust){
        if(!SR.track.hasTrack){ return; } //if no track or we are on the finish line, do nothing
        CarEntityPosition entity = trackers.ContainsKey(id) ? trackers[id] : null;
        //special cases for starting lines (prefinishline doesnt have a spline, its part of finishline)
        //jumpLanding doesn't have a spline either, stay on jump ramp
        SegmentType segmentType = SR.track.GetSegmentType(trackIndex);
        TrackSpline trackPiece;
        if(segmentType == SegmentType.PreFinishLine){ //if we are on the prefinish line, we need to get the next piece of track
            trackPiece = SR.track.GetTrackSpline(trackIndex + 1); //get the next piece of track
        } else if(segmentType == SegmentType.JumpLanding){ //if we are on the jump landing, we need to stay on the jump ramp
            trackPiece = SR.track.GetTrackSpline(trackIndex - 1); //get the previous piece of track
        }else{
            trackPiece = SR.track.GetTrackSpline(trackIndex); //get the current piece of track
        }
        if(entity == null){ entity = AddTracker(id); } //create a new tracker if it doesn't exist
        
        // Check for finish line crossing BEFORE updating the spline
        // This must happen before SetTrackSpline because finish line has no spline
        if(segmentType == SegmentType.FinishLine && trust == CarTrust.Trusted){ //if we are on the finish line, we have finished the lap
            bool countLap = entity.segmentsSinceDelocalized > 4; //only count lap if more than 4 segments since delocalisation
            OnCarCrossedFinishLine?.Invoke(id, countLap);
        }
        
        if(trackPiece != null && segmentType != SegmentType.FinishLine){ entity.SetTrackSpline(trackPiece, trackIndex); } //should always be true, but just in case
        entity.SetTrust(trust);
        entity.SetSpeed(speed);
        entity.SetOffset(horizontalOffset);
    }
    CarEntityPosition AddTracker(string id){
        if(trackers.ContainsKey(id)){ return trackers[id]; } //return the existing tracker (should never happen)
        CarEntityPosition entity = Instantiate(carPrefab, Vector3.zero, Quaternion.identity).GetComponent<CarEntityPosition>();
        int model = (int)SR.io.GetCarFromID(id).modelName;
        entity.Setup(id, model); //setup the car entity
        entity.gameObject.name = $"{id} True Position";
        entity.transform.GetChild(0).gameObject.name = $"{id} Model";
        entity.carModelManager = entity.transform.GetChild(0).GetComponent<CarModelManager>();

        //check if there is a CarController with this ID to get the colour from
        CarController cc = SR.cms.GetController(id);
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
        ResetOvertakeTracking(); //reset overtake tracking
        UpdateAIOpponentLocations(); //update the AI opponent locations
    }
    public void CarDelocalised(string id){ if(trackers.ContainsKey(id)){ trackers[id].Delocalise(); } }
    public void ResetLapDelocalizationFlag(string id){ if(trackers.ContainsKey(id)){ trackers[id].segmentsSinceDelocalized = 0; } }
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
    public Transform GetCarVisualTransform(string id){
        if(trackers.ContainsKey(id)){
            return trackers[id].GetVisualTransform();
        }
        return null; //car not found
    }
    public Transform GetCarRealTransform(string id){
        if(trackers.ContainsKey(id)){
            return trackers[id].transform;
        }
        return null; //car not found
    }
    public string GetCarAhead(string startingCar, int ahead = 1)
    {
        if(ahead > trackers.Count - 1) { return null;  } // No car ahead
        else
        {
            //this is probably bugged
            TrackCoordinate startingCoord = GetCarTrackCoordinate(startingCar);
            if (startingCoord == null) { return null; } // Car not found
            var orderedCars = trackers.Keys
                .Where(id => id != startingCar)
                .OrderBy(id => GetCarTrackCoordinate(id).DistanceY(startingCoord))
                .ToList();
            return orderedCars[ahead - 1];
        }
    }
    
    /// <summary>
    /// Get the car behind the starting car.
    /// </summary>
    public string GetCarBehind(string startingCar, int behind = 1)
    {
        if(behind > trackers.Count - 1) { return null;  } // No car behind
        else
        {
            TrackCoordinate startingCoord = GetCarTrackCoordinate(startingCar);
            if (startingCoord == null) { return null; } // Car not found
            var orderedCars = trackers.Keys
                .Where(id => id != startingCar)
                .OrderByDescending(id => GetCarTrackCoordinate(id).DistanceY(startingCoord))
                .ToList();
            return orderedCars[behind - 1];
        }
    }
    
    /// <summary>
    /// Get all car IDs sorted from first to last based on their track position.
    /// Cars further along the track (higher idx, then higher progression) are first.
    /// </summary>
    public List<string> GetSortedCarIDs()
    {
        if(trackers.Count == 0) { return new List<string>(); }
        
        // Sort cars by their track position (idx descending, then progression descending)
        var sortedCars = trackers.Keys
            .OrderByDescending(id => {
                TrackCoordinate coord = GetCarTrackCoordinate(id);
                if(coord == null) return -1;
                // Use idx as primary, progression as secondary (combine into single value for sorting)
                // Multiply idx by 1000 to make it the primary sort factor
                return (coord.idx * 1000.0) + coord.progression;
            })
            .ToList();
        
        return sortedCars;
    }
    
    /// <summary>
    /// Check if a car is either first or last place based on track position.
    /// Returns 1 if first, -1 if last, 0 if neither or car not found.
    /// </summary>
    public int IsFirstOrLast(string carID)
    {
        if(trackers.Count <= 1) { return 0; } // Can't be first or last with 1 or 0 cars
        if(!trackers.ContainsKey(carID)) { return 0; } // Car not found
        
        List<string> sortedCars = GetSortedCarIDs();
        if(sortedCars.Count == 0) { return 0; }
        
        if(sortedCars[0] == carID) { return 1; } // First place
        if(sortedCars[sortedCars.Count - 1] == carID) { return -1; } // Last place
        
        return 0; // Neither first nor last
    }
    public delegate void CarCrossedFinishLine(string id, bool trusted);
    public event CarCrossedFinishLine? OnCarCrossedFinishLine;
    
    // Overtake event: (overtakingCarID, overtakenCarID, newPosition)
    public delegate void OvertakeOccurred(string overtakingCar, string overtakenCar, int newPosition);
    public event OvertakeOccurred? OnOvertakeOccurred;
    
    void Update()
    {
        UpdateOvertakeTracking();
    }
    
    /// <summary>
    /// Updates position tracking and detects overtakes with debouncing.
    /// </summary>
    private void UpdateOvertakeTracking()
    {
        if(trackers.Count < 2) return; // Need at least 2 cars for overtakes
        
        List<string> sortedCars = GetSortedCarIDs();
        if(sortedCars.Count < 2) return;
        
        // Build current position map (1-indexed: 1 = first place)
        Dictionary<string, int> newPositions = new Dictionary<string, int>();
        for(int i = 0; i < sortedCars.Count; i++)
        {
            newPositions[sortedCars[i]] = i + 1;
        }
        
        // Initialize confirmed positions if empty
        if(confirmedPositions.Count == 0)
        {
            foreach(var kvp in newPositions)
            {
                confirmedPositions[kvp.Key] = kvp.Value;
            }
            currentPositions = new Dictionary<string, int>(newPositions);
            return;
        }
        
        // Check for position changes
        foreach(var kvp in newPositions)
        {
            string carID = kvp.Key;
            int newPos = kvp.Value;
            
            // Get current tracked position (or use new position if car is new)
            int currentTrackedPos = currentPositions.ContainsKey(carID) ? currentPositions[carID] : newPos;
            int confirmedPos = confirmedPositions.ContainsKey(carID) ? confirmedPositions[carID] : newPos;
            
            if(newPos != currentTrackedPos)
            {
                // Position changed from what we were tracking
                if(newPos != confirmedPos)
                {
                    // Position differs from confirmed - start or update pending change
                    pendingPositionChanges[carID] = (newPos, Time.time);
                }
                else
                {
                    // Position reverted to confirmed - cancel pending change
                    pendingPositionChanges.Remove(carID);
                }
            }
        }
        
        // Update current positions for next frame comparison
        currentPositions = new Dictionary<string, int>(newPositions);
        
        // Process pending changes that have passed debounce time
        List<string> confirmedChanges = new List<string>();
        foreach(var kvp in pendingPositionChanges)
        {
            string carID = kvp.Key;
            int pendingPos = kvp.Value.newPosition;
            float changeTime = kvp.Value.changeTime;
            
            // Check if current position still matches the pending position
            if(!currentPositions.ContainsKey(carID) || currentPositions[carID] != pendingPos)
            {
                // Position changed again, will be handled in next cycle
                continue;
            }
            
            // Check if debounce time has passed
            if(Time.time - changeTime >= overtakeDebounceTime)
            {
                int oldConfirmedPos = confirmedPositions.ContainsKey(carID) ? confirmedPositions[carID] : pendingPos;
                
                // Only fire overtake event if position improved (lower number = better position)
                if(pendingPos < oldConfirmedPos)
                {
                    // Find who got overtaken (the car now in the position behind)
                    string overtakenCar = FindCarAtPosition(pendingPos + 1, currentPositions);
                    if(overtakenCar != null)
                    {
                        // Queue announcer line for overtake
                        UCarData carData = SR.io?.GetCarFromID(carID);
                        if(carData != null)
                        {
                            if(pendingPos == 1)
                            {
                                SR.pa?.QueueLine(AudioAnnouncerManager.AnnouncerLine.CarTakesLead, 9, carData.modelName);
                            }
                            else
                            {
                                SR.pa?.QueueLine(AudioAnnouncerManager.AnnouncerLine.CarOvertakes, 6, carData.modelName);
                            }
                        }
                        OnOvertakeOccurred?.Invoke(carID, overtakenCar, pendingPos);
                    }
                }
                
                // Update confirmed position
                confirmedPositions[carID] = pendingPos;
                confirmedChanges.Add(carID);
            }
        }
        
        // Remove confirmed changes from pending
        foreach(string carID in confirmedChanges)
        {
            pendingPositionChanges.Remove(carID);
        }
        
        // Clean up positions for cars that no longer exist
        CleanupRemovedCars(newPositions);
    }
    
    /// <summary>
    /// Find the car at a specific position.
    /// </summary>
    private string FindCarAtPosition(int position, Dictionary<string, int> positions)
    {
        foreach(var kvp in positions)
        {
            if(kvp.Value == position) return kvp.Key;
        }
        return null;
    }
    
    /// <summary>
    /// Remove tracking data for cars that no longer exist.
    /// </summary>
    private void CleanupRemovedCars(Dictionary<string, int> currentCars)
    {
        List<string> toRemove = new List<string>();
        foreach(string carID in confirmedPositions.Keys)
        {
            if(!currentCars.ContainsKey(carID))
            {
                toRemove.Add(carID);
            }
        }
        foreach(string carID in toRemove)
        {
            confirmedPositions.Remove(carID);
            pendingPositionChanges.Remove(carID);
            currentPositions.Remove(carID);
        }
    }
    
    /// <summary>
    /// Get the current confirmed position of a car (1 = first place).
    /// Returns -1 if car not found.
    /// </summary>
    public int GetCarPosition(string carID)
    {
        if(confirmedPositions.ContainsKey(carID))
        {
            return confirmedPositions[carID];
        }
        return -1;
    }
    
    /// <summary>
    /// Reset all overtake tracking data. Call this when starting a new race.
    /// </summary>
    public void ResetOvertakeTracking()
    {
        confirmedPositions.Clear();
        pendingPositionChanges.Clear();
        currentPositions.Clear();
    }
}
