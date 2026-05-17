using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public partial class CarController : MonoBehaviour
{
    // Fields owned by this partial
    [SerializeField] List<SpeedModifier> speedModifiers = new List<SpeedModifier>();
    ParticleSystem SlowVFX;
    bool slowVFXInitialized = false;
    Dictionary<CarStatus, float> statusList = new Dictionary<CarStatus, float>(); // status -> endTime
    float freezeStartTime; int freezeStartSpeed; float freezeTotalDuration;

#region SPEED MODIFIERS
    public void AddSpeedModifier(SpeedModifier mod){
        if(mod.ID != null){
            for(int i = 0; i < speedModifiers.Count; i++){ 
                if(speedModifiers[i].ID == mod.ID){ speedModifiers[i] = mod; return; }
            }
        }
        speedModifiers.Add(mod);
    }
    int GetSpeedAfterModifiers(int baseSpeed){
        int originalSpeed = baseSpeed;
        int speedModifier = 0;
        for(int i = 0; i < speedModifiers.Count; i++){
            if(speedModifiers[i].GetType() == typeof(FlatSpeedModifier))
            {
                speedModifier += ((FlatSpeedModifier)speedModifiers[i]).flatAmount;
            }
            if(speedModifiers[i].endTime <= Time.time){
                speedModifiers.RemoveAt(i);
                i--;
            }
        }
        baseSpeed += speedModifier;
        float percentModifier = 0;
        int totalPercentModifiers = 0;
        for(int i = 0; i < speedModifiers.Count; i++){
            if(speedModifiers[i].GetType() == typeof(PercentSpeedModifier))
            {
                percentModifier += ((PercentSpeedModifier)speedModifiers[i]).percentage;
                totalPercentModifiers++;
            }
            if(speedModifiers[i].endTime <= Time.time){
                speedModifiers.RemoveAt(i);
                i--;
            }
        }
        if(totalPercentModifiers > 0){
            percentModifier /= totalPercentModifiers;
            baseSpeed = Mathf.RoundToInt(baseSpeed * percentModifier);
        }
        if(baseSpeed < 0){ baseSpeed = 0; }
        
        // Update SlowVFX directly based on effective slow
        // If output = input, slow = 0; if output = 0, slow = 1
        float slowPercent = 0f;
        if(originalSpeed > 0){
            slowPercent = 1f - (float)baseSpeed / originalSpeed;
            slowPercent = Mathf.Clamp01(slowPercent);
        }
        UpdateSlowVFX(slowPercent);
        return baseSpeed;
    }
#endregion
#region STATUS EFFECTS
    /// <summary>
    /// Apply a status to the car for the given duration. If the status is already active, its duration is refreshed.
    /// </summary>
    public void SetStatusEffect(CarStatus status, float duration){
        statusList[status] = Time.time + duration;
        if(status == CarStatus.Meltdown) {
            if(duration > 0){ SR.gas?.SpawnMeltdownEffect(this, duration); } // Spawn meltdown visual effect
            else { SR.gas.ClearEffectForCar(GetID(), "Meltdown"); } // Clear effect if duration is 0 or negative
        }
        else if(status == CarStatus.Frozen) { //capture for 
            freezeStartTime = Time.time;
            freezeStartSpeed = speed;
            freezeTotalDuration = duration;
            if(duration > 0){ SR.gas?.SpawnFrozenEffect(this, duration); } // Spawn frozen visual effect
            else { SR.gas.ClearEffectForCar(GetID(), "Frozen"); } // Clear effect if duration is 0 or negative
        }
    }
    /// <summary>
    /// Returns true if the given status is currently active. Lazily removes expired entries.
    /// </summary>
    public bool GetStatusEffect(CarStatus status){
        if(statusList.TryGetValue(status, out float endTime)){
            if(endTime > Time.time) return true;
            statusList.Remove(status); // Lazily remove expired entry
        }
        return false;
    }
    float GetStatusEffectRemainingDuration(CarStatus status){
        if(statusList.TryGetValue(status, out float endTime))
            return Mathf.Max(0, endTime - Time.time);
        return 0f;
    }
#endregion
#region SLOW VFX CONTROL
    void InitSlowVFX()
    {
        if(string.IsNullOrEmpty(carID)) return;
        
        // Find the smoothed model transform and get the SlowVFX particle system
        Transform smoothedModel = SR.cet?.GetCarVisualTransform(carID);
        if(smoothedModel == null)
        {
            Debug.LogWarning($"[SlowVFX] Could not find smoothed model for car {carID}");
            slowVFXInitialized = false;
            SlowVFX = null;
            return;
        }
        
        // Look for particle system in children (not just on root)
        ParticleSystem newVFX = smoothedModel.GetComponentInChildren<ParticleSystem>();
        Debug.Log($"[SlowVFX] Init for car {carID}: found ParticleSystem={newVFX != null}, on object={(newVFX != null ? newVFX.gameObject.name : "null")}");
        
        // Check if it's a different particle system (car changed)
        if(newVFX != SlowVFX)
        {
            SlowVFX = newVFX;
            if(SlowVFX != null)
            {
                slowVFXInitialized = true;
                
                // Access emission module fresh and disable initially
                var emission = SlowVFX.emission;
                
                Debug.Log($"[SlowVFX] Initialized for {carID}");
            }
            else
            {
                slowVFXInitialized = false;
            }
        }
        else if(SlowVFX != null)
        {
            slowVFXInitialized = true;
        }
    }
    
    /// <summary>
    /// Update SlowVFX emission rate. Called from GetSpeedAfterModifiers.
    /// </summary>
    /// <param name="slowPercent">0 = no slow, 1 = fully stopped</param>
    void UpdateSlowVFX(float slowPercent)
    {
        // Check if particle system was destroyed or car changed
        if(SlowVFX == null || !slowVFXInitialized)
        {
            InitSlowVFX();
            if(!slowVFXInitialized || SlowVFX == null) return;
        }
        
        // Access modules fresh each time (they're structs that reference back to the system)
        var emission = SlowVFX.emission;
        
        // Locked/disabled takes priority - show no slow VFX
        if(GetStatusEffect(CarStatus.Locked)) {
            emission.rateOverTime = 0f;
            return;
        }
        
        float emissionRate = slowPercent * 100f;
        
        ParticleSystem.MinMaxCurve minMax = emission.rateOverTime;
        // Enable emission and set rate
        minMax.constant = emissionRate;
        emission.rateOverTime = minMax;
        if(emissionRate > 0) {
            //Debug.Log($"[SlowVFX] Car {carID}: rate={emissionRate:F1}, value = {SlowVFX.emission.rateOverTime.constant:F2}");
        }
    }
    
    /// <summary>
    /// Reset SlowVFX state when car changes or disconnects.
    /// </summary>
    void ResetSlowVFX()
    {
        if(SlowVFX != null && slowVFXInitialized)
        {
            var emission = SlowVFX.emission;
            emission.rateOverTime = 0f;
        }
        slowVFXInitialized = false;
        SlowVFX = null;
    }
#endregion
#region DISABLED STATUS
    /// <summary>
    /// Disable the car for 3.5 seconds (applies Locked status), recharge to 50% energy, and flash red engine light
    /// </summary>
    public void DisableCar() {   
        float duration = 3.5f;
        if(GetStatusEffect(CarStatus.Meltdown)) { 
            duration += 2f; // Meltdown cars are disabled for 5.5 seconds instead of 3.5
            SetStatusEffect(CarStatus.Meltdown, -1f); // Clear Meltdown (its done the damage)
        } 
        // Apply Locked status for 3.5 seconds
        SetStatusEffect(CarStatus.Locked, duration);
        
        // Track disable in analytics
        playerAnalytics?.RecordDisable();
        
        // Play disable sound effect
        SR.sfx?.PlaySFX(SFXEvent.CarDisabled);
        
        // Spawn world disabled visual effect
        SR.gas?.SpawnDisabled(this, duration);
        
        // Play announcer line if not busy (these lines shouldn't interrupt commentary)
        UCarData carData = SR.io?.GetCarFromID(carID);
        if(carData != null)
        {
            SR.pa?.PlayLineIfNotBusy(AudioAnnouncerManager.AnnouncerLine.CarReactorDisabled, carData.modelName);
        }
        StartCoroutine(DisableCarCoroutine());
    }
    
    IEnumerator DisableCarCoroutine()
    {
        // Apply 3.5s complete stop
        AddSpeedModifier(new FlatSpeedModifier(-5000, 3.5f, "Disabled"));
        SetTailEffect(LightEffect.THROB, 14, 1, 16, 3.5f, 99);
        SetHeadLights(new LightData[] {
            new LightData{ channel = LightChannel.FRONT_RED, effect = LightEffect.THROB, startStrength = 14, endStrength = 1, cyclesPer10Seconds = 16 },
            LightData.ClearFor(LightChannel.FRONT_GREEN)
        }, 3.5f, 99);
        
        // Start flashing red engine light
        LightData[] disabledEngine = new LightData[3]{
                new LightData{ channel = LightChannel.RED, effect = LightEffect.THROB, startStrength = 14, endStrength = 1, cyclesPer10Seconds = 8 },
                LightData.ClearFor(LightChannel.GREEN),
                LightData.ClearFor(LightChannel.BLUE)
            };
        SetEngineLight(disabledEngine, 3.5f, 99);
        
        // Recharge to 50% energy over the disable duration
        float startEnergy = energy;
        float targetEnergy = (maxEnergy + statMaxEnergyMod) * 0.5f;
        float duration = 3.5f;
        float elapsed = 0f;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            energy = Mathf.Lerp(startEnergy, targetEnergy, elapsed / duration);
            yield return new WaitForEndOfFrame();
        }
        
        energy = targetEnergy;
        
        // Clear all speed modifiers after disable wears off
        speedModifiers.Clear();
        UpdateSlowVFX(0f); // Reset VFX (Locked status has now expired)
    }
