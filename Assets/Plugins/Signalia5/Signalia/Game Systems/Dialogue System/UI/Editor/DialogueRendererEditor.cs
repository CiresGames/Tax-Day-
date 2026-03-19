#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.DialogueSystem;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    [CustomEditor(typeof(DialogueRenderer)), CanEditMultipleObjects]
    public class DialogueRendererEditor : Editor
    {
        private SerializedProperty dialogueStyleNameProp;
        private SerializedProperty speechAreaProp;
        private SerializedProperty speakerNameProp;
        private SerializedProperty speechAreaContainerProp;
        private SerializedProperty speakerNameContainerProp;
        private SerializedProperty choicesContainerProp;
        private SerializedProperty configViewForDialogueProp;
        private SerializedProperty continueButtonProp;
        private SerializedProperty choiceButtonsContentProp;
        private SerializedProperty choiceButtonPrefabProp;
        private SerializedProperty choiceNotAPrefabProp;
        private SerializedProperty buttonWarmupProp;
        private SerializedProperty readingAnimationProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Configuration", "UI References", "Choices", "Customization" };

        private void OnEnable()
        {
            dialogueStyleNameProp = serializedObject.FindProperty("dialogueStyleName");
            speechAreaProp = serializedObject.FindProperty("speechArea");
            speakerNameProp = serializedObject.FindProperty("speakerName");
            speechAreaContainerProp = serializedObject.FindProperty("speechAreaContainer");
            speakerNameContainerProp = serializedObject.FindProperty("speakerNameContainer");
            choicesContainerProp = serializedObject.FindProperty("choicesContainer");
            configViewForDialogueProp = serializedObject.FindProperty("configViewForDialogue");
            continueButtonProp = serializedObject.FindProperty("continueButton");
            choiceButtonsContentProp = serializedObject.FindProperty("choiceButtonsContent");
            choiceButtonPrefabProp = serializedObject.FindProperty("choiceButtonPrefab");
            choiceNotAPrefabProp = serializedObject.FindProperty("choiceNotAPrefab");
            buttonWarmupProp = serializedObject.FindProperty("buttonWarmup");
            readingAnimationProp = serializedObject.FindProperty("readingAnimation");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox("Handles moving through dialogue and displaying it. This renderer displays dialogue with the configured style and manages UI elements for speech, speaker names, and choices.", MessageType.Info);

            GUILayout.Space(6);
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0: DrawConfigurationTab(); break;
                case 1: DrawUIReferencesTab(); break;
                case 2: DrawChoicesTab(); break;
                case 3: DrawCustomizationTab(); break;
            }

            GUILayout.Space(8);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigurationTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dialogue Style", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dialogueStyleNameProp, new GUIContent("Dialogue Style Name", "The dialogue style to use when displaying dialogue. This renderer will be used to display dialogue with this style. If empty, will use 'default'."));
            
            if (string.IsNullOrEmpty(dialogueStyleNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Dialogue Style Name is empty. Will use 'default' style.", MessageType.Info);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("View Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(configViewForDialogueProp, new GUIContent("Auto Config View", "If true, will automatically configure the view for dialogue using ApplyDialogueSystemSettings()."));
            EditorGUILayout.EndVertical();
        }

        private void DrawUIReferencesTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Text Elements", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(speechAreaProp, new GUIContent("Speech Area", "TMP_Text component that displays the dialogue speech text."));
            EditorGUILayout.PropertyField(speakerNameProp, new GUIContent("Speaker Name", "TMP_Text component that displays the speaker's name."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Container Views", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(speechAreaContainerProp, new GUIContent("Speech Area Container", "UIView that contains the speech area. Used for show/hide animations."));
            EditorGUILayout.PropertyField(speakerNameContainerProp, new GUIContent("Speaker Name Container", "UIView that contains the speaker name. Used for show/hide animations."));
            EditorGUILayout.PropertyField(choicesContainerProp, new GUIContent("Choices Container", "UIView that contains the choice buttons. Used for show/hide animations."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Button Elements", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(continueButtonProp, new GUIContent("Continue Button", "UIButton component used to continue dialogue. Listens to global continue event and can be manually clicked."));
            
            if (continueButtonProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Continue Button is not assigned. Dialogue progression may not work properly.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawChoicesTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Choice Buttons", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(choiceButtonsContentProp, new GUIContent("Choice Buttons Content", "Transform parent where choice buttons will be instantiated."));
            
            if (choiceButtonsContentProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Choice Buttons Content is not assigned. Choice buttons cannot be displayed.", MessageType.Warning);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Choice Button Prefab", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(choiceButtonPrefabProp, new GUIContent("Choice Button Prefab", "Prefab GameObject used for choice buttons. This is pooled for performance."));
            EditorGUILayout.PropertyField(choiceNotAPrefabProp, new GUIContent("Choice Not A Prefab", "When true, disables the prefab since it's in the scene and used not as a prefab."));
            
            if (choiceButtonPrefabProp.objectReferenceValue == null && !choiceNotAPrefabProp.boolValue)
            {
                EditorGUILayout.HelpBox("Choice Button Prefab is not assigned. Choice buttons cannot be created.", MessageType.Warning);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Pooling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(buttonWarmupProp, new GUIContent("Button Warmup", "Number of buttons to pre-instantiate in the pool. Helps prevent stuttering when adding choices."));
            
            if (buttonWarmupProp.intValue < 0)
            {
                EditorGUILayout.HelpBox("Button Warmup should be 0 or greater. Negative values will be ignored.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCustomizationTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Reading Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(readingAnimationProp, new GUIContent("Reading Animation", "Animation configuration for the speech area content. Controls how text appears during dialogue."));
            
            if (readingAnimationProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Reading Animation is not assigned. Text will appear instantly without animation.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif

