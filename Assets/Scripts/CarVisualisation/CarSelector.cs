using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static OverdriveServer.NetStructures;

public class CarSelector : MonoBehaviour
{
    //grid is 6x6
    [SerializeField] GameObject ModelCarPrefab, PlayerMarkerPrefab;
    [SerializeField] Transform slectionPlatform;
    List<PlayerController> players = new List<PlayerController>();
    List<GameObject> playerMarkers = new List<GameObject>();
    List<Vector2> markerPositions = new List<Vector2>(); // Grid positions (0-5, 0-5)
    List<uint> allModels = new List<uint>();
    List<string> allCarIDs = new List<string>(); // Track car IDs for selection
    List<CarSelectionData> allCarData = new List<CarSelectionData>(); // All car data for selection
    List<bool> markerLocked = new List<bool>(); // Track which markers are locked
    List<string> selectedCarIDs = new List<string>(); // Track which cars are selected by players
    List<string> aiSelectedCarIDs = new List<string>(); // Track which cars have AI
    List<GameObject> aiMarkers = new List<GameObject>(); // AI markers on the grid
    Dictionary<string, GameObject> aiMarkerDict = new Dictionary<string, GameObject>(); // Map car ID to AI marker
    Dictionary<string, Coroutine> connectionTimers = new Dictionary<string, Coroutine>(); // Track connection timers
    Vector2 platformSize;
    int gridSize = 6;
    float refreshTimer = 0;
    CMS cms;
    
    // Event triggered when all players have selected cars
    public System.Action OnAllPlayersSelected;
    
    void OnEnable(){
        slectionPlatform.gameObject.SetActive(true);
        
        // Get CMS reference
        cms = CMS.cms;
        
        // Get real car data from CarInterface
        RefreshCarData();
        
        // Connect to all available cars immediately for faster selection
        ConnectToAllAvailableCars();
        
        // Calculate platform size for marker movement
        platformSize = new Vector2(slectionPlatform.localScale.x, slectionPlatform.localScale.z);
        platformSize *= 0.82f;
        platformSize.x *= 0.86f;
        
        SpawnCars();
        RefreshPlayers();
        InitializePlayerMarkers();
        DespawnDisconnectedAIs();

        // Restore markers for controllers that already have active cars
        RestoreExistingCarSelections();
        
        // Subscribe to events with delay to avoid accidental selections on page load
        StartCoroutine(SubscribeToEventsDelayed());
    }
    void DespawnDisconnectedAIs(){
        AIController[] ais = FindObjectsOfType<AIController>();
        foreach(AIController ai in ais){
            if(!ai.GetComponent<CarController>().IsCarConnected()){
                string desiredID = ai.GetID();
                if(!string.IsNullOrEmpty(desiredID)){
                    cms.RemoveAI(desiredID); //remove this AI and Controller
                    Debug.Log($"Removing disconnected AI for car: {desiredID} on Car Selector enable");
                }
            }
        }
    }
    
