using UnityEngine;
using TMPro;

public class SupportPanel : MonoBehaviour
{
    [SerializeField] TMP_Text playtimeText, closeButtonText;
    float playtimeSecondsAtEnterSupport = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"Entered Support Panel at {Time.realtimeSinceStartup} seconds");
        playtimeSecondsAtEnterSupport = Time.realtimeSinceStartup;
        Update();
    }

    // Update is called once per frame
    void Update()
    {
        //set playtime text as hours:minutes:seconds from (or just MM:SS if less than an hour) from currrent playtime (Time.realtimeSinceStartup) 
        float playtime = Time.realtimeSinceStartup;
        int hours = Mathf.FloorToInt(playtime / 3600);
        int minutes = Mathf.FloorToInt((playtime % 3600) / 60);
        int seconds = Mathf.FloorToInt(playtime % 60);
        if(hours > 0){
            playtimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        }
        else{
            playtimeText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        //set the close button text to "CLOSE (X)" if we have been in the support panel for less than 5 seconds, otherwise just "CLOSE"
        if(Time.realtimeSinceStartup - playtimeSecondsAtEnterSupport < 5f){
            closeButtonText.text = string.Format("CLOSE ({0})", Mathf.CeilToInt(5f - (Time.realtimeSinceStartup - playtimeSecondsAtEnterSupport)));
        }
        else{
            closeButtonText.text = "CLOSE";
        }

    }
    public void CloseButtonPressed()
    {
        //if we have been in the support panel for less than 5 seconds, do nothing
        if(Time.realtimeSinceStartup - playtimeSecondsAtEnterSupport < 5f)
        { return; }
        else
        {
            SR.ui.SetUILayer(0); //menu
        }
    }
    public void SupporterButtonPressed()
    {
        SR.cms.isSupporter = true; //stop showing support popup for supporters
        Application.OpenURL("https://ko-fi.com/masterairscrachdev");
    }
}
