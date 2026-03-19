using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.Utilities.Editors
{
    [CustomEditor(typeof(SignaliaTime))]
    // bad place for this one but the context fits
    public class SignaliaTimeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
                return;
            EditorGUILayout.HelpBox("This component is used internally by the Signalia Input System. Don't manually add it.", MessageType.Warning);
        }
    }
    /// <summary>
    /// Custom Editor for TimeModifierComponent. Provides a clean inspector interface
    /// for configuring time scale modification settings and testing them at runtime.
    /// </summary>
    [CustomEditor(typeof(TimeModifierComponent))]
    [CanEditMultipleObjects]
    public class TimeModifierComponentEditor : Editor
    {
        private SerializedProperty setTimeScaleProp;
        private SerializedProperty slowDownScaleProp;
        private SerializedProperty temporaryDurationProp;
        private SerializedProperty freezeFrameDurationProp;
        private SerializedProperty useUnscaledTimeForTemporaryProp;
        private SerializedProperty restoreOnDisableProp;

        // Runtime custom scale values
        private float customTemporaryScale = 0.5f;
        private float customTemporaryDuration = 1f;
        private float customPersistentScale = 0.5f;

        private void OnEnable()
        {
            setTimeScaleProp = serializedObject.FindProperty("setTimeScale");
            slowDownScaleProp = serializedObject.FindProperty("slowDownScale");
            temporaryDurationProp = serializedObject.FindProperty("temporaryDuration");
            freezeFrameDurationProp = serializedObject.FindProperty("freezeFrameDuration");
            useUnscaledTimeForTemporaryProp = serializedObject.FindProperty("useUnscaledTimeForTemporary");
            restoreOnDisableProp = serializedObject.FindProperty("restoreOnDisable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);

            // Default Templates Section
            EditorGUILayout.LabelField("Default Templates", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            if (setTimeScaleProp != null)
                EditorGUILayout.PropertyField(setTimeScaleProp, new GUIContent("Set Time Scale", "Time scale used for SetTime options."));
            if (slowDownScaleProp != null)
                EditorGUILayout.PropertyField(slowDownScaleProp, new GUIContent("Slow Down Scale", "Time scale used for slow down options."));
            if (temporaryDurationProp != null)
                EditorGUILayout.PropertyField(temporaryDurationProp, new GUIContent("Temporary Duration", "Duration used for temporary SetTime/SlowDown actions."));
            if (freezeFrameDurationProp != null)
                EditorGUILayout.PropertyField(freezeFrameDurationProp, new GUIContent("Freeze Frame Duration", "Duration used for quick freeze frame actions."));
            
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Behavior Section
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            if (useUnscaledTimeForTemporaryProp != null)
                EditorGUILayout.PropertyField(useUnscaledTimeForTemporaryProp, new GUIContent("Use Unscaled Time For Temporary", "Use unscaled time for temporary durations so they expire even when time is slowed."));
            if (restoreOnDisableProp != null)
                EditorGUILayout.PropertyField(restoreOnDisableProp, new GUIContent("Restore On Disable", "Restore the time scale if this component is disabled or destroyed."));
            
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Runtime Controls Section
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                float currentTimeScale = SignaliaTime.Instance != null ? SignaliaTime.ModifiersAggregate : Time.timeScale;
                EditorGUILayout.HelpBox($"Current Time Scale: {currentTimeScale:F2} (Modifiers: {SignaliaTime.ModifierCount})", MessageType.Info);
                
                EditorGUILayout.Space(5);

                TimeModifierComponent timeModifier = (TimeModifierComponent)target;

                // Set Time Scale Section
                EditorGUILayout.LabelField("Set Time Scale", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Temporary"))
                {
                    timeModifier.SetTimeScaleTemporary();
                }
                if (GUILayout.Button("Persistent"))
                {
                    timeModifier.SetTimeScalePersistent();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Slow Down Section
                EditorGUILayout.LabelField("Slow Down", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Temporary"))
                {
                    timeModifier.SlowDownTemporary();
                }
                if (GUILayout.Button("Persistent"))
                {
                    timeModifier.SlowDownPersistent();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Freeze Frame Section
                EditorGUILayout.LabelField("Freeze Frame", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Quick"))
                {
                    timeModifier.FreezeFrameQuick();
                }
                if (GUILayout.Button("One Frame"))
                {
                    timeModifier.FreezeFrameOneFrame();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Custom Scale Section
                EditorGUILayout.LabelField("Custom Scale", EditorStyles.miniLabel);
                
                // Temporary custom scale
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scale", GUILayout.Width(60));
                customTemporaryScale = EditorGUILayout.FloatField(customTemporaryScale, GUILayout.Width(80));
                EditorGUILayout.LabelField("Duration", GUILayout.Width(70));
                customTemporaryDuration = EditorGUILayout.FloatField(customTemporaryDuration, GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply Temporary", GUILayout.Width(120)))
                {
                    timeModifier.ApplyCustomScaleTemporary(customTemporaryScale, customTemporaryDuration);
                }
                EditorGUILayout.EndHorizontal();
                
                // Persistent custom scale
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scale", GUILayout.Width(60));
                customPersistentScale = EditorGUILayout.FloatField(customPersistentScale, GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply Persistent", GUILayout.Width(120)))
                {
                    timeModifier.ApplyCustomScalePersistent(customPersistentScale);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Clear Persistent
                if (GUILayout.Button("Clear Persistent", GUILayout.Height(25)))
                {
                    timeModifier.ClearPersistent();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime controls are only available during Play Mode.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

