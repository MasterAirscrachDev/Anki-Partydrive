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
            carInteraface = CarInteraface.io;
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
        //cms.TTS(lineupMessage);
        AudioAnnouncerManager.pa.PlayLine(LineupStarting);
        
        OnLineupStarted();
        carInteraface.OnLineupEvent += OnLineupUpdate;
        cms.SetPlayersRacingMode(true); // Set players to racing mode when lineup starts
    }
    
    /// <summary>
    /// Handles lineup progress updates from the server.
    /// </summary>
    public void OnLineupUpdate(string id, int remaining)
    {
        OnLineupProgress(id, remaining);
        
        if(remaining == 0){
            StartCoroutine(CountDown());
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
        
        OnCountdownStarted(activeCars);
        
        showText.text = "Get Ready!";
        AudioAnnouncerManager.pa.PlayLine(OnYourMarks);
        yield return new WaitForSeconds(3);
        
        showText.text = "3";
        AudioAnnouncerManager.pa.PlayLine(Count3);
        yield return new WaitForSeconds(1);
        
        showText.text = "2";
        AudioAnnouncerManager.pa.PlayLine(Count2);
        yield return new WaitForSeconds(1);
        
        showText.text = "1";
        AudioAnnouncerManager.pa.PlayLine(Count1);
        yield return new WaitForSeconds(1);
        
        showText.text = "GO!";
        AudioAnnouncerManager.pa.PlayLine(Go);
        cms.SetGlobalLock(false);
        gameActive = true;
        
        OnGameStarted(activeCars);
        
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
        
        showText.text = endMessage;
        cms.SetGlobalLock(true);
        cms.StopAllCars();
        cms.SetPlayersRacingMode(false); // Set players back to menu mode
        AudioAnnouncerManager.pa.PlayLine(line);
        
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
        
        OnCarCrossedFinish(carID, score);
    }
    
    /// <summary>
    /// Cleanup when game mode is disabled.
    /// </summary>
    protected virtual void CleanupGameMode()
    {
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
    protected virtual void OnCountdownStarted(string[] activeCars) { }
    
    /// <summary>
    /// Called when the game actually starts (after "GO!"). Override for game start logic.
    /// </summary>
    protected virtual void OnGameStarted(string[] activeCars) { }
    
    /// <summary>
    /// Called when a car crosses the finish line. Must be implemented by derived classes.
    /// </summary>
    protected abstract void OnCarCrossedFinish(string carID, bool score);
    
    /// <summary>
    /// Called when the game ends. Override for cleanup or final scoring.
    /// </summary>
    protected virtual void OnGameEnded() { }
    
    /// <summary>
    /// Called when the mode is being cleaned up. Override for mode-specific cleanup.
    /// </summary>
    protected virtual void OnModeCleanup() { }
}