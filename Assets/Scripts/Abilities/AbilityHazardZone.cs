using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHazardZone : MonoBehaviour
{
    [SerializeField] int energyChange = -5; //can be negative or positive
    [SerializeField] int speedModifier = 0; //can be negative or positive
    [SerializeField] bool speedIsMultiplier = false;
    [SerializeField] float speedModifierDuration = 1f;
    [SerializeField] string modifierTag = "HazardZone";
    float hazardRange;
    CarController owner;
    List<CarController> affectedControllers = new List<CarController>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Setup(float hazardRange, float lifetime, CarController owner)
    {
        this.hazardRange = hazardRange;
        this.owner = owner;
        Destroy(gameObject, lifetime);
    }

    // Update is called once per frame
    void Update()
    {
        ApplyHazardEffect();
    }
    void ApplyHazardEffect()
    {
        List<CarController> hits = SR.cms.CubeCheckControllers(transform.position, transform.forward, hazardRange);
        AbilityController abilityController = GetComponentInParent<AbilityController>();
        foreach(CarController hit in hits){
            if(hit == owner) continue; //don't hit self
            if(affectedControllers.Contains(hit)) continue; //already affected
            affectedControllers.Add(hit);
            if(energyChange != 0){
                if(energyChange < 0) { 
                    hit.UseEnergy(-energyChange);  //negative to use energy
                    // Report damage back to the ability owner
                    abilityController?.ReportDamage(-energyChange);
                }
                else { hit.ChargeEnergy(energyChange); }
            }
            if(speedModifier != 0)
            {
                hit.AddSpeedModifier(speedModifier, speedIsMultiplier, speedModifierDuration, modifierTag);
            }
        }
    }
}
