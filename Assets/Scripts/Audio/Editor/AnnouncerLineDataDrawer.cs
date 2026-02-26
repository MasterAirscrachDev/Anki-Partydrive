#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static OverdriveServer.NetStructures;

[CustomPropertyDrawer(typeof(AudioAnnouncerManager.AnnouncerLineData))]
public class AnnouncerLineDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Create custom label
        SerializedProperty lineProp = property.FindPropertyRelative("line");
        SerializedProperty lineTypeProp = property.FindPropertyRelative("lineType");
        string customLabel = GetCustomLabel(property, lineProp, lineTypeProp);
        
        // Draw foldout with custom label
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, customLabel, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw line enum
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), lineProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw lineType enum
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), lineTypeProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw folder path field
            SerializedProperty folderPathProp = property.FindPropertyRelative("folderPath");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), folderPathProp, new GUIContent("Folder Path"));
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Conditionally draw based on lineType
            AudioAnnouncerManager.LineType lineType = (AudioAnnouncerManager.LineType)lineTypeProp.enumValueIndex;
            
            if (lineType == AudioAnnouncerManager.LineType.Unique)
            {
                // Draw uniqueClips array
                SerializedProperty uniqueClipsProp = property.FindPropertyRelative("uniqueClips");
                float arrayHeight = EditorGUI.GetPropertyHeight(uniqueClipsProp, true);
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, arrayHeight), uniqueClipsProp, new GUIContent("Unique Clips"), true);
            }
            else if (lineType == AudioAnnouncerManager.LineType.CarSpecific)
            {
                // Draw carSpecificClips list
                SerializedProperty carClipsProp = property.FindPropertyRelative("carSpecificClips");
                float listHeight = EditorGUI.GetPropertyHeight(carClipsProp, true);
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, listHeight), carClipsProp, new GUIContent("Car-Specific Clips"), true);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
        float height = EditorGUIUtility.singleLineHeight; // Foldout
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // line enum
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // lineType enum
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // folder path
        
        SerializedProperty lineTypeProp = property.FindPropertyRelative("lineType");
        AudioAnnouncerManager.LineType lineType = (AudioAnnouncerManager.LineType)lineTypeProp.enumValueIndex;
        
        if (lineType == AudioAnnouncerManager.LineType.Unique)
        {
            SerializedProperty uniqueClipsProp = property.FindPropertyRelative("uniqueClips");
            height += EditorGUI.GetPropertyHeight(uniqueClipsProp, true);
        }
        else if (lineType == AudioAnnouncerManager.LineType.CarSpecific)
        {
            SerializedProperty carClipsProp = property.FindPropertyRelative("carSpecificClips");
            height += EditorGUI.GetPropertyHeight(carClipsProp, true);
        }
        
        return height;
    }
    
    private string GetCustomLabel(SerializedProperty property, SerializedProperty lineProp, SerializedProperty lineTypeProp)
    {
        string lineName = lineProp.enumNames[lineProp.enumValueIndex];
        AudioAnnouncerManager.LineType lineType = (AudioAnnouncerManager.LineType)lineTypeProp.enumValueIndex;
        
        if (lineType == AudioAnnouncerManager.LineType.Unique)
        {
            SerializedProperty uniqueClipsProp = property.FindPropertyRelative("uniqueClips");
            int clipCount = uniqueClipsProp.arraySize;
            return $"{lineName} ({clipCount})";
        }
        else if (lineType == AudioAnnouncerManager.LineType.CarSpecific)
        {
            SerializedProperty carSpecificClipsProp = property.FindPropertyRelative("carSpecificClips");
            int carCount = carSpecificClipsProp.arraySize;
            return $"{lineName} ({carCount} Cars)";
        }
        
        return lineName;
    }
    
    private static bool IsAudioFile(string path)
    {
        return path.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase) || 
               path.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase) || 
               path.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Custom editor for AudioAnnouncerManager that adds a "Reprocess All Dialog" button
