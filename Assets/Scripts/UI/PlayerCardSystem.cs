using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerCardSystem : MonoBehaviour
{
    [SerializeField] TMP_Text carName, positionText;
    Material statusMaterial;
    [SerializeField] RawImage statusImage;
    [SerializeField] Image plateImage;
    [SerializeField] GameObject[] attachments;
    [SerializeField] CarSprite[] statusCarsArray;
    GameObject currentAttachment;
    int attachmentIndex = -1;
    void OnEnable()
    {
        statusMaterial = new Material(statusImage.material);
        statusImage.material = statusMaterial;
    }
    public void SetCarName(string name, int model = -1){
        carName.text = name;
        carName.transform.GetChild(0).GetComponent<TMP_Text>().text = name; //set the shadow text to the same name
        Sprite sprite = statusCarsArray[0].sprite; //No car
        if(model != -1){
            sprite = statusCarsArray[1].sprite; //Unknown car
            foreach(CarSprite cs in statusCarsArray){
                if(cs.id == model){
                    sprite = cs.sprite;
                    break;
                }
            }
        }
        statusImage.texture = sprite.texture;
    }
    public void SetPosition(int position){
        //get the position value and add the suffix
        if(position == 0){ positionText.text = "---"; return; }
        string suffix = "th";
        if(position == 1){ suffix = "st"; }
        else if(position == 2){ suffix = "nd"; }
        else if(position == 3){ suffix = "rd"; }
        positionText.text = $"{position}{suffix}";
    }
    public void SetEnergy(int energy, int maxEnergy = 100){
        //set the energyPercent using the energy value and maxEnergy
        float energyPercent = (float)energy / (float)maxEnergy;
        statusMaterial.SetFloat("_FillAmount", energyPercent);
        statusMaterial.SetFloat("_BlinkSpeed", energyPercent < 0.25f ? 1.0f : 0.0f);
    }
    void SetAttachment(int index){
        //set the attachment active state using the index
        if(index == -1){ //no attachment
            if(currentAttachment != null){ Destroy(currentAttachment); attachmentIndex = -1;}
        }
        else{
            if(index != attachmentIndex && currentAttachment != null){
                Destroy(currentAttachment);
            }
            if(index == attachmentIndex){ return; }
            currentAttachment = Instantiate(attachments[index], gameObject.transform);
            attachmentIndex = index;
        }
    }
    public void SetColor(Color color){
        //set the color of the player card using the color value
        //pastelize the color
        color = Color.Lerp(color, Color.white, 0.5f);
        plateImage.color = color;
    }
    public void ClearAttachment(){
        SetAttachment(-1);
    }
    public void SetTimeTrialTime(float time){
        //set the time trial time using the time value
        int mins = 0;
        while(time >= 60){ time -= 60; mins++; }
        if(attachmentIndex != 0){ SetAttachment(0); }
        currentAttachment.GetComponent<TMP_Text>().text = $"{mins}:{time.ToString("00.00")}";
    }
    public void SetLapCount(int lapCount){
        if(attachmentIndex != 1){ SetAttachment(1);}
        currentAttachment.GetComponent<TMP_Text>().text = $"{lapCount} Laps";
    }
    public void SetAbilityIcon(Ability ab) {
        if(attachmentIndex != 2){ SetAttachment(2); }
        Sprite abilitySprite = SR.gas.GetAbilityTexture(ab);
        Image abilityImage = currentAttachment.transform.GetChild(0).GetChild(1).GetComponent<Image>();
        abilityImage.sprite = abilitySprite;
        abilityImage.enabled = abilitySprite != null;
    }
    void OnDestroy()
    {
        //clean up the material instance
        if (statusMaterial != null)
        { Destroy(statusMaterial); }
    }
    [System.Serializable] class CarSprite
    {
        public int id;
        public Sprite sprite;
    }
}