using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static OverdriveServer.NetStructures;

public class LapsMode : MonoBehaviour
{
    [SerializeField] TMP_Text showText;
    [SerializeField] GameObject startButton, menuButton, replayButton;
    CarEntityTracker carEntityTracker;
    CarInteraface carInteraface;
    CMS cms;
    Dictionary<string, int> carLaps = new Dictionary<string, int>();
    void OnEnable()
    {
        if(cms == null){
            cms = FindObjectOfType<CMS>();
            carInteraface = CarInteraface.io;
            carEntityTracker = FindObjectOfType<CarEntityTracker>();
        }
        StartMode();
    }
    void StartMode(){
        showText.text = "Place cars on the track";
        cms.SetGlobalLock(true);
        startButton.SetActive(true);
        menuButton.SetActive(true);
        replayButton.SetActive(false);
    }
    public void LineupAndStart(){
        FindObjectOfType<UIManager>().SwitchToTrackCamera(true);
        startButton.SetActive(false);
        FindObjectOfType<CarInteraface>().ApiCallV2(SV_LINEUP, 0);
        cms.TTS("Supercars to the starting line");
        carLaps.Clear(); //clear the lap count
        carInteraface.OnLineupEvent += OnLineupUpdate;
    }
    IEnumerator CountDown(){
        FindObjectOfType<UIManager>().SwitchToTrackCamera(true);
        string[] activeCars = carEntityTracker.GetActiveCars();
        foreach(string carID in activeCars){
            carLaps[carID] = 0; //reset the lap count
            cms.GetController(carID).SetPosition(0); //reset the position
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
        cms.SetGlobalLock(false);
        yield return new WaitForSeconds(1);
        showText.text = "";
        StartCoroutine(StartListeningForFinishLine());
    }
    IEnumerator StartListeningForFinishLine(){ //3 seconds after the start
        yield return new WaitForSeconds(3);
        carEntityTracker.OnCarCrossedFinishLine += CarCrossedFinish;
    }
    void EndGame(){
        showText.text = "Game Over!";
        cms.SetGlobalLock(true);
        cms.StopAllCars();
        cms.TTS("Game Over!");
        carEntityTracker.OnCarCrossedFinishLine -= CarCrossedFinish;
        menuButton.SetActive(true);
        replayButton.SetActive(true);
    }
    public void CarCrossedFinish(string carID, bool score){
        //is there a lapCount for this carID?
        if(carLaps.ContainsKey(carID)){
            carLaps[carID]++;
            cms.GetController(carID).SetLapCount(carLaps[carID]);
        }
        //sort the cars by lap count and set the position
        List<KeyValuePair<string, int>> sortedCars = new List<KeyValuePair<string, int>>(carLaps);
        sortedCars.Sort((x, y) => y.Value.CompareTo(x.Value)); //sort by lap count descending
        int position = 1;
        foreach(KeyValuePair<string, int> car in sortedCars){
            cms.GetController(car.Key).SetPosition(position);
            position++;
        }

        //if any car has finished 10 laps, end the game
        if(carLaps[carID] >= 10){
            EndGame();
        }
    }
    public void OnLineupUpdate(string id, int remaining){
        //Debug.Log($"Lineup update: {id} {remaining}");
        if(remaining == 0){
            StartCoroutine(CountDown());
            carInteraface.OnLineupEvent -= OnLineupUpdate;
        }
    }
    public void BackToMenu(){ //called by ui
        FindObjectOfType<UIManager>().SetUILayer(0);
    }
    public void Replay(){ //called by ui
        StartMode();
    }
}
