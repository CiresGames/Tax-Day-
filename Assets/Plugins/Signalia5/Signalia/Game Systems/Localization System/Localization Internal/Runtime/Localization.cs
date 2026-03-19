using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
    /// <summary>
    /// Core localization system that handles string key retrieval and dictionary management.
    /// This system provides a simple, performant way to access localized strings without any MonoBehaviour dependencies.
    /// Initialize with Localization.Initialize(pathToLocBookJson) and retrieve strings with Localization.ReadKey(key).
    /// </summary>
    public static class Localization
    {
        private static Dictionary<string, LocalizationEntry> localizationDictionary = new Dictionary<string, LocalizationEntry>();
        private static Dictionary<string, AudioEntry> audioDictionary = new Dictionary<string, AudioEntry>();
        private static Dictionary<string, SpriteEntry> spriteDictionary = new Dictionary<string, SpriteEntry>();
        private static Dictionary<string, AssetEntry> assetDictionary = new Dictionary<string, AssetEntry>();
        private static bool isInitialized = false;
        
        /// <summary>
        /// Contains all language variants for a single localization key.
        /// </summary>
        [System.Serializable]
        public class LocalizationEntry
        {
            public string key;
            public string originalValue;
            public List<LanguageVariant> variants = new List<LanguageVariant>();
            public List<string> aliases = new List<string>();
        }
        
        /// <summary>
        /// A single language variant with its language code and translated value.
        /// </summary>
        [System.Serializable]
        public class LanguageVariant
        {
            public string languageCode;
            public string value;
        }
        
        /// <summary>
        /// An audio entry with variants for different languages.
        /// </summary>
        [System.Serializable]
        public class AudioEntry
        {
            public string key;
            public List<AudioVariant> variants = new List<AudioVariant>();
        }
        
        [System.Serializable]
        public class AudioVariant
        {
            public string languageCode;
            public AudioClip audioClip;
        }
        
        /// <summary>
        /// A sprite entry with variants for different languages.
        /// </summary>
        [System.Serializable]
        public class SpriteEntry
        {
            public string key;
            public List<SpriteVariant> variants = new List<SpriteVariant>();
        }
        
        [System.Serializable]
        public class SpriteVariant
        {
            public string languageCode;
            public Sprite sprite;
        }
        
        /// <summary>
        /// An asset entry with variants for different languages.
        /// </summary>
        [System.Serializable]
        public class AssetEntry
        {
            public string key;
            public List<AssetVariant> variants = new List<AssetVariant>();
        }
        
        [System.Serializable]
        public class AssetVariant
        {
            public string languageCode;
            public UnityEngine.Object asset;
        }
        
        /// <summary>
        /// Root structure for the localization JSON file.
        /// </summary>
        [System.Serializable]
        private class LocalizationData
        {
            public List<LocalizationEntry> entries = new List<LocalizationEntry>();
        }
        
        /// <summary>
        /// Initializes the localization system from a JSON file path.
        /// This should be called once at game startup before using any localization features.
        /// </summary>
        /// <param name="pathToLocBookJson">Path to the localization JSON file (e.g., from a LocBook asset)</param>
        public static void Initialize(string pathToLocBookJson)
        {
            if (string.IsNullOrEmpty(pathToLocBookJson))
            {
                Debug.LogError("[Signalia Localization] Cannot initialize: path to LocBook JSON is null or empty.");
                return;
            }
            
            try
            {
                string json = File.ReadAllText(pathToLocBookJson);
                LocalizationData data = JsonUtility.FromJson<LocalizationData>(json);
                
                if (data == null || data.entries == null)
                {
                    Debug.LogError("[Signalia Localization] Failed to parse localization JSON from: " + pathToLocBookJson);
                    return;
                }
                
                localizationDictionary.Clear();
                
                foreach (var entry in data.entries)
                {
                    if (!string.IsNullOrEmpty(entry.key))
                    {
                        // Ensure aliases is initialized for backward compatibility with older LocBook formats
                        if (entry.aliases == null)
                        {
                            entry.aliases = new List<string>();
                        }
                        localizationDictionary[entry.key] = entry;
                    }
                }
                
                isInitialized = true;
                
                if (RuntimeValues.Debugging.IsDebugging)
                {
                    Debug.Log($"[Signalia Localization] Initialized with {localizationDictionary.Count} entries from: {pathToLocBookJson}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia Localization] Error initializing from {pathToLocBookJson}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Initializes the localization system from a LocBook ScriptableObject asset.
        /// This is the recommended initialization method as it handles JSON generation automatically.
        /// </summary>
        /// <param name="locBook"> archived - kept for backward compatibility</param>
        [System.Obsolete("Use Initialize(LocBook[]) instead")]
        public static void Initialize(LocBook locBook)
        {
            if (locBook == null)
            {
                Debug.LogError("[Signalia Localization] Cannot initialize: LocBook is null.");
                return;
            }
            
            string json = locBook.GetJsonData();
            InitializeFromJson(json);
        }

        /// <summary>
        /// Initializes the localization system from an array of LocBook ScriptableObject assets.
        /// All entries from all LocBooks are merged into a single dictionary.
        /// </summary>
        /// <param name="locBooks">Array of LocBook assets to initialize from</param>
        public static void Initialize(LocBook[] locBooks)
        {
            if (locBooks == null || locBooks.Length == 0)
            {
                Debug.LogError("[Signalia Localization] Cannot initialize: LocBooks array is null or empty.");
                return;
            }
            
            localizationDictionary.Clear();
            audioDictionary.Clear();
            spriteDictionary.Clear();
            assetDictionary.Clear();
            
            int totalEntries = 0;
            int totalAudioEntries = 0;
            int totalSpriteEntries = 0;
            int totalAssetEntries = 0;
            
            foreach (var locBook in locBooks)
            {
                if (locBook == null) continue;
                
                try
                {
                    // Load text entries
                    string json = locBook.GetJsonData();
                    LocalizationData data = JsonUtility.FromJson<LocalizationData>(json);
                    
                    if (data != null && data.entries != null)
                    {
                        foreach (var entry in data.entries)
                        {
                            if (!string.IsNullOrEmpty(entry.key))
                            {
                                // Ensure aliases is initialized for backward compatibility with older LocBook formats
                                if (entry.aliases == null)
                                {
                                    entry.aliases = new List<string>();
                                }
                                
                                if (localizationDictionary.ContainsKey(entry.key))
                                {
                                    if (RuntimeValues.Debugging.IsDebugging)
                                    {
                                        Debug.LogWarning($"[Signalia Localization] Duplicate key '{entry.key}' found in LocBook '{locBook.name}'. Skipping.");
                                    }
                                }
                                else
                                {
                                    localizationDictionary[entry.key] = entry;
                                    totalEntries++;
                                }
                            }
                        }
                    }
                    
                    // Load audio entries from audio pages
                    foreach (var audioPage in locBook.AudioPages)
                    {
                        if (audioPage.audioEntries != null)
                        {
                            foreach (var audioEntry in audioPage.audioEntries)
                            {
                                if (!string.IsNullOrEmpty(audioEntry.key))
                                {
                                    if (!audioDictionary.ContainsKey(audioEntry.key))
                                    {
                                        // Create runtime audio entry
                                        var runtimeEntry = new AudioEntry
                                        {
                                            key = audioEntry.key,
                                            variants = new List<AudioVariant>()
                                        };
                                        
                                        foreach (var variant in audioEntry.variants)
                                        {
                                            runtimeEntry.variants.Add(new AudioVariant
                                            {
                                                languageCode = variant.languageCode,
                                                audioClip = variant.audioClip
                                            });
                                        }
                                        
                                        audioDictionary[audioEntry.key] = runtimeEntry;
                                        totalAudioEntries++;
                                    }
                                }
                            }
                        }
                    }
                    
                    // Load sprite entries from image pages
                    foreach (var imagePage in locBook.ImagePages)
                    {
                        if (imagePage.spriteEntries != null)
                        {
                            foreach (var spriteEntry in imagePage.spriteEntries)
                            {
                                if (!string.IsNullOrEmpty(spriteEntry.key))
                                {
                                    if (!spriteDictionary.ContainsKey(spriteEntry.key))
                                    {
                                        // Create runtime sprite entry
                                        var runtimeEntry = new SpriteEntry
                                        {
                                            key = spriteEntry.key,
                                            variants = new List<SpriteVariant>()
                                        };
                                        
                                        foreach (var variant in spriteEntry.variants)
                                        {
                                            runtimeEntry.variants.Add(new SpriteVariant
                                            {
                                                languageCode = variant.languageCode,
                                                sprite = variant.sprite
                                            });
                                        }
                                        
                                        spriteDictionary[spriteEntry.key] = runtimeEntry;
                                        totalSpriteEntries++;
                                    }
                                }
                            }
                        }
                    }
                    
                    // Load asset entries from asset pages
                    foreach (var assetPage in locBook.AssetPages)
                    {
                        if (assetPage.assetEntries != null)
                        {
                            foreach (var assetEntry in assetPage.assetEntries)
                            {
                                if (!string.IsNullOrEmpty(assetEntry.key))
                                {
                                    if (!assetDictionary.ContainsKey(assetEntry.key))
                                    {
                                        // Create runtime asset entry
                                        var runtimeEntry = new AssetEntry
                                        {
                                            key = assetEntry.key,
                                            variants = new List<AssetVariant>()
                                        };
                                        
                                        foreach (var variant in assetEntry.variants)
                                        {
                                            runtimeEntry.variants.Add(new AssetVariant
                                            {
                                                languageCode = variant.languageCode,
                                                asset = variant.asset
                                            });
                                        }
                                        
                                        assetDictionary[assetEntry.key] = runtimeEntry;
                                        totalAssetEntries++;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Signalia Localization] Error loading LocBook '{locBook.name}': {e.Message}");
                }
            }
            
            isInitialized = true;
            
            LocalizationRuntime.Initialize();
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.Log($"[Signalia Localization] Initialized with {totalEntries} text entries, {totalAudioEntries} audio entries, {totalSpriteEntries} sprite entries, and {totalAssetEntries} asset entries from {locBooks.Length} LocBook(s).");
            }
        }
        
        /// <summary>
        /// Initializes the localization system directly from JSON string data.
        /// </summary>
        /// <param name="json">The JSON string containing localization data</param>
        public static void InitializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[Signalia Localization] Cannot initialize: JSON is null or empty.");
                return;
            }
            
            try
            {
                LocalizationData data = JsonUtility.FromJson<LocalizationData>(json);
                
                if (data == null || data.entries == null)
                {
                    Debug.LogError("[Signalia Localization] Failed to parse localization JSON.");
                    return;
                }
                
                localizationDictionary.Clear();
                
                foreach (var entry in data.entries)
                {
                    if (!string.IsNullOrEmpty(entry.key))
                    {
                        // Ensure aliases is initialized for backward compatibility with older LocBook formats
                        if (entry.aliases == null)
                        {
                            entry.aliases = new List<string>();
                        }
                        localizationDictionary[entry.key] = entry;
                    }
                }
                
                isInitialized = true;
                
                if (RuntimeValues.Debugging.IsDebugging)
                {
                    Debug.Log($"[Signalia Localization] Initialized with {localizationDictionary.Count} entries from JSON data.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia Localization] Error initializing from JSON: {e.Message}");
            }
        }
        
        /// <summary>
        /// Retrieves a localized string by its key for the current language.
        /// </summary>
        /// <param name="key">The localization key</param>
        /// <returns>The localized string, or the key itself if not found</returns>
        public static string ReadKey(string key)
        {
            return ReadKey(key, LocalizationRuntime.CurrentLanguageCode);
        }
        
        /// <summary>
        /// Retrieves a localized string by its key for a specific language.
        /// This method applies formatting (e.g., Arabic shaping) automatically.
        /// </summary>
        /// <param name="key">The localization key</param>
        /// <param name="languageCode">The language code to retrieve (e.g., "en", "es", "fr")</param>
        /// <returns>The localized string with formatting applied, or fallback value if not found</returns>
        public static string ReadKey(string key, string languageCode, string paragraphStyle = "")
        {
            string rawText = ReadKeyRaw(key, languageCode);
            return ApplyFormatting(rawText, languageCode, paragraphStyle);
        }
        
        /// <summary>
        /// Retrieves a localized string by its key without applying any formatting.
        /// Use this when you need to apply string formatting (e.g., string.Format) before language-specific formatting.
        /// </summary>
        /// <param name="key">The localization key</param>
        /// <returns>The raw localized string without formatting, or the key itself if not found</returns>
        public static string ReadKeyRaw(string key)
        {
            return ReadKeyRaw(key, LocalizationRuntime.CurrentLanguageCode);
        }
        
        /// <summary>
        /// Retrieves a localized string by its key for a specific language without applying any formatting.
        /// Use this when you need to apply string formatting (e.g., string.Format) before language-specific formatting.
        /// </summary>
        /// <param name="key">The localization key</param>
        /// <param name="languageCode">The language code to retrieve (e.g., "en", "es", "fr")</param>
        /// <returns>The raw localized string without formatting, or fallback value if not found</returns>
        public static string ReadKeyRaw(string key, string languageCode)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Signalia Localization] System not initialized. Call Initialize() first.");
                return key;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            
            // Try to find the entry by key
            if (localizationDictionary.TryGetValue(key, out LocalizationEntry entry))
            {
                // Find the variant for the requested language
                var variant = entry.variants.Find(v => v.languageCode == languageCode);
                
                if (variant != null && !string.IsNullOrEmpty(variant.value))
                {
                    return variant.value;
                }

                // Fallback to original value
                if (!string.IsNullOrEmpty(entry.originalValue))
                {
                    return entry.originalValue;
                }
            }

            // If hybrid key mode is enabled, search by original value, variant values, and aliases
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            if (config != null && config.LocalizationSystem.HybridKey)
            {
                foreach (var kvp in localizationDictionary)
                {
                    var searchEntry = kvp.Value;
                    
                    // Check if the key matches the original value
                    if (searchEntry.originalValue == key)
                    {
                        var variant = searchEntry.variants.Find(v => v.languageCode == languageCode);
                        
                        if (variant != null && !string.IsNullOrEmpty(variant.value))
                        {
                            return variant.value;
                        }

                        return searchEntry.originalValue;
                    }

                    // Check if the key matches any variant value
                    foreach (var variant in searchEntry.variants)
                    {
                        if (variant.value == key)
                        {
                            var targetVariant = searchEntry.variants.Find(v => v.languageCode == languageCode);

                            if (targetVariant != null && !string.IsNullOrEmpty(targetVariant.value))
                            {
                                return targetVariant.value;
                            }

                            return searchEntry.originalValue;
                        }
                    }
                    
                    // Check if the key matches any alias
                    if (searchEntry.aliases != null && searchEntry.aliases.Count > 0)
                    {
                        foreach (var alias in searchEntry.aliases)
                        {
                            if (!string.IsNullOrEmpty(alias) && alias == key)
                            {
                                var targetVariant = searchEntry.variants.Find(v => v.languageCode == languageCode);

                                if (targetVariant != null && !string.IsNullOrEmpty(targetVariant.value))
                                {
                                    return targetVariant.value;
                                }

                                return searchEntry.originalValue;
                            }
                        }
                    }
                }
            }

            // Ultimate fallback: return the key itself
            return key;
        }
        
        /// <summary>
        /// Applies language-specific formatting (such as Arabic shaping) to a text string.
        /// Uses the current language code if none is specified.
        /// </summary>
        /// <param name="text">The text to format</param>
        /// <param name="languageCode">Optional language code override (uses current language if empty)</param>
        /// <returns>The formatted text, or the original text if no formatting is required</returns>
        public static string ApplyFormatting(string text, string languageCode = "", string paragraphStyle = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            
            string targetLanguage = string.IsNullOrEmpty(languageCode) ? LocalizationRuntime.CurrentLanguageCode : languageCode;
            
            return LocalizationRuntime.FormatForLanguage(text, targetLanguage, paragraphStyle);
        }
        
        /// <summary>
        /// Checks if a localization key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists</returns>
        public static bool HasKey(string key)
        {
            return isInitialized && localizationDictionary.ContainsKey(key);
        }
        
        /// <summary>
        /// Gets all available language codes from the loaded localization data.
        /// </summary>
        /// <returns>List of language codes</returns>
        public static List<string> GetAvailableLanguages()
        {
            if (!isInitialized)
            {
                return new List<string>();
            }
            
            HashSet<string> languages = new HashSet<string>();
            
            foreach (var entry in localizationDictionary.Values)
            {
                foreach (var variant in entry.variants)
                {
                    if (!string.IsNullOrEmpty(variant.languageCode))
                    {
                        languages.Add(variant.languageCode);
                    }
                }
            }
            
            return new List<string>(languages);
        }
        
        /// <summary>
        /// Retrieves a localized audio clip by its key for the current language.
        /// </summary>
        /// <param name="key">The audio key</param>
        /// <returns>The localized audio clip, or null if not found</returns>
        public static AudioClip ReadAudioClip(string key)
        {
            return ReadAudioClip(key, LocalizationRuntime.CurrentLanguageCode);
        }
        
        /// <summary>
        /// Retrieves a localized audio clip by its key for a specific language.
        /// </summary>
        /// <param name="key">The audio key</param>
        /// <param name="languageCode">The language code to retrieve</param>
        /// <returns>The localized audio clip, or null if not found</returns>
        public static AudioClip ReadAudioClip(string key, string languageCode)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Signalia Localization] System not initialized. Call Initialize() first.");
                return null;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            
            if (audioDictionary.TryGetValue(key, out AudioEntry entry))
            {
                var variant = entry.variants.Find(v => v.languageCode == languageCode);
                
                if (variant != null && variant.audioClip != null)
                {
                    return variant.audioClip;
                }
                
                // Fallback to first available variant if current language not found
                if (entry.variants.Count > 0 && entry.variants[0].audioClip != null)
                {
                    return entry.variants[0].audioClip;
                }
            }
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.LogWarning($"[Signalia Localization] Audio clip not found for key '{key}' and language '{languageCode}'.");
            }
            
            return null;
        }
        
        /// <summary>
        /// Retrieves a localized sprite by its key for the current language.
        /// </summary>
        /// <param name="key">The sprite key</param>
        /// <returns>The localized sprite, or null if not found</returns>
        public static Sprite ReadSprite(string key)
        {
            return ReadSprite(key, LocalizationRuntime.CurrentLanguageCode);
        }
        
        /// <summary>
        /// Retrieves a localized sprite by its key for a specific language.
        /// </summary>
        /// <param name="key">The sprite key</param>
        /// <param name="languageCode">The language code to retrieve</param>
        /// <returns>The localized sprite, or null if not found</returns>
        public static Sprite ReadSprite(string key, string languageCode)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Signalia Localization] System not initialized. Call Initialize() first.");
                return null;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            
            if (spriteDictionary.TryGetValue(key, out SpriteEntry entry))
            {
                var variant = entry.variants.Find(v => v.languageCode == languageCode);
                
                if (variant != null && variant.sprite != null)
                {
                    return variant.sprite;
                }
                
                // Fallback to first available variant if current language not found
                if (entry.variants.Count > 0 && entry.variants[0].sprite != null)
                {
                    return entry.variants[0].sprite;
                }
            }
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.LogWarning($"[Signalia Localization] Sprite not found for key '{key}' and language '{languageCode}'.");
            }
            
            return null;
        }
        
        /// <summary>
        /// Retrieves a localized asset by its key for the current language.
        /// </summary>
        /// <param name="key">The asset key</param>
        /// <returns>The localized asset, or null if not found</returns>
        public static T ReadAsset<T>(string key) where T : UnityEngine.Object
        {
            return ReadAsset<T>(key, LocalizationRuntime.CurrentLanguageCode);
        }
        
        /// <summary>
        /// Retrieves a localized asset by its key for a specific language.
        /// </summary>
        /// <param name="key">The asset key</param>
        /// <param name="languageCode">The language code to retrieve</param>
        /// <returns>The localized asset, or null if not found</returns>
        public static T ReadAsset<T>(string key, string languageCode) where T : UnityEngine.Object
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Signalia Localization] System not initialized. Call Initialize() first.");
                return null;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            
            if (assetDictionary.TryGetValue(key, out AssetEntry entry))
            {
                var variant = entry.variants.Find(v => v.languageCode == languageCode);
                
                if (variant != null && variant.asset != null)
                {
                    return variant.asset as T;
                }
                
                // Fallback to first available variant if current language not found
                if (entry.variants.Count > 0 && entry.variants[0].asset != null)
                {
                    return entry.variants[0].asset as T;
                }
            }
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.LogWarning($"[Signalia Localization] Asset not found for key '{key}' and language '{languageCode}'.");
            }
            
            return null;
        }
        
        /// <summary>
        /// Checks if an audio key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists</returns>
        public static bool HasAudioKey(string key)
        {
            return isInitialized && audioDictionary.ContainsKey(key);
        }
        
        /// <summary>
        /// Checks if a sprite key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists</returns>
        public static bool HasSpriteKey(string key)
        {
            return isInitialized && spriteDictionary.ContainsKey(key);
        }
        
        /// <summary>
        /// Checks if an asset key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists</returns>
        public static bool HasAssetKey(string key)
        {
            return isInitialized && assetDictionary.ContainsKey(key);
        }
        
        /// <summary>
        /// Clears all localization data and resets the system.
        /// </summary>
        public static void Clear()
        {
            localizationDictionary.Clear();
            audioDictionary.Clear();
            spriteDictionary.Clear();
            assetDictionary.Clear();
            isInitialized = false;
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.Log("[Signalia Localization] System cleared.");
            }
        }
        
        /// <summary>
        /// Returns whether the system has been initialized.
        /// </summary>
        public static bool IsInitialized => isInitialized;
        
        /// <summary>
        /// Gets the total number of localization entries loaded.
        /// </summary>
        public static int EntryCount => localizationDictionary.Count;
    }
}
