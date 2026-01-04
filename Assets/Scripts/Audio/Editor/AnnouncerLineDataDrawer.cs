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
            //SerializedProperty lineProp = property.FindPropertyRelative("line");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), lineProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw lineType enum
            //SerializedProperty lineTypeProp = property.FindPropertyRelative("lineType");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), lineTypeProp);
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
                // Draw auto-fill button
                if (GUI.Button(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Auto-Fill From Clipboard"))
                {
                    string clipboardPath = EditorGUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(clipboardPath))
                    {
                        AutoPopulateFromClipboard(property, clipboardPath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Clipboard Empty", "Please copy a folder path to your clipboard first.", "OK");
                    }
                }
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
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
        
        SerializedProperty lineTypeProp = property.FindPropertyRelative("lineType");
        AudioAnnouncerManager.LineType lineType = (AudioAnnouncerManager.LineType)lineTypeProp.enumValueIndex;
        
        if (lineType == AudioAnnouncerManager.LineType.Unique)
        {
            SerializedProperty uniqueClipsProp = property.FindPropertyRelative("uniqueClips");
            height += EditorGUI.GetPropertyHeight(uniqueClipsProp, true);
        }
        else if (lineType == AudioAnnouncerManager.LineType.CarSpecific)
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // button
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
    
    private void AutoPopulateFromClipboard(SerializedProperty lineDataProperty, string clipboardPath)
    {
        // Clean up the path - remove quotes and "Assets/" prefix if present
        clipboardPath = clipboardPath.Trim().Trim('"').Trim('\'');
        if (clipboardPath.StartsWith("Assets/"))
            clipboardPath = clipboardPath.Substring("Assets/".Length);
        if (clipboardPath.StartsWith("Assets\\"))
            clipboardPath = clipboardPath.Substring("Assets\\".Length);
        
        // Auto-populate using this path (don't store it)
        PopulateCarClips(lineDataProperty, clipboardPath);
        
        lineDataProperty.serializedObject.ApplyModifiedProperties();
    }
    
    private void PopulateCarClips(SerializedProperty lineDataProperty, string folderPath)
    {
        // Construct the full path
        string fullPath = Path.Combine(Application.dataPath, folderPath);
        
        if (!Directory.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Folder Not Found", 
                $"The folder '{folderPath}' does not exist in the Assets directory.\n\nFull path: {fullPath}", 
                "OK");
            return;
        }
        
        // Get all audio files in the folder
        string[] audioFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase) || 
                       f.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase) || 
                       f.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        if (audioFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("No Audio Files", 
                $"No audio files (.wav, .mp3, .ogg) found in '{folderPath}'", 
                "OK");
            return;
        }
        
        // Group files by car model name
        Dictionary<ModelName, List<AudioClip>> carClipsDict = new Dictionary<ModelName, List<AudioClip>>();
        
        // Get all ModelName enum values
        ModelName[] allModels = (ModelName[])System.Enum.GetValues(typeof(ModelName));
        
        foreach (string audioFile in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(audioFile);
            
            // Try to match the filename to a car model (exact match)
            ModelName matchedModel = ModelName.Unknown;
            foreach (ModelName model in allModels)
            {
                if (model == ModelName.Unknown) continue;
                
                string modelName = model.ToString();
                // Exact match: filename must be exactly "CarName" or "CarName" followed by digits
                // This prevents X52 from matching X52Ice
                if (fileName.Equals(modelName, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedModel = model;
                    break;
                }
                else if (fileName.Length > modelName.Length && 
                         fileName.StartsWith(modelName, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the remaining part is just digits
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
                Debug.LogWarning($"Could not match file '{fileName}' to any car model. Skipping.");
                continue;
            }
            
            // Load the audio clip
            string assetPath = "Assets/" + folderPath + "/" + Path.GetFileName(audioFile);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            
            if (clip != null)
            {
                if (!carClipsDict.ContainsKey(matchedModel))
                {
                    carClipsDict[matchedModel] = new List<AudioClip>();
                }
                carClipsDict[matchedModel].Add(clip);
            }
            else
            {
                Debug.LogWarning($"Could not load audio clip at path: {assetPath}");
            }
        }
        
        if (carClipsDict.Count == 0)
        {
            EditorUtility.DisplayDialog("No Matches", 
                "No audio files could be matched to car models. Make sure file names contain the car model name (e.g., 'Skull_01.wav', 'Guardian_LapComplete.mp3').", 
                "OK");
            return;
        }
        
        // Update the carSpecificClips property
        SerializedProperty carSpecificClipsProp = lineDataProperty.FindPropertyRelative("carSpecificClips");
        
        // Clear existing car-specific clips
        carSpecificClipsProp.ClearArray();
        
        // Add new clips
        int clipCount = 0;
        foreach (var kvp in carClipsDict)
        {
            carSpecificClipsProp.InsertArrayElementAtIndex(carSpecificClipsProp.arraySize);
            SerializedProperty newCarClipsProp = carSpecificClipsProp.GetArrayElementAtIndex(carSpecificClipsProp.arraySize - 1);
            
            SerializedProperty carModelProp = newCarClipsProp.FindPropertyRelative("carModel");
            SerializedProperty clipsProp = newCarClipsProp.FindPropertyRelative("clips");
            
            carModelProp.enumValueIndex = (int)kvp.Key;
            
            clipsProp.ClearArray();
            foreach (AudioClip clip in kvp.Value)
            {
                clipsProp.InsertArrayElementAtIndex(clipsProp.arraySize);
                clipsProp.GetArrayElementAtIndex(clipsProp.arraySize - 1).objectReferenceValue = clip;
                clipCount++;
            }
        }
        
        EditorUtility.DisplayDialog("Success", 
            $"Auto-populated {clipCount} audio clips for {carClipsDict.Count} car models.", 
            "OK");
    }
}
#endif
