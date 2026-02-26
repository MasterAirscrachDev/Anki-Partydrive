using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all 2D sound effects for the game (UI, powerups, abilities, etc.)
/// Access via SR.sfx.PlaySFX(SFXEvent)
/// </summary>
public class SFXManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] List<SFXEventData> sfxEvents = new List<SFXEventData>();
    
    void Start()
    {
        // Ensure audio source exists
        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if(audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        // Configure for 2D audio
        audioSource.spatialBlend = 0f;
    }
    
    public void OnSettingsChanged(SettingsState settings)
    {
        if(audioSource != null)
        {
            audioSource.volume = settings.sfxVolume;
        }
    }
    
    /// <summary>
    /// Play a sound effect by event type
    /// </summary>
    public void PlaySFX(SFXEvent sfxEvent)
    {
        if(audioSource == null) return;
        
        SFXEventData eventData = sfxEvents.Find(x => x.sfxEvent == sfxEvent);
        if(eventData == null)
        {
            Debug.LogWarning($"SFX event {sfxEvent} not found in sfxEvents list");
            return;
        }
        
        AudioClip clipToPlay = null;
        
        if(eventData.clips != null && eventData.clips.Length > 0)
        {
            clipToPlay = eventData.clips[UnityEngine.Random.Range(0, eventData.clips.Length)];
        }
        
        if(clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, eventData.volumeMultiplier);
        }
        else
        {
            Debug.LogWarning($"No audio clip found for SFX event {sfxEvent}");
        }
    }
}

/// <summary>
/// Enum for all SFX events in the game
/// </summary>
[Serializable]
public enum SFXEvent
{
    // UI Sounds
    UIButtonClick = 0,
    UIButtonHover = 1,
    UIMenuOpen = 2,
    UIMenuClose = 3,
    UIError = 4,
    UIConfirm = 5,
    
    // Ability Sounds
    AbilityPickup = 10,
    MissileLaunch = 20,
    Explosion = 21,
    SeekingMissileLaunch = 22,
    EMPActivate = 23,
    TrailDrop = 25,
    OrbitalLaserCharge = 26,
    OrbitalLaserFire = 27,
    CrasherBoostLaunch = 28,
    GrapplerAttach = 29,
    LightningStrike = 30,
    RechargerActivate = 31,
    TrafficConeHit = 32,
    
    // Car Sounds
    EnergyPickup = 40,
    CarDisabled = 41,
    CarRepaired = 42,
    PerfectStart = 43,
    
    // Race Sounds
    CountdownTick = 50,
    RaceStart = 51,
    RaceFinish = 53,
}

/// <summary>
/// Data class for SFX event configuration
/// </summary>
[Serializable]
public class SFXEventData
{
    public SFXEvent sfxEvent;
    [Tooltip("Array of audio clips - one will be randomly selected when played")]
    public AudioClip[] clips;
    [Range(0f, 2f)]
    public float volumeMultiplier = 1f;
}
