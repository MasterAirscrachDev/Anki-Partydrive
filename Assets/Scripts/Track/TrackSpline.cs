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
    public Vector3 GetPoint(TrackCoordinate trackCoordinate){
        return transform.TransformPoint(CurveInterpolator.Cilp(PointsFromOffset(trackCoordinate.offset), trackCoordinate.progression));
    }

    public TrackCoordinate GetTrackCoordinate(Vector3 position) {
        // First, transform the world position to local space
        Vector3 localPosition = transform.InverseTransformPoint(position);
        
        // We need to find the progression and offset that best fits this position
        float bestProgression = 0;
        float bestOffset = 0;
        float minDistance = float.MaxValue;
        
        // Try different progressions from 0 to 1
        int steps = 20; // Increase for better accuracy
        for (int i = 0; i <= steps; i++) {
            float t = (float)i / steps;
            
            // For each progression value, find the best offset
            float bestOffsetForThisT = 0;
            float minDistForThisT = float.MaxValue;
            
            // Sample a few offset values
            int offsetSteps = 10; // Increase for better accuracy
            for (int j = 0; j <= offsetSteps; j++) {
                float offset = Mathf.Lerp(-72.25f, 72.25f, (float)j / offsetSteps);
                
                // Get the point in local space for this progression and offset
                Vector3[] pointsWithOffset = PointsFromOffset(offset);
                Vector3 pointOnTrack = CurveInterpolator.Cilp(pointsWithOffset, t);
                
                float dist = Vector3.Distance(localPosition, pointOnTrack);
                if (dist < minDistForThisT) {
                    minDistForThisT = dist;
                    bestOffsetForThisT = offset;
                }
            }
            
            // See if this progression is better than what we found so far
            if (minDistForThisT < minDistance) {
                minDistance = minDistForThisT;
                bestProgression = t;
                bestOffset = bestOffsetForThisT;
            }
        }
        
        // Refine the result with a binary search or other optimization method
        // [Optional, but improves precision]
        bestProgression = RefineProgression(localPosition, bestProgression, bestOffset);
        bestOffset = RefineOffset(localPosition, bestProgression, bestOffset);
        
        return new TrackCoordinate(0, bestOffset, bestProgression); // Assuming idx is not used in this context
    }

    // Helper method to refine the progression value (optional for better precision)
    float RefineProgression(Vector3 localPosition, float initialProgression, float offset) {
        // Binary search or gradient descent to find the best progression
        float minT = Mathf.Max(0, initialProgression - 0.1f);
        float maxT = Mathf.Min(1, initialProgression + 0.1f);
        
        for (int i = 0; i < 8; i++) { // 8 refinement steps
            float t1 = minT + (maxT - minT) / 3;
            float t2 = minT + 2 * (maxT - minT) / 3;
            
            Vector3[] pointsWithOffset = PointsFromOffset(offset);
            Vector3 p1 = CurveInterpolator.Cilp(pointsWithOffset, t1);
            Vector3 p2 = CurveInterpolator.Cilp(pointsWithOffset, t2);
            
            float dist1 = Vector3.Distance(localPosition, p1);
            float dist2 = Vector3.Distance(localPosition, p2);
            
            if (dist1 < dist2) {
                maxT = t2;
            } else {
                minT = t1;
            }
        }
        
        return (minT + maxT) / 2;
    }

    // Helper method to refine the offset value (optional for better precision)
    float RefineOffset(Vector3 localPosition, float progression, float initialOffset) {
        // Binary search to find the best offset
        float minOffset = Mathf.Max(-72.25f, initialOffset - 10);
        float maxOffset = Mathf.Min(72.25f, initialOffset + 10);
        
        for (int i = 0; i < 8; i++) { // 8 refinement steps
            float o1 = minOffset + (maxOffset - minOffset) / 3;
            float o2 = minOffset + 2 * (maxOffset - minOffset) / 3;
            
            Vector3[] points1 = PointsFromOffset(o1);
            Vector3[] points2 = PointsFromOffset(o2);
            
            Vector3 p1 = CurveInterpolator.Cilp(points1, progression);
            Vector3 p2 = CurveInterpolator.Cilp(points2, progression);
            
            float dist1 = Vector3.Distance(localPosition, p1);
            float dist2 = Vector3.Distance(localPosition, p2);
            
            if (dist1 < dist2) {
                maxOffset = o2;
            } else {
                minOffset = o1;
            }
        }
        
        return (minOffset + maxOffset) / 2;
    }

    Vector3[] PointsFromOffset(float offset){
        offset = Mathf.Clamp(offset, -72.25f, 72.25f);
        offset *= 0.72f; //we actually dont want the true edges
        //scale offset to 0-1
        offset = Mathf.InverseLerp(-72.25f, 72.25f, offset);

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
        float[] lanes = { -72.25f, -21.25f, 21.25f, 72.25f };
        for(int i = 0; i < lanes.Length; i++){
            Vector3[] points = PointsFromOffset(lanes[i]);
            Gizmos.color = Color.Lerp(Color.red, Color.blue, (float)i / (lanes.Length - 1));
            for(int j = 0; j < points.Length - 1; j++){
                Gizmos.DrawLine(transform.TransformPoint(points[j]), transform.TransformPoint(points[j + 1]));
            }
        }
    }
}
