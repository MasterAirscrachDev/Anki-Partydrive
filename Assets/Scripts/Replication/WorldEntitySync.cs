using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldEntitySync : MonoBehaviour
{
    public static WorldEntitySync Instance { get; private set; }

    readonly Dictionary<string, IWorldEntity> _entities = new();

    public event Action<WorldEntityData> OnEntityAdded;
    public event Action<string> OnEntityRemoved;

    public int EntityCount => _entities.Count;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(IWorldEntity entity)
    {
        if (entity == null || _entities.ContainsKey(entity.EntityId)) return;
        _entities[entity.EntityId] = entity;
        OnEntityAdded?.Invoke(entity.GetEntityData());
    }

    public void Unregister(string entityId)
    {
        if (string.IsNullOrEmpty(entityId) || !_entities.ContainsKey(entityId)) return;
        _entities.Remove(entityId);
        OnEntityRemoved?.Invoke(entityId);
    }

    // Returns a point-in-time snapshot of all tracked entities.
    public WorldEntityData[] GetSnapshot()
    {
        var snapshot = new WorldEntityData[_entities.Count];
        int i = 0;
        foreach (var e in _entities.Values)
            snapshot[i++] = e.GetEntityData();
        return snapshot;
    }
}
