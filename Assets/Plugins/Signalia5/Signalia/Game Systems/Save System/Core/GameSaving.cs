using System;
using System.Threading.Tasks;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.SaveSystem
{
    /// <summary>
    /// Public API for the Game Saving System.
    /// Provides simple static methods for save/load operations.
    /// All complexity is abstracted away - just call Save() and Load().
    /// 
    /// Example Usage:
    ///   GameSaving.Save("playerName", "Hero", "playerdata");
    ///   string name = GameSaving.Load("playerName", "playerdata", "Unknown");
    /// </summary>
    public static class GameSaving
    {
        private static bool _initialized = false;
        private static GameSavingAutoSave _autoSave = null;

        /// <summary>
        /// Initializes the save system (called automatically on first use)
        /// </summary>
        private static void Initialize()
        {
            if (_initialized) return;

            // Initialize persistent data path cache (must be called on main thread)
            SaveFileHandler.InitializeCache();

            // Create auto-save MonoBehaviour
            if (_autoSave == null)
            {
                GameObject autoSaveObject = new GameObject("GameSavingAutoSave");
                _autoSave = autoSaveObject.AddComponent<GameSavingAutoSave>();
                UnityEngine.Object.DontDestroyOnLoad(autoSaveObject);
            }

            _initialized = true;

            if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                Debug.Log("GameSaving system initialized");
        }

        #region Save Operations

        /// <summary>
        /// Saves a key-value pair to the specified file (synchronous).
        /// The value is written to disk immediately.
        /// Use this for critical data that must be persisted immediately.
        /// 
        /// Example: GameSaving.Save("health", 100, "playerdata");
        /// </summary>
        /// <param name="key">The key to save under</param>
        /// <param name="value">The value to save (any serializable type)</param>
        /// <param name="fileName">The file name (without extension)</param>
        public static void Save(string key, object value, string fileName)
        {
            Initialize();
            SaveManager.Instance.Save(key, value, fileName);
        }

        /// <summary>
        /// Saves a key-value pair to the specified file (asynchronous).
        /// The value is queued for writing and batched with other saves.
        /// Use this for non-critical data to improve performance.
        /// 
        /// Example: await GameSaving.SaveAsync("score", 1000, "playerdata");
        /// </summary>
        /// <param name="key">The key to save under</param>
        /// <param name="value">The value to save (any serializable type)</param>
        /// <param name="fileName">The file name (without extension)</param>
        public static async Task SaveAsync(string key, object value, string fileName)
        {
            Initialize();
            await SaveManager.Instance.SaveAsync(key, value, fileName);
        }

        /// <summary>
        /// Saves a preference value using the default settings file (synchronous).
        /// Preferences are typically used for game settings like volume, graphics quality, etc.
        /// 
        /// Example: GameSaving.SavePreference("musicVolume", 0.8f);
        /// </summary>
        /// <param name="key">The key to save under</param>
        /// <param name="value">The value to save</param>
        public static void SavePreference(string key, object value)
        {
            Initialize();
            string settingsFile = ConfigReader.GetConfig().SavingSystem.SettingsFileName;
            SaveManager.Instance.Save(key, value, settingsFile);
        }

        /// <summary>
        /// Saves a preference value using the default settings file (asynchronous).
        /// 
        /// Example: await GameSaving.SavePreferenceAsync("fullscreen", true);
        /// </summary>
        /// <param name="key">The key to save under</param>
        /// <param name="value">The value to save</param>
        public static async Task SavePreferenceAsync(string key, object value)
        {
            Initialize();
            string settingsFile = ConfigReader.GetConfig().SavingSystem.SettingsFileName;
            await SaveManager.Instance.SaveAsync(key, value, settingsFile);
        }

        /// <summary>
        /// Forces all pending async saves to complete immediately.
        /// Use this before critical operations like application quit or scene transitions.
        /// 
        /// Example: await GameSaving.ForceSaveAllAsync();
        /// </summary>
        public static async Task ForceSaveAllAsync()
        {
            Initialize();
            await SaveManager.Instance.ForceSaveAllAsync();
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Loads a value from the specified file.
        /// Returns the default value if the key doesn't exist.
        /// 
        /// Example: int health = GameSaving.Load("health", "playerdata", 100);
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="key">The key to load</param>
        /// <param name="fileName">The file name (without extension)</param>
        /// <param name="defaultValue">The value to return if key doesn't exist</param>
        /// <returns>The loaded value or defaultValue</returns>
        public static T Load<T>(string key, string fileName, T defaultValue)
        {
            Initialize();
            return SaveManager.Instance.Load(key, fileName, defaultValue);
        }

        /// <summary>
        /// Loads a value from the specified file.
        /// Returns default(T) if the key doesn't exist.
        /// 
        /// Example: int health = GameSaving.Load<int>("health", "playerdata");
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="key">The key to load</param>
        /// <param name="fileName">The file name (without extension)</param>
        /// <returns>The loaded value or default(T)</returns>
        public static T Load<T>(string key, string fileName)
        {
            Initialize();
            return SaveManager.Instance.Load(key, fileName, default(T));
        }

        /// <summary>
        /// Loads a string value from the specified file (legacy method).
        /// Returns empty string if the key doesn't exist.
        /// 
        /// Example: string name = GameSaving.StringTypeLoad("playerName", "playerdata");
        /// </summary>
        /// <param name="key">The key to load</param>
        /// <param name="fileName">The file name (without extension)</param>
        /// <returns>The loaded string or empty string</returns>
        public static string StringTypeLoad(string key, string fileName)
        {
            Initialize();
            return SaveManager.Instance.Load(key, fileName, "");
        }

        /// <summary>
        /// Loads a value from the specified file (asynchronous).
        /// Returns the default value if the key doesn't exist.
        /// 
        /// Example: int health = await GameSaving.LoadAsync("health", "playerdata", 100);
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="key">The key to load</param>
        /// <param name="fileName">The file name (without extension)</param>
        /// <param name="defaultValue">The value to return if key doesn't exist</param>
        /// <returns>The loaded value or defaultValue</returns>
        public static async Task<T> LoadAsync<T>(string key, string fileName, T defaultValue)
        {
            Initialize();
            return await SaveManager.Instance.LoadAsync(key, fileName, defaultValue);
        }

        /// <summary>
        /// Loads a preference value from the default settings file.
        /// Returns the default value if the key doesn't exist.
        /// 
        /// Example: float volume = GameSaving.LoadPreference("musicVolume", 1.0f);
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="key">The key to load</param>
        /// <param name="defaultValue">The value to return if key doesn't exist</param>
        /// <returns>The loaded value or defaultValue</returns>
        public static T LoadPreference<T>(string key, T defaultValue)
        {
            Initialize();
            string settingsFile = ConfigReader.GetConfig().SavingSystem.SettingsFileName;
            return SaveManager.Instance.Load(key, settingsFile, defaultValue);
        }

        /// <summary>
        /// Loads a preference value from the default settings file (asynchronous).
        /// 
        /// Example: bool fullscreen = await GameSaving.LoadPreferenceAsync("fullscreen", false);
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="key">The key to load</param>
        /// <param name="defaultValue">The value to return if key doesn't exist</param>
        /// <returns>The loaded value or defaultValue</returns>
        public static async Task<T> LoadPreferenceAsync<T>(string key, T defaultValue)
        {
            Initialize();
            string settingsFile = ConfigReader.GetConfig().SavingSystem.SettingsFileName;
            return await SaveManager.Instance.LoadAsync(key, settingsFile, defaultValue);
        }

        #endregion

        #region Modification Operations

        /// <summary>
        /// Deletes a key from a save file.
        /// 
        /// Example: GameSaving.DeleteKey("oldScore", "playerdata");
        /// </summary>
        /// <param name="key">The key to delete</param>
        /// <param name="fileName">The file name (without extension)</param>
        public static void DeleteKey(string key, string fileName)
        {
            Initialize();
            SaveManager.Instance.DeleteKey(key, fileName);
        }

        /// <summary>
        /// Deletes a key from a save file (asynchronous).
        /// The deletion is queued and batched with other operations.
        /// 
        /// Example: await GameSaving.DeleteAsync("oldScore", "playerdata");
        /// </summary>
        /// <param name="key">The key to delete</param>
        /// <param name="fileName">The file name (without extension)</param>
        public static async Task DeleteAsync(string key, string fileName)
        {
            Initialize();
            await SaveManager.Instance.DeleteKeyAsync(key, fileName);
        }

        /// <summary>
        /// Deletes an entire save file.
        /// 
        /// Example: GameSaving.DeleteFile("playerdata");
        /// </summary>
        /// <param name="fileName">The file name (without extension)</param>
        public static void DeleteFile(string fileName)
        {
            Initialize();
            SaveManager.Instance.DeleteFile(fileName);
        }

        /// <summary>
        /// Checks if a key exists in a save file.
        /// 
        /// Example: if (GameSaving.KeyExists("health", "playerdata")) { ... }
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <param name="fileName">The file name (without extension)</param>
        /// <returns>True if the key exists</returns>
        public static bool KeyExists(string key, string fileName)
        {
            Initialize();
            return SaveManager.Instance.KeyExists(key, fileName);
        }

        /// <summary>
        /// Checks if a save file exists.
        /// 
        /// Example: if (GameSaving.FileExists("playerdata")) { ... }
        /// </summary>
        /// <param name="fileName">The file name (without extension)</param>
        /// <returns>True if the file exists</returns>
        public static bool FileExists(string fileName)
        {
            Initialize();
            return SaveManager.Instance.FileExists(fileName);
        }

        /// <summary>
        /// Wipes all save data (deletes all save files).
        /// Use with caution - this is irreversible!
        /// 
        /// Example: GameSaving.WipeAllData();
        /// </summary>
        public static void WipeAllData()
        {
            Initialize();
            SaveManager.Instance.WipeAllData();
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Initializes the cache for a specific file (legacy method).
        /// Note: Caching is now automatic, but this is kept for backwards compatibility.
        /// 
        /// Example: GameSaving.InitializeLoadedCache("playerdata");
        /// </summary>
        /// <param name="fileName">The file name (without extension)</param>
        public static void InitializeLoadedCache(string fileName)
        {
            Initialize();
            // Load the file to ensure it's in cache
            SaveManager.Instance.Load<object>(fileName, fileName, null);
        }

        /// <summary>
        /// Clears the cache for a specific file.
        /// The file will be re-loaded from disk on next access.
        /// 
        /// Example: GameSaving.ClearCache("playerdata");
        /// </summary>
        /// <param name="fileName">The file name (without extension)</param>
        public static void ClearCache(string fileName)
        {
            Initialize();
            SaveManager.Instance.ClearCache(fileName);
        }

        /// <summary>
        /// Clears all cached save files.
        /// All files will be re-loaded from disk on next access.
        /// 
        /// Example: GameSaving.ClearAllCaches();
        /// </summary>
        public static void ClearAllCaches()
        {
            Initialize();
            SaveManager.Instance.ClearAllCaches();
        }

        /// <summary>
        /// Checks if there are any pending saves.
        /// 
        /// Example: if (GameSaving.HasPendingSaves()) { ... }
        /// </summary>
        /// <returns>True if there are pending saves</returns>
        public static bool HasPendingSaves()
        {
            Initialize();
            return SaveManager.Instance.HasPendingSaves();
        }

        /// <summary>
        /// Checks if a specific file has pending saves (legacy method).
        /// 
        /// Example: if (GameSaving.HasPendingSaves("playerdata")) { ... }
        /// </summary>
        /// <param name="fileName">The file name to check</param>
        /// <returns>True if the file has pending saves</returns>
        public static bool HasPendingSaves(string fileName)
        {
            // For backwards compatibility, always return false since we can't track per-file pending status in the new architecture
            return HasPendingSaves();
        }

        /// <summary>
        /// Gets the number of files with pending saves.
        /// 
        /// Example: int count = GameSaving.GetPendingSaveCount();
        /// </summary>
        /// <returns>Number of files with pending saves</returns>
        public static int GetPendingSaveCount()
        {
            Initialize();
            return SaveManager.Instance.GetPendingSaveCount();
        }

        /// <summary>
        /// Loads all key-value pairs from a save file (legacy method).
        /// Returns a dictionary of all data in the file.
        /// 
        /// Example: Dictionary<string, string> data = GameSaving.LoadAllData("playerdata");
        /// </summary>
        /// <param name="fileName">The file name (without extension)</param>
        /// <returns>Dictionary containing all key-value pairs</returns>
        public static System.Collections.Generic.Dictionary<string, string> LoadAllData(string fileName)
        {
            Initialize();
            fileName = NormalizeFileName(fileName);
            var metadata = SaveManager.Instance.GetOrCreateMetadataPublic(fileName);
            
            var result = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var entry in metadata.Data.data)
            {
                result[entry.key] = entry.value;
            }
            return result;
        }

        /// <summary>
        /// Loads all key-value pairs from a save file asynchronously.
        /// Returns a dictionary of all data in the file.
        /// 
        /// Example: var data = await GameSaving.LoadAllAsync("playerdata");
        /// </summary>
        /// <param name="fileName">The file name (without extension)</param>
        /// <returns>Dictionary containing all key-value pairs</returns>
        public static async Task<System.Collections.Generic.Dictionary<string, string>> LoadAllAsync(string fileName)
        {
            Initialize();
            fileName = NormalizeFileName(fileName);
            return await SaveManager.Instance.LoadAllAsync(fileName);
        }

        /// <summary>
        /// Normalizes file name by removing extension if present (internal helper)
        /// </summary>
        private static string NormalizeFileName(string fileName)
        {
            string extension = ConfigReader.GetConfig().SavingSystem.SaveFileExtension;
            if (fileName.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - extension.Length);
            }
            return fileName;
        }

        #endregion

        #region Custom Parsers

        /// <summary>
        /// Registers a custom parser for serializing/deserializing custom types.
        /// Custom parsers have priority over built-in parsers.
        /// 
        /// Example:
        /// GameSaving.RegisterCustomParser(new MyCustomParser());
        /// </summary>
        /// <param name="parser">The custom parser to register</param>
        public static void RegisterCustomParser(ISaveParser parser)
        {
            SaveParsers.RegisterCustomParser(parser);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Called by GameSavingAutoSave when application quits
        /// </summary>
        internal static async void OnApplicationQuit()
        {
            if (HasPendingSaves())
            {
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log("Application quitting - flushing all pending saves...");
                
                await ForceSaveAllAsync();
                
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log("All pending saves completed.");
            }
        }

        /// <summary>
        /// Called by GameSavingAutoSave when application pauses (mobile)
        /// </summary>
        internal static async void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && HasPendingSaves())
            {
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log("Application pausing - flushing all pending saves...");
                
                await ForceSaveAllAsync();
                
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log("All pending saves completed.");
            }
        }

        #endregion
    }
}
