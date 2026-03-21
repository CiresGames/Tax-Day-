using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(QuestionData))]
public class QuestionDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("uid"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("question"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("answer"));

        QuestionData data = (QuestionData)target;

        // Only show choices if MULTIPLE_CHOICE is selected
        if (data.type == QuestionData.TYPE.MULTIPLE_CHOICE)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("choices"),
                includeChildren: true
            );
        }

        serializedObject.ApplyModifiedProperties();
    }
}