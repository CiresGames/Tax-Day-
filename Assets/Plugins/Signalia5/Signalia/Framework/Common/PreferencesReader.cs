using UnityEngine;
using System.IO;

namespace AHAKuo.Signalia.Framework
{
    /// <summary>
    /// Manages loading and saving of Signalia editor preferences from a JSON file.
    /// Editor-only: This file is not included in builds.
    /// </summary>
#if UNITY_EDITOR
    public static class PreferencesReader
    {
        private static SignaliaPreferences cachedPreferences;
        private const string PreferencesFileName = "SignaliaPreferences.json";
        private static string PreferencesFilePath => Path.Combine("Assets", "AHAKuo Creations", "Signalia", PreferencesFileName);

        /// <summary>
        /// Gets the current preferences, loading from file if necessary.
        /// </summary>
        public static SignaliaPreferences GetPreferences(bool forceReload = false)
        {
            if (forceReload || cachedPreferences == null)
            {
                LoadPreferences();
            }
            return cachedPreferences;
        }

        /// <summary>
        /// Loads preferences from the JSON file, creating default preferences if the file doesn't exist.
        /// </summary>
        private static void LoadPreferences()
        {
            string fullPath = PreferencesFilePath;
            
            if (File.Exists(fullPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(fullPath);
                    cachedPreferences = JsonUtility.FromJson<SignaliaPreferences>(jsonContent);
                    
                    if (cachedPreferences == null)
                    {
                        Debug.LogWarning("[Signalia Preferences] Failed to deserialize preferences file. Creating default preferences.");
                        cachedPreferences = SignaliaPreferences.CreateDefault();
                        SavePreferences();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Signalia Preferences] Error loading preferences file: {e.Message}");
                    cachedPreferences = SignaliaPreferences.CreateDefault();
                    SavePreferences();
                }
            }
            else
            {
                // File doesn't exist, create default preferences
                cachedPreferences = SignaliaPreferences.CreateDefault();
                SavePreferences();
            }
        }

        /// <summary>
        /// Saves the current preferences to the JSON file.
        /// </summary>
        public static void SavePreferences()
        {
            if (cachedPreferences == null)
            {
                Debug.LogWarning("[Signalia Preferences] Cannot save null preferences. Creating default preferences.");
                cachedPreferences = SignaliaPreferences.CreateDefault();
            }

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(PreferencesFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonContent = JsonUtility.ToJson(cachedPreferences, true); // Pretty print
                File.WriteAllText(PreferencesFilePath, jsonContent);
                
                // Refresh Unity's asset database so the file appears in the Project window
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia Preferences] Error saving preferences file: {e.Message}");
            }
        }

        /// <summary>
        /// Forces a reload of preferences from the file.
        /// </summary>
        public static void ReloadPreferences()
        {
            cachedPreferences = null;
            LoadPreferences();
        }
    }
#else
    // Runtime stub - preferences are editor-only
    public static class PreferencesReader
    {
        public static SignaliaPreferences GetPreferences(bool forceReload = false)
        {
            return SignaliaPreferences.CreateDefault();
        }
    }
#endif
}

