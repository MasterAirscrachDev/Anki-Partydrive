#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SFXEventData))]
public class SFXEventDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get properties
        SerializedProperty sfxEventProp = property.FindPropertyRelative("sfxEvent");
        SerializedProperty clipsProp = property.FindPropertyRelative("clips");
        
        // Create custom label "SFXEvent (number of clips)"
        string eventName = sfxEventProp.enumNames[sfxEventProp.enumValueIndex];
        int clipCount = clipsProp.arraySize;
        string customLabel = $"{eventName} ({clipCount} clips)";
        
        // Draw foldout with custom label
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), 
            property.isExpanded, 
            customLabel, 
            true
        );
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw sfxEvent enum
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), 
                sfxEventProp
            );
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw clips array
            float arrayHeight = EditorGUI.GetPropertyHeight(clipsProp, true);
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, arrayHeight), 
                clipsProp, 
                new GUIContent("Clips"), 
                true
            );
            yPos += arrayHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw volumeMultiplier
            SerializedProperty volumeProp = property.FindPropertyRelative("volumeMultiplier");
            EditorGUI.PropertyField(
                new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), 
                volumeProp
            );
            
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
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // sfxEvent enum
        
        SerializedProperty clipsProp = property.FindPropertyRelative("clips");
        height += EditorGUI.GetPropertyHeight(clipsProp, true) + EditorGUIUtility.standardVerticalSpacing; // clips array
        
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // volumeMultiplier
        
        return height;
    }
}
#endif
