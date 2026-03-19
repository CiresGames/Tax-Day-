using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities.SIGInput;
using System.Collections.Generic;
using System.Linq;

namespace AHAKuo.Signalia.Framework.Editors
{
    public partial class FrameworkSettings : EditorWindow
    {
        #region Tabs - Input System

        private void DrawInputSystemTab()
        {
            EditorGUILayout.HelpBox("Configure input blockers, cursor visibility behavior, and input buffering.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            EditorGUI.BeginChangeCheck();
            bool enableInputSystem = EditorGUILayout.Toggle(
                new GUIContent("Enable Signalia Input System",
                    "When disabled, Signalia will not read input actions, apply modifiers, or auto-load action maps."),
                config.InputSystem.EnableSignaliaInputSystem);

            if (EditorGUI.EndChangeCheck())
            {
                config.InputSystem.EnableSignaliaInputSystem = enableInputSystem;
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.HelpBox(
                enableInputSystem
                    ? "Signalia's input system is enabled. Configure blockers, cursor rules, and buffering below."
                    : "Signalia's input system is disabled. Input actions will not be read or enforced until you re-enable it.",
                enableInputSystem ? MessageType.Info : MessageType.Warning);

            if (!enableInputSystem)
            {
                EditorGUIUtility.labelWidth = 0;
                return;
            }

            GUILayout.Space(10);
            DrawInputBlockersSection();

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);
            DrawInputMiceVisibilitySection();

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);
            DrawInputBufferingSection();

            EditorGUIUtility.labelWidth = 0;
        }

