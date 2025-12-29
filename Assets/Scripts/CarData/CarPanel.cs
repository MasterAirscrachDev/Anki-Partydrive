using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CarPanel : MonoBehaviour
{
    [SerializeField] Button speedBalanceButton, AIButton;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Image playerColorImage;
    string carID, oldName;
    bool isAI = false;
    // Start is called before the first frame update
    public void Setup(string name, string id, CarController currentController){
        nameInput.text = name;
        oldName = name;
        carID = id;
        speedBalanceButton.onClick.AddListener(OnSpeedBalanceButton);
        AIButton.onClick.AddListener(OnAIToggle);
        nameInput.onEndEdit.AddListener(OnNameChanged);

        if(currentController != null){
            if(currentController.isAI){
                isAI = true;
                AIButton.GetComponentInChildren<TextMeshProUGUI>().text = "Remove AI";
            }
            SetPlayerColor(currentController.GetPlayerColor());
        }else{
            SetPlayerColor(Color.clear);
        }
    }
    public void OnSpeedBalanceButton(){
        speedBalanceButton.interactable = false;
        //initiate speed balance for this car
        UIManager ui = UIManager.active;
        ui.SetUILayer(3); //go to track scanning UI
        ui.ToggleUILayer(4, true); //enable balancing UI
        FindObjectOfType<CarBalancer>().Setup(carID);
    }
    public void OnAIToggle(){
        CMS cms = FindObjectOfType<CMS>();
        if(isAI){ cms.RemoveAI(carID); }
        else{ cms.AddAI(carID); }
        AIButton.interactable = false;
        FindObjectOfType<CarsManagement>().RenderCarList();
    }

    public void OnNameChanged(string name){
        //sanitize name (only a-zA-Z0-9 and () )
        string sanitizedName = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9()]", "");
        if(sanitizedName != name){
            nameInput.text = sanitizedName;
        }
        Debug.Log($"Name changed from {oldName} to {sanitizedName}");
        if(sanitizedName != oldName){
            //send name change to server
            //Debug.Log($"Sending name change to server: {carID} {sanitizedName}");
            //oldName = sanitizedName;
        }
    }
    public string GetCarID(){
        return carID;
    }

    void SetPlayerColor(Color color){
        playerColorImage.color = color;
    }
}
