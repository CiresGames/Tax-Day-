#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.Framework;
using System.Linq;
using System.IO;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
    /// <summary>
    /// Handles .locbook file lifecycle:
    /// 1. Right-click context menu to create LocBook from .locbook
    /// 2. Auto-creates LocBook ScriptableObjects when new .locbook files are imported
    /// 3. Auto-updates existing LocBook assets when their referenced .locbook files change (when enabled in config)
    /// </summary>
    public class LocBookAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (Application.isBatchMode)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
#if UNITY_2020_1_OR_NEWER
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;
#endif

            var locbookPaths = importedAssets.Where(path => path.EndsWith(".locbook")).ToArray();
            
            if (locbookPaths.Length == 0)
                return;

            // Auto-create LocBook assets for .locbook files that have no connected asset yet
            AutoCreateLocBooks(locbookPaths);

            // Update existing LocBook assets (only when auto-update is enabled in config)
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            if (config == null || !config.LocalizationSystem.AutoUpdateLocbooks)
                return;

            UpdateExistingLocBooks(locbookPaths, config);
        }
        
        /// <summary>
        /// For any .locbook file that does not yet have a LocBook ScriptableObject alongside it,
        /// automatically create one, link them, and import the data.
        /// </summary>
        private static void AutoCreateLocBooks(string[] locbookPaths)
        {
            var alreadyLinked = new System.Collections.Generic.HashSet<string>();
            string[] locBookGuids = AssetDatabase.FindAssets("t:LocBook");
            
            foreach (string guid in locBookGuids)
            {
                string lbPath = AssetDatabase.GUIDToAssetPath(guid);
                LocBook lb = AssetDatabase.LoadAssetAtPath<LocBook>(lbPath);
                if (lb != null && lb.LocbookFile != null)
                {
                    string linked = AssetDatabase.GetAssetPath(lb.LocbookFile);
                    if (!string.IsNullOrEmpty(linked))
                        alreadyLinked.Add(linked);
                }
            }
            
            foreach (string locbookPath in locbookPaths)
            {
                if (alreadyLinked.Contains(locbookPath))
                    continue;
                
                string dir = Path.GetDirectoryName(locbookPath);
                string nameNoExt = Path.GetFileNameWithoutExtension(locbookPath);
                string assetPath = Path.Combine(dir, nameNoExt + ".asset").Replace("\\", "/");
                
                if (AssetDatabase.LoadAssetAtPath<LocBook>(assetPath) != null)
                    continue;
                
                CreateLocBookFromLocbook(locbookPath, assetPath);
            }
        }
        
        /// <summary>
        /// Finds all LocBook assets that reference one of the changed .locbook files and updates them.
        /// </summary>
        private static void UpdateExistingLocBooks(string[] locbookPaths, SignaliaConfigAsset config)
        {
            string[] locBookGuids = AssetDatabase.FindAssets("t:LocBook");
            
            foreach (string guid in locBookGuids)
            {
                string locBookPath = AssetDatabase.GUIDToAssetPath(guid);
                LocBook locBook = AssetDatabase.LoadAssetAtPath<LocBook>(locBookPath);
                
                if (locBook == null || locBook.LocbookFile == null)
                    continue;
                
                string locbookFilePath = AssetDatabase.GetAssetPath(locBook.LocbookFile);
                
                if (string.IsNullOrEmpty(locbookFilePath))
                    continue;
                
                if (locbookPaths.Contains(locbookFilePath) && HasLocbookFileChanged(locBook, locbookFilePath))
                {
                    Debug.Log($"[Signalia LocBook] Auto-updating {locBook.name} due to changes in {locbookFilePath}");
                    
                    try
                    {
                        locBook.UpdateAssetFromFile();
                        EditorUtility.SetDirty(locBook);
                        AssetDatabase.SaveAssets();
                        
                        if (config.LocalizationSystem.AutoRefreshCacheInRuntime && Application.isPlaying)
                        {
                            Debug.Log($"[Signalia LocBook] Refreshing localization cache in runtime for {locBook.name}");
                            AHAKuo.Signalia.GameSystems.Localization.Internal.Localization.Initialize(config.LocalizationSystem.LocBooks);
                            if (!string.IsNullOrEmpty(config.LocalizationSystem.LanguageChangedEvent))
                                AHAKuo.Signalia.Framework.SIGS.Send(config.LocalizationSystem.LanguageChangedEvent);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[Signalia LocBook] Failed to auto-update {locBook.name}: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a LocBook ScriptableObject from a .locbook file, links them, and imports.
        /// </summary>
        public static LocBook CreateLocBookFromLocbook(string locbookPath, string assetPath = null)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                string dir = Path.GetDirectoryName(locbookPath);
                string nameNoExt = Path.GetFileNameWithoutExtension(locbookPath);
                assetPath = Path.Combine(dir, nameNoExt + ".asset").Replace("\\", "/");
            }
            
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            try
            {
                string json = File.ReadAllText(locbookPath);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[Signalia LocBook] .locbook file is empty: {locbookPath}");
                    return null;
                }
                
                LocBook locBook = ScriptableObject.CreateInstance<LocBook>();
                AssetDatabase.CreateAsset(locBook, assetPath);
                
                locBook.LocbookFile = AssetDatabase.LoadAssetAtPath<Object>(locbookPath);
                locBook.LoadFromJson(json);
                
                EditorUtility.SetDirty(locBook);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[Signalia LocBook] Created '{Path.GetFileName(assetPath)}' from '{Path.GetFileName(locbookPath)}'");
                return locBook;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia LocBook] Failed to create LocBook from {locbookPath}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Right-click context menu: Create LocBook from selected .locbook file(s).
        /// </summary>
        [MenuItem("Assets/Signalia/Create LocBook from .locbook", false, 50)]
        private static void CreateLocBookFromSelection()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".locbook"))
                {
                    CreateLocBookFromLocbook(path);
                }
            }
        }
        
        [MenuItem("Assets/Signalia/Create LocBook from .locbook", true)]
        private static bool CreateLocBookFromSelectionValidation()
        {
            return Selection.objects.Any(obj =>
            {
                string path = AssetDatabase.GetAssetPath(obj);
                return !string.IsNullOrEmpty(path) && path.EndsWith(".locbook");
            });
        }
        
        /// <summary>
        /// Checks if the .locbook file has changed by comparing entry counts and basic structure.
        /// This helps avoid unnecessary updates when the file hasn't actually changed.
        /// </summary>
        private static bool HasLocbookFileChanged(LocBook locBook, string locbookFilePath)
        {
            try
            {
                // Read the file content
                string json = System.IO.File.ReadAllText(locbookFilePath);
                
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }
                
                // Try to parse as External format
                var externalData = JsonUtility.FromJson<LocBook.ExternalLocBookData>(json);
                
                if (externalData != null && externalData.pages != null)
                {
                    // Count entries in the file
                    int fileEntryCount = 0;
                    foreach (var page in externalData.pages)
                    {
                        if (page.pageFiles != null)
                        {
                            fileEntryCount += page.pageFiles.Count;
                        }
                    }
                    
                    // Compare with current entry count
                    int currentEntryCount = locBook.EntryCount;
                    
                    // If counts differ, definitely changed
                    if (fileEntryCount != currentEntryCount)
                    {
                        return true;
                    }
                    
                    // If counts are the same, check page structure
                    if (externalData.pages.Count != locBook.PageCount)
                    {
                        return true;
                    }
                    
                    // Additional check: compare first entry's originalValue if available
                    if (externalData.pages.Count > 0 && 
                        externalData.pages[0].pageFiles != null && 
                        externalData.pages[0].pageFiles.Count > 0 &&
                        locBook.Pages.Count > 0 &&
                        locBook.Pages[0].entries.Count > 0)
                    {
                        string fileFirstValue = externalData.pages[0].pageFiles[0].originalValue;
                        string assetFirstValue = locBook.Pages[0].entries[0].originalValue;
                        
                        if (fileFirstValue != assetFirstValue)
                        {
                            return true;
                        }
                    }
                }
                
                // If we can't determine, assume it changed to be safe
                return true;
            }
            catch (System.Exception)
            {
                // If there's an error parsing, assume it changed
                return true;
            }
        }
    }
}
#endif
