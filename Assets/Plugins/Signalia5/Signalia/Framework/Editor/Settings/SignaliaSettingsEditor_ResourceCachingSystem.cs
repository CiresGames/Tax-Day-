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
        #region Game Systems - Resource Caching

        private void DrawResourceCachingTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.ResourceCachingHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Configure Resource Assets for efficient resource caching. " +
                                   "Resource Assets contain cached resources accessible by string keys for instant access.", MessageType.Info);

            GUILayout.Space(10);

            // Show current resource assets count
            int resourceAssetCount = config.ResourceAssets != null ? config.ResourceAssets.Length : 0;
            EditorGUILayout.LabelField($"Current Resource Assets: {resourceAssetCount}", EditorStyles.boldLabel);
            
            // Load Resource Assets button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 30 };
            if (GUILayout.Button("Load Resource Assets", buttonStyle))
            {
                bool success = ResourceHandler.LoadResourceAssets();
                
                if (success)
                {
                    EditorUtility.DisplayDialog("Resource Assets Loaded", 
                        "Successfully loaded ResourceAsset files into the Signalia config. " +
                        "All cached resources are now available for instant access.", 
                        "OK");
                    
                    // Refresh the config to show updated values
                    config = ConfigReader.GetConfig(true);
                }
                else
                {
                    EditorUtility.DisplayDialog("Resource Assets Load Failed", 
                        "Failed to load ResourceAsset files. Check the Console for details. " +
                        "Make sure you have ResourceAsset files in the Resources/Signalia folder.", 
                        "OK");
                }
            }

            GUILayout.Space(10);

            // Documentation button
            GUIStyle docButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, fixedHeight = 25 };
            if (GUILayout.Button("📖 Open Documentation", docButtonStyle))
            {
                OpenResourceCachingDocumentation();
            }

            GUILayout.Space(10);

            // Display ResourceAssets array - using direct config access like audio assets
            EditorGUIUtility.labelWidth = 150;
            
            if (config.ResourceAssets != null && config.ResourceAssets.Length > 0)
            {
                EditorGUILayout.LabelField("Resource Assets List:", EditorStyles.boldLabel);
                
                for (int i = 0; i < config.ResourceAssets.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    
                    // Show asset name or "Missing" if null
                    string assetName = config.ResourceAssets[i] != null ? config.ResourceAssets[i].name : "Missing Reference";
                    GUIStyle assetLabelStyle = config.ResourceAssets[i] != null ? EditorStyles.label : EditorStyles.miniLabel;
                    Color originalColor = GUI.color;
                    if (config.ResourceAssets[i] == null) GUI.color = Color.red;
                    
                    EditorGUILayout.LabelField($"{i + 1}. {assetName}", assetLabelStyle, GUILayout.Width(200));
                    GUI.color = originalColor;
                    
                    // Remove button
                    if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Resource Asset", 
                            $"Are you sure you want to remove '{assetName}' from the resource assets list?", 
                            "Yes", "Cancel"))
                        {
                            // Create new array without this element
                            var newResourceAssets = new ResourceAsset[config.ResourceAssets.Length - 1];
                            for (int j = 0, k = 0; j < config.ResourceAssets.Length; j++)
                            {
                                if (j != i)
                                {
                                    newResourceAssets[k++] = config.ResourceAssets[j];
                                }
                            }
                            config.ResourceAssets = newResourceAssets;
                            EditorUtility.SetDirty(config);
                            Debug.Log($"[Signalia] Removed ResourceAsset '{assetName}' from config.");
                        }
                    }
                    
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(5);
                
                // Clear all button
                if (GUILayout.Button("Clear All Resource Assets", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Resource Assets", 
                        "Are you sure you want to remove all resource assets from the config?", 
                        "Yes", "Cancel"))
                    {
                        config.ResourceAssets = new ResourceAsset[0];
                        EditorUtility.SetDirty(config);
                        Debug.Log("[Signalia] Cleared all ResourceAssets from config.");
                    }
                }
                
                EditorGUILayout.HelpBox($"Resource Assets configured. Use SIGS.GetResource<T>(key) to access cached resources.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No resource assets assigned. Use 'Load Resource Assets' button above to populate them.", MessageType.Info);
            }

            EditorGUIUtility.labelWidth = 0;
        }


        #endregion

    }
}
