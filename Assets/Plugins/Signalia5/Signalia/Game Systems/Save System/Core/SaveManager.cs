using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.SaveSystem
{
    /// <summary>
    /// Core manager for the Save System.
    /// Handles caching, threading, and coordination between memory and file system.
    /// Thread-safe using SemaphoreSlim locks per file.
    /// </summary>
    public class SaveManager
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SaveManager();
                }
                return _instance;
            }
        }

        // Cache: fileName -> SaveFileMetadata
        private readonly ConcurrentDictionary<string, SaveFileMetadata> _cache = new ConcurrentDictionary<string, SaveFileMetadata>();

        private SaveManager()
        {
            // Initialize persistent data path cache (must be on main thread)
            SaveFileHandler.InitializeCache();
            
            // Initialize cached files on startup
            InitializeCachedFiles();
        }

        /// <summary>
        /// Initializes cached files specified in config
        /// </summary>
        private void InitializeCachedFiles()
        {
            var cachedFiles = ConfigReader.GetConfig().SavingSystem.CachedSaveFiles;
            
            if (cachedFiles != null && cachedFiles.Length > 0)
            {
                foreach (var fileName in cachedFiles)
                {
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        // Load into cache
                        GetOrCreateMetadata(fileName);
                        
                        if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                            Debug.Log($"Pre-cached save file: {fileName}");
                    }
                }
            }
        }

        #region Save Operations

        /// <summary>
        /// Synchronous save - writes immediately to disk
        /// </summary>
        public void Save(string key, object value, string fileName)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError("Save key cannot be null or empty.");
                return;
            }

            fileName = NormalizeFileName(fileName);
            string serializedValue = SaveParsers.Serialize(value);

            var metadata = GetOrCreateMetadata(fileName);

            // Lock file for thread safety
            metadata.FileLock.Wait();
            try
            {
                // Update cache
                metadata.Data.SetValue(key, serializedValue);
                metadata.IsDirty = true;
                metadata.IsLoaded = true;

                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log($"Saved '{key}' = '{serializedValue}' to '{fileName}' (cache)");

                // Flush immediately (synchronous)
                FlushFile(fileName, metadata);
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        /// <summary>
        /// Asynchronous save - queues write with delay to batch multiple saves
        /// </summary>
        public async Task SaveAsync(string key, object value, string fileName)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError("Save key cannot be null or empty.");
                return;
            }

            fileName = NormalizeFileName(fileName);
            string serializedValue = SaveParsers.Serialize(value);

            var metadata = GetOrCreateMetadata(fileName);

            // Lock file for thread safety
            await metadata.FileLock.WaitAsync();
            try
            {
                // Update cache
                metadata.Data.SetValue(key, serializedValue);
                metadata.IsDirty = true;
                metadata.IsLoaded = true;

                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log($"Queued save '{key}' = '{serializedValue}' to '{fileName}' (cache)");
            }
            finally
            {
                metadata.FileLock.Release();
            }

            // Queue async write with delay (batches multiple saves)
            QueueAsyncWrite(fileName);
        }

        /// <summary>
        /// Queues an async write operation with a 100ms delay to batch saves
        /// </summary>
        private void QueueAsyncWrite(string fileName)
        {
            var metadata = GetOrCreateMetadata(fileName);

            // Cancel existing pending task if any
            if (metadata.PendingTask != null && !metadata.PendingTask.IsCompleted)
            {
                // Task is already queued, let it handle the write
                return;
            }

            // Queue new task
            metadata.PendingTask = Task.Run(async () =>
            {
                // Wait 100ms to batch multiple saves
                await Task.Delay(100);

                await metadata.FileLock.WaitAsync();
                try
                {
                    if (metadata.IsDirty)
                    {
                        SaveFileHandler.WriteFile(fileName, metadata.Data);
                        metadata.IsDirty = false;

                        if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                            Debug.Log($"Flushed async write for '{fileName}'");
                    }
                }
                finally
                {
                    metadata.FileLock.Release();
                }
            });
        }

        /// <summary>
        /// Forces all pending saves to complete
        /// </summary>
        public async Task ForceSaveAllAsync()
        {
            List<Task> tasks = new List<Task>();

            foreach (var kvp in _cache)
            {
                var metadata = kvp.Value;
                
                if (metadata.IsDirty)
                {
                    var fileName = kvp.Key;
                    tasks.Add(Task.Run(async () =>
                    {
                        await metadata.FileLock.WaitAsync();
                        try
                        {
                            if (metadata.IsDirty)
                            {
                                SaveFileHandler.WriteFile(fileName, metadata.Data);
                                metadata.IsDirty = false;

                                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                                    Debug.Log($"Force flushed '{fileName}'");
                            }
                        }
                        finally
                        {
                            metadata.FileLock.Release();
                        }
                    }));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Flushes a specific file to disk (synchronous)
        /// </summary>
        private void FlushFile(string fileName, SaveFileMetadata metadata)
        {
            if (!metadata.IsDirty)
                return;

            SaveFileHandler.WriteFile(fileName, metadata.Data);
            metadata.IsDirty = false;

            if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                Debug.Log($"Flushed '{fileName}' to disk");
        }

        #endregion

        #region Load Operations

        /// <summary>
        /// Synchronous load - reads from cache or disk
        /// </summary>
        public T Load<T>(string key, string fileName, T defaultValue)
        {
            fileName = NormalizeFileName(fileName);
            var metadata = GetOrCreateMetadata(fileName);

            metadata.FileLock.Wait();
            try
            {
                // Load from disk if not yet loaded
                if (!metadata.IsLoaded)
                {
                    metadata.Data = SaveFileHandler.ReadFile(fileName);
                    metadata.IsLoaded = true;

                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Loaded '{fileName}' from disk");
                }

                // Get value from cache
                if (metadata.Data.TryGetValue(key, out string value))
                {
                    return SaveParsers.Deserialize<T>(value);
                }

                return defaultValue;
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        /// <summary>
        /// Asynchronous load - reads from cache or disk
        /// </summary>
        public async Task<T> LoadAsync<T>(string key, string fileName, T defaultValue)
        {
            fileName = NormalizeFileName(fileName);
            var metadata = GetOrCreateMetadata(fileName);

            await metadata.FileLock.WaitAsync();
            try
            {
                // Load from disk if not yet loaded
                if (!metadata.IsLoaded)
                {
                    metadata.Data = await Task.Run(() => SaveFileHandler.ReadFile(fileName));
                    metadata.IsLoaded = true;

                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Loaded '{fileName}' from disk (async)");
                }

                // Get value from cache
                if (metadata.Data.TryGetValue(key, out string value))
                {
                    return SaveParsers.Deserialize<T>(value);
                }

                return defaultValue;
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        /// <summary>
        /// Loads all key-value pairs from a file asynchronously
        /// </summary>
        public async Task<Dictionary<string, string>> LoadAllAsync(string fileName)
        {
            fileName = NormalizeFileName(fileName);
            var metadata = GetOrCreateMetadata(fileName);

            await metadata.FileLock.WaitAsync();
            try
            {
                // Load from disk if not yet loaded
                if (!metadata.IsLoaded)
                {
                    metadata.Data = await Task.Run(() => SaveFileHandler.ReadFile(fileName));
                    metadata.IsLoaded = true;

                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Loaded '{fileName}' from disk (async)");
                }

                // Build dictionary from all entries
                var result = new Dictionary<string, string>();
                foreach (var entry in metadata.Data.data)
                {
                    result[entry.key] = entry.value;
                }

                return result;
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        #endregion

        #region Modification Operations

        /// <summary>
        /// Deletes a key from a save file
        /// </summary>
        public void DeleteKey(string key, string fileName)
        {
            fileName = NormalizeFileName(fileName);
            var metadata = GetOrCreateMetadata(fileName);

            metadata.FileLock.Wait();
            try
            {
                if (!metadata.IsLoaded)
                {
                    metadata.Data = SaveFileHandler.ReadFile(fileName);
                    metadata.IsLoaded = true;
                }

                if (metadata.Data.RemoveKey(key))
                {
                    metadata.IsDirty = true;
                    FlushFile(fileName, metadata);

                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Deleted key '{key}' from '{fileName}'");
                }
                else
                {
                    Debug.LogWarning($"Key '{key}' not found in '{fileName}'");
                }
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        /// <summary>
        /// Deletes a key from a save file (asynchronous)
        /// </summary>
        public async Task DeleteKeyAsync(string key, string fileName)
        {
            fileName = NormalizeFileName(fileName);
            var metadata = GetOrCreateMetadata(fileName);

            await metadata.FileLock.WaitAsync();
            try
            {
                if (!metadata.IsLoaded)
                {
                    metadata.Data = SaveFileHandler.ReadFile(fileName);
                    metadata.IsLoaded = true;
                }

                if (metadata.Data.RemoveKey(key))
                {
                    metadata.IsDirty = true;

                    if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                        Debug.Log($"Queued delete key '{key}' from '{fileName}'");

                    // Queue async write with delay (batches multiple deletes)
                    QueueAsyncWrite(fileName);
                }
                else
                {
                    Debug.LogWarning($"Key '{key}' not found in '{fileName}'");
                }
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        /// <summary>
        /// Deletes an entire save file
        /// </summary>
        public void DeleteFile(string fileName)
        {
            fileName = NormalizeFileName(fileName);

            // Remove from cache
            if (_cache.TryRemove(fileName, out var metadata))
            {
                metadata.FileLock.Dispose();
            }

            // Delete from disk
            SaveFileHandler.DeleteFile(fileName);

            if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                Debug.Log($"Deleted file '{fileName}'");
        }

        /// <summary>
        /// Checks if a key exists in a save file
        /// </summary>
        public bool KeyExists(string key, string fileName)
        {
            fileName = NormalizeFileName(fileName);
            var metadata = GetOrCreateMetadata(fileName);

            metadata.FileLock.Wait();
            try
            {
                if (!metadata.IsLoaded)
                {
                    metadata.Data = SaveFileHandler.ReadFile(fileName);
                    metadata.IsLoaded = true;
                }

                return metadata.Data.ContainsKey(key);
            }
            finally
            {
                metadata.FileLock.Release();
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        public bool FileExists(string fileName)
        {
            fileName = NormalizeFileName(fileName);
            return SaveFileHandler.FileExists(fileName);
        }

        /// <summary>
        /// Wipes all save data
        /// </summary>
        public void WipeAllData()
        {
            // Clear cache
            foreach (var metadata in _cache.Values)
            {
                metadata.FileLock.Dispose();
            }
            _cache.Clear();

            // Delete all files with the save extension
            string savePath = Application.persistentDataPath;
            string directory = ConfigReader.GetConfig().SavingSystem.SaveDirectoryPath;
            
            if (!string.IsNullOrEmpty(directory))
            {
                savePath = System.IO.Path.Combine(savePath, directory);
            }

            if (System.IO.Directory.Exists(savePath))
            {
                string extension = ConfigReader.GetConfig().SavingSystem.SaveFileExtension;
                string[] files = System.IO.Directory.GetFiles(savePath, "*" + extension, System.IO.SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                }

                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log($"Wiped all save data ({files.Length} files)");
            }
        }

        /// <summary>
        /// Clears the cache for a specific file
        /// </summary>
        public void ClearCache(string fileName)
        {
            fileName = NormalizeFileName(fileName);
            
            if (_cache.TryRemove(fileName, out var metadata))
            {
                metadata.FileLock.Dispose();
                
                if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                    Debug.Log($"Cleared cache for '{fileName}'");
            }
        }

        /// <summary>
        /// Clears all caches
        /// </summary>
        public void ClearAllCaches()
        {
            foreach (var metadata in _cache.Values)
            {
                metadata.FileLock.Dispose();
            }
            _cache.Clear();

            if (ConfigReader.GetConfig().SavingSystem.LogSaving)
                Debug.Log("Cleared all caches");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets or creates metadata for a file
        /// </summary>
        private SaveFileMetadata GetOrCreateMetadata(string fileName)
        {
            return _cache.GetOrAdd(fileName, _ => new SaveFileMetadata());
        }

        /// <summary>
        /// Gets or creates metadata for a file (public version for legacy API support)
        /// </summary>
        internal SaveFileMetadata GetOrCreateMetadataPublic(string fileName)
        {
            var metadata = GetOrCreateMetadata(fileName);
            
            // Ensure file is loaded
            if (!metadata.IsLoaded)
            {
                metadata.FileLock.Wait();
                try
                {
                    if (!metadata.IsLoaded)
                    {
                        metadata.Data = SaveFileHandler.ReadFile(fileName);
                        metadata.IsLoaded = true;
                    }
                }
                finally
                {
                    metadata.FileLock.Release();
                }
            }
            
            return metadata;
        }

        /// <summary>
        /// Normalizes file name by removing extension if present
        /// </summary>
        private string NormalizeFileName(string fileName)
        {
            string extension = ConfigReader.GetConfig().SavingSystem.SaveFileExtension;

            if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - extension.Length);
            }

            return fileName;
        }

        /// <summary>
        /// Checks if there are any pending saves
        /// </summary>
        public bool HasPendingSaves()
        {
            foreach (var metadata in _cache.Values)
            {
                if (metadata.IsDirty)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the number of files with pending saves
        /// </summary>
        public int GetPendingSaveCount()
        {
            int count = 0;
            foreach (var metadata in _cache.Values)
            {
                if (metadata.IsDirty)
                    count++;
            }
            return count;
        }

        #endregion
    }

    /// <summary>
    /// Metadata for a cached save file
    /// </summary>
    public class SaveFileMetadata
    {
        /// <summary>
        /// The actual save data
        /// </summary>
        public SaveFileData Data { get; set; } = new SaveFileData();

        /// <summary>
        /// Whether the file has been loaded from disk
        /// </summary>
        public bool IsLoaded { get; set; } = false;

        /// <summary>
        /// Whether the file has unsaved changes
        /// </summary>
        public bool IsDirty { get; set; } = false;

        /// <summary>
        /// Pending async write task
        /// </summary>
        public Task PendingTask { get; set; } = null;

        /// <summary>
        /// File lock for thread safety
        /// </summary>
        public SemaphoreSlim FileLock { get; } = new SemaphoreSlim(1, 1);
    }
}
