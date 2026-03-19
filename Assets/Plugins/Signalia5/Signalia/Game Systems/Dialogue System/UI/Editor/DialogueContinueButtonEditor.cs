#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.DialogueSystem;
using AHAKuo.Signalia.UI;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    [CustomEditor(typeof(DialogueContinueButton)), CanEditMultipleObjects]
    public class DialogueContinueButtonEditor : Editor
    {
        private SerializedProperty buttonProp;

        private void OnEnable()
        {
            buttonProp = serializedObject.FindProperty("button");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox("Represents a UI button used to continue dialogue sequences. This button signals progression to the next part of the dialogue when interacted with by the user. Requires a UIButton component.", MessageType.Info);

            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Component Reference", EditorStyles.boldLabel);
            
            DialogueContinueButton continueButton = (DialogueContinueButton)target;
            UIButton button = continueButton.GetComponent<UIButton>();
            
            if (button == null)
            {
                EditorGUILayout.HelpBox("⚠️ UIButton component is missing! This component requires a UIButton to function properly.", MessageType.Error);
                
                if (GUILayout.Button("Add UIButton Component", GUILayout.Height(25)))
                {
                    Undo.AddComponent<UIButton>(continueButton.gameObject);
                    EditorUtility.SetDirty(continueButton.gameObject);
                }
            }
            else
            {
                EditorGUILayout.ObjectField("UIButton Component", button, typeof(UIButton), true);
                EditorGUILayout.HelpBox("✓ UIButton component found. The button will automatically listen to continue events and input actions configured in Signalia Settings.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Input Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Continue actions are configured in Signalia Settings > Dialogue System. Default actions: 'Confirm', 'Submit', 'Interact'. The button will respond to these input actions when dialogue is active.", MessageType.Info);
            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            // Runtime information
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("🎮 Runtime Status", EditorStyles.boldLabel);
                
                bool inDialogue = DialogueManager.InDialogueNow;
                bool isActive = continueButton.isActiveAndEnabled;
                
                EditorGUILayout.LabelField("In Dialogue:", inDialogue ? "✓ Yes" : "✗ No");
                EditorGUILayout.LabelField("Button Active:", isActive ? "✓ Yes" : "✗ No");
                
                if (inDialogue && isActive)
                {
                    EditorGUILayout.HelpBox("Button is active and listening for continue input. Click the button or press the configured input actions to continue dialogue.", MessageType.Info);
                }
                else if (!inDialogue)
                {
                    EditorGUILayout.HelpBox("Dialogue system is not currently active. The button will respond when dialogue begins.", MessageType.None);
                }
                else if (!isActive)
                {
                    EditorGUILayout.HelpBox("Button GameObject is inactive. Enable it to allow continue functionality.", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(8);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif

