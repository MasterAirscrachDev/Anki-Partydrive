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

                Material[] materials = model.transform.GetChild(2).GetComponent<Renderer>().materials; // Get the materials of the new model
                //model.transform.GetChild(2).GetComponent<Renderer>().material.color = models[i].defaultColor; // Set the default color
                materials[0].color = models[i].defaultColor; // Set the default color
                model.transform.GetChild(2).GetComponent<Renderer>().materials = materials; // Set the new materials
                break;
            }
        }
    }

    public void ShowTrustedModel(bool trusted){
        this.holo.SetActive(!trusted);
        this.model.SetActive(trusted);
    }

    [System.Serializable] class CarModel {
        public int id;
        public GameObject modelPref;
        public Color defaultColor;
    }
}
