using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSpline : MonoBehaviour
{
    [SerializeField] Vector3[] leftPoints, rightPoints;
    [SerializeField] float[] trackWidths; //should be same length as points
    public bool flipped = false;
    [SerializeField] bool DEBUG_RenderSmoothCurve = false;
    public Vector3 GetPoint(float t, float offset){
        return transform.TransformPoint(CurveInterpolator.Cilp(PointsFromOffset(offset, GetWidth(t)), t));
    }
    public Vector3 GetPoint(TrackCoordinate trackCoordinate){
        return transform.TransformPoint(CurveInterpolator.Cilp(PointsFromOffset(trackCoordinate.offset, GetWidth(trackCoordinate.progression)), trackCoordinate.progression));
    }
    /// <summary>
    /// Get the width of the track at a specific progression t (0 to 1)
    /// </summary>
    /// <param name="t">Time</param>
    /// <returns>float half Width</returns>
    // <summary>
    public float GetWidth(float t){
        if(trackWidths.Length == 0){ return 67.5f; } //modular tracks
        return LerpArray(trackWidths, t); //drive tracks
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
                float offset = Mathf.Lerp(-67.5f, 67.5f, (float)j / offsetSteps);
                
                // Get the point in local space for this progression and offset
                Vector3[] pointsWithOffset = PointsFromOffset(offset, 67.5f);
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
        
        //convert the bestOffset to be clamped within the track width at bestProgression
        float halfWidth = GetWidth(bestProgression);
        //scale bestOffset to be within -1 to 1
        bestOffset = bestOffset / 67.5f;
        bestOffset = bestOffset * halfWidth;

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
            
            Vector3[] pointsWithOffset = PointsFromOffset(offset, 67.5f);
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
        float halfWidth = GetWidth(progression);
        float minOffset = Mathf.Max(-halfWidth, initialOffset - 10);
        float maxOffset = Mathf.Min(halfWidth, initialOffset + 10);
        
        for (int i = 0; i < 8; i++) { // 8 refinement steps
            float o1 = minOffset + (maxOffset - minOffset) / 3;
            float o2 = minOffset + 2 * (maxOffset - minOffset) / 3;
            
            Vector3[] points1 = PointsFromOffset(o1, 67.5f);
            Vector3[] points2 = PointsFromOffset(o2, 67.5f);
            
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

    Vector3[] PointsFromOffset(float offset, float maxOffset){
        offset = Mathf.Clamp(offset, -maxOffset, maxOffset);
        offset *= 0.72f; //we actually dont want the true edges
        //scale offset to 0-1
        offset = Mathf.InverseLerp(-maxOffset, maxOffset, offset);

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
        float[] lanes = { -67.5f, -22.5f, 22.5f, 67.5f };
        
        if(DEBUG_RenderSmoothCurve){
            //render each lane but with smoothing 10x per point
            int smoothSteps = 10;
            for(int i = 0; i < lanes.Length; i++){
                Vector3 previousPoint = GetPoint(0, lanes[i]);
                Gizmos.color = Color.Lerp(Color.red, Color.blue, (float)i / (lanes.Length - 1));
                for(int j = 1; j <= (leftPoints.Length - 1) * smoothSteps; j++){
                    float t = (float)j / ((leftPoints.Length - 1) * smoothSteps);
                    Vector3 currentPoint = GetPoint(t, lanes[i]);
                    Gizmos.DrawLine(previousPoint, currentPoint);
                    previousPoint = currentPoint;
                }
            }
        }
        else
        {
            for(int i = 0; i < lanes.Length; i++){
                Vector3[] points = PointsFromOffset(lanes[i], 67.5f);
                Gizmos.color = Color.Lerp(Color.red, Color.blue, (float)i / (lanes.Length - 1));
                for(int j = 0; j < points.Length - 1; j++){
                    Gizmos.DrawLine(transform.TransformPoint(points[j]), transform.TransformPoint(points[j + 1]));
                }
            }
        }
    }
    float LerpArray(float[] arr, float t){
        if(arr.Length == 0) return 0;
        if(arr.Length == 1) return arr[0];
        t = Mathf.Clamp01(t) * (arr.Length - 1);
        int idx = Mathf.FloorToInt(t);
        if(idx >= arr.Length - 1) return arr[arr.Length - 1];
        float frac = t - idx;
        return Mathf.Lerp(arr[idx], arr[idx + 1], frac);
    }
}
