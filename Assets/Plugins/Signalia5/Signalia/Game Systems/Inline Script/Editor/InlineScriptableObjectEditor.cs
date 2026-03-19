#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.External;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Editor
{
    [CustomEditor(typeof(InlineScriptableObject))]
    internal class InlineScriptableObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty inlineScriptProperty;

        private void OnEnable()
        {
            inlineScriptProperty = serializedObject.FindProperty("inlineScript");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var scriptableObject = (InlineScriptableObject)target;

            EditorGUILayout.Space();
            
            // Header
            EditorGUILayout.LabelField("Inline Script", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This ScriptableObject contains an inline script that can be executed from anywhere in your project. " +
                                  "This script runs in a non-MonoBehaviour context, so it doesn't have access to transform, gameObject, or other MonoBehaviour features.", MessageType.Info);
            
            EditorGUILayout.Space();

            // Script info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Script: {(scriptableObject.InlineScript != null ? "Assigned" : "None")}", EditorStyles.miniLabel);
            if (GUILayout.Button("Execute", GUILayout.Width(100)))
            {
                scriptableObject.Execute();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Draw the inline script field
            EditorGUILayout.PropertyField(inlineScriptProperty, new GUIContent("Inline Script"), true);

            EditorGUILayout.Space();

            // Utility buttons
            DrawUtilityButtons(scriptableObject);

            EditorGUILayout.Space();

            // Footer info
            EditorGUILayout.HelpBox("Tip: Use Execute() to run the script.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUtilityButtons(InlineScriptableObject scriptableObject)
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Set Empty Script"))
            {
                var newScript = new InlineVoid();
                scriptableObject.SetScript(newScript);
                EditorUtility.SetDirty(scriptableObject);
            }
            
            if (GUILayout.Button("Clear Script"))
            {
                if (EditorUtility.DisplayDialog("Clear Script", 
                    "Are you sure you want to clear the inline script? This action cannot be undone.", 
                    "Yes", "Cancel"))
                {
                    scriptableObject.ClearScript();
                    EditorUtility.SetDirty(scriptableObject);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
