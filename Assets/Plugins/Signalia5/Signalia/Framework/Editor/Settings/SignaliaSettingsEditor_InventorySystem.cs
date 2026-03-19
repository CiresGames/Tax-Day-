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
        private void DrawInventoryTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.InventoryHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Configure auto-save behavior for inventory modifications. When enabled, specific inventories will be automatically saved whenever items are added, removed, or modified.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Enable Auto-Save
            config.InventorySystem.EnableAutoSave = EditorGUILayout.Toggle(
                new GUIContent("Enable Auto-Save", "When enabled, configured inventories will be automatically saved on modification"),
                config.InventorySystem.EnableAutoSave
            );

            if (config.InventorySystem.EnableAutoSave)
            {
                GUILayout.Space(5);

                // Auto-Save Any Inventory
                config.InventorySystem.AutoSaveAnyInventory = EditorGUILayout.Toggle(
                    new GUIContent("Auto-Save Any Inventory", "When enabled, ALL inventories will be auto-saved regardless of ID. This overrides the Auto-Save Inventory IDs list."),
                    config.InventorySystem.AutoSaveAnyInventory
                );

                if (!config.InventorySystem.AutoSaveAnyInventory)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Auto-Save Inventory IDs:", EditorStyles.boldLabel);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    if (config.InventorySystem.AutoSaveInventoryIds == null || config.InventorySystem.AutoSaveInventoryIds.Length == 0)
                    {
                        EditorGUILayout.HelpBox("No inventory IDs configured. Add specific inventory IDs below to enable auto-save for them.", MessageType.Warning);
                    }
                    else
                    {
                        for (int i = 0; i < config.InventorySystem.AutoSaveInventoryIds.Length; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            config.InventorySystem.AutoSaveInventoryIds[i] = EditorGUILayout.TextField(config.InventorySystem.AutoSaveInventoryIds[i]);
                            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                            {
                                var list = config.InventorySystem.AutoSaveInventoryIds.ToList();
                                list.RemoveAt(i);
                                config.InventorySystem.AutoSaveInventoryIds = list.ToArray();
                                EditorGUILayout.EndHorizontal();
                                break;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    if (GUILayout.Button("Add Inventory ID"))
                    {
                        var list = config.InventorySystem.AutoSaveInventoryIds?.ToList() ?? new List<string>();
                        list.Add("");
                        config.InventorySystem.AutoSaveInventoryIds = list.ToArray();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("Auto-save is enabled for ALL inventories. The specific inventory ID list is ignored.", MessageType.Info);
                }

                GUILayout.Space(10);

                // Log Auto-Save
                config.InventorySystem.LogAutoSave = EditorGUILayout.Toggle(
                    new GUIContent("Log Auto-Save", "Enable console logging when inventories are auto-saved"),
                    config.InventorySystem.LogAutoSave
                );
            }
            else
            {
                EditorGUILayout.HelpBox("Auto-save is disabled. Inventory changes will not be automatically saved.", MessageType.Warning);
            }

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Item References Section
            EditorGUILayout.LabelField("Item References", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure ItemSO references for inventory save/load system. These ItemSOs are cached at runtime for efficient lookups. Use 'Scan & Cache' to automatically find all ItemSO assets.",
                MessageType.Info
            );

            GUILayout.Space(5);

            // Auto-scan button
            if (GUILayout.Button("Scan & Cache ItemSOs from Resources", GUILayout.Height(30)))
            {
                ScanAndCacheItemSOs();
            }

            GUILayout.Space(5);

            // Display Item References array
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            SerializedObject serializedConfig = new SerializedObject(config);
            SerializedProperty itemRefsProperty = serializedConfig.FindProperty("InventorySystem.ItemReferences");

            if (itemRefsProperty != null)
            {
                EditorGUILayout.PropertyField(itemRefsProperty, true);
                serializedConfig.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Unable to display Item References. This may be a serialization issue.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Display Settings
            EditorGUILayout.LabelField("Display Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure how item information is displayed in the ItemDisplayerPanel.",
                MessageType.Info
            );

            GUILayout.Space(5);

            config.InventorySystem.ShowTotalQuantityInDisplayer = EditorGUILayout.Toggle(
                new GUIContent("Show Total Quantity in Displayer", "When enabled, displays the total quantity across all stacks in the inventory instead of just the current stack quantity."),
                config.InventorySystem.ShowTotalQuantityInDisplayer,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Custom Property Display Settings
            EditorGUILayout.LabelField("Custom Property Display Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure the default fallback text shown when an item's custom property is not found in ItemDisplayerPanel or ItemSlot.",
                MessageType.Info
            );

            GUILayout.Space(5);

            config.InventorySystem.CustomPropertyFallbackText = EditorGUILayout.TextField(
                new GUIContent("Property Fallback Text", "Text displayed when a custom property is not found in an item"),
                config.InventorySystem.CustomPropertyFallbackText,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(20);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Failure Events Section
            EditorGUILayout.LabelField("Failure Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure events that are sent when inventory operations fail (add, remove, or move). These events can be used to provide user feedback or handle errors gracefully.\n\n" +
                "Event Parameters:\n" +
                "• OnItemAddFailed: (ItemSO item, InventoryDefinition inventory, int quantity, string reason)\n" +
                "• OnItemRemoveFailed: (ItemSO item, InventoryDefinition inventory, int quantity, string reason)\n" +
                "• OnItemMoveFailed: (ItemSO item, InventoryDefinition source, InventoryDefinition target, int quantity, string reason)",
                MessageType.Info
            );

            GUILayout.Space(5);

            config.InventorySystem.OnItemAddFailed = EditorGUILayout.TextField(
                new GUIContent("On Item Add Failed Event", "Event sent when items fail to be added. Parameters: ItemSO, InventoryDefinition, int, string"),
                config.InventorySystem.OnItemAddFailed,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            config.InventorySystem.OnItemRemoveFailed = EditorGUILayout.TextField(
                new GUIContent("On Item Remove Failed Event", "Event sent when items fail to be removed. Parameters: ItemSO, InventoryDefinition, int, string"),
                config.InventorySystem.OnItemRemoveFailed,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            config.InventorySystem.OnItemMoveFailed = EditorGUILayout.TextField(
                new GUIContent("On Item Move Failed Event", "Event sent when items fail to be moved. Parameters: ItemSO, InventoryDefinition source, InventoryDefinition target, int, string"),
                config.InventorySystem.OnItemMoveFailed,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorUtility.SetDirty(config);
        }

        /// <summary>
        /// Scans Resources folder for ItemSO assets and populates the ItemReferences array
        /// </summary>
        private void ScanAndCacheItemSOs()
        {
            try
            {
                List<ItemSO> foundItems = new List<ItemSO>();
                
                // Search for all ItemSO assets in Resources
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemSO", new[] { "Assets" });
                
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Scan Complete", 
                        "No ItemSO assets found in the project. Please create ItemSO assets first.", 
                        "OK");
                    return;
                }

                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    ItemSO asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                    
                    if (asset != null)
                    {
                        foundItems.Add(asset);
                    }
                }

                // Update config
                config.InventorySystem.ItemReferences = foundItems.ToArray();
                EditorUtility.SetDirty(config);
                
                EditorUtility.DisplayDialog("Scan Complete", 
                    $"Found and cached {foundItems.Count} ItemSO asset(s).\n\nThese items will now be available for saving/loading inventories.", 
                    "OK");
                
                Debug.Log($"[Signalia Inventory] Scanned and cached {foundItems.Count} ItemSO assets");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia Inventory] Failed to scan ItemSO assets: {e.Message}");
                EditorUtility.DisplayDialog("Scan Failed", 
                    $"Failed to scan ItemSO assets:\n\n{e.Message}", 
                    "OK");
            }
        }
    }
}
