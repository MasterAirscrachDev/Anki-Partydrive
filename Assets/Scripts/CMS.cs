using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Central Management System
public class CMS : MonoBehaviour
{
    [SerializeField] GameObject botPrefab;
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
    public void AddController(CarController controller, bool isAI = false){
        controllers.Add(controller);
        if(isAI){ return; } //if the controller is AI, return (Colour is set in AIController)
        Debug.Log($"available colors: {freeColors.Count}");
        Color c = freeColors[0];
        freeColors.RemoveAt(0);
        usedColors.Add(c);
        controller.SetColour(c);
    }
    public void SpawnAI(string id){
        GameObject bot = Instantiate(botPrefab, transform.position, Quaternion.identity);
        AIController ai = bot.GetComponent<AIController>();
        ai.SetID(id);
    }
    public void RemoveAI(string id){
        foreach(CarController controller in controllers){
            if(controller.GetID() == id){
                Color c = controller.GetPlayerColor();
                freeColors.Add(c);
                usedColors.Remove(c);
                controllers.Remove(controller);
                Destroy(controller.gameObject);
                break;
            }
        }
        FindObjectOfType<PlayerCardmanager>().UpdateCardCount(); //Remove the card from the UI
    }
    public void SetGameMode(string mode){ //called from ui
        gameMode = mode;
    }
    public void SetGame(bool isGame){
        this.isGame = isGame;
    }
    public void SetGlobalLock(bool isLocked){
        foreach(CarController controller in controllers){
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
    public void OnCarOutOfEnergyCarCallback(string id, CarController controller){
        onCarNoEnergy?.Invoke(id, controller);
    }
    
    //use in gamemode scripts to set the behavior of cars when they run out of energy
    public delegate void OnCarNoEnergy(string id, CarController controller);
    public event OnCarNoEnergy onCarNoEnergy;
}