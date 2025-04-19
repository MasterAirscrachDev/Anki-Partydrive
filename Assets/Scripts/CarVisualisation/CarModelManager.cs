using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarModelManager : MonoBehaviour
{
    [SerializeField] Mesh[] models;
    GameObject holo, model;
    public void Setup(int modelIndex){
        this.holo = transform.GetChild(0).gameObject;
        this.model = transform.GetChild(1).gameObject;

        //Add more models as available
        if(modelIndex == 18){ //mammoth
            holo.transform.GetChild(2).GetComponent<MeshFilter>().mesh = models[1];
            model.transform.GetChild(2).GetComponent<MeshFilter>().mesh = models[1];
        }
    }

    public void SetHolo(bool holo){
        this.holo.SetActive(holo);
        this.model.SetActive(!holo);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
