using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.DialogueSystem.Utilities;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    [CustomEditor(typeof(DialogueStarter)), CanEditMultipleObjects]
    public class DialogueStarterEditor : Editor
    {
        private SerializedProperty styleNameProp;
        private SerializedProperty dgProp;

        private int selectedTab;
        private readonly string[] tabNames = { "Configuration", "Runtime" };

        private void OnEnable()
        {
            styleNameProp = serializedObject.FindProperty("styleName");
            dgProp = serializedObject.FindProperty("dg");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Starts dialogue when invoked. Assign a DialogueBook and optionally a style name; call StartDialogue() from UI or code.",
                MessageType.Info);
            GUILayout.Space(6);

            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames, 24);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawConfigurationTab();
                    break;
                case 1:
                    DrawRuntimeTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigurationTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Dialogue", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dgProp, new GUIContent("Dialogue Book", "The DialogueBook asset to play when starting dialogue."));
            EditorGUILayout.PropertyField(styleNameProp, new GUIContent("Style Name", "Dialogue style to use (e.g. \"default\")."));

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to start dialogue from this inspector.", MessageType.Info);
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("Start Dialogue", EditorStyles.boldLabel);
            var starter = (DialogueStarter)target;
            bool hasBook = starter != null && dgProp.objectReferenceValue != null;

            EditorGUI.BeginDisabledGroup(!hasBook);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start Dialogue", GUILayout.Height(28)))
            {
                if (starter != null)
                    starter.StartDialogue();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            if (!hasBook)
                EditorGUILayout.HelpBox("Assign a Dialogue Book in the Configuration tab first.", MessageType.Warning);

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
    }
}
