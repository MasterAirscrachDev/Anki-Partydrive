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
public enum Ability
{
    None = 0, Missle1 = 1, Missle2 = 2, Missle3 = 3, MissleSeeking1 = 4, MissleSeeking2 = 5, MissleSeeking3 = 6,
    EMP = 7, Shield = 8, TrailDamage = 9, TrailSlow = 10, Overdrive = 11, CrasherBoost = 12,
}
public enum AIAbilityUsageMode
{
    Any = 0, Close = 1, Ahead = 2, Behind = 3,
}