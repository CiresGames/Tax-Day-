using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.SaveSystem
{
    /// <summary>
    /// Handles low-level file I/O operations for the Save System.
    /// Responsibilities: File reading, writing, encryption, decryption, backup, and recovery.
    /// </summary>
    public static class SaveFileHandler
    {
        // Cache persistent data path to avoid calling from non-main threads
        private static string _cachedPersistentDataPath = null;
        private static bool _pathInitialized = false;

        /// <summary>
        /// Initializes cache early on main thread (called automatically at runtime startup).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            InitializeCache();
        }

        /// <summary>
        /// Initializes the cached persistent data path. Should be called on main thread.
        /// </summary>
        public static void InitializeCache()
        {
            if (!_pathInitialized)
            {
                _cachedPersistentDataPath = Application.persistentDataPath;
                _pathInitialized = true;
                
                try
                {
                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Cached persistent data path: {_cachedPersistentDataPath}");
                }
                catch
                {
                    // Config not loaded yet, just cache the path silently
                    Debug.Log($"Cached persistent data path for SaveFileHandler: {_cachedPersistentDataPath}");
                }
            }
        }

        /// <summary>
        /// Gets the cached persistent data path (safe to call from any thread).
        /// </summary>
        private static string GetPersistentDataPath()
        {
            if (!_pathInitialized)
            {
                // Fallback to immediate access if not initialized
                return Application.persistentDataPath;
            }
            return _cachedPersistentDataPath;
        }

        /// <summary>
        /// Writes save data to disk with optional encryption and backup
        /// </summary>
        /// <param name="fileName">File name without extension</param>
        /// <param name="data">The save file data to write</param>
        public static void WriteFile(string fileName, SaveFileData data)
        {
            try
            {
                string filePath = GetFullPath(fileName);
                string directory = Path.GetDirectoryName(filePath);
                
                // Create directory if it doesn't exist
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Created directory: {directory}");
                }
                
                // Create backup of existing file
                if (File.Exists(filePath))
                {
                    string backupPath = filePath + ".backup";
                    File.Copy(filePath, backupPath, true);
                }
                
                // Serialize to JSON
                string json = JsonUtility.ToJson(data, true);
                
                // Check if encryption is enabled for this file
                var encryptionEntry = GetEncryptionEntry(fileName);
                if (encryptionEntry != null && encryptionEntry.encrypt && !string.IsNullOrEmpty(encryptionEntry.password))
                {
                    json = Encrypt(json, encryptionEntry.password);
                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Encrypted save file: {fileName}");
                }
                
                // Write to disk
                File.WriteAllText(filePath, json);
                
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log($"Saved file to disk: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write save file '{fileName}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reads save data from disk with optional decryption and recovery from backup
        /// </summary>
        /// <param name="fileName">File name without extension</param>
        /// <returns>The save file data, or a new empty data structure if file doesn't exist</returns>
        public static SaveFileData ReadFile(string fileName)
        {
            string filePath = GetFullPath(fileName);
            
            if (!File.Exists(filePath))
            {
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log($"Save file not found: {filePath}. Creating new file.");
                return new SaveFileData();
            }
            
            EncryptionEntry encryptionEntry = null;

            try
            {
                string json = File.ReadAllText(filePath);
                
                // Check if encryption is enabled for this file
                encryptionEntry = GetEncryptionEntry(fileName);
                if (encryptionEntry != null && encryptionEntry.encrypt && !string.IsNullOrEmpty(encryptionEntry.password))
                {
                    try
                    {
                        json = Decrypt(json, encryptionEntry.password);
                        if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                            Debug.Log($"Decrypted save file: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to decrypt save file '{fileName}': {ex.Message}");
                        return TryRestoreFromBackup(filePath, encryptionEntry);
                    }
                }
                
                SaveFileData data = JsonUtility.FromJson<SaveFileData>(json);
                
                if (data == null)
                {
                    Debug.LogWarning($"Failed to parse save file '{fileName}'. Attempting backup restore.");
                    return TryRestoreFromBackup(filePath, encryptionEntry);
                }
                
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to read save file '{fileName}': {ex.Message}");
                return TryRestoreFromBackup(filePath, encryptionEntry);
            }
        }

        /// <summary>
        /// Attempts to restore save data from backup file
        /// </summary>
        private static SaveFileData TryRestoreFromBackup(string filePath, EncryptionEntry encryptionEntry)
        {
            string backupPath = filePath + ".backup";
            
            if (!File.Exists(backupPath))
            {
                Debug.LogWarning($"No backup file found for: {filePath}");
                return new SaveFileData();
            }
            
            try
            {
                Debug.Log($"Attempting to restore from backup: {backupPath}");
                string json = File.ReadAllText(backupPath);
                
                // Decrypt if needed
                if (encryptionEntry != null && encryptionEntry.encrypt && !string.IsNullOrEmpty(encryptionEntry.password))
                {
                    json = Decrypt(json, encryptionEntry.password);
                }
                
                SaveFileData data = JsonUtility.FromJson<SaveFileData>(json);
                
                if (data != null)
                {
                    Debug.Log($"Successfully restored from backup: {backupPath}");
                    // Restore the backup as the main file
                    File.Copy(backupPath, filePath, true);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to restore from backup: {ex.Message}");
            }
            
            return new SaveFileData();
        }

        /// <summary>
        /// Deletes a save file and its backup
        /// </summary>
        public static void DeleteFile(string fileName)
        {
            string filePath = GetFullPath(fileName);
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Deleted save file: {filePath}");
                }
                
                string backupPath = filePath + ".backup";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Deleted backup file: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete save file '{fileName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        public static bool FileExists(string fileName)
        {
            return File.Exists(GetFullPath(fileName));
        }

        /// <summary>
        /// Gets the full path for a save file (handles subdirectories)
        /// </summary>
        public static string GetFullPath(string fileName)
        {
            string extension = ConfigReader.GetConfig().SavingSystem.SaveFileExtension;
            string directory = ConfigReader.GetConfig().SavingSystem.SaveDirectoryPath;
            
            // Normalize fileName (remove extension if already present)
            if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - extension.Length);
            }
            
            // Combine with directory path
            string relativePath = string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName);
            
            // Add extension
            relativePath += extension;
            
            // Combine with cached persistent data path (safe for use from any thread)
            return Path.Combine(GetPersistentDataPath(), relativePath);
        }

        /// <summary>
        /// Gets the encryption entry for a specific file
        /// </summary>
        private static EncryptionEntry GetEncryptionEntry(string fileName)
        {
            var rules = ConfigReader.GetConfig().SavingSystem.EncryptionRules;
            
            foreach (var entry in rules)
            {
                if (entry.fileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
            
            return null;
        }

        #region Encryption/Decryption

        /// <summary>
        /// Encrypts a string using AES-256 encryption
        /// </summary>
        private static string Encrypt(string plainText, string password)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    // Derive key from password using SHA-256
                    byte[] key = DeriveKey(password);
                    aes.Key = key;
                    
                    // Derive IV from password + "IV" suffix
                    byte[] iv = DeriveIV(password);
                    aes.IV = iv;
                    
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                            cs.Write(plainBytes, 0, plainBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Encryption failed: {ex.Message}");
                return plainText;
            }
        }

        /// <summary>
        /// Decrypts a string using AES-256 encryption
        /// </summary>
        private static string Decrypt(string cipherText, string password)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    // Derive key from password using SHA-256
                    byte[] key = DeriveKey(password);
                    aes.Key = key;
                    
                    // Derive IV from password + "IV" suffix
                    byte[] iv = DeriveIV(password);
                    aes.IV = iv;
                    
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    
                    using (MemoryStream ms = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (StreamReader reader = new StreamReader(cs))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Decryption failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Derives a 256-bit key from password using SHA-256
        /// </summary>
        private static byte[] DeriveKey(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        /// <summary>
        /// Derives a 128-bit IV from password + "IV" suffix using SHA-256 (truncated to 16 bytes)
        /// </summary>
        private static byte[] DeriveIV(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "IV"));
                byte[] iv = new byte[16]; // AES IV is 16 bytes
                Array.Copy(hash, iv, 16);
                return iv;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the data structure for a save file
    /// </summary>
    [Serializable]
    public class SaveFileData
    {
        public SaveDataEntry[] data = new SaveDataEntry[0];

        /// <summary>
        /// Sets a value in the save data
        /// </summary>
        public void SetValue(string key, string value)
        {
            // Find existing entry
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].key == key)
                {
                    data[i].value = value;
                    return;
                }
            }
            
            // Add new entry
            Array.Resize(ref data, data.Length + 1);
            data[data.Length - 1] = new SaveDataEntry { key = key, value = value };
        }

        /// <summary>
        /// Gets a value from the save data
        /// </summary>
        public bool TryGetValue(string key, out string value)
        {
            foreach (var entry in data)
            {
                if (entry.key == key)
                {
                    value = entry.value;
                    return true;
                }
            }
            
            value = null;
            return false;
        }

        /// <summary>
        /// Checks if a key exists in the save data
        /// </summary>
        public bool ContainsKey(string key)
        {
            foreach (var entry in data)
            {
                if (entry.key == key)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a key from the save data
        /// </summary>
        public bool RemoveKey(string key)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i].key == key)
                {
                    // Remove entry by creating a new array without it
                    SaveDataEntry[] newData = new SaveDataEntry[data.Length - 1];
                    Array.Copy(data, 0, newData, 0, i);
                    Array.Copy(data, i + 1, newData, i, data.Length - i - 1);
                    data = newData;
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Represents a single key-value pair in the save data
    /// </summary>
    [Serializable]
    public class SaveDataEntry
    {
        public string key;
        public string value;
    }
}
