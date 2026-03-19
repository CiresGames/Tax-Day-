#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Editors
{
    [CustomEditor(typeof(Triggerbox)), CanEditMultipleObjects]
    public class TriggerboxEditor : Editor
    {
        private SerializedProperty triggerModeProp;
        private SerializedProperty triggerSpaceProp;
        private SerializedProperty raycastBoxSizeProp;
        private SerializedProperty raycastBoxOffsetProp;
        private SerializedProperty drawDebugGizmosProp;
        private SerializedProperty gizmoColorProp;
        private SerializedProperty useLayerFilterProp;
        private SerializedProperty allowedLayersProp;
        private SerializedProperty useTagFilterProp;
        private SerializedProperty requiredTagProp;
        private SerializedProperty useStayCooldownProp;
        private SerializedProperty stayCooldownProp;
        private SerializedProperty onEnterProp;
        private SerializedProperty onStayProp;
        private SerializedProperty onExitProp;
        private SerializedProperty onEnterStringProp;
        private SerializedProperty onStayStringProp;
        private SerializedProperty onExitStringProp;
        private SerializedProperty onEnterAudioProp;
        private SerializedProperty onStayAudioProp;
        private SerializedProperty onExitAudioProp;
        private SerializedProperty disableAfterTriggerProp;
        private SerializedProperty deactivateGameObjectProp;
        private SerializedProperty consistentSaveKeyProp;

        private int selectedTab;
        private readonly string[] tabs = { "General", "Filters", "Events", "Audio", "Disabling" };

        private void OnEnable()
        {
            triggerModeProp = serializedObject.FindProperty("triggerMode");
            triggerSpaceProp = serializedObject.FindProperty("triggerSpace");
            raycastBoxSizeProp = serializedObject.FindProperty("raycastBoxSize");
            raycastBoxOffsetProp = serializedObject.FindProperty("raycastBoxOffset");
            drawDebugGizmosProp = serializedObject.FindProperty("drawDebugGizmos");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
            useLayerFilterProp = serializedObject.FindProperty("useLayerFilter");
            allowedLayersProp = serializedObject.FindProperty("allowedLayers");
            useTagFilterProp = serializedObject.FindProperty("useTagFilter");
            requiredTagProp = serializedObject.FindProperty("requiredTag");
            useStayCooldownProp = serializedObject.FindProperty("useStayCooldown");
            stayCooldownProp = serializedObject.FindProperty("stayCooldown");
            onEnterProp = serializedObject.FindProperty("onEnter");
            onStayProp = serializedObject.FindProperty("onStay");
            onExitProp = serializedObject.FindProperty("onExit");
            onEnterStringProp = serializedObject.FindProperty("onEnterStringEvent");
            onStayStringProp = serializedObject.FindProperty("onStayStringEvent");
            onExitStringProp = serializedObject.FindProperty("onExitStringEvent");
            onEnterAudioProp = serializedObject.FindProperty("onEnterAudio");
            onStayAudioProp = serializedObject.FindProperty("onStayAudio");
            onExitAudioProp = serializedObject.FindProperty("onExitAudio");
            disableAfterTriggerProp = serializedObject.FindProperty("disableAfterTrigger");
            deactivateGameObjectProp = serializedObject.FindProperty("deactivateGameObject");
            consistentSaveKeyProp = serializedObject.FindProperty("consistentSaveKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Trigger boxes fire UnityEvents and Signalia string events when objects enter, stay inside, or exit the configured zone. Supports collider and raycast modes with optional persistence.", MessageType.Info);

            GUILayout.Space(6);
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawGeneralTab();
                    break;
                case 1:
                    DrawFiltersTab();
                    break;
                case 2:
                    DrawEventsTab();
                    break;
                case 3:
                    DrawAudioTab();
                    break;
                case 4:
                    DrawDisablingTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("Trigger Mode", "Choose whether the trigger relies on collider callbacks or a raycast zone."));
            EditorGUILayout.PropertyField(triggerSpaceProp, new GUIContent("Trigger Space", "Switch between 3D and 2D physics."));

            if ((Triggerbox.TriggerMode)triggerModeProp.enumValueIndex == Triggerbox.TriggerMode.Raycast)
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField("Raycast Zone", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(raycastBoxSizeProp, new GUIContent("Size", "World size of the raycast box (local space)."));
                EditorGUILayout.PropertyField(raycastBoxOffsetProp, new GUIContent("Offset", "Local offset from the transform origin."));
                EditorGUILayout.PropertyField(drawDebugGizmosProp, new GUIContent("Draw Gizmos", "Toggle scene gizmos for the raycast zone."));
                if (drawDebugGizmosProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Gizmo Color"));
                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Stay Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useStayCooldownProp, new GUIContent("Use Stay Cooldown", "Apply SIGS.CooldownGate between stay events."));
            if (useStayCooldownProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(stayCooldownProp, new GUIContent("Stay Cooldown", "Seconds between stay callbacks per object."));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawFiltersTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Layer & Tag Filters", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useLayerFilterProp, new GUIContent("Use Layer Filter"));
            if (useLayerFilterProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(allowedLayersProp, new GUIContent("Allowed Layers"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(useTagFilterProp, new GUIContent("Use Tag Filter"));
            if (useTagFilterProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                string newTag = EditorGUILayout.TagField("Required Tag", requiredTagProp.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    requiredTagProp.stringValue = newTag;
                }
                EditorGUI.indentLevel--;

                if (string.IsNullOrEmpty(requiredTagProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Tag filter is enabled but no tag has been provided.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onEnterProp, new GUIContent("On Enter"), true);
            EditorGUILayout.PropertyField(onStayProp, new GUIContent("On Stay"), true);
            EditorGUILayout.PropertyField(onExitProp, new GUIContent("On Exit"), true);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("String Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onEnterStringProp, new GUIContent("Enter Event", "String to SendEvent() when entering."));
            EditorGUILayout.PropertyField(onStayStringProp, new GUIContent("Stay Event", "String to SendEvent() while staying."));
            EditorGUILayout.PropertyField(onExitStringProp, new GUIContent("Exit Event", "String to SendEvent() on exit."));

            EditorGUILayout.EndVertical();
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔊 Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure audio events for trigger events. These audio keys should match entries in your AudioAsset scriptable objects.", MessageType.Info);

            GUILayout.Space(6);
            EditorHelpers.DrawAudioDropdown("Enter Audio", onEnterAudioProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when an object enters the trigger zone (uses SIGS.PlayAudio)", MessageType.None);

            GUILayout.Space(5);
            EditorHelpers.DrawAudioDropdown("Stay Audio", onStayAudioProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play while an object stays in the trigger zone (uses SIGS.PlayAudio)", MessageType.None);

            GUILayout.Space(5);
            EditorHelpers.DrawAudioDropdown("Exit Audio", onExitAudioProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when an object exits the trigger zone (uses SIGS.PlayAudio)", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawDisablingTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(disableAfterTriggerProp, new GUIContent("Disable Mode", "Control whether the trigger runs continuously or only once."));

            Triggerbox.DisableMode mode = (Triggerbox.DisableMode)disableAfterTriggerProp.enumValueIndex;
            if (mode != Triggerbox.DisableMode.None)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(deactivateGameObjectProp, new GUIContent("Deactivate GameObject", "Disable the GameObject when the trigger finishes instead of disabling just this component."));

                if (mode == Triggerbox.DisableMode.Consistent)
                {
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("Consistency", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(consistentSaveKeyProp, new GUIContent("Save Key", "Unique key stored via GameSaving to keep the trigger disabled."));
                    if (GUILayout.Button("Generate", GUILayout.Width(90f)))
                    {
                        GenerateConsistentKeys();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.HelpBox("Consistent mode requires the Signalia Save System package.", MessageType.Warning);

                    if (string.IsNullOrWhiteSpace(consistentSaveKeyProp.stringValue))
                    {
                        EditorGUILayout.HelpBox("Provide a unique save key so this trigger can remember its state across sessions.", MessageType.Error);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not Triggerbox trigger)
                    {
                        continue;
                    }

                    if (trigger.Mode == Triggerbox.TriggerMode.Collider)
                    {
                        if (trigger.Space == Triggerbox.TriggerSpace.Space3D)
                        {
                            if (!trigger.TryGetComponent(out Collider collider))
                            {
                                EditorGUILayout.HelpBox($"{trigger.name} is missing a Collider component set as trigger.", MessageType.Warning);
                                break;
                            }

                            if (!collider.isTrigger)
                            {
                                EditorGUILayout.HelpBox($"{trigger.name}'s Collider should be marked as a trigger for Triggerbox to work.", MessageType.Warning);
                                break;
                            }
                        }
                        else
                        {
                            if (!trigger.TryGetComponent(out Collider2D collider2D))
                            {
                                EditorGUILayout.HelpBox($"{trigger.name} is missing a Collider2D component set as trigger.", MessageType.Warning);
                                break;
                            }

                            if (!collider2D.isTrigger)
                            {
                                EditorGUILayout.HelpBox($"{trigger.name}'s Collider2D should be marked as a trigger for Triggerbox to work.", MessageType.Warning);
                                break;
                            }
                        }
                    }

                    if (trigger.Mode == Triggerbox.TriggerMode.Raycast && !trigger.DrawsGizmos())
                    {
                        EditorGUILayout.HelpBox("Consider enabling gizmos while editing to visualize the raycast bounds.", MessageType.Info);
                        break;
                    }
                }
            }
        }

        private void GenerateConsistentKeys()
        {
            foreach (Object targetObject in targets)
            {
                if (targetObject is not Triggerbox trigger)
                {
                    continue;
                }

                Undo.RecordObject(trigger, "Generate Consistent Save Key");
                string generatedKey = trigger.GenerateConsistentKey();
                SerializedObject so = new SerializedObject(targetObject);
                so.Update();
                SerializedProperty keyProp = so.FindProperty("consistentSaveKey");
                keyProp.stringValue = generatedKey;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(trigger);
            }

            serializedObject.Update();
        }
    }
}
#endif
