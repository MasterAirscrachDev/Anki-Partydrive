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
    float abilityTimer = 0f; //timer for ability usage
    const float ABILITY_CHECK_INTERVAL = 1.5f; //how often to check if we should use an ability

    int currentTargetSpeed = 0;
    float currentTargetOffset = 0f;
    bool setup = false; //setup variable to check if the AI is setup
    bool inputsLocked = true;
    Coroutine perfectStartCoroutine = null;
    
    // Ability usage mapping table
    static readonly Dictionary<Ability, AIAbilityUsageMode> abilityUsageTable = new Dictionary<Ability, AIAbilityUsageMode>
    {
        { Ability.Missle1, AIAbilityUsageMode.Ahead },
        { Ability.Missle2, AIAbilityUsageMode.Ahead },
        { Ability.Missle3, AIAbilityUsageMode.Ahead },
        { Ability.MissleSeeking1, AIAbilityUsageMode.Ahead },
        { Ability.MissleSeeking2, AIAbilityUsageMode.Ahead },
        { Ability.MissleSeeking3, AIAbilityUsageMode.Ahead },
        { Ability.EMP, AIAbilityUsageMode.Close },
        { Ability.Recharger, AIAbilityUsageMode.Any },
        { Ability.TrailDamage, AIAbilityUsageMode.Behind },
        { Ability.TrailSlow, AIAbilityUsageMode.Behind },
        { Ability.Overdrive, AIAbilityUsageMode.Any },
        { Ability.CrasherBoost, AIAbilityUsageMode.Ahead },
        { Ability.OrbitalLazer, AIAbilityUsageMode.Any },
        { Ability.Grappler, AIAbilityUsageMode.Ahead },
        { Ability.LightningPower, AIAbilityUsageMode.Any },
        { Ability.TrafficCone, AIAbilityUsageMode.Any }
    };
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
            
            // Stop perfect start attempt when race begins
            if(perfectStartCoroutine != null){
                StopCoroutine(perfectStartCoroutine);
                perfectStartCoroutine = null;
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
    
    /// <summary>
    /// Start AI perfect start attempt with random timing and success rate
    /// </summary>
    public void TryPerfectStart(){
        if(perfectStartCoroutine != null){
            StopCoroutine(perfectStartCoroutine);
        }
        perfectStartCoroutine = StartCoroutine(PerfectStartAttempt());
    }
    
    IEnumerator PerfectStartAttempt(){
        // AI will press accelerate at a random time during the "2" window
        // 70% success rate - AI gets it right most of the time
        float successChance = 0.7f;
        
        if(Random.value < successChance){
            // Successful timing - press sometime during the 1 second window
            float pressDelay = Random.Range(0.1f, 0.9f);
            yield return new WaitForSeconds(pressDelay);
            
            // Press accelerate (this will be detected by CarController)
            carController.Iaccel = 1f;
        }
        // If failed, AI just won't press during the window
        
        perfectStartCoroutine = null;
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
            abilityTimer += Time.deltaTime;
            if (abilityTimer > ABILITY_CHECK_INTERVAL)
            {
                abilityTimer = 0f;
                CheckAndUseAbility();
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
        
        // Update AI state based on position
        UpdateAIStateBasedOnPosition();
        
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
    
    /// <summary>
    /// Update AI state based on our position relative to other cars
    /// Drive defensively when in lead, aggressively when behind
    /// </summary>
    void UpdateAIStateBasedOnPosition()
    {
        if (carLocations == null || carLocations.Length == 0)
        {
            state = AIState.Speed; // Default to speed if no other cars
            return;
        }
        
        int carsAhead = 0;
        int carsBehind = 0;
        
        foreach (var carLoc in carLocations)
        {
            if (carLoc.IsAhead(ourCoord))
            {
                carsAhead++;
            }
            else
            {
                carsBehind++;
            }
        }
        
        // Determine position (1st, 2nd, last, etc)
        int totalCars = carLocations.Length + 1; // +1 for ourselves
        int ourPosition = carsAhead + 1; // If 0 cars ahead, we're 1st
        
        // Choose state based on position
        if (ourPosition == 1)
        {
            // We're in first place - drive defensively
            state = carsBehind > 0 ? AIState.Defence : AIState.Speed;
        }
        else if (ourPosition == totalCars)
        {
            // We're in last place - drive as fast as possible
            state = AIState.Speed;
        }
        else if (carsAhead == 1)
        {
            // We're in 2nd place - pursue the leader
            state = AIState.Persuit;
        }
        else
        {
            // We're in the middle - use target mode for optimal racing
            state = AIState.Target;
        }
    }
    
    /// <summary>
    /// Check if the AI should use its current ability based on the ability usage mode
    /// </summary>
    void CheckAndUseAbility()
    {
        if (!carController.IsCarConnected()) { return; }
        
        // Get current ability from car controller (we'll need to add a getter for this)
        // For now, we'll trigger the ability input which will use it if available
        Ability currentAbility = GetCurrentAbility();
        if (currentAbility == Ability.None) { return; }
        
        // Check if we should use this ability based on its usage mode
        if (!abilityUsageTable.TryGetValue(currentAbility, out AIAbilityUsageMode usageMode))
        {
            usageMode = AIAbilityUsageMode.Any; // Default to any if not in table
        }
        
        bool shouldUse = ShouldUseAbility(usageMode);
        if (shouldUse)
        {
            carController.IitemA = true; // Trigger ability
            StartCoroutine(ResetAbilityInput());
        }
    }
    
    /// <summary>
    /// Determine if ability should be used based on the usage mode
    /// </summary>
    bool ShouldUseAbility(AIAbilityUsageMode mode)
    {
        if (carLocations == null || carLocations.Length == 0) { return false; }
        if (ourCoord == null) { return false; }
        
        switch (mode)
        {
            case AIAbilityUsageMode.Any:
                return true; // Always use
                
            case AIAbilityUsageMode.Close:
                // Use when any car is close (within 1.5 segments)
                foreach (var carLoc in carLocations)
                {
                    if (ourCoord.DistanceY(carLoc) < 1.5f) { return true; }
                }
                return false;
                
            case AIAbilityUsageMode.Ahead:
                // Use when there's a car ahead of us
                foreach (var carLoc in carLocations)
                {
                    if (carLoc.IsAhead(ourCoord) && ourCoord.DistanceY(carLoc) < 3f) { return true; }
                }
                return false;
                
            case AIAbilityUsageMode.Behind:
                // Use when there's a car behind us
                foreach (var carLoc in carLocations)
                {
                    if (ourCoord.IsAhead(carLoc) && ourCoord.DistanceY(carLoc) < 2f) { return true; }
                }
                return false;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Get the current ability from the car controller
    /// </summary>
    Ability GetCurrentAbility()
    {
        return carController.GetCurrentAbility();
    }
    
    /// <summary>
    /// Reset ability input after a short delay to simulate button press
    /// </summary>
    IEnumerator ResetAbilityInput()
    {
        yield return new WaitForSeconds(0.1f);
        carController.IitemA = false;
    }
}
