using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using AHAKuo.Signalia.GameSystems.Localization.External;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
    /// <summary>
    /// A ScriptableObject that stores localization data and mirrors the structure of a JSON localization file.
    /// This asset can be created from a source JSON file or built manually in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "New LocBook", menuName = "Signalia/Game Systems/Localization/LocBook", order = 1)]
    public class LocBook : ScriptableObject
    {
    [Serializable]
    public class Page
    {
        [Tooltip("Unique identifier for this page")]
        public string pageId;
        
        [Tooltip("Description of this page")]
        [TextArea(1, 3)]
        public string aboutPage;
        
        [Tooltip("Localization entries in this page")]
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }
    
    [Serializable]
    public class AudioPage
    {
        [Tooltip("Unique identifier for this page")]
        public string pageId;
        
        [Tooltip("Description of this page")]
        [TextArea(1, 3)]
        public string aboutPage;
        
        [Tooltip("Audio clip entries in this page")]
        public List<AudioEntry> audioEntries = new List<AudioEntry>();
    }
    
    [Serializable]
    public class ImagePage
    {
        [Tooltip("Unique identifier for this page")]
        public string pageId;
        
        [Tooltip("Description of this page")]
        [TextArea(1, 3)]
        public string aboutPage;
        
        [Tooltip("Sprite/Image entries in this page")]
        public List<SpriteEntry> spriteEntries = new List<SpriteEntry>();
    }
    
    [Serializable]
    public class AssetPage
    {
        [Tooltip("Unique identifier for this page")]
        public string pageId;
        
        [Tooltip("Description of this page")]
        [TextArea(1, 3)]
        public string aboutPage;
        
        [Tooltip("Asset entries in this page (generic Unity Objects)")]
        public List<AssetEntry> assetEntries = new List<AssetEntry>();
    }
        
        [Serializable]
        public class LocalizationEntry
        {
            [Tooltip("The unique key used to retrieve this localization entry")]
            public string key;
            
            [Tooltip("The original value in the source language")]
            [TextArea(2, 4)]
            public string originalValue;
            
            [Tooltip("Translations for different languages")]
            public List<LanguageVariant> variants = new List<LanguageVariant>();
            
            [Tooltip("Alternative keys that can be used to find this entry (only works when Hybrid Key mode is enabled)")]
            public List<string> aliases = new List<string>();
        }
        
    [Serializable]
    public class LanguageVariant
    {
        [Tooltip("Language code (e.g., 'en', 'es', 'fr', 'ar')")]
        public string languageCode;
        
        [Tooltip("The translated text for this language")]
        [TextArea(2, 4)]
        public string value;
    }
    
    [Serializable]
    public class AudioEntry
    {
        [Tooltip("The unique key used to retrieve this audio clip")]
        public string key;
        
        [Tooltip("Audio clip variants for different languages")]
        public List<AudioVariant> variants = new List<AudioVariant>();
    }
    
    [Serializable]
    public class AudioVariant
    {
        [Tooltip("Language code (e.g., 'en', 'es', 'fr', 'ar')")]
        public string languageCode;
        
        [Tooltip("The audio clip for this language")]
        public AudioClip audioClip;
    }
    
    [Serializable]
    public class SpriteEntry
    {
        [Tooltip("The unique key used to retrieve this sprite")]
        public string key;
        
        [Tooltip("Sprite variants for different languages")]
        public List<SpriteVariant> variants = new List<SpriteVariant>();
    }
    
    [Serializable]
    public class SpriteVariant
    {
        [Tooltip("Language code (e.g., 'en', 'es', 'fr', 'ar')")]
        public string languageCode;
        
        [Tooltip("The sprite for this language")]
        public Sprite sprite;
    }
    
    [Serializable]
    public class AssetEntry
    {
        [Tooltip("The unique key used to retrieve this asset")]
        public string key;
        
        [Tooltip("Asset variants for different languages (can be any Unity Object)")]
        public List<AssetVariant> variants = new List<AssetVariant>();
    }
    
    [Serializable]
    public class AssetVariant
    {
        [Tooltip("Language code (e.g., 'en', 'es', 'fr', 'ar')")]
        public string languageCode;
        
        [Tooltip("The asset for this language (can be any Unity Object type)")]
        public UnityEngine.Object asset;
    }
        
        [SerializeField]
        [Tooltip("Reference to the external .locbook file (JSON format) - Mainly for text entries")]
        private UnityEngine.Object locbookFile;
        
        [SerializeField]
        [Tooltip("Text pages in this LocBook (readonly in Unity, edit in Lingramia)")]
        private List<Page> pages = new List<Page>();
        
        [SerializeField]
        [Tooltip("Audio pages in this LocBook (managed in Unity)")]
        private List<AudioPage> audioPages = new List<AudioPage>();
        
        [SerializeField]
        [Tooltip("Image pages in this LocBook (managed in Unity)")]
        private List<ImagePage> imagePages = new List<ImagePage>();
        
        [SerializeField]
        [Tooltip("Asset pages in this LocBook (managed in Unity)")]
        private List<AssetPage> assetPages = new List<AssetPage>();
        
        /// <summary>
        /// Gets or sets the reference to the external .locbook file.
        /// </summary>
        public UnityEngine.Object LocbookFile 
        { 
            get => locbookFile; 
            set => locbookFile = value; 
        }
        
        /// <summary>
        /// Gets all text pages in this LocBook.
        /// </summary>
        public List<Page> Pages => pages;
        
        /// <summary>
        /// Gets all audio pages in this LocBook.
        /// </summary>
        public List<AudioPage> AudioPages => audioPages;
        
        /// <summary>
        /// Gets all image pages in this LocBook.
        /// </summary>
        public List<ImagePage> ImagePages => imagePages;
        
        /// <summary>
        /// Gets all asset pages in this LocBook.
        /// </summary>
        public List<AssetPage> AssetPages => assetPages;
        
        /// <summary>
        /// Gets all localization entries from all pages (flattened for backward compatibility).
        /// </summary>
        public List<LocalizationEntry> Entries
        {
            get
            {
                var allEntries = new List<LocalizationEntry>();
                foreach (var page in pages)
                {
                    allEntries.AddRange(page.entries);
                }
                return allEntries;
            }
        }
        
        /// <summary>
        /// Generates JSON data from this LocBook's entries in the internal format.
        /// This JSON can be used to initialize the Localization system.
        /// Flattens all pages into a single list of entries.
        /// </summary>
        /// <returns>JSON string representation of all entries in internal format</returns>
        public string GetJsonData()
        {
            var data = new LocBookJsonData { entries = Entries };
            return JsonUtility.ToJson(data, true);
        }
        
        /// <summary>
        /// Generates JSON data in the External format compatible with Lingramia.
        /// Preserves the page structure from the internal format.
        /// </summary>
        /// <returns>JSON string representation in External/Lingramia format</returns>
        public string GetExternalJsonData()
        {
            var externalPages = new List<ExternalPage>();
            
            foreach (var page in pages)
            {
                var externalPage = new ExternalPage
                {
                    aboutPage = page.aboutPage,
                    pageId = page.pageId,
                    pageFiles = new List<ExternalPageFile>()
                };
                
                foreach (var entry in page.entries)
                {
                    var pageFile = new ExternalPageFile
                    {
                        key = entry.key,
                        originalValue = entry.originalValue,
                        variants = new List<ExternalVariant>(),
                        aliases = entry.aliases != null ? new List<string>(entry.aliases) : new List<string>()
                    };
                    
                    foreach (var variant in entry.variants)
                    {
                        pageFile.variants.Add(new ExternalVariant
                        {
                            language = variant.languageCode,
                            _value = variant.value
                        });
                    }
                    
                    externalPage.pageFiles.Add(pageFile);
                }
                
                externalPages.Add(externalPage);
            }
            
            var externalData = new ExternalLocBookData
            {
                pages = externalPages
            };
            
            return JsonUtility.ToJson(externalData, true);
        }
        
        /// <summary>
        /// Loads entries from a list of ExternalPageFile objects (used during import).
        /// Creates a single page containing all the entries.
        /// Only affects text pages - audio/image/asset pages are managed separately.
        /// </summary>
        /// <param name="pageFiles">List of page files to import</param>
        public void LoadFromJson(List<ExternalPageFile> pageFiles)
        {
            if (pageFiles == null || pageFiles.Count == 0)
            {
                Debug.LogError("[Signalia LocBook] Cannot load from null or empty page files.");
                return;
            }
            
            pages.Clear();
            var newPage = new Page
            {
                pageId = "ImportedPage",
                aboutPage = "Imported from legacy format",
                entries = new List<LocalizationEntry>()
            };
            
            int entryIndex = 0;
            
            foreach (var pageFile in pageFiles)
            {
                // Ensure aliases is properly initialized from JSON deserialization
                // Unity's JsonUtility may not properly deserialize arrays, so we need to handle this carefully
                List<string> aliasesList = new List<string>();
                if (pageFile.aliases != null)
                {
                    // Create a new list and copy all aliases, filtering out null/empty values
                    foreach (var alias in pageFile.aliases)
                    {
                        if (!string.IsNullOrEmpty(alias))
                        {
                            aliasesList.Add(alias);
                        }
                    }
                }
                
                var entry = new LocalizationEntry
                {
                    key = !string.IsNullOrEmpty(pageFile.key) ? pageFile.key : GenerateKeyFromString(pageFile.originalValue, entryIndex),
                    originalValue = pageFile.originalValue,
                    variants = new List<LanguageVariant>(),
                    aliases = aliasesList
                };
                
                if (pageFile.variants != null)
                {
                    foreach (var variant in pageFile.variants)
                    {
                        entry.variants.Add(new LanguageVariant
                        {
                            languageCode = variant.language,
                            value = variant._value
                        });
                    }
                }
                
                newPage.entries.Add(entry);
                entryIndex++;
            }
            
            pages.Add(newPage);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            
            Debug.Log($"[Signalia LocBook] Loaded {newPage.entries.Count} entries from External page files.");
        }
        
        /// <summary>
        /// Updates the asset by deserializing the referenced .locbook file.
        /// This is the primary way to load data into the LocBook asset.
        /// </summary>
        public void UpdateAssetFromFile()
        {
#if UNITY_EDITOR
            if (locbookFile == null)
            {
                Debug.LogError("[Signalia LocBook] No .locbook file reference set. Please assign a .locbook file first.");
                return;
            }
            
            string path = UnityEditor.AssetDatabase.GetAssetPath(locbookFile);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Signalia LocBook] Could not get path for referenced .locbook file.");
                return;
            }
            
            try
            {
                string json = System.IO.File.ReadAllText(path);
                LoadFromJson(json);
                Debug.Log($"[Signalia LocBook] Updated asset from file: {path}");

                // send sigs event for localization data updated
                if (Application.isPlaying)
                    SIGS.Send(ConfigReader.GetConfig().LocalizationSystem.LanguageChangedEvent);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Signalia LocBook] Error reading .locbook file: {e.Message}");
            }
