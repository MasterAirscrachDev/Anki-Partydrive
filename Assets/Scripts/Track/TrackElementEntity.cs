using System;
using UnityEngine;

// Added to track element GameObjects when spawned by TrackElementManager.
// Self-registers/unregisters with WorldEntitySync automatically.
public class TrackElementEntity : MonoBehaviour, IWorldEntity
{
    string _entityId;
    public string EntityId => _entityId;

    string _entityType = "TrackElement";
    public string EntityType => _entityType;

    void Awake()
    {
        _entityId = Guid.NewGuid().ToString();
    }

    public void Setup(string elementType)
    {
        _entityType = elementType;
    }

    void Start()
    {
        SR.wes?.Register(this);
    }

    void OnDestroy()
    {
        SR.wes?.Unregister(_entityId);
    }

    public WorldEntityData GetEntityData()
    {
        return WorldEntityData.Build(_entityId, _entityType, transform);
    }
}
