using System.Collections;
using System.Collections.Generic;
using static OverdriveServer.NetStructures;
using static AudioAnnouncerManager.AnnouncerLine;
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
            carEntityTracker.ResetLapDelocalizationFlag(carID); //reset delocalization flag so first lap counts
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
        SR.pa.PlayLine(AudioAnnouncerManager.AnnouncerLine.RemainingTime2Mins);
        
        yield return new WaitForSeconds(60);
        SR.pa.PlayLine(AudioAnnouncerManager.AnnouncerLine.RemainingTime1Min);
        
        yield return new WaitForSeconds(50);
        
        // Final 10-second countdown
        int seconds = 10;
        AudioAnnouncerManager.AnnouncerLine[] announcerLines = new AudioAnnouncerManager.AnnouncerLine[]
        { Count10, Count9, Count8, Count7, Count6, Count5, Count4, Count3, Count2, Count1 };
        while(seconds > 0 && gameActive)
        {
            SR.pa.PlayLine(announcerLines[10 - seconds]);
            showText.text = $"{seconds}";
            yield return new WaitForSeconds(1);
            seconds--;
        }
        
        if(gameActive)
        {
            EndGame("Time's Up!", TimesUp);
            
            //get the model of the winning car
            ModelName winningCarModel = cms.CarModelFromId(carTimes[0].id);
            SR.pa.PlayLine(CarWins, winningCarModel);
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
                    //cms.TTS($"{cms.CarNameFromId(carID)} did a new fastest lap");
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
