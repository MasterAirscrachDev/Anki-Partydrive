using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCameraTester : MonoBehaviour
{
    [SerializeField] TrackCamera trackCamera;
    [ContextMenu("Test Camera")]
    public void TestCamera(){
        if(transform.childCount == 0) return;
        
        // Calculate accurate bounds using min/max of all child positions
        var (center, size) = CalculateTrackBounds();
        
        trackCamera.gameObject.SetActive(true);
        trackCamera.TrackUpdated(center, size);
        DebugDrawTrackBounds();
    }
    
    /// <summary>
    /// Calculate track bounds using proper min/max detection.
    /// Returns center position and half-extents (size from center to edge).
    /// </summary>
    (Vector3 center, Vector2 size) CalculateTrackBounds(){
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        
        foreach(Transform child in transform){
            min = Vector3.Min(min, child.position);
            max = Vector3.Max(max, child.position);
        }
        
        Vector3 center = (min + max) / 2f;
        // Size is half-extents plus 0.5f padding (each track piece is ~1m)
        Vector2 size = new Vector2(
            (max.x - min.x) / 2f + 0.5f,
            (max.z - min.z) / 2f + 0.5f
        );
        
        return (center, size);
    }

    void DebugDrawTrackBounds(){
        if(transform.childCount == 0) return;
        
        var (center, size) = CalculateTrackBounds();
        
        Debug.DrawLine(center + new Vector3(-size.x, 0, -size.y), center + new Vector3(size.x, 0, -size.y), Color.red, 30f);
        Debug.DrawLine(center + new Vector3(size.x, 0, -size.y), center + new Vector3(size.x, 0, size.y), Color.red, 30f);
        Debug.DrawLine(center + new Vector3(size.x, 0, size.y), center + new Vector3(-size.x, 0, size.y), Color.red, 30f);
        Debug.DrawLine(center + new Vector3(-size.x, 0, size.y), center + new Vector3(-size.x, 0, -size.y), Color.red, 30f);
    }
}
