#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using AHAKuo.Signalia.Utilities.SIGInput;
using AHAKuo.Signalia.Framework.Editors;
using System.Linq;
using AHAKuo.Signalia.Framework;
using System.Collections.Generic;

namespace AHAKuo.Signalia.Utilities.SIGInput.Editors
{
    /// <summary>
    /// Custom editor for SignaliaActionMap data asset.
    /// Provides a clean interface for managing input action definitions.
    /// </summary>
    [CustomEditor(typeof(SignaliaActionMap))]
    [CanEditMultipleObjects]
    public class SignaliaActionMapEditor : Editor
    {
        private SerializedProperty mapNameProp;
        private SerializedProperty initialStateProp;
        private SerializedProperty blockedActionMapsProp;
        private SerializedProperty actionsProp;
        private ReorderableList actionsList;
        private ReorderableList blockedMapsList;
        private HashSet<int> selectedActionIndices = new HashSet<int>();

        private void OnEnable()
        {
            mapNameProp = serializedObject.FindProperty("MapName");
            initialStateProp = serializedObject.FindProperty("InitialState");
            blockedActionMapsProp = serializedObject.FindProperty("BlockedActionMaps");
            actionsProp = serializedObject.FindProperty("Actions");
            selectedActionIndices.Clear(); // Clear selection when switching targets
            SetupReorderableList();
        }

