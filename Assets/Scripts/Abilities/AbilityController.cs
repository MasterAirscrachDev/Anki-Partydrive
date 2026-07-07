using System;
using System.Collections.Generic;
using UnityEngine;
using static OverdriveServer.NetStructures;

public class AbilityController : MonoBehaviour, IWorldEntity
{
    CarController carController;

    string _entityId;
    public string EntityId => _entityId;

    string _entityType = "UnknownAbility";
    public string EntityType => _entityType;

    bool _didRegister;

    void Awake()
    {
        _entityId = Guid.NewGuid().ToString();
    }

    void Start()
    {
        // Defer to a sibling IWorldEntity if one exists (e.g. AbilityGrappler).
        if (HasOtherWorldEntity()) return;

        // Auto-detect type from sibling ability script if not set explicitly.
        if (_entityType == "UnknownAbility")
        {
            foreach (var c in GetComponents<MonoBehaviour>())
            {
                if (c == this) continue;
                string name = c.GetType().Name;
                if (name.StartsWith("Ability")) { _entityType = name; break; }
            }
        }

        SR.wes?.Register(this);
        _didRegister = true;
    }

    void OnDestroy()
    {
        if (_didRegister) SR.wes?.Unregister(_entityId);
    }

    bool HasOtherWorldEntity()
    {
        foreach (var c in GetComponents<MonoBehaviour>())
            if (c != this && c is IWorldEntity) return true;
        return false;
    }

    public void Setup(CarController controller)
    {
        carController = controller;
    }

    // Called by specific ability scripts to name themselves.
    public void SetEntityType(string type)
    {
        _entityType = type;
    }

    public CarController GetCarController()
    {
        return carController;
    }

    public WorldEntityData GetEntityData()
    {
        var extras = new Dictionary<string, string>();
        GetComponent<IAbilityExtras>()?.PopulateExtras(extras);
        return WorldEntityData.Build(_entityId, _entityType, transform,
            carController?.GetID(), extras.Count > 0 ? extras : null);
    }

    public void ReportDamage(float amount)
    {
        if (carController == null || amount <= 0) return;
        carController.RecordDamageDealt(amount);
    }
}
public enum Ability
{
    None = 0, Missle1 = 1, Missle2 = 2, Missle3 = 3, MissleSeeking1 = 4, MissleSeeking2 = 5, MissleSeeking3 = 6,
    EMP = 7, Recharger = 8, TrailDamage = 9, TrailSlow = 10, Overdrive = 11, CrasherBoost = 12, OrbitalLazer = 13,
    Grappler = 14, LightningPower = 15, TrafficCone = 16, BubbleShield = 17, IceWall = 18
}
public enum AIAbilityUsageMode
{
    Any = 0, Close = 1, Ahead = 2, Behind = 3,
}
public enum AIAbilityAimMode
{
    Any = 0, Narrow = 1, Wide = 2,
}
