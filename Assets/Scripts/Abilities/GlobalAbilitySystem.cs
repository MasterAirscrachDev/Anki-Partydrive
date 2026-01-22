using System.Collections.Generic;
using UnityEngine;

public class GlobalAbilitySystem : MonoBehaviour
{
    [SerializeField] List<AbilityTexturePair> abilityTextures;
    [SerializeField] GameObject missilePrefab, seekingMissilePrefab;
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
    public void SpawnMissile(CarController control, Vector3 target)
    {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(missilePrefab, start, Quaternion.identity);
        missile.GetComponent<AbilityController>().Setup(control);
        missile.GetComponent<AbilityMissile>().Setup(target);
    }
    public void SpawnSeekingMissile(CarController control, string targetID)
    {
        Vector3 start = SR.cet.GetCarVisualPosition(control.GetID());
        GameObject missile = Instantiate(seekingMissilePrefab, start, Quaternion.identity);
        missile.GetComponent<AbilityController>().Setup(control);
        missile.GetComponent<AbilityMissile>().Setup(SR.cet.GetCarVisualTransform(targetID));
    }
    class AbilityTexturePair
    {
        public Ability ability;
        public Sprite texture;
    }
}
