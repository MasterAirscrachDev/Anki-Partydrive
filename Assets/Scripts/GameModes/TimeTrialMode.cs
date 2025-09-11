using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTrialMode : GameMode
{
    List<CarTime> carTimes = new List<CarTime>();
    
    void Awake()
    {
        // Configure time trial settings
        initialText = "Place cars on the track";
        lineupMessage = "Supercars to the starting line";
    }
    
    protected override void OnModeStart()
    {
        carTimes.Clear();
    }
    
    protected override void OnLineupStarted()
    {
        carTimes.Clear();
    }
    
    protected override void OnCountdownStarted(string[] activeCars)
    {
        foreach(string carID in activeCars){
            carTimes.Add(new CarTime(carID));
        }
    }
    
    protected override void OnGameStarted(string[] activeCars)
    {
        foreach(CarTime ct in carTimes){
            ct.lapStartedTime = Time.time;
        }
        
        // Start the 3-minute time trial countdown
        StartCoroutine(TimeTrialTimeLimit());
    }
    
    /// <summary>
    /// Handles the 3-minute time trial countdown
    /// </summary>
    IEnumerator TimeTrialTimeLimit()
    {
        // Wait 2 minutes 59 seconds (179 seconds total for 3-minute trial)
        yield return new WaitForSeconds(60);
        cms.TTS("2 minutes remaining");
        
        yield return new WaitForSeconds(60);
        cms.TTS("1 minute remaining");
        
        yield return new WaitForSeconds(50);
        
        // Final 10-second countdown
        int seconds = 10;
        while(seconds > 0 && gameActive)
        {
            cms.TTS($"{seconds}");
            showText.text = $"{seconds}";
            yield return new WaitForSeconds(1);
            seconds--;
        }
        
        if(gameActive)
        {
            EndGame("Time's Up!");
        }
    }
    
    protected override void OnCarCrossedFinish(string carID, bool score)
    {
        //is there a carTime for this car?
        CarTime ct = carTimes.Find(x => x.id == carID);
        if(ct == null){
            ct = new CarTime(carID);
            ct.lapStartedTime = Time.time;
            carTimes.Add(ct);
        }
        else{
            if(score){
                //set the lap time if it is less than the current lap time
                float newLapTime = Time.time - ct.lapStartedTime;
                if(ct.bestLapTime == 0 || newLapTime < ct.bestLapTime){
                    ct.bestLapTime = newLapTime;
                    cms.TTS($"{cms.CarNameFromId(carID)} did a new fastest lap");
                    cms.GetController(carID).SetTimeTrialTime(ct.bestLapTime);
                }
            }
            
            ct.lapStartedTime = Time.time;
        }
        //sort the list by lap time (but put cars with no lap time at the end)
        carTimes.Sort((x, y) => x.bestLapTime == 0 ? 1 : y.bestLapTime == 0 ? -1 : x.bestLapTime.CompareTo(y.bestLapTime));
        for(int i = 0; i < carTimes.Count; i++){
            if(cms.GetController(carTimes[i].id) != null){
                cms.GetController(carTimes[i].id).SetPosition(i + 1);
            }
        }
    }
    
    class CarTime{
        public CarTime(string id) {
            this.id = id;
        }
        public string id;
        public float lapStartedTime;
        public float bestLapTime;
    }
}
