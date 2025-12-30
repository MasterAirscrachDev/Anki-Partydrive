using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHazardZone : MonoBehaviour
{
    [SerializeField] float tickRate = 0.5f; //time between hazard ticks
    [SerializeField] int energyChange = -5; //can be negative or positive
    [SerializeField] int speedModifier = 0; //can be negative or positive
    [SerializeField] bool speedIsMultiplier = false;
    [SerializeField] float speedModifierDuration = 1f;
    [SerializeField] string modifierTag = "HazardZone";
    float timeSinceLastTick = 0f;
    float hazardRange;
    CarController owner;
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
        timeSinceLastTick += Time.deltaTime;
        if(timeSinceLastTick >= tickRate){
            timeSinceLastTick = 0f;
            ApplyHazardEffect();
        }
    }
    void ApplyHazardEffect()
    {
        List<CarController> hits = CMS.cms.CubeCheckControllers(transform.position, transform.forward, hazardRange);
        foreach(CarController hit in hits){
            if(hit == owner) continue; //don't hit self
            if(energyChange != 0){
                if(energyChange < 0) { hit.UseEnergy(-energyChange);  } //negative to use energy
                else { hit.ChargeEnergy(energyChange); }
            }
            if(speedModifier != 0)
            {
                hit.AddSpeedModifier(speedModifier, speedIsMultiplier, speedModifierDuration, modifierTag);
            }
        }
    }
}
