using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using System.IO;
using System;

using System.Linq;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Utilities.SIGInput;
using AHAKuo.Signalia.Framework;

// GAME SYSTEMS
using AHAKuo.Signalia.GameSystems.AudioLayering;
using AHAKuo.Signalia.GameSystems.SaveSystem;
using AHAKuo.Signalia.GameSystems.DialogueSystem;
using AHAKuo.Signalia.GameSystems.Inventory;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.GameSystems.TutorialSystem;
using AHAKuo.Signalia.GameSystems.Localization;
using AHAKuo.Signalia.GameSystems.Localization.External;
using AHAKuo.Signalia.GameSystems.LoadingScreens;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.GameSystems.PoolingSystem;
using AHAKuo.Signalia.GameSystems.Notifications;
using AHAKuo.Signalia.GameSystems.AchievementSystem;

namespace AHAKuo.Signalia.Framework.Editors
{
    public partial class FrameworkSettings : EditorWindow
    {
        private void DrawDialogueTab()
        {
            GUILayout.Space(10);
            // TODO: Add Dialogue header graphic when available
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.DialogueHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Configure Dialogue System behavior and input bindings.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.DialogueSystem.continueButtonDelay = EditorGUILayout.FloatField(
                new GUIContent("Continue Cooldown (secs)", "Minimum time between continue triggers."),
                config.DialogueSystem.continueButtonDelay);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Exit / Flow", EditorStyles.boldLabel);
            config.DialogueSystem.reshowOnExitObject = EditorGUILayout.Toggle(
                new GUIContent("Reshow On Exit Object", "Hide and re-show dialogue UI when chaining into an exit dialogue object."),
                config.DialogueSystem.reshowOnExitObject);
            config.DialogueSystem.reshowDelay = EditorGUILayout.FloatField(
                new GUIContent("Reshow Delay (secs)", "Delay before re-showing dialogue UI when chaining to an exit object."),
                config.DialogueSystem.reshowDelay);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Choice Display", EditorStyles.boldLabel);
            config.DialogueSystem.disableContinueButtonOnChoice = EditorGUILayout.Toggle(
                new GUIContent("Disable Continue On Choice", "When on a choice line, hide/disable the continue button."),
                config.DialogueSystem.disableContinueButtonOnChoice);
            config.DialogueSystem.hideSpeakerNameOnChoice = EditorGUILayout.Toggle(
                new GUIContent("Hide Speaker Name On Choice", "Hide the speaker name container while choices are shown."),
                config.DialogueSystem.hideSpeakerNameOnChoice);
            config.DialogueSystem.hideSpeechAreaOnChoice = EditorGUILayout.Toggle(
                new GUIContent("Hide Speech Area On Choice", "Hide the speech area container while choices are shown."),
                config.DialogueSystem.hideSpeechAreaOnChoice);
            config.DialogueSystem.ChoiceOmissionMode =
                (ChoiceOmission)EditorGUILayout.EnumPopup("Choice Omission Mode",
                    config.DialogueSystem.ChoiceOmissionMode);
            if (config.DialogueSystem.ChoiceOmissionMode == ChoiceOmission.ShowDisabled)
                config.DialogueSystem.ChoiceOmissionString = EditorGUILayout.TextField("Choice Omission String [Leave Empty for Default]",
                config.DialogueSystem.ChoiceOmissionString);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Continue Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Dialogue continue buttons can also be triggered by Signalia input actions.\n\n" +
                "Add one or more action names (e.g., 'Confirm', 'Submit'). These must exist in your SignaliaActionMap assets.",
                MessageType.Info);

            if (config.DialogueSystem.ContinueActionNames == null)
            {
                config.DialogueSystem.ContinueActionNames = new[] { "Confirm", "Submit", "Interact" };
                EditorUtility.SetDirty(config);
            }

            int actionCount = config.DialogueSystem.ContinueActionNames.Length;
            EditorGUILayout.LabelField($"Configured Continue Actions: {actionCount}", EditorStyles.miniBoldLabel);

