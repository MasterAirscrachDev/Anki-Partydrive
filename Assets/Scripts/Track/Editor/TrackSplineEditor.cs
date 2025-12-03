using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrackSpline))]
[CanEditMultipleObjects]
public class TrackSplineEditor : Editor
{
    private SerializedProperty leftPointsProp;
    private SerializedProperty rightPointsProp;
    private SerializedProperty trackWidthsProp;
    private SerializedProperty flippedProp;
    private SerializedProperty debugRenderSmoothCurveProp;

    private int selectedPointIndex = -1;
    private bool editLeftPoints = false;
    private bool editRightPoints = false;
    private bool showHandles = true;
    private float handleSize = 0.5f;

    // Clipboard for copy/paste functionality
    private static Vector3? copiedLeftPoint = null;
    private static Vector3? copiedRightPoint = null;
    // Single endpoint clipboard for start/end matching between splines
    private static Vector3? copiedEndpointLeft = null;
    private static Vector3? copiedEndpointRight = null;
    private static string copiedEndpointSource = null; // "start" or "end"

    private void OnEnable()
    {
        leftPointsProp = serializedObject.FindProperty("leftPoints");
        rightPointsProp = serializedObject.FindProperty("rightPoints");
        trackWidthsProp = serializedObject.FindProperty("trackWidths");
        flippedProp = serializedObject.FindProperty("flipped");
        debugRenderSmoothCurveProp = serializedObject.FindProperty("DEBUG_RenderSmoothCurve");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Track Spline Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Toggle for showing handles
        showHandles = EditorGUILayout.Toggle("Show Handles", showHandles);
        handleSize = EditorGUILayout.Slider("Handle Size", handleSize, 0.1f, 2f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editing Mode", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = editLeftPoints ? Color.green : Color.white;
        if (GUILayout.Button(editLeftPoints ? "✓ Left Points" : "Left Points"))
        {
            editLeftPoints = !editLeftPoints;
        }
        GUI.backgroundColor = editRightPoints ? Color.cyan : Color.white;
        if (GUILayout.Button(editRightPoints ? "✓ Right Points" : "Right Points"))
        {
            editRightPoints = !editRightPoints;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Display current selection info
        if (selectedPointIndex >= 0)
        {
            EditorGUILayout.HelpBox($"Selected: Point {selectedPointIndex}", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Point Arrays", EditorStyles.boldLabel);

        // Left Points
        EditorGUILayout.PropertyField(leftPointsProp, true);
        
        // Right Points
        EditorGUILayout.PropertyField(rightPointsProp, true);

        // Track Widths
        EditorGUILayout.PropertyField(trackWidthsProp, true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(flippedProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point"))
        {
            AddPoint();
        }
        if (GUILayout.Button("Remove Last Point"))
        {
            RemoveLastPoint();
        }
        EditorGUILayout.EndHorizontal();

        if (selectedPointIndex >= 0)
        {
            if (GUILayout.Button("Remove Selected Point"))
            {
                RemovePointAtIndex(selectedPointIndex);
                selectedPointIndex = -1;
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Subdivide Spline"))
        {
            SubdivideSpline();
        }

        // Copy/Paste section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Copy/Paste Points", EditorStyles.boldLabel);

        // Selected point copy/paste
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = selectedPointIndex >= 0;
        if (GUILayout.Button("Copy Selected Pair"))
        {
            CopySelectedPointPair();
        }
        GUI.enabled = copiedLeftPoint.HasValue && copiedRightPoint.HasValue && selectedPointIndex >= 0;
        if (GUILayout.Button("Paste to Selected"))
        {
            PasteToSelectedPointPair();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Start/End copy/paste for matching splines
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Start/End Matching", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Copy the end of one spline and paste it as the start of the next to ensure they connect.", MessageType.None);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Start"))
        {
            CopyStartPointPair();
        }
        if (GUILayout.Button("Copy End"))
        {
            CopyEndPointPair();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = copiedEndpointLeft.HasValue && copiedEndpointRight.HasValue;
        if (GUILayout.Button("Paste to Start"))
        {
            PasteToStartPointPair();
        }
        if (GUILayout.Button("Paste to End"))
        {
            PasteToEndPointPair();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Extend as new spline
        EditorGUILayout.Space();
        if (GUILayout.Button("Extend as New Spline"))
        {
            ExtendAsNewSpline();
        }
        EditorGUILayout.Space();
        //DEBUG_RENDER_SMOOTH_CURVE toggle
        EditorGUILayout.PropertyField(debugRenderSmoothCurveProp, new GUIContent("DEBUG Render Smooth Curve"));

        // Show clipboard status
        string clipboardStatus = "";
        if (copiedLeftPoint.HasValue)
            clipboardStatus += "Point pair copied. ";
        if (copiedEndpointLeft.HasValue)
            clipboardStatus += $"Endpoint copied (from {copiedEndpointSource}).";
        
        if (!string.IsNullOrEmpty(clipboardStatus))
        {
            EditorGUILayout.HelpBox(clipboardStatus, MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        if (!showHandles) return;

        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;

        // Use SerializedObject only for reading, get fresh instance
        SerializedObject so = new SerializedObject(target);
        SerializedProperty leftPoints = so.FindProperty("leftPoints");
        SerializedProperty rightPoints = so.FindProperty("rightPoints");

        // Draw and handle left points
        DrawPointHandles(leftPoints, transform, Color.green, true, editLeftPoints, so);

        // Draw and handle right points
        DrawPointHandles(rightPoints, transform, Color.cyan, false, editRightPoints, so);

        // Draw connecting lines between left and right points
        DrawConnectingLines(transform, leftPoints, rightPoints);

        so.ApplyModifiedProperties();
    }

    private void DrawPointHandles(SerializedProperty pointsArray, Transform transform, Color color, bool isLeft, bool canEdit, SerializedObject so)
    {
        if (pointsArray == null || pointsArray.arraySize == 0) return;

        Handles.color = color;

        for (int i = 0; i < pointsArray.arraySize; i++)
        {
            SerializedProperty pointProp = pointsArray.GetArrayElementAtIndex(i);
            Vector3 localPoint = pointProp.vector3Value;
            Vector3 worldPoint = transform.TransformPoint(localPoint);

            // Determine if this point is selected
            bool isSelected = (selectedPointIndex == i);
            
            // Draw selection button
            float size = HandleUtility.GetHandleSize(worldPoint) * handleSize;
            
            if (isSelected && canEdit)
            {
                Handles.color = Color.yellow;
            }
            else
            {
                Handles.color = canEdit ? color : new Color(color.r, color.g, color.b, 0.3f);
            }

            // Draw a button to select the point
            if (canEdit && Handles.Button(worldPoint, Quaternion.identity, size * 0.15f, size * 0.2f, Handles.SphereHandleCap))
            {
                selectedPointIndex = i;
                Repaint();
            }
            else if (!canEdit)
            {
                // Just draw the sphere without interaction
                Handles.SphereHandleCap(0, worldPoint, Quaternion.identity, size * 0.15f, EventType.Repaint);
            }

            // Draw label
            Handles.Label(worldPoint + Vector3.up * size * 0.3f, $"{(isLeft ? "L" : "R")}{i}", EditorStyles.boldLabel);

            // If this point can be edited, show the position handle
            if (canEdit)
            {
                EditorGUI.BeginChangeCheck();
                
                Vector3 newWorldPoint = Handles.PositionHandle(worldPoint, Quaternion.identity);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move Track Point");
                    Vector3 newLocalPoint = transform.InverseTransformPoint(newWorldPoint);
                    pointProp.vector3Value = newLocalPoint;
                    selectedPointIndex = i;
                }
            }
        }
    }

    private void DrawConnectingLines(Transform transform, SerializedProperty leftPoints, SerializedProperty rightPoints)
    {
        if (leftPoints == null || rightPoints == null) return;

        int count = Mathf.Min(leftPoints.arraySize, rightPoints.arraySize);

        Handles.color = new Color(1f, 1f, 0f, 0.5f);

        for (int i = 0; i < count; i++)
        {
            Vector3 leftWorld = transform.TransformPoint(leftPoints.GetArrayElementAtIndex(i).vector3Value);
            Vector3 rightWorld = transform.TransformPoint(rightPoints.GetArrayElementAtIndex(i).vector3Value);
            
            Handles.DrawDottedLine(leftWorld, rightWorld, 4f);
        }

        // Draw spline preview lines
        Handles.color = new Color(0f, 1f, 0f, 0.3f);
        for (int i = 0; i < leftPoints.arraySize - 1; i++)
        {
            Vector3 p1 = transform.TransformPoint(leftPoints.GetArrayElementAtIndex(i).vector3Value);
            Vector3 p2 = transform.TransformPoint(leftPoints.GetArrayElementAtIndex(i + 1).vector3Value);
            Handles.DrawLine(p1, p2);
        }

        Handles.color = new Color(0f, 1f, 1f, 0.3f);
        for (int i = 0; i < rightPoints.arraySize - 1; i++)
        {
            Vector3 p1 = transform.TransformPoint(rightPoints.GetArrayElementAtIndex(i).vector3Value);
            Vector3 p2 = transform.TransformPoint(rightPoints.GetArrayElementAtIndex(i + 1).vector3Value);
            Handles.DrawLine(p1, p2);
        }
    }

    private void AddPoint()
    {
        serializedObject.Update();

        Vector3 newLeftPoint = Vector3.zero;
        Vector3 newRightPoint = Vector3.zero;

        if (leftPointsProp.arraySize > 0)
        {
            Vector3 lastLeft = leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 1).vector3Value;
            Vector3 lastRight = rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 1).vector3Value;
            
            // Estimate direction from second-to-last to last point
            if (leftPointsProp.arraySize > 1)
            {
                Vector3 prevLeft = leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 2).vector3Value;
                Vector3 direction = (lastLeft - prevLeft).normalized;
                newLeftPoint = lastLeft + direction * 0.25f;
                
                Vector3 prevRight = rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 2).vector3Value;
                Vector3 directionRight = (lastRight - prevRight).normalized;
                newRightPoint = lastRight + directionRight * 0.25f;
            }
            else
            {
                newLeftPoint = lastLeft + Vector3.forward * 0.25f;
                newRightPoint = lastRight + Vector3.forward * 0.25f;
            }
        }

        leftPointsProp.InsertArrayElementAtIndex(leftPointsProp.arraySize);
        leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 1).vector3Value = newLeftPoint;

        rightPointsProp.InsertArrayElementAtIndex(rightPointsProp.arraySize);
        rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 1).vector3Value = newRightPoint;

        // Also add a track width entry if needed
        if (trackWidthsProp.arraySize > 0)
        {
            float lastWidth = trackWidthsProp.GetArrayElementAtIndex(trackWidthsProp.arraySize - 1).floatValue;
            trackWidthsProp.InsertArrayElementAtIndex(trackWidthsProp.arraySize);
            trackWidthsProp.GetArrayElementAtIndex(trackWidthsProp.arraySize - 1).floatValue = lastWidth;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void RemoveLastPoint()
    {
        serializedObject.Update();

        if (leftPointsProp.arraySize > 0)
        {
            leftPointsProp.DeleteArrayElementAtIndex(leftPointsProp.arraySize - 1);
        }
        if (rightPointsProp.arraySize > 0)
        {
            rightPointsProp.DeleteArrayElementAtIndex(rightPointsProp.arraySize - 1);
        }
        if (trackWidthsProp.arraySize > leftPointsProp.arraySize && trackWidthsProp.arraySize > 0)
        {
            trackWidthsProp.DeleteArrayElementAtIndex(trackWidthsProp.arraySize - 1);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void RemovePointAtIndex(int index)
    {
        serializedObject.Update();

        if (index >= 0 && index < leftPointsProp.arraySize)
        {
            leftPointsProp.DeleteArrayElementAtIndex(index);
        }
        if (index >= 0 && index < rightPointsProp.arraySize)
        {
            rightPointsProp.DeleteArrayElementAtIndex(index);
        }
        if (index >= 0 && index < trackWidthsProp.arraySize)
        {
            trackWidthsProp.DeleteArrayElementAtIndex(index);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void SubdivideSpline()
    {
        if (leftPointsProp.arraySize < 2 || rightPointsProp.arraySize < 2) return;

        serializedObject.Update();
        Undo.RecordObject(target, "Subdivide Spline");

        // Collect current points
        List<Vector3> leftPoints = new List<Vector3>();
        List<Vector3> rightPoints = new List<Vector3>();
        List<float> widths = new List<float>();

        for (int i = 0; i < leftPointsProp.arraySize; i++)
        {
            leftPoints.Add(leftPointsProp.GetArrayElementAtIndex(i).vector3Value);
        }
        for (int i = 0; i < rightPointsProp.arraySize; i++)
        {
            rightPoints.Add(rightPointsProp.GetArrayElementAtIndex(i).vector3Value);
        }
        for (int i = 0; i < trackWidthsProp.arraySize; i++)
        {
            widths.Add(trackWidthsProp.GetArrayElementAtIndex(i).floatValue);
        }

        // Create new subdivided lists
        List<Vector3> newLeftPoints = new List<Vector3>();
        List<Vector3> newRightPoints = new List<Vector3>();
        List<float> newWidths = new List<float>();

        for (int i = 0; i < leftPoints.Count - 1; i++)
        {
            // Add original point
            newLeftPoints.Add(leftPoints[i]);
            newRightPoints.Add(rightPoints[i]);
            if (i < widths.Count)
                newWidths.Add(widths[i]);

            // Add midpoint
            Vector3 midLeft = Vector3.Lerp(leftPoints[i], leftPoints[i + 1], 0.5f);
            Vector3 midRight = Vector3.Lerp(rightPoints[i], rightPoints[i + 1], 0.5f);
            newLeftPoints.Add(midLeft);
            newRightPoints.Add(midRight);
            
            if (i < widths.Count && i + 1 < widths.Count)
                newWidths.Add(Mathf.Lerp(widths[i], widths[i + 1], 0.5f));
            else if (widths.Count > 0)
                newWidths.Add(widths[widths.Count - 1]);
        }

        // Add last original point
        newLeftPoints.Add(leftPoints[leftPoints.Count - 1]);
        newRightPoints.Add(rightPoints[rightPoints.Count - 1]);
        if (widths.Count > 0)
            newWidths.Add(widths[widths.Count - 1]);

        // Clear and repopulate arrays
        leftPointsProp.ClearArray();
        for (int i = 0; i < newLeftPoints.Count; i++)
        {
            leftPointsProp.InsertArrayElementAtIndex(i);
            leftPointsProp.GetArrayElementAtIndex(i).vector3Value = newLeftPoints[i];
        }

        rightPointsProp.ClearArray();
        for (int i = 0; i < newRightPoints.Count; i++)
        {
            rightPointsProp.InsertArrayElementAtIndex(i);
            rightPointsProp.GetArrayElementAtIndex(i).vector3Value = newRightPoints[i];
        }

        if (newWidths.Count > 0)
        {
            trackWidthsProp.ClearArray();
            for (int i = 0; i < newWidths.Count; i++)
            {
                trackWidthsProp.InsertArrayElementAtIndex(i);
                trackWidthsProp.GetArrayElementAtIndex(i).floatValue = newWidths[i];
            }
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"Subdivided spline: {leftPoints.Count} -> {newLeftPoints.Count} points");
    }

    private void CopySelectedPointPair()
    {
        if (selectedPointIndex < 0) return;
        if (selectedPointIndex >= leftPointsProp.arraySize || selectedPointIndex >= rightPointsProp.arraySize) return;

        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;
        
        // Store in world space
        Vector3 localLeft = leftPointsProp.GetArrayElementAtIndex(selectedPointIndex).vector3Value;
        Vector3 localRight = rightPointsProp.GetArrayElementAtIndex(selectedPointIndex).vector3Value;
        copiedLeftPoint = transform.TransformPoint(localLeft);
        copiedRightPoint = transform.TransformPoint(localRight);
        
        Debug.Log($"Copied point pair at index {selectedPointIndex} (world space)");
    }

    private void PasteToSelectedPointPair()
    {
        if (!copiedLeftPoint.HasValue || !copiedRightPoint.HasValue) return;
        if (selectedPointIndex < 0) return;
        if (selectedPointIndex >= leftPointsProp.arraySize || selectedPointIndex >= rightPointsProp.arraySize) return;

        serializedObject.Update();
        
        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;
        
        Undo.RecordObject(target, "Paste Point Pair");
        // Convert from world space to local space
        leftPointsProp.GetArrayElementAtIndex(selectedPointIndex).vector3Value = transform.InverseTransformPoint(copiedLeftPoint.Value);
        rightPointsProp.GetArrayElementAtIndex(selectedPointIndex).vector3Value = transform.InverseTransformPoint(copiedRightPoint.Value);
        
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"Pasted point pair to index {selectedPointIndex} (from world space)");
    }

    private void CopyStartPointPair()
    {
        if (leftPointsProp.arraySize == 0 || rightPointsProp.arraySize == 0) return;

        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;
        
        // Store in world space
        Vector3 localLeft = leftPointsProp.GetArrayElementAtIndex(0).vector3Value;
        Vector3 localRight = rightPointsProp.GetArrayElementAtIndex(0).vector3Value;
        copiedEndpointLeft = transform.TransformPoint(localLeft);
        copiedEndpointRight = transform.TransformPoint(localRight);
        copiedEndpointSource = "start";
        
        Debug.Log("Copied start point pair to endpoint clipboard (world space)");
    }

    private void CopyEndPointPair()
    {
        if (leftPointsProp.arraySize == 0 || rightPointsProp.arraySize == 0) return;

        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;
        
        // Store in world space
        Vector3 localLeft = leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 1).vector3Value;
        Vector3 localRight = rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 1).vector3Value;
        copiedEndpointLeft = transform.TransformPoint(localLeft);
        copiedEndpointRight = transform.TransformPoint(localRight);
        copiedEndpointSource = "end";
        
        Debug.Log("Copied end point pair to endpoint clipboard (world space)");
    }

    private void PasteToStartPointPair()
    {
        if (!copiedEndpointLeft.HasValue || !copiedEndpointRight.HasValue) return;
        if (leftPointsProp.arraySize == 0 || rightPointsProp.arraySize == 0) return;

        serializedObject.Update();
        
        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;
        
        Undo.RecordObject(target, "Paste to Start Point Pair");
        // Convert from world space to local space
        leftPointsProp.GetArrayElementAtIndex(0).vector3Value = transform.InverseTransformPoint(copiedEndpointLeft.Value);
        rightPointsProp.GetArrayElementAtIndex(0).vector3Value = transform.InverseTransformPoint(copiedEndpointRight.Value);
        
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"Pasted endpoint (from {copiedEndpointSource}) to start (converted to local space)");
    }

    private void PasteToEndPointPair()
    {
        if (!copiedEndpointLeft.HasValue || !copiedEndpointRight.HasValue) return;
        if (leftPointsProp.arraySize == 0 || rightPointsProp.arraySize == 0) return;

        serializedObject.Update();
        
        TrackSpline spline = (TrackSpline)target;
        Transform transform = spline.transform;
        
        Undo.RecordObject(target, "Paste to End Point Pair");
        // Convert from world space to local space
        leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 1).vector3Value = transform.InverseTransformPoint(copiedEndpointLeft.Value);
        rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 1).vector3Value = transform.InverseTransformPoint(copiedEndpointRight.Value);
        
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"Pasted endpoint (from {copiedEndpointSource}) to end (converted to local space)");
    }

    private void ExtendAsNewSpline()
    {
        if (leftPointsProp.arraySize == 0 || rightPointsProp.arraySize == 0) return;

        TrackSpline sourceSpline = (TrackSpline)target;
        Transform sourceTransform = sourceSpline.transform;

        // Get the last points in world space
        Vector3 lastLeftLocal = leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 1).vector3Value;
        Vector3 lastRightLocal = rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 1).vector3Value;
        Vector3 lastLeftWorld = sourceTransform.TransformPoint(lastLeftLocal);
        Vector3 lastRightWorld = sourceTransform.TransformPoint(lastRightLocal);

        // Calculate position (center between left and right end points)
        Vector3 centerPosition = (lastLeftWorld + lastRightWorld) / 2f;

        // Calculate forward direction (direction of progress along the spline)
        Vector3 forward = Vector3.forward;
        if (leftPointsProp.arraySize >= 2)
        {
            Vector3 prevLeftLocal = leftPointsProp.GetArrayElementAtIndex(leftPointsProp.arraySize - 2).vector3Value;
            Vector3 prevRightLocal = rightPointsProp.GetArrayElementAtIndex(rightPointsProp.arraySize - 2).vector3Value;
            Vector3 prevLeftWorld = sourceTransform.TransformPoint(prevLeftLocal);
            Vector3 prevRightWorld = sourceTransform.TransformPoint(prevRightLocal);
            Vector3 prevCenter = (prevLeftWorld + prevRightWorld) / 2f;
            forward = (centerPosition - prevCenter).normalized;
            if (forward == Vector3.zero) forward = sourceTransform.forward;
        }
        else
        {
            forward = sourceTransform.forward;
        }

        // Create new GameObject
        GameObject newSplineObj = new GameObject("SegmentX");
        Undo.RegisterCreatedObjectUndo(newSplineObj, "Create Extended Spline");

        // Position and rotate the new object
        newSplineObj.transform.position = centerPosition;
        newSplineObj.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        // Parent to same parent as source if it has one
        if (sourceTransform.parent != null)
        {
            newSplineObj.transform.SetParent(sourceTransform.parent);
        }

        // Add TrackSpline component
        TrackSpline newSpline = newSplineObj.AddComponent<TrackSpline>();

        // Get SerializedObject for the new spline to set its arrays
        SerializedObject newSerializedObject = new SerializedObject(newSpline);
        SerializedProperty newLeftPoints = newSerializedObject.FindProperty("leftPoints");
        SerializedProperty newRightPoints = newSerializedObject.FindProperty("rightPoints");
        SerializedProperty newTrackWidths = newSerializedObject.FindProperty("trackWidths");
        SerializedProperty newFlipped = newSerializedObject.FindProperty("flipped");

        // Copy the flipped value from the source spline
        newFlipped.boolValue = flippedProp.boolValue;

        // Convert end points to local space of the new spline
        Vector3 newLeftLocal = newSplineObj.transform.InverseTransformPoint(lastLeftWorld);
        Vector3 newRightLocal = newSplineObj.transform.InverseTransformPoint(lastRightWorld);

        // Add the first point pair
        newLeftPoints.InsertArrayElementAtIndex(0);
        newLeftPoints.GetArrayElementAtIndex(0).vector3Value = newLeftLocal;

        newRightPoints.InsertArrayElementAtIndex(0);
        newRightPoints.GetArrayElementAtIndex(0).vector3Value = newRightLocal;
        //copy the first track width if exists
        if (trackWidthsProp.arraySize > 0)
        {
            float firstWidth = trackWidthsProp.GetArrayElementAtIndex(trackWidthsProp.arraySize - 1).floatValue;
            newTrackWidths.InsertArrayElementAtIndex(0);
            newTrackWidths.GetArrayElementAtIndex(0).floatValue = firstWidth;
        }


        newSerializedObject.ApplyModifiedProperties();

        // Select the new object
        Selection.activeGameObject = newSplineObj;

        Debug.Log("Created new extended TrackSpline");
    }
}
