using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoCarController : MonoBehaviour
{
    [SerializeField] int speed, lane;
    [SerializeField] bool update;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(update){
            update = false;
            CarInteraface carInterface = GetComponent<CarInteraface>();
            string id = carInterface.cars[0].id;
            carInterface.ControlCar(id, speed, lane);
        }
    }
}
