using UnityEngine;

public class AbilityController : MonoBehaviour
{
    CarController carController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(CarController controller){
        carController = controller;
    }

    public CarController GetCarController(){
        return carController;
    }
}
