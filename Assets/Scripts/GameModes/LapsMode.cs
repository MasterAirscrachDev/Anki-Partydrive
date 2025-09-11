using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static OverdriveServer.NetStructures;

public class LapsMode : GameMode
{
    [SerializeField] int targetLaps = 15;
    Dictionary<string, int> carLaps = new Dictionary<string, int>();
    
    void Awake()
    {
        // Configure laps mode settings
        initialText = "Place cars on the track";
        lineupMessage = "Supercars to the starting line";
    }
    
    protected override void OnModeStart()
    {
        carLaps.Clear();
    }
    
    protected override void OnLineupStarted()
    {
        carLaps.Clear(); //clear the lap count
    }
    
    protected override void OnCountdownStarted(string[] activeCars)
    {
        foreach(string carID in activeCars){
            carLaps[carID] = 0; //reset the lap count
            cms.GetController(carID).SetPosition(0); //reset the position
        }
    }
    
    protected override void OnCarCrossedFinish(string carID, bool score){
        //is there a lapCount for this carID?
        if(carLaps.ContainsKey(carID)){
            carLaps[carID]++;
            cms.GetController(carID).SetLapCount(carLaps[carID]);
            cms.TTS($"{cms.CarNameFromId(carID)} has completed {carLaps[carID]} laps");
        }else{ return; } //if not, ignore it
        //sort the cars by lap count and set the position
        List<KeyValuePair<string, int>> sortedCars = new List<KeyValuePair<string, int>>(carLaps);
        sortedCars.Sort((x, y) => y.Value.CompareTo(x.Value)); //sort by lap count descending
        int position = 1;
        foreach(KeyValuePair<string, int> car in sortedCars){
            cms.GetController(car.Key).SetPosition(position);
            position++;
        }

        //if any car has finished the target number of laps, end the game
        if(carLaps[carID] >= targetLaps){
            EndGame("Race Complete!");
        }
    }
}
