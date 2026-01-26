using System.Collections.Generic;
using static OverdriveServer.NetStructures;
using static AudioAnnouncerManager.AnnouncerLine;
using UnityEngine;

public class PartyMode : GameMode
{
    [SerializeField] int targetLaps = 15;
    Dictionary<string, int> carLaps = new Dictionary<string, int>();
    
    void Awake() {// Configure laps mode settings
        initialText = "Place cars on the track";
        lineupMessage = "Supercars to the starting line";
    }
    
    protected override void OnModeStart()
    { 
        carLaps.Clear();
        // Don't spawn elements here - wait until countdown starts
        
        // Subscribe to out of energy event
        cms.onCarNoEnergy += OnCarNoEnergy;
    }
    
    protected override void OnLineupStarted()
    { carLaps.Clear();  } //clear the lap count
    
    protected override void OnCountdownStarted(string[] activeCars) {
        // Spawn powerups and energy elements before the countdown
        SR.tem.SpawnElements();
        
        foreach(string carID in activeCars){
            carLaps[carID] = 0; //reset the lap count
            cms.GetController(carID).SetPosition(0); //reset the position
            carEntityTracker.ResetLapDelocalizationFlag(carID); //reset delocalization flag so first lap counts
        }
    }
    
    protected override void OnCarCrossedFinish(string carID, bool score){
        //is there a lapCount for this carID?
        if(carLaps.ContainsKey(carID)){
            // Only count the lap if the car wasn't delocalized
            if(score){
                carLaps[carID]++;
                try{
                    //cms.GetController(carID).SetLapCount(carLaps[carID]);
                }catch{
                    //Debug.LogWarning($"Could not set lap count for car {carID}");
                }
            }
        }else{ return; } //if not, ignore it
        //sort the cars by lap count and set the position
        List<KeyValuePair<string, int>> sortedCars = new List<KeyValuePair<string, int>>(carLaps);
        sortedCars.Sort((x, y) => y.Value.CompareTo(x.Value)); //sort by lap count descending
        int position = 1;
        foreach(KeyValuePair<string, int> car in sortedCars){
            try{
                cms.GetController(car.Key).SetPosition(position);
            }catch(System.Exception e){
                //Debug.LogError($"Error setting position for car {car.Key}: {e.Message}\n{e.StackTrace}");
            }
            position++;
        }
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
    
    protected override void OnModeCleanup()
    {
        // Unsubscribe from out of energy event
        if(cms != null)
        {
            cms.onCarNoEnergy -= OnCarNoEnergy;
        }
    }
    
    void OnCarNoEnergy(string carID, CarController controller)
    {
        // Apply 2 second, 100% speed reduction
        controller.AddSpeedModifier(-100, true, 2f, "NoEnergy");
        Debug.Log($"Car {carID} ran out of energy - applying 2s speed penalty");
    }
}