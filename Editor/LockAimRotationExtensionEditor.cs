using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LockAimRotationExtension))]
public class LockAimRotationExtensionEditor : UnityEditor.Editor
{
    const float ToggleWidth = 18f;
    const float RowSpacing  = 4f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawAxisPassthrough("X  (Pitch)", serializedObject.FindProperty("X"));
        GUILayout.Space(RowSpacing);
        DrawAxisPassthrough("Y  (Yaw)",   serializedObject.FindProperty("Y"));
        GUILayout.Space(RowSpacing);
        DrawAxisPassthrough("Z  (Roll)",  serializedObject.FindProperty("Z"));

        serializedObject.ApplyModifiedProperties();
    }

    void DrawAxisPassthrough(string label, SerializedProperty prop)
    {
        var enabledProp = prop.FindPropertyRelative("Enabled");

        var rect = EditorGUILayout.GetControlRect();

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        float x = rect.x;

        var toggleRect = new Rect(x, rect.y, ToggleWidth, rect.height);
        var labelRect  = new Rect(x + ToggleWidth, rect.y, rect.width - ToggleWidth, rect.height);

        enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

        using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
            EditorGUI.LabelField(labelRect, new GUIContent(label, "Aimの調整を無視してAim前の回転をスループット"));

        EditorGUI.indentLevel = indent;
    }
}
