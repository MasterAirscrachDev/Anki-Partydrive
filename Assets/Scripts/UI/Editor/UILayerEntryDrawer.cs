#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UILayerEntry))]
public class UILayerEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty nameProp = property.FindPropertyRelative("name");
        SerializedProperty layerProp = property.FindPropertyRelative("layer");

        string displayName = string.IsNullOrEmpty(nameProp.stringValue) ? label.text : nameProp.stringValue;
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            displayName,
            true
        );

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                nameProp, new GUIContent("Name")
            );
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                layerProp, new GUIContent("Layer")
            );

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif
