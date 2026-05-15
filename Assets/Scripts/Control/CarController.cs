using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public partial class CarController : MonoBehaviour
{
#region FIELDS
    [SerializeField] int speed;
    [SerializeField] float lane;
    [SerializeField] string carID; // Currently connected car ID
    [SerializeField] string desiredCarID; // Car ID we want to connect to
    bool isSetup = false;
    const float TICK_RATE = 0.4f; // Time in seconds between control ticks
    float energy = 75, lastEnergyDrainTime, canTickAt;
    int oldSpeed; float oldLane;
    bool wasBoostLastFrame = false, isRacing = false, isAI = false;
    Color playerColor = Color.white;
    string playerName = "Player";
    public PlayerCardSystem pcs; //used to update the UI
    // Perfect start timing
    bool perfectStartWindowOpen = false, acceleratedDuringPerfectWindow = false;
    //INPUT VALUES======
    InputFrame inputs;
    bool itemALastFrame = false, itemBLastFrame = false;
    //===================
#endregion
#region INITIALIZATION & CONFIGURATION
    // Start is called before the first frame update
    public void Setup(bool isAI, string playerName)
    {
        if(isSetup){ return; }
        this.playerName = playerName;
        this.isAI = isAI;
        isSetup = true;
        SR.cms.AddController(this, isAI); // CMS.AddController handles card count + colour
    }
    public void SetColour(Color c){
        playerColor = c;
        if(pcs == null){ return; }
        pcs.SetColor(c);
    }
    public void SetCard(PlayerCardSystem pcs){
        //Debug.Log("SetCard");
        this.pcs = pcs;
        string text = "Sitting Out";
        int model = -1;
        if(!string.IsNullOrEmpty(desiredCarID)){
            int idx = SR.io.GetCarIndex(desiredCarID);
            if(idx != -1){
                text = SR.io.cars[idx].name;
                model = (int)SR.io.cars[idx].modelName;
            } else {
                text = "Connecting...";
            }
        }
        pcs.SetCarName(text, model);
        pcs.SetEnergy((int)energy, (int)maxEnergy);
        pcs.SetColor(playerColor);
    }
    public void UpdateInputs(InputFrame newInputs, int type){
        if(GetStatusEffect(CarStatus.Frozen)){ return; } // Don't update inputs if frozen
        if(GetStatusEffect(CarStatus.Overridden) && type != 2){ return; } // Don't update inputs if overridden by an ability (except for special aim which is used by some abilities)
        inputs = newInputs;
    }
#endregion
#region GAMEPLAY HELPERS
    public void UseEnergy(float amount, bool isDamage = true){
        if(GetStatusEffect(CarStatus.Locked)){ return; } // Don't use energy if car is locked/disabled
        if(GetStatusEffect(CarStatus.Invulnerable) && isDamage){ return; } // Don't use energy for damage if car is invulnerable
        energy -= amount;
        lastEnergyDrainTime = Time.time; //used for delayed energy recharge
        // Track energy draining for taillight flash
        if(amount > 0 && isDamage){
            SetTailEffect(LightEffect.FLASH, 1, 7, 100, 0.8f, 2);
            playerAnalytics?.RecordDamageTaken(amount);
        }
        if(energy < 0){ 
            if(energy + amount > 0 && !GetStatusEffect(CarStatus.Immortal)) //cannot die from 0 energy while immortal
            { SR.cms.OnCarOutOfEnergyCarCallback(carID, this); } //call the event for no energy
            energy = 0;
        }
    }
    public void ChargeEnergy(float amount){
        energy += amount;
        if(energy > maxEnergy + statMaxEnergyMod){ energy = maxEnergy + statMaxEnergyMod; }
    }
    public void StopCar(){
        // Clear all inputs to stop any ongoing movement
        inputs = InputFrame.Empty();
        // Reset speed
        speed = 0; oldSpeed = 0;
        // Send stop command to the physical car
        UCarData carData = SR.io.GetCarFromID(carID);
        if(carData == null){ return; }
        // Send stop command multiple times to ensure it's received
        for(int i = 0; i < 3; i++)
        { SR.io.ControlCar(carData, 0, Mathf.RoundToInt(lane)); }
    }
    public void ResetCar() //should be called before a race starts
    {
        //energy reset to 75%
        energy = (maxEnergy + statMaxEnergyMod) * 0.75f;
        //clear speed modifiers
        speedModifiers.Clear();
        //reset ability
        currentAbility = Ability.None;
        if(pcs != null){ 
            pcs.ClearAttachment();
            pcs.SetPosition(0);
        }
        lights.Reset(); // Reset light timers
        
        // Reset perfect start tracking
        perfectStartWindowOpen = false;
        acceleratedDuringPerfectWindow = false;
        
        // Reset player analytics for new race
        playerAnalytics?.ResetStats();
    }
#endregion
#region CONTROL TICKER
    public void DoControlImmediate(){
        SR.io.ControlCar(SR.io.GetCarFromID(carID), speed, Mathf.RoundToInt(lane));
    }
    void FixedUpdate(){ //every 0.02 seconds, used for input tracking and control ticking
        // Track acceleration input during perfect start window (even before racing)
        if(!isRacing && perfectStartWindowOpen && inputs.accel > 0.5f){ acceleratedDuringPerfectWindow = true; }
        CheckAndClearLights();
        // Don't process any input or movement if not yet racing
        if(!isRacing){ 
            if(Time.time >= canTickAt){
                canTickAt = Time.time + TICK_RATE;       
                CheckForCarConnection(); // Still check for car connection while waiting to race
            }
            return;
        }
        
        if(inputs.accel > 0 && inputs.boost && energy > 1){//if we are accelerating and boosting and have energy, use boost
            UseEnergy(baseBoostCost, false);
            AddSpeedModifier(new FlatSpeedModifier(Mathf.RoundToInt(baseBoostSpeed + statBoostMod), TICK_RATE/2, "Boost"));
            playerAnalytics?.AddBoostTime(TICK_RATE/2);
        } else if(!inputs.boost && energy < maxEnergy){ //if we stopped boosting and have less than max energy, start recharging
            if(lastEnergyDrainTime + 0.75f < Time.time){  ChargeEnergy(baseEnergyGain + statEnergyRechargeMod); } // Only recharge if not recently drained
            playerAnalytics?.ResetBoost();
        }else{ playerAnalytics?.ResetBoost(); } // If we're not boosting, reset boost tracking for analytics

        if (GetStatusEffect(CarStatus.Meltdown)) { UseEnergy(0.25f);  } // Meltdown causes constant energy drain

        int targetSpeed = (int)Mathf.Lerp(minTargetSpeed, maxTargetSpeed + statSpeedMod, inputs.accel);
        bool frozen = GetStatusEffect(CarStatus.Frozen);
        if (frozen)
        {
            float frozenDuration = GetStatusEffectRemainingDuration(CarStatus.Frozen) - freezeStartTime;
            float t = Mathf.Clamp01(freezeTotalDuration / frozenDuration);
            speed = (int)(freezeStartSpeed * (1f - Mathf.Pow(t, 4.0f)));
        }
        else {
            speed = (int)Mathf.Lerp(speed, targetSpeed, (inputs.accel == 0) ? 0.021f : 0.019f); //these 2 values are the deceleration and acceleration lerp speeds (to be experimented with)
        }

        if(speed < 150){ speed = 0; } //cut speed to 0 if slow speed
        else if(inputs.accel > 0 && speed < 150){ speed = 150; } //snap to 150 if accelerating
        if (!frozen) { //if we are frozen we cannot steer at all, so skip lane changes
            if (GetStatusEffect(CarStatus.Scrambled)) { lane -= inputs.steer * (baseSteering + statSteerMod); }
            else { lane += inputs.steer * (baseSteering + statSteerMod); }
        }
        
        // Get dynamic track width from the car's actual current position
        float trackHalfWidth = GetTrackHalfWidth();
        lane = Mathf.Clamp(lane, -trackHalfWidth, trackHalfWidth); //clamp lane to track bounds
        pcs.SetEnergy((int)energy, (int)maxEnergy);

        //use ability inputs
        if(inputs.itemA && !itemALastFrame){ UseAbility(); }

        itemALastFrame = inputs.itemA;
        itemBLastFrame = inputs.itemB;
        // CONTROL TICKER ==============================================================================================
        if(canTickAt <= Time.time){
            canTickAt = Time.time + TICK_RATE; // Control tick
            // Check for car connection if we have a desired car but no current car
            CheckForCarConnection();
            int desiredSpeed = GetSpeedAfterModifiers(speed);
            
            if(isRacing && (desiredSpeed != oldSpeed || lane != oldLane)){
                oldLane = lane;
                oldSpeed = desiredSpeed;
                UCarData carData = SR.io.GetCarFromID(carID);
                if(carData != null){
                    SR.io.ControlCar(carData, desiredSpeed, Mathf.RoundToInt(lane));
                }
            }
            //if we have less than 25% energy, start flashing the taillights
            if(GetEnergyPercent() < 0.25f){
                float percentageOfQuarter = 1 - GetEnergyPercent() / 0.25f;
                int flashRate = Mathf.FloorToInt(Mathf.Lerp(2, 50, percentageOfQuarter)); // Flash faster as energy gets lower
                int maxFlashIntensity = Mathf.FloorToInt(Mathf.Lerp(7, 12, percentageOfQuarter)); // Flash more intensely as energy gets lower
                SetTailEffect(LightEffect.THROB, 3, maxFlashIntensity, flashRate, 3f);
            }
        }
    }
    public void ForceNextTick(){
        canTickAt = 0;
    }
    float GetTrackHalfWidth() {
        // Get dynamic track width from the car's actual current position
        float trackHalfWidth = 67.5f; // Default for modular tracks
        if (SR.track.hasTrack && !string.IsNullOrEmpty(carID)) {
            // Get the car's current track position from the tracking system
            if (SR.cet != null) {
                TrackCoordinate currentPos = SR.cet.GetCarTrackCoordinate(carID);
                if (currentPos != null) {
                    // Get the actual track spline this car is currently on
                    TrackSpline currentSpline = SR.track.GetTrackSpline(currentPos.idx);
                    if (currentSpline != null) {
                        // Use the car's current progression to get the precise width at this location
                        trackHalfWidth = currentSpline.GetWidth(currentPos.progression);
                    }
                }
            }
        }
        return trackHalfWidth;
    }
#endregion
#region CAR CONNECTION MANAGEMENT
    void CheckForCarConnection(){
        // Block reconnection while connection is suspended
        if(SR.cms != null && SR.cms.ConnectionSuspended) return;
        
        // If we don't have a desired car, nothing to check
        if(string.IsNullOrEmpty(desiredCarID)) return;
        
        // If we already have the desired car connected, nothing to check
        if(carID == desiredCarID && !string.IsNullOrEmpty(carID)) {
            // Double check the car still exists
            UCarData carData = SR.io.GetCarFromID(carID);
            if(carData == null){
                Debug.Log($"Car {carID} has disconnected, waiting for reconnection");
                carID = "";
                if(pcs != null) pcs.SetCarName("Disconnected");
                //ApplyBaseStats(carData);
            }
            return;
        }
        
        // Check if desired car is now CONNECTED
        UCarData desiredCar = SR.io.GetCarFromID(desiredCarID);
        if(desiredCar != null){
            // Car is CONNECTED — bind it
            carID = desiredCarID;
            SR.pa.PlayLine(AudioAnnouncerManager.AnnouncerLine.CarSelected, desiredCar.modelName);
            Debug.Log($"Successfully connected to desired car: {carID}");
            FindFirstObjectByType<CarEntityTracker>().SetCarColorByID(carID, playerColor);
            if(pcs != null) pcs.SetCarName(desiredCar.name, (int)desiredCar.modelName);
        } else {
            // Car not CONNECTED yet — show state-aware status
            if(pcs != null && !string.IsNullOrEmpty(desiredCarID)){
                int knownIdx = SR.io.GetCarIndex(desiredCarID);
                string stateLabel = knownIdx != -1 ? SR.io.cars[knownIdx].cState switch {
                    ConnectedState.CHARGING  => "Charging...",
                    ConnectedState.LOST      => "Reconnecting...",
                    ConnectedState.AVAILABLE => "Connecting...",
                    _ => "Disconnected"
                } : "Disconnected";
                pcs.SetCarName(stateLabel);
            }
        }
    }
    
    public void CheckCarExists(){
        if(string.IsNullOrEmpty(carID)) return;
        int idx = SR.io.GetCarIndex(carID);
        // Clear carID if the car is gone entirely OR is no longer in a commandable state
        if(idx == -1 || SR.io.cars[idx].cState != ConnectedState.CONNECTED){
            carID = "";
            if(pcs != null && !string.IsNullOrEmpty(desiredCarID)) pcs.SetCarName("Disconnected");
        }
    }
    public void ClearCarID(){ carID = ""; }
    public void SetCar(UCarData data){
        if(data == null){
            carID = "";
            desiredCarID = "";
            ResetSlowVFX();
            
            if(pcs != null) pcs.SetCarName("Sitting Out");
            return;
        }
        if(desiredCarID != data.id)
        {
            desiredCarID = data.id;
            SR.pa.PlayLine(AudioAnnouncerManager.AnnouncerLine.CarSelected, data.modelName);
        }
        
        
        // Try to connect immediately if car is available
        if(SR.io.GetCarFromID(data.id) != null){
            carID = data.id;
            ResetSlowVFX(); // Reset VFX for new car
            FindFirstObjectByType<CarEntityTracker>().SetCarColorByID(carID, playerColor);
            if(pcs != null) pcs.SetCarName(data.name, (int)data.modelName);
            ApplyBaseStats(data);
            Debug.Log($"Immediately connected to car: {carID}");
        } else {
            // Car not available yet, will be checked in ControlTicker
            carID = "";
            ResetSlowVFX();
            if(pcs != null && !string.IsNullOrEmpty(desiredCarID)) pcs.SetCarName($"Disconnected");
            Debug.Log($"Car {data.id} not available yet, will wait for connection");
        }
    }
    
    // Set desired car by ID (for AI controllers)
    public void SetDesiredCarID(string id){
        if(string.IsNullOrEmpty(id)){
            carID = "";
            desiredCarID = "";
            if(pcs != null) pcs.SetCarName("Sitting Out");
            return;
        }
        UCarData carData = SR.io.GetCarFromID(id);
        if(desiredCarID != id)
        {
            desiredCarID = id;
            if(carData != null)
            {
                SR.pa.PlayLine(AudioAnnouncerManager.AnnouncerLine.CarSelected, carData.modelName);
            }
        }
        
        // Try to connect immediately if car is available
        if(carData != null){
            carID = id;
            FindFirstObjectByType<CarEntityTracker>().SetCarColorByID(carID, playerColor);
            if(pcs != null) pcs.SetCarName(carData.name, (int)carData.modelName);
            ApplyBaseStats(carData);
            Debug.Log($"Immediately connected to car: {carID}");
        } else {
            // Car not available yet, will be checked in ControlTicker
            carID = "";
            if(pcs != null && !string.IsNullOrEmpty(desiredCarID)) pcs.SetCarName($"Disconnected");
            Debug.Log($"Car {id} not available yet, will wait for connection");
        }
    }
    void ApplyBaseStats(UCarData data)
    {
        OverdriveStatDefaults.StatTable statTable = OverdriveStatDefaults.GetDefaultsForCarBalanced(data.modelName);
        SetStatModifiers(statTable.speedModPoints, statTable.steerModPoints, statTable.boostModPoints, statTable.maxEnergyModPoints, statTable.energyRechargeModPoints);
    }
#endregion
#region PUBLIC GETTERS & SETTERS
    public (int, float) GetMetrics(){ return (speed, lane); }
    public string GetDesiredCarID(){ return desiredCarID; }
    public string GetPlayerName(){ return playerName; }
    public ModelName GetCarModel(){ 
        // Use desiredCarID for a state-agnostic lookup so model info is available even when LOST/CHARGING
        if(!string.IsNullOrEmpty(desiredCarID)){
            int idx = SR.io.GetCarIndex(desiredCarID);
            if(idx != -1) return SR.io.cars[idx].modelName;
        }
        return ModelName.Unknown;
    }
    public bool IsCarConnected(){ return !string.IsNullOrEmpty(carID) && carID == desiredCarID; }
    /// <summary>Returns true if this controller has a car assigned (any state). Use instead of IsCarConnected() when you only need to know if a car is assigned, not whether it's currently commandable.</summary>
    public bool HasDesiredCar(){ return !string.IsNullOrEmpty(desiredCarID); }
    public bool IsCarAI(){ return isAI; }
    public Ability GetCurrentAbility(){ return currentAbility; }
    public void SetLocked(bool state){ 
        isRacing = !state; 
        //ResetCar();
        
        // Apply perfect start boost when starting to race
        if(isRacing && acceleratedDuringPerfectWindow){
            ApplyPerfectStartBoost();
            acceleratedDuringPerfectWindow = false;
            perfectStartWindowOpen = false;
        }
        
        //if we also have a AIController, set the inputs locked to the same value
        AIController ai = GetComponent<AIController>();
        if(ai != null){
            ai.SetInputsLocked(state);
        }
        else if(isRacing){
            DoControlImmediate(); //if racing, set the car to the current speed and lane
        }
    }
    /// <summary>
    /// Get the current connected car ID
    /// </summary>
    /// <returns>The Currently controlling car ID or an empty string</returns>
    public string GetID(){ return carID; }
    public Color GetPlayerColor(){ return playerColor; }
    public float GetEnergyPercent(){ return energy / (maxEnergy + statMaxEnergyMod); }
    public CarControllerAnalytics GetPlayerAnalytics(){ return playerAnalytics; }
    
    /// <summary>
    /// Record damage dealt to another car (called by abilities when they hit targets)
    /// </summary>
    public void RecordDamageDealt(float amount) {
        bool triggeredBigDamage = playerAnalytics?.RecordDamageDealt(amount) ?? false;
        if(triggeredBigDamage) {
            UCarData carData = SR.io?.GetCarFromID(carID);
            if(carData != null) { SR.pa?.QueueLine(AudioAnnouncerManager.AnnouncerLine.CarDealsBigDamage, 4, carData.modelName); }
        }
    }
#endregion
#region PERFECT START
    /// <summary>
    /// Called by GameMode when countdown reaches "2" - opens the perfect start timing window
    /// </summary>
    public void OpenPerfectStartWindow(){
        perfectStartWindowOpen = true;
        acceleratedDuringPerfectWindow = false;
        
        // If this is an AI controller, trigger AI perfect start attempt
        if(isAI){
            AIController ai = GetComponent<AIController>();
            if(ai != null){
                ai.TryPerfectStart();
            }
        }
    }
    
    /// <summary>
    /// Called by GameMode when countdown moves past "2" - closes the perfect start timing window
    /// </summary>
    public void ClosePerfectStartWindow(){
        perfectStartWindowOpen = false;
    }

    void ApplyPerfectStartBoost(){
        // Apply a 3-second speed boost
        AddSpeedModifier(new FlatSpeedModifier(500, 3f, "PerfectStart"));
        Debug.Log($"Perfect Start! Car {carID} ({playerName})");
    }
#endregion
#region UI UPDATE FUNCTIONS
    int currentPosition = 0;
    
    public void SetPosition(int position){ 
        currentPosition = position;
        if(pcs == null) {
            Debug.Log($"PCS was null in SetPosition for carID {carID}");
            FindFirstObjectByType<PlayerCardmanager>().UpdateCardCount(); //try to get the card again
        }
        pcs.SetPosition(position); }
    
    public int GetPosition() { return currentPosition; }
    public void SetTimeTrialTime(float time){ 
        if(pcs == null) {
            Debug.Log($"PCS was null in SetTimeTrialTime for carID {carID}");
            FindFirstObjectByType<PlayerCardmanager>().UpdateCardCount(); //try to get the card again
        }
        pcs.SetTimeTrialTime(time); 
    }
    public void SetLapCount(int lapCount){ 
        if(pcs == null) {
            FindFirstObjectByType<PlayerCardmanager>().UpdateCardCount(); //try to get the card again
        }
        //Debug.Log($"Setting lap count to {lapCount} for carID {carID} pcs:{pcs != null}");
        pcs.SetLapCount(lapCount); 
    }
#endregion

    void OnDestroy(){
        // Only disconnect car if we're not in car selection mode
        // The CarSelector will handle disconnections when leaving the menu
        CarSelector selector = FindFirstObjectByType<CarSelector>();
        if(selector != null && selector.gameObject.activeInHierarchy){ return; }
        
        // Disconnect car when controller is destroyed (not in selection menu)
        string carToDisconnect = !string.IsNullOrEmpty(carID) ? carID : desiredCarID;
        if(!string.IsNullOrEmpty(carToDisconnect) && SR.io != null){
            SR.io.DisconnectCar(carToDisconnect);
            Debug.Log($"CarController destroyed - disconnecting car: {carToDisconnect}");
        }
    }
}
public class InputFrame
{
    public float accel, steer;
    public bool boost, itemA, itemB;
    public float specialAim; // -1 = backwards, 0 = none, 1 = forwards
    public InputFrame(float accel, float steer, bool boost, bool itemA, bool itemB, float specialAim){
        this.accel = accel; this.steer = steer; this.boost = boost; this.itemA = itemA; this.itemB = itemB; this.specialAim = specialAim;
    }
    public static InputFrame Empty(){ return new InputFrame(0,0,false,false,false,0); }
}