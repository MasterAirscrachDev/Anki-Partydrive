using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Threading.Tasks;
using UnityEngine.UI;

public class CarBalancer : MonoBehaviour
{
    [SerializeField] CarInteraface carInterface;
    [SerializeField] TrackGenerator trackGenerator;
    [SerializeField] CarEntityTracker carTracker;
    [SerializeField] UIManager ui;
    [SerializeField] TMP_Text messageText;
    [SerializeField] Button recalibrateButton, saveButton, backButton;
    TrackPiece[] requestedTrack;
    float startTime;
    string carID;
    UCarData carData;
    bool waitingForTrack = true, trackingLaps = false;

    bool trackUpdateSubbed = false, finishLineSubbed = false;

    int step = -1; float timeout;
    List<float> lapTimes = new List<float>();
    const float targetLapTime = 7.75f; //7.75 is the target laptime
    const float targetLapTimeTolerance = 0.02f; //0.02 seconds tolerance for lap time
    int currentSpeedMod = 0; //car mod value, used to calculate the target lap time
    int increment = 10; //increment for speed mod
    int directionOfIncrement = 0; //direction of increment, 1 for up, -1 for down

    //7.75 is the target laptime

    void Start(){
        requestedTrack = new TrackPiece[9];
        requestedTrack[0] = new TrackPiece(TrackPieceType.PreFinishLine, 0, false);
        requestedTrack[1] = new TrackPiece(TrackPieceType.FinishLine, 0, false);
        requestedTrack[2] = new TrackPiece(TrackPieceType.Straight, 0, true);
        requestedTrack[3] = new TrackPiece(TrackPieceType.Turn, 0, true);
        requestedTrack[4] = new TrackPiece(TrackPieceType.Turn, 0, true);
        requestedTrack[5] = new TrackPiece(TrackPieceType.Straight, 0, false);
        requestedTrack[6] = new TrackPiece(TrackPieceType.Straight, 0, false);
        requestedTrack[7] = new TrackPiece(TrackPieceType.Turn, 0, true);
        requestedTrack[8] = new TrackPiece(TrackPieceType.Turn, 0, true);
    }
    void OnDisable(){
        SubTrack(false);
        SubFinishLine(false);
        waitingForTrack = false;
        trackingLaps = false;
        currentSpeedMod = 0;
        increment = 10;
        directionOfIncrement = 0;
        step = -1;
        saveButton.interactable = false;
        recalibrateButton.interactable = false;
        messageText.gameObject.SetActive(false);
        Debug.Log($"CarBalancer disabled");
    }
    void SubTrack(bool sub){
        if(sub && !trackUpdateSubbed){
            trackGenerator.OnTrackValidated += OnTrack;
            trackUpdateSubbed = true;
        }else if(!sub && trackUpdateSubbed){
            trackGenerator.OnTrackValidated -= OnTrack;
            trackUpdateSubbed = false;
        }
    }
    void SubFinishLine(bool sub){
        if(sub && !finishLineSubbed){
            carTracker.OnCarCrossedFinishLine += OnFinish;
            finishLineSubbed = true;
        }else if(!sub && finishLineSubbed){
            carTracker.OnCarCrossedFinishLine -= OnFinish;
            finishLineSubbed = false;
        }
    }
    // Start is called before the first frame update
    public void Setup(string id) {
        step = 0; timeout = 1;
        carID = id;
        waitingForTrack = true;
        trackingLaps = false;
        carData = carInterface.GetCarFromID(carID);
        if(carData == null){
            Debug.Log($"Car {carID} not found, disabling balancer");
            return;
        }
        SubTrack(true);
        SubFinishLine(true);
    }
    void Update(){
        if(timeout > 0){ timeout -= Time.deltaTime; return; }
        if(step == 0){
            Debug.Log($"CarBalancer step 0, waiting for track to be built");
            step = 1;
            messageText.gameObject.SetActive(true);
            messageText.text = $"Please Build this track\n(Straights can be substituted)\n\nThen scan with car: {carData.name}";
            ui.SwitchToTrackCamera(true);
            if(trackGenerator.hasTrack){
                bool matches = CheckTrack(trackGenerator.GetTrackPieces());
                if(matches){
                    Debug.Log("Track matches, balancing car");
                    SubTrack(false);
                    messageText.text = $"Track ok, press the button to start balancing";
                    ui.SetUILayer(4); //disable Scanning UI
                    recalibrateButton.interactable = true;
                    step = -1;
                    return;
                }
            }
            trackGenerator.Generate(requestedTrack, false);
            waitingForTrack = true;
        }else if(step == 1){
            if(waitingForTrack){ timeout = 1.5f; return; }
            else{
                Debug.Log($"CarBalancer step 1, track scanned");
                ui.SetUILayer(4); //disable Scanning UI
                //car should now be lining up at this point
                timeout = 2.1f;
                step = 2;
            }
        } else if(step == 2){
            Debug.Log($"CarBalancer step 2, wallbounce to left");
            carInterface.ControlCar(carData, 300, -150);
            timeout = 2.1f;
            step = 3;
        }else if(step == 3){
            Debug.Log($"CarBalancer step 3, centering car");
            carInterface.ControlCar(carData, 500, 0);
            timeout = 8;
            step = 4;
        }else if(step == 4){
            carInterface.ControlCar(carData, 500, 0);
            trackingLaps = false;
            step = -1;
        }
    }
    void OnTrack(TrackPiece[] track){
        Debug.Log($"Track validated, callback");
        try{
            if(waitingForTrack){
                bool matches = CheckTrack(track);
                if(matches){
                    Debug.Log("Track matches, balancing car");
                    trackGenerator.OnTrackValidated -= OnTrack;
                    waitingForTrack = false;
                }
                else{
                    Debug.Log("Track does not match, redoing track");
                    trackGenerator.Generate(requestedTrack, false);
                }
            }else{
                Debug.Log("Not waiting for track somehow");
            }
        }catch (Exception e){
            Debug.Log($"Error in track validation: {e.Message}");
        }
    }
    bool CheckTrack(TrackPiece[] track){
        if(track.Length != requestedTrack.Length){ return false; }
        for (int i = 0; i < track.Length; i++)
        {
            if(track[i].type != requestedTrack[i].type){
                if(!(track[i].type == TrackPieceType.FnFSpecial && requestedTrack[i].type == TrackPieceType.Straight)){
                    return false;
                }
            }
        }
        return true;
    }
    void OnFinish(string id){
        if(id == carID){
            if(!trackingLaps){
                trackingLaps = true;
                startTime = Time.time;
                lapTimes.Clear();
                messageText.text = $"Awaiting Lap";
            }else{
                float lapTime = Time.time - startTime;
                startTime = Time.time;
                lapTimes.Add(lapTime);
                string lapTimeString = "Lap Times:\n";
                for(int i = 0; i < lapTimes.Count; i++){
                    lapTimeString += $"{i + 1}: {lapTimes[i]:F2}s\n";
                }
                messageText.text = lapTimeString;
                if(lapTimes.Count >= 6){
                    lapTimes.Sort();
                    float last = lapTimes[0];
                    int outliers = 0;
                    //calculate average lap time
                    float averageLapTime = 0;
                    for(int i = 1; i < lapTimes.Count; i++){
                        if(lapTimes[i] - last < 0.8f){
                            averageLapTime += lapTimes[i];
                        }else{
                            outliers++;
                        }
                    }
                    averageLapTime /= (lapTimes.Count - 1);
                    messageText.text += $"Average Lap Time: {averageLapTime:F2}s\nOutliers: {outliers}\n\n";

                    if(averageLapTime > targetLapTime + targetLapTimeTolerance){ // Too slow
                        if(directionOfIncrement == -1){ //if we were slowing down, half the increment
                            increment = Mathf.Clamp(Mathf.RoundToInt(increment / 2), 1, 10);
                        }
                        currentSpeedMod += increment;
                        directionOfIncrement = 1;
                        carInterface.ControlCar(carInterface.GetCarFromID(carID), 500 + currentSpeedMod, 0);
                        lapTimes.Clear();
                        messageText.text += $"Car is too slow, speeding up {currentSpeedMod}\n";
                    }else if(averageLapTime < targetLapTime - targetLapTimeTolerance){ // Too fast
                        if(directionOfIncrement == 1){ //if we were speeding up, half the increment
                            increment = Mathf.Clamp(Mathf.RoundToInt(increment / 2), 1, 10);
                        }
                        currentSpeedMod -= increment;
                        directionOfIncrement = -1;
                        carInterface.ControlCar(carInterface.GetCarFromID(carID), 500 + currentSpeedMod, 0);
                        lapTimes.Clear();
                        messageText.text += $"Car is too fast, slowing down {currentSpeedMod}\n";
                    }
                    else{
                        SubFinishLine(false);
                        carInterface.ControlCar(carInterface.GetCarFromID(carID), 0, 0);
                        messageText.text += $"Balancing complete!\n";
                        if(currentSpeedMod != 0){
                            messageText.text += $"Car speed mod: {currentSpeedMod}\n";
                            saveButton.interactable = true;
                        }
                        recalibrateButton.interactable = true;
                    }
                }
            }
        }
    }
    public void Recalibrate(){
        recalibrateButton.interactable = false;
        saveButton.interactable = false;
        SubFinishLine(true);
        messageText.text = $"Recalibrating car...\nPlease wait";
        waitingForTrack = false;
        trackingLaps = false;
        currentSpeedMod = 0;
        increment = 10;
        directionOfIncrement = 0;
        step = 1;
    }
    public void SaveSpeedMod(){
        SaveSpeedModToFile(carID, currentSpeedMod);
        saveButton.interactable = false;
    }
    async Task SaveSpeedModToFile(string id, int speedMod){
        //save the speed mod to the car data
        FileSuper fs = new FileSuper("AnkiServer", "ReplayStudios");
        Save s = await fs.LoadFile($"{id}.dat");
        if(s == null){
            messageText.text = $"Car not found in file system\n";
            return;
        }else{
            s.SetVar("speedBalance", speedMod);
        }
        await fs.SaveFile($"{id}.dat", s);
        messageText.text = $"Car speed mod saved to file\n";
        carInterface.ApiCallV2(NetDefinitions.EVENT_REFRESH_CONFIGS, 0);
    }
    public void BackToMenu(){
        ui.SetUILayer(0); //go back to main menu
        carInterface.ControlCar(carData, 0, 0);
    }
}
