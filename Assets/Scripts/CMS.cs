using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Central Management System
public class CMS : MonoBehaviour
{
    [SerializeField] GameObject botPrefab;
    [SerializeField] CarEntityTracker carEntityTracker;
    public readonly List<CarController> controllers = new List<CarController>();
    public string gameMode = "";
    public bool isGame = false;
    List<Color> freeColors = new List<Color>();
    List<Color> usedColors = new List<Color>();
    CarInteraface carInterface;
    public static CMS cms;
    void Start() { 
        carInterface = CarInteraface.io;
        StartCoroutine(GetCarInterface());
        //color list purple, green, red, blue
        freeColors.Add(new Color(1, 0, 1)); //purple/pink
        freeColors.Add(new Color(0, 1, 0)); //green
        freeColors.Add(new Color(1, 0, 0)); //red
        freeColors.Add(new Color(0, 0, 1)); //blue
        freeColors.Add(new Color(0, 1, 1)); //Cyan
        freeColors.Add(new Color(1, 1, 0)); //yellow

        cms = this;
    }
    IEnumerator GetCarInterface(){
        yield return new WaitForSeconds(0.5f);
        carInterface = CarInteraface.io;
    }
    public void LoadGamemode(){
        SetAllCarEnginesLights(); // Set engine lights for all cars when gamemode starts
        
        UIManager ui = UIManager.active;
        if(cms.gameMode == "Time Trial"){
            ui.SetUILayer(5);
        }
        else if (cms.gameMode == "Laps"){
            ui.SetUILayer(6);
        }
        else if (cms.gameMode == "Laps2"){
            ui.SetUILayer(7);
        }
        else if(cms.gameMode == "Party"){
            Debug.Log("Party Mode not done yet");
        }
    }
    
    public void SetPlayersRacingMode(bool racingMode){
        foreach(CarController controller in controllers){
            if(controller == null) continue;
            
            PlayerController playerController = controller.GetComponent<PlayerController>();
            if(playerController != null){
                playerController.SetRacingMode(racingMode);
                Debug.Log($"Set {controller.GetPlayerName()} racing mode to: {racingMode}");
            }
        }
    }
    
    public void SetAllCarEnginesLights(){
        if(carInterface == null) return;
        
        foreach(CarController controller in controllers){
            if(controller == null || string.IsNullOrEmpty(controller.GetID())) continue;
            
            // Get the car data for this controller
            UCarData carData = carInterface.GetCarFromID(controller.GetID());
            if(carData == null) continue;
            
            // Get the player color and convert to RGB (0-255)
            Color playerColor = controller.GetPlayerColor();
            int r = Mathf.RoundToInt(playerColor.r * 255);
            int g = Mathf.RoundToInt(playerColor.g * 255);
            int b = Mathf.RoundToInt(playerColor.b * 255);
            
            // Set the car's engine light to match the player color
            carInterface.SetCarColours(carData, r, g, b);
            Debug.Log($"Set engine light for {controller.GetPlayerName()}'s car ({controller.GetID()}) to RGB({r},{g},{b})");
        }
    }
    public void AddController(CarController controller, bool isAI = false){
        controllers.Add(controller);
        if(isAI){ return; } //if the controller is AI, return (Colour is set in AIController)
        Debug.Log($"available colors: {freeColors.Count}");
        Color c = freeColors[0];
        freeColors.RemoveAt(0);
        usedColors.Add(c);
        controller.SetColour(c);
    }
    public void AddAI(string id){
        GameObject bot = Instantiate(botPrefab, transform.position, Quaternion.identity);
        AIController ai = bot.GetComponent<AIController>();
        ai.SetID(id);
    }
    public void RemoveAI(string id){
        Debug.Log($"Attempting to remove AI {id}");
        bool hasRemovedAI = false;
        foreach(CarController controller in controllers){
            if(controller.GetComponent<AIController>() != null && controller.GetComponent<AIController>().GetID() == id){
                controllers.Remove(controller);
                Destroy(controller.gameObject);
                Debug.Log($"Removed AI {id}, available colors: {freeColors.Count}");
                hasRemovedAI = true;
                break;
            }else{
                Debug.Log($"Checked controller {controller.GetID()}, does not match {id}");
            }
        }
        if(!hasRemovedAI){
            Debug.LogWarning($"Attempted to remove AI {id}, but no matching controller was found.");
        }
        // Ensure PlayerCard cleanup happens after the controller is removed
        StartCoroutine(UpdateCardCountDelayed());
    }
    
