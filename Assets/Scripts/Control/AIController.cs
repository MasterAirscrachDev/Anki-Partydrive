using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;
using static TrackPathSolver;

public class AIController : MonoBehaviour
{
    [SerializeField][TextArea(5, 10)] string Debug;
    CarController carController;
    [SerializeField] int depth = 3; //planning depth
    [SerializeField] float timeout = 0.6f; //timeout for planning
    [SerializeField] string setCarID = ""; //debugging variable to set the car ID
    [SerializeField] AIState state = AIState.Speed; //state of the AI
    TrackCoordinate[] carLocations; //list of other car locations
    List<TrackCoordinate> obstacles = new List<TrackCoordinate>(); //list of other obsticals
    TrackCoordinate ourCoord, currentTarget; //current target of the car (may be null)
    float timer = 0f; //timer for the AI logic

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
        string[] names = { "Jimmy [Bot]", "Bob [Bot]", "Doug [Bot]", "Gary [Bot]", "Jess [Bot]", "Sam [Bot]", "Kate [Bot]", "Dave [Bot]" };

        carController.SetPlayerName(names[Random.Range(0, names.Length)]); //set the player name to AI
        carController.SetColour(new Color(1, 0, 0)); //set the color to red

        setup = true; //set the setup variable to true
        FindFirstObjectByType<CarEntityTracker>().UpdateAIOpponentLocations(); //update the AI opponent locations
    }
    public void SetOpponentLocations(TrackCoordinate[] coords){
        carLocations = coords; //set the car locations to the given coordinates
    }
    public void SetOurLocation(TrackCoordinate coord){
        ourCoord = coord; //set our car location to the given coordinates
    }
    public void EnteredCarManagement(){
        if(!carController.IsCarConnected()){ //if car is not connected when entering
            string desiredID = carController.GetDesiredCarID();
            if(!string.IsNullOrEmpty(desiredID)){
                FindFirstObjectByType<CMS>().RemoveAI(desiredID); //remove this AI and Controller
                UnityEngine.Debug.Log($"Removing disconnected AI for car: {desiredID} when entering car management");
            }
        }
    }
    public void SetID(string id){ //Sets our desired Car ID
        if(!setup){ Start();}
        carController.SetDesiredCarID(id);
    }
    public string GetID() => carController.GetDesiredCarID(); //returns the desired ID of the car
    public void SetInputsLocked(bool locked){
        inputsLocked = locked; //set the inputs locked variable to the given value
        if(!locked){
            AILogic(); //if the inputs have just been unlocked, run the AI logic once
            UpdateInputs(); //update the inputs
            if(carController.IsCarConnected()){ //only send commands if car is connected
                carController.DoControlImmediate(); //update the car controller immediately
            }
        }
        else {
            // Immediately clear all inputs when locked
            carController.Iaccel = 0;
            carController.Isteer = 0;
            carController.Iboost = false;
            carController.IitemA = false;
            carController.IitemB = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!inputsLocked)
        {
            timer += Time.deltaTime;
            if (timer > timeout)
            {
                timer = 0f;
                AILogic();
            }
            UpdateInputs(); //update the inputs if they are not locked
        }
        else
        {
            carController.Iaccel = 0; //if the inputs are locked, set the acceleration to 0
            carController.Isteer = 0; //if the inputs are locked, set the steering to 0
            carController.Iboost = false; //if the inputs are locked, set the boost to false
            carController.IitemA = false; //if the inputs are locked, set the item A to false
            carController.IitemB = false; //if the inputs are locked, set the item B to false
        }
        if (setCarID != "")
        { //if we have a car ID set, set the car ID to the one we have set
            SetID(setCarID); setCarID = ""; //reset the car ID
        }
    }
    void UpdateInputs(){
        if (!carController.IsCarConnected()) { return; } //don't update inputs if car is not connected
        (int controllerSpeed, float controllerOffset) = carController.GetMetrics(); //get the current speed and offset of the car
        carController.Iaccel = currentTargetSpeed > controllerSpeed ? 1 : 0; //set the acceleration to 1 if we want to go faster, -1 if we want to go slower
        
        if(Mathf.Abs(currentTargetOffset - controllerOffset) < 2f){ //if we are close to the target offset, set the steering to 0
            carController.Isteer = 0;
        }else{
            carController.Isteer = Mathf.Clamp(currentTargetOffset - controllerOffset, -1f, 1f); //set the steering to the difference between the target offset and the current offset
        }
    }
    public void SetAIState(AIState newState){
        state = newState; //set the AI state to the given state
    }

    void AILogic()
    {
        if (carLocations == null || carLocations.Length == 0) { return; } //if there are no car locations, return
        if (ourCoord == null) { return; } //if our car location is null, return
        if (!carController.IsCarConnected()) { return; } //if our car is not connected, wait until it is
        if (!setup) { Start(); } //if the AI is not setup, setup the AI
        PathingInputs inputs = new PathingInputs
        {
            currentTargetSpeed = currentTargetSpeed,
            currentTargetOffset = currentTargetOffset,
            ourCoord = ourCoord,
            carLocations = carLocations.ToArray(),
            currentTarget = currentTarget,
            state = state,
            depth = depth
        };
        (int tSpd, float tOff, bool boost, string log) = GetBestPath(inputs);
        currentTargetSpeed = tSpd; //set the target speed to the given speed
        currentTargetOffset = tOff;
        carController.Iboost = boost;
        Debug = log; //set the debug string to the given log
    }
}
