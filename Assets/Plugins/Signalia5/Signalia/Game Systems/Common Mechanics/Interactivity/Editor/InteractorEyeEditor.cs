#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.CommonMechanics;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Editors
{
    [CustomEditor(typeof(InteractorEye)), CanEditMultipleObjects]
    public class InteractorEyeEditor : Editor
    {
        private SerializedProperty eyeOriginProp;
        private SerializedProperty originOffsetProp;
        private SerializedProperty scanIntervalProp;
        private SerializedProperty requireLookAtProp;
        private SerializedProperty drawGizmosProp;
        private SerializedProperty gizmoColorIdleProp;
        private SerializedProperty gizmoColorHitProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Scan", "Gizmos" };

        private void OnEnable()
        {
            eyeOriginProp = serializedObject.FindProperty("eyeOrigin");
            originOffsetProp = serializedObject.FindProperty("originOffset");
            scanIntervalProp = serializedObject.FindProperty("scanInterval");
            requireLookAtProp = serializedObject.FindProperty("requireLookAt");
            drawGizmosProp = serializedObject.FindProperty("drawGizmos");
            gizmoColorIdleProp = serializedObject.FindProperty("gizmoColorIdle");
            gizmoColorHitProp = serializedObject.FindProperty("gizmoColorHit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox("Scans for InteractiveZones. The eye must be inside a zone's box and on an allowed layer. Optionally requires looking at the zone's look point.", MessageType.Info);

            GUILayout.Space(6);
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0: DrawScanTab(); break;
                case 1: DrawGizmosTab(); break;
            }

            GUILayout.Space(8);
            DrawRuntimeInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScanTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Eye Origin", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(eyeOriginProp, new GUIContent("Eye Origin", "Transform to use as the eye position and forward. Defaults to this transform if not set."));
            EditorGUILayout.PropertyField(originOffsetProp, new GUIContent("Origin Offset", "Local offset from the eye origin."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Scan Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(scanIntervalProp, new GUIContent("Scan Interval", "How often to scan for zones (seconds). Set to 0 for every frame."));

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Look-At Requirement", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(requireLookAtProp, new GUIContent("Require Look At", "When enabled, the eye must be looking at the zone's look point to activate it. When disabled, just being inside the box is enough."));

            if (requireLookAtProp.boolValue)
            {
                EditorGUILayout.HelpBox("Look-at is required: The eye must be inside a zone's box AND looking at its look point (within the zone's angle threshold).", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Look-at is disabled: Simply being inside a zone's box (and on an allowed layer) will activate the interaction prompt.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGizmosTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(drawGizmosProp, new GUIContent("Draw Gizmos", "Show eye position and current target in Scene view."));

            if (drawGizmosProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(gizmoColorIdleProp, new GUIContent("Idle Color", "Gizmo color when no zone is targeted."));
                EditorGUILayout.PropertyField(gizmoColorHitProp, new GUIContent("Hit Color", "Gizmo color when a zone is targeted."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeInfo()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            var eye = (InteractorEye)target;
            if (eye != null)
            {
                var currentZone = eye.CurrentZone;
                if (currentZone != null)
                {
                    EditorGUILayout.LabelField("Current Zone:", currentZone.name);
                    EditorGUILayout.LabelField("Zone Interactable:", currentZone.IsInteractable ? "Yes" : "No");
                    EditorGUILayout.LabelField("Zone Consumed:", currentZone.IsConsumed ? "Yes" : "No");

                    GUILayout.Space(6);
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Try Interact", GUILayout.Height(25)))
                    {
                        eye.TryInteract();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.LabelField("Current Zone:", "None");
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif

