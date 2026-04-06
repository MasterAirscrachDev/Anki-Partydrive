using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHazardZone : MonoBehaviour
{
    [SerializeField] int energyDrain = 4; //can be negative or positive
    [SerializeField] int speedModifier = 0; //can be negative or positive
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
        SR.gas?.RegisterHazard(transform);
        Destroy(gameObject, lifetime);
    }
    
    void OnDestroy()
    {
        SR.gas?.UnregisterHazard(transform);
    }

    // Update is called once per frame
    void Update()
    {
        ApplyHazardEffect();
    }
    void ApplyHazardEffect()
    {
        List<CarController> hits = SR.cms.CubeCheckControllers(transform.position, transform.forward, hazardRange);
        foreach(CarController hit in hits){
            if(hit == owner) continue; //don't hit self
            if(affectedControllers.Contains(hit)) continue; //already affected
            affectedControllers.Add(hit);
            if(energyDrain != 0){
                if(energyDrain < 0) { 
                    hit.UseEnergy(energyDrain); 
                    // Report damage back to the owner directly
                    owner?.RecordDamageDealt(-energyDrain);
                }
                else { hit.ChargeEnergy(energyDrain); }
            }
            if(speedModifier != 0)
            { hit.AddSpeedModifier(new FlatSpeedModifier(speedModifier, speedModifierDuration, modifierTag)); }
        }
    }
}
