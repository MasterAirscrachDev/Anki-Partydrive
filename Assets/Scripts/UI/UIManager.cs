using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public struct UILayerEntry {
    public string name;
    public GameObject layer;
}

public class UIManager : MonoBehaviour
{
    [Header("Menu UI")]
    [SerializeField] TMP_Text menuCarCount;
    [SerializeField] GameObject NoServerWarningText;
    [SerializeField] GameObject CarsLoadingIcon, settingsPanel, creditsPanel;
    [SerializeField] Button playButton;
    [SerializeField] Button carsMenuButton;
    [SerializeField] Button trackMenuButton;
    [Header("Scanning UI")]
    [SerializeField] GameObject TrackScan;
    [SerializeField] GameObject TrackCancelScan;
    [SerializeField] TMP_Text ScanningStatusText;
    
    [Header("Other")]
    [SerializeField] UILayerEntry[] UILayers;
    [SerializeField] TMP_Text finishCounterText;
    [SerializeField] GameObject MainCamera;
    [SerializeField] GameObject TrackCamera;
    int finishCounter = 1;
    string UIlayer = "Menu";

    [ContextMenu("Toggle Track Camera")]
    public void ToggleTrackCamera(){ SwitchToTrackCamera(!TrackCamera.activeSelf); }
    float lastSupportPanelTime = 0;

    GameObject GetLayerObject(string name) {
        for (int i = 0; i < UILayers.Length; i++)
            if (UILayers[i].name == name) return UILayers[i].layer;
        return null;
    }

    // Start is called before the first frame update
    void Start() {
        SetUILayer("Menu");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void NoServerWarning(){
        playButton.interactable = false;
        carsMenuButton.interactable = false;
        trackMenuButton.interactable = false;
        NoServerWarningText.SetActive(true);
    }
    public void ServerConnected(){
        carsMenuButton.interactable = true;
        trackMenuButton.interactable = true;
    }   
    public void SwitchToTrackCamera(bool track){
        MainCamera.SetActive(!track);
        TrackCamera.SetActive(track);
    }
    public void SetUILayer(string layer){ //sets the active UI layer, disables all others
        for (int i = 0; i < UILayers.Length; i++)
        { UILayers[i].layer.SetActive(UILayers[i].name == layer); }
        UIlayer = layer;

        if(layer == "Menu"){ //if we are in the main menu, disable the play button
            SwitchToTrackCamera(false);
            if(!SR.cms.isSupporter){ 
                //if it has been more than 3 minutes since the last time we showed the support panel, show it again
                if(Time.realtimeSinceStartup - lastSupportPanelTime > 180f){
                    lastSupportPanelTime = Time.realtimeSinceStartup;
                    SR.ui.SetUILayer("Support"); //support panel
                    return;
                }
            }
            bool canPlay = SR.track.hasTrack;
            playButton.interactable = canPlay;
            playButton.GetComponentInChildren<TMP_Text>().text = canPlay ? "PLAY" : "Scan a Track To Start";
        }
        else if(layer == "TrackScanning"){ //if we are entering the track scanning page
            CheckConnectedCarsOnTrackPage();
        }
        else if(layer == "CarSelection") {
            MainCamera.SetActive(false);//disable the main camera when entering the carselecton
        }
        if(layer != "Menu")
        {
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }
        //find the first button in the layer and select it
        SelectFirstButtonInCurrentLayer();
    }
    public void ToggleUILayer(string layer, bool active){ //toggles a specific UI layer
        GameObject obj = GetLayerObject(layer);
        if(obj != null) obj.SetActive(active);
    }
    public string GetUILayer(){
        return UIlayer;
    }
    [ContextMenu("Select First Button")]
    public void SelectFirstButtonInCurrentLayer(){
        GameObject current = GetLayerObject(UIlayer);
        if(current == null) return;
        Button[] buttons = current.GetComponentsInChildren<Button>();
        if(buttons.Length > 0){
            buttons[0].Select();
        }
    }
    public void SetCarsCount(int count){
        menuCarCount.text = count > 0 ? count.ToString() : "";
        CarsLoadingIcon.SetActive(count == 0);
    }
    public void ChangeFinishCounter(int change){
        finishCounter += change;
        finishCounter = Mathf.Clamp(finishCounter, 1, 9);
        finishCounterText.text = finishCounter.ToString();
    }
    public int GetFinishCounter(){ return finishCounter; }
    public void SetIsScanningTrack(bool scan){
        TrackScan.SetActive(!scan);
        TrackCancelScan.SetActive(scan);
        //select whatever button is active
        if(UIlayer == "TrackScanning"){ //if we are in the scanning UI
            if(scan){ TrackCancelScan.GetComponent<Button>().Select(); }
            else{ TrackScan.GetComponent<Button>().Select(); }
        }
    }
    
    public void SetScanningStatusText(string text){
        ScanningStatusText.text = text;
    }
    
    void CheckConnectedCarsOnTrackPage(){
        // Check if there are any connected cars when entering the track page
        CarInteraface carInterface = SR.io;
        if(carInterface == null) return;
        
        // Check if any cars are currently connected
        bool hasConnectedCars = false;
        if(carInterface.cars != null && carInterface.cars.Length > 0){
            hasConnectedCars = true;
        }
        
        if(!hasConnectedCars){
            // Show alert using ScanningStatusText
            SetScanningStatusText("No connected cars detected. Please connect cars to scan the track.");
        } else {
            // Clear any previous alert
            SetScanningStatusText("");
        }
    }
    
    public void QuitGame(){
        Application.Quit();
    }
    public void OpenLink(string url){
        Application.OpenURL(url);
    }
}
