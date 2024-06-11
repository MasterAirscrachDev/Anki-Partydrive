using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SmoothLineOnPoints : MonoBehaviour
{
    [SerializeField] bool calculate = false;
    [SerializeField] int resolution = 1;
    // Update is called once per frame
    void Update()
    {
        if(calculate){
            calculate = false;
            int childCount = transform.childCount;
            Vector3[] points = new Vector3[childCount];
            for(int i = 0; i < childCount; i++){
                points[i] = transform.GetChild(i).position;
            }
            Calculate(points, resolution);
            
        }
    }
    public void Calculate(Vector3[] points, int resolution){
        int steps = points.Length * resolution;
        Vector3[] smoothedPoints = new Vector3[steps];
        for(int i = 0; i < steps; i++){
            smoothedPoints[i] = CurveInterpolator.Cilp(points, (float)i / (steps - 1)) + new Vector3(0, 0.1f, 0);
        }
        GetComponent<LineRenderer>().positionCount = steps;
        GetComponent<LineRenderer>().SetPositions(smoothedPoints);
    }
}
