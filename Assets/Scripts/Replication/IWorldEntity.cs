using System.Collections.Generic;
using UnityEngine;

public interface IWorldEntity
{
    string EntityId { get; }
    string EntityType { get; }
    WorldEntityData GetEntityData();
}

// Optional interface for ability scripts that want to add type-specific data to
// an AbilityController entity snapshot (e.g. missile target position).
public interface IAbilityExtras
{
    void PopulateExtras(Dictionary<string, string> extras);
}

[System.Serializable]
public class WorldEntityData
{
    public string id;
    public string type;
    public string ownerId;
    public float px, py, pz;
    public float rx, ry, rz, rw;
    public float sx, sy, sz;
    public Dictionary<string, string> extras;

    public static WorldEntityData Build(string id, string type, Transform t,
        string ownerId = null, Dictionary<string, string> extras = null)
    {
        return new WorldEntityData
        {
            id = id, type = type, ownerId = ownerId,
            px = t.position.x, py = t.position.y, pz = t.position.z,
            rx = t.rotation.x, ry = t.rotation.y, rz = t.rotation.z, rw = t.rotation.w,
            sx = t.localScale.x, sy = t.localScale.y, sz = t.localScale.z,
            extras = extras
        };
    }
}
