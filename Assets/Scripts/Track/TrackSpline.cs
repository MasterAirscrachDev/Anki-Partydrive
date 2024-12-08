using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSpline : MonoBehaviour
{
    [SerializeField] Vector3[] leftPoints, RightPoints;
    [SerializeField] bool simple = true;
    public bool flipped = false;
    public Vector3 GetPoint(float t, float offset){
        return transform.TransformPoint(CurveInterpolator.Cilp(PointsFromOffset(offset), t));
    }
    public float GetLength(float offset){
        if(simple){ return 1f; }
        Vector3[] points = PointsFromOffset(offset);

        float length = 0;
        int steps = 10;
        for(int i = 0; i < steps; i++){
            Vector3 p1 = CurveInterpolator.Cilp(points, i / (float)steps);
            Vector3 p2 = CurveInterpolator.Cilp(points, (i + 1) / (float)steps);
            length += Vector3.Distance(p1, p2);
        }
        return length;
    }

    Vector3[] PointsFromOffset(float offset){
        offset = Mathf.Clamp(offset, -70, 70);
        offset *= 0.9f; //we actually dont want the true edges
        //scale offset to 0-1
        offset = (offset + 70) / 140;

        Vector3[] points = new Vector3[leftPoints.Length];
        for(int i = 0; i < points.Length; i++){
            Vector3 A = flipped ? RightPoints[i] : leftPoints[i];
            Vector3 B = flipped ? leftPoints[i] : RightPoints[i];
            points[i] = Vector3.Lerp(A, B, offset);
        }
        return points;
    }
}
