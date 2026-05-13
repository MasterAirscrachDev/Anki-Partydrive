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
            cms = FindFirstObjectByType<CMS>();
            carInterface = SR.io;
        }   
        //backButton.SetActive(!cms.isGame);
        //raceButton.SetActive(cms.isGame);
        //foreach(CarController controller in cms.controllers){ controller.AssignCarsManager(this); }
        RenderCarList();
    }
    void OnDisable() {
        backButton.SetActive(false);
        raceButton.SetActive(false);
        //foreach(CarController controller in cms.controllers){ controller.AssignCarsManager(null); }
    }
    public void NextCar(CarController change) {
        FindFirstObjectByType<CarEntityTracker>().SetCarColorByID(change.GetID(), Color.clear); //reset the colour of the old car
        int index = carInterface.GetCarIndex(change.GetID());
        index++;
        //while the index is less than the number of cars, and the car at that index does not have a controller, increment the index
        //if the index is greater than the number of cars, set it to -1 and break
        while(index < carInterface.cars.Length && (carInterface.cars[index].cState != ConnectedState.CONNECTED || cms.GetController(carInterface.cars[index].id) != null)){ index++; }
        if(index >= carInterface.cars.Length){ index = -1; } //if the index is greater than the number of cars, set it to -1


        change.SetCar(index == -1 ? null : carInterface.cars[index]);
        RenderCarList();
    }
    public void LoadGamemode(){ //called from UI
        UIManager ui = SR.ui;
        if(cms.gameMode == "Time Trial"){
            ui.SetUILayer("ModeTimeTrial");
        }
        else if (cms.gameMode == "Laps"){
            ui.SetUILayer("ModeLaps");
        }
        else if (cms.gameMode == "Laps2"){
            ui.SetUILayer("CarSelection");
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

        UCarData[] connectedCars = System.Array.FindAll(carInterface.cars ?? new UCarData[0], c => c.cState == ConnectedState.CONNECTED);
        foreach (Transform child in carListParent) { Destroy(child.gameObject); }
        carPanels = new CarPanel[connectedCars.Length];
        for (int i = 0; i < connectedCars.Length; i++){ //for each connected car
            GameObject carItem = Instantiate(carListItemPrefab, carListParent);
            CarPanel carPanel = carItem.GetComponent<CarPanel>();
            CarController carController = cms.GetController(connectedCars[i].id);

            carPanel.Setup(connectedCars[i].name, connectedCars[i].id, carController);
            carPanels[i] = carPanel;
            RectTransform carItemRect = carItem.GetComponent<RectTransform>();
            carItemRect.anchoredPosition = new Vector2(carItemRect.anchoredPosition.x, (-i * 100) - 50);
            
            if(carController != null){
                Color color = carController.GetPlayerColor();
                //convert the color to a int3
                int R = (int)(color.r * 14);
                int G = (int)(color.g * 14);
                int B = (int)(color.b * 14);
                LightData RD = LightData.L(LightChannel.RED, LightEffect.THROB, R, Mathf.Clamp(R - 3, 0, 14), 10);
                LightData GR = LightData.L(LightChannel.GREEN, LightEffect.THROB, G, Mathf.Clamp(G - 3, 0, 14), 10);
                LightData BL = LightData.L(LightChannel.BLUE, LightEffect.THROB, B, Mathf.Clamp(B - 3, 0, 14), 10);
                carInterface.SetCarColoursComplex(connectedCars[i], new LightData[]{RD, GR, BL});
            }else{
                LightData[] colors = new LightData[3];
                colors[0] = new LightData{ channel = LightChannel.RED, effect = LightEffect.THROB, startStrength = 14, endStrength = 0, cyclesPer10Seconds = 6 };
                colors[1] = new LightData{ channel = LightChannel.GREEN, effect = LightEffect.THROB, startStrength = 14, endStrength = 0, cyclesPer10Seconds = 5 };
                colors[2] = new LightData{ channel = LightChannel.BLUE, effect = LightEffect.THROB, startStrength = 14, endStrength = 0, cyclesPer10Seconds = 4 };
                carInterface.SetCarColoursComplex(connectedCars[i], colors);
            }
        }
        RectTransform carListRect = carListParent.GetComponent<RectTransform>();
        carListRect.sizeDelta = new Vector2(carListRect.sizeDelta.x, connectedCars.Length * 100);

        buttons = FindObjectsOfType<Button>();
        //select the previously selected button
        if(selectedIndex != -1 && selectedIndex < buttons.Length){
            eventSystem.SetSelectedGameObject(buttons[selectedIndex].gameObject);
        }else{
            eventSystem.SetSelectedGameObject(buttons[0].gameObject); //select the first button
        }
    }
}