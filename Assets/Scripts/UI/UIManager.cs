using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    [SerializeField] GameObject[] UILayers;
    [SerializeField] TMP_Text finishCounterText;
    [SerializeField] GameObject MainCamera;
    [SerializeField] GameObject TrackCamera;
    int finishCounter = 1;
    int UIlayer = 0;

    [ContextMenu("Toggle Track Camera")]
    public void ToggleTrackCamera(){ SwitchToTrackCamera(!TrackCamera.activeSelf); }
    float lastSupportPanelTime = 0;

    // Start is called before the first frame update
    void Start() {
        SetUILayer(0);
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
    public void SetUILayer(int layer){ //sets the active UI layer, disables all others
        for (int i = 0; i < UILayers.Length; i++)
        { UILayers[i].SetActive(i == layer); }
        UIlayer = layer;

        if(layer == 0){ //if we are in the main menu, disable the play button
            SwitchToTrackCamera(false);
            if(!SR.cms.isSupporter){ 
                //if it has been more than 10 minutes since the last time we showed the support panel, show it again
                if(Time.realtimeSinceStartup - lastSupportPanelTime > 600f) {
                    lastSupportPanelTime = Time.realtimeSinceStartup;
                    SR.ui.SetUILayer(9); //support panel
                    return;
                }
            }
            bool canPlay = SR.track.hasTrack;
            playButton.interactable = canPlay;
            playButton.GetComponentInChildren<TMP_Text>().text = canPlay ? "PLAY" : "Scan a Track To Start";
        }
        else if(layer == 3){ //if we are entering the track scanning page
            CheckConnectedCarsOnTrackPage();
        }
        if(layer != 0)
        {
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }
        //find the first button in the layer and select it
        Button[] buttons = UILayers[layer].GetComponentsInChildren<Button>();
        if(buttons.Length > 0){
            buttons[0].Select();
        }
    }
    public void ToggleUILayer(int layer, bool active){ //toggles a specific UI layer
        UILayers[layer].SetActive(active);
    }
    public int GetUILayer(){
        return UIlayer;
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
        if(UIlayer == 3){ //if we are in the scanning UI
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
