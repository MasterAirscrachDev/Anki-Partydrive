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
    // Start is called before the first frame update
    public void Setup(string name, string id){
        nameInput.text = name;
        oldName = name;
        carID = id;
        speedBalanceButton.onClick.AddListener(OnSpeedBalanceButton);
        AIButton.onClick.AddListener(OnAIToggle);
        nameInput.onEndEdit.AddListener(OnNameChanged);
    }

    public void OnSpeedBalanceButton(){
        speedBalanceButton.interactable = false;
        //initiate speed balance for this car
        UIManager ui = FindObjectOfType<UIManager>();
        ui.SetUILayer(3); //go to track scanning UI
        ui.ToggleUILayer(4, true); //enable balancing UI
        FindObjectOfType<CarBalancer>().Setup(carID);
    }
    public void OnAIToggle(){
        FindObjectOfType<CMS>().SpawnAI(carID);
        AIButton.interactable = false;
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

    public void SetPlayerColor(Color color){
        playerColorImage.color = color;
    }
}
