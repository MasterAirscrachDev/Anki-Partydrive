using System.Collections.Generic;
using UnityEngine;

public class GlobalAbilitySystem : MonoBehaviour
{
    [SerializeField] List<AbilityTexturePair> abilityTextures;
    [SerializeField] GameObject missilePrefab;
    [SerializeField] GameObject MissileParticlePrefab, SeekingMissileParticlePrefab;
    [SerializeField] GameObject empPrefab;
    [SerializeField] GameObject damageTrailPrefab, slowTrailPrefab;
    [SerializeField] GameObject orbitalLazerPrefab;
    [SerializeField] GameObject crasherBoostPrefab;
    [SerializeField] GameObject crasherSlowParticlePrefab;
    [SerializeField] GameObject grapplerPrefab;
    [SerializeField] GameObject lightningPowerPrefab;
    [SerializeField] GameObject rechargerPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public Sprite GetAbilityTexture(Ability ability)
    {
        foreach(AbilityTexturePair atp in abilityTextures)
        {
            if(atp.ability == ability)
            {
                return atp.texture;
            }
        }
        return null;
    }
    /// <summary>
    /// Spawns a missile from the given car towards the target position.
    /// </summary>
    /// <param name="control"></param>
    /// <param name="target"></param>
    public void SpawnMissile(CarController control, Vector3 target) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(missilePrefab, start, Quaternion.identity);
        missile.GetComponent<AbilityController>().Setup(control);
        missile.GetComponent<AbilityMissile>().Setup(target);
        SR.sfx?.PlaySFX(SFXEvent.MissileLaunch);
    }
    /// <summary>
    /// Spawns a missile from the given car that will target a distance forward on the track;
    /// </summary>
    /// <param name="control"></param>
    /// <param name="distanceForward">the distance forward along the track (Segment space)</param>
    public void SpawnMissile(CarController control, float distanceForward = 1.8f) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(missilePrefab);
        missile.transform.position = start;
        missile.GetComponent<AbilityController>().Setup(control);
        TrackCoordinate tc = SR.cet.GetCarTrackCoordinate(control.GetID());

        Vector3 targetPos = SR.track.TrackCoordinateToWorldspace(tc + distanceForward );

        missile.GetComponent<AbilityMissile>().Setup(targetPos);
        
    }
    /// <summary>
    /// Spawns a seeking missile from the given car towards the target car.
    /// </summary>
    /// <param name="control"></param>
    /// <param name="targetID"></param>
    public void SpawnSeekingMissile(CarController control, string targetID) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(missilePrefab);
        missile.transform.position = start;
        missile.GetComponent<AbilityController>().Setup(control);
        missile.GetComponent<AbilityMissile>().Setup(SR.cet.GetCarRealTransform(targetID));
    }
    /// <summary>
    /// Spawns a seeking missile from the given car towards the next car ahead or behind.
    /// </summary>
    /// <param name="control"></param>
    /// <param name="targetBehind">If true, targets car behind instead of ahead</param>
    public void SpawnSeekingMissile(CarController control, bool targetBehind = false) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(missilePrefab);
        missile.transform.position = start;
        missile.GetComponent<AbilityController>().Setup(control);
        
        string targetID;
        if(targetBehind) {
            targetID = SR.cet.GetCarBehind(control.GetID());
        } else {
            targetID = SR.cet.GetCarAhead(control.GetID());
        }
        
        if(targetID == null) return;
        missile.GetComponent<AbilityMissile>().Setup(SR.cet.GetCarRealTransform(targetID));
        
    }
    public void SpawnMissileParticle(Vector3 position, bool seeking) {
        GameObject particle = Instantiate(seeking ? SeekingMissileParticlePrefab : MissileParticlePrefab);
        particle.transform.position = position;
        SR.sfx?.PlaySFX(SFXEvent.Explosion);
    }
    public void SpawnEMP(CarController control) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject emp = Instantiate(empPrefab, start, Quaternion.identity);
        emp.GetComponent<AbilityController>().Setup(control);
        SR.sfx?.PlaySFX(SFXEvent.EMPActivate);
    }
    public void SpawnDamageTrail(CarController control) {
        Transform model = SR.cet.GetCarVisualTransform(control.GetID());
        GameObject trail = Instantiate(damageTrailPrefab, model.position, model.rotation);
        trail.transform.parent = model;
        trail.GetComponent<AbilityController>().Setup(control);
        SR.sfx?.PlaySFX(SFXEvent.TrailDrop);
    }
    public void SpawnSlowTrail(CarController control) {
        Transform model = SR.cet.GetCarVisualTransform(control.GetID());
        GameObject trail = Instantiate(slowTrailPrefab, model.position, model.rotation);
        trail.transform.parent = model;
        trail.GetComponent<AbilityController>().Setup(control);
        SR.sfx?.PlaySFX(SFXEvent.TrailDrop);
    }
    public void SpawnOrbitalLazer(CarController control) {
        // Get the leading car according to the gamemode's position tracking
        CarController firstPlaceCar = SR.cms.GetFirstPlaceCar();
        if(firstPlaceCar == null) return;
        
        string firstPlaceCarID = firstPlaceCar.GetID();
        
        Transform targetTransform = SR.cet.GetCarVisualTransform(firstPlaceCarID);
        if(targetTransform == null) return;
        
        GameObject lazer = Instantiate(orbitalLazerPrefab);
        lazer.GetComponent<AbilityController>().Setup(control);
        lazer.GetComponent<AbilityOrbitalLazer>().Setup(targetTransform, firstPlaceCar);
        SR.sfx?.PlaySFX(SFXEvent.OrbitalLaserCharge);
    }
    
    /// <summary>
    /// Spawns a crasher boost that targets the preceding car or car behind.
    /// Applies 500 boost for 2.5s, then 80% slow for 3s.
    /// </summary>
    /// <param name="targetBehind">If true, targets car behind instead of ahead</param>
    public void SpawnCrasherBoost(CarController control, bool targetBehind = false) {
        string targetID;
        if(targetBehind) {
            targetID = SR.cet.GetCarBehind(control.GetID());
        } else {
            targetID = SR.cet.GetCarAhead(control.GetID());
        }
        
        if(targetID == null) return;
        
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject crasher = Instantiate(crasherBoostPrefab);
        crasher.transform.position = start;
        crasher.GetComponent<AbilityController>().Setup(control);
        crasher.GetComponent<AbilityCrasherBoost>().Setup(SR.cet.GetCarRealTransform(targetID), targetID);
        SR.sfx?.PlaySFX(SFXEvent.CrasherBoostLaunch);
    }
    
    /// <summary>
    /// Spawns a particle effect when CrasherBoost slow is applied.
    /// </summary>
    public void SpawnCrasherSlowParticle(Vector3 position) {
        if(crasherSlowParticlePrefab == null) return;
        GameObject particle = Instantiate(crasherSlowParticlePrefab);
        particle.transform.position = position;
    }
    
    /// <summary>
    /// Spawns a grappler that applies 20% boost on user and 20% reduction on preceding car for 8s.
    /// </summary>
    public void SpawnGrappler(CarController control) {
        string targetID = SR.cet.GetCarAhead(control.GetID());
        if(targetID == null) return;
        
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject grappler = Instantiate(grapplerPrefab);
        grappler.transform.position = start;
        grappler.GetComponent<AbilityController>().Setup(control);
        grappler.GetComponent<AbilityGrappler>().Setup(SR.cet.GetCarVisualTransform(targetID), targetID, control.GetID());
        SR.sfx?.PlaySFX(SFXEvent.GrapplerAttach);
    }
    /// <summary>
    /// Spawns a lightning power that targets all cars except the user.
    /// </summary>
    public void SpawnLightningPower(CarController control) {
        string[] allCarIDs = SR.cet.GetActiveCars(control.GetID()); //get all active cars except the user
        List<CarController> targets = new List<CarController>();
        foreach(string carID in allCarIDs) {
            CarController target = SR.cms.GetController(carID);
            if(target != null) {
                targets.Add(target);
            }
        }
        
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject lightningPower = Instantiate(lightningPowerPrefab);
        lightningPower.transform.position = start;
        lightningPower.GetComponent<AbilityController>().Setup(control);
        lightningPower.GetComponent<AbilityLightningPower>().Setup(targets.ToArray());
    }
    
    /// <summary>
    /// Spawns a recharger that follows the user and gives energy over time.
    /// </summary>
    public void SpawnRecharger(CarController control) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject recharger = Instantiate(rechargerPrefab);
        recharger.transform.position = start;
        recharger.GetComponent<AbilityController>().Setup(control);
        recharger.GetComponent<AbilityRecharger>().Setup(control);
        SR.sfx?.PlaySFX(SFXEvent.RechargerActivate);
    }

    [System.Serializable]
    class AbilityTexturePair
    {
        public Ability ability;
        public Sprite texture;
    }
}