    IEnumerator SubscribeToEventsDelayed(){
        yield return new WaitForSeconds(0.5f);
        if(cms != null){
            cms.onSelect += OnPlayerSelectPressed;
            cms.onAltSelect += OnPlayerAltSelectPressed;
            cms.onBackToMenu += OnPlayerBackPressed;
            cms.onStartSelect += OnPlayerStartPressed;
        }
    }
    void RefreshPlayers(){
        players.Clear();
        PlayerController[] pcs = FindObjectsOfType<PlayerController>();
        foreach(PlayerController pc in pcs){
            if(pc.enabled){
                players.Add(pc);
            }
        }
    }
    void OnDisable(){
        slectionPlatform.gameObject.SetActive(false);
        
        // Disconnect cars that don't have matching controllers
        DisconnectUnusedCars();
        
        // Unsubscribe from CMS events
        if(cms != null){
            cms.onSelect -= OnPlayerSelectPressed;
            cms.onAltSelect -= OnPlayerAltSelectPressed;
            cms.onBackToMenu -= OnPlayerBackPressed;
            cms.onStartSelect -= OnPlayerStartPressed;
        }
        
        // Clean up player markers
        foreach(GameObject marker in playerMarkers){
            if(marker != null){
                DestroyImmediate(marker);
            }
        }
        // Clean up AI markers
        foreach(GameObject aiMarker in aiMarkers){
            if(aiMarker != null){
                DestroyImmediate(aiMarker);
            }
        }
        
        // Clean up connection timers
        foreach(var timer in connectionTimers.Values){
            if(timer != null){
                StopCoroutine(timer);
            }
        }
        
        playerMarkers.Clear();
        markerPositions.Clear();
        markerLocked.Clear();
        selectedCarIDs.Clear();
        aiSelectedCarIDs.Clear();
        aiMarkers.Clear();
        aiMarkerDict.Clear();
        connectionTimers.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        RefreshPlayers();
        UpdatePlayerMarkers();
        refreshTimer += Time.deltaTime;
        if(refreshTimer > 5f){ //every 5 seconds
            refreshTimer = 0;
            RefreshCarsDisplay(); //refresh car data and respawn cars (this will also reposition markers)
        }
    }
    void SpawnCars(){
        // Clear any existing cars
        foreach(Transform child in slectionPlatform){
            if(child != slectionPlatform && child.GetComponent<CarModelManager>() != null){
                Destroy(child.gameObject);
            }
        }
        
        //spawn cars in a grid using real car data
        Vector2 size = new Vector2(slectionPlatform.localScale.x, slectionPlatform.localScale.z);
        size *= 0.82f;
        size.x *= 0.86f;
        
        int totalCars = allCarData.Count;
        
        // Calculate optimal grid dimensions for centering with proper grid snapping
        int carsPerRow = Mathf.Min(6, Mathf.CeilToInt(Mathf.Sqrt(totalCars)));
        int rows = Mathf.CeilToInt((float)totalCars / carsPerRow);
        
        // Calculate centering offsets (rounded to ensure grid snapping)
        int rowOffset = Mathf.RoundToInt((6 - rows) * 0.5f);
        
        int carIndex = 0;
        for(int row = 0; row < rows && carIndex < totalCars; row++){
            int carsInThisRow = Mathf.Min(carsPerRow, totalCars - carIndex);
            int colOffset = Mathf.RoundToInt((6 - carsInThisRow) * 0.5f);
            
            for(int col = 0; col < carsInThisRow; col++){
                int gridX = col + colOffset;
                int gridY = row + rowOffset;
                
                // Ensure we stay within grid bounds
                gridX = Mathf.Clamp(gridX, 0, 5);
                gridY = Mathf.Clamp(gridY, 0, 5);
                
                Vector3 pos = new Vector3(
                    slectionPlatform.position.x - size.x/2 + size.x/5 * gridX, 
                    slectionPlatform.position.y, 
                    slectionPlatform.position.z - size.y/2 + size.y/5 * gridY
                );
                GameObject car = Instantiate(ModelCarPrefab, pos, Quaternion.identity);
                
                CarSelectionData carData = allCarData[carIndex];
                car.GetComponent<CarModelManager>().Setup((int)carData.model, carData.isConnected ? Color.green : Color.yellow);
                car.transform.parent = slectionPlatform;
                
                carIndex++;
            }
        }
    }
    
    void RefreshCarData(){
        allCarData.Clear();
        allModels.Clear();
        allCarIDs.Clear();
        
        // Get car interface
        CarInteraface carInterface = CarInteraface.io;
        if(carInterface == null) return;
        
        // Refresh available cars from server
        carInterface.RefreshAvailableCars();
        
        // Get all cars for selection
        allCarData = carInterface.GetAllCarsForSelection();
        
        // Update legacy lists for compatibility
        foreach(CarSelectionData carData in allCarData){
            allModels.Add(carData.model);
            allCarIDs.Add(carData.id);
        }
    }
    
