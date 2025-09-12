using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OverdriveServer.NetStructures;

//This script manages the car selection UI and interactions with the CMS
public class CarsManagement : MonoBehaviour
{
    [SerializeField] GameObject backButton, raceButton;
    [SerializeField] Transform carListParent;
    [SerializeField] GameObject carListItemPrefab;
    CMS cms;
    CarInteraface carInterface;
    CarPanel[] carPanels;
    [SerializeField] EventSystem eventSystem;
    // Start is called before the first frame update
    void OnEnable() {
        if(cms == null){
            cms = FindObjectOfType<CMS>();
            carInterface = CarInteraface.io;
        }   
        backButton.SetActive(!cms.isGame);
        raceButton.SetActive(cms.isGame);
        foreach(CarController controller in cms.controllers){ controller.AssignCarsManager(this); }
        RenderCarList();
    }
    void OnDisable() {
        backButton.SetActive(false);
        raceButton.SetActive(false);
        foreach(CarController controller in cms.controllers){ controller.AssignCarsManager(null); }
    }
    public void NextCar(CarController change) {
        FindObjectOfType<CarEntityTracker>().SetCarIDColor(change.GetID(), Color.clear); //reset the colour of the old car
        int index = carInterface.GetCarIndex(change.GetID());
        index++;
        //while the index is less than the number of cars, and the car at that index does not have a controller, increment the index
        //if the index is greater than the number of cars, set it to -1 and break
        while(index < carInterface.cars.Length && cms.GetController(carInterface.cars[index].id) != null){ index++; }
        if(index >= carInterface.cars.Length){ index = -1; } //if the index is greater than the number of cars, set it to -1


        change.SetCar(index == -1 ? null : carInterface.cars[index]);
        RenderCarList();
    }
    public void LoadGamemode(){ //called from UI
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

    public void RenderCarList(){
        //store the index of the currently selected UI element
        GameObject selected = eventSystem.currentSelectedGameObject;
        Button[] buttons = FindObjectsOfType<Button>();
        int selectedIndex = -1;
        for(int i = 0; i < buttons.Length; i++){
            if(buttons[i].gameObject == selected){
                selectedIndex = i;
                break;
            }
        }

        foreach (Transform child in carListParent) { Destroy(child.gameObject); }
        carPanels = new CarPanel[carInterface.cars.Length];
        for (int i = 0; i < carInterface.cars.Length; i++){ //for each car connected
            GameObject carItem = Instantiate(carListItemPrefab, carListParent);
            CarPanel carPanel = carItem.GetComponent<CarPanel>();
            CarController carController = cms.GetController(carInterface.cars[i].id);

            carPanel.Setup(carInterface.cars[i].name, carInterface.cars[i].id, carController);
            carPanels[i] = carPanel;
            RectTransform carItemRect = carItem.GetComponent<RectTransform>();
            carItemRect.anchoredPosition = new Vector2(carItemRect.anchoredPosition.x, (-i * 100) - 50);
            
            if(carController != null){
                Color color = carController.GetPlayerColor();
                //convert the color to a int3
                int R = (int)(color.r * 200);
                int G = (int)(color.g * 200);
                int B = (int)(color.b * 200);
                carInterface.SetCarColours(carInterface.cars[i], R, G, B);
            }else{
                LightData[] colors = new LightData[3];
                colors[0] = new LightData{ channel = LightChannel.RED, effect = LightEffect.THROB, startStrength = 20, endStrength = 0, cyclesPer10Seconds = 6 };
                colors[1] = new LightData{ channel = LightChannel.GREEN, effect = LightEffect.THROB, startStrength = 20, endStrength = 0, cyclesPer10Seconds = 5 };
                colors[2] = new LightData{ channel = LightChannel.BLUE, effect = LightEffect.THROB, startStrength = 20, endStrength = 0, cyclesPer10Seconds = 4 };
                carInterface.SetCarColoursComplex(carInterface.cars[i], colors);
            }
        }
        RectTransform carListRect = carListParent.GetComponent<RectTransform>();
        carListRect.sizeDelta = new Vector2(carListRect.sizeDelta.x, carInterface.cars.Length * 100);

        buttons = FindObjectsOfType<Button>();
        //select the previously selected button
        if(selectedIndex != -1 && selectedIndex < buttons.Length){
            eventSystem.SetSelectedGameObject(buttons[selectedIndex].gameObject);
        }else{
            eventSystem.SetSelectedGameObject(buttons[0].gameObject); //select the first button
        }
    }
}