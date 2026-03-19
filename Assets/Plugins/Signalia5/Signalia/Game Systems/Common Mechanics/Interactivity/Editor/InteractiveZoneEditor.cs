#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.CommonMechanics;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Editors
{
    [CustomEditor(typeof(InteractiveZone)), CanEditMultipleObjects]
    public class InteractiveZoneEditor : Editor
    {
        private SerializedProperty boxSizeProp;
        private SerializedProperty boxOffsetProp;
        private SerializedProperty allowedLayersProp;
        private SerializedProperty use2DPhysicsProp;
        private SerializedProperty lookPointProp;
        private SerializedProperty lookAngleThresholdProp;
        private SerializedProperty requireLineOfSightProp;
        private SerializedProperty lineOfSightMaskProp;
        private SerializedProperty radioConditionsProp;
        private SerializedProperty scriptableConditionsProp;
        private SerializedProperty promptDisplayModeProp;
        private SerializedProperty promptViewProp;
        private SerializedProperty promptViewNameProp;
        private SerializedProperty promptTextProp;
        private SerializedProperty promptDeadKeyProp;
        private SerializedProperty defaultPromptTextProp;
        private SerializedProperty hidePromptOnDisableProp;
        private SerializedProperty onInteractProp;
        private SerializedProperty stringEventsProp;
        private SerializedProperty interactAudioProp;
        private SerializedProperty persistenceProp;
        private SerializedProperty saveKeyOverrideProp;
        private SerializedProperty autoDisableOnConsumedProp;
        private SerializedProperty interactionCooldownProp;
        private SerializedProperty requireReEntryProp;
        private SerializedProperty debugLogsProp;
        private SerializedProperty interactableProp;
        private SerializedProperty drawGizmosProp;
        private SerializedProperty gizmoColorProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Zone", "LookAt", "Prompt", "Events", "Audio", "Advanced" };

        private void OnEnable()
        {
            boxSizeProp = serializedObject.FindProperty("boxSize");
            boxOffsetProp = serializedObject.FindProperty("boxOffset");
            allowedLayersProp = serializedObject.FindProperty("allowedLayers");
            use2DPhysicsProp = serializedObject.FindProperty("use2DPhysics");
            lookPointProp = serializedObject.FindProperty("lookPoint");
            lookAngleThresholdProp = serializedObject.FindProperty("lookAngleThreshold");
            requireLineOfSightProp = serializedObject.FindProperty("requireLineOfSight");
            lineOfSightMaskProp = serializedObject.FindProperty("lineOfSightMask");
            radioConditionsProp = serializedObject.FindProperty("radioConditions");
            scriptableConditionsProp = serializedObject.FindProperty("scriptableConditions");
            promptDisplayModeProp = serializedObject.FindProperty("promptDisplayMode");
            promptViewProp = serializedObject.FindProperty("promptView");
            promptViewNameProp = serializedObject.FindProperty("promptViewName");
            promptTextProp = serializedObject.FindProperty("promptText");
            promptDeadKeyProp = serializedObject.FindProperty("promptDeadKey");
            defaultPromptTextProp = serializedObject.FindProperty("defaultPromptText");
            hidePromptOnDisableProp = serializedObject.FindProperty("hidePromptOnDisable");
            onInteractProp = serializedObject.FindProperty("onInteract");
            stringEventsProp = serializedObject.FindProperty("stringEvents");
            interactAudioProp = serializedObject.FindProperty("interactAudio");
            persistenceProp = serializedObject.FindProperty("persistence");
            saveKeyOverrideProp = serializedObject.FindProperty("saveKeyOverride");
            autoDisableOnConsumedProp = serializedObject.FindProperty("autoDisableOnConsumed");
            interactionCooldownProp = serializedObject.FindProperty("interactionCooldown");
            requireReEntryProp = serializedObject.FindProperty("requireReEntry");
            debugLogsProp = serializedObject.FindProperty("debugLogs");
            interactableProp = serializedObject.FindProperty("interactable");
            drawGizmosProp = serializedObject.FindProperty("drawGizmos");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox("Raycast box zone for interactions. Interactor must be inside the box and on an allowed layer. Optionally requires looking at the look point (controlled by Interactor).", MessageType.Info);

            GUILayout.Space(6);
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0: DrawZoneTab(); break;
                case 1: DrawLookAtTab(); break;
                case 2: DrawPromptTab(); break;
                case 3: DrawEventsTab(); break;
                case 4: DrawAudioTab(); break;
                case 5: DrawAdvancedTab(); break;
            }

            GUILayout.Space(8);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawZoneTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Raycast Box Zone", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(boxSizeProp, new GUIContent("Box Size", "Size of the interaction zone box."));
            EditorGUILayout.PropertyField(boxOffsetProp, new GUIContent("Box Offset", "Local offset from the transform position."));
            EditorGUILayout.PropertyField(allowedLayersProp, new GUIContent("Allowed Layers", "Only Interactor objects on these layers can interact."));
            EditorGUILayout.PropertyField(use2DPhysicsProp, new GUIContent("Use 2D Physics", "Use 2D calculations for the box check."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(drawGizmosProp, new GUIContent("Draw Gizmos", "Show zone box in Scene view."));
            if (drawGizmosProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Gizmo Color"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(radioConditionsProp, true);
            EditorGUILayout.PropertyField(scriptableConditionsProp, true);

            EditorGUILayout.HelpBox("Conditions block interaction if any check fails:\n" +
                "• Radio Conditions: Check Signalia Radio keys (LiveKey/DeadKey exists or bool values)\n" +
                "• Scriptable Conditions: ScriptableObject assets that inherit from InteractiveZoneConditionAsset\n\n" +
                "To create a Scriptable Condition asset:\n" +
                "1. Create a C# script that inherits from InteractiveZoneConditionAsset\n" +
                "2. Implement the abstract method: IsConditionMet(InteractiveZone zone, GameObject interactor)\n" +
                "3. Return true to allow interaction, false to block\n" +
                "4. Create a ScriptableObject asset instance from your script\n" +
                "5. Assign the asset to this list", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawLookAtTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Look-at checking is optional and controlled by the Interactor's 'Require LookAt' toggle. These settings define the look point and thresholds when look-at is required.", MessageType.Info);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Look Point", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lookPointProp, new GUIContent("Look Point", "The point the Interactor must look at. Defaults to box center if not set."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lookAngleThresholdProp, new GUIContent("Look Angle Threshold", "Maximum angle (degrees) between eye forward and look point direction."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Line of Sight", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(requireLineOfSightProp, new GUIContent("Require Line Of Sight", "Block interaction if something blocks the view."));
            if (requireLineOfSightProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lineOfSightMaskProp, new GUIContent("Line Of Sight Mask", "Layers checked for obstruction."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPromptTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(promptDisplayModeProp);

            var displayMode = (InteractiveZone.PromptDisplayMode)promptDisplayModeProp.enumValueIndex;
            if (displayMode == InteractiveZone.PromptDisplayMode.LocalUIView)
            {
                EditorGUILayout.PropertyField(promptViewProp);
            }
            else
            {
                EditorGUILayout.PropertyField(promptViewNameProp);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
            
            if (displayMode == InteractiveZone.PromptDisplayMode.LocalUIView)
            {
                EditorGUILayout.PropertyField(promptTextProp, new GUIContent("Prompt Text", "The TMP_Text component that displays the prompt text. Auto-found in children if not assigned."));
                
                if (promptTextProp.objectReferenceValue == null && promptViewProp.objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("TMP_Text will be auto-found in children of the Prompt View on Awake() if not assigned.", MessageType.Info);
                }
                else if (promptTextProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No TMP_Text assigned and no Prompt View assigned. Please assign a Prompt View or TMP_Text.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(promptDeadKeyProp, new GUIContent("Prompt Dead Key", "DeadKey name used to set the prompt text when using UIViewByName mode."));
            }
            
            EditorGUILayout.PropertyField(defaultPromptTextProp);

            GUILayout.Space(6);
            EditorGUILayout.PropertyField(hidePromptOnDisableProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onInteractProp, true);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("String Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stringEventsProp, true);

            EditorGUILayout.EndVertical();
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔊 Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure audio events for interactions. These audio keys should match entries in your AudioAsset scriptable objects.", MessageType.Info);

            GUILayout.Space(6);
            EditorHelpers.DrawAudioDropdown("Interact Audio", interactAudioProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when interaction occurs (uses SIGS.PlayAudio)", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(interactableProp, new GUIContent("Interactable", "Whether this zone is enabled for interaction. Controllable via API."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Interaction Buffer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(interactionCooldownProp, new GUIContent("Cooldown (seconds)", "Time in seconds before this zone can be interacted with again. Set to 0 for no cooldown."));
            EditorGUILayout.PropertyField(requireReEntryProp, new GUIContent("Require Re-Entry", "If enabled, the interactor must exit and re-enter the zone before interacting again."));
            EditorGUILayout.HelpBox("Cooldown and re-entry can be used together. Use API methods ClearCooldown(), ClearReEntryRequirement(), or ResetInteractionBuffer() to override.", MessageType.None);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(persistenceProp);

            var mode = (InteractiveZone.PersistenceMode)persistenceProp.enumValueIndex;
            if (mode == InteractiveZone.PersistenceMode.OneShot)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(autoDisableOnConsumedProp, new GUIContent("Auto Disable On Consumed", "Disable the zone after interaction. No saving - just disables it."));
                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("OneShot: Disables the zone after interaction. Does not save state.", MessageType.Info);
            }
            else if (mode == InteractiveZone.PersistenceMode.Consistent)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(saveKeyOverrideProp, new GUIContent("Save Key Override", "Optional custom key for saving. If empty, uses auto-generated key based on scene and hierarchy path."));
                EditorGUILayout.PropertyField(autoDisableOnConsumedProp, new GUIContent("Auto Disable On Consumed", "Disable the zone after interaction."));
                EditorGUI.indentLevel--;

                EditorGUILayout.HelpBox("Consistent: Disables the zone AND saves its consumed state using the Game Saving system. State persists across game sessions.", MessageType.Info);
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Interaction Blockers", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Interaction can be blocked by:\n" +
                "• Interactable flag (set via SetInteractable() API)\n" +
                "• Consumed state (OneShot/Consistent persistence)\n" +
                "• GameObject disabled or inactive\n" +
                "• Dialogue mode (if configured in Signalia Settings)\n" +
                "• Interaction cooldown\n" +
                "• Re-entry requirement\n" +
                "• Failed conditions (Radio/Behaviour/Scriptable)", MessageType.None);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(debugLogsProp);

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