        private void SetupReorderableList()
        {
            blockedMapsList = new ReorderableList(serializedObject, blockedActionMapsProp, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "Blocked Action Maps", EditorStyles.boldLabel);
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty element = blockedActionMapsProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element,
                        GUIContent.none);
                },
                elementHeight = EditorGUIUtility.singleLineHeight + 4,
                onAddCallback = list =>
                {
                    int index = list.serializedProperty.arraySize;
                    list.serializedProperty.arraySize++;
                    list.index = index;
                    list.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
                }
            };

            actionsList = new ReorderableList(serializedObject, actionsProp, true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Input Actions", EditorStyles.boldLabel);
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = actionsProp.GetArrayElementAtIndex(index);
                    if (element == null) return;

                    // Draw selection indicator
                    bool isSelected = selectedActionIndices.Contains(index);
                    if (isSelected)
                    {
                        EditorGUI.DrawRect(rect, new Color(0.2f, 0.4f, 0.8f, 0.3f));
                    }

                    SerializedProperty actionNameProp = element.FindPropertyRelative("ActionName");
                    SerializedProperty actionTypeProp = element.FindPropertyRelative("ActionType");

                    float checkboxWidth = 20f;
                    float nameWidth = (rect.width - checkboxWidth) * 0.6f;
                    float typeWidth = (rect.width - checkboxWidth) * 0.35f;
                    float spacing = 5f;

                    Rect checkboxRect = new Rect(rect.x + 2, rect.y + 2, checkboxWidth - 4, EditorGUIUtility.singleLineHeight);
                    Rect nameRect = new Rect(rect.x + checkboxWidth, rect.y + 2, nameWidth - spacing, EditorGUIUtility.singleLineHeight);
                    Rect typeRect = new Rect(rect.x + checkboxWidth + nameWidth, rect.y + 2, typeWidth, EditorGUIUtility.singleLineHeight);

                    // Selection checkbox
                    bool newSelection = EditorGUI.Toggle(checkboxRect, isSelected);
                    if (newSelection != isSelected)
                    {
                        if (Event.current.control || Event.current.command)
                        {
                            // Multi-select mode
                            if (newSelection)
                                selectedActionIndices.Add(index);
                            else
                                selectedActionIndices.Remove(index);
                        }
                        else
                        {
                            // Single select mode
                            selectedActionIndices.Clear();
                            if (newSelection)
                                selectedActionIndices.Add(index);
                        }
                    }

                    EditorGUI.PropertyField(nameRect, actionNameProp, GUIContent.none);
                    EditorGUI.PropertyField(typeRect, actionTypeProp, GUIContent.none);
                },
                elementHeight = EditorGUIUtility.singleLineHeight + 4,
                onSelectCallback = (ReorderableList list) =>
                {
                    if (Event.current.control || Event.current.command)
                    {
                        // Multi-select mode
                        if (selectedActionIndices.Contains(list.index))
                            selectedActionIndices.Remove(list.index);
                        else
                            selectedActionIndices.Add(list.index);
                    }
                    else
                    {
                        // Single select mode
                        selectedActionIndices.Clear();
                        selectedActionIndices.Add(list.index);
                    }
                },
                onAddCallback = (ReorderableList list) =>
                {
                    int index = list.serializedProperty.arraySize;
                    list.serializedProperty.arraySize++;
                    list.index = index;

                    SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("ActionName").stringValue = "";
                    element.FindPropertyRelative("ActionType").enumValueIndex = 0;
                    
                    // Clear selection when adding
                    selectedActionIndices.Clear();
                },
                onRemoveCallback = (ReorderableList list) =>
                {
                    RemoveSelectedActions(list);
                },
                onReorderCallback = (ReorderableList list) =>
                {
                    // Clear selection after reordering as indices may have changed
                    selectedActionIndices.Clear();
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Define input actions for your game. Each action has a unique name and a type (Bool, Float, or Vector2). These actions are used by the Signalia Input Bridge system.", MessageType.Info);

            GUILayout.Space(10);

            // Map Name Section
            DrawMapNameSection();

            GUILayout.Space(10);

            // Initial State Section
            DrawInitialStateSection();

            GUILayout.Space(10);

            // Priority / Blocking Section
            DrawBlockingSection();

            GUILayout.Space(10);

            // Config Management Section
            DrawConfigManagementSection();

            GUILayout.Space(10);

            // Validation warnings
            DrawValidationWarnings();

            GUILayout.Space(5);

            // Actions list
            EditorGUILayout.LabelField("📋 Action Definitions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Add, remove, and configure input actions. Action names should be unique and descriptive (e.g., 'Jump', 'Move', 'Aim').", MessageType.None);

            actionsList.DoLayoutList();

            GUILayout.Space(10);

            // Quick actions
            DrawQuickActions();

            GUILayout.Space(10);

            // Info section
            DrawInfoSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMapNameSection()
        {
            EditorGUILayout.LabelField("🏷️ Action Map Name", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(mapNameProp, new GUIContent("Map Name", "The name used to identify this action map. Use InputStateModifier with this name to control entire action maps. If left empty, the asset name will be used."));
            
            SignaliaActionMap actionMap = (SignaliaActionMap)target;
            string displayName = string.IsNullOrWhiteSpace(actionMap.MapName) ? actionMap.name : actionMap.MapName;
            
            EditorGUILayout.HelpBox($"This action map will be identified as: '{displayName}'\n\nUse InputStateModifier with BlockedActionMaps to control this map, or configure Input Blockers in Signalia Settings.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawInitialStateSection()
        {
            EditorGUILayout.LabelField("🚦 Initial State", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (initialStateProp != null)
            {
                EditorGUILayout.PropertyField(
                    initialStateProp,
                    new GUIContent(
                        "Initial State",
                        "Defines whether this action map should start enabled or disabled when Signalia initializes runtime values."));
            }

            SignaliaActionMap actionMap = (SignaliaActionMap)target;
            string displayName = string.IsNullOrWhiteSpace(actionMap.MapName) ? actionMap.name : actionMap.MapName;

            string hint = actionMap.InitialState == SignaliaActionMapInitialState.Disabled
                ? $"This action map will start DISABLED on boot.\n\nUse InputStateModifier with ForceEnabledActionMaps to enable it at runtime."
                : $"This action map will start ENABLED on boot.\n\nUse InputStateModifier with BlockedActionMaps to disable it at runtime, or configure Input Blockers in Signalia Settings.";

            EditorGUILayout.HelpBox(hint, MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigManagementSection()
        {
            EditorGUILayout.LabelField("⚙️ Config Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var actionMap = target as SignaliaActionMap;
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            if (config == null)
            {
                EditorGUILayout.HelpBox("Signalia Config not found. Cannot manage references.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            bool isInConfig = IsActionMapInConfig(config, actionMap);

            // Show current status
            string statusText = isInConfig ? "✓ In Config" : "✗ Not In Config";
            Color statusColor = isInConfig ? Color.green : Color.red;

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", statusStyle);

            // Show explanation
            if (isInConfig)
            {
                EditorGUILayout.HelpBox("This Action Map is loaded in the Signalia config.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("This Action Map is not in the Signalia config.", MessageType.Warning);
            }

            GUILayout.Space(5);

            // Toggle button
            string buttonText = isInConfig ? "Remove from Config" : "Add to Config";
            Color buttonColor = isInConfig ? Color.red : Color.green;

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (isInConfig)
                {
                    RemoveActionMapFromConfig(config, actionMap);
                    EditorUtility.DisplayDialog("Action Map Removed",
                        $"Successfully removed '{actionMap.name}' from the Signalia config. " +
                        "This action map is now removed from the pipeline and will not be used by the input system.",
                        "OK");
                }
                else
                {
                    AddActionMapToConfig(config, actionMap);
                    EditorUtility.DisplayDialog("Action Map Added",
                        $"Successfully added '{actionMap.name}' to the Signalia config. " +
                        "This action map is now in the pipeline and will be used by the input system.",
                        "OK");
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawBlockingSection()
        {
            EditorGUILayout.LabelField("🧭 Action Map Priority", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            SignaliaActionMap actionMap = (SignaliaActionMap)target;
            string thisMapName = string.IsNullOrWhiteSpace(actionMap.MapName) ? actionMap.name : actionMap.MapName;

            EditorGUILayout.HelpBox(
                "If this action map is enabled, you can suppress input from other action maps (even if those maps are enabled). " +
                "This lets you author action map priority (e.g., a Menu map suppressing Gameplay).",
                MessageType.Info);

            blockedMapsList.DoLayoutList();

            // Validation warnings
            if (actionMap.BlockedActionMaps != null && actionMap.BlockedActionMaps.Count > 0)
            {
                // Self reference
                if (actionMap.BlockedActionMaps.Any(m => m == actionMap))
                {
                    EditorGUILayout.HelpBox("🚨 This map is blocking itself. This will be ignored at runtime.", MessageType.Warning);
                }

                // Duplicate references
                var duplicates = actionMap.BlockedActionMaps
                    .Where(m => m != null)
                    .GroupBy(m => m)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Count > 0)
                {
                    EditorGUILayout.HelpBox($"⚠️ Duplicate blocked-map entries detected: {string.Join(", ", duplicates.Select(d => d.name))}", MessageType.Warning);
                }

                // Mutual blocking warning (simple heuristic)
                var mutual = actionMap.BlockedActionMaps
                    .Where(m => m != null && m.BlockedActionMaps != null && m.BlockedActionMaps.Contains(actionMap))
                    .ToList();

                if (mutual.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"⚠️ Mutual blocking detected between '{thisMapName}' and: {string.Join(", ", mutual.Select(m => string.IsNullOrWhiteSpace(m.MapName) ? m.name : m.MapName))}\n" +
                        "This can lead to confusing priority behavior. Prefer one-directional blocking.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private bool IsActionMapInConfig(SignaliaConfigAsset config, SignaliaActionMap actionMap)
        {
            if (config == null || config.InputActionMaps == null || actionMap == null)
                return false;

            foreach (var map in config.InputActionMaps)
            {
                if (map == actionMap)
                    return true;
            }

            return false;
        }

        private void AddActionMapToConfig(SignaliaConfigAsset config, SignaliaActionMap actionMap)
        {
            if (config == null || actionMap == null)
                return;

            var list = new System.Collections.Generic.List<SignaliaActionMap>(config.InputActionMaps ?? new SignaliaActionMap[0]);

            if (!list.Contains(actionMap))
            {
                list.Add(actionMap);
                config.InputActionMaps = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void RemoveActionMapFromConfig(SignaliaConfigAsset config, SignaliaActionMap actionMap)
        {
            if (config == null || actionMap == null)
                return;

            var list = new System.Collections.Generic.List<SignaliaActionMap>(config.InputActionMaps ?? new SignaliaActionMap[0]);

            if (list.Remove(actionMap))
            {
                config.InputActionMaps = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawValidationWarnings()
        {
            SignaliaActionMap actionMap = (SignaliaActionMap)target;

            if (actionMap.Actions == null || actionMap.Actions.Count == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No actions defined. Add at least one action to use this action map.", MessageType.Warning);
                return;
            }

            // Check for duplicate action names
            var duplicateNames = actionMap.Actions
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.ActionName))
                .GroupBy(a => a.ActionName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"🚨 Duplicate action names detected: {string.Join(", ", duplicateNames)}\nEach action name must be unique!",
                    MessageType.Error);
            }

            // Check for empty action names
            // int emptyCount = actionMap.Actions.Count(a => a != null && string.IsNullOrWhiteSpace(a.ActionName));
            // if (emptyCount > 0)
            // {
            //     EditorGUILayout.HelpBox(
            //         $"⚠️ {emptyCount} action(s) have empty names. Action names are required for the input system to work properly.",
            //         MessageType.Warning);
            // }

            // Check for null actions
            int nullCount = actionMap.Actions.Count(a => a == null);
            if (nullCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ {nullCount} null action(s) detected. Remove null entries to keep the action map clean.",
                    MessageType.Warning);
            }
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("⚡ Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("➕ Add Common Actions", GUILayout.Height(25)))
            {
                AddCommonActions();
            }

            if (GUILayout.Button("🧹 Remove Empty Actions", GUILayout.Height(25)))
            {
                RemoveEmptyActions();
            }

            if (GUILayout.Button("🔍 Validate All", GUILayout.Height(25)))
            {
                ValidateAllActions();
            }

            EditorGUILayout.EndHorizontal();

            // Multi-selection actions
            if (selectedActionIndices.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"Selected: {selectedActionIndices.Count} action(s)", EditorStyles.miniLabel);
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button($"🗑️ Remove Selected ({selectedActionIndices.Count})", GUILayout.Height(25)))
                {
                    RemoveSelectedActions(actionsList);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Clear Selection", GUILayout.Height(25), GUILayout.Width(120)))
                {
                    selectedActionIndices.Clear();
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("💡 Tip: Hold Ctrl/Cmd and click to multi-select actions. Use the checkboxes or click on items to select.", MessageType.Info);
            }
            else
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("💡 Tip: Hold Ctrl/Cmd and click to multi-select actions for bulk removal.", MessageType.None);
            }
        }

        private void RemoveSelectedActions(ReorderableList list)
        {
            if (selectedActionIndices.Count == 0)
            {
                // Fallback to single item removal if nothing is selected
                if (list.index >= 0 && list.index < list.serializedProperty.arraySize)
                {
                    string actionName = list.serializedProperty.GetArrayElementAtIndex(list.index).FindPropertyRelative("ActionName").stringValue;
                    
                    if (EditorUtility.DisplayDialog(
                        "Remove Action",
                        $"Are you sure you want to remove action '{actionName}'?",
                        "Remove",
                        "Cancel"))
                    {
                        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                        if (list.index >= list.serializedProperty.arraySize)
                            list.index = list.serializedProperty.arraySize - 1;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                        Repaint();
                    }
                }
                return;
            }

            // Get action names for confirmation dialog
            var actionNames = new List<string>();
            var sortedIndices = selectedActionIndices.OrderByDescending(i => i).ToList();
            
            foreach (int index in sortedIndices)
            {
                if (index >= 0 && index < list.serializedProperty.arraySize)
                {
                    var element = list.serializedProperty.GetArrayElementAtIndex(index);
                    string actionName = element.FindPropertyRelative("ActionName").stringValue;
                    if (string.IsNullOrWhiteSpace(actionName))
                        actionName = "<unnamed>";
                    actionNames.Add(actionName);
                }
            }

            string message = selectedActionIndices.Count == 1
                ? $"Are you sure you want to remove action '{actionNames[0]}'?"
                : $"Are you sure you want to remove {selectedActionIndices.Count} actions?\n\n{string.Join(", ", actionNames.Take(5))}" + (actionNames.Count > 5 ? "..." : "");

            if (EditorUtility.DisplayDialog(
                "Remove Actions",
                message,
                "Remove",
                "Cancel"))
            {
                SignaliaActionMap actionMap = (SignaliaActionMap)target;
                Undo.RecordObject(actionMap, "Remove Selected Actions");

                // Remove from highest index to lowest to preserve indices
                foreach (int index in sortedIndices)
                {
                    if (index >= 0 && index < list.serializedProperty.arraySize)
                    {
                        list.serializedProperty.DeleteArrayElementAtIndex(index);
                    }
                }

                selectedActionIndices.Clear();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(actionMap);
                Repaint();
            }
        }

        private void AddCommonActions()
        {
            SignaliaActionMap actionMap = (SignaliaActionMap)target;
            Undo.RecordObject(actionMap, "Add Common Actions");

            string[] commonActions = new[]
            {
                "Jump", "Interact", "Attack", "Dash", "Pause", "Menu", "Cancel", "Confirm"
            };

            foreach (string actionName in commonActions)
            {
                // Check if action already exists
                if (actionMap.Actions.Any(a => a != null && a.ActionName == actionName))
                    continue;

                SignaliaActionDefinition newAction = new SignaliaActionDefinition
                {
                    ActionName = actionName,
                    ActionType = SignaliaActionType.Bool
                };

                actionMap.Actions.Add(newAction);
            }

            EditorUtility.SetDirty(actionMap);
            serializedObject.Update();
        }

        private void RemoveEmptyActions()
        {
            SignaliaActionMap actionMap = (SignaliaActionMap)target;
            Undo.RecordObject(actionMap, "Remove Empty Actions");

            int removedCount = actionMap.Actions.RemoveAll(a => a == null || string.IsNullOrWhiteSpace(a.ActionName));

            if (removedCount > 0)
            {
                EditorUtility.SetDirty(actionMap);
                serializedObject.Update();
                Debug.Log($"Removed {removedCount} empty action(s).");
            }
            else
            {
                EditorUtility.DisplayDialog("No Empty Actions", "No empty actions found to remove.", "OK");
            }
        }

        private void ValidateAllActions()
        {
            SignaliaActionMap actionMap = (SignaliaActionMap)target;
            var issues = new System.Collections.Generic.List<string>();

            if (actionMap.Actions == null || actionMap.Actions.Count == 0)
            {
                issues.Add("No actions defined");
            }
            else
            {
                // Check for duplicates
                var duplicates = actionMap.Actions
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.ActionName))
                    .GroupBy(a => a.ActionName)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicates.Count > 0)
                {
                    issues.Add($"Duplicate names: {string.Join(", ", duplicates)}");
                }

                // Check for empty names
                int emptyCount = actionMap.Actions.Count(a => a != null && string.IsNullOrWhiteSpace(a.ActionName));
                if (emptyCount > 0)
                {
                    issues.Add($"{emptyCount} action(s) with empty names");
                }

                // Check for null entries
                int nullCount = actionMap.Actions.Count(a => a == null);
                if (nullCount > 0)
                {
                    issues.Add($"{nullCount} null action(s)");
                }
            }

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "✓ All actions are valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Issues", string.Join("\n", issues), "OK");
            }
        }

        private void DrawInfoSection()
        {
            SignaliaActionMap actionMap = (SignaliaActionMap)target;

            EditorGUILayout.LabelField("ℹ️ Action Type Guide", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("• Bool: Binary on/off actions (e.g., Jump, Attack, Interact)", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• Float: Continuous value actions (e.g., Movement Speed, Trigger Pressure)", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• Vector2: 2D directional actions (e.g., Movement Direction, Mouse Position)", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // Statistics
            if (actionMap.Actions != null && actionMap.Actions.Count > 0)
            {
                int boolCount = actionMap.Actions.Count(a => a != null && a.ActionType == SignaliaActionType.Bool);
                int floatCount = actionMap.Actions.Count(a => a != null && a.ActionType == SignaliaActionType.Float);
                int vector2Count = actionMap.Actions.Count(a => a != null && a.ActionType == SignaliaActionType.Vector2);

                EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Total Actions: {actionMap.Actions.Count}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Bool Actions: {boolCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Float Actions: {floatCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Vector2 Actions: {vector2Count}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(5);

            EditorGUILayout.HelpBox("💡 Tip: Assign this Action Map to your Signalia Config Asset (Input section) to use these actions in your game.", MessageType.Info);
        }
    }
}
#endif