        private void DrawInputBlockersSection()
        {
            EditorGUILayout.LabelField("Input Blockers:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Input blockers can disable specific action maps or actions when certain UIViews are shown. " +
                "When a blocker is triggered, it will block/unblock the configured targets.",
                MessageType.Info);

            if (config.InputSystem.InputBlockers == null)
            {
                config.InputSystem.InputBlockers = new InputBlocker[0];
                EditorUtility.SetDirty(config);
            }

            int blockerCount = config.InputSystem.InputBlockers.Length;
            EditorGUILayout.LabelField($"Configured Blockers: {blockerCount}", EditorStyles.miniBoldLabel);

            // Draw existing blockers
            for (int i = 0; i < config.InputSystem.InputBlockers.Length; i++)
            {
                var blocker = config.InputSystem.InputBlockers[i];

                GUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Blocker {i + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    var newList = new List<InputBlocker>(config.InputSystem.InputBlockers);
                    if (i >= 0 && i < newList.Count)
                    {
                        newList.RemoveAt(i);
                        config.InputSystem.InputBlockers = newList.ToArray();
                        EditorUtility.SetDirty(config);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();

                blocker.UIViewName = EditorGUILayout.TextField(
                    new GUIContent("UIView Name", "The Menu Name of the UIView (must match exactly)."),
                    blocker.UIViewName);

                GUILayout.Space(5);

                // Block Targets
                EditorGUILayout.LabelField("Block Targets:", EditorStyles.miniBoldLabel);

                // Action Maps or Actions toggle
                bool useActionMaps = blocker.ActionMapNames != null && blocker.ActionMapNames.Length > 0;
                bool useActions = !useActionMaps && blocker.ActionNames != null && blocker.ActionNames.Length > 0;

                bool newUseActionMaps = EditorGUILayout.Toggle(
                    new GUIContent("Block Action Maps", "When enabled, blocks entire action maps. When disabled, blocks specific actions."),
                    useActionMaps);

                if (newUseActionMaps != useActionMaps)
                {
                    if (newUseActionMaps)
                    {
                        blocker.ActionMapNames = new string[] { "" };
                        blocker.ActionNames = new string[0];
                    }
                    else
                    {
                        blocker.ActionMapNames = new string[0];
                        blocker.ActionNames = new string[] { "" };
                    }
                }

                if (newUseActionMaps)
                {
                    // Draw Action Maps
                    if (blocker.ActionMapNames == null)
                    {
                        blocker.ActionMapNames = new string[] { "" };
                    }

                    int mapCount = blocker.ActionMapNames.Length;
                    EditorGUILayout.LabelField($"Action Maps to Block: {mapCount}", EditorStyles.miniLabel);

                    var availableMapNames = GetAvailableActionMapNames();

                    for (int j = 0; j < blocker.ActionMapNames.Length; j++)
                    {
                        int mapIndex = j;
                        GUILayout.BeginHorizontal();

                        string mapName = blocker.ActionMapNames[mapIndex];
                        string newMapName = EditorGUILayout.TextField(
                            new GUIContent($"Map {mapIndex + 1}", "Action map name to block."),
                            mapName);

                        if (newMapName != mapName)
                        {
                            blocker.ActionMapNames[mapIndex] = newMapName;
                        }

                        // Dropdown button
                        Rect dropdownRect = GUILayoutUtility.GetRect(25, 18, GUILayout.Width(25));
                        if (GUI.Button(dropdownRect, "▼", EditorStyles.miniButton))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("(None)"), string.IsNullOrWhiteSpace(mapName), () =>
                            {
                                if (mapIndex >= 0 && mapIndex < blocker.ActionMapNames.Length)
                                {
                                    blocker.ActionMapNames[mapIndex] = "";
                                    EditorUtility.SetDirty(config);
                                }
                            });

                            if (availableMapNames.Count > 0)
                            {
                                menu.AddSeparator("");
                            }

                            foreach (string availableMap in availableMapNames)
                            {
                                bool isSelected = availableMap == mapName;
                                string capturedMap = availableMap;
                                menu.AddItem(new GUIContent(availableMap), isSelected, () =>
                                {
                                    if (mapIndex >= 0 && mapIndex < blocker.ActionMapNames.Length)
                                    {
                                        blocker.ActionMapNames[mapIndex] = capturedMap;
                                        EditorUtility.SetDirty(config);
                                    }
                                });
                            }

                            menu.ShowAsContext();
                        }

                        if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                        {
                            var newList = new List<string>(blocker.ActionMapNames);
                            if (mapIndex >= 0 && mapIndex < newList.Count)
                            {
                                newList.RemoveAt(mapIndex);
                                blocker.ActionMapNames = newList.ToArray();
                                EditorUtility.SetDirty(config);
                            }
                            GUILayout.EndHorizontal();
                            break;
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Action Map", GUILayout.Height(22)))
                    {
                        var newList = new List<string>(blocker.ActionMapNames ?? System.Array.Empty<string>());
                        newList.Add("");
                        blocker.ActionMapNames = newList.ToArray();
                        EditorUtility.SetDirty(config);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    // Draw Actions
                    if (blocker.ActionNames == null)
                    {
                        blocker.ActionNames = new string[] { "" };
                    }

                    int actionCount = blocker.ActionNames.Length;
                    EditorGUILayout.LabelField($"Actions to Block: {actionCount}", EditorStyles.miniLabel);

                    var availableActions = GetAvailableActionNames();

                    for (int j = 0; j < blocker.ActionNames.Length; j++)
                    {
                        int actionIndex = j;
                        GUILayout.BeginHorizontal();

                        string actionName = blocker.ActionNames[actionIndex];
                        string newActionName = EditorGUILayout.TextField(
                            new GUIContent($"Action {actionIndex + 1}", "Action name to block."),
                            actionName);

                        if (newActionName != actionName)
                        {
                            blocker.ActionNames[actionIndex] = newActionName;
                        }

                        // Dropdown button
                        Rect dropdownRect = GUILayoutUtility.GetRect(25, 18, GUILayout.Width(25));
                        if (GUI.Button(dropdownRect, "▼", EditorStyles.miniButton))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("(None)"), string.IsNullOrWhiteSpace(actionName), () =>
                            {
                                if (actionIndex >= 0 && actionIndex < blocker.ActionNames.Length)
                                {
                                    blocker.ActionNames[actionIndex] = "";
                                    EditorUtility.SetDirty(config);
                                }
                            });

                            if (availableActions.Count > 0)
                            {
                                menu.AddSeparator("");
                            }

                            foreach (string availableAction in availableActions)
                            {
                                bool isSelected = availableAction == actionName;
                                string capturedAction = availableAction;
                                menu.AddItem(new GUIContent(availableAction), isSelected, () =>
                                {
                                    if (actionIndex >= 0 && actionIndex < blocker.ActionNames.Length)
                                    {
                                        blocker.ActionNames[actionIndex] = capturedAction;
                                        EditorUtility.SetDirty(config);
                                    }
                                });
                            }

                            menu.ShowAsContext();
                        }

                        if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                        {
                            var newList = new List<string>(blocker.ActionNames);
                            if (actionIndex >= 0 && actionIndex < newList.Count)
                            {
                                newList.RemoveAt(actionIndex);
                                blocker.ActionNames = newList.ToArray();
                                EditorUtility.SetDirty(config);
                            }
                            GUILayout.EndHorizontal();
                            break;
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Action", GUILayout.Height(22)))
                    {
                        var newList = new List<string>(blocker.ActionNames ?? System.Array.Empty<string>());
                        newList.Add("");
                        blocker.ActionNames = newList.ToArray();
                        EditorUtility.SetDirty(config);
                    }
                    GUILayout.EndHorizontal();
                }

                blocker.Description = EditorGUILayout.TextField(
                    new GUIContent("Description", "Optional description for debugging."),
                    blocker.Description);

                if (EditorGUI.EndChangeCheck())
                {
                    config.InputSystem.InputBlockers[i] = blocker;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Blocker", GUILayout.Height(22)))
            {
                var newList = new List<InputBlocker>(config.InputSystem.InputBlockers ?? System.Array.Empty<InputBlocker>());
                newList.Add(new InputBlocker
                {
                    UIViewName = "",
                    ActionMapNames = new string[0],
                    ActionNames = new string[0],
                    Description = ""
                });
                config.InputSystem.InputBlockers = newList.ToArray();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Clear All", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Clear All Input Blockers",
                    "Are you sure you want to remove all input blockers?",
                    "Yes", "Cancel"))
                {
                    config.InputSystem.InputBlockers = System.Array.Empty<InputBlocker>();
                    EditorUtility.SetDirty(config);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawInputMiceVisibilitySection()
        {
            EditorGUILayout.LabelField("Cursor Visibility:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Configure cursor visibility behavior based on UIView visibility. The cursor can be shown, hidden, or hidden and locked to center.",
                MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.InputSystem.CursorVisibility.AutoAddCursorController = EditorGUILayout.Toggle(
                new GUIContent("Auto-Add Cursor Controller",
                    "Automatically add the InputMiceVisibilityController component to the scene when Watchman initializes."),
                config.InputSystem.CursorVisibility.AutoAddCursorController);

            config.InputSystem.CursorVisibility.DefaultVisibilityMode =
                (CursorVisibilityDefaultMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Default Visibility Mode",
                        "Baseline cursor behavior when no input state modifiers are active."),
                    config.InputSystem.CursorVisibility.DefaultVisibilityMode);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("View-Based Visibility Rules:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Configure cursor visibility rules for specific UIViews. When a view with a matching Menu Name becomes visible or hidden, " +
                "the configured cursor action is applied.",
                MessageType.Info);

            if (config.InputSystem.CursorVisibility.ViewVisibilityRules == null)
            {
                config.InputSystem.CursorVisibility.ViewVisibilityRules = new UIViewCursorVisibilityRule[0];
                EditorUtility.SetDirty(config);
            }

            int ruleCount = config.InputSystem.CursorVisibility.ViewVisibilityRules.Length;
            EditorGUILayout.LabelField($"Configured Rules: {ruleCount}", EditorStyles.miniBoldLabel);

            // Draw existing rules
            for (int i = 0; i < config.InputSystem.CursorVisibility.ViewVisibilityRules.Length; i++)
            {
                var rule = config.InputSystem.CursorVisibility.ViewVisibilityRules[i];

                GUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Rule {i + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    var newList = new List<UIViewCursorVisibilityRule>(config.InputSystem.CursorVisibility.ViewVisibilityRules);
                    if (i >= 0 && i < newList.Count)
                    {
                        newList.RemoveAt(i);
                        config.InputSystem.CursorVisibility.ViewVisibilityRules = newList.ToArray();
                        EditorUtility.SetDirty(config);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();

                rule.ViewName = EditorGUILayout.TextField(
                    new GUIContent("View Name", "The Menu Name of the UIView (must match exactly)."),
                    rule.ViewName);

                rule.OnViewVisible = (CursorVisibilityAction)EditorGUILayout.EnumPopup(
                    new GUIContent("On View Visible", "What happens to the cursor when this view becomes visible."),
                    rule.OnViewVisible);

                rule.OnViewHidden = (CursorVisibilityAction)EditorGUILayout.EnumPopup(
                    new GUIContent("On View Hidden", "What happens to the cursor when this view becomes hidden."),
                    rule.OnViewHidden);

                rule.Description = EditorGUILayout.TextField(
                    new GUIContent("Description", "Optional description for debugging."),
                    rule.Description);

                if (EditorGUI.EndChangeCheck())
                {
                    config.InputSystem.CursorVisibility.ViewVisibilityRules[i] = rule;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Rule", GUILayout.Height(22)))
            {
                var newList = new List<UIViewCursorVisibilityRule>(config.InputSystem.CursorVisibility.ViewVisibilityRules ?? System.Array.Empty<UIViewCursorVisibilityRule>());
                newList.Add(new UIViewCursorVisibilityRule
                {
                    ViewName = "",
                    OnViewVisible = CursorVisibilityAction.Show,
                    OnViewHidden = CursorVisibilityAction.HideAndLock,
                    Description = ""
                });
                config.InputSystem.CursorVisibility.ViewVisibilityRules = newList.ToArray();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Clear All", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Clear All Visibility Rules",
                    "Are you sure you want to remove all cursor visibility rules?",
                    "Yes", "Cancel"))
                {
                    config.InputSystem.CursorVisibility.ViewVisibilityRules = System.Array.Empty<UIViewCursorVisibilityRule>();
                    EditorUtility.SetDirty(config);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Dialogue System Rule:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Special rule for the Dialogue System. When dialogue is active, the cursor visibility will be automatically managed.",
                MessageType.Info);

            config.InputSystem.CursorVisibility.EnableDialogueSystemRule = EditorGUILayout.Toggle(
                new GUIContent("Enable Dialogue System Rule",
                    "When enabled, automatically manages cursor visibility when dialogue starts and ends."),
                config.InputSystem.CursorVisibility.EnableDialogueSystemRule);

            EditorGUI.BeginDisabledGroup(!config.InputSystem.CursorVisibility.EnableDialogueSystemRule);

            config.InputSystem.CursorVisibility.OnDialogueStart = (CursorVisibilityAction)EditorGUILayout.EnumPopup(
                new GUIContent("On Dialogue Start", "What happens to the cursor when dialogue starts."),
                config.InputSystem.CursorVisibility.OnDialogueStart);

            config.InputSystem.CursorVisibility.OnDialogueEnd = (CursorVisibilityAction)EditorGUILayout.EnumPopup(
                new GUIContent("On Dialogue End", "What happens to the cursor when dialogue ends."),
                config.InputSystem.CursorVisibility.OnDialogueEnd);

            EditorGUI.EndDisabledGroup();
        }

        private void DrawInputBufferingSection()
        {
            EditorGUILayout.LabelField("Input Buffering:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Input buffering allows inputs to be stored for a short time and executed when conditions allow. " +
                "For example, if a player presses Jump during a landing animation, the jump can be buffered and executed " +
                "automatically when the animation completes. Use SIGS.BufferInput() and SIGS.ConsumeBufferedInput() in code.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            config.InputSystem.EnableInputBuffering = EditorGUILayout.Toggle(
                new GUIContent("Enable Input Buffering",
                    "Enable the input buffering system. When enabled, inputs can be buffered using SIGS.BufferInput() and consumed with SIGS.ConsumeBufferedInput()."),
                config.InputSystem.EnableInputBuffering);

            EditorGUI.BeginDisabledGroup(!config.InputSystem.EnableInputBuffering);

            GUILayout.Space(5);

            config.InputSystem.DefaultBufferDuration = EditorGUILayout.Slider(
                new GUIContent("Default Buffer Duration",
                    "Default duration (in seconds) that buffered inputs will remain valid. Individual buffers can override this value."),
                config.InputSystem.DefaultBufferDuration,
                0.05f,
                2.0f);

            EditorGUILayout.HelpBox(
                $"Buffered inputs will expire after {config.InputSystem.DefaultBufferDuration:F2} seconds by default.",
                MessageType.None);

            GUILayout.Space(5);

            config.InputSystem.UseUnscaledTimeForBuffers = EditorGUILayout.Toggle(
                new GUIContent("Use Unscaled Time",
                    "When enabled, buffer expiration uses unscaled time (not affected by time scale changes). " +
                    "When disabled, buffers use scaled time (affected by time scale)."),
                config.InputSystem.UseUnscaledTimeForBuffers);

            EditorGUILayout.HelpBox(
                config.InputSystem.UseUnscaledTimeForBuffers
                    ? "Buffers will expire based on real time, unaffected by time scale changes."
                    : "Buffers will expire based on scaled time, affected by time scale changes.",
                MessageType.None);

            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }

            GUILayout.Space(10);

            // Usage example
            EditorGUILayout.LabelField("Usage Example:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "// Buffer an input when it can't execute immediately\n" +
                "if (SIGS.GetInputDown(\"Jump\") && isLanding)\n" +
                "{\n" +
                "    SIGS.BufferInput(\"Jump\", duration: 0.3f);\n" +
                "}\n\n" +
                "// Later, check and consume the buffered input\n" +
                "if (SIGS.ConsumeBufferedInput(\"Jump\"))\n" +
                "{\n" +
                "    PerformJump(); // Buffered input was found and consumed\n" +
                "}",
                MessageType.None);
        }

        private List<string> GetAvailableActionMapNames()
        {
            var mapNames = new List<string>();
            var configAsset = ConfigReader.GetConfig(true);
            if (configAsset?.InputActionMaps != null)
            {
                foreach (var map in configAsset.InputActionMaps)
                {
                    if (map != null)
                    {
                        string effectiveName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                        if (!string.IsNullOrWhiteSpace(effectiveName) && !mapNames.Contains(effectiveName))
                        {
                            mapNames.Add(effectiveName);
                        }
                    }
                }
            }
            return mapNames.OrderBy(x => x).ToList();
        }

        private List<string> GetAvailableActionNames()
        {
            var actionNames = new HashSet<string>();
            var configAsset = ConfigReader.GetConfig(true);
            if (configAsset?.InputActionMaps != null)
            {
                foreach (var map in configAsset.InputActionMaps)
                {
                    if (map?.Actions != null)
                    {
                        foreach (var action in map.Actions)
                        {
                            if (action != null && !string.IsNullOrWhiteSpace(action.ActionName))
                            {
                                actionNames.Add(action.ActionName);
                            }
                        }
                    }
                }
            }
            return actionNames.OrderBy(x => x).ToList();
        }

        #endregion
    }
}