#endif
        }
        
        /// <summary>
        /// Loads data from a JSON string and populates this LocBook.
        /// Supports both Internal LocBook format and External Localization format.
        /// Only affects text pages - audio/image/asset pages are managed separately.
        /// </summary>
        /// <param name="json">The JSON string to load from</param>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[Signalia LocBook] Cannot load from null or empty JSON.");
                return;
            }
            
            try
            {
                // Try Internal format first (flat entries - convert to page structure)
                var internalData = JsonUtility.FromJson<LocBookJsonData>(json);
                
                if (internalData != null && internalData.entries != null && internalData.entries.Count > 0)
                {
                    // Internal format detected - wrap entries in a single page
                    // Ensure aliases is initialized for backward compatibility with older LocBook formats
                    foreach (var entry in internalData.entries)
                    {
                        if (entry.aliases == null)
                        {
                            entry.aliases = new List<string>();
                        }
                    }
                    
                    pages.Clear();
                    var page = new Page
                    {
                        pageId = "InternalFormat",
                        aboutPage = "Converted from internal format",
                        entries = internalData.entries
                    };
                    pages.Add(page);
                    
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                    
                    Debug.Log($"[Signalia LocBook] Loaded {internalData.entries.Count} entries from Internal JSON format.");
                    return;
                }
                
                // Try External format (New structure: pages containing entries)
                var externalData = JsonUtility.FromJson<ExternalLocBookData>(json);
                
                if (externalData != null && externalData.pages != null && externalData.pages.Count > 0)
                {
                    // External format detected - convert to internal page structure
                    pages.Clear();
                    
                    foreach (var externalPage in externalData.pages)
                    {
                        if (externalPage.pageFiles == null) continue;
                        
                        var page = new Page
                        {
                            pageId = externalPage.pageId,
                            aboutPage = externalPage.aboutPage,
                            entries = new List<LocalizationEntry>()
                        };
                        
                        int entryIndex = 0;
                        
                        foreach (var pageFile in externalPage.pageFiles)
                        {
                            // Ensure aliases is properly initialized from JSON deserialization
                            // Unity's JsonUtility may not properly deserialize arrays in nested structures
                            List<string> aliasesList = new List<string>();
                            if (pageFile.aliases != null)
                            {
                                // Create a new list and copy all aliases, filtering out null/empty values
                                foreach (var alias in pageFile.aliases)
                                {
                                    if (!string.IsNullOrEmpty(alias))
                                    {
                                        aliasesList.Add(alias);
                                    }
                                }
                            }
                            
                            var entry = new LocalizationEntry
                            {
                                // Use key from pageFile if available, otherwise generate
                                key = !string.IsNullOrEmpty(pageFile.key) ? pageFile.key : GenerateKeyFromString(pageFile.originalValue, entryIndex),
                                originalValue = pageFile.originalValue,
                                variants = new List<LanguageVariant>(),
                                aliases = aliasesList
                            };
                            
                            if (pageFile.variants != null)
                            {
                                foreach (var variant in pageFile.variants)
                                {
                                    entry.variants.Add(new LanguageVariant
                                    {
                                        languageCode = variant.language,
                                        value = variant._value
                                    });
                                }
                            }
                            
                            page.entries.Add(entry);
                            entryIndex++;
                        }
                        
                        pages.Add(page);
                    }
                    
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                    
                    int totalEntries = pages.Sum(p => p.entries.Count);
                    Debug.Log($"[Signalia LocBook] Loaded {totalEntries} entries from External JSON format across {pages.Count} pages.");
                    return;
                }
                
                Debug.LogError("[Signalia LocBook] Failed to parse JSON data. Unrecognized format or empty data.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Signalia LocBook] Error loading from JSON: {e.Message}\\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Generates a clean key from a string by converting to lowercase and replacing spaces/special chars.
        /// </summary>
        private string GenerateKeyFromString(string input, int fallbackIndex)
        {
            if (string.IsNullOrEmpty(input))
            {
                return $"entry_{fallbackIndex}";
            }
            
            // Convert to lowercase, replace spaces and special characters
            string key = input.ToLower()
                .Replace(" ", "_")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace(":", "")
                .Replace(";", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("(", "")
                .Replace(")", "");
            
            // Limit length
            if (key.Length > 50)
            {
                key = key.Substring(0, 50);
            }
            
            // Ensure uniqueness by checking existing keys across all pages
            string baseKey = key;
            int counter = 1;
            var allEntries = Entries;
            while (allEntries.Exists(e => e.key == key))
            {
                key = $"{baseKey}_{counter}";
                counter++;
            }
            
            return key;
        }
        
        /// <summary>
        /// Adds a new localization entry.
        /// Note: Entries should be edited in Lingramia. This method is for backward compatibility.
        /// </summary>
        /// <param name="key">The localization key</param>
        /// <param name="originalValue">The original value</param>
        public void AddEntry(string key, string originalValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[Signalia LocBook] Cannot add entry with null or empty key.");
                return;
            }
            
            // Check if key already exists across all pages
            if (Entries.Exists(e => e.key == key))
            {
                Debug.LogWarning($"[Signalia LocBook] Entry with key '{key}' already exists.");
                return;
            }
            
            // Ensure at least one page exists
            if (pages.Count == 0)
            {
                pages.Add(new Page
                {
                    pageId = "DefaultPage",
                    aboutPage = "Auto-created default page",
                    entries = new List<LocalizationEntry>()
                });
            }
            
            // Add to the first page
            pages[0].entries.Add(new LocalizationEntry
            {
                key = key,
                originalValue = originalValue,
                variants = new List<LanguageVariant>()
            });
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /// <summary>
        /// Adds a new localization entry to a specific page by pageId.
        /// Creates the page if it doesn't exist.
        /// </summary>
        /// <param name="pageId">The page ID to add the entry to</param>
        /// <param name="key">The localization key</param>
        /// <param name="originalValue">The original value</param>
        /// <returns>True if the entry was added successfully</returns>
        public bool AddEntryToPage(string pageId, string key, string originalValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[Signalia LocBook] Cannot add entry with null or empty key.");
                return false;
            }
            
            // Check if key already exists across all pages
            if (Entries.Exists(e => e.key == key))
            {
                Debug.LogWarning($"[Signalia LocBook] Entry with key '{key}' already exists.");
                return false;
            }
            
            // Find or create the page
            Page targetPage = pages.FirstOrDefault(p => p.pageId == pageId);
            if (targetPage == null)
            {
                targetPage = new Page
                {
                    pageId = pageId,
                    aboutPage = $"Auto-created page: {pageId}",
                    entries = new List<LocalizationEntry>()
                };
                pages.Add(targetPage);
            }
            
            // Add the entry to the page
            targetPage.entries.Add(new LocalizationEntry
            {
                key = key,
                originalValue = originalValue,
                variants = new List<LanguageVariant>()
            });
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            
            return true;
        }
        
        /// <summary>
        /// Gets a page by its pageId, or creates it if it doesn't exist.
        /// </summary>
        /// <param name="pageId">The page ID to find or create</param>
        /// <returns>The page with the specified ID</returns>
        public Page GetOrCreatePage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                pageId = "DefaultPage";
            }
            
            Page page = pages.FirstOrDefault(p => p.pageId == pageId);
            if (page == null)
            {
                page = new Page
                {
                    pageId = pageId,
                    aboutPage = $"Auto-created page: {pageId}",
                    entries = new List<LocalizationEntry>()
                };
                pages.Add(page);
                
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            
            return page;
        }
        
        /// <summary>
        /// Removes a localization entry by key.
        /// Note: Entries should be edited in Lingramia. This method is for backward compatibility.
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns>True if the entry was removed</returns>
        public bool RemoveEntry(string key)
        {
            int removed = 0;
            
            // Search through all pages and remove matching entries
            foreach (var page in pages)
            {
                removed += page.entries.RemoveAll(e => e.key == key);
            }
            
#if UNITY_EDITOR
            if (removed > 0)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
            
            return removed > 0;
        }
        
        /// <summary>
        /// Gets all unique language codes from all entries.
        /// </summary>
        /// <returns>List of language codes</returns>
        public List<string> GetAllLanguageCodes()
        {
            HashSet<string> codes = new HashSet<string>();

            var entries = Entries;
            
            foreach (var entry in entries)
            {
                foreach (var variant in entry.variants)
                {
                    if (!string.IsNullOrEmpty(variant.languageCode))
                    {
                        codes.Add(variant.languageCode);
                    }
                }
            }
            
            return new List<string>(codes);
        }
        
        /// <summary>
        /// Data structure for JSON serialization that matches LocBook's internal structure.
        /// </summary>
        [Serializable]
        private class LocBookJsonData
        {
            public List<LocalizationEntry> entries;
        }
        
        /// <summary>
        /// Data structures for External Localization format (from Lingramia app).
        /// In the new format, pages represent entries within a single LocBook, not different assets.
        /// </summary>
        [Serializable]
        public class ExternalLocBookData
        {
            public List<ExternalPage> pages;
        }
        
        [Serializable]
        public class ExternalPage
        {
            public string aboutPage;
            public string pageId;
            public List<ExternalPageFile> pageFiles;
        }
        
        [Serializable]
        public class ExternalPageFile
        {
            public string key;
            public string originalValue;
            public List<ExternalVariant> variants;
            public List<string> aliases;
        }
        
        [Serializable]
        public class ExternalVariant
        {
            public string _value;
            public string language;
        }
        
        /// <summary>
        /// Gets the number of pages in this LocBook.
        /// </summary>
        public int PageCount => pages.Count;
        
        /// <summary>
        /// Gets the total number of entries across all pages in this LocBook.
        /// </summary>
        public int EntryCount => Entries.Count;
    }
}
