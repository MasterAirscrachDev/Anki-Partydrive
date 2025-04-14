using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCameraTester : MonoBehaviour
{
    [SerializeField] TrackCamera trackCamera;
    [ContextMenu("Test Camera")]
    public void TestCamera(){
        trackCamera.TrackUpdated(transform.position, new Vector2(transform.localScale.x, transform.localScale.z));
    }
}