    void InitializePlayerMarkers(){
        // Create initial markers for existing players
        for(int i = 0; i < players.Count; i++){
            CreatePlayerMarker(i);
        }
    }
    
    void RestoreExistingCarSelections(){
        if(cms == null || cms.controllers == null) return;
        
        // Check each controller in CMS to see if it has an active car
        foreach(CarController controller in cms.controllers){
            string desiredCarID = controller.GetDesiredCarID();
            if(string.IsNullOrEmpty(desiredCarID)) continue;
            
            // Check if this is an AI controller
            if(controller.GetComponent<AIController>() != null){
                // Handle AI car restoration
                RestoreAICarSelection(desiredCarID);
            } else {
                // Handle player car restoration
                RestorePlayerCarSelection(controller, desiredCarID);
            }
        }
    }
    
    void RestorePlayerCarSelection(CarController controller, string carID){
        // Find which player index this controller corresponds to
        int playerIndex = -1;
        for(int i = 0; i < players.Count; i++){
            if(players[i] != null && players[i].GetComponent<CarController>() == controller){
                playerIndex = i;
                break;
            }
        }
        
        if(playerIndex == -1) return; // Controller not found in current players
        
        // Find the car in our car data
        int carIndex = FindCarIndex(carID);
        if(carIndex == -1) return; // Car not found in current data
        
        // Calculate grid position for this car
        Vector2 carGridPos = GetGridPositionForCarIndex(carIndex);
        if(carGridPos.x < 0) return; // Invalid position
        
        // Update player marker to be locked on this car
        selectedCarIDs[playerIndex] = carID;
        markerLocked[playerIndex] = true;
        markerPositions[playerIndex] = carGridPos;
        
        // Update marker world position
        if(playerIndex < playerMarkers.Count && playerMarkers[playerIndex] != null){
            UpdateMarkerWorldPosition(playerMarkers[playerIndex], carGridPos);
        }
        
       // Debug.Log($"Restored player {playerIndex + 1} selection on car: {carID} at grid ({carGridPos.x}, {carGridPos.y})");
    }
    
    void RestoreAICarSelection(string carID){
        // Find the car in our car data
        int carIndex = FindCarIndex(carID);
        if(carIndex == -1) return; // Car not found in current data
        
        // Calculate grid position for this car
        Vector2 carGridPos = GetGridPositionForCarIndex(carIndex);
        if(carGridPos.x < 0) return; // Invalid position
        
        // Mark car as AI-selected and create/show marker
        if(!aiSelectedCarIDs.Contains(carID)){
            aiSelectedCarIDs.Add(carID);
            CreateAIMarker(carID, (int)carGridPos.x, (int)carGridPos.y);
            //Debug.Log($"Restored AI selection on car: {carID} at grid ({carGridPos.x}, {carGridPos.y})");
        } else if(aiMarkerDict.ContainsKey(carID)){
            // AI already exists, just reposition and show the marker
            GameObject aiMarker = aiMarkerDict[carID];
            if(aiMarker != null){
                aiMarker.SetActive(true); // Make sure marker is visible
                UpdateMarkerWorldPosition(aiMarker, carGridPos);
                //Debug.Log($"Repositioned and showed existing AI marker for car: {carID} at grid ({carGridPos.x}, {carGridPos.y})");
            }
        }
    }
    
    int FindCarIndex(string carID){
        for(int i = 0; i < allCarData.Count; i++){
            if(allCarData[i].id == carID){
                return i;
            }
        }
        return -1;
    }
    
    Vector2 GetGridPositionForCarIndex(int carIndex){
        if(carIndex < 0 || carIndex >= allCarData.Count) return new Vector2(-1, -1);
        
        int totalCars = allCarData.Count;
        int carsPerRow = Mathf.Min(6, Mathf.CeilToInt(Mathf.Sqrt(totalCars)));
        int rows = Mathf.CeilToInt((float)totalCars / carsPerRow);
        int rowOffset = Mathf.RoundToInt((6 - rows) * 0.5f);
        
        int row = carIndex / carsPerRow;
        int col = carIndex % carsPerRow;
        
        int carsInThisRow = Mathf.Min(carsPerRow, totalCars - (row * carsPerRow));
        int colOffset = Mathf.RoundToInt((6 - carsInThisRow) * 0.5f);
        
        int gridX = col + colOffset;
        int gridY = row + rowOffset;
        
        gridX = Mathf.Clamp(gridX, 0, 5);
        gridY = Mathf.Clamp(gridY, 0, 5);
        
        return new Vector2(gridX, gridY);
    }
    
