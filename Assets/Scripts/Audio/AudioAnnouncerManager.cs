using System;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AudioAnnouncerManager : MonoBehaviour
{
    public static AudioAnnouncerManager pa;
    
    [SerializeField] AudioSource audioSource;
    [SerializeField] List<AnnouncerLineData> announcerLines = new List<AnnouncerLineData>();
    
    void Start()
    {
        if(pa == null){
            pa = this;
        }
        
        // Ensure audio source exists
        if(audioSource == null){
            audioSource = GetComponent<AudioSource>();
            if(audioSource == null){
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// Play an announcer line with optional car model for car-specific lines
    /// </summary>
    public void PlayLine(AnnouncerLine line, ModelName carModel = ModelName.Unknown)
    {
        if(audioSource == null) return;
        
        // Find the announcer line data
        AnnouncerLineData lineData = announcerLines.Find(x => x.line == line);
        if(lineData == null){
            Debug.LogWarning($"Announcer line {line} not found in announcerLines list");
            return;
        }
        
        AudioClip clipToPlay = null;
        
        if(lineData.lineType == LineType.Unique){
            // Play random clip from unique clips array
            if(lineData.uniqueClips != null && lineData.uniqueClips.Length > 0){
                clipToPlay = lineData.uniqueClips[UnityEngine.Random.Range(0, lineData.uniqueClips.Length)];
            }
        }
        else if(lineData.lineType == LineType.CarSpecific){
            // Find the car-specific clips
            CarModelClips carClips = lineData.carSpecificClips.Find(x => x.carModel == carModel);
            if(carClips != null && carClips.clips != null && carClips.clips.Length > 0){
                clipToPlay = carClips.clips[UnityEngine.Random.Range(0, carClips.clips.Length)];
            }
            else{
                Debug.LogWarning($"No clips found for car model {carModel} on line {line}");
            }
        }
        
        if(clipToPlay != null){
            audioSource.PlayOneShot(clipToPlay);
        }
        else{
            Debug.LogWarning($"No audio clip found for announcer line {line}");
        }
    }
    
    [Serializable]
    public enum AnnouncerLine
    {
        LineupStarting = 0, OnYourMarks = 7, Go = 4,
        Count10 = 1, Count9 = 2, Count8 = 3, Count7 = 8, Count6 = 9, Count5 = 10, Count4 = 11, Count3 = 12, Count2 = 13, Count1 = 14,
        RemainingTime5Mins = 7, RemainingTime4Mins = 15, RemainingTime3Mins = 16, RemainingTime2Mins = 17, RemainingTime1Min = 18,
        CarLapComplete = 5, RaceOver = 6, TimesUp = 19
    }
    
    [Serializable]
    public enum LineType
    {
        Unique,        // Same audio for all situations (e.g., "Go!", "Race Over")
        CarSpecific    // Different audio per car model (e.g., car-specific lap complete messages)
    }
    
    [Serializable]
    public class AnnouncerLineData
    {
        public AnnouncerLine line;
        public LineType lineType = LineType.Unique;
        
        [Tooltip("Array of audio clips - one will be randomly selected when played")]
        public AudioClip[] uniqueClips;
        
        [Tooltip("Audio clips organized by car model")]
        public List<CarModelClips> carSpecificClips = new List<CarModelClips>();
    }
    
    [Serializable]
    public class CarModelClips
    {
        public ModelName carModel;
        [Tooltip("Array of audio clips for this car - one will be randomly selected")]
        public AudioClip[] clips;
    }
}
