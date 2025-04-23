using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSpline : MonoBehaviour
{
    [SerializeField] Vector3[] leftPoints, rightPoints;
    public bool flipped = false;
    public Vector3 GetPoint(float t, float offset){
        return transform.TransformPoint(CurveInterpolator.Cilp(PointsFromOffset(offset), t));
    }

    Vector3[] PointsFromOffset(float offset){
        offset = Mathf.Clamp(offset, -70, 70);
        offset *= 0.8f; //we actually dont want the true edges
        //scale offset to 0-1
        offset = (offset + 70) / 140;

        Vector3[] points = new Vector3[leftPoints.Length];
        for(int i = 0; i < points.Length; i++){
            Vector3 A = flipped ? rightPoints[i] : leftPoints[i];
            Vector3 B = flipped ? leftPoints[i] : rightPoints[i];
            points[i] = Vector3.Lerp(A, B, offset);
        }
        return points;
    }

    public Vector3 GetEndLinkPoint(){
        Vector3 A = flipped ? rightPoints[rightPoints.Length - 1] : leftPoints[leftPoints.Length - 1];
        Vector3 B = flipped ? leftPoints[leftPoints.Length - 1] : rightPoints[rightPoints.Length - 1];
        return transform.TransformPoint(Vector3.Lerp(A, B, 0.3f));
    }
    public Vector3 GetStartLinkPoint(){
        Vector3 A = flipped ? rightPoints[0] : leftPoints[0];
        Vector3 B = flipped ? leftPoints[0] : rightPoints[0];
        return transform.TransformPoint(Vector3.Lerp(A, B, 0.3f));
    }

    void OnDrawGizmosSelected(){
        Gizmos.color = Color.green;
        for(int i = 0; i < leftPoints.Length - 1; i++){
            Gizmos.DrawLine(transform.TransformPoint(leftPoints[i]), transform.TransformPoint(leftPoints[i + 1]));
            Gizmos.DrawLine(transform.TransformPoint(rightPoints[i]), transform.TransformPoint(rightPoints[i + 1]));
        }
        Vector3[] points = PointsFromOffset(30);
        Gizmos.color = Color.blue;
        for(int i = 0; i < points.Length - 1; i++){
            Gizmos.DrawLine(transform.TransformPoint(points[i]), transform.TransformPoint(points[i + 1]));
        }
    }
}
