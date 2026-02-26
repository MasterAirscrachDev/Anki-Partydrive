using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static OverdriveServer.NetStructures;
using static AudioAnnouncerManager.AnnouncerLine;

/// <summary>
/// Base class for all game modes. Handles common functionality like setup, countdown, lineup, and UI management.
/// </summary>
public abstract class GameMode : MonoBehaviour
{
    [Header("Game Mode UI")]
    [SerializeField] protected TMP_Text showText;
    [SerializeField] protected GameObject startButton, menuButton, replayButton;
    
    [Header("Game Mode Settings")]
    [SerializeField] protected string initialText = "Place cars on the track";
    [SerializeField] protected string lineupMessage = "Supercars to the starting line";
    
    // Core references
    protected CarEntityTracker carEntityTracker;
    protected CarInteraface carInteraface;
    protected CMS cms;
    protected UIManager uiManager;
    
    // Game state
    protected bool gameActive = false;
    protected bool gameEnding = false;
    
    // Overtake tracking
    protected Dictionary<string, int> previousPositions = new Dictionary<string, int>();
    
    void OnEnable()
    {
        InitializeReferences();
        StartMode();
    }
    
    void OnDisable()
    {
        CleanupGameMode();
    }
    
    /// <summary>
    /// Initialize core component references. Called automatically in OnEnable.
    /// </summary>
    protected virtual void InitializeReferences()
    {
        if(cms == null){
            cms = FindFirstObjectByType<CMS>();
            carInteraface = SR.io;
            carEntityTracker = FindFirstObjectByType<CarEntityTracker>();
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }
    
    /// <summary>
    /// Start the game mode. Sets up initial UI state and calls OnModeStart.
    /// </summary>
    protected virtual void StartMode()
    {
        showText.text = initialText;
        cms.SetGlobalLock(true);
        startButton.SetActive(true);
        menuButton.SetActive(true);
        replayButton.SetActive(false);
        gameActive = false;
        gameEnding = false;
        SR.tem.ClearElements();
        uiManager.SwitchToTrackCamera(true);
        
        OnModeStart();
    }
    
    /// <summary>
    /// Called when the player clicks the start button. Initiates lineup process.
    /// </summary>
    public void LineupAndStart()
    {
        uiManager.SwitchToTrackCamera(true);
        startButton.SetActive(false);
        FindFirstObjectByType<CarInteraface>().ApiCallV2(SV_LINEUP, 0);
        SR.pa.PlayLine(LineupStarting);
        SR.tem.ClearElements();
        OnLineupStarted();
        carInteraface.OnLineupEvent += OnLineupUpdate;
    }
    
    /// <summary>
    /// Handles lineup progress updates from the server.
    /// </summary>
    public void OnLineupUpdate(string id, int remaining)
    {
        OnLineupProgress(id, remaining);
        if(remaining == 0){
            StartCoroutine(CountDown());
            cms.SetPlayersRacingMode(true); // Set players to racing mode when lineup starts
            carInteraface.OnLineupEvent -= OnLineupUpdate;
        }
    }
    
    /// <summary>
    /// Standard countdown sequence. Can be overridden for custom countdown behavior.
    /// </summary>
    protected virtual IEnumerator CountDown()
    {
        uiManager.SwitchToTrackCamera(true);
        string[] activeCars = carEntityTracker.GetActiveCars();
        
        // Reset all cars before countdown
        foreach(string carID in activeCars){
            CarController controller = cms.GetController(carID);
            if(controller != null){
                controller.ResetCar();
            }
        }
        
        OnCountdownStarted(activeCars);
        
        showText.text = "Get Ready!";
        SR.pa.PlayLine(OnYourMarks);
        yield return new WaitForSeconds(3);
        
        showText.text = "3";
        SR.pa.PlayLine(Count3);
        yield return new WaitForSeconds(1);
        
        showText.text = "2";
        SR.pa.PlayLine(Count2);
        
        // Open perfect start timing window for all cars
        foreach(string carID in activeCars){
            CarController controller = cms.GetController(carID);
            if(controller != null){
                controller.OpenPerfectStartWindow();
            }
        }
        
        yield return new WaitForSeconds(1);
        
        showText.text = "1";
        SR.pa.PlayLine(Count1);
        
        // Close perfect start timing window
        foreach(string carID in activeCars){
            CarController controller = cms.GetController(carID);
            if(controller != null){
                controller.ClosePerfectStartWindow();
            }
        }
        
        yield return new WaitForSeconds(1);
        
        showText.text = "GO!";
        SR.pa.PlayLine(Go);
        cms.SetGlobalLock(false);
        gameActive = true;
        
        // Start live commentary
        SR.pa.StartLiveCommentary();
        
        OnGameStarted(activeCars);
        
        // Start position update ticker
        StartCoroutine(PositionUpdateTicker());
        
        yield return new WaitForSeconds(1);
        showText.text = "";
        
        StartCoroutine(StartListeningForFinishLine());
    }
    
    /// <summary>
    /// Starts listening for finish line events after a brief delay.
    /// </summary>
    protected virtual IEnumerator StartListeningForFinishLine()
    {
        yield return new WaitForSeconds(3);
        carEntityTracker.OnCarCrossedFinishLine += CarCrossedFinish;
    }
    

    
    /// <summary>
    /// Ends the game with the specified message.
    /// </summary>
    protected virtual void EndGame(string endMessage = "Game Over!", AudioAnnouncerManager.AnnouncerLine line = RaceOver)
    {
        if(gameEnding) return; // Prevent multiple calls
        gameEnding = true;
        gameActive = false;
        
        // Stop live commentary
        SR.pa.StopLiveCommentary();
        
        showText.text = endMessage;
        cms.SetGlobalLock(true);
        cms.StopAllCars();
        cms.SetPlayersRacingMode(false); // Set players back to menu mode
        SR.pa.PlayLine(line);
        
        carEntityTracker.OnCarCrossedFinishLine -= CarCrossedFinish;
        
        menuButton.SetActive(true);
        replayButton.SetActive(true);
        
        OnGameEnded();
    }
    
    /// <summary>
    /// Handles finish line crossings. Calls the abstract OnCarCrossedFinish method.
    /// </summary>
    public void CarCrossedFinish(string carID, bool score)
    {
        if(!gameActive) return;
        
        // Queue announcer line for lap completion
        if(score)
        {
            UCarData carData = SR.io?.GetCarFromID(carID);
            if(carData != null)
            {
                SR.pa?.QueueLine(AudioAnnouncerManager.AnnouncerLine.CarLapComplete, 6, carData.modelName);
            }
        }
        
        OnCarCrossedFinish(carID, score);
    }
    
    /// <summary>
    /// Cleanup when game mode is disabled.
    /// </summary>
    protected virtual void CleanupGameMode()
    {
        // Stop live commentary if still running
        if(SR.pa != null)
        {
            SR.pa.StopLiveCommentary();
        }
        
        if(carInteraface != null)
        {
            carInteraface.OnLineupEvent -= OnLineupUpdate;
        }
        
        if(carEntityTracker != null)
        {
            carEntityTracker.OnCarCrossedFinishLine -= CarCrossedFinish;
        }
        
        // Ensure players are set back to menu mode when gamemode is disabled
        if(cms != null)
        {
            cms.SetPlayersRacingMode(false);
        }
        
        OnModeCleanup();
    }
    
    /// <summary>
    /// Returns to the main menu. Called by UI buttons.
    /// </summary>
    public virtual void BackToMenu()
    {
        uiManager.SetUILayer(0);
    }
    
    /// <summary>
    /// Restarts the current game mode. Called by UI buttons.
    /// </summary>
    public virtual void Replay()
    {
        StartMode();
    }
    
    // Abstract and virtual methods for derived classes to implement/override
    
    /// <summary>
    /// Called when the mode starts. Override to initialize mode-specific data.
    /// </summary>
    protected virtual void OnModeStart() { }
    
    /// <summary>
    /// Called when lineup process begins. Override for mode-specific lineup logic.
    /// </summary>
    protected virtual void OnLineupStarted() { }
    
    /// <summary>
    /// Called during lineup progress updates. Override to handle lineup progress.
    /// </summary>
    protected virtual void OnLineupProgress(string carID, int remaining) { }
    
    /// <summary>
    /// Called when countdown starts. Override to initialize game-specific data.
    /// </summary>
    protected virtual void OnCountdownStarted(string[] activeCars)
    {
        previousPositions.Clear(); // Reset overtake tracking for new race
    }
    
    /// <summary>
    /// Called when the game actually starts (after "GO!"). Override for game start logic.
    /// </summary>
    protected virtual void OnGameStarted(string[] activeCars) { }
    
    /// <summary>
    /// Called when a car crosses the finish line. Must be implemented by derived classes.
    /// </summary>
    protected abstract void OnCarCrossedFinish(string carID, bool score);
    
    /// <summary>
    /// Updates positions for all active cars. Override this in derived classes for mode-specific position logic.
    /// Default implementation sorts by track position only.
    /// </summary>
    protected virtual void UpdatePositions()
    {
        // Get cars sorted by track position (first to last)
        List<string> sortedCars = carEntityTracker.GetSortedCarIDs();
        
        // Update position for each car and check for overtakes
        ApplyPositionsWithOvertakeDetection(sortedCars);
    }
    
    /// <summary>
    /// Applies positions to cars and detects overtakes. Call this from derived UpdatePositions methods.
    /// </summary>
    protected void ApplyPositionsWithOvertakeDetection(List<string> sortedCars)
    {
        int position = 1;
        foreach(string carID in sortedCars)
        {
            CarController controller = cms.GetController(carID);
            if(controller != null)
            {
                int previousPosition = previousPositions.ContainsKey(carID) ? previousPositions[carID] : position;
                controller.SetPosition(position);
                
                // Check for overtake (position improved = lower number)
                if(previousPosition > position && previousPositions.ContainsKey(carID))
                {
                    OnOvertakeDetected(carID, position, previousPosition);
                }
                
                previousPositions[carID] = position;
            }
            position++;
        }
    }
    
    /// <summary>
    /// Called when an overtake is detected. Override in derived classes to customize announcement behavior.
    /// </summary>
    protected virtual void OnOvertakeDetected(string carID, int newPosition, int oldPosition)
    {
        UCarData carData = SR.io?.GetCarFromID(carID);
        if(carData == null) return;
        
        if(newPosition == 1)
        {
            SR.pa?.QueueLine(AudioAnnouncerManager.AnnouncerLine.CarTakesLead, 10, carData.modelName);
        }
        else
        {
            SR.pa?.QueueLine(AudioAnnouncerManager.AnnouncerLine.CarOvertakes, 4, carData.modelName);
        }
    }
    
    /// <summary>
    /// Coroutine that updates positions every second during active gameplay.
    /// </summary>
    protected virtual IEnumerator PositionUpdateTicker()
    {
        while(gameActive)
        {
            UpdatePositions();
            yield return new WaitForSeconds(1f);
        }
    }
    
    /// <summary>
    /// Called when the game ends. Override for cleanup or final scoring.
    /// </summary>
    protected virtual void OnGameEnded()
    {
        //do a final position update to ensure correct final standings are shown at end of race
        UpdatePositions();
    }
    
    /// <summary>
    /// Called when the mode is being cleaned up. Override for mode-specific cleanup.
    /// </summary>
    protected virtual void OnModeCleanup() { }
}