    void RepositionMarkersAfterCarRearrangement(){
        // Reposition player markers that have selected cars
        for(int i = 0; i < players.Count && i < selectedCarIDs.Count; i++){
            string selectedCarID = selectedCarIDs[i];
            if(!string.IsNullOrEmpty(selectedCarID) && markerLocked[i]){
                // Find new position for this car
                int carIndex = FindCarIndex(selectedCarID);
                if(carIndex != -1){
                    Vector2 newGridPos = GetGridPositionForCarIndex(carIndex);
                    if(newGridPos.x >= 0){
                        // Update marker position
                        markerPositions[i] = newGridPos;
                        if(i < playerMarkers.Count && playerMarkers[i] != null){
                            UpdateMarkerWorldPosition(playerMarkers[i], newGridPos);
                        }
                        //Debug.Log($"Repositioned player {i + 1} marker to new car position: ({newGridPos.x}, {newGridPos.y})");
                    }
                } else {
                    // Car no longer available - deselect it
                    Debug.Log($"Car {selectedCarID} no longer available, deselecting player {i + 1}");
                    DeselectCar(i);
                }
            }
        }
        
        // Reposition AI markers (keep AIs while on the page even if cars disconnect)
        List<string> aiCarsToReposition = new List<string>(aiSelectedCarIDs);
        foreach(string carID in aiCarsToReposition){
            int carIndex = FindCarIndex(carID);
            if(carIndex != -1){
                Vector2 newGridPos = GetGridPositionForCarIndex(carIndex);
                if(newGridPos.x >= 0 && aiMarkerDict.ContainsKey(carID)){
                    // Update AI marker position and make sure it's visible
                    GameObject aiMarker = aiMarkerDict[carID];
                    if(aiMarker != null){
                        aiMarker.SetActive(true); // Ensure marker is visible
                        UpdateMarkerWorldPosition(aiMarker, newGridPos);
                        //Debug.Log($"Repositioned AI marker for car {carID} to new position: ({newGridPos.x}, {newGridPos.y})");
                    }
                }
            } else {
                // Car no longer available - hide marker but keep AI while on the page
                if(aiMarkerDict.ContainsKey(carID)){
                    GameObject aiMarker = aiMarkerDict[carID];
                    if(aiMarker != null){
                        aiMarker.SetActive(false); // Hide marker but keep AI
                        Debug.Log($"Car {carID} disconnected while on page, hiding marker but keeping AI");
                    }
                }
            }
        }
    }
    
    void CreatePlayerMarker(int playerIndex){
        if(playerIndex < 0 || playerIndex >= players.Count) return;
        
        // Create marker
        GameObject marker = Instantiate(PlayerMarkerPrefab, slectionPlatform.position, Quaternion.identity);
        marker.transform.parent = slectionPlatform;
        
        // Set marker appearance (color and name) based on player's car controller
        SetMarkerAppearance(marker, playerIndex);
        
        // Initialize position (start at center of grid)
        Vector2 gridPos = new Vector2(2.5f, 2.5f);
        
        // Offset slightly for each player to avoid overlap
        gridPos.x += (playerIndex % 2) * 0.5f;
        gridPos.y += (playerIndex / 2) * 0.5f;
        
        // Ensure position is within bounds
        gridPos.x = Mathf.Clamp(gridPos.x, 0, gridSize - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, gridSize - 1);
        
        UpdateMarkerWorldPosition(marker, gridPos);
        
        playerMarkers.Add(marker);
        markerPositions.Add(gridPos);
        markerLocked.Add(false); // Start unlocked
        selectedCarIDs.Add(""); // No car selected initially
    }
    
