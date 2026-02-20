using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AudioAnnouncerManager : MonoBehaviour
{
    
    [SerializeField] AudioSource audioSource;
    [SerializeField] List<AnnouncerLineData> announcerLines = new List<AnnouncerLineData>();
    
    [Header("Live Commentary Settings")]
    [SerializeField] float minLineInterval = 7f;
    [SerializeField] float maxLineInterval = 9f;
    
    // Line queue system
    private List<QueuedLine> lineQueue = new List<QueuedLine>();
    private Coroutine liveCommentaryCoroutine;
    private bool liveCommentaryActive = false;
    
    // Track the last announced leader to prevent duplicate announcements
    private ModelName lastAnnouncedLeader = ModelName.Unknown;
    
    /// <summary>
    /// Represents a queued announcer line with importance and optional car model
    /// </summary>
    private class QueuedLine
    {
        public AnnouncerLine line;
        public int importance; // 1-10, higher = more important
        public ModelName carModel;
        public float queueTime;
        
        public QueuedLine(AnnouncerLine line, int importance, ModelName carModel = ModelName.Unknown)
        {
            this.line = line;
            this.importance = Mathf.Clamp(importance, 1, 10);
            this.carModel = carModel;
            this.queueTime = Time.time;
        }
    }
    
    void Start()
    {
        // Ensure audio source exists
        if(audioSource == null){
            audioSource = GetComponent<AudioSource>();
            if(audioSource == null){
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    public void OnSettingsChanged(SettingsState settings)
    {
        if(audioSource != null)
        {
            audioSource.volume = settings.announcerVolume;
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
        PlaceCarsOnTrack = 20, LineupStarting = 0, OnYourMarks = 7, Go = 4,
        Count10 = 1, Count9 = 2, Count8 = 3, Count7 = 8, Count6 = 9, Count5 = 10, Count4 = 11, Count3 = 12, Count2 = 13, Count1 = 14,
        RemainingTime5Mins = 7, RemainingTime4Mins = 15, RemainingTime3Mins = 16, RemainingTime2Mins = 17, RemainingTime1Min = 18,
        CarLapComplete = 5, RaceOver = 6, TimesUp = 19,
        CarOvertakes = 21, CarTakesLead = 22,
        TrackSmall = 23, TrackMedium = 24, TrackLarge = 25, TrackMatOval = 26, TrackMatBottleneck = 27, TrackMatCrossroads = 28,
        TrackHasJump = 29, TrackHasManyJumps = 30, TrackHasCrossroad = 31, TrackHasManyCrossroads = 32,
        RaceBanter = 33,
        CarReactorDisabled = 34, CarRepaired = 35,
        CarWins = 37, CarSelected = 40,
        CarGetsOverdriveAbility = 38, CarUsesOverdriveAbility = 39,
        VehicleOffTrack = 36, CarDealsBigDamage = 41,
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
    
    #region Live Commentary System
    
    /// <summary>
    /// Start live commentary during a race. Runs periodically and picks lines from queue.
    /// </summary>
    public void StartLiveCommentary()
    {
        if(liveCommentaryActive) return;
        
        liveCommentaryActive = true;
        lineQueue.Clear();
        lastAnnouncedLeader = ModelName.Unknown; // Reset leader tracking for new race
        liveCommentaryCoroutine = StartCoroutine(LiveCommentaryLoop());
    }
    
    /// <summary>
    /// Stop live commentary when race ends.
    /// </summary>
    public void StopLiveCommentary()
    {
        liveCommentaryActive = false;
        
        if(liveCommentaryCoroutine != null)
        {
            StopCoroutine(liveCommentaryCoroutine);
            liveCommentaryCoroutine = null;
        }
        
        lineQueue.Clear();
    }
    
    /// <summary>
    /// Add a line to the queue with importance (1-10) and optional car model.
    /// </summary>
    public void QueueLine(AnnouncerLine line, int importance, ModelName carModel = ModelName.Unknown)
    {
        if(!liveCommentaryActive) return;
        lineQueue.Add(new QueuedLine(line, importance, carModel));
    }
    
    /// <summary>
    /// Main loop that runs during live commentary.
    /// </summary>
    private IEnumerator LiveCommentaryLoop()
    {
        while(liveCommentaryActive)
        {
            float interval = UnityEngine.Random.Range(minLineInterval, maxLineInterval);
            yield return new WaitForSeconds(interval);
            
            if(liveCommentaryActive)
            {
                RunLine();
            }
        }
    }
    
    /// <summary>
    /// Pick and play a line from the queue. If empty, plays RaceBanter.
    /// Clears the queue after playing to prevent stale announcements.
    /// </summary>
    private void RunLine()
    {
        // Filter out duplicate CarTakesLead announcements for the same car
        lineQueue.RemoveAll(q => q.line == AnnouncerLine.CarTakesLead && q.carModel == lastAnnouncedLeader);
        
        QueuedLine selectedLine = null;
        
        if(lineQueue.Count == 0)
        {
            // Queue is empty, add banter
            selectedLine = new QueuedLine(AnnouncerLine.RaceBanter, 1);
        }
        else
        {
            // Select line with weighted randomness based on importance
            // Higher importance = more likely to be selected, but with some variance
            selectedLine = SelectLineWithRandomness();
        }
        
        if(selectedLine != null)
        {
            // Track who was announced as leader
            if(selectedLine.line == AnnouncerLine.CarTakesLead)
            {
                lastAnnouncedLeader = selectedLine.carModel;
            }
            
            PlayLine(selectedLine.line, selectedLine.carModel);
        }
        
        // Clear the queue after playing to ensure lines don't play long after they were requested
        lineQueue.Clear();
    }
    
    /// <summary>
    /// Select a line from queue with weighted randomness.
    /// Higher importance lines are more likely but not guaranteed.
    /// </summary>
    private QueuedLine SelectLineWithRandomness()
    {
        if(lineQueue.Count == 0) return null;
        if(lineQueue.Count == 1) return lineQueue[0];
        
        // Sort by importance descending
        lineQueue.Sort((a, b) => b.importance.CompareTo(a.importance));
        
        // Add randomness: weighted selection with importance as weight
        // Add a small random factor (0-3) to each importance to add variety
        float totalWeight = 0f;
        List<float> weights = new List<float>();
        
        foreach(var line in lineQueue)
        {
            float weight = line.importance + UnityEngine.Random.Range(0f, 3f);
            weights.Add(weight);
            totalWeight += weight;
        }
        
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        
        for(int i = 0; i < lineQueue.Count; i++)
        {
            cumulative += weights[i];
            if(randomValue <= cumulative)
            {
                return lineQueue[i];
            }
        }
        
        // Fallback to highest importance
        return lineQueue[0];
    }
    
    #endregion
}
