using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Car Management System
public class CMS : MonoBehaviour
{
    int controlCount = 0;
    public readonly List<CarController> controllers = new List<CarController>();
    public void AddController(CarController controller){
        controllers.Add(controller);
        controller.SetControlIndex(controlCount);
        controlCount++;
    }
    
}