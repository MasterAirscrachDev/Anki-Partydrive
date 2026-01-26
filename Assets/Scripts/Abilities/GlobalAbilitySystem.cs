using System.Collections.Generic;
using UnityEngine;

public class GlobalAbilitySystem : MonoBehaviour
{
    [SerializeField] List<AbilityTexturePair> abilityTextures;
    [SerializeField] GameObject missilePrefab;
    [SerializeField] GameObject MissileParticlePrefab, SeekingMissileParticlePrefab;
    [SerializeField] GameObject empPrefab;
    [SerializeField] GameObject damageTrailPrefab, slowTrailPrefab;
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
    }
    /// <summary>
    /// Spawns a missile from the given car that will target a distance forward on the track;
    /// </summary>
    /// <param name="control"></param>
    /// <param name="distanceForward">the distance forward along the track (Segment space)</param>
    public void SpawnMissile(CarController control, float distanceForward = 1.2f) {
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
        missile.GetComponent<AbilityMissile>().Setup(SR.cet.GetCarVisualTransform(targetID));
    }
    /// <summary>
    /// Spawns a seeking missile from the given car towards the next car ahead.
    /// </summary>
    /// <param name="control"></param>
    public void SpawnSeekingMissile(CarController control) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(missilePrefab);
        missile.transform.position = start;
        missile.GetComponent<AbilityController>().Setup(control);
        string targetID = SR.cet.GetCarAhead(control.GetID());
        missile.GetComponent<AbilityMissile>().Setup(SR.cet.GetCarVisualTransform(targetID));
    }
    public void SpawnMissileParticle(Vector3 position, bool seeking) {
        GameObject particle = Instantiate(seeking ? SeekingMissileParticlePrefab : MissileParticlePrefab);
        particle.transform.position = position;
    }
    public void SpawnEMP(CarController control) {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject emp = Instantiate(empPrefab, start, Quaternion.identity);
        emp.GetComponent<AbilityController>().Setup(control);
        //emp.GetComponent<AbilityEMP>().Setup();
    }
    public void SpawnDamageTrail(CarController control) {
        Transform model = SR.cet.GetCarVisualTransform(control.GetID());
        GameObject trail = Instantiate(damageTrailPrefab, model.position, model.rotation);
        trail.transform.parent = model;
        trail.GetComponent<AbilityController>().Setup(control);
        //trail.GetComponent<AbilityDamageTrail>().Setup();
    }
    public void SpawnSlowTrail(CarController control) {
        Transform model = SR.cet.GetCarVisualTransform(control.GetID());
        GameObject trail = Instantiate(slowTrailPrefab, model.position, model.rotation);
        trail.transform.parent = model;
        trail.GetComponent<AbilityController>().Setup(control);
        //trail.GetComponent<AbilitySlowTrail>().Setup();
    }
    [System.Serializable]
    class AbilityTexturePair
    {
        public Ability ability;
        public Sprite texture;
    }
}
