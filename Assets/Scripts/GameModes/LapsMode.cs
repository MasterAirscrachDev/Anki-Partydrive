using System.Collections.Generic;
using System.Linq;
using static OverdriveServer.NetStructures;
using static AudioAnnouncerManager.AnnouncerLine;
using UnityEngine;

public class LapsMode : GameMode
{
    [Header("Lap Settings")]
    [SerializeField] int lapsSmallTrack = 20;
    [SerializeField] int lapsMediumTrack = 15;
    [SerializeField] int lapsLargeTrack = 10;
    
    int targetLaps = 15;
    Dictionary<string, int> carLaps = new Dictionary<string, int>();
    
    void Awake() {// Configure laps mode settings
        initialText = "Place cars on the track";
        lineupMessage = "Supercars to the starting line";
    }
    
    protected override void OnModeStart()
    { 
        carLaps.Clear();
        // Set target laps based on track size
        if(SR.track != null && SR.track.hasTrack)
        {
            TrackGenerator.TrackSize trackSize = SR.track.GetTrackSize();
            targetLaps = trackSize switch
            {
                TrackGenerator.TrackSize.Small => lapsSmallTrack,
                TrackGenerator.TrackSize.Medium => lapsMediumTrack,
                TrackGenerator.TrackSize.Large => lapsLargeTrack,
                _ => lapsMediumTrack
            };
        }
    }
    
    protected override void OnLineupStarted()
    { carLaps.Clear();  } //clear the lap count
    
    protected override void OnCountdownStarted(string[] activeCars) {
        base.OnCountdownStarted(activeCars); // Clear previousPositions for overtake tracking
        foreach(string carID in activeCars){
            carLaps[carID] = 0; //reset the lap count
            cms.GetController(carID).SetPosition(0); //reset the position
            carEntityTracker.ResetLapDelocalizationFlag(carID); //reset delocalization flag so first lap counts
        }
    }
    
    /// <summary>
    /// Override position updates to consider lap count first, then track position for ties
    /// </summary>
    protected override void UpdatePositions()
    {
        if(carLaps.Count == 0) { return; }
        
        // Sort cars by lap count (descending), then by track position for ties
        List<string> sortedCars = carLaps.Keys
            .OrderByDescending(carID => {
                // Get lap count for this car
                int laps = carLaps.ContainsKey(carID) ? carLaps[carID] : 0;
                
                // Get track position for tie breaking
                TrackCoordinate coord = carEntityTracker.GetCarTrackCoordinate(carID);
                double trackPosition = 0;
                if(coord != null)
                {
                    trackPosition = (coord.idx * 1000.0) + coord.progression;
                }
                
                // Combine: laps * 1000000 + trackPosition
                // This ensures laps are primary sort, track position breaks ties
                return (laps * 1000000.0) + trackPosition;
            })
            .ToList();
        
        // Apply positions with overtake detection
        ApplyPositionsWithOvertakeDetection(sortedCars);
    }
    
    protected override void OnCarCrossedFinish(string carID, bool score){
        //is there a lapCount for this carID?
        if(carLaps.ContainsKey(carID)){
            // Only count the lap if the car wasn't delocalized
            if(score){
                carLaps[carID]++;
                try{
                    cms.GetController(carID).SetLapCount(carLaps[carID]);
                }catch{
                    //Debug.LogWarning($"Could not set lap count for car {carID}");
                }
                
                //cms.TTS($"{cms.CarNameFromId(carID)} has completed {carLaps[carID]} laps");
            }
        }else{ return; } //if not, ignore it
        
        // Position updates are now handled by the UpdatePositions ticker
        // No need to manually update positions here
        
        //if any car has finished the target number of laps, end the game
        if(carLaps[carID] >= targetLaps){
            EndGame("Race Complete!");
            //debug log the lap counts
            string results = "Final Lap Counts:\n";
            foreach(var kvp in carLaps){
                results += $"{cms.CarNameFromId(kvp.Key)}: {kvp.Value} laps\n";
            }
            Debug.Log(results);
            //get the model of the winning car
            ModelName winningCarModel = cms.CarModelFromId(carID);
            SR.pa.PlayLine(CarWins, winningCarModel);
        }
    }
}