using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AIController : MonoBehaviour
{
    [SerializeField][TextArea(5, 10)] string Debug;
    CarController carController;
    [SerializeField] int depth = 3; //planning depth
    [SerializeField] float timeout = 0.6f; //timeout for planning
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
        string[] names = { "Jimmy Bot", "Bob Bot", "Doug Bot", "Gary Bot", "Jess Bot", "Sam Bot", "Kate Bot", "Dave Bot" };

        carController.SetPlayerName(names[Random.Range(0, names.Length)]); //set the player name to AI
        carController.SetColour(new Color(1, 0, 0)); //set the color to red

        setup = true; //set the setup variable to true
        FindObjectOfType<CarEntityTracker>().UpdateAIOpponentLocations(); //update the AI opponent locations
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
            SetID(setCarID); setCarID = ""; //reset the car ID
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
    public void SetAIState(AIState newState){
        state = newState; //set the AI state to the given state
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
        // Values to use in AI logic
        bool blockedAhead = false;
        bool blockedLeft = false;
        bool blockedRight = false;
        bool carClose = false;

        int targetSpeed = currentTargetSpeed;
        float targetLane = currentTargetOffset;
        carController.Iboost = false; //set the boost to false by default

        
        foreach (var carLoc in carLocations) {
            float YDistance = ourCoord.DistanceY(carLoc); //calculate the Y distance between us and the car
            float XDistance = ourCoord.DistanceX(carLoc); //calculate the X distance between us and the car
            bool carIsInOurLane = carLoc.IntersectsOffset(ourCoord); //check if the car other car occupies any part of our X space
            bool weAreAheadOf = carLoc.IsAhead(ourCoord); //check if we are ahead of the car 

            // Check if the car is ahead of us and in our lane
            if(YDistance < (1.5f * carLoc.BACK_DISTANCE)){ //dont worry about cars that are far away
                if(carIsInOurLane && !weAreAheadOf){ //if the car is in our lane and we are ahead of it, set blocked ahead to true
                    blockedAhead = true;
                }else if(XDistance < (carLoc.SIDE_DISTANCE + ourCoord.SIDE_DISTANCE) && !weAreAheadOf){ //if the car is in our lane and we are behind it, set blocked left or right to true depending on the offset of the car
                    if(carLoc.offset > ourCoord.offset){
                        blockedRight = true;
                    }else{
                        blockedLeft = true;
                    }
                }
                carClose = true; //set car close to true if the car is in our lane and we are ahead of it
            }
        }
        if(!blockedLeft && targetLane >= 55.25){
            blockedLeft = true; //if we are close to the left wall, set blocked left to true
        }
        if(!blockedRight && targetLane <= -55.25){
            blockedRight = true; //if we are close to the right wall, set blocked right to true
        }
        log += carClose ? "Car Close\n" : "No Car Close\n"; //add to the log if a car is close or not
        // Log blocked status
        if (blockedAhead) log += "Blocked Ahead!\n";
        if (blockedLeft) log += "Blocked Left!\n";
        if (blockedRight) log += "Blocked Right!\n";

        

        bool upcomingTurn = futureTrack[0] == SegmentType.Turn || futureTrack[1] == SegmentType.Turn;

        // Implement state-specific behavior
        switch (state) {
            case AIState.Speed:
                // Focus on maximum speed
                targetSpeed = 1000;
                
                if (blockedAhead) {
                    // Overtaking logic - try to pass blocked cars
                    targetSpeed = 800;
                    log += "Slowing down due to obstacle ahead\n";
                    
                    // Try to overtake - left by default, right if left is blocked
                    if (!blockedLeft) {
                        targetLane = currentTargetOffset - CAR_SPACING;
                        log += "Overtaking to the left\n";
                    } else if (!blockedRight) {
                        targetLane = currentTargetOffset + CAR_SPACING;
                        log += "Overtaking to the right\n";
                    } else {
                        targetLane = currentTargetOffset;
                        targetSpeed = 600; // Significant speed reduction if both sides blocked
                        log += "Can't overtake - both sides blocked\n";
                    }
                } else {
                    // No car blocking us - position for optimal racing line
                    if (futureTrack.Take(3).Any(s => s == SegmentType.Turn)) {
                        // There's a turn coming up - position for it
                        int turnIndex = 0;
                        for (int i = 0; i < 3 && i < futureTrack.Length; i++) {
                            if (futureTrack[i] == SegmentType.Turn) {
                                turnIndex = i;
                                break;
                            }
                        }
                        
                        bool turnReversed = TrackGenerator.track.GetSegmentReversed(turnIndex + ourCoord.idx);
                        // Inside of the turn (negative offset for left turn, positive for right turn)
                        targetLane = turnReversed ? 55.25f : -55.25f;
                        log += $"Positioning for upcoming turn at {targetLane}\n";
                    } else {
                        // No turn coming, stay toward center
                        targetLane = 4f; // Center of track
                        carController.Iboost = true;
                        log += "Boosting!\n";
                    }
                }
                break;
            case AIState.Target:
                // Find optimal path through track
                if (upcomingTurn) {
                    bool onTurnNow = futureTrack[0] == SegmentType.Turn;
                    int turnIndex = onTurnNow ? 0 : 1;
                    bool turnReversed = TrackGenerator.track.GetSegmentReversed(turnIndex + ourCoord.idx);
                    
                    // Take ideal racing line but adjust if blocked
                    float idealOffset = turnReversed ? 38.25f : -38.25f;
                    targetLane = idealOffset;
                    
                    // If ideal racing line is blocked, adjust
                    if ((idealOffset < 0 && blockedLeft) || (idealOffset > 0 && blockedRight)) {
                        targetLane = -idealOffset * 0.5f; // Use half of the opposite side
                        log += $"Adjusting racing line to avoid obstacle: {targetLane}\n";
                    }
                    
                    targetSpeed = 600 + (onTurnNow ? 0 : 150);
                    log += $"Target racing line: {targetLane}\n";
                } else {
                    targetLane = 4f;
                    targetSpeed = 850;
                }
                break;
                
            case AIState.Persuit:
                // Stay close behind target and look for overtaking opportunities
                if (currentTarget != null) {
                    // Calculate optimal overtaking position
                    float overtakingOffset;
                    
                    // Decide which side to overtake based on track position and available space
                    bool leftClear = !blockedLeft;
                    bool rightClear = !blockedRight;
                    
                    if (currentTarget.offset > 0) {
                        // If car ahead is on right side, prefer overtaking on left if clear
                        if (leftClear) {
                            overtakingOffset = currentTarget.offset - CAR_SPACING;
                            log += "Planning left overtake\n";
                        } else if (rightClear) {
                            // If left blocked but right clear, try wide right overtake
                            overtakingOffset = currentTarget.offset + CAR_SPACING;
                            log += "Planning wide right overtake (left blocked)\n";
                        } else {
                            // Both sides blocked, maintain distance
                            overtakingOffset = currentTarget.offset;
                            targetSpeed = 600; // Slow down
                            log += "Both sides blocked, maintaining position\n";
                        }
                    } else {
                        // If car ahead is on left side, prefer overtaking on right if clear
                        if (rightClear) {
                            overtakingOffset = currentTarget.offset + CAR_SPACING;
                            log += "Planning right overtake\n";
                        } else if (leftClear) {
                            // If right blocked but left clear, try wide left overtake
                            overtakingOffset = currentTarget.offset - CAR_SPACING;
                            log += "Planning wide left overtake (right blocked)\n";
                        } else {
                            // Both sides blocked, maintain distance
                            overtakingOffset = currentTarget.offset;
                            targetSpeed = 600; // Slow down
                            log += "Both sides blocked, maintaining position\n";
                        }
                    }
                    
                    // Clamp the overtaking position to valid track bounds
                    overtakingOffset = Mathf.Clamp(overtakingOffset, -72f, 72f);
                    
                    // If we're close enough and path isn't blocked, attempt overtaking
                    if (currentTarget.DistanceY(ourCoord) < 2.0f && !blockedAhead) {
                        targetLane = overtakingOffset;
                        targetSpeed = 1000; // Maximum speed for overtaking
                        if (!upcomingTurn) carController.Iboost = true;
                        log += $"Overtaking maneuver at offset {targetLane}\n";
                    } else {
                        // Not close enough to overtake or path blocked, just follow for now
                        targetLane = currentTarget.offset;
                        targetSpeed = blockedAhead ? 700 : 900; // Slow down if blocked
                        log += $"Following at distance {currentTarget.DistanceY(ourCoord)}\n";
                    }
                } else {
                    // No car ahead, revert to speed mode
                    state = AIState.Speed;
                }
                break;
                
            case AIState.Defence:
                // Find safest path away from other cars using CAR_SPACING
                if (carClose || blockedAhead) {
                    float opposingOffset = 0f;
                    
                    // Find the most immediate threat
                    if (carClose) {
                        for (int i = 0; i < carLocations.Length; i++) {
                            if (carLocations[i].idx == ourCoord.idx) {
                                opposingOffset = carLocations[i].offset;
                                break;
                            }
                        }
                    } else if (blockedAhead && currentTarget != null) {
                        opposingOffset = currentTarget.offset;
                    }
                    
                    // Choose the safest direction based on blocked status
                    if (opposingOffset > 0) {
                        // Threat is on the right, try to move left
                        if (!blockedLeft) {
                            targetLane = Mathf.Max(-72f, opposingOffset - CAR_SPACING);
                            log += $"Defensive move left {targetLane}\n";
                        } else if (!blockedRight) {
                            // Left is blocked, try moving right instead (wide turn)
                            targetLane = Mathf.Min(72f, opposingOffset + CAR_SPACING);
                            log += $"Defensive move right (wide) {targetLane}\n";
                        } else {
                            // Both sides blocked, slow down drastically
                            targetSpeed = 300;
                            log += "All directions blocked, emergency slow down!\n";
                        }
                    } else {
                        // Threat is on the left, try to move right
                        if (!blockedRight) {
                            targetLane = Mathf.Min(72f, opposingOffset + CAR_SPACING);
                            log += $"Defensive move right {targetLane}\n";
                        } else if (!blockedLeft) {
                            // Right is blocked, try moving left instead (wide turn)
                            targetLane = Mathf.Max(-72f, opposingOffset - CAR_SPACING);
                            log += $"Defensive move left (wide) {targetLane}\n";
                        } else {
                            // Both sides blocked, slow down drastically
                            targetSpeed = 300;
                            log += "All directions blocked, emergency slow down!\n";
                        }
                    }
                    
                    // Be cautious with speed in defensive mode
                    targetSpeed = Mathf.Min(targetSpeed, upcomingTurn ? 500 : 700);
                } else {
                    // Revert to target mode if no immediate threats
                    state = AIState.Target;
                }
                break;
                
            case AIState.Block:
                // Block overtaking attempts by staying in the same lane but offsetting slightly
                // Note: This is the only mode where we intentionally don't avoid other cars
                if (currentTarget != null && !currentTarget.IsAhead(ourCoord)) {
                    // Check if car behind is attempting to overtake
                    float behindOffset = currentTarget.offset;
                    bool possibleOvertakeAttempt = Mathf.Abs(behindOffset - ourCoord.offset) > (CAR_SPACING * 0.5f);
                    
                    if (possibleOvertakeAttempt) {
                        // Move in the direction they're trying to overtake
                        targetLane = behindOffset;
                        log += $"Blocking overtake attempt at {targetLane}\n";
                    } else {
                        // Just stay in their path
                        targetLane = behindOffset;
                        log += $"Blocking path at {targetLane}\n";
                    }
                    
                    // Moderate speed for blocking
                    targetSpeed = upcomingTurn ? 450 : 550;
                } else {
                    // If no suitable car to block, revert to speed mode
                    state = AIState.Speed;
                }
                break;
        }
        
        // Apply common avoidance logic for all states except Block
        // This provides a final safety check against collisions
        if (state != AIState.Block) {
            // Emergency avoidance if we're about to hit something head-on
            if (blockedAhead && targetSpeed > 600) {
                targetSpeed = 600;
                log += "Emergency speed reduction!\n";
            }
            
            // Final adjustment to avoid blocked sides in any state
            if (targetLane < currentTargetOffset && blockedLeft) {
                targetLane = currentTargetOffset;
            } else if (targetLane > currentTargetOffset && blockedRight) {
                targetLane = currentTargetOffset;
            }
        }
        // Apply turn-specific logic regardless of state
        if (upcomingTurn && state != AIState.Block) {
            bool onTurnNow = futureTrack[0] == SegmentType.Turn;
            
            // Always be more cautious in turns
            if (targetSpeed > 750) targetSpeed = 750;
            // If in turn currently, reduce speed further
            if (onTurnNow && targetSpeed > 600) targetSpeed = 600;
            // Further reduce speed in turns if obstacles present
            if ((blockedLeft || blockedRight) && targetSpeed > 500) {
                targetSpeed = 500;
                log += "Reducing speed for turn with obstacles\n";
            }
            
            // Ensure we're not boosting in turns
            carController.Iboost = false;
        }

        Debug = log; //add the log to the debug string
        currentTargetSpeed = targetSpeed; //set the target speed to the calculated target speed
        currentTargetOffset = targetLane; //set the target offset to the calculated target offset
    }

    public enum AIState{
        Speed, //Drive as fast as possible
        Target, //Drive to the target
        Persuit, //Stay close behind the target
        Defence, //Stay as far from other cars as possible
        Block, //drive in front of the target then slow down
    }
}
