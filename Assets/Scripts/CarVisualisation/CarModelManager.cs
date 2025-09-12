using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarModelManager : MonoBehaviour
{
    
    [SerializeField] CarModel[] models;
    GameObject holo, model;
    public void Setup(int modelIndex, Color playerColor){
        this.holo = transform.GetChild(0).gameObject;
        this.model = transform.GetChild(1).gameObject;

        for(int i = 0; i < models.Length; i++){
            if(models[i].id == modelIndex){
                Destroy(model.transform.GetChild(2).gameObject); // Destroy the old model
                Instantiate(models[i].modelPref, model.transform); // Instantiate the new model

                break;
            }
        }
        SetColour(playerColor);
    }
    public void SetColour(Color color){
        Material colorMat = transform.GetChild(2).GetComponent<Renderer>().material;
        color.a = Mathf.Min(0.666f, color.a); //cap alpha to 0.666
        colorMat.color = color;
    }

    public void ShowTrustedModel(bool trusted){
        this.holo.SetActive(!trusted);
        this.model.SetActive(trusted);
    }

    [System.Serializable] class CarModel {
        public int id;
        public GameObject modelPref;
    }
}
