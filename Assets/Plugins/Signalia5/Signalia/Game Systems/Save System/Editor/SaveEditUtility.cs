using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using System;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.GameSystems.SaveSystem
{
    [Obsolete("This utility is deprecated. Use the game system tab in the signalia settings.")]
    /// <summary>
    /// Utility to help save and load data from the editor.
    /// </summary>
    public class SaveEditUtility : EditorWindow
    {
        private string key = "";
        private string value = "";
        private string filename = "savefile"; // No need to add .sav extension, it's handled automatically
        private List<string> currentKeys = new();
        private int selectedKeyIndex = 0;

        [MenuItem("Tools/Signalia/Game Systems/[OBSOLETE] Save Edit Utility")]
        public static void ShowWindow()
        {
            // tell the user this is deprecated and to use the game system tab in the signalia settings
            if (EditorUtility.DisplayDialog(
                "Deprecated Utility",
                "This Save Edit Utility is deprecated. Please use the Game System tab in the Signalia Settings instead.",
                "Go There"
            ))
            {
                // open the Signalia Settings window
                FrameworkSettings.OpenToTab(7,1);
                return;
            }
            GetWindow<SaveEditUtility>("Save Edit Utility");
        }

        private void OnGUI()
        {
            GUILayout.Label("Save/Load Utility", EditorStyles.boldLabel);

            // Custom Help Box with Larger Font
            GUIStyle helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 14,
                wordWrap = true,
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.LabelField(
                "Welcome to the Game House Save System!\n\n" +
                "How to Use from Code:\n" +
                "- Save: SIGS.SaveData(\"PlayerName\", \"Jenny\", \"savefile\");\n" +
                "- Load: string name = SIGS.LoadData<string>(\"PlayerName\", \"savefile\");\n\n" +
                "How to Use from Editor:\n" +
                "1. Enter the Filename at the top.\n" +
                "2. Add a Key and Value.\n" +
                "3. Use 'Save Value' to store it.\n" +
                "4. Use 'Refresh Keys' to view existing keys.\n" +
                "5. Select a key to edit or view its value.",
                helpBoxStyle
            );

            EditorGUILayout.Space(10);

            GUIStyle filenameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.cyan }
            };
            GUILayout.Label("Save File Name (Important):", filenameStyle);

            filename = EditorGUILayout.TextField("Enter Filename:", filename);

            EditorGUILayout.Space(10);
            GUILayout.Label("Key-Value Pair", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key:", GUILayout.Width(50));
            key = EditorGUILayout.TextField(key);
            EditorGUILayout.LabelField("Value:", GUILayout.Width(50));
            value = EditorGUILayout.TextField(value);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Keys", GUILayout.Width(120)))
            {
                RefreshKeys();
            }

            if (currentKeys.Count > 0)
            {
                selectedKeyIndex = EditorGUILayout.Popup("Existing Keys", selectedKeyIndex, currentKeys.ToArray());
                if (currentKeys.Count > selectedKeyIndex)
                {
                    if (GUILayout.Button("Select Key", GUILayout.Width(100)))
                    {
                        key = currentKeys[selectedKeyIndex];
                        value = SIGS.LoadString(key, filename);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Save and Load Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Value", GUILayout.Height(30)))
            {
                SaveValue();
            }
            if (GUILayout.Button("Load Value", GUILayout.Height(30)))
            {
                LoadValue();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Open Save Folder button
            if (GUILayout.Button("Open Save Folder", GUILayout.Height(25)))
            {
                OpenSaveFolder();
            }

            // Wipe All Data Button
            if (GUILayout.Button("Wipe All Data", GUILayout.Height(25)))
            {
                SIGS.WipeAllSaveData();
                Debug.Log($"Wiped all data.");
                RefreshKeys();
            }

            EditorGUILayout.Space();
        }

        private void SaveValue()
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(filename))
            {
                Debug.LogError("Key and Filename must be provided.");
                return;
            }

            SIGS.SaveData(key, value, filename);
            Debug.Log($"Saved '{key}: {value}' in '{filename}.sav'.");
            RefreshKeys();
        }

        private void LoadValue()
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(filename))
            {
                Debug.LogError("Key and Filename must be provided.");
                return;
            }

            string loadedValue = SIGS.LoadString(key, filename);
            if (!string.IsNullOrEmpty(loadedValue))
            {
                value = loadedValue;
                Debug.Log($"Loaded '{key}: {value}' from '{filename}.sav'.");
            }
            else
            {
                Debug.LogWarning($"Key '{key}' not found in '{filename}.sav'.");
            }
        }

        private void RefreshKeys()
        {
            currentKeys.Clear();
            Dictionary<string, string> saveData = SIGS.LoadAllSaveData(filename);

            if (saveData.Count > 0)
            {
                currentKeys.AddRange(saveData.Keys);
                Debug.Log($"Found {currentKeys.Count} keys in '{filename}.sav'.");
            }
            else
            {
                Debug.LogWarning("No keys found or save file does not exist.");
            }
        }

        private void OpenSaveFolder()
        {
            string filePath = Path.Combine(Application.persistentDataPath, filename + ConfigReader.GetConfig().SavingSystem.SaveFileExtension);
            if (File.Exists(filePath))
            {
                EditorUtility.RevealInFinder(filePath);
            }
            else
            {
                Debug.LogError("Save file does not exist.");
            }
        }
    }
}
