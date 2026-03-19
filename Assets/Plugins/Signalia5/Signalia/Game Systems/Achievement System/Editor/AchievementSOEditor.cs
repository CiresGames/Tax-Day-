#if UNITY_EDITOR
using System;
using AHAKuo.Signalia.Framework;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem.Editors
{
    [CustomEditor(typeof(AchievementSO)), CanEditMultipleObjects]
    public class AchievementSOEditor : Editor
    {
        private SerializedProperty iconProp;
        private SerializedProperty titleProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty idProp;

        private int tabIndex;
        private readonly string[] tabs = { "Details", "ID" };

        private void OnEnable()
        {
            iconProp = serializedObject.FindProperty("icon");
            titleProp = serializedObject.FindProperty("title");
            descriptionProp = serializedObject.FindProperty("description");
            idProp = serializedObject.FindProperty("id");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);
            EditorGUILayout.HelpBox("AchievementSO defines icon/title/description and a unique ID used for saving and backend unlock calls.", MessageType.Info);
            GUILayout.Space(6);

            GUI.backgroundColor = Color.gray;
            tabIndex = GUILayout.Toolbar(tabIndex, tabs, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (tabIndex)
            {
                case 0:
                    DrawDetails();
                    break;
                case 1:
                    DrawIdentity();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon"));
            EditorGUILayout.PropertyField(titleProp, new GUIContent("Title"));
            EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Description"));
            EditorGUILayout.EndVertical();

            if (string.IsNullOrWhiteSpace(titleProp.stringValue))
                EditorGUILayout.HelpBox("Title is empty. This is what players will see in notifications/UI.", MessageType.Warning);
        }

        private void DrawIdentity()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(idProp, new GUIContent("ID", "Unique key used for saving/loading and backend calls."));

            if (GUILayout.Button("Generate", GUILayout.Width(90)))
            {
                idProp.stringValue = $"ach_{Guid.NewGuid():N}";
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrWhiteSpace(idProp.stringValue))
            {
                EditorGUILayout.HelpBox("ID is required. Without it, the achievement can't be reliably saved or unlocked via SIGS.UnlockAchievement(id).", MessageType.Error);
            }
            else if (idProp.stringValue.Contains(" "))
            {
                EditorGUILayout.HelpBox("ID contains spaces. Consider using snake_case to avoid save-key mismatches.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif

