using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarModelManager : MonoBehaviour
{
    [SerializeField] CarModel[] models;
    GameObject holo, model;
    public void Setup(int modelIndex){
        this.holo = transform.GetChild(0).gameObject;
        this.model = transform.GetChild(1).gameObject;

        for(int i = 0; i < models.Length; i++){
            if(models[i].id == modelIndex){
                Destroy(model.transform.GetChild(2).gameObject); // Destroy the old model
                Instantiate(models[i].modelPref, model.transform); // Instantiate the new model
                break;
            }
        }
    }

    public void SetHolo(bool holo){
        this.holo.SetActive(holo);
        this.model.SetActive(!holo);
    }

    [System.Serializable] class CarModel {
        public int id;
        public GameObject modelPref;
        public Color defaultColor;
    }
}
