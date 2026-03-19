#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Entities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    [CustomEditor(typeof(EntityLogic)), CanEditMultipleObjects]
    public class EntityLogicEditor : Editor
    {
        private SerializedProperty isDisabledProp;
        private SerializedProperty logicNameProp;
        private SerializedProperty logicDescriptionProp;
        private SerializedProperty tickBufferProp;
        private SerializedProperty statesProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Configuration", "States", "Runtime" };

        private ReorderableList statesReorderableList;

        private void OnEnable()
        {
            try
            {
                if (targets == null || targets.Length == 0 || targets[0] == null)
                    return;

                isDisabledProp = serializedObject.FindProperty("isDisabled");
                logicNameProp = serializedObject.FindProperty("logicName");
                logicDescriptionProp = serializedObject.FindProperty("logicDescription");
                tickBufferProp = serializedObject.FindProperty("tickBuffer");
                statesProp = serializedObject.FindProperty("states");

                SetupStatesReorderableList();
            }
            catch (System.Exception)
            {
                // Target is null or destroyed, ignore
            }
        }

        private void SetupStatesReorderableList()
        {
            if (statesProp == null)
                return;

            statesReorderableList = new ReorderableList(serializedObject, statesProp, true, true, true, true);

            statesReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "FSM States", EditorStyles.boldLabel);
            };

            statesReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= statesProp.arraySize)
                    return;

                SerializedProperty element = statesProp.GetArrayElementAtIndex(index);
                SerializedProperty stateNameProp = element.FindPropertyRelative("stateName");

                string stateName = string.IsNullOrEmpty(stateNameProp?.stringValue) ? $"[State {index}]" : stateNameProp.stringValue;

                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.PropertyField(rect, element, new GUIContent(stateName), true);
            };

            statesReorderableList.elementHeightCallback = (int index) =>
            {
                if (index >= statesProp.arraySize)
                    return EditorGUIUtility.singleLineHeight;

                SerializedProperty element = statesProp.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4;
            };

            statesReorderableList.onAddCallback = (ReorderableList list) =>
            {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;

                SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);
                SerializedProperty stateNameProp = newElement.FindPropertyRelative("stateName");
                if (stateNameProp != null)
                    stateNameProp.stringValue = $"New State {index}";
            };
        }

        public override void OnInspectorGUI()
        {
            try
            {
                if (targets == null || targets.Length == 0 || targets[0] == null)
                    return;
            }
            catch (System.Exception)
            {
                return;
            }

            serializedObject.Update();

            EditorGUILayout.HelpBox("Handles the connection between flow and logic pieces. Uses a data asset to know and mark the flow of the logic.", MessageType.Info);
            GUILayout.Space(6);

            // Toolbar
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames, 24);
            GUILayout.Space(6);

            // Tab content
            switch (selectedTab)
            {
                case 0:
                    DrawConfigurationTab();
                    break;
                case 1:
                    DrawStatesTab();
                    break;
                case 2:
                    DrawRuntimeDebuggingSection();
                    break;
            }
            
            // Launch Node Editor Button
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Launch Node Editor", GUILayout.Height(30)))
            {
                EntityNodalWindow.OpenWithContext((EntityLogic)target);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigurationTab()
        {
            // Configuration Properties
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            if (isDisabledProp != null)
                EditorGUILayout.PropertyField(isDisabledProp, new GUIContent("Is Disabled", "If enabled, this logic will be skipped during processing."));
            if (logicNameProp != null)
                EditorGUILayout.PropertyField(logicNameProp, new GUIContent("Logic Name", "Name identifier for this logic."));
            if (logicDescriptionProp != null)
                EditorGUILayout.PropertyField(logicDescriptionProp, new GUIContent("Logic Description", "Description of what this logic does."));
            if (tickBufferProp != null)
                EditorGUILayout.PropertyField(tickBufferProp, new GUIContent("Tick Buffer", "Time buffer between ticks. Closer to zero means faster brain."));

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        private void DrawStatesTab()
        {
            EditorGUILayout.HelpBox("Define the FSM states for this logic. Each state contains actions to execute and conditions to evaluate for transitions.", MessageType.Info);
            GUILayout.Space(5);

            if (statesReorderableList != null && statesProp != null)
            {
                statesReorderableList.DoLayoutList();
            }
            else
            {
                EditorGUILayout.HelpBox("States property not found.", MessageType.Warning);
            }

            GUILayout.Space(5);

            // Quick state navigation
            if (statesProp != null && statesProp.arraySize > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Quick Navigation", EditorStyles.boldLabel);
                GUILayout.Space(3);

                EditorGUILayout.BeginHorizontal();
                int buttonsPerRow = 3;
                int buttonCount = 0;

                for (int i = 0; i < statesProp.arraySize; i++)
                {
                    SerializedProperty state = statesProp.GetArrayElementAtIndex(i);
                    SerializedProperty stateNameProp = state.FindPropertyRelative("stateName");
                    string stateName = string.IsNullOrEmpty(stateNameProp?.stringValue) ? $"State {i}" : stateNameProp.stringValue;

                    GUI.backgroundColor = state.isExpanded ? Color.green : Color.white;
                    if (GUILayout.Button(stateName, GUILayout.Height(22)))
                    {
                        // Toggle expansion and collapse others
                        for (int j = 0; j < statesProp.arraySize; j++)
                        {
                            statesProp.GetArrayElementAtIndex(j).isExpanded = (j == i) && !state.isExpanded;
                        }
                    }
                    GUI.backgroundColor = Color.white;

                    buttonCount++;
                    if (buttonCount >= buttonsPerRow && i < statesProp.arraySize - 1)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        buttonCount = 0;
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawRuntimeDebuggingSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🐛 Runtime Information", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime information is only available during play mode. Press Play to see logic state.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EntityLogic entityLogic = (EntityLogic)target;
            if (entityLogic == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            GUILayout.Space(5);

            // Time Display
            EditorGUILayout.LabelField("Time Information", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Time in State:", entityLogic.TimeInState);
            EditorGUILayout.FloatField("Time in Logic:", entityLogic.TimeInLogic);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            // Current State Info
            EditorGUILayout.LabelField("State Information", EditorStyles.boldLabel);
            if (entityLogic.CurrentState != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Current State:", entityLogic.CurrentState.stateName ?? "Unnamed State");
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Current State:", "None (Unready)");
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();

            // Force repaint for live updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
#endif
