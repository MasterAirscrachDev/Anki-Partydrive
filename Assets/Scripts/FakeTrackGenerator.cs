using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FakeTrackGenerator : MonoBehaviour
{
    [SerializeField] bool test;
    [SerializeField] Segment[] segments;

    // Update is called once per frame
    void Update()
    {
        if(test){
            test = false;
            List<Vector3> points = new List<Vector3>();
            List<int> turnIndexes = new List<int>();
            Vector3 currentPoint = new Vector3(0, 0, -0.5f);
            Vector3 forward = Vector3.forward;
            points.Add(currentPoint);
            float lastSegmentHeight = 0;
            for(int i = 0; i < segments.Length; i++){
                Segment segment = segments[i];
                //round elevation to the nearest 0.25
                float elevation = Mathf.Round(segment.elevation * 4) / 4;
                elevation *= 0.2f;
                if(segment.type == TrackType.Straight || segment.type == TrackType.Finish || segment.type == TrackType.Poweup){
                    currentPoint += forward;
                    currentPoint.y = elevation;
                    points.Add(currentPoint);
                    //if powerup or finish, add relevent effect
                }
                else{
                    turnIndexes.Add(points.Count);
                    Debug.DrawRay(currentPoint, Vector3.up, Color.green, 10);
                    int orientation = segment.type == TrackType.CurveLeft ? -1 : 1;
                    Vector3 circleCenter = currentPoint + (Quaternion.Euler(0, 90 * orientation, 0) * forward) * 0.5f;
                    Debug.DrawRay(circleCenter, Vector3.up, Color.red, 10);
                    // Calculate the initial angle based on the current forward direction to ensure correct rotation start
                    float initialAngle = Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg;
                    for(int j = 0; j <= 10; j++){
                        // Adjust the angle increment based on orientation and step
                        float angleIncrement = 9 * orientation; // 9 degrees per step, adjusted for orientation
                        float angle = initialAngle + angleIncrement * j;
                        Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                        Vector3 nextPoint = circleCenter + direction * 0.5f; // Use the calculated direction for the next point
                        Vector3 newPoint = nextPoint + new Vector3(0, Mathf.Lerp(elevation, lastSegmentHeight,  (j == 0) ? 0f : j / 10f), 0);
                        //Debug.Log($"at {j}: {Mathf.Lerp(elevation, lastSegmentHeight,  (9f == 0) ? 0f : j / 9f)}");
                        Debug.DrawRay(newPoint, Vector3.up, Color.blue, 10);
                        points.Add(newPoint);
                    }
                    //flip the order of the last 10 points to ensure the curve is smooth
                    points.RemoveAt(points.Count - 1);
                    points.Reverse(points.Count - 10, 10);
                    currentPoint = points[points.Count - 1];
                    // Update forward direction based on the final orientation of the curve
                    forward = points[points.Count - 1] - points[points.Count - 2];
                    forward.Normalize();
                    //snap forward to the nearest axis
                    if(Mathf.Abs(forward.x) > Mathf.Abs(forward.z)){
                        forward = new Vector3(Mathf.Sign(forward.x), 0, 0);
                    }
                    else{
                        forward = new Vector3(0, 0, Mathf.Sign(forward.z));
                    }
                }
                //Debug.Log($"Segment {i} finished at {currentPoint} with elevation {elevation}");
                if(elevation > lastSegmentHeight){
                    lastSegmentHeight = elevation;
                }
                else{
                    lastSegmentHeight = elevation;
                }
                
            }


            GetComponent<SmoothLineOnPoints>().Calculate(points.ToArray(), 1);
            // for(int i = 0; i < turnIndexes.Count; i++){
            //     Debug.Log($"Turn at {turnIndexes[i]}");
            // }
            GetComponent<TrackGenerator>().GenerateTrackFromComplexPoints(points.ToArray(), turnIndexes.ToArray(),0.5f);

        }
    }
    public void TestGen(TrackType[] tracks){
        segments = new Segment[tracks.Length];
        for(int i = 0; i < tracks.Length; i++){
            segments[i] = new Segment();
            segments[i].type = tracks[i];
            segments[i].elevation = 0;
        }
        test = true;
    }
}
[System.Serializable]
public enum TrackType
{
    Straight,
    CurveLeft,
    CurveRight,
    Jump,
    CrissCross,
    Finish,
    Poweup,
    Unknown
}
[System.Serializable]
class Segment
{
    public TrackType type;
    public float elevation;
    public bool addPowerups;
}