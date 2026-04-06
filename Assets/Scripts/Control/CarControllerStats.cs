using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

// Partial CarController — base stat fields and modifiers configuration
public partial class CarController : MonoBehaviour
{
#region BASE STATS & MODIFIERS
    //BASE MODIFIERS======
    public const float maxEnergy = 100;
    public const int maxTargetSpeed = 750;
    public const int minTargetSpeed = 50;
    public const int baseBoostSpeed = 450; //100
    public const float baseBoostCost = 0.5f;
    public const float baseEnergyGain = 0.04f;
    public const float baseSteering = 2f;
    public float statSpeedMod = 0f;
    public float statSteerMod = 0f;
    public float statBoostMod = 0f;
    public float statMaxEnergyMod = 0f;
    public float statEnergyRechargeMod = 0f;
    //===================
    [SerializeField] CarControllerAnalytics playerAnalytics = new CarControllerAnalytics();

    public void SetStatModifiers(int speedModPoints, int steerModPoints, int boostModPoints, int maxEnergyModPoints, int energyRechargeModPoints){
        statSpeedMod = speedModPoints * 25f; // speed per point
        statSteerMod = steerModPoints * 0.4f; // steering per point 
        statBoostMod = boostModPoints * 5f; // speed per point
        statMaxEnergyMod = maxEnergyModPoints * 9f; // energy per point
        statEnergyRechargeMod = energyRechargeModPoints * 0.005f; // energycharge per point
    }
#endregion
}

/// <summary>
/// Tracks per-race analytics for a car. Stored as a variable in CarController.
/// Stats are reset at the start of each race and can be used for end screens or announcer dialogue.
/// </summary>
[System.Serializable]
public class CarControllerAnalytics
{
    [Header("Damage Stats")]
    [SerializeField] float totalDamageDealt;
    [SerializeField] float totalDamageTaken;
    
    [Header("Boost Stats")]
    [SerializeField] float longestBoost;
    [SerializeField] float totalBoostTime;
    
    [Header("Ability Stats")]
    [SerializeField] int totalAbilityPickups;
    [SerializeField] int totalDisables;
    
    // Recent damage tracking (last 5 seconds)
    [SerializeField] float recentDamageWindow = 5f;
    private List<(float time, float amount)> recentDamageDealt = new List<(float, float)>();
    
    // Threshold for big damage announcement
    const float BIG_DAMAGE_THRESHOLD = 60f;
    
    // Boost tracking
    bool isBoosting = false;
    float currentBoostDuration = 0f;

    
    #region Public Accessors
    public float TotalDamageDealt => totalDamageDealt;
    public float TotalDamageTaken => totalDamageTaken;
    public float LongestBoost => longestBoost;
    public float TotalBoostTime => totalBoostTime;
    public int TotalAbilityPickups => totalAbilityPickups;
    public int TotalDisables => totalDisables;
    
    /// <summary>
    /// Returns the damage dealt in the last recentDamageWindow seconds.
    /// </summary>
    public float DamageDealtRecent
    {
        get
        {
            CleanupRecentDamage();
            float total = 0f;
            foreach(var entry in recentDamageDealt)
            {
                total += entry.amount;
            }
            return total;
        }
    }
    #endregion
    
    #region Stat Recording Methods
    /// <summary>
    /// Record damage dealt to another car. Returns true if big damage threshold was just crossed.
    /// </summary>
    public bool RecordDamageDealt(float amount)
    {
        if(amount <= 0)
        {
            return false;
        }
        
        float previousRecentDamage = DamageDealtRecent;
        totalDamageDealt += amount;
        recentDamageDealt.Add((Time.time, amount));
        
        // Check if we just crossed the big damage threshold
        float currentRecentDamage = DamageDealtRecent;
        bool triggered = previousRecentDamage < BIG_DAMAGE_THRESHOLD && currentRecentDamage >= BIG_DAMAGE_THRESHOLD;
        //Debug.Log($"[BigDamage] PlayerStats: previous={previousRecentDamage:F1}, current={currentRecentDamage:F1}, threshold={BIG_DAMAGE_THRESHOLD}");
        return triggered;
    }
    
    /// <summary>
    /// Record damage taken from any source.
    /// </summary>
    public void RecordDamageTaken(float amount)
    {
        if(amount <= 0) return;
        totalDamageTaken += amount;
    }

    /// <summary>
    public void AddBoostTime(float duration)
    {
        totalBoostTime += duration;
        currentBoostDuration += duration;
        if(currentBoostDuration > longestBoost)
        { longestBoost = currentBoostDuration; }
    }

    public void ResetBoost()
    {
        currentBoostDuration = 0f;
    }
    
    /// <summary>
    /// Record an ability pickup.
    /// </summary>
    public void RecordAbilityPickup()
    {
        totalAbilityPickups++;
    }
    
    /// <summary>
    /// Record a disable (car ran out of energy and was disabled).
    /// </summary>
    public void RecordDisable()
    {
        totalDisables++;
    }
    
    /// <summary>
    /// Reset all stats. Call this at the start of a new race.
    /// </summary>
    public void ResetStats()
    {
        totalDamageDealt = 0f;
        totalDamageTaken = 0f;
        longestBoost = 0f;
        totalBoostTime = 0f;
        totalAbilityPickups = 0;
        totalDisables = 0;
        recentDamageDealt.Clear();
        isBoosting = false;
    }
    #endregion
    
    #region Private Helpers
    /// <summary>
    /// Remove damage entries older than recentDamageWindow.
    /// </summary>
    public void CleanupRecentDamage()
    {
        float cutoffTime = Time.time - recentDamageWindow;
        recentDamageDealt.RemoveAll(entry => entry.time < cutoffTime);
    }
    #endregion
}

