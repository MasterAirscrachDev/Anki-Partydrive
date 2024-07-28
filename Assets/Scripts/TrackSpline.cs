using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSpline : MonoBehaviour
{
    [SerializeField] Vector3[] points;
    public Vector3 GetPoint(float t){
        return transform.TransformPoint(CurveInterpolator.Cilp(points, t));
    }
}
