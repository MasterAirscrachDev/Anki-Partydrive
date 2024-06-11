using UnityEngine;

public static class CurveInterpolator
{
    public static Vector3 Cilp(Vector3[] points, float t){
        if (points.Length < 2) return Vector3.zero;
        if (points.Length == 2) return Vector3.Lerp(points[0], points[1], t);
        t = Mathf.Clamp01(t);
        int lastPointIndex = points.Length - 1, indexInPoints = (int)(t * lastPointIndex);
        if (indexInPoints == lastPointIndex) indexInPoints = lastPointIndex - 1;
        float localPointTime = t * lastPointIndex - indexInPoints;
        Vector3 firstPoint = points[indexInPoints], nextPoint = points[indexInPoints + 1];
        Vector3 cTangent1 = indexInPoints > 0 ? points[indexInPoints - 1] : firstPoint + (nextPoint - firstPoint).normalized;
        Vector3 cTangent2 = indexInPoints < lastPointIndex - 1 ? points[indexInPoints + 2] : nextPoint + (firstPoint - nextPoint).normalized;
        
        return 0.5f * ((-cTangent1 + 3 * firstPoint - 3 * nextPoint + cTangent2) * Mathf.Pow(localPointTime, 3) +
            (2 * cTangent1 - 5 * firstPoint + 4 * nextPoint - cTangent2) * 
            Mathf.Pow(localPointTime, 2) + (-cTangent1 + nextPoint) * localPointTime + 2 * firstPoint );
    }
}