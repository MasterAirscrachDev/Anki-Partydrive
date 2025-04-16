using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarsManagement : MonoBehaviour
{
    [SerializeField] GameObject backButton, raceButton;
    [SerializeField] Transform carListParent;
    [SerializeField] GameObject carListItemPrefab;
    CMS cms;
    CarInteraface carInterface;
    CarPanel[] carPanels;
    // Start is called before the first frame update
    void OnEnable() {
        if(cms == null){
            cms = FindObjectOfType<CMS>();
            carInterface = CarInteraface.io;
        }   
        backButton.SetActive(!cms.isGame);
        raceButton.SetActive(cms.isGame);
        foreach(CarController controller in cms.controllers){
            controller.carsManagement = this;
        }
        RenderCarList();
    }
    void OnDisable() {
        backButton.SetActive(false);
        raceButton.SetActive(false);
        foreach(CarController controller in cms.controllers){
            controller.carsManagement = null;
        }
    }
    public void NextCar(CarController change){
        string ID = change.GetID();
        int index = carInterface.GetCar(ID);
        //Debug.Log($"Current Car is {index}");
        index++;
        if(index >= carInterface.cars.Length){
            index = -1;
        }
        //Debug.Log($"Next Car is {index}");
        if(index == -1){
            change.SetID(null);
        }else{
            change.SetID(carInterface.cars[index]);
        }

        RenderCarList();
    }
    public void LoadGamemode(){
        UIManager ui = FindObjectOfType<UIManager>();
        if(cms.gameMode == GameMode.TimeTrial){
            ui.SetUILayer(5);
        }
        else if(cms.gameMode == GameMode.Party){
            Debug.Log("Party Mode not done yet");
        }
    }

    public void RenderCarList(){
        foreach (Transform child in carListParent) {
            Destroy(child.gameObject);
        }
        carPanels = new CarPanel[carInterface.cars.Length];
        for (int i = 0; i < carInterface.cars.Length; i++){
            GameObject carItem = Instantiate(carListItemPrefab, carListParent);
            CarPanel carPanel = carItem.GetComponent<CarPanel>();
            carPanel.Setup(carInterface.cars[i].name, carInterface.cars[i].id);
            carPanels[i] = carPanel;
            RectTransform carItemRect = carItem.GetComponent<RectTransform>();
            carItemRect.anchoredPosition = new Vector2(carItemRect.anchoredPosition.x, (-i * 100) - 50);
            bool found = false;
            for(int j = 0; j < cms.controllers.Count; j++){
                if(cms.controllers[j].GetID() == carInterface.cars[i].id){
                    Color color = cms.controllers[j].GetPlayerColor();
                    carPanel.SetPlayerColor(color);
                    //convert the color to a int3
                    int R = (int)(color.r * 255);
                    int G = (int)(color.g * 255);
                    int B = (int)(color.b * 255);
                    carInterface.SetCarColours(carInterface.cars[i], R, G, B);
                    found = true;
                    break;
                }
            }
            if(!found){
                carPanel.SetPlayerColor(Color.clear);
            }
        }
        RectTransform carListRect = carListParent.GetComponent<RectTransform>();
        carListRect.sizeDelta = new Vector2(carListRect.sizeDelta.x, carInterface.cars.Length * 100);
    }
}
