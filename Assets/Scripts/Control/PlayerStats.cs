using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks per-race statistics for a car. Stored as a variable in CarController.
/// Stats are reset at the start of each race and can be used for end screens or announcer dialogue.
/// </summary>
[System.Serializable]
public class PlayerStats
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
    
    // Recent damage tracking (last 2 seconds)
    [SerializeField] float recentDamageWindow = 2f;
    private List<(float time, float amount)> recentDamageDealt = new List<(float, float)>();
    
    // Threshold for big damage announcement
    const float BIG_DAMAGE_THRESHOLD = 100f;
    
    // Boost tracking
    private bool isBoosting = false;
    private float currentBoostStartTime;
    
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
        if(amount <= 0) return false;
        
        float previousRecentDamage = DamageDealtRecent;
        totalDamageDealt += amount;
        recentDamageDealt.Add((Time.time, amount));
        
        // Check if we just crossed the big damage threshold
        float currentRecentDamage = DamageDealtRecent;
        return previousRecentDamage < BIG_DAMAGE_THRESHOLD && currentRecentDamage >= BIG_DAMAGE_THRESHOLD;
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
    /// Call when the player starts boosting.
    /// </summary>
    public void StartBoost()
    {
        if(!isBoosting)
        {
            isBoosting = true;
            currentBoostStartTime = Time.time;
        }
    }
    
    /// <summary>
    /// Call when the player stops boosting.
    /// </summary>
    public void EndBoost()
    {
        if(isBoosting)
        {
            isBoosting = false;
            float boostDuration = Time.time - currentBoostStartTime;
            totalBoostTime += boostDuration;
            if(boostDuration > longestBoost)
            {
                longestBoost = boostDuration;
            }
        }
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
