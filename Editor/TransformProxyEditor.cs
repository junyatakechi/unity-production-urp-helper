using UnityEditor;
using UnityEngine;

namespace JayT.UnityProductionUrpHelper
{
    [CustomEditor(typeof(TransformProxy))]
    public class TransformProxyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Target
            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchMethod"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("point"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTag"));

            // Mode
            EditorGUILayout.Space();
            SerializedProperty modeProp = serializedObject.FindProperty("movingCopyMode");
            EditorGUILayout.PropertyField(modeProp);

            bool isRelative = (TransformProxy.MovingCopyMode)modeProp.enumValueIndex == TransformProxy.MovingCopyMode.Relative;
            if (isRelative)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("relativePositionScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("relativeRotationScale"));
                EditorGUI.indentLevel--;
            }

            // Copy Position
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Copy Position", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyPositionX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyPositionY"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyPositionZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("positionOffset"));

            // Copy Rotation
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Copy Rotation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyRotationX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyRotationY"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyRotationZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationOffset"));


            serializedObject.ApplyModifiedProperties();
        }
    }
}
