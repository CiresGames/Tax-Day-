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
        #region Game Systems - Save System

        // ──────────────────────────────
        // Save System Fields
        // ──────────────────────────────
        private string saveKey = "";
        private string saveValue = "";
        private string saveFilename = "savefile";
        private List<string> saveKeys = new();
        private int selectedSaveKeyIndex = 0;
        private int saveTabIndex = 0;
        private readonly string[] saveTabs = new string[] { "Settings", "Utility" };

        // ──────────────────────────────
        // Save System Methods
        // ──────────────────────────────
        private void DrawSaveSystemTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.GameSavingHeader, maxWidthProperties);
            GUILayout.Space(10);

            GUI.backgroundColor = Color.gray;
            saveTabIndex = GUILayout.Toolbar(saveTabIndex, saveTabs, GUILayout.Height(24), GUILayout.MaxWidth(maxWidthProperties));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            switch (saveTabIndex)
            {
                case 0: DrawSaveSettingsTab(); break;
                case 1: DrawSaveUtilityTab(); break;
            }
        }

        // ──────────────────────────────
        // Save System Tab Methods
        // ──────────────────────────────
        private void DrawSaveSettingsTab()
        {
            config.SavingSystem.SaveFileExtension = EditorGUILayout.TextField(
                new GUIContent("Save File Extension", "Extension for the save file."),
                config.SavingSystem.SaveFileExtension, GUILayout.MaxWidth(maxWidthProperties));

            config.SavingSystem.SettingsFileName = EditorGUILayout.TextField(
                new GUIContent("Settings File Name", "Name of the settings file (for preferences)."),
                config.SavingSystem.SettingsFileName, GUILayout.MaxWidth(maxWidthProperties));

            config.SavingSystem.SaveDirectoryPath = EditorGUILayout.TextField(
                new GUIContent("Save Directory Path", "Directory path for save files (relative to persistentDataPath). Leave empty for root."),
                config.SavingSystem.SaveDirectoryPath, GUILayout.MaxWidth(maxWidthProperties));

            config.SavingSystem.LogSaving = EditorGUILayout.Toggle(
                new GUIContent("Log Saving", "Logs saving actions."),
                config.SavingSystem.LogSaving, GUILayout.MaxWidth(maxWidthProperties));

            EditorGUILayout.HelpBox(
                               "Preloaded Save Files: List of save files to preload at Signalia initialization. " +
                               "Helps avoid performance hiccups when loading save data for the first time during gameplay. " +
                               "It's recommended to use this for save files that are frequently accessed, especially those that are encrypted.",
                               MessageType.Info);

            // Display the array as a comma-separated string
            string commaSeparated = string.Join(",", config.SavingSystem.CachedSaveFiles);
            commaSeparated = EditorGUILayout.TextField(
                new GUIContent("Preloaded Save Files", "Comma-separated list of preloaded save files. Write down the name of each save file (without extension)."),
                commaSeparated, GUILayout.MaxWidth(maxWidthProperties));

            // Parse back to array
            config.SavingSystem.CachedSaveFiles = commaSeparated
                .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            GUILayout.Space(20);

            // Encryption Rules Section
            EditorGUILayout.LabelField("Per-File Encryption Rules", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure encryption settings per save file. Each file can have its own password.\n" +
                "IMPORTANT: Use strong passwords. If you lose a password, that save file will be unrecoverable!",
                MessageType.Warning);

            // Display encryption rules
            if (config.SavingSystem.EncryptionRules == null)
            {
                config.SavingSystem.EncryptionRules = new EncryptionEntry[0];
            }

            GUILayout.Space(5);

            // Display each encryption entry
            for (int i = 0; i < config.SavingSystem.EncryptionRules.Length; i++)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Rule {i + 1}", EditorStyles.boldLabel, GUILayout.Width(60));
                
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    var list = config.SavingSystem.EncryptionRules.ToList();
                    list.RemoveAt(i);
                    config.SavingSystem.EncryptionRules = list.ToArray();
                    EditorUtility.SetDirty(config);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
                GUILayout.EndHorizontal();

                config.SavingSystem.EncryptionRules[i].fileName = EditorGUILayout.TextField(
                    new GUIContent("File Name", "Name of the file to encrypt (without extension)"),
                    config.SavingSystem.EncryptionRules[i].fileName);

                config.SavingSystem.EncryptionRules[i].encrypt = EditorGUILayout.Toggle(
                    new GUIContent("Enable Encryption", "Whether to encrypt this file"),
                    config.SavingSystem.EncryptionRules[i].encrypt);

                if (config.SavingSystem.EncryptionRules[i].encrypt)
                {
                    config.SavingSystem.EncryptionRules[i].password = EditorGUILayout.PasswordField(
                        new GUIContent("Password", "Encryption password for this file"),
                        config.SavingSystem.EncryptionRules[i].password);
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            // Add new encryption rule button
            if (GUILayout.Button("Add Encryption Rule", GUILayout.Height(30)))
            {
                var list = config.SavingSystem.EncryptionRules.ToList();
                list.Add(new EncryptionEntry { fileName = "newfile", encrypt = false, password = "" });
                config.SavingSystem.EncryptionRules = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawSaveUtilityTab()
        {
            saveFilename = EditorGUILayout.TextField("Filename (path included):", saveFilename, GUILayout.MaxWidth(maxWidthProperties));

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Key-Value Pair", EditorStyles.boldLabel, GUILayout.MaxWidth(maxWidthProperties));
            saveKey = EditorGUILayout.TextField("Key:", saveKey, GUILayout.MaxWidth(maxWidthProperties));
            saveValue = EditorGUILayout.TextField("Value:", saveValue, GUILayout.MaxWidth(maxWidthProperties));

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxWidthProperties));
            if (GUILayout.Button("Save Value", GUILayout.Height(30), GUILayout.MaxWidth(maxWidthProperties)))
                SaveValueToFile();

            if (GUILayout.Button("Load Value", GUILayout.Height(30), GUILayout.MaxWidth(maxWidthProperties)))
                LoadValueFromFile();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(maxWidthProperties));
            if (GUILayout.Button("Refresh Keys", GUILayout.Width(120), GUILayout.MaxWidth(maxWidthProperties)))
                RefreshSaveKeys();

            if (saveKeys.Count > 0)
            {
                selectedSaveKeyIndex = EditorGUILayout.Popup("Existing Keys", selectedSaveKeyIndex, saveKeys.ToArray(), GUILayout.MaxWidth(maxWidthProperties));
                if (GUILayout.Button("Select Key", GUILayout.Width(100), GUILayout.MaxWidth(maxWidthProperties)))
                {
                    saveKey = saveKeys[selectedSaveKeyIndex];
                    saveValue = SIGS.LoadString(saveKey, saveFilename);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);
            if (GUILayout.Button("Open Save Folder", GUILayout.Width(200), GUILayout.MaxWidth(maxWidthProperties)))
                OpenSaveFolder();

            if (GUILayout.Button("Wipe All Data", GUILayout.Width(200), GUILayout.MaxWidth(maxWidthProperties)))
            {
                SIGS.WipeAllSaveData();
                Debug.Log("Wiped all save data.");
                RefreshSaveKeys();
            }
        }

        // ──────────────────────────────
        // Save System Utility Methods
        // ──────────────────────────────
        private void SaveValueToFile()
        {
            if (string.IsNullOrEmpty(saveKey) || string.IsNullOrEmpty(saveFilename))
            {
                Debug.LogError("Key and filename must be provided.");
                return;
            }

            SIGS.SaveData(saveKey, saveValue, saveFilename);
            Debug.Log($"Saved '{saveKey}: {saveValue}' to '{saveFilename}.sav'");
            RefreshSaveKeys();
        }

        private void LoadValueFromFile()
        {
            if (string.IsNullOrEmpty(saveKey) || string.IsNullOrEmpty(saveFilename))
            {
                Debug.LogError("Key and filename must be provided.");
                return;
            }

            var loaded = SIGS.LoadString(saveKey, saveFilename);
            if (!string.IsNullOrEmpty(loaded))
            {
                saveValue = loaded;
                Debug.Log($"Loaded '{saveKey}: {saveValue}' from '{saveFilename}.sav'");
            }
            else
            {
                Debug.LogWarning($"Key '{saveKey}' not found in '{saveFilename}.sav'");
            }
        }

        private void RefreshSaveKeys()
        {
            saveKeys.Clear();
            var data = SIGS.LoadAllSaveData(saveFilename);
            if (data.Count > 0)
            {
                saveKeys.AddRange(data.Keys);
                Debug.Log($"Found {saveKeys.Count} keys in '{saveFilename}.sav'");
            }
            else
            {
                Debug.LogWarning("No save data found.");
            }
        }

        private void OpenSaveFolder()
        {
            var folderPath = Application.persistentDataPath;

            if (Directory.Exists(folderPath))
                EditorUtility.RevealInFinder(folderPath);
            else
                Debug.LogError("Persistent data path does not exist.");
        }

        #endregion

    }
}
