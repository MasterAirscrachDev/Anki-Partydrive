#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioAnnouncerManager.AnnouncerLineData))]
public class AnnouncerLineDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw line enum
            SerializedProperty lineProp = property.FindPropertyRelative("line");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), lineProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // Draw lineType enum
            SerializedProperty lineTypeProp = property.FindPropertyRelative("lineType");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), lineTypeProp);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            
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
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2; // lineType enum
        
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
}
#endif
