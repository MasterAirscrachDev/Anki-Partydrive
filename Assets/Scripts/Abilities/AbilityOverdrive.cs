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
            StartCoroutine(DoNuke(car));
        }
        else{ // missile storm (temp for wip cars)
            StartCoroutine(DoMissileStorm(car));
        }
    }

    void DoFrozenFrontier(CarController car)
    {
        SR.gas.SpawnIceberg(car, 1, 8, 5f, true);
        Destroy(gameObject); // Destroy the ability object immediately since the effect is handled by the iceberg itself
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
        Destroy(gameObject); // Destroy the ability object after the effect is done
    }
    IEnumerator DoNuke(CarController car)
    {
        SR.sfx.PlaySFX(SFXEvent.NukeExplosion);
        Vector3 carPosition = SR.cet.GetCarVisualPosition(car.GetID());
        SR.gas.SpawnNukeParticles(carPosition);
        //apply Meltdown modifier to all cars within 15 units
        List<CarController> cars = SR.cms.SphereCheckControllers(carPosition, 15f);
        foreach(CarController c in cars)
        { if(c.GetID() != car.GetID()) { c.SetStatusEffect(CarStatus.Meltdown, 6f); } }
        int carCount = cars.Count -2; //dont count self and one other car

        int trackLength = SR.track.GetTrackLength();
        yield return new WaitForSeconds(2f); //wait for 2s before spawning counters
        for(int i = 0; i < carCount; i++)
        {
            //get an index along the track spaced by the track length divided by the car count, with some random offset
            int offset = Random.Range(-trackLength/10, trackLength/10) + (i * trackLength / carCount);
            offset = (offset + trackLength) % trackLength; //wrap around the track length
            TrackCoordinate spawn = new TrackCoordinate(offset, Random.Range(-50f, 50f), Random.Range(0f, 1f));
            GameObject repair = SR.gas.SpawnRepair(spawn);
            Destroy(repair, 10f); // Destroy the repair after 10 seconds to clean up
        }

        Destroy(gameObject); // Destroy the ability object immediately since the effect is handled by the particles and status effects
    }
}