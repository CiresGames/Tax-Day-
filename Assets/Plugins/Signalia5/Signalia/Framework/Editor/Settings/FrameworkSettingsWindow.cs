using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.GameSystems.Inventory.Data;

namespace AHAKuo.Signalia.Framework.Editors
{
    /// <summary>
    /// Main Signalia Settings window. Implementation is split across multiple partial files:
    /// `SignaliaSettingsEditor_*` for each tab / game system.
    /// </summary>
    public partial class FrameworkSettings : EditorWindow
    {
        #region Core State

        private float maxWidthProperties => position.width - 20;
        private Vector2 scrollPosition;
        private SignaliaConfigAsset config;

        private readonly string[] tabs = { "UI Config", "Radio and Effects", "Time", "Input System", "Overrides", "Assets", "Debug", "Game Systems", "Preferences" };
        private int selectedTab;

        private readonly string[] gameSystemTabs = { "Dialogue", "Saving", "Inventory", "Pooling", "Loading", "Localization", "Audio Layering", "Tutorial", "Resource Caching", "Common Mechanics", "Inline Script", "Achievements" };
        private int selectedGameSystemTab;

        // Common Mechanics subtabs
        private int commonMechanicsSubtab;
        private readonly string[] commonMechanicsSubtabs = { "Currencies", "Interactive Zones" };

        private static SerializedObject serializedObject;
        private SignaliaPreferences preferences;

        private const float LABEL_WIDTH = 250;

        #endregion

        #region Menu Items

        [MenuItem("Tools/Signalia/Settings")]
        public static void ShowWindow()
        {
            GetWindow<FrameworkSettings>("Signalia Settings").minSize = new Vector2(600, 600);
            serializedObject = new SerializedObject(GetWindow<FrameworkSettings>());
        }

        [MenuItem("Tools/Signalia/Documentation")]
        public static void OpenDocumentation()
        {
            string relativePath = FrameworkConstants.MiscPaths.GETTING_STARTED;
            string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to open Signalia documentation: " + e.Message);
                EditorUtility.DisplayDialog("Error", "Failed to open Signalia documentation. Please check if the file exists at: " + fullPath, "OK");
            }
        }

        [MenuItem("Tools/Signalia/Load Audio Assets")]
        public static void LoadAudioAssets()
        {
            bool success = ResourceHandler.LoadAudioAssets();

            if (success)
            {
                EditorUtility.DisplayDialog(
                    "Audio Assets Loaded",
                    "Successfully loaded AudioAsset files into the Signalia config. All assets preloaded and ready to use.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Audio Assets Load Failed",
                    "Failed to load AudioAsset files. Check the Console for details. Make sure you have AudioAsset files in the Resources/Signalia folder.",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Signalia/Game Systems/Load Resource Assets")]
        public static void LoadResourceAssets()
        {
            bool success = ResourceHandler.LoadResourceAssets();

            if (success)
            {
                EditorUtility.DisplayDialog(
                    "Resource Assets Loaded",
                    "Successfully loaded ResourceAsset files into the Signalia config. All cached resources are now available for instant access.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Resource Assets Load Failed",
                    "Failed to load ResourceAsset files. Check the Console for details. Make sure you have ResourceAsset files in the Resources/Signalia folder.",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Signalia/Game Systems/Resource Caching Documentation")]
        public static void OpenResourceCachingDocumentation()
        {
            string relativePath = "Assets/AHAKuo Creations/Signalia/Game Systems/Resource Caching/README.md";
            string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to open Resource Caching documentation: " + e.Message);
                EditorUtility.DisplayDialog("Error", "Failed to open Resource Caching documentation. Please check if the file exists at: " + fullPath, "OK");
            }
        }

        [MenuItem("Tools/Signalia/Game Systems/Load Item References")]
        public static void LoadItemReferences()
        {
            try
            {
                SignaliaConfigAsset cfg = ConfigReader.GetConfig(true);
                if (cfg == null)
                {
                    EditorUtility.DisplayDialog("Error", "Signalia Config not found. Please ensure SignaliaConfig asset exists.", "OK");
                    return;
                }

                List<ItemSO> foundItems = new();
                string[] guids = AssetDatabase.FindAssets("t:ItemSO", new[] { "Assets" });

                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Scan Complete", "No ItemSO assets found in the project. Please create ItemSO assets first.", "OK");
                    return;
                }

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    ItemSO asset = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                    if (asset != null)
                        foundItems.Add(asset);
                }

                cfg.InventorySystem.ItemReferences = foundItems.ToArray();
                EditorUtility.SetDirty(cfg);

                EditorUtility.DisplayDialog(
                    "Scan Complete",
                    $"Found and cached {foundItems.Count} ItemSO asset(s).\n\nThese items will now be available for saving/loading inventories.",
                    "OK"
                );

                Debug.Log($"[Signalia Inventory] Scanned and cached {foundItems.Count} ItemSO assets");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Signalia Inventory] Failed to scan ItemSO assets: {e.Message}");
                EditorUtility.DisplayDialog("Scan Failed", $"Failed to scan ItemSO assets:\n\n{e.Message}", "OK");
            }
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            config = ConfigReader.GetConfig(true); // ensure most recent config
            preferences = PreferencesReader.GetPreferences();
        }

        public static void OpenToTab(int tabIndex, int gameSystemTab)
        {
            var window = GetWindow<FrameworkSettings>("Signalia Settings");
            window.minSize = new Vector2(500, 500);
            window.selectedTab = tabIndex;
            window.selectedGameSystemTab = gameSystemTab;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(GraphicLoader.SignaliaSettings, GUILayout.Height(200), GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            switch (selectedTab)
            {
                case 0: DrawUIConfigTab(); break;
                case 1: DrawRadioAndEffectsTab(); break;
                case 2: DrawTimeTab(); break;
                case 3: DrawInputSystemTab(); break;
                case 4: DrawOverridesTab(); break;
                case 5: DrawAssetsTab(); break;
                case 6: DrawDebugging(); break;
                case 7: DrawGameSystemsTabs(); break;
                case 8: DrawPreferencesTab(); break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(20);
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };

            if (GUILayout.Button("Save Settings", buttonStyle))
            {
                ConfigReader.SaveConfig();
                EditorGUI.FocusTextInControl(null);
                EditorUtility.DisplayDialog("Settings Saved", "Signalia settings have been saved.", "OK");
            }
        }

        #endregion
    }
}