    void UpdatePlayerMarkers(){
        // Ensure we have the right number of markers
        while(playerMarkers.Count < players.Count){
            CreatePlayerMarker(playerMarkers.Count);
        }
        
        // Remove excess markers and their tracking data
        while(playerMarkers.Count > players.Count){
            if(playerMarkers[playerMarkers.Count - 1] != null){
                DestroyImmediate(playerMarkers[playerMarkers.Count - 1]);
            }
            playerMarkers.RemoveAt(playerMarkers.Count - 1);
            markerPositions.RemoveAt(markerPositions.Count - 1);
            markerLocked.RemoveAt(markerLocked.Count - 1);
            selectedCarIDs.RemoveAt(selectedCarIDs.Count - 1);
        }
        
        // Update marker positions based on player input
        for(int i = 0; i < players.Count && i < playerMarkers.Count; i++){
            if(players[i] != null && playerMarkers[i] != null){
                // Only update position if marker is not locked
                if(!markerLocked[i]){
                    UpdateMarkerPosition(i);
                }
                // Always refresh appearance (color and name) in case they changed
                SetMarkerAppearance(playerMarkers[i], i);
            }
        }
    }
    
    void UpdateMarkerPosition(int playerIndex){
        if(playerIndex >= players.Count || playerIndex >= markerPositions.Count) return;
        
        PlayerController player = players[playerIndex];
        Vector2 input = player.moveInput;
        
        // Apply movement with some smoothness
        Vector2 currentPos = markerPositions[playerIndex];
        Vector2 targetPos = currentPos;
        
        // Movement speed (adjust as needed)
        float moveSpeed = 5f * Time.deltaTime;
        
        // Rotate input by 95 degrees for side perspective
        // 95 degrees = 95 * (π/180) ≈ 1.658 radians
        float rotationRad = 80f * Mathf.Deg2Rad;
        Vector2 rotatedInput = new Vector2(
            input.x * Mathf.Cos(rotationRad) - input.y * Mathf.Sin(rotationRad),
            input.x * Mathf.Sin(rotationRad) + input.y * Mathf.Cos(rotationRad)
        );
        
        // Apply rotated input to grid position
        targetPos.x += rotatedInput.x * moveSpeed;
        targetPos.y += rotatedInput.y * moveSpeed;
        
        // Clamp to grid bounds
        targetPos.x = Mathf.Clamp(targetPos.x, 0f, gridSize - 1f);
        targetPos.y = Mathf.Clamp(targetPos.y, 0f, gridSize - 1f);
        
        markerPositions[playerIndex] = targetPos;
        
        // Update world position
        UpdateMarkerWorldPosition(playerMarkers[playerIndex], targetPos);
    }
    
    void UpdateMarkerWorldPosition(GameObject marker, Vector2 gridPos){
        if(marker == null) return;
        
        // Convert grid position to world position
        Vector3 worldPos = new Vector3(
            slectionPlatform.position.x - platformSize.x/2 + platformSize.x/(gridSize-1) * gridPos.x,
            slectionPlatform.position.y + 0.1f, // Slightly above platform
            slectionPlatform.position.z - platformSize.y/2 + platformSize.y/(gridSize-1) * gridPos.y
        );
        
        marker.transform.position = worldPos;
    }
    
    // Public method to refresh car data and respawn cars (can be called when new cars are detected)
    public void RefreshCarsDisplay(){
        RefreshCarData();
        SpawnCars();
        
        // Reposition markers that need to be moved due to car layout changes
        RepositionMarkersAfterCarRearrangement();
    }
    
    // CMS callback for player select input
    void OnPlayerSelectPressed(PlayerController player){
        int playerIndex = players.IndexOf(player);
        if(playerIndex != -1){
            HandlePlayerSelection(playerIndex);
        }
    }
    
