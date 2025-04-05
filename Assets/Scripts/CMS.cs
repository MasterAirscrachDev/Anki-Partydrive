using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Central Management System
public class CMS : MonoBehaviour
{
    public readonly List<CarController> controllers = new List<CarController>();
    public GameMode gameMode = GameMode.None;
    public bool isGame = false;
    CarInteraface carInterface;
    void Start()
    { carInterface = FindObjectOfType<CarInteraface>(); }
    public void AddController(CarController controller){
        controllers.Add(controller);
    }
    public void SetGameMode(int mode){
        gameMode = (GameMode)mode;
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
}
[System.Serializable]
public enum GameMode{
    None, TimeTrial, Party
}