#endregion
}
public enum CarStatus
{
    //Untargetable and unaffected by damage
    Invulnerable = 0, 
    //Cannot change speed or lane
    Frozen = 1, 
    //steering controls are inverted
    Scrambled = 2,
    //Car cannot be controlled, will be overridden to 0 speed, cannot take damage or use items
    Locked = 3, 
    //requires an override to change speed, lane, or use items. (EG, override controls to AI for some abilites)
    Overridden = 4,
    // Car will take constant energy damage over time
    Meltdown = 5, 
    //Become immune to slowdown speed modifiers
    Unstoppable = 6, 
    // Cannot take fatal damage
    Immortal = 7, 
}
public class ActiveStatus
{
    public CarStatus status;
    public float endTime;
    public ActiveStatus(CarStatus status, float duration){
        this.status = status;
        this.endTime = Time.time + duration;
    }
}
public class SpeedModifier{
    public float endTime;
    public string ID;
}
public class PercentSpeedModifier : SpeedModifier{
    public float percentage;
    public PercentSpeedModifier(float percent, float duration, string ID){
        this.percentage = percent;
        this.endTime = Time.time + duration;
        this.ID = ID;
    }
}
public class FlatSpeedModifier : SpeedModifier{
    public int flatAmount;
    public FlatSpeedModifier(int amount, float duration, string ID){
        this.flatAmount = amount;
        this.endTime = Time.time + duration;
        this.ID = ID;
    }
}