    // CMS callback for player alt select input (AI selection)
    void OnPlayerAltSelectPressed(PlayerController player){
        int playerIndex = players.IndexOf(player);
        if(playerIndex != -1){
            HandlePlayerAISelection(playerIndex);
        }
    }
    
    // CMS callback for back button input
    void OnPlayerBackPressed(){
        // Go back to main menu (UI layer 0)
        UIManager.active.SetUILayer(0);
        Debug.Log("Back button pressed - returning to main menu");
    }
    
    // CMS callback for start button input
    void OnPlayerStartPressed(PlayerController player){
        // Trigger gamemode loading when start is pressed in selection menu
        if(cms != null){
            cms.LoadGamemode();
            //Debug.Log("Start button pressed - loading gamemode");
        }
    }
    
    void HandlePlayerAISelection(int playerIndex){
        if(playerIndex >= markerPositions.Count) return;
        
        // Check if marker is on a car position
        Vector2 gridPos = markerPositions[playerIndex];
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);
        
        gridX = Mathf.Clamp(gridX, 0, gridSize - 1);
        gridY = Mathf.Clamp(gridY, 0, gridSize - 1);
        
        int carIndex = GetCarIndexAtGridPosition(gridX, gridY);
        if(carIndex == -1) return; // No car at this position
        
        CarSelectionData carData = allCarData[carIndex];
        if(string.IsNullOrEmpty(carData.id)) return; // No valid car
        
