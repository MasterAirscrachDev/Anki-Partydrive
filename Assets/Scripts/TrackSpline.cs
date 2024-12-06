using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSpline : MonoBehaviour
{
    [SerializeField] Vector3[] leftPoints, RightPoints;
    public Vector3 GetPoint(float t, float offset){
        offset = Mathf.Clamp(offset, -70, 70);
        //scale offset to 0-1
        offset = (offset + 70) / 140;


        Vector3[] points = new Vector3[leftPoints.Length];
        for(int i = 0; i < points.Length; i++){
            points[i] = Vector3.Lerp(leftPoints[i], RightPoints[i], offset);
        }

        return transform.TransformPoint(CurveInterpolator.Cilp(points, t));
    }
}
