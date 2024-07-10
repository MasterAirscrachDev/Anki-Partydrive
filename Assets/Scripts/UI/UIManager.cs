using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject MainCamera, TrackCamera, CarsLoadingIcon;
    [SerializeField] GameObject[] UILayers;
    [SerializeField] TMP_Text devCarCount, menuCarCount, finishCounterText;
    int finishCounter = 1;
    int UIlayer = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SwitchToTrackCamera(bool track){
        MainCamera.SetActive(!track);
        TrackCamera.SetActive(track);
    }
    public void SetUILayer(int layer){
        for (int i = 0; i < UILayers.Length; i++)
        { UILayers[i].SetActive(i == layer); }
        UIlayer = layer;
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

}
