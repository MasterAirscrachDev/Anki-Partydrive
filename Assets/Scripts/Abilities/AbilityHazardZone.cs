using System;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHazardZone : MonoBehaviour, IWorldEntity
{
    [SerializeField] int energyDrain = 4;
    [SerializeField] int speedModifier = 0;
    [SerializeField] float speedModifierDuration = 1f;
    [SerializeField] string modifierTag = "HazardZone";
    [SerializeField] float hazardRange = 0.1f;
    CarController owner;
    List<CarController> affectedControllers = new List<CarController>();

    string _entityId;
    public string EntityId => _entityId;
    public string EntityType => "AbilityHazardZone";

    void Awake()
    {
        _entityId = Guid.NewGuid().ToString();
    }

    void Start()
    {
        SR.wes?.Register(this);
    }

    public WorldEntityData GetEntityData()
    {
        var extras = new Dictionary<string, string>
        {
            ["range"] = hazardRange.ToString("F3")
        };
        return WorldEntityData.Build(_entityId, EntityType, transform, owner?.GetID(), extras);
    }

    public void Setup(float lifetime, CarController owner)
    {
        this.owner = owner;
        SR.gas?.RegisterHazard(transform);
        Destroy(gameObject, lifetime);
    }

    void OnDestroy()
    {
        SR.gas?.UnregisterHazard(transform);
        SR.wes?.Unregister(_entityId);
    }

    void Update()
    {
        ApplyHazardEffect();
    }
    void ApplyHazardEffect()
    {
        List<CarController> hits = SR.cms.CubeCheckControllers(transform.position, transform.forward, hazardRange);
        foreach(CarController hit in hits){
            if(hit == owner) continue;
            if(affectedControllers.Contains(hit)) continue;
            affectedControllers.Add(hit);
            if(energyDrain != 0){
                if(energyDrain < 0) {
                    hit.UseEnergy(energyDrain);
                    owner?.RecordDamageDealt(-energyDrain);
                }
                else { hit.ChargeEnergy(energyDrain); }
            }
            if(speedModifier != 0)
            { hit.AddSpeedModifier(new FlatSpeedModifier(speedModifier, speedModifierDuration, modifierTag)); }
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        hazardRange = Mathf.Max(hazardRange, 0.1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(hazardRange, hazardRange, hazardRange));
    }
}
