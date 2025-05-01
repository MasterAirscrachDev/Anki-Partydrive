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

    //lane values may seem odd, they are based on the true lanes
    //{72.25f, 63.75f, 55.25f, 46.75f, 38.25f, 29.75f, 21.25f, 12.75f, 4.25f, -4.25f, -12.75f, -21.25f, -29.75f, -38.25f, -46.75f, -55.25f, -63.75f, -72.25f};

    void AILogic(){
        SegmentType[] futureTrack = new SegmentType[depth + 1]; //depth + 1 because we want to include the current segment
        for (int i = 0; i < futureTrack.Length; i++)
        { futureTrack[i] = TrackGenerator.track.GetSegmentType(ourCoord.idx + i); }
        
        string log = "";
        for (int i = 0; i < futureTrack.Length; i++)
        { log += futureTrack[i] + " "; }

        // Find closest cars and their positions relative to us
        TrackCoordinate closestCarAhead = null;
        TrackCoordinate closestCarBehind = null;
        float closestAheadDist = float.MaxValue;
        float closestBehindDist = float.MaxValue;
        // Values to use in AI logic
        bool blockedAhead = false;
        bool blockedLeft = false;
        bool blockedRight = false;

        
        foreach (var carLoc in carLocations) {
            float YDistance = ourCoord.DistanceY(carLoc);
            float XDistance = ourCoord.DistanceX(carLoc);
            bool weAreAheadOf = carLoc.IsAhead(ourCoord);

            if (YDistance < 4.5f) { //if the car is within 4.5 segments of us
                if (weAreAheadOf) {
                    // Car is behind us
                    if (XDistance < closestBehindDist) {
                        closestCarBehind = carLoc;
                        closestBehindDist = XDistance;
                    }
                } else {
                    // Car is ahead of us
                    if (XDistance < closestAheadDist) {
                        closestCarAhead = carLoc;
                        closestAheadDist = XDistance;
                    }
                    if(XDistance < 0.5f && XDistance < CAR_SPACING) { //if the car is within 0.5 segments of us and within CAR_SPACING
                        blockedAhead = true; //set the blocked ahead variable to true
                    }
                }
                if(YDistance < 0.2f){ //car is very close to us
                    if(XDistance < CAR_SPACING){ //car is very close to us
                        if(carLoc.offset > 0){ //car is on the right side
                            blockedRight = true; //set the blocked right variable to true
                        }else{
                            blockedLeft = true; //set the blocked left variable to true
                        }
                    }
                }
            }
        }
        
        bool carClose = carLocations.Any(x => x.idx == ourCoord.idx); //check if any car is on the same segment as us
        log += carClose ? "Car Close\n" : "No Car Close\n"; //add to the log if a car is close or not

        // Log blocked status
        if (blockedAhead) log += "Blocked Ahead!\n";
        if (blockedLeft) log += "Blocked Left!\n";
        if (blockedRight) log += "Blocked Right!\n";

        int targetSpeed = currentTargetSpeed;
        float targetOffset = currentTargetOffset;
        carController.Iboost = false; //set the boost to false by default

        bool upcomingTurn = futureTrack[0] == SegmentType.Turn || futureTrack[1] == SegmentType.Turn;
        
        // State transition logic
        // Adjust AI state based on race conditions
        if (blockedAhead && state != AIState.Defence) {
            // If path ahead is blocked, switch to defensive mode
            state = AIState.Defence;
            log += "State: Emergency defensive mode due to blocked path\n";
        }
        else if (closestCarAhead != null && closestAheadDist < 3) {
            // If someone is closely ahead, try to pursue or block them
            if (Random.value > 0.3f) {
                state = AIState.Persuit;
                log += "State: Pursuing car ahead\n";
            } else {
                state = AIState.Block;
                log += "State: Blocking car ahead\n";
            }
        } else if (carClose) {
            // If a car is very close on same segment, go defensive
            state = AIState.Defence;
            log += "State: Defensive mode\n";
        } else if (upcomingTurn && Random.value > 0.7f) {
            // Sometimes be cautious on turns
            state = AIState.Target;
            log += "State: Taking optimal path\n";
        } else if (!upcomingTurn && futureTrack.Take(3).All(s => s != SegmentType.Turn)) {
            // If we have a straight path, go for speed
            state = AIState.Speed;
            log += "State: Speed mode\n";
        }

        // Implement state-specific behavior
        switch (state) {
            case AIState.Speed:
                // Focus on maximum speed
                targetSpeed = 1000;
                targetOffset = 4f; // Center of track
                
                // If blocked ahead, reduce speed slightly to navigate safely
                if (blockedAhead) {
                    targetSpeed = 800;
                    log += "Slowing down due to obstacle ahead\n";
                }
                
                // Boost if we have a clear path ahead
                if (futureTrack.Take(3).All(s => s != SegmentType.Turn) && !carClose && !blockedAhead) {
                    carController.Iboost = true;
                    log += "Boosting!\n";
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
                    targetOffset = idealOffset;
                    
                    // If ideal racing line is blocked, adjust
                    if ((idealOffset < 0 && blockedLeft) || (idealOffset > 0 && blockedRight)) {
                        targetOffset = -idealOffset * 0.5f; // Use half of the opposite side
                        log += $"Adjusting racing line to avoid obstacle: {targetOffset}\n";
                    }
                    
                    targetSpeed = 600 + (onTurnNow ? 0 : 150);
                    log += $"Target racing line: {targetOffset}\n";
                } else {
                    targetOffset = 4f;
                    targetSpeed = 850;
                }
                break;
                
            case AIState.Persuit:
                // Stay close behind target and look for overtaking opportunities
                if (closestCarAhead != null) {
                    // Calculate optimal overtaking position
                    float overtakingOffset;
                    
                    // Decide which side to overtake based on track position and available space
                    bool leftClear = !blockedLeft;
                    bool rightClear = !blockedRight;
                    
                    if (closestCarAhead.offset > 0) {
                        // If car ahead is on right side, prefer overtaking on left if clear
                        if (leftClear) {
                            overtakingOffset = closestCarAhead.offset - CAR_SPACING;
                            log += "Planning left overtake\n";
                        } else if (rightClear) {
                            // If left blocked but right clear, try wide right overtake
                            overtakingOffset = closestCarAhead.offset + CAR_SPACING;
                            log += "Planning wide right overtake (left blocked)\n";
                        } else {
                            // Both sides blocked, maintain distance
                            overtakingOffset = closestCarAhead.offset;
                            targetSpeed = 600; // Slow down
                            log += "Both sides blocked, maintaining position\n";
                        }
                    } else {
                        // If car ahead is on left side, prefer overtaking on right if clear
                        if (rightClear) {
                            overtakingOffset = closestCarAhead.offset + CAR_SPACING;
                            log += "Planning right overtake\n";
                        } else if (leftClear) {
                            // If right blocked but left clear, try wide left overtake
                            overtakingOffset = closestCarAhead.offset - CAR_SPACING;
                            log += "Planning wide left overtake (right blocked)\n";
                        } else {
                            // Both sides blocked, maintain distance
                            overtakingOffset = closestCarAhead.offset;
                            targetSpeed = 600; // Slow down
                            log += "Both sides blocked, maintaining position\n";
                        }
                    }
                    
                    // Clamp the overtaking position to valid track bounds
                    overtakingOffset = Mathf.Clamp(overtakingOffset, -72f, 72f);
                    
                    // If we're close enough and path isn't blocked, attempt overtaking
                    if (closestAheadDist < 2.0f && !blockedAhead) {
                        targetOffset = overtakingOffset;
                        targetSpeed = 1000; // Maximum speed for overtaking
                        if (!upcomingTurn) carController.Iboost = true;
                        log += $"Overtaking maneuver at offset {targetOffset}\n";
                    } else {
                        // Not close enough to overtake or path blocked, just follow for now
                        targetOffset = closestCarAhead.offset;
                        targetSpeed = blockedAhead ? 700 : 900; // Slow down if blocked
                        log += $"Following at distance {closestAheadDist}\n";
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
                    } else if (blockedAhead && closestCarAhead != null) {
                        opposingOffset = closestCarAhead.offset;
                    }
                    
                    // Choose the safest direction based on blocked status
                    if (opposingOffset > 0) {
                        // Threat is on the right, try to move left
                        if (!blockedLeft) {
                            targetOffset = Mathf.Max(-72f, opposingOffset - CAR_SPACING);
                            log += $"Defensive move left {targetOffset}\n";
                        } else if (!blockedRight) {
                            // Left is blocked, try moving right instead (wide turn)
                            targetOffset = Mathf.Min(72f, opposingOffset + CAR_SPACING);
                            log += $"Defensive move right (wide) {targetOffset}\n";
                        } else {
                            // Both sides blocked, slow down drastically
                            targetSpeed = 300;
                            log += "All directions blocked, emergency slow down!\n";
                        }
                    } else {
                        // Threat is on the left, try to move right
                        if (!blockedRight) {
                            targetOffset = Mathf.Min(72f, opposingOffset + CAR_SPACING);
                            log += $"Defensive move right {targetOffset}\n";
                        } else if (!blockedLeft) {
                            // Right is blocked, try moving left instead (wide turn)
                            targetOffset = Mathf.Max(-72f, opposingOffset - CAR_SPACING);
                            log += $"Defensive move left (wide) {targetOffset}\n";
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
                if (closestCarBehind != null && closestBehindDist < 2) {
                    // Check if car behind is attempting to overtake
                    float behindOffset = closestCarBehind.offset;
                    bool possibleOvertakeAttempt = Mathf.Abs(behindOffset - ourCoord.offset) > (CAR_SPACING * 0.5f);
                    
                    if (possibleOvertakeAttempt) {
                        // Move in the direction they're trying to overtake
                        targetOffset = behindOffset;
                        log += $"Blocking overtake attempt at {targetOffset}\n";
                    } else {
                        // Just stay in their path
                        targetOffset = behindOffset;
                        log += $"Blocking path at {targetOffset}\n";
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
            if (targetOffset < 0 && blockedLeft) {
                float safeOffset = Mathf.Min(0f, targetOffset + CAR_SPACING * 0.75f);
                log += $"Final adjustment: avoiding left obstacle, changing offset from {targetOffset} to {safeOffset}\n";
                targetOffset = safeOffset;
            } else if (targetOffset > 0 && blockedRight) {
                float safeOffset = Mathf.Max(0f, targetOffset - CAR_SPACING * 0.75f);
                log += $"Final adjustment: avoiding right obstacle, changing offset from {targetOffset} to {safeOffset}\n";
                targetOffset = safeOffset;
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
