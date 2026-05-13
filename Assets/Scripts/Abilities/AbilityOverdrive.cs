using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures.ModelName;
//This is the overdrive ability, this will give a varying effect based on the model of the car that is using it
//some effects will take a while to implment so if a specific car doesnt have an effect it will get the default
public class AbilityOverdrive : MonoBehaviour
{
    public void Setup(AbilityController ab)
    {
        CarController car = ab.GetCarController();
        if(car.GetCarModel() == IceWave || car.GetCarModel() == x52Ice){ //frozen frontier effect
            DoFrozenFrontier(car);
        }
        else if(car.GetCarModel() == Nuke || car.GetCarModel() == NukePhantom){ //nuke effect
            DoNuke(car);
        }
        else{ // missile storm (temp for wip cars)
            StartCoroutine(DoMissileStorm(car));
        }
    }

    void DoFrozenFrontier(CarController car)
    {
        SR.gas.SpawnIceberg(car, 1, 8, 5f, true);
    }
    IEnumerator DoMissileStorm(CarController car)
    {
        //for 12 seconds spawn a seeking missile every 0.75 seconds
        //get a list of all cars that are not the user and cycle through them with each missile spawn
        float duration = 12f;
        float spawnInterval = 0.75f;
        float elapsed = 0f;
        int targetIndex = 0;
        string[] targets = SR.cet.GetActiveCars(car.GetID());
        while(elapsed < duration)
        {
            if(targets.Length > 0)
            {
                string targetID = targets[targetIndex % targets.Length];
                SR.gas.SpawnSeekingMissile(car, targetID);
                targetIndex++;
            }
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }

    }
    void DoNuke(CarController car)
    {
        SR.gas.SpawnNukeParticles(car.transform.position);
        //apply Meltdown modifier to all cars within 15 units
        List<CarController> cars = SR.cms.SphereCheckControllers(car.transform.position, 15f);
        foreach(CarController c in cars)
        {
            if(c.GetID() != car.GetID())
            {
                c.SetStatusEffect(CarStatus.Meltdown, 6f);
            }
        }
    }
}