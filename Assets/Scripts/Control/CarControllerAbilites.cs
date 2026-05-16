using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public partial class CarController : MonoBehaviour
{
#region ABILITIES
    Ability currentAbility = Ability.None; bool doingPickupAnim;
    void OnItembox() {
        if(currentAbility != Ability.None || doingPickupAnim){ return; } //already have an ability
        else {
            doingPickupAnim = true;
            StartCoroutine(DoNewAbilityAnimation());
        }
    }
    void UseAbility() {
        if(currentAbility == Ability.None || doingPickupAnim || GetStatusEffect(CarStatus.Locked)){ return; } //no ability to use
        else {
            int customLights = 0;
            if(currentAbility == Ability.Missle3){ 
                SR.gas.SpawnMissile(this, inputs.specialAim > 0.5f ? 3.6f : inputs.specialAim < -0.5f ? -0.5f : 1.8f);
                SetAbilityImmediate(Ability.Missle2); customLights = 1;
            }
            else if(currentAbility == Ability.Missle2){ 
                SR.gas.SpawnMissile(this, inputs.specialAim > 0.5f ? 3.6f : inputs.specialAim < -0.5f ? -0.5f : 1.8f);
                SetAbilityImmediate(Ability.Missle1); customLights = 1;
            }
            else if(currentAbility == Ability.Missle1){ 
                SR.gas.SpawnMissile(this, inputs.specialAim > 0.5f ? 3.6f : inputs.specialAim < -0.5f ? -0.5f : 1.8f);
                SetAbilityImmediate(Ability.None); customLights = 1;
            }
            else if(currentAbility == Ability.MissleSeeking3){ SR.gas.SpawnSeekingMissile(this, inputs.specialAim < -0.5f); SetAbilityImmediate(Ability.MissleSeeking2); customLights = 2; }
            else if(currentAbility == Ability.MissleSeeking2){ SR.gas.SpawnSeekingMissile(this, inputs.specialAim < -0.5f); SetAbilityImmediate(Ability.MissleSeeking1); customLights = 2; }
            else if(currentAbility == Ability.MissleSeeking1){ SR.gas.SpawnSeekingMissile(this, inputs.specialAim < -0.5f); SetAbilityImmediate(Ability.None); customLights = 2; }
            else if(currentAbility == Ability.EMP){ 
                SR.gas.SpawnEMP(this); SetAbilityImmediate(Ability.None); 
                customLights = 3;
                // Flash blue engine light when using EMP
                LightData[] empLights = new LightData[3];
                empLights[0] = LightData.ClearFor(LightChannel.RED);
                empLights[1] = LightData.ClearFor(LightChannel.GREEN);
                empLights[2] = LightData.L(LightChannel.BLUE, LightEffect.THROB, 0, 14, 10);
                SetEngineLight(empLights, 0.6f, 2); //chargeup effect
                empLights = new LightData[1];
                empLights[0] = LightData.L(LightChannel.BLUE, LightEffect.FLASH, 4, 9, 128);
                StartCoroutine(SetEngineLightDelayed(empLights, 0.25f, 0.5f, 3)); // Flash blue after a short delay to sync with EMP explosion
            }
            else if(currentAbility == Ability.TrailDamage){ SR.gas.SpawnDamageTrail(this); SetAbilityImmediate(Ability.None); 
                LightData[] lightarr = new LightData[1];
                lightarr[0] = LightData.L(LightChannel.FRONT_RED, LightEffect.THROB, 4, 9, 15);
                SetHeadLights(lightarr, 2f, 2); customLights = 3;
            }
            else if(currentAbility == Ability.TrailSlow){ SR.gas.SpawnSlowTrail(this); SetAbilityImmediate(Ability.None); 
                LightData[] lightarr = new LightData[2];
                lightarr[0] = LightData.L(LightChannel.FRONT_GREEN, LightEffect.THROB, 2, 8, 15 );
                lightarr[1] = LightData.L(LightChannel.FRONT_RED, LightEffect.STEADY, 1, 3, 10 );
                SetHeadLights(lightarr, 2f, 2); customLights = 3;
            }
            else if(currentAbility == Ability.OrbitalLazer){ SR.gas.SpawnOrbitalLazer(this); SetAbilityImmediate(Ability.None); 
                LightData[] lightarr = new LightData[3];
                lightarr[0] = LightData.L(LightChannel.RED, LightEffect.FADE, 0, 14, 10 );
                lightarr[1] = LightData.L(LightChannel.BLUE, LightEffect.FADE, 0, 14, 10 );
                lightarr[2] = LightData.L(LightChannel.GREEN, LightEffect.FADE, 0, 14, 10 );
                SetHeadLights(lightarr, 1.1f, 2); customLights = 3;
            }
            else if(currentAbility == Ability.CrasherBoost){ SR.gas.SpawnCrasherBoost(this, inputs.specialAim < -0.5f); SetAbilityImmediate(Ability.None);  }
            else if(currentAbility == Ability.Grappler){ SR.gas.SpawnGrappler(this); SetAbilityImmediate(Ability.None); }
            else if(currentAbility == Ability.LightningPower){ SR.gas.SpawnLightningPower(this); SetAbilityImmediate(Ability.None); }
            else if(currentAbility == Ability.Recharger){ SR.gas.SpawnRecharger(this); SetAbilityImmediate(Ability.None); }
            else if(currentAbility == Ability.TrafficCone){ SR.gas.SpawnTrafficCone(this); SetAbilityImmediate(Ability.None);
                LightData[] lightarr = new LightData[2];
                lightarr[0] = LightData.L(LightChannel.FRONT_RED, LightEffect.FADE, 1, 14, 10 );
                lightarr[1] = LightData.L(LightChannel.FRONT_GREEN, LightEffect.FADE, 2, 14, 10 );
                SetHeadLights(lightarr, 0.5f, 2); customLights = 3;
            }
            else if(currentAbility == Ability.IceBlast){ SR.gas.SpawnIceberg(this); SetAbilityImmediate(Ability.None); 
            }
            else if(currentAbility == Ability.Overdrive){ SR.gas.SpawnOverdrive(this); SetAbilityImmediate(Ability.None); 
            }
            if(customLights < 3){ //batched effects or abilites without defined lights
                if(customLights == 1) { //regular missiles
                    LightData[] lightarr = new LightData[2];
                    lightarr[0] = LightData.L(LightChannel.FRONT_RED, LightEffect.FADE, 5, 14, 60 );
                    lightarr[1] = LightData.L(LightChannel.FRONT_GREEN, LightEffect.FADE, 5, 14, 60 );
                    SetHeadLights(lightarr, 0.5f, 2);
                } else if(customLights == 2){ //seeking missiles

                } else { // Fallback effect if no custom effect defined for this ability
                    LightData[] lights = new LightData[2];
                    lights[0] = new LightData{channel = LightChannel.FRONT_RED, effect= LightEffect.FLASH, startStrength= 8, endStrength = 11, cyclesPer10Seconds = 128};
                    lights[1] = new LightData{channel = LightChannel.FRONT_GREEN, effect= LightEffect.FLASH, startStrength= 4, endStrength = 11, cyclesPer10Seconds = 64};
                    SetHeadLights(lights, 1.5f);
                }
            }
        }
    }
    IEnumerator DoNewAbilityAnimation()
    {
        Ability[] rareAbilities = new Ability[] { 
            Ability.OrbitalLazer,
            Ability.LightningPower,
            Ability.Overdrive
        };  
        // Default ability list (balanced)
        Ability[] validAbilities = new Ability[] { 
            Ability.Missle3, Ability.MissleSeeking3, 
            Ability.EMP, 
            Ability.TrailDamage, Ability.TrailSlow, 
            Ability.CrasherBoost,
            Ability.Grappler,
            Ability.Recharger,
            Ability.TrafficCone,
            Ability.IceBlast
        };
        
        // First place abilities (worse items)
        Ability[] firstPlaceAbilities = new Ability[] { 
            Ability.Missle3, Ability.TrailSlow, 
            Ability.EMP, Ability.TrailDamage,
            Ability.Recharger, Ability.TrafficCone
        };
        
        // Last place abilities (better items)
        Ability[] lastPlaceAbilities = new Ability[] { 
            Ability.MissleSeeking3,
            Ability.MissleSeeking3,
            Ability.MissleSeeking3,
            Ability.CrasherBoost,
            Ability.Grappler,
            Ability.Grappler,
            Ability.IceBlast,
            Ability.IceBlast,
            Ability.Overdrive,
            Ability.Overdrive
        };
        
        //over 1 second, cycle through abilities every 0.1 second (always use default list for animation)
        float animationDuration = 1f;
        float cycleInterval = 0.1f;
        float elapsed = 0f;
        while(elapsed < animationDuration)
        {
            Ability prospectiveAbility = validAbilities[Random.Range(0, validAbilities.Length)];
            //update UI here
            if(pcs != null)
            { pcs.SetAbilityIcon(prospectiveAbility); }
            yield return new WaitForSeconds(cycleInterval);
            elapsed += cycleInterval;
        }
        
        // Determine final ability based on position
        Ability[] finalAbilityList = validAbilities;
        bool isFirst = false;
        // Check if this car is first or last
        if(!string.IsNullOrEmpty(carID)) {
            int positionCheck = SR.cet.IsFirstOrLast(carID);
            if(positionCheck == 1) {
                isFirst = true;
                // First place - use worse items
                finalAbilityList = firstPlaceAbilities;
            }
            else if(positionCheck == -1)
            {
                // Last place - use better items
                finalAbilityList = lastPlaceAbilities;
            }
        }
        //if we are not first roll a 1/3 odds to add the rare abilities to the pool
        if(!isFirst && Random.Range(0, 3) == 0)
        {
            List<Ability> extendedList = new List<Ability>(finalAbilityList);
            extendedList.AddRange(rareAbilities);
            finalAbilityList = extendedList.ToArray();
        }
        
        //final ability
        currentAbility = finalAbilityList[Random.Range(0, finalAbilityList.Length)];
        if(pcs != null)
        { pcs.SetAbilityIcon(currentAbility); }
        doingPickupAnim = false;
        
        // Track ability pickup in stats
        playerAnalytics?.RecordAbilityPickup();
    }
    public void SetAbilityImmediate(Ability ability)
    {
        currentAbility = ability;
        if(pcs != null)
        { pcs.SetAbilityIcon(currentAbility); }
    }
    public void OnCollectElement(TrackCarCollider.EType type)
    {
        // Handle element collection logic here
        switch(type)
        {
            case TrackCarCollider.EType.EnergyCore:
                ChargeEnergy((maxEnergy + statMaxEnergyMod) * 0.75f);
                break;
            case TrackCarCollider.EType.ItemBox:
                OnItembox();
                break;
            // Add cases for other element types as needed
            default:
                break;
        }
    }
#endregion
}
