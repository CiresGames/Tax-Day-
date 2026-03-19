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
        #region Game Systems - Localization System

        private void DrawLocalizationTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.LocalizationHeader, maxWidthProperties);
            GUILayout.Space(10);

            GUI.backgroundColor = Color.gray;
            localizationTabIndex = GUILayout.Toolbar(localizationTabIndex, localizationTabs, GUILayout.Height(24), GUILayout.MaxWidth(maxWidthProperties));
            GUI.backgroundColor = Color.white;

            switch (localizationTabIndex)
            {
                case 0: DrawInternalLocalizationTab(); break;
                case 1: DrawExternalLocalizationTab(); break;
            }
        }

        private void DrawInternalLocalizationTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("The Internal Localization system provides a lightweight, performant way to localize your game without MonoBehaviour dependencies. " +
                                   "Use SIGS.GetLocalizedString(key) or the SetLocalizedText() extension method on TMP_Text components.",
                                   MessageType.Info);

            EditorGUILayout.Space(10);

            // Hybrid Key Mode
            EditorGUILayout.LabelField("Hybrid Key Mode", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            config.LocalizationSystem.HybridKey = EditorGUILayout.Toggle(
                new GUIContent("Enable Hybrid Key", "When enabled, the system will search for strings by key, value, and aliases. Useful for projects with hardcoded strings."),
                config.LocalizationSystem.HybridKey,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            if (config.LocalizationSystem.HybridKey)
            {
                EditorGUILayout.HelpBox("⚠️ Hybrid Key mode is enabled. This allows searching by key, original value, variant values, and aliases, but may impact performance. " +
                                       "Only enable this if you have hardcoded strings that need localization.",
                                       MessageType.Warning);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // LocBook Configuration
            EditorGUILayout.LabelField("LocBook Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Display array of LocBooks
            SerializedObject serializedConfig = new SerializedObject(config);
            SerializedProperty locBooksProperty = serializedConfig.FindProperty("LocalizationSystem.LocBooks");

            if (locBooksProperty != null)
            {
                EditorGUILayout.PropertyField(locBooksProperty, new GUIContent("LocBooks", "Array of LocBook assets containing localization data"), true);
                serializedConfig.ApplyModifiedProperties();
            }

            EditorGUIUtility.labelWidth = 0;

            if (config.LocalizationSystem.LocBooks == null || config.LocalizationSystem.LocBooks.Length == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No LocBook assigned. Please create and assign a LocBook asset to enable localization.", MessageType.Warning);

                if (GUILayout.Button("Create New LocBook", GUILayout.Height(25), GUILayout.MaxWidth(maxWidthProperties)))
                {
                    CreateNewLocBook();
                }
            }
            else
            {
                int totalEntries = 0;
                foreach (var lb in config.LocalizationSystem.LocBooks)
                {
                    if (lb != null)
                        totalEntries += lb.EntryCount;
                }
                EditorGUILayout.HelpBox($"✓ {config.LocalizationSystem.LocBooks.Length} LocBook(s) assigned with {totalEntries} total entries.", MessageType.Info);

                if (GUILayout.Button("Auto-Load All LocBooks", GUILayout.Height(25), GUILayout.MaxWidth(maxWidthProperties)))
                {
                    LoadAllLocBooksFromProject(config);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Text Style Cache
            EditorGUILayout.LabelField("Text Style Cache", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox("Text Styles define font and formatting settings for different languages. " +
                                   "Create TextStyle assets and add them here to apply language-specific formatting automatically.",
                                   MessageType.Info);

            // Display current text styles
            if (config.LocalizationSystem.TextStyleCache == null || config.LocalizationSystem.TextStyleCache.Length == 0)
            {
                EditorGUILayout.HelpBox("No TextStyle assets cached. Create TextStyle assets for each language you want to support.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Cached Text Styles: {config.LocalizationSystem.TextStyleCache.Length}", EditorStyles.miniLabel);

                foreach (var style in config.LocalizationSystem.TextStyleCache)
                {
                    if (style != null)
                    {
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField($"[{style.LanguageCode}] {style.name}", GUILayout.Width(200));

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = style;
                            EditorGUIUtility.PingObject(style);
                        }

                        if (GUILayout.Button("×", GUILayout.Width(20)))
                        {
                            RemoveTextStyleFromCache(style);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            if (GUILayout.Button("Create New Text Style", GUILayout.Height(25), GUILayout.MaxWidth(maxWidthProperties)))
            {
                CreateNewTextStyle();
            }

            if (GUILayout.Button("Auto-Load Text Styles", GUILayout.Height(25), GUILayout.MaxWidth(maxWidthProperties)))
            {
                LoadTextStylesFromProject();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Default Settings
            EditorGUILayout.LabelField("Default Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.LocalizationSystem.DefaultStartingLanguageCode = EditorGUILayout.TextField(
                new GUIContent("Default Language Code", "The default starting language (e.g., 'en', 'es', 'fr'). Used when no saved preference exists."),
                config.LocalizationSystem.DefaultStartingLanguageCode,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Save Settings
            EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.LocalizationSystem.LanguageOptionSaveKey = EditorGUILayout.TextField(
                new GUIContent("Language Save Key", "The key used to save/load the user's language preference."),
                config.LocalizationSystem.LanguageOptionSaveKey,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Events
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.LocalizationSystem.LanguageChangedEvent = EditorGUILayout.TextField(
                new GUIContent("Language Changed Event", "Radio event sent when the language is changed. UI elements can listen to this to update their display."),
                config.LocalizationSystem.LanguageChangedEvent,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Internal Settings
            EditorGUILayout.LabelField("Internal", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            config.LocalizationSystem.AutoUpdateLocbooks = EditorGUILayout.Toggle(
                new GUIContent("Auto Update Locbooks", "When enabled, automatically updates LocBook assets when their referenced .locbook files are imported or modified."),
                config.LocalizationSystem.AutoUpdateLocbooks,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            if (config.LocalizationSystem.AutoUpdateLocbooks)
            {
                EditorGUILayout.HelpBox("✓ Auto Update is enabled. LocBook assets will automatically update when their .locbook files change.",
                                       MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Auto Update is disabled. You'll need to manually click 'Update Asset' in each LocBook inspector to sync changes.",
                                       MessageType.None);
            }

            EditorGUILayout.Space(5);

            config.LocalizationSystem.AutoRefreshCacheInRuntime = EditorGUILayout.Toggle(
                new GUIContent("Auto Refresh Cache in Runtime", "When enabled, automatically refreshes the localization cache when LocBook assets are updated while the game is playing."),
                config.LocalizationSystem.AutoRefreshCacheInRuntime,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            if (config.LocalizationSystem.AutoRefreshCacheInRuntime)
            {
                EditorGUILayout.HelpBox("⚠️ Auto Refresh in Runtime is enabled. The localization cache will reload when assets are updated during play mode.\n\n" +
                                       "WARNING: This will impact editor performance while working. Only enable if you need to test localization changes in real-time.",
                                       MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Auto Refresh in Runtime is disabled. You'll need to restart play mode to see localization changes.",
                                       MessageType.None);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📖 Documentation", GUILayout.Height(30)))
            {
                EditorUtility.DisplayDialog("Localization Documentation",
                    "Localization Internal System\n\n" +
                    "Getting Started:\n" +
                    "1. Create a LocBook asset (Create > Signalia > Localization > LocBook)\n" +
                    "2. Add localization entries manually or import from JSON\n" +
                    "3. Assign the LocBook in these settings\n" +
                    "4. Call SIGS.InitializeLocalization() at game start\n" +
                    "5. Use SIGS.GetLocalizedString(key) or TMP_Text.SetLocalizedText(key)\n\n" +
                    "For more information, check the Signalia documentation.",
                    "OK");
            }

            if (GUILayout.Button("🔧 Initialize System (Runtime)", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    if (config.LocalizationSystem.LocBooks != null && config.LocalizationSystem.LocBooks.Length > 0)
                    {
                        SIGS.InitializeLocalization();
                        EditorUtility.DisplayDialog("Localization Initialized",
                            "The localization system has been initialized with the configured LocBooks.",
                            "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Cannot Initialize",
                            "Please assign LocBooks in the settings first.",
                            "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Not in Play Mode",
                        "The localization system can only be initialized in Play Mode. " +
                        "Normally, you would call SIGS.InitializeLocalization() at game startup.",
                        "OK");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        
        private void CreateNewLocBook()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create LocBook",
                "NewLocBook",
                "asset",
                "Choose where to save the new LocBook asset");
            
            if (!string.IsNullOrEmpty(path))
            {
                var locBook = ScriptableObject.CreateInstance<AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook>();
                AssetDatabase.CreateAsset(locBook, path);
                AssetDatabase.SaveAssets();
                
                // Add to array
                var list = new System.Collections.Generic.List<AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook>(config.LocalizationSystem.LocBooks ?? new AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook[0]);
                list.Add(locBook);
                config.LocalizationSystem.LocBooks = list.ToArray();
                EditorUtility.SetDirty(config);
                
                Selection.activeObject = locBook;
                EditorGUIUtility.PingObject(locBook);
                
                EditorUtility.DisplayDialog("LocBook Created", 
                    $"LocBook created at {path} and added to config.", 
                    "OK");
            }
        }
        
        private void LoadAllLocBooksFromProject(SignaliaConfigAsset config)
        {
            string[] guids = AssetDatabase.FindAssets("t:LocBook");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No LocBooks Found", 
                    "No LocBook assets found in the project.", 
                    "OK");
                return;
            }
            
            var locBooks = new System.Collections.Generic.List<AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var locBook = AssetDatabase.LoadAssetAtPath<AHAKuo.Signalia.GameSystems.Localization.Internal.LocBook>(path);
                
                if (locBook != null)
                {
                    locBooks.Add(locBook);
                }
            }
            
            config.LocalizationSystem.LocBooks = locBooks.ToArray();
            EditorUtility.SetDirty(config);
            
            EditorUtility.DisplayDialog("LocBooks Loaded", 
                $"Found and assigned {locBooks.Count} LocBook asset(s).", 
                "OK");
        }
        
        private void CreateNewTextStyle()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Text Style",
                "NewTextStyle",
                "asset",
                "Choose where to save the new TextStyle asset");
            
            if (!string.IsNullOrEmpty(path))
            {
                var textStyle = ScriptableObject.CreateInstance<AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle>();
                AssetDatabase.CreateAsset(textStyle, path);
                AssetDatabase.SaveAssets();
                
                Selection.activeObject = textStyle;
                EditorGUIUtility.PingObject(textStyle);
                
                EditorUtility.DisplayDialog("TextStyle Created", 
                    $"TextStyle created at {path}. You can now configure it and add it to the config using the 'Add to Config' button in its inspector.", 
                    "OK");
            }
        }
        
        private void LoadTextStylesFromProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:TextStyle");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No TextStyles Found", 
                    "No TextStyle assets found in the project. Create some first.", 
                    "OK");
                return;
            }
            
            var styles = new System.Collections.Generic.List<AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle>();
            
            if (config.LocalizationSystem.TextStyleCache != null)
            {
                styles.AddRange(config.LocalizationSystem.TextStyleCache);
            }
            
            int addedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var style = AssetDatabase.LoadAssetAtPath<AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle>(path);
                
                if (style != null && !styles.Contains(style))
                {
                    styles.Add(style);
                    addedCount++;
                }
            }
            
            config.LocalizationSystem.TextStyleCache = styles.ToArray();
            EditorUtility.SetDirty(config);
            
            EditorUtility.DisplayDialog("Text Styles Loaded", 
                $"Found {guids.Length} TextStyle assets. Added {addedCount} new ones to the cache.", 
                "OK");
        }
        
        private void RemoveTextStyleFromCache(AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle style)
        {
            var list = new System.Collections.Generic.List<AHAKuo.Signalia.GameSystems.Localization.Internal.TextStyle>(config.LocalizationSystem.TextStyleCache);
            
            if (list.Remove(style))
            {
                config.LocalizationSystem.TextStyleCache = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawExternalLocalizationTab()
        {
            // Note: Import/Export functionality has been moved to per-LocBook workflow
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("The import/export workflow has been updated!\n\n" +
                "Each LocBook asset now manages its own .locbook file reference. " +
                "To work with localization data:\n\n" +
                "1. Select a LocBook asset in your project\n" +
                "2. Assign a .locbook file in the inspector\n" +
                "3. Use 'Open in Lingramia' to edit\n" +
                "4. Use 'Update Asset' to import changes back to Unity\n\n" +
                "This new workflow provides better organization and supports multiple LocBooks simultaneously.", 
                MessageType.Info);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Open LocBook Documentation", GUILayout.Height(30), GUILayout.MaxWidth(maxWidthProperties)))
            {
                // Open documentation or show help
                EditorUtility.DisplayDialog("LocBook Workflow", 
                    "To use the new localization workflow:\n\n" +
                    "1. Create or select a LocBook asset (Create > Signalia > Game Systems > Localization > LocBook)\n" +
                    "2. In the inspector, assign your .locbook file to the 'LocBook File' field\n" +
                    "3. Click '🚀 Open in Lingramia' to edit in the external app\n" +
                    "4. After saving in Lingramia, click '🔄 Update Asset' to import changes\n" +
                    "5. Use 'Load in Config' to add the LocBook to your active configuration\n\n" +
                    "Each LocBook is now self-contained with its own .locbook file reference!", 
                    "Got it!");
            }
        }


        #endregion

    }
}
