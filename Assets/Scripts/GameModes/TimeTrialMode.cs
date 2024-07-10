using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class TimeTrialMode : MonoBehaviour
{
    [SerializeField] TMP_Text showText;
    [SerializeField] GameObject startButton;
    CMS cms;
    List<carTime> carTimes = new List<carTime>();
    void OnEnable()
    {
        if(cms == null){
            cms = FindObjectOfType<CMS>();
        }
        showText.text = "Line Up The Cars";
        cms.TTS("Please line up the cars before starting");
        startButton.SetActive(true);
    }
    public void StartGame(){
        startButton.SetActive(false);
        StartCoroutine(CountDown());
        FindObjectOfType<CarInteraface>().timeTrialMode = this;
    }
    IEnumerator CountDown(){
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
        cms.SetGlobalLock(false);
        yield return new WaitForSeconds(1);
        showText.text = "";
        StartCoroutine(EndGame());
    }
    IEnumerator EndGame(){
        //wait 5:59
        yield return new WaitForSeconds(59);
        cms.TTS("4 minutes remaining");
        yield return new WaitForSeconds(60);
        cms.TTS("3 minutes remaining");
        yield return new WaitForSeconds(60);
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
        FindObjectOfType<CarInteraface>().timeTrialMode = null;
    }
    public void CarCrossedFinish(string[] data){
        string carID = data[1];
        string longTime = data[2];
        DateTime time = DateTime.FromBinary(long.Parse(longTime));
        //is there a carTime for this car?
        carTime ct = carTimes.Find(x => x.carID == carID);
        if(ct == null){
            ct = new carTime();
            ct.carID = carID;
            ct.lapStartedTime = time;
            carTimes.Add(ct);
        }
        else{
            //set the lap time if it is less than the current lap time
            float newLapTime = (float)(time - ct.lapStartedTime).TotalSeconds;
            if(ct.lapTime == 0 || newLapTime < ct.lapTime){
                ct.lapTime = newLapTime;
                cms.TTS($"{cms.CarNameFromId(carID)} did a new fastest lap");
                cms.GetController(carID).SetTimeTrialTime(ct.lapTime);

            }
            ct.lapStartedTime = time;
        }
        //sort the list by lap time (but put cars with no lap time at the end)
        carTimes.Sort((x, y) => x.lapTime == 0 ? 1 : y.lapTime == 0 ? -1 : x.lapTime.CompareTo(y.lapTime));
        for(int i = 0; i < carTimes.Count; i++){
            if(cms.GetController(carTimes[i].carID) != null){
                cms.GetController(carTimes[i].carID).SetPosition(i + 1);
            }
        }
    }
    class carTime{
        public string carID;
        public DateTime lapStartedTime;
        public float lapTime;
    }
}
