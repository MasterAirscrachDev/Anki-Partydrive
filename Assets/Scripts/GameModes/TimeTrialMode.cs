using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static OverdriveServer.NetStructures;

public class TimeTrialMode : MonoBehaviour
{
    [SerializeField] TMP_Text showText;
    [SerializeField] GameObject startButton, menuButton, replayButton;
    CarEntityTracker carEntityTracker;
    CarInteraface carInteraface;
    CMS cms;
    List<CarTime> carTimes = new List<CarTime>();
    void OnEnable()
    {
        if(cms == null){
            cms = FindObjectOfType<CMS>();
            carInteraface = FindObjectOfType<CarInteraface>();
            carEntityTracker = FindObjectOfType<CarEntityTracker>();
        }
        StartMode();
    }
    void StartMode(){
        showText.text = "Place all cars on the track";
        cms.SetGlobalLock(true);
        startButton.SetActive(true);
        menuButton.SetActive(true);
        replayButton.SetActive(false);
    }
    public void LineupAndStart(){
        FindObjectOfType<UIManager>().SwitchToTrackCamera(true);
        startButton.SetActive(false);
        FindObjectOfType<CarInteraface>().ApiCallV2(SV_LINEUP, "");
        cms.TTS("Supercars to the starting line");
        carInteraface.OnLineupEvent += OnLineupUpdate;
    }
    IEnumerator CountDown(){
        FindObjectOfType<UIManager>().SwitchToTrackCamera(true);
        string[] activeCars = carEntityTracker.GetActiveCars();
        foreach(string carID in activeCars){
            carTimes.Add(new CarTime(carID));
        }
        showText.text = "Get Ready!";
        cms.TTS("Get Ready!");
        yield return new WaitForSeconds(3);
        showText.text = "3";
        cms.TTS("3");
        yield return new WaitForSeconds(1);
        showText.text = "2";
        cms.TTS("2");
        yield return new WaitForSeconds(1);
        showText.text = "1";
        cms.TTS("1");
        yield return new WaitForSeconds(1);
        showText.text = "GO!";
        cms.TTS("Go!");
        carEntityTracker.OnCarCrossedFinishLine += CarCrossedFinish;
        cms.SetGlobalLock(false);
        foreach(CarTime ct in carTimes){
            ct.lapStartedTime = Time.time;
        }
        yield return new WaitForSeconds(1);
        showText.text = "";
        StartCoroutine(EndGame());
    }
    IEnumerator EndGame(){
        //wait 2:59
        yield return new WaitForSeconds(59);
        cms.TTS("2 minutes remaining");
        yield return new WaitForSeconds(60);
        cms.TTS("1 minute remaining");
        yield return new WaitForSeconds(50);
        int seconds = 10;
        while(seconds > 0){
            cms.TTS($"{seconds}");
            showText.text = $"{seconds}";
            yield return new WaitForSeconds(1);
            seconds--;
        }
        showText.text = "Game Over!";
        cms.SetGlobalLock(true);
        cms.StopAllCars();
        cms.TTS("Game Over!");
        carEntityTracker.OnCarCrossedFinishLine -= CarCrossedFinish;
        menuButton.SetActive(true);
        replayButton.SetActive(true);
    }
    public void CarCrossedFinish(string carID){
        //is there a carTime for this car?
        CarTime ct = carTimes.Find(x => x.id == carID);
        if(ct == null){
            ct = new CarTime(carID);
            ct.lapStartedTime = Time.time;
            carTimes.Add(ct);
        }
        else{
            //set the lap time if it is less than the current lap time
            float newLapTime = Time.time - ct.lapStartedTime;
            if(ct.bestLapTime == 0 || newLapTime < ct.bestLapTime){
                ct.bestLapTime = newLapTime;
                cms.TTS($"{cms.CarNameFromId(carID)} did a new fastest lap");
                cms.GetController(carID).SetTimeTrialTime(ct.bestLapTime);
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
    public void OnLineupUpdate(string id, int remaining){
        if(remaining == 0){
            StartCoroutine(CountDown());
            carInteraface.OnLineupEvent -= OnLineupUpdate;
        }
    }
    public void BackToMenu(){
        FindObjectOfType<UIManager>().SetUILayer(0);
    }
    public void Replay(){
        StartMode();
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
