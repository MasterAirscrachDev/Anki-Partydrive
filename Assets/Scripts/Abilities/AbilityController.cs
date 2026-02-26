using UnityEngine;
using static OverdriveServer.NetStructures;

public class AbilityController : MonoBehaviour
{
    CarController carController;
    
    // Sets up the ability with the given car controller as the owner
    public void Setup(CarController controller){
        carController = controller;
    }

    public CarController GetCarController(){
        return carController;
    }
    
    /// <summary>
    /// Report damage dealt by this ability back to the owner.
    /// The CarController/PlayerStats will handle big damage announcements.
    /// </summary>
    /// <param name="amount">The amount of damage dealt</param>
    public void ReportDamage(float amount)
    {
        if(carController == null || amount <= 0) return;
        
        // Record damage dealt in the car's stats - PlayerStats handles big damage detection
        carController.RecordDamageDealt(amount);
    }
}
public enum Ability
{
    None = 0, Missle1 = 1, Missle2 = 2, Missle3 = 3, MissleSeeking1 = 4, MissleSeeking2 = 5, MissleSeeking3 = 6,
    EMP = 7, Recharger = 8, TrailDamage = 9, TrailSlow = 10, Overdrive = 11, CrasherBoost = 12, OrbitalLazer = 13, 
    Grappler = 14, LightningPower = 15, TrafficCone = 16
}
public enum AIAbilityUsageMode
{
    Any = 0, Close = 1, Ahead = 2, Behind = 3,
}