    System.Collections.IEnumerator UpdateCardCountDelayed(){
        yield return new WaitForEndOfFrame(); // Wait one frame to ensure cleanup
        PlayerCardmanager cardManager = FindFirstObjectByType<PlayerCardmanager>();
        if(cardManager != null){
            cardManager.UpdateCardCount();
        }
    }
    public void SetGameMode(string mode){ //called from ui
        gameMode = mode;
    }
    public void SetGame(bool isGame){
        this.isGame = isGame;
    }
    public void SetGlobalLock(bool isLocked){
        foreach(CarController controller in controllers){
            if(controller == null){ continue; }
            controller.SetLocked(isLocked);
        }
    }
    public CarController GetController(string carID){
        return controllers.Find(x => x.GetID() == carID);
    }
    public void StopAllCars(){
        foreach(CarController controller in controllers){
            controller.StopCar();
        }
    }
    public void TTS(string text){
        carInterface.TTSCall(text);
    }
    public string CarNameFromId(string id){
        for(int i = 0; i < carInterface.cars.Length; i++){
            if(carInterface.cars[i].id == id){
                return carInterface.cars[i].name;
            }
        }
        return "Unknown Car";
    }
    public List<CarController> SphereCheckControllers(Vector3 position, float range){
        List<CarController> controllersInRange = new List<CarController>();
        foreach(CarController controller in controllers){
            if(controller == null){ continue; } //skip null controllers
            if(controller.GetID() == ""){ continue; } //skip uninitialised cars
            if(Vector3.Distance(carEntityTracker.GetCarVisualPosition(controller.GetID()), position) <= range){
                controllersInRange.Add(controller);
            }
        }
        return controllersInRange;
    }
    public List<CarController> CubeCheckControllers(Vector3 center, Vector3 forward, float size)
    {
        List<CarController> controllersInRange = new List<CarController>();
        foreach(CarController controller in controllers){
            if(controller == null){ continue; } //skip null controllers
            if(controller.GetID() == ""){ continue; } //skip uninitialised cars
            Vector3 carPos = carEntityTracker.GetCarVisualPosition(controller.GetID());
            Vector3 toCar = carPos - center;
            float forwardDist = Vector3.Dot(toCar, forward.normalized);
            Vector3 projectedPoint = center + forward.normalized * forwardDist;
            float sideDist = Vector3.Distance(carPos, projectedPoint);
            if(Mathf.Abs(forwardDist) <= size / 2 && sideDist <= size / 2){
                controllersInRange.Add(controller);
            }
        }
        return controllersInRange;
    }
    public void OnCarOutOfEnergyCarCallback(string id, CarController controller){ onCarNoEnergy?.Invoke(id, controller); }
    public void OnBackToMenuCallback(){ onBackToMenu?.Invoke(); }
    public void OnSelectCallback(PlayerController pc){ onSelect?.Invoke(pc); }
    public void OnAltSelectCallback(PlayerController pc){ onAltSelect?.Invoke(pc); }
    public void OnStartSelectCallback(PlayerController pc){ onStartSelect?.Invoke(pc); }
    
    //use in gamemode scripts to set the behavior of cars when they run out of energy
    public delegate void OnCarNoEnergy(string id, CarController controller);
    public event OnCarNoEnergy onCarNoEnergy;
    public delegate void OnUIBackToMenu();
    public event OnUIBackToMenu onBackToMenu;
    public delegate void OnUISelect(PlayerController pc);
    public event OnUISelect onSelect;
    public delegate void OnUIAltSelect(PlayerController pc);
    public event OnUIAltSelect onAltSelect;
    public delegate void OnUIStartSelect(PlayerController pc);
    public event OnUIStartSelect onStartSelect;
}