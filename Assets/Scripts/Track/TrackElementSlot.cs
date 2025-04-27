using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackElementSlot : MonoBehaviour
{
    [SerializeField] TrackElementType type = TrackElementType.Any;
    void OnDrawGizmos(){
        Gizmos.color = type == TrackElementType.Any ? Color.blue : Color.green;
        Gizmos.DrawSphere(transform.position, 0.01f);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f);
    }
    public enum TrackElementType{
        Any, Positive
    }
}
