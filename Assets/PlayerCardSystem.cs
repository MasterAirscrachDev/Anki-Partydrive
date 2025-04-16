using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerCardSystem : MonoBehaviour
{
    [SerializeField] TMP_Text playerName, carName, positionText;
    [SerializeField] Image energyBar;
    [SerializeField] GameObject[] attachments;
    float energyPercent = 0.75f;
    int attachmentIndex = -1;
    public void SetPlayerName(string name){
        playerName.text = name;
    }
    public void SetCarName(string name){
        carName.text = name;
    }
    public void SetPosition(int position){
        //get the position value and add the suffix
        string suffix = "th";
        if(position == 1){ suffix = "st"; }
        else if(position == 2){ suffix = "nd"; }
        else if(position == 3){ suffix = "rd"; }
        positionText.text = $"{position}{suffix}";
    }
    public int GetEnergy(int maxEnergy = 100){
        //get the energy value using the energyPercent of maxEnergy
        return (int)Mathf.Lerp(0, maxEnergy, energyPercent);
    }
    public void SetEnergy(int energy, int maxEnergy = 100){
        //set the energyPercent using the energy value and maxEnergy
        energyPercent = (float)energy / (float)maxEnergy;
        energyBar.fillAmount = energyPercent;
    }
    public void SetAttachment(int index){
        //set the attachment active state using the index
        if(index == -1){ //no attachment
            if(gameObject.transform.childCount > 1){ Destroy(gameObject.transform.GetChild(1).gameObject);  }
        }
        else{
            if(attachmentIndex != -1){ Destroy(gameObject.transform.GetChild(1).gameObject); }
            Instantiate(attachments[index], gameObject.transform);
            attachmentIndex = index;
        }
    }
    public void SetColor(Color color){
        //set the color of the player card using the color value
        GetComponent<Image>().color = color;
    }
    public void SetTimeTrialTime(float time){
        //set the time trial time using the time value
        int mins = 0;
        while(time >= 60){
            time -= 60;
            mins++;
        }
        if(attachmentIndex != 0){
            SetAttachment(0);
        }
        gameObject.transform.GetChild(2).GetComponent<TMP_Text>().text = $"{mins}:{time.ToString("00.00")}";
    }
}