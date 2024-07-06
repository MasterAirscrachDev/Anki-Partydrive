using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerCardSystem : MonoBehaviour
{
    [SerializeField] TMP_Text characterName, carName, positionText;
    [SerializeField] Image energyBar;
    float energyPercent = 0.75f;
    public void SetCharacterName(string name){
        characterName.text = name;
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
}
