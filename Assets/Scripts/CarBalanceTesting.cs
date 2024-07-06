using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarBalanceTesting : MonoBehaviour
{
    [SerializeField] int finishes = -1;
    float startTime;
    int carIndex = 0;
    public void Test(){
        CarInteraface carInterface = FindObjectOfType<CarInteraface>();
        //get the first car that isnt on charge
        while(carInterface.cars[carIndex].charging && carIndex < carInterface.cars.Length){ carIndex++; }
        if(carIndex == carInterface.cars.Length){ return; }
        carInterface.ControlCar(carInterface.cars[carIndex], 550, 0);
        finishes = -1;
    }
    public void CrossedFinish(){
        finishes++;
        Debug.Log("Finishes: " + finishes);
        if(finishes == 0){
            startTime = Time.time;
        }
        if(finishes == 5){
            float timeTaken = Time.time - startTime;
            //convert to mm:ss:ms
            int m = 0;
            while(timeTaken >= 60){
                timeTaken -= 60;
                m++;
            }
            Debug.Log($"Time taken: {m}:{timeTaken}");
            CarInteraface carInterface = FindObjectOfType<CarInteraface>();
            carInterface.ControlCar(carInterface.cars[carIndex], 0, 0);
        }
    }
}
//temp for testing ss, lower = better
//sport laptime: 2:47.3495 (1. 2:46.99922) (2. 2:56.57298)
//the sandblaster: 2:42.1987  
//fucking minecraft car: 2:38.8149
//truck: 2:28.4687 (1. 2:31.83606) (2. 2:33.56726) (3. 2:37.33946)