using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class CarController : MonoBehaviour
{
#region FIELDS
    [SerializeField] int speed;
    [SerializeField] float lane;
    [SerializeField] string carID; // Currently connected car ID
    [SerializeField] string desiredCarID; // Car ID we want to connect to
    float energy = 75;
    int oldSpeed; float oldLane;
    bool wasBoostLastFrame = false;
    bool isSetup = false;
    [SerializeField] bool locked = true;
    public bool isAI = false;
    Color playerColor = Color.white;
    string playerName = "Player";
    Ability currentAbility = Ability.None; bool doingPickupAnim;
    CMS cms; //interface for gamemodes
    public PlayerCardSystem pcs; //used to update the UI
    CarsManagement carsManagement; //used when in the car selection screen
    CarEntityTracker carTracker; //used to get current position for dynamic width calculation
    List<SpeedModifer> speedModifiers = new List<SpeedModifer>();
    //INPUT VALUES======
    public float Iaccel, Isteer;
    public bool Iboost, IitemA, IitemB;
    //===================
    //BASE MODIFIERS======
    const float maxEnergy = 100;
    const int maxTargetSpeed = 750;
    const int minTargetSpeed = 50;
    const int baseBoostSpeed = 100;
    const float baseBoostCost = 0.5f;
    const float baseEnergyGain = 0.02f;
    const float baseSteering = 2f;
    public float statSpeedMod = 0f;
    public float statSteerMod = 0f;
    public float statBoostMod = 0f;
    public float statMaxEnergyMod = 0f;
    public float statEnergyRechargeMod = 0f;
    //===================
#endregion
#region INITIALIZATION & CONFIGURATION
    // Start is called before the first frame update
    public void Setup(bool isAI)
    {
        if(isSetup){ return; }
        this.isAI = isAI;
        isSetup = true;
        cms = FindFirstObjectByType<CMS>();
        cms.AddController(this, isAI);
        carTracker = FindFirstObjectByType<CarEntityTracker>(); // Cache the tracker reference
        ControlTicker(); //start the control ticker
        FindFirstObjectByType<PlayerCardmanager>().UpdateCardCount(); //this calls SetCard()

        int uiLayer = SR.ui.GetUILayer();
        if(uiLayer == 2){
            carsManagement = FindFirstObjectByType<CarsManagement>();
        }
    }
    public void SetColour(Color c){
        playerColor = c;
        if (!isAI)
        { GetComponent<PlayerController>().TrySetGamepadColor(c); }
        if(pcs == null){ return; }
        pcs.SetColor(c);
    }
    public void SetPlayerName(string name){
        playerName = name;
    }
    public void SetCard(PlayerCardSystem pcs){
        //Debug.Log("SetCard");
        this.pcs = pcs;
        UCarData carData = SR.io.GetCarFromID(carID);
        //Debug.Log($"SetCard: id {carID}, desired {desiredCarID}, cardata {carData != null}, pcs: {pcs != null}");
        string text = "Sitting Out";
        int model = -1;
        if(!string.IsNullOrEmpty(desiredCarID)){
            if(carData != null){ 
                text = carData.name; 
                model = (int)carData.modelName;
            } else {
                text = $"Waiting for {desiredCarID}";
            }
        }
        pcs.SetCarName(text, model);
        pcs.SetEnergy((int)energy, (int)maxEnergy);
        pcs.SetColor(playerColor);
    }
    public void SetStatModifiers(int speedModPoints, int steerModPoints, int boostModPoints, int maxEnergyModPoints, int energyRechargeModPoints){
        statSpeedMod = speedModPoints * 25f; // speed per point
        statSteerMod = steerModPoints * 0.4f; // steering per point 
        statBoostMod = boostModPoints * 5f; // speed per point
        statMaxEnergyMod = maxEnergyModPoints * 9f; // energy per point
        statEnergyRechargeMod = energyRechargeModPoints * 0.005f; // energycharge per point
    }
    public void AssignCarsManager(CarsManagement carsManagement){
        this.carsManagement = carsManagement;
        if(carsManagement != null){ 
            AIController ai = GetComponent<AIController>();
            if(ai != null){
                ai.EnteredCarManagement();
            }
        }
    }
#endregion
#region GAMEPLAY HELPERS
    public void AddSpeedModifier(int mod, bool isPercentage, float time, string ID = null){
        if(mod == 0){ return; }
        if(ID != null){
            for(int i = 0; i < speedModifiers.Count; i++){
                if(speedModifiers[i].ID == ID){
                    speedModifiers[i] = new SpeedModifer(mod, isPercentage, time, ID);
                    return;
                }
            }
        }
        speedModifiers.Add(new SpeedModifer(mod, isPercentage, time, ID));
    }
    public void UseEnergy(float amount, bool isDamage = true){
        energy -= amount;
        if(energy < 0){ 
            energy = 0;
            cms.OnCarOutOfEnergyCarCallback(carID, this); //call the event for no energy
        }
    }
    public void ChargeEnergy(float amount){
        energy += amount;
        if(energy > maxEnergy + statMaxEnergyMod){ energy = maxEnergy + statMaxEnergyMod; }
    }
    public void StopCar(){
        // Clear all inputs to stop any ongoing movement
        Iaccel = 0;
        Isteer = 0;
        Iboost = false;
        IitemA = false;
        IitemB = false;
        
        // Reset speed
        speed = 0;
        oldSpeed = 0;
        
        // Send stop command to the physical car
        UCarData carData = SR.io.GetCarFromID(carID);
        if(carData == null){ return; }
        
        // Send stop command multiple times to ensure it's received
        for(int i = 0; i < 3; i++)
        { SR.io.ControlCar(carData, 0, Mathf.RoundToInt(lane)); }
    }
#endregion
#region CONTROL TICKER
    async Task ControlTicker(){
        while(true){
            if(!Application.isPlaying){ return; }
            await Task.Delay(500); //approx 2 ticks per second
            
            // Check for car connection if we have a desired car but no current car
            CheckForCarConnection();
            
            if(!locked && (speed != oldSpeed || lane != oldLane)){
                oldLane = lane;
                oldSpeed = speed;
                UCarData carData = SR.io.GetCarFromID(carID);
                if(carData != null){
                    SR.io.ControlCar(carData, speed, Mathf.RoundToInt(lane));
                }
            }
        }
    }
    public void DoControlImmediate(){
        SR.io.ControlCar(SR.io.GetCarFromID(carID), speed, Mathf.RoundToInt(lane));
    }
    void FixedUpdate(){
        // Don't process any input or movement if the car is locked (game ended)
        if(locked){ return; }
        
        if(Iaccel > 0 && Iboost && energy > 1){
            UseEnergy(baseBoostCost, false);
            AddSpeedModifier(Mathf.RoundToInt(baseBoostSpeed * statBoostMod), false, 0.1f, "Boost");
        } else if(!Iboost && energy < maxEnergy){
            ChargeEnergy(baseEnergyGain * statEnergyRechargeMod);
        }

        int targetSpeed = (int)Mathf.Lerp(minTargetSpeed, maxTargetSpeed + statSpeedMod, Iaccel);
        speed = (int)Mathf.Lerp(speed, targetSpeed, (Iaccel == 0) ? 0.05f : 0.01f); //these 2 values are the deceleration and acceleration lerp speeds (to be experimented with)

        int speedModifier = 0;
        List<int> speedPercentModifierList = null;
        for(int i = 0; i < speedModifiers.Count; i++){
            speedModifiers[i].time -= Time.fixedDeltaTime;
            if(speedModifiers[i].isPercentage){
                if(speedPercentModifierList == null){ speedPercentModifierList = new List<int>(); }
                speedPercentModifierList.Add(speedModifiers[i].modifier);
            }else{
                speedModifier += speedModifiers[i].modifier;
            }
            if(speedModifiers[i].time <= 0){
                speedModifiers.RemoveAt(i);
                i--;
            }
        }
        speed += speedModifier;

        if(speedPercentModifierList != null){
            float percent = 1f;
            for(int i = 0; i < speedPercentModifierList.Count; i++){
                percent += speedPercentModifierList[i] / 100f;
            }
            percent /= speedPercentModifierList.Count;
            speed = Mathf.RoundToInt(speed * percent);
        }

        if(Iaccel == 0 && speed < 150){ speed = 0; } //cut speed to 0 if no input and slow speed
        else if(Iaccel > 0 && speed < 150){ speed = 150; } //snap to 150 if accelerating

        lane += Isteer * (baseSteering + statSteerMod);
        
        // Get dynamic track width from the car's actual current position
        float trackHalfWidth = GetTrackHalfWidth();
        lane = Mathf.Clamp(lane, -trackHalfWidth, trackHalfWidth); //clamp lane to track bounds
        pcs.SetEnergy((int)energy, (int)maxEnergy);
    }
    float GetTrackHalfWidth() {
        // Get dynamic track width from the car's actual current position
        float trackHalfWidth = 67.5f; // Default for modular tracks
        if (SR.track.hasTrack && !string.IsNullOrEmpty(carID)) {
            // Get the car's current track position from the tracking system
            if (carTracker != null) {
                TrackCoordinate currentPos = carTracker.GetCarTrackCoordinate(carID);
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
    void Update(){
        if(Iboost && !wasBoostLastFrame){
            if(carsManagement != null){
                carsManagement.NextCar(this);
            }
        }
        wasBoostLastFrame = Iboost;
    }
#endregion
#region CAR CONNECTION MANAGEMENT
    void CheckForCarConnection(){
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
            }
            return;
        }
        
        // Check if desired car is now available
        UCarData desiredCar = SR.io.GetCarFromID(desiredCarID);
        if(desiredCar != null){
            // Car is available, connect to it
            carID = desiredCarID;
            SR.pa.PlayLine(AudioAnnouncerManager.AnnouncerLine.CarSelected, desiredCar.modelName);
            Debug.Log($"Successfully connected to desired car: {carID}");
            FindFirstObjectByType<CarEntityTracker>().SetCarColorByID(carID, playerColor);
            if(pcs != null) pcs.SetCarName(desiredCar.name, (int)desiredCar.modelName);
        } else {
            // Car not available yet, update UI to show waiting status
            if(pcs != null) pcs.SetCarName($"Disconnected");
        }
    }
    
    public void CheckCarExists(){
        int idx = SR.io.GetCarIndex(carID);
        if(idx == -1){
            //Debug.LogError($"Car {carID} has disconnected!");
            carID = "";
            if(pcs != null) pcs.SetCarName("Disconnected");
        }
    }
    public void SetCar(UCarData data){
        if(data == null){
            carID = "";
            desiredCarID = "";
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
            FindFirstObjectByType<CarEntityTracker>().SetCarColorByID(carID, playerColor);
            if(pcs != null) pcs.SetCarName(data.name, (int)data.modelName);
            Debug.Log($"Immediately connected to car: {carID}");
        } else {
            // Car not available yet, will be checked in ControlTicker
            carID = "";
            if(pcs != null) pcs.SetCarName($"Disconnected");
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
            Debug.Log($"Immediately connected to car: {carID}");
        } else {
            // Car not available yet, will be checked in ControlTicker
            carID = "";
            if(pcs != null) pcs.SetCarName($"Disconnected");
            Debug.Log($"Car {id} not available yet, will wait for connection");
        }
    }
#endregion
#region PUBLIC GETTERS & SETTERS
    public (int, float) GetMetrics(){ return (speed, lane); }
    public string GetDesiredCarID(){ return desiredCarID; }
    public string GetPlayerName(){ return playerName; }
    public bool IsCarConnected(){ return !string.IsNullOrEmpty(carID) && carID == desiredCarID; }
    public void SetLocked(bool state){ 
        locked = state; 
        //if we also have a AIController, set the inputs locked to the same value
        AIController ai = GetComponent<AIController>();
        if(ai != null){
            ai.SetInputsLocked(state);
        }
        else if(!locked){
            DoControlImmediate(); //if we are unlocked, set the car to the current speed and lane
        }
    }
    /// <summary>
    /// Get the current connected car ID
    /// </summary>
    /// <returns>The Currently controlling car ID or an empty string</returns>
    public string GetID(){ return carID; }
    public Color GetPlayerColor(){ return playerColor; }
#endregion
#region UI UPDATE FUNCTIONS
    public void SetPosition(int position){ 
        if(pcs == null) {
            Debug.Log($"PCS was null in SetPosition for carID {carID}");
            FindFirstObjectByType<PlayerCardmanager>().UpdateCardCount(); //try to get the card again
        }
        pcs.SetPosition(position); }
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

    public void OnCollectElement(TrackCarCollider.EType type)
    {
        // Handle element collection logic here
        switch(type)
        {
            case TrackCarCollider.EType.EnergyCore:
                ChargeEnergy(25); // TEMP VALUSE
                break;
            case TrackCarCollider.EType.ItemBox:
                OnItembox();
                break;
            // Add cases for other element types as needed
            default:
                break;
        }
    }

    void OnItembox()
    {
        if(currentAbility != Ability.None || doingPickupAnim){ return; } //already have an ability
        else
        {
            doingPickupAnim = true;
            StartCoroutine(DoNewAbilityAnimation());
        }
    }
    void UseAbility()
    {
        if(currentAbility == Ability.None || doingPickupAnim){ return; } //no ability to use
        else
        {
            if(currentAbility == Ability.Missle3){ SR.gas.SpawnMissile(this); SetAbilityImmediate(Ability.Missle2); }
            else if(currentAbility == Ability.Missle2){ SR.gas.SpawnMissile(this); SetAbilityImmediate(Ability.Missle1); }
            else if(currentAbility == Ability.Missle1){ SR.gas.SpawnMissile(this); SetAbilityImmediate(Ability.None); }
            else if(currentAbility == Ability.MissleSeeking3){ SR.gas.SpawnSeekingMissile(this); SetAbilityImmediate(Ability.MissleSeeking2); }
            else if(currentAbility == Ability.MissleSeeking2){ SR.gas.SpawnSeekingMissile(this); SetAbilityImmediate(Ability.MissleSeeking1); }
            else if(currentAbility == Ability.MissleSeeking1){ SR.gas.SpawnSeekingMissile(this); SetAbilityImmediate(Ability.None); }
            else if(currentAbility == Ability.EMP){ SR.gas.SpawnEMP(this); SetAbilityImmediate(Ability.None); }
        }
    }
    IEnumerator DoNewAbilityAnimation()
    {
        Ability[] validAbilities = new Ability[] { Ability.Missle3, Ability.MissleSeeking3, Ability.EMP, Ability.Shield, Ability.TrailDamage, Ability.TrailSlow, Ability.CrasherBoost };
        //over 1 second, cycle through abilities every 0.1 second
        float animationDuration = 1f;
        float cycleInterval = 0.1f;
        float elapsed = 0f;
        while(elapsed < animationDuration)
        {
            Ability prospectiveAbility = validAbilities[Random.Range(0, validAbilities.Length)];
            //update UI here
            if(pcs != null)
            { pcs.SetAbilityIcon(prospectiveAbility); }
            yield return new WaitForSeconds(cycleInterval);
            elapsed += cycleInterval;
        }
        //final ability
        currentAbility = validAbilities[Random.Range(0, validAbilities.Length)];
        if(pcs != null)
        { pcs.SetAbilityIcon(currentAbility); }
        doingPickupAnim = false;
    }
    public void SetAbilityImmediate(Ability ability)
    {
        currentAbility = ability;
        if(pcs != null)
        { pcs.SetAbilityIcon(currentAbility); }
    }
    
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
class SpeedModifer{
    public int modifier;
    public bool isPercentage;
    public float time;
    public string ID;
    public SpeedModifer(int mod, bool isPercentage, float time, string ID){
        this.modifier = mod;
        this.isPercentage = isPercentage;
        this.time = time;
        this.ID = ID;
    }
}