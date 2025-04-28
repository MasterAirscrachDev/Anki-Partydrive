using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AIController : MonoBehaviour
{
    [SerializeField][TextArea(5, 10)] string Debug;
    CarController carController;
    CarEntityTracker carEntityTracker;
    [SerializeField] int depth = 3; //planning depth
    [SerializeField] float timeout = 2f; //timeout for planning
    [SerializeField] string setCarID = ""; //debugging variable to set the car ID
    [SerializeField] AIState state = AIState.Speed; //state of the AI
    const float CAR_SPACING = 45f;
    TrackCoordinate[] carLocations; //list of other car locations
    List<TrackCoordinate> obstacles = new List<TrackCoordinate>(); //list of other obsticals
    TrackCoordinate ourCoord, currentTarget; //current target of the car (may be null)
    float timer = 0f, fixCarTimer = 10f; //timer for the AI logic
    string ourID;

    int currentTargetSpeed = 0;
    float currentTargetOffset = 0f;
    bool setup = false; //setup variable to check if the AI is setup
    bool inputsLocked = true;
    // Start is called before the first frame update
    void Start()
    {
        if(setup){ return; } //if the AI is already setup, return
        carController = GetComponent<CarController>();
        carController.Setup(true); //setup the car controller
        carEntityTracker = FindObjectOfType<CarEntityTracker>();
        carController.statSteerMod = 2; //AIs have more steering power
        string[] names = { "Jimmy Bot", "Bob Bot", "Doug Bot", "Gary Bot", "Jess Bot", "Sam Bot", "Kate Bot", "Dave Bot" };

        carController.SetPlayerName(names[Random.Range(0, names.Length)]); //set the player name to AI
        carController.SetColour(new Color(1, 0, 0)); //set the color to red

        setup = true; //set the setup variable to true
    }
    public void SetOpponentLocations(TrackCoordinate[] coords){
        carLocations = coords; //set the car locations to the given coordinates
    }
    public void SetOurLocation(TrackCoordinate coord){
        ourCoord = coord; //set our car location to the given coordinates
    }
    public void EnteredCarManagement(){
        if(carController.GetCarID() == ""){ //if ourID is not set,
            FindObjectOfType<CMS>().RemoveAI(ourID); //remove this AI and Controller
        }
    }
    public void SetID(string id){ //Sets our Car or tries to reconnect to the car
        ourID = id;
        if(!setup){ Start();}
        UCarData carData = CarInteraface.io.GetCarFromID(id);
        carController.SetCar(carData);
    }
    public string GetID() => ourID; //returns the ID of the car
    public void SetInputsLocked(bool locked){
        inputsLocked = locked; //set the inputs locked variable to the given value
        if(!locked){
            AILogic(); //if the inputs have just been unlocked, run the AI logic once
            UpdateInputs(); //update the inputs
            carController.DoControlImmediate(); //update the car controller immediately
        }
    }

    // Update is called once per frame
    void Update() {
        if(!inputsLocked){
            timer += Time.deltaTime;
            if(timer > timeout){
                timer = 0f;
                AILogic();
            }
            UpdateInputs(); //update the inputs if they are not locked
            
        }else{
            carController.Iaccel = 0; //if the inputs are locked, set the acceleration to 0
            carController.Isteer = 0; //if the inputs are locked, set the steering to 0
            carController.Iboost = false; //if the inputs are locked, set the boost to false
            carController.Idrift = 0; //if the inputs are locked, set the drift to 0
            carController.IitemA = false; //if the inputs are locked, set the item A to false
            carController.IitemB = false; //if the inputs are locked, set the item B to false
        }
        

        if(setCarID != ""){ //if we have a car ID set, set the car ID to the one we have set
            SetID(setCarID);
            setCarID = ""; //reset the car ID
        }

        if(carController.GetCarID() == ""){ //if we don't have a car ID, set the car ID to the one we have set
            fixCarTimer -= Time.deltaTime; //decrease the timer
            if(fixCarTimer < 0f){ //if the timer is less than 0, set the car ID to the one we have set
                SetID(ourID); //set the car ID to the one we have set
                fixCarTimer = 10f; //reset the timer
            }
        }
    }
    void UpdateInputs(){
        (int controllerSpeed, float controllerOffset) = carController.GetMetrics(); //get the current speed and offset of the car
        carController.Iaccel = currentTargetSpeed > controllerSpeed ? 1 : 0; //set the acceleration to 1 if we want to go faster, -1 if we want to go slower
        
        if(Mathf.Abs(currentTargetOffset - controllerOffset) < 2f){ //if we are close to the target offset, set the steering to 0
            carController.Isteer = 0;
        }else{
            carController.Isteer = Mathf.Clamp(currentTargetOffset - controllerOffset, -1f, 1f); //set the steering to the difference between the target offset and the current offset
        }
    }

    //lane values may seem odd, they are based on the true lanes
    //{72.25f, 63.75f, 55.25f, 46.75f, 38.25f, 29.75f, 21.25f, 12.75f, 4.25f, -4.25f, -12.75f, -21.25f, -29.75f, -38.25f, -46.75f, -55.25f, -63.75f, -72.25f};

    void AILogic(){

        SegmentType[] futureTrack = new SegmentType[depth + 1]; //depth + 1 because we want to include the current segment
        for (int i = 0; i < futureTrack.Length; i++)
        { futureTrack[i] = TrackGenerator.track.GetSegmentType(ourCoord.idx + i); }
        string log = "";
        for (int i = 0; i < futureTrack.Length; i++)
        { log += futureTrack[i] + " "; }

        bool carClose = carLocations.Any(x => x.idx == ourCoord.idx); //check if any car is on the same segment as us
        log += carClose ? "Car Close\n" : "No Car Close\n"; //add to the log if a car is close or not

        int targetSpeed = currentTargetSpeed;
        float targetOffset = currentTargetOffset;
        carController.Iboost = false; //set the boost to false by default

        bool upcomingTurn = futureTrack[0] == SegmentType.Turn || futureTrack[1] == SegmentType.Turn;
        
        if(upcomingTurn){ //do turn logic
            bool onTurnNow = futureTrack[0] == SegmentType.Turn; //check if we are on a turn now

            log += "Upcoming Turn"; //add to the log if a turn is upcoming
            int turnIndex = futureTrack[0] == SegmentType.Turn ? 0 : 1; //check if the next segment is a turn or the one after that
            if(carClose){ //try to avoid the preceding car
                targetSpeed = 550 + (onTurnNow ? 0 : 200); //set the target speed to 550 if we are on a turn, 750 if we are not
                float opposingOffset = 0f;
                for (int i = 0; i < carLocations.Length; i++)
                { if(carLocations[i].idx == ourCoord.idx){ opposingOffset = carLocations[i].offset; break; } } //get the offset of the car in front of us

                //move away from the car in front of us
                if(opposingOffset > 0){ //if the car is on the right, move to the left
                    targetOffset = -55f; //move to the left
                    log += $"Left {targetOffset}"; //add to the log if we are moving to the left
                }else if(opposingOffset < 0){ //if the car is on the left, move to the right
                    targetOffset = 55f; //move to the right
                    log += $"Right {targetOffset}"; //add to the log if we are moving to the right
                }

            }else{ //move to the inside of the turn
                bool turnReversed = TrackGenerator.track.GetSegmentReversed(turnIndex + ourCoord.idx); //check if the next segment is reversed
                targetOffset = turnReversed ? 46f : -46f; //move to the inside of the turn
                targetSpeed = 500 + (onTurnNow ? 0 : 100);
                log += $"Inside Turn {targetOffset}"; //add to the log if we are moving to the inside of the turn
            }
        }else{
            targetSpeed = 800; //set the target speed to the max speed
            //if there are no turns in the next 3 segments, start Boosting
            if(futureTrack[0] != SegmentType.Turn && futureTrack[1] != SegmentType.Turn && futureTrack[2] != SegmentType.Turn){
                carController.Iboost = true; //set the boost to true
                log += "Boosting\n"; //add to the log if we are boosting
            }

            if(carClose){
                float opposingOffset = 0f;
                for (int i = 0; i < carLocations.Length; i++)
                { if(carLocations[i].idx == ourCoord.idx){ opposingOffset = carLocations[i].offset; break; } } //get the offset of the car in front of us
                //move away from the car in front of us
                if(opposingOffset > 0){ //if the car is on the right, move to the left
                    targetOffset = -55f; //move to the left
                    log += $"Left {targetOffset}"; //add to the log if we are moving to the left
                }else if(opposingOffset < 0){ //if the car is on the left, move to the right
                    targetOffset = 55f; //move to the right
                    log += $"Right {targetOffset}"; //add to the log if we are moving to the right
                }
            }
            else{ //if there are no cars close, set the target offset to 0
                targetOffset = 4f; //move to the center of the track
                log += $"Center {targetOffset}"; //add to the log if we are moving to the center of the track
                targetSpeed = 1000; //set the target speed to the max speed
            }
        }

        Debug = log; //add the log to the debug string
        currentTargetSpeed = targetSpeed; //set the target speed to the calculated target speed
        currentTargetOffset = targetOffset; //set the target offset to the calculated target offset
    }

    enum AIState{
        Speed, //Drive as fast as possible
        Target, //Drive to the target
        Persuit, //Stay close behind the target
        Defence, //Stay as far from other cars as possible
        Block, //drive in front of the target then slow down
    }


}
