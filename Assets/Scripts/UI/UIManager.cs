using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Developer UI")]
    [SerializeField] TMP_Text devCarCount;
    [SerializeField] TMP_Text devSpeed; 
    [SerializeField] TMP_Text devOffset;
    [Header("Menu UI")]
    [SerializeField] TMP_Text menuCarCount;
    [SerializeField] GameObject CarsLoadingIcon;
    [Header("Scanning UI")]
    [SerializeField] GameObject TrackScan;
    [SerializeField] GameObject TrackCancelScan;
    
    [Header("Other")]
    [SerializeField] GameObject[] UILayers;
    [SerializeField] TMP_Text finishCounterText;
    [SerializeField] CarInteraface cars;
    [SerializeField] GameObject MainCamera;
    [SerializeField] GameObject TrackCamera;
    int finishCounter = 1;
    int UIlayer = 0;
    int devCarSpeed = 400, devCarOffset = 0;

    // Start is called before the first frame update
    void Start() {
        devSpeed.text = devCarSpeed.ToString();
        devOffset.text = devCarOffset.ToString();
        SetUILayer(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SwitchToTrackCamera(bool track){
        MainCamera.SetActive(!track);
        TrackCamera.SetActive(track);
    }
    public void SetUILayer(int layer){ //sets the active UI layer, disables all others
        for (int i = 0; i < UILayers.Length; i++)
        { UILayers[i].SetActive(i == layer); }
        UIlayer = layer;
    }
    public void ToggleUILayer(int layer, bool active){ //toggles a specific UI layer
        UILayers[layer].SetActive(active);
    }
    public int GetUILayer(){
        return UIlayer;
    }
    public void SetCarsCount(int count){
        devCarCount.text = count.ToString();
        menuCarCount.text = count > 0 ? count.ToString() : "";
        CarsLoadingIcon.SetActive(count == 0);
    }
    public void ChangeFinishCounter(int change){
        finishCounter += change;
        finishCounter = Mathf.Clamp(finishCounter, 1, 9);
        finishCounterText.text = finishCounter.ToString();
    }
    public int GetFinishCounter(){
        return finishCounter;
    }
    public void SetIsScanningTrack(bool scan){
        TrackScan.SetActive(!scan);
        TrackCancelScan.SetActive(scan);
    }
    public void ModDevCarSpeed(int speed){
        devCarSpeed += speed;
        devSpeed.text = devCarSpeed.ToString();
    }
    public void ModDevCarOffset(int offset){
        devCarOffset += offset;
        devOffset.text = devCarOffset.ToString();
    }
    public void ApplyDevCarSpeed(){
        cars.DEBUGSetCarsSpeed(devCarSpeed);
    }
    public void ApplyDevCarOffset(){
        cars.DEBUGSetCarsLane(devCarOffset);
    }
}