        // Check if this car already has an AI
        if(aiSelectedCarIDs.Contains(carData.id)){
            // Remove AI from this car
            RemoveAIFromCar(carData.id);
        } else if(!selectedCarIDs.Contains(carData.id)){
            // Only allow AI selection if the car is not selected by a player
            SpawnAIForCar(carData.id, gridX, gridY);
        }
    }
    
    void HandlePlayerSelection(int playerIndex){
        if(playerIndex >= markerPositions.Count) return;
        
        // Check if marker is on a car position
        Vector2 gridPos = markerPositions[playerIndex];
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);
        
        gridX = Mathf.Clamp(gridX, 0, gridSize - 1);
        gridY = Mathf.Clamp(gridY, 0, gridSize - 1);
        
        int carIndex = GetCarIndexAtGridPosition(gridX, gridY);
        if(carIndex == -1) return; // No car at this position
        
        CarSelectionData carData = allCarData[carIndex];
        if(string.IsNullOrEmpty(carData.id)) return; // No valid car
        
        // Check if player already has a car selected
        if(!string.IsNullOrEmpty(selectedCarIDs[playerIndex])){
            // Deselect current car and unlock marker
            DeselectCar(playerIndex);
        } else {
            // Try to select the car if it's not already selected
            if(!IsCarSelected(carData.id)){
                SelectCar(playerIndex, carData.id);
            }
        }
    }
    
    int GetCarIndexAtGridPosition(int gridX, int gridY){
        // Calculate car index based on how cars were spawned
        int totalCars = allCarData.Count;
        int carsPerRow = Mathf.Min(6, Mathf.CeilToInt(Mathf.Sqrt(totalCars)));
        int rows = Mathf.CeilToInt((float)totalCars / carsPerRow);
        int rowOffset = Mathf.RoundToInt((6 - rows) * 0.5f);
        
        int row = gridY - rowOffset;
        if(row < 0 || row >= rows) return -1;
        
        int carsInThisRow = Mathf.Min(carsPerRow, totalCars - (row * carsPerRow));
        int colOffset = Mathf.RoundToInt((6 - carsInThisRow) * 0.5f);
        
        int col = gridX - colOffset;
        if(col < 0 || col >= carsInThisRow) return -1;
        
        int carIndex = row * carsPerRow + col;
        return carIndex < totalCars ? carIndex : -1;
    }
    
    bool IsCarSelected(string carID){
        return (selectedCarIDs.Contains(carID) || aiSelectedCarIDs.Contains(carID)) && !string.IsNullOrEmpty(carID);
    }
    
    void SelectCar(int playerIndex, string carID){
        selectedCarIDs[playerIndex] = carID;
        markerLocked[playerIndex] = true;
        
        // Assign car to the player's controller (car is already connected)
        if(playerIndex < players.Count && players[playerIndex] != null){
            CarController carController = players[playerIndex].GetComponent<CarController>();
            if(carController != null){
                carController.SetDesiredCarID(carID);
                Debug.Log($"Assigned car {carID} to player {playerIndex + 1} controller");
            }
        }
        
        Debug.Log($"Player {playerIndex + 1} selected car: {carID} (already connected)");
        
        // Check if all players have selected cars
        CheckAllPlayersSelected();
    }
    
    IEnumerator SetAIMarkerAppearanceDelayed(GameObject marker, string carID){
        yield return new WaitForSeconds(0.1f); // Wait for AI name assignment to complete
        SetAIMarkerAppearance(marker, carID);
    }
    
    void DeselectCar(int playerIndex){
        string previousCarID = selectedCarIDs[playerIndex];
        selectedCarIDs[playerIndex] = "";
        markerLocked[playerIndex] = false;
        
        // Clear car assignment from the player's controller
        if(playerIndex < players.Count && players[playerIndex] != null){
            CarController carController = players[playerIndex].GetComponent<CarController>();
            if(carController != null){
                carController.SetDesiredCarID("");
                Debug.Log($"Cleared car assignment from player {playerIndex + 1} controller");
            }
        }
        
        // Cancel connection timer if it's running (cleanup)
        if(!string.IsNullOrEmpty(previousCarID) && connectionTimers.ContainsKey(previousCarID)){
            StopCoroutine(connectionTimers[previousCarID]);
            connectionTimers.Remove(previousCarID);
        }
        
        // Note: Car disconnection will be handled when leaving selection menu
        Debug.Log($"Player {playerIndex + 1} deselected car: {previousCarID} (will disconnect on menu exit)");
    }
    
    void CheckAllPlayersSelected(){
        bool allSelected = true;
        for(int i = 0; i < players.Count; i++){
            if(string.IsNullOrEmpty(selectedCarIDs[i])){
                allSelected = false;
                break;
            }
        }
        
        if(allSelected && players.Count > 0){
            Debug.Log($"All {players.Count} players have selected cars! ({aiSelectedCarIDs.Count} AI cars also active)");
            OnAllPlayersSelected?.Invoke();
        }
    }
    
    void SpawnAIForCar(string carID, int gridX, int gridY){
        if(cms == null) return;
        
        // Spawn AI through CMS
        cms.AddAI(carID);
        
        // Mark car as AI-selected
        aiSelectedCarIDs.Add(carID);
        
        // Create AI marker at the car position
        CreateAIMarker(carID, gridX, gridY);
        
        Debug.Log($"Spawned AI for car: {carID} at grid position ({gridX}, {gridY}) (already connected)");
    }
    
    void RemoveAIFromCar(string carID){
        if(cms == null) return;
        
        // Cancel connection timer if it's running (cleanup)
        if(connectionTimers.ContainsKey(carID)){
            StopCoroutine(connectionTimers[carID]);
            connectionTimers.Remove(carID);
        }
        
        // Remove AI through CMS
        cms.RemoveAI(carID);
        
        // Remove from AI selected list
        aiSelectedCarIDs.Remove(carID);
        
        // Remove AI marker
        RemoveAIMarker(carID);
        
        // Note: Car disconnection will be handled when leaving selection menu
        Debug.Log($"Removed AI from car: {carID} (will disconnect on menu exit)");
    }
    
    void CreateAIMarker(string carID, int gridX, int gridY){
        // Create AI marker
        GameObject aiMarker = Instantiate(PlayerMarkerPrefab, slectionPlatform.position, Quaternion.identity);
        aiMarker.transform.parent = slectionPlatform;
        
        // Set AI marker appearance with delay to ensure AI name is assigned first
        StartCoroutine(SetAIMarkerAppearanceDelayed(aiMarker, carID));
        
        // Position the marker at the car location
        Vector2 gridPos = new Vector2(gridX, gridY);
        UpdateMarkerWorldPosition(aiMarker, gridPos);
        
        aiMarkers.Add(aiMarker);
        aiMarkerDict[carID] = aiMarker;
        
        //Debug.Log($"Created AI marker for car {carID} at grid ({gridX}, {gridY})");
    }
    
    void RemoveAIMarker(string carID){
        // Find and remove the specific AI marker for this car
        if(aiMarkerDict.ContainsKey(carID)){
            GameObject marker = aiMarkerDict[carID];
            if(marker != null){
                DestroyImmediate(marker);
            }
            aiMarkers.Remove(marker);
            aiMarkerDict.Remove(carID);
        }
    }
    
    void SetAIMarkerAppearance(GameObject marker, string carID){
        if(marker == null) return;
        
        // Set AI marker color (orange/red to distinguish from players)
        Color aiColor = Color.red;
        
        // Set color for all Image components on the marker
        Image[] images = marker.GetComponentsInChildren<Image>();
        foreach(Image img in images){
            img.color = aiColor;
        }
        
        // Get AI name from the CarController
        string aiName = "AI";
        if(cms != null){
            foreach(CarController controller in cms.controllers){
                if(controller.GetDesiredCarID() == carID){
                    aiName = controller.GetPlayerName();
                    Debug.Log($"Found AI controller for car {carID} with name: {aiName}");
                    break;
                }
            }
        }
        
        // Set AI text with the actual AI name
        TMP_Text[] textComponents = marker.GetComponentsInChildren<TMP_Text>();
        foreach(TMP_Text text in textComponents){
            text.text = aiName;
        }
    }
    
    void SetMarkerAppearance(GameObject marker, int playerIndex){
        if(marker == null || playerIndex >= players.Count) return;
        
        // Get player's car controller
        CarController carController = players[playerIndex].GetComponent<CarController>();
        if(carController == null) return;
        
        Color playerColor = carController.GetPlayerColor();
        string playerName = carController.GetPlayerName();
        
        // Modify color if marker is locked (selected a car)
        if(playerIndex < markerLocked.Count && markerLocked[playerIndex]){
            playerColor = Color.Lerp(playerColor, Color.white, 0.5f); // Brighten when locked
        }
        
        // Set color for all Image components on the marker
        Image[] images = marker.GetComponentsInChildren<Image>();
        foreach(Image img in images){
            img.color = playerColor;
        }
        // Set text for all TMP_Text components on the marker
        TMP_Text nametext = marker.GetComponentInChildren<TMP_Text>();
        nametext.text = playerName;
    }
    
    void ConnectToAllAvailableCars(){
        CarInteraface carInterface = CarInteraface.io;
        if(carInterface == null) return;
        
        Debug.Log("Connecting to all available cars for faster selection...");
        
        // Connect to all available cars
        foreach(CarSelectionData carData in allCarData){
            if(!string.IsNullOrEmpty(carData.id)){
                carInterface.ConnectCar(carData.id);
                Debug.Log($"Connecting to car: {carData.id}");
            }
        }
    }
    
    void DisconnectUnusedCars(){
        CarInteraface carInterface = CarInteraface.io;
        if(carInterface == null || cms == null) return;
        
        Debug.Log("Disconnecting cars without matching controllers...");
        
        // Get all car IDs that have controllers
        HashSet<string> carsWithControllers = new HashSet<string>();
        foreach(CarController controller in cms.controllers){
            string carID = controller.GetDesiredCarID();
            if(!string.IsNullOrEmpty(carID)){
                carsWithControllers.Add(carID);
            }
        }
        
        // Disconnect cars that don't have controllers
        foreach(CarSelectionData carData in allCarData){
            if(!string.IsNullOrEmpty(carData.id) && !carsWithControllers.Contains(carData.id)){
                carInterface.DisconnectCar(carData.id);
                Debug.Log($"Disconnecting unused car: {carData.id}");
            }
        }
    }
}