using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarsManagement : MonoBehaviour
{
    [SerializeField] GameObject backButton, raceButton;
    CMS cms;
    CarInteraface carInterface;
    // Start is called before the first frame update
    void OnEnable()
    {
        if(cms == null){
            cms = FindObjectOfType<CMS>();
            carInterface = FindObjectOfType<CarInteraface>();
        }   
        backButton.SetActive(!cms.isGame);
        raceButton.SetActive(cms.isGame);
        foreach(CarController controller in cms.controllers){
            controller.carsManagement = this;
        }
    }
    void OnDisable()
    {
        backButton.SetActive(false);
        raceButton.SetActive(false);
        foreach(CarController controller in cms.controllers){
            controller.carsManagement = null;
        }
    }
    public void NextCar(CarController change){
        
        int index = change.GetCarIndex();
        //Debug.Log($"Current Car is {index}");
        index++;
        if(index >= carInterface.cars.Length){
            index = -1;
        }
        //Debug.Log($"Next Car is {index}");
        change.SetID(index == -1 ? "" : carInterface.cars[index].id);
    }
    public void LoadGamemode(){
        UIManager ui = FindObjectOfType<UIManager>();
        if(cms.gameMode == GameMode.TimeTrial){
            ui.SetUILayer(4);
        }
        else if(cms.gameMode == GameMode.Party){
            Debug.Log("Party Mode not done yet");
        }
    }
}
