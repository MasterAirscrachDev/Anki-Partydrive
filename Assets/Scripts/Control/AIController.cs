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

    float timer = 0f;
    string ourID;

    int currentTargetSpeed = 0;
    float currentTargetOffset = 0f;
    // Start is called before the first frame update
    void Start()
    {
        carController = GetComponent<CarController>();
        carEntityTracker = FindObjectOfType<CarEntityTracker>();
        carController.statSteerMod = 2; //AIs have more steering power
    }
    public void SetID(string id){
        ourID = id;
        if(carController == null){
            carController = GetComponent<CarController>();
        }
        UCarData carData = CarInteraface.io.GetCarFromID(id);
        carController.SetID(carData);
    }

    // Update is called once per frame
    void Update()
    { //act on data here
        timer += Time.deltaTime;
        if(timer > timeout){
            timer = 0f;
            AILogic();
        }

        (int controllerSpeed, float controllerOffset) = carController.GetMetrics(); //get the current speed and offset of the car
        carController.Iaccel = currentTargetSpeed > controllerSpeed ? 1 : 0; //set the acceleration to 1 if we want to go faster, -1 if we want to go slower
        
        if(Mathf.Abs(currentTargetOffset - controllerOffset) < 2f){ //if we are close to the target offset, set the steering to 0
            carController.Isteer = 0;
        }else{
            carController.Isteer = Mathf.Clamp(currentTargetOffset - controllerOffset, -1f, 1f); //set the steering to the difference between the target offset and the current offset
        }

        if(setCarID != ""){ //if we have a car ID set, set the car ID to the one we have set
            SetID(setCarID);
            setCarID = ""; //reset the car ID
        }
    }

    void AILogic(){
        string[] trackedCars = carEntityTracker.GetActiveCars(ourID);
        (uint i, float x, float y)[] positions = new (uint, float, float)[trackedCars.Length];
        for (int c = 0; c < trackedCars.Length; c++)
        { positions[c] = carEntityTracker.GetCarIXY(trackedCars[c]); }
        (uint I, float X, float Y) = carEntityTracker.GetCarIXY(ourID);

        SegmentType[] futureTrack = new SegmentType[depth + 1]; //depth + 1 because we want to include the current segment
        for (int i = 0; i < futureTrack.Length; i++)
        { futureTrack[i] = TrackGenerator.track.GetSegmentType((int)I + i); }
        string log = "";
        for (int i = 0; i < futureTrack.Length; i++)
        { log += futureTrack[i] + " "; }

        bool carClose = positions.Any(x => x.i == I); //check if any car is on the same segment as us
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
                for (int i = 0; i < positions.Length; i++)
                { if(positions[i].i == I){ opposingOffset = positions[i].x; break; } } //get the offset of the car in front of us

                //move away from the car in front of us
                if(opposingOffset > 0){ //if the car is on the right, move to the left
                    targetOffset = -50f; //move to the left
                    log += $"Left {targetOffset}"; //add to the log if we are moving to the left
                }else if(opposingOffset < 0){ //if the car is on the left, move to the right
                    targetOffset = 50f; //move to the right
                    log += $"Right {targetOffset}"; //add to the log if we are moving to the right
                }

            }else{ //move to the inside of the turn
                bool turnReversed = TrackGenerator.track.GetSegmentReversed(turnIndex + (int)I); //check if the next segment is reversed
                targetOffset = turnReversed ? 45f : -45f; //move to the inside of the turn
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
                for (int i = 0; i < positions.Length; i++)
                { if(positions[i].i == I){ opposingOffset = positions[i].x; break; } } //get the offset of the car in front of us
                //move away from the car in front of us
                if(opposingOffset > 0){ //if the car is on the right, move to the left
                    targetOffset = -50f; //move to the left
                    log += $"Left {targetOffset}"; //add to the log if we are moving to the left
                }else if(opposingOffset < 0){ //if the car is on the left, move to the right
                    targetOffset = 50f; //move to the right
                    log += $"Right {targetOffset}"; //add to the log if we are moving to the right
                }
            }
            else{ //if there are no cars close, set the target offset to 0
                targetOffset = 0f; //move to the center of the track
                log += $"Center {targetOffset}"; //add to the log if we are moving to the center of the track
                targetSpeed = 1000; //set the target speed to the max speed
            }
        }

        Debug = log; //add the log to the debug string
        currentTargetSpeed = targetSpeed; //set the target speed to the calculated target speed
        currentTargetOffset = targetOffset; //set the target offset to the calculated target offset
    }
}