/// and reports unused audio files under Assets/Audio/Announcer.
/// </summary>
[CustomEditor(typeof(AudioAnnouncerManager))]
public class AudioAnnouncerManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Reprocess All Dialog", GUILayout.Height(30)))
        {
            ReprocessAllDialog();
        }
    }
    
    private void ReprocessAllDialog()
    {
        serializedObject.Update();
        
        SerializedProperty announcerLinesProp = serializedObject.FindProperty("announcerLines");
        
        int totalPreviousClips = 0;
        int totalNewClips = 0;
        int processedCount = 0;
        int skippedCount = 0;
        HashSet<string> allLoadedAssetPaths = new HashSet<string>();
        
        // Sort models by name length descending for matching
        ModelName[] allModels = (ModelName[])System.Enum.GetValues(typeof(ModelName));
        var sortedModels = allModels
            .Where(m => m != ModelName.Unknown)
            .OrderByDescending(m => m.ToString().Length)
            .ToArray();
        
        for (int i = 0; i < announcerLinesProp.arraySize; i++)
        {
            SerializedProperty lineDataProp = announcerLinesProp.GetArrayElementAtIndex(i);
            SerializedProperty folderPathProp = lineDataProp.FindPropertyRelative("folderPath");
            string folderPath = folderPathProp.stringValue;
            
            SerializedProperty lineProp = lineDataProp.FindPropertyRelative("line");
            string lineName = lineProp.enumNames[lineProp.enumValueIndex];
            
            if (string.IsNullOrEmpty(folderPath))
            {
                skippedCount++;
                continue;
            }
            
            // Clean up the path
            folderPath = folderPath.Trim().Trim('"').Trim('\'');
            string relativePath = folderPath;
            if (relativePath.StartsWith("Assets/"))
                relativePath = relativePath.Substring("Assets/".Length);
            if (relativePath.StartsWith("Assets\\"))
                relativePath = relativePath.Substring("Assets\\".Length);
            
            string assetFolderPath = folderPath.StartsWith("Assets/") || folderPath.StartsWith("Assets\\") 
                ? folderPath.Replace('\\', '/') 
                : "Assets/" + relativePath.Replace('\\', '/');
            
            string fullPath = Path.Combine(Application.dataPath, relativePath);
            
            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[Announcer Reprocess All] Folder not found for '{lineName}': {fullPath}");
                skippedCount++;
                continue;
            }
            
            string[] audioFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => IsAudioFile(f))
                .ToArray();
            
            if (audioFiles.Length == 0)
            {
                Debug.LogWarning($"[Announcer Reprocess All] No audio files in folder for '{lineName}': {assetFolderPath}");
                skippedCount++;
                continue;
            }
            
            SerializedProperty lineTypeProp = lineDataProp.FindPropertyRelative("lineType");
            AudioAnnouncerManager.LineType lineType = (AudioAnnouncerManager.LineType)lineTypeProp.enumValueIndex;
            
            if (lineType == AudioAnnouncerManager.LineType.Unique)
            {
                SerializedProperty uniqueClipsProp = lineDataProp.FindPropertyRelative("uniqueClips");
                int prevCount = uniqueClipsProp.arraySize;
                totalPreviousClips += prevCount;
                
                List<AudioClip> clips = new List<AudioClip>();
                foreach (string audioFile in audioFiles)
                {
                    string assetPath = assetFolderPath + "/" + Path.GetFileName(audioFile);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip != null)
                    {
                        clips.Add(clip);
                        allLoadedAssetPaths.Add(assetPath.Replace('\\', '/'));
                    }
                }
                
                uniqueClipsProp.ClearArray();
                foreach (AudioClip clip in clips)
                {
                    uniqueClipsProp.InsertArrayElementAtIndex(uniqueClipsProp.arraySize);
                    uniqueClipsProp.GetArrayElementAtIndex(uniqueClipsProp.arraySize - 1).objectReferenceValue = clip;
                }
                
                totalNewClips += clips.Count;
                Debug.Log($"[Announcer Reprocess All] {lineName} (Unique): {prevCount} -> {clips.Count} clips");
            }
            else if (lineType == AudioAnnouncerManager.LineType.CarSpecific)
            {
                SerializedProperty carSpecificClipsProp = lineDataProp.FindPropertyRelative("carSpecificClips");
                int prevClipCount = 0;
                for (int c = 0; c < carSpecificClipsProp.arraySize; c++)
                {
                    prevClipCount += carSpecificClipsProp.GetArrayElementAtIndex(c).FindPropertyRelative("clips").arraySize;
                }
                totalPreviousClips += prevClipCount;
                
                Dictionary<ModelName, List<AudioClip>> carClipsDict = new Dictionary<ModelName, List<AudioClip>>();
                
                foreach (string audioFile in audioFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(audioFile);
                    
                    ModelName matchedModel = ModelName.Unknown;
                    foreach (ModelName model in sortedModels)
                    {
                        string modelName = model.ToString();
                        if (fileName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            matchedModel = model;
                            break;
                        }
                        else if (fileName.Length > modelName.Length && 
                                 fileName.StartsWith(modelName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            string remainder = fileName.Substring(modelName.Length);
                            if (remainder.All(char.IsDigit))
                            {
                                matchedModel = model;
                                break;
                            }
                        }
                    }
                    
                    if (matchedModel == ModelName.Unknown)
                    {
                        Debug.LogWarning($"[Announcer Reprocess All] Could not match file '{fileName}' to any car model in '{lineName}'. Skipping.");
                        continue;
                    }
                    
                    string assetPath = assetFolderPath + "/" + Path.GetFileName(audioFile);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip != null)
                    {
                        if (!carClipsDict.ContainsKey(matchedModel))
                            carClipsDict[matchedModel] = new List<AudioClip>();
                        carClipsDict[matchedModel].Add(clip);
                        allLoadedAssetPaths.Add(assetPath.Replace('\\', '/'));
                    }
                }
                
                carSpecificClipsProp.ClearArray();
                int newClipCount = 0;
                foreach (var kvp in carClipsDict)
                {
                    carSpecificClipsProp.InsertArrayElementAtIndex(carSpecificClipsProp.arraySize);
                    SerializedProperty newCarClipsProp = carSpecificClipsProp.GetArrayElementAtIndex(carSpecificClipsProp.arraySize - 1);
                    
                    newCarClipsProp.FindPropertyRelative("carModel").enumValueIndex = (int)kvp.Key;
                    SerializedProperty clipsProp = newCarClipsProp.FindPropertyRelative("clips");
                    
                    clipsProp.ClearArray();
                    foreach (AudioClip clip in kvp.Value)
                    {
                        clipsProp.InsertArrayElementAtIndex(clipsProp.arraySize);
                        clipsProp.GetArrayElementAtIndex(clipsProp.arraySize - 1).objectReferenceValue = clip;
                        newClipCount++;
                    }
                }
                
                totalNewClips += newClipCount;
                Debug.Log($"[Announcer Reprocess All] {lineName} (CarSpecific): {prevClipCount} -> {newClipCount} clips ({carClipsDict.Count} cars)");
            }
            
            processedCount++;
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // Summary
        Debug.Log($"[Announcer Reprocess All] === SUMMARY === Processed {processedCount} lines, skipped {skippedCount} (no folder path). Total clips: {totalPreviousClips} -> {totalNewClips}");
        
        // Collect ALL loaded clips from every line (including manually assigned ones without folder paths)
        // so the unused file check catches anything not referenced anywhere
        HashSet<string> allReferencedPaths = new HashSet<string>(allLoadedAssetPaths);
        CollectAllReferencedClips(announcerLinesProp, allReferencedPaths);
        
        // Find unused audio files in Assets/Audio/Announcer (recursive)
        FindUnusedAnnouncerFiles(allReferencedPaths);
    }
    
    /// <summary>
    /// Collects asset paths of all AudioClips currently assigned in every announcer line entry,
    /// including manually assigned ones that have no folder path set.
    /// </summary>
    private void CollectAllReferencedClips(SerializedProperty announcerLinesProp, HashSet<string> referencedPaths)
    {
        for (int i = 0; i < announcerLinesProp.arraySize; i++)
        {
            SerializedProperty lineDataProp = announcerLinesProp.GetArrayElementAtIndex(i);
            
            // Collect uniqueClips
            SerializedProperty uniqueClipsProp = lineDataProp.FindPropertyRelative("uniqueClips");
            for (int u = 0; u < uniqueClipsProp.arraySize; u++)
            {
                Object clipObj = uniqueClipsProp.GetArrayElementAtIndex(u).objectReferenceValue;
                if (clipObj != null)
                {
                    string path = AssetDatabase.GetAssetPath(clipObj);
                    if (!string.IsNullOrEmpty(path))
                        referencedPaths.Add(path.Replace('\\', '/'));
                }
            }
            
            // Collect carSpecificClips
            SerializedProperty carClipsProp = lineDataProp.FindPropertyRelative("carSpecificClips");
            for (int c = 0; c < carClipsProp.arraySize; c++)
            {
                SerializedProperty clipsProp = carClipsProp.GetArrayElementAtIndex(c).FindPropertyRelative("clips");
                for (int k = 0; k < clipsProp.arraySize; k++)
                {
                    Object clipObj = clipsProp.GetArrayElementAtIndex(k).objectReferenceValue;
                    if (clipObj != null)
                    {
                        string path = AssetDatabase.GetAssetPath(clipObj);
                        if (!string.IsNullOrEmpty(path))
                            referencedPaths.Add(path.Replace('\\', '/'));
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Scans Assets/Audio/Announcer recursively for audio files not loaded by any announcer line entry.
    /// </summary>
    private void FindUnusedAnnouncerFiles(HashSet<string> loadedPaths)
    {
        string announcerRoot = Path.Combine(Application.dataPath, "Audio", "Announcer");
        
        if (!Directory.Exists(announcerRoot))
        {
            Debug.LogWarning("[Announcer Reprocess All] Assets/Audio/Announcer directory not found.");
            return;
        }
        
        string[] allAnnouncerFiles = Directory.GetFiles(announcerRoot, "*.*", SearchOption.AllDirectories)
            .Where(f => IsAudioFile(f))
            .ToArray();
        
        List<string> unusedFiles = new List<string>();
        
        foreach (string file in allAnnouncerFiles)
        {
            // Convert to Unity asset path format
            string relativePath = file.Substring(Application.dataPath.Length);
            string assetPath = ("Assets" + relativePath).Replace('\\', '/');
            
            if (!loadedPaths.Contains(assetPath))
            {
                unusedFiles.Add(assetPath);
            }
        }
        
        if (unusedFiles.Count == 0)
        {
            Debug.Log("[Announcer Reprocess All] All audio files in Assets/Audio/Announcer are in use.");
        }
        else
        {
            Debug.LogWarning($"[Announcer Reprocess All] === {unusedFiles.Count} UNUSED AUDIO FILES ===");
            foreach (string unused in unusedFiles)
            {
                Debug.LogWarning($"  [UNUSED] {unused}");
            }
        }
    }
    
    private static bool IsAudioFile(string path)
    {
        return path.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase) || 
               path.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase) || 
               path.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase);
    }
}
#endif
