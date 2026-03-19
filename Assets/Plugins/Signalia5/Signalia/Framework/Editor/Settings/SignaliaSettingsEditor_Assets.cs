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
        #region Menu Items

        [MenuItem("Tools/Signalia/Generate Stock Assets")]
        public static void GenerateStockAssetsMenu()
        {
            GenerateStockAssets();
        }

        #endregion

        #region Tabs - Assets

        private void DrawAssetsTab()
        {
            EditorGUILayout.HelpBox("Configure audio assets and mixer settings for the project.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.AudioMixerAsset = (AudioMixerAsset)EditorGUILayout.ObjectField(new GUIContent("Audio Mixer Asset", "Default audio mixer asset to be used."), config.AudioMixerAsset, typeof(AudioMixerAsset), false);

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Audio Assets: Direct references to AudioAsset files. Loading these prevents the Resources.LoadAll freeze issue that occurs when the system searches for audio files at runtime. Without direct references, the game will freeze momentarily when audio is first played.", MessageType.Info);
            
            // Show current audio assets count
            int audioAssetCount = config.AudioAssets != null ? config.AudioAssets.Length : 0;
            EditorGUILayout.LabelField($"Current Audio Assets: {audioAssetCount}", EditorStyles.boldLabel);
            
            // Load Audio Assets button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 30 };
            if (GUILayout.Button("Load Audio Assets", buttonStyle))
            {
                bool success = ResourceHandler.LoadAudioAssets();
                
                if (success)
                {
                    EditorUtility.DisplayDialog("Audio Assets Loaded", 
                        "Successfully loaded AudioAsset files into the Signalia config. " +
                        "This should prevent the Resources.LoadAll freeze issue during gameplay.", 
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Audio Assets Load Failed", 
                        "Failed to load AudioAsset files. Check the Console for details. " +
                        "Make sure you have AudioAsset files in the Resources/Signalia folder.", 
                        "OK");
                }
            }

            GUILayout.Space(10);

            // Audio Assets Foldout Group
            if (config.AudioAssets != null && config.AudioAssets.Length > 0)
            {
                EditorGUILayout.LabelField("Audio Assets List:", EditorStyles.boldLabel);
                
                for (int i = 0; i < config.AudioAssets.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    
                    // Show asset name or "Missing" if null
                    string assetName = config.AudioAssets[i] != null ? config.AudioAssets[i].name : "Missing Reference";
                    GUIStyle assetLabelStyle = config.AudioAssets[i] != null ? EditorStyles.label : EditorStyles.miniLabel;
                    Color originalColor = GUI.color;
                    if (config.AudioAssets[i] == null) GUI.color = Color.red;
                    
                    EditorGUILayout.LabelField($"{i + 1}. {assetName}", assetLabelStyle, GUILayout.Width(200));
                    GUI.color = originalColor;
                    
                    // Remove button
                    if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Audio Asset", 
                            $"Are you sure you want to remove '{assetName}' from the audio assets list?", 
                            "Yes", "Cancel"))
                        {
                            // Create new array without this element
                            var newAudioAssets = new AudioAsset[config.AudioAssets.Length - 1];
                            for (int j = 0, k = 0; j < config.AudioAssets.Length; j++)
                            {
                                if (j != i)
                                {
                                    newAudioAssets[k++] = config.AudioAssets[j];
                                }
                            }
                            config.AudioAssets = newAudioAssets;
                            EditorUtility.SetDirty(config);
                            Debug.Log($"[Signalia] Removed AudioAsset '{assetName}' from config.");
                        }
                    }
                    
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(5);
                
                // Clear all button
                if (GUILayout.Button("Clear All Audio Assets", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Audio Assets", 
                        "Are you sure you want to remove all audio assets from the config? ", 
                        "Yes", "Cancel"))
                    {
                        config.AudioAssets = new AudioAsset[0];
                        EditorUtility.SetDirty(config);
                        Debug.Log("[Signalia] Cleared all AudioAssets from config.");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No audio assets assigned. Use 'Load Audio Assets' button above to populate them.", MessageType.Info);
            }

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Input Action Maps Section
            EditorGUILayout.HelpBox("Input Action Maps: Direct references to SignaliaActionMap files. These define action names and types used by Signalia's input bridge.", MessageType.Info);
            
            // Show current input action maps count
            int inputActionMapCount = config.InputActionMaps != null ? config.InputActionMaps.Length : 0;
            EditorGUILayout.LabelField($"Current Input Action Maps: {inputActionMapCount}", EditorStyles.boldLabel);
            
            // Load Input Action Maps button
            if (GUILayout.Button("Load Input Action Maps", buttonStyle))
            {
                bool success = ResourceHandler.LoadInputActionMaps();
                
                if (success)
                {
                    EditorUtility.DisplayDialog("Input Action Maps Loaded", 
                        "Successfully loaded SignaliaActionMap files into the Signalia config.", 
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Input Action Maps Load Failed", 
                        "Failed to load SignaliaActionMap files. Check the Console for details. " +
                        "Make sure you have SignaliaActionMap files in your project.", 
                        "OK");
                }
            }

            GUILayout.Space(10);

            // Input Action Maps List
            if (config.InputActionMaps != null && config.InputActionMaps.Length > 0)
            {
                EditorGUILayout.LabelField("Input Action Maps List:", EditorStyles.boldLabel);
                
                for (int i = 0; i < config.InputActionMaps.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    
                    // Show asset name or "Missing" if null
                    string assetName = config.InputActionMaps[i] != null ? config.InputActionMaps[i].name : "Missing Reference";
                    GUIStyle assetLabelStyle = config.InputActionMaps[i] != null ? EditorStyles.label : EditorStyles.miniLabel;
                    Color originalColor = GUI.color;
                    if (config.InputActionMaps[i] == null) GUI.color = Color.red;
                    
                    EditorGUILayout.LabelField($"{i + 1}. {assetName}", assetLabelStyle, GUILayout.Width(200));
                    GUI.color = originalColor;
                    
                    // Show action count if available
                    if (config.InputActionMaps[i] != null && config.InputActionMaps[i].Actions != null)
                    {
                        EditorGUILayout.LabelField($"({config.InputActionMaps[i].Actions.Count} actions)", EditorStyles.miniLabel, GUILayout.Width(100));
                    }
                    
                    // Remove button
                    if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Input Action Map", 
                            $"Are you sure you want to remove '{assetName}' from the input action maps list?", 
                            "Yes", "Cancel"))
                        {
                            // Create new array without this element
                            var newInputActionMaps = new SignaliaActionMap[config.InputActionMaps.Length - 1];
                            for (int j = 0, k = 0; j < config.InputActionMaps.Length; j++)
                            {
                                if (j != i)
                                {
                                    newInputActionMaps[k++] = config.InputActionMaps[j];
                                }
                            }
                            config.InputActionMaps = newInputActionMaps;
                            EditorUtility.SetDirty(config);
                            Debug.Log($"[Signalia] Removed SignaliaActionMap '{assetName}' from config.");
                        }
                    }
                    
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(5);
                
                // Clear all button
                if (GUILayout.Button("Clear All Input Action Maps", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Input Action Maps", 
                        "Are you sure you want to remove all input action maps from the config?", 
                        "Yes", "Cancel"))
                    {
                        config.InputActionMaps = new SignaliaActionMap[0];
                        EditorUtility.SetDirty(config);
                        Debug.Log("[Signalia] Cleared all SignaliaActionMaps from config.");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No input action maps assigned. Use 'Load Input Action Maps' button above to populate them.", MessageType.Info);
            }

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Stock Assets Generation Section
            EditorGUILayout.HelpBox("Stock Assets: Generate sample assets for quick debugging and testing. Assets will be placed in Resources/Signalia/GeneratedStock/", MessageType.Info);
            
            if (GUILayout.Button("Generate Stock Assets", buttonStyle))
            {
                FrameworkSettings.GenerateStockAssets();
            }

            EditorGUIUtility.labelWidth = 0;
        }

        private static void GenerateStockAssets()
        {
            string basePath = "Assets/Resources/Signalia/GeneratedStock";
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                string resourcesPath = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                string signaliaPath = "Assets/Resources/Signalia";
                if (!AssetDatabase.IsValidFolder(signaliaPath))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Signalia");
                }
                
                AssetDatabase.CreateFolder("Assets/Resources/Signalia", "GeneratedStock");
            }

            List<string> generatedAssets = new List<string>();

            // Generate Inventory System assets
            generatedAssets.AddRange(GenerateInventoryStockAssets(basePath));

            // Generate Achievement System assets
            generatedAssets.AddRange(GenerateAchievementStockAssets(basePath));

            // Generate Input Action Maps
            generatedAssets.AddRange(GenerateInputActionMapStockAssets(basePath));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (generatedAssets.Count > 0)
            {
                string message = $"Successfully generated {generatedAssets.Count} stock asset(s):\n\n" + string.Join("\n", generatedAssets);
                EditorUtility.DisplayDialog("Stock Assets Generated", message, "OK");
                Debug.Log($"[Signalia] Generated {generatedAssets.Count} stock assets in {basePath}");
            }
            else
            {
                EditorUtility.DisplayDialog("Stock Assets Generation", 
                    "No assets were generated. Make sure the required game systems (Inventory, Achievement) are installed.", 
                    "OK");
            }
        }

        private static List<string> GenerateInventoryStockAssets(string basePath)
        {
            List<string> generated = new List<string>();
            string inventoryPath = Path.Combine(basePath, "Inventory");
            
            if (!AssetDatabase.IsValidFolder(inventoryPath))
            {
                AssetDatabase.CreateFolder(basePath, "Inventory");
            }

            // Create sample items
            var items = new[]
            {
                new { Name = "Health Potion", Description = "A magical potion that restores health.", Category = "Consumables", MaxStack = 10, IsUsable = true },
                new { Name = "Iron Sword", Description = "A sturdy iron sword for combat.", Category = "Weapons", MaxStack = 1, IsUsable = false },
                new { Name = "Gold Coin", Description = "Currency used for trading.", Category = "Currency", MaxStack = 999, IsUsable = false },
                new { Name = "Wooden Shield", Description = "A basic shield for defense.", Category = "Armor", MaxStack = 1, IsUsable = false },
                new { Name = "Mana Potion", Description = "Restores magical energy.", Category = "Consumables", MaxStack = 10, IsUsable = true }
            };

            foreach (var itemData in items)
            {
                ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
                item.SetItemData(
                    itemData.Name,
                    itemData.Description,
                    itemData.Category,
                    itemData.MaxStack,
                    itemData.MaxStack * 10,
                    itemData.IsUsable,
                    true
                );

                string assetPath = Path.Combine(inventoryPath, $"{itemData.Name.Replace(" ", "")}.asset");
                AssetDatabase.CreateAsset(item, assetPath);
                generated.Add(assetPath);
            }

            return generated;
        }

        private static List<string> GenerateAchievementStockAssets(string basePath)
        {
            List<string> generated = new List<string>();
            string achievementPath = Path.Combine(basePath, "Achievements");
            
            if (!AssetDatabase.IsValidFolder(achievementPath))
            {
                AssetDatabase.CreateFolder(basePath, "Achievements");
            }

            // Create sample achievements
            var achievements = new[]
            {
                new { Id = "first_steps", Title = "First Steps", Description = "Complete your first quest." },
                new { Id = "treasure_hunter", Title = "Treasure Hunter", Description = "Collect 100 gold coins." },
                new { Id = "warrior", Title = "Warrior", Description = "Defeat 10 enemies in combat." },
                new { Id = "explorer", Title = "Explorer", Description = "Visit 5 different locations." },
                new { Id = "master_craftsman", Title = "Master Craftsman", Description = "Craft 20 items." }
            };

            foreach (var achData in achievements)
            {
                AchievementSO achievement = ScriptableObject.CreateInstance<AchievementSO>();
                achievement.SetAchievementData(achData.Id, achData.Title, achData.Description);

                string assetPath = Path.Combine(achievementPath, $"{achData.Title.Replace(" ", "")}.asset");
                AssetDatabase.CreateAsset(achievement, assetPath);
                generated.Add(assetPath);
            }

            return generated;
        }

        private static List<string> GenerateInputActionMapStockAssets(string basePath)
        {
            List<string> generated = new List<string>();
            string inputPath = Path.Combine(basePath, "InputActionMaps");
            
            if (!AssetDatabase.IsValidFolder(inputPath))
            {
                AssetDatabase.CreateFolder(basePath, "InputActionMaps");
            }

            // Define common action maps with their actions
            var actionMaps = new[]
            {
                new
                {
                    MapName = "UI",
                    Actions = new[]
                    {
                        new { Name = "Navigate", Type = SignaliaActionType.Vector2 },
                        new { Name = "Submit", Type = SignaliaActionType.Bool },
                        new { Name = "Cancel", Type = SignaliaActionType.Bool },
                        new { Name = "Menu", Type = SignaliaActionType.Bool },
                        new { Name = "Scroll", Type = SignaliaActionType.Vector2 },
                        new { Name = "Tab", Type = SignaliaActionType.Bool },
                        new { Name = "Back", Type = SignaliaActionType.Bool }
                    }
                },
                new
                {
                    MapName = "Gameplay",
                    Actions = new[]
                    {
                        new { Name = "Move", Type = SignaliaActionType.Vector2 },
                        new { Name = "Look", Type = SignaliaActionType.Vector2 },
                        new { Name = "Jump", Type = SignaliaActionType.Bool },
                        new { Name = "Sprint", Type = SignaliaActionType.Bool },
                        new { Name = "Crouch", Type = SignaliaActionType.Bool },
                        new { Name = "Interact", Type = SignaliaActionType.Bool },
                        new { Name = "Attack", Type = SignaliaActionType.Bool },
                        new { Name = "Block", Type = SignaliaActionType.Bool },
                        new { Name = "Dash", Type = SignaliaActionType.Bool },
                        new { Name = "Reload", Type = SignaliaActionType.Bool },
                        new { Name = "Use", Type = SignaliaActionType.Bool },
                        new { Name = "Drop", Type = SignaliaActionType.Bool }
                    }
                },
                new
                {
                    MapName = "Car",
                    Actions = new[]
                    {
                        new { Name = "Accelerate", Type = SignaliaActionType.Float },
                        new { Name = "Brake", Type = SignaliaActionType.Float },
                        new { Name = "Steer", Type = SignaliaActionType.Float },
                        new { Name = "Handbrake", Type = SignaliaActionType.Bool },
                        new { Name = "ShiftUp", Type = SignaliaActionType.Bool },
                        new { Name = "ShiftDown", Type = SignaliaActionType.Bool },
                        new { Name = "Horn", Type = SignaliaActionType.Bool },
                        new { Name = "LookBack", Type = SignaliaActionType.Bool }
                    }
                },
                new
                {
                    MapName = "Camera",
                    Actions = new[]
                    {
                        new { Name = "Look", Type = SignaliaActionType.Vector2 },
                        new { Name = "Zoom", Type = SignaliaActionType.Float },
                        new { Name = "Reset", Type = SignaliaActionType.Bool },
                        new { Name = "ToggleView", Type = SignaliaActionType.Bool }
                    }
                },
                new
                {
                    MapName = "Combat",
                    Actions = new[]
                    {
                        new { Name = "Attack", Type = SignaliaActionType.Bool },
                        new { Name = "HeavyAttack", Type = SignaliaActionType.Bool },
                        new { Name = "Block", Type = SignaliaActionType.Bool },
                        new { Name = "Dodge", Type = SignaliaActionType.Bool },
                        new { Name = "Parry", Type = SignaliaActionType.Bool },
                        new { Name = "LockOn", Type = SignaliaActionType.Bool },
                        new { Name = "SwitchTarget", Type = SignaliaActionType.Bool }
                    }
                },
                new
                {
                    MapName = "Inventory",
                    Actions = new[]
                    {
                        new { Name = "OpenInventory", Type = SignaliaActionType.Bool },
                        new { Name = "UseItem", Type = SignaliaActionType.Bool },
                        new { Name = "DropItem", Type = SignaliaActionType.Bool },
                        new { Name = "Equip", Type = SignaliaActionType.Bool },
                        new { Name = "Navigate", Type = SignaliaActionType.Vector2 },
                        new { Name = "Select", Type = SignaliaActionType.Bool }
                    }
                }
            };

            foreach (var mapData in actionMaps)
            {
                SignaliaActionMap actionMap = ScriptableObject.CreateInstance<SignaliaActionMap>();
                actionMap.MapName = mapData.MapName;
                actionMap.Actions = new List<SignaliaActionDefinition>();

                foreach (var actionData in mapData.Actions)
                {
                    actionMap.Actions.Add(new SignaliaActionDefinition
                    {
                        ActionName = actionData.Name,
                        ActionType = actionData.Type
                    });
                }

                string assetPath = Path.Combine(inputPath, $"{mapData.MapName}ActionMap.asset");
                AssetDatabase.CreateAsset(actionMap, assetPath);
                generated.Add(assetPath);
            }

            return generated;
        }

        #endregion

    }
}
