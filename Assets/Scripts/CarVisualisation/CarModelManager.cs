using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarModelManager : MonoBehaviour
{
    GameObject holo, model;
    // Start is called before the first frame update
    void Start()
    {
        holo = transform.GetChild(0).gameObject;
        model = transform.GetChild(1).gameObject;
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