            for (int i = 0; i < config.DialogueSystem.ContinueActionNames.Length; i++)
            {
                GUILayout.BeginHorizontal();
                config.DialogueSystem.ContinueActionNames[i] = EditorGUILayout.TextField(
                    new GUIContent($"Action {i + 1}", "Input action name to treat as a dialogue continue trigger."),
                    config.DialogueSystem.ContinueActionNames[i]);

                if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    var newList = new List<string>(config.DialogueSystem.ContinueActionNames);
                    if (i >= 0 && i < newList.Count)
                    {
                        newList.RemoveAt(i);
                        config.DialogueSystem.ContinueActionNames = newList.ToArray();
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
                var newList = new List<string>(config.DialogueSystem.ContinueActionNames ?? Array.Empty<string>());
                newList.Add("");
                config.DialogueSystem.ContinueActionNames = newList.ToArray();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Reset Defaults", GUILayout.Height(22)))
            {
                config.DialogueSystem.ContinueActionNames = new[] { "Confirm", "Submit", "Interact" };
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Clear", GUILayout.Height(22)))
            {
                config.DialogueSystem.ContinueActionNames = Array.Empty<string>();
                EditorUtility.SetDirty(config);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Action Map Management", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automatically manage action maps when dialogue starts/ends. This disables gameplay input and enables GUI input during dialogue.",
                MessageType.Info);

            config.DialogueSystem.EnableActionMapSwitching = EditorGUILayout.Toggle(
                new GUIContent("Enable Action Map Switching", "When enabled, automatically switches action maps when dialogue begins/ends."),
                config.DialogueSystem.EnableActionMapSwitching);

            if (config.DialogueSystem.EnableActionMapSwitching)
            {
                EditorGUI.indentLevel++;
                
                config.DialogueSystem.ActionReEnableDelay = EditorGUILayout.FloatField(
                    new GUIContent("Action Re-Enable Delay (secs)", "Delay in seconds before re-enabling action maps when dialogue ends. Default is 0.1 seconds."),
                    config.DialogueSystem.ActionReEnableDelay);
                
                GUILayout.Space(6);
                
                // Get available action maps for reference
                SignaliaConfigAsset configAsset = config;
                List<string> availableMapNames = new List<string>();
                Dictionary<string, string> assetNameToMapName = new Dictionary<string, string>(); // Maps asset name -> MapName
                Dictionary<string, string> mapNameToMapName = new Dictionary<string, string>(); // Maps MapName -> MapName (for lookup)
                
                if (configAsset != null && configAsset.InputActionMaps != null)
                {
                    foreach (var map in configAsset.InputActionMaps)
                    {
                        if (map != null)
                        {
                            // Always get the MapName property value directly from the asset
                            string mapNameProperty = map.MapName; // Get the actual MapName property value
                            string assetName = map.name;
                            
                            // Determine the effective map name: use MapName if set, otherwise use asset name (runtime behavior)
                            string effectiveMapName = string.IsNullOrWhiteSpace(mapNameProperty) ? assetName : mapNameProperty;
                            
                            // Store mapping from asset name to effective MapName (for resolving old values that might be asset names)
                            if (!string.IsNullOrWhiteSpace(assetName))
                            {
                                assetNameToMapName[assetName] = effectiveMapName;
                            }
                            
                            // Add to dropdown: prefer MapName property value, but if empty, use asset name
                            // This matches runtime behavior where empty MapName falls back to asset name
                            if (!string.IsNullOrWhiteSpace(effectiveMapName))
                            {
                                mapNameToMapName[effectiveMapName] = effectiveMapName;
                                if (!availableMapNames.Contains(effectiveMapName))
                                {
                                    availableMapNames.Add(effectiveMapName);
                                }
                            }
                        }
                    }
                }
                
                // Helper function to resolve a stored value to the actual MapName
                // This matches the runtime behavior: MapName if set, otherwise asset name
                System.Func<string, string> resolveMapName = (string storedValue) =>
                {
                    if (string.IsNullOrWhiteSpace(storedValue))
                        return storedValue;
                    
                    // First, check if it matches any MapName property value directly
                    foreach (var map in configAsset.InputActionMaps)
                    {
                        if (map != null && !string.IsNullOrWhiteSpace(map.MapName) && map.MapName.Equals(storedValue, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return map.MapName; // Return exact MapName property value
                        }
                    }
                    
                    // If it's already a valid effective MapName, return it
                    if (mapNameToMapName.ContainsKey(storedValue))
                        return storedValue;
                    
                    // If it's an asset name, resolve to the effective MapName (MapName property or asset name)
                    if (assetNameToMapName.ContainsKey(storedValue))
                        return assetNameToMapName[storedValue];
                    
                    // Check if any asset's effective name matches (case-insensitive)
                    foreach (var map in configAsset.InputActionMaps)
                    {
                        if (map != null)
                        {
                            string effectiveName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                            if (effectiveName.Equals(storedValue, System.StringComparison.OrdinalIgnoreCase))
                            {
                                return effectiveName; // Return the effective name
                            }
                        }
                    }
                    
                    // Otherwise return as-is (custom name)
                    return storedValue;
                };

                if (availableMapNames.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Available action maps: {string.Join(", ", availableMapNames)}", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("No action maps found in config. Add action maps to your Signalia Config first.", MessageType.Warning);
                }

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Disable Action Maps", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Action map names to disable when dialogue starts. These maps will be disabled when dialogue begins and re-enabled when dialogue ends.",
                    MessageType.Info);

                if (config.DialogueSystem.DisableActionMapNames == null)
                {
                    config.DialogueSystem.DisableActionMapNames = new[] { "Default" };
                    EditorUtility.SetDirty(config);
                }

                int disableCount = config.DialogueSystem.DisableActionMapNames.Length;
                EditorGUILayout.LabelField($"Configured Disable Maps: {disableCount}", EditorStyles.miniBoldLabel);

                for (int i = 0; i < config.DialogueSystem.DisableActionMapNames.Length; i++)
                {
                    // Capture index in local variable to avoid closure issues
                    int index = i;
                    
                    GUILayout.BeginHorizontal();
                    
                    string storedValue = config.DialogueSystem.DisableActionMapNames[index];
                    string resolvedMapName = resolveMapName(storedValue);
                    
                    // If the stored value is an asset name, update it to the MapName
                    if (storedValue != resolvedMapName && assetNameToMapName.ContainsKey(storedValue))
                    {
                        config.DialogueSystem.DisableActionMapNames[index] = resolvedMapName;
                        storedValue = resolvedMapName;
                        resolvedMapName = storedValue;
                        EditorUtility.SetDirty(config);
                    }
                    
                    // Text field - always editable for manual entry
                    string newValue = EditorGUILayout.TextField(
                        new GUIContent($"Map {index + 1}", "Action map name to disable. Use dropdown button to select from available maps."),
                        resolvedMapName);
                    
                    // If user typed an asset name, resolve it to MapName
                    if (newValue != resolvedMapName)
                    {
                        string resolvedNewValue = resolveMapName(newValue);
                        config.DialogueSystem.DisableActionMapNames[index] = resolvedNewValue;
                    }
                    else
                    {
                        config.DialogueSystem.DisableActionMapNames[index] = resolvedMapName;
                    }
                    
                    // Dropdown button to select from available maps
                    Rect dropdownButtonRect = GUILayoutUtility.GetRect(25, 18, GUILayout.Width(25));
                    if (GUI.Button(dropdownButtonRect, "▼", EditorStyles.miniButton))
                    {
                        GenericMenu menu = new GenericMenu();
                        
                        // Add "(None)" option
                        menu.AddItem(new GUIContent("(None)"), string.IsNullOrWhiteSpace(resolvedMapName), () =>
                        {
                            if (index >= 0 && index < config.DialogueSystem.DisableActionMapNames.Length)
                            {
                                config.DialogueSystem.DisableActionMapNames[index] = "";
                                EditorUtility.SetDirty(config);
                            }
                        });
                        
                        // Add separator if we have available maps
                        if (availableMapNames.Count > 0)
                        {
                            menu.AddSeparator("");
                        }
                        
                        // Add all available maps - show MapName property value when available
                        foreach (string effectiveMapName in availableMapNames)
                        {
                            bool isSelected = effectiveMapName == resolvedMapName;
                            string capturedEffectiveName = effectiveMapName; // Capture for closure
                            
                            // Find the asset and get its MapName property value for display
                            string displayName = capturedEffectiveName;
                            foreach (var map in configAsset.InputActionMaps)
                            {
                                if (map != null)
                                {
                                    string assetEffectiveName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                                    if (assetEffectiveName == capturedEffectiveName)
                                    {
                                        // Prefer showing MapName property value if it's set
                                        if (!string.IsNullOrWhiteSpace(map.MapName))
                                        {
                                            displayName = map.MapName; // Show the actual MapName property value
                                        }
                                        // Otherwise displayName stays as effectiveName (asset name fallback)
                                        break;
                                    }
                                }
                            }
                            
                            menu.AddItem(new GUIContent(displayName), isSelected, () =>
                            {
                                if (index >= 0 && index < config.DialogueSystem.DisableActionMapNames.Length)
                                {
                                    // Store the effective name (what runtime will use: MapName if set, otherwise asset name)
                                    config.DialogueSystem.DisableActionMapNames[index] = capturedEffectiveName;
                                    EditorUtility.SetDirty(config);
                                }
                            });
                        }
                        
                        // Show menu below the button
                        menu.DropDown(dropdownButtonRect);
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        var newList = new List<string>(config.DialogueSystem.DisableActionMapNames);
                        if (index >= 0 && index < newList.Count)
                        {
                            newList.RemoveAt(index);
                            config.DialogueSystem.DisableActionMapNames = newList.ToArray();
                            EditorUtility.SetDirty(config);
                        }
                        GUILayout.EndHorizontal();
                        break;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Map", GUILayout.Height(22)))
                {
                    if (availableMapNames.Count > 0)
                    {
                        // Add first available map by default
                        var newList = new List<string>(config.DialogueSystem.DisableActionMapNames ?? Array.Empty<string>());
                        newList.Add(availableMapNames[0]);
                        config.DialogueSystem.DisableActionMapNames = newList.ToArray();
                    }
                    else
                    {
                        var newList = new List<string>(config.DialogueSystem.DisableActionMapNames ?? Array.Empty<string>());
                        newList.Add("");
                        config.DialogueSystem.DisableActionMapNames = newList.ToArray();
                    }
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Reset Defaults", GUILayout.Height(22)))
                {
                    config.DialogueSystem.DisableActionMapNames = new[] { "Default" };
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Clear", GUILayout.Height(22)))
                {
                    config.DialogueSystem.DisableActionMapNames = Array.Empty<string>();
                    EditorUtility.SetDirty(config);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Enable Action Maps", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Action map names to enable when dialogue starts. These maps will be enabled when dialogue begins and disabled when dialogue ends.",
                    MessageType.Info);

                if (config.DialogueSystem.EnableActionMapNames == null)
                {
                    config.DialogueSystem.EnableActionMapNames = new[] { "GUI" };
                    EditorUtility.SetDirty(config);
                }

                int enableCount = config.DialogueSystem.EnableActionMapNames.Length;
                EditorGUILayout.LabelField($"Configured Enable Maps: {enableCount}", EditorStyles.miniBoldLabel);

                for (int i = 0; i < config.DialogueSystem.EnableActionMapNames.Length; i++)
                {
                    // Capture index in local variable to avoid closure issues
                    int index = i;
                    
                    GUILayout.BeginHorizontal();
                    
                    string storedValue = config.DialogueSystem.EnableActionMapNames[index];
                    string resolvedMapName = resolveMapName(storedValue);
                    
                    // If the stored value is an asset name, update it to the MapName
                    if (storedValue != resolvedMapName && assetNameToMapName.ContainsKey(storedValue))
                    {
                        config.DialogueSystem.EnableActionMapNames[index] = resolvedMapName;
                        storedValue = resolvedMapName;
                        resolvedMapName = storedValue;
                        EditorUtility.SetDirty(config);
                    }
                    
                    // Text field - always editable for manual entry
                    string newValue = EditorGUILayout.TextField(
                        new GUIContent($"Map {index + 1}", "Action map name to enable. Use dropdown button to select from available maps."),
                        resolvedMapName);
                    
                    // If user typed an asset name, resolve it to MapName
                    if (newValue != resolvedMapName)
                    {
                        string resolvedNewValue = resolveMapName(newValue);
                        config.DialogueSystem.EnableActionMapNames[index] = resolvedNewValue;
                    }
                    else
                    {
                        config.DialogueSystem.EnableActionMapNames[index] = resolvedMapName;
                    }
                    
                    // Dropdown button to select from available maps
                    Rect dropdownButtonRect = GUILayoutUtility.GetRect(25, 18, GUILayout.Width(25));
                    if (GUI.Button(dropdownButtonRect, "▼", EditorStyles.miniButton))
                    {
                        GenericMenu menu = new GenericMenu();
                        
                        // Add "(None)" option
                        menu.AddItem(new GUIContent("(None)"), string.IsNullOrWhiteSpace(resolvedMapName), () =>
                        {
                            if (index >= 0 && index < config.DialogueSystem.EnableActionMapNames.Length)
                            {
                                config.DialogueSystem.EnableActionMapNames[index] = "";
                                EditorUtility.SetDirty(config);
                            }
                        });
                        
                        // Add separator if we have available maps
                        if (availableMapNames.Count > 0)
                        {
                            menu.AddSeparator("");
                        }
                        
                        // Add all available maps - show MapName property value when available
                        foreach (string effectiveMapName in availableMapNames)
                        {
                            bool isSelected = effectiveMapName == resolvedMapName;
                            string capturedEffectiveName = effectiveMapName; // Capture for closure
                            
                            // Find the asset and get its MapName property value for display
                            string displayName = capturedEffectiveName;
                            foreach (var map in configAsset.InputActionMaps)
                            {
                                if (map != null)
                                {
                                    string assetEffectiveName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                                    if (assetEffectiveName == capturedEffectiveName)
                                    {
                                        // Prefer showing MapName property value if it's set
                                        if (!string.IsNullOrWhiteSpace(map.MapName))
                                        {
                                            displayName = map.MapName; // Show the actual MapName property value
                                        }
                                        // Otherwise displayName stays as effectiveName (asset name fallback)
                                        break;
                                    }
                                }
                            }
                            
                            menu.AddItem(new GUIContent(displayName), isSelected, () =>
                            {
                                if (index >= 0 && index < config.DialogueSystem.EnableActionMapNames.Length)
                                {
                                    // Store the effective name (what runtime will use: MapName if set, otherwise asset name)
                                    config.DialogueSystem.EnableActionMapNames[index] = capturedEffectiveName;
                                    EditorUtility.SetDirty(config);
                                }
                            });
                        }
                        
                        // Show menu below the button
                        menu.DropDown(dropdownButtonRect);
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        var newList = new List<string>(config.DialogueSystem.EnableActionMapNames);
                        if (index >= 0 && index < newList.Count)
                        {
                            newList.RemoveAt(index);
                            config.DialogueSystem.EnableActionMapNames = newList.ToArray();
                            EditorUtility.SetDirty(config);
                        }
                        GUILayout.EndHorizontal();
                        break;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Map", GUILayout.Height(22)))
                {
                    if (availableMapNames.Count > 0)
                    {
                        // Add first available map by default
                        var newList = new List<string>(config.DialogueSystem.EnableActionMapNames ?? Array.Empty<string>());
                        newList.Add(availableMapNames[0]);
                        config.DialogueSystem.EnableActionMapNames = newList.ToArray();
                    }
                    else
                    {
                        var newList = new List<string>(config.DialogueSystem.EnableActionMapNames ?? Array.Empty<string>());
                        newList.Add("");
                        config.DialogueSystem.EnableActionMapNames = newList.ToArray();
                    }
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Reset Defaults", GUILayout.Height(22)))
                {
                    config.DialogueSystem.EnableActionMapNames = new[] { "GUI" };
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Clear", GUILayout.Height(22)))
                {
                    config.DialogueSystem.EnableActionMapNames = Array.Empty<string>();
                    EditorUtility.SetDirty(config);
                }
                GUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUIUtility.labelWidth = 0;
        }
    }
}
