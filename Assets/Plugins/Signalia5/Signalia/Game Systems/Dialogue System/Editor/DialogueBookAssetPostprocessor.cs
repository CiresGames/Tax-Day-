using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.DialogueSystem;
using System.Linq;
using System.IO;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    /// <summary>
    /// Handles .dlgbook file lifecycle:
    /// 1. Right-click context menu to create DialogueBook from .dlgbook
    /// 2. Auto-creates DialogueBook ScriptableObjects when new .dlgbook files are imported
    /// 3. Auto-updates existing DialogueBook assets when their referenced .dlgbook files change
    /// </summary>
    public class DialogueBookAssetPostprocessor : AssetPostprocessor
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

            var dlgbookPaths = importedAssets.Where(path => path.EndsWith(".dlgbook")).ToArray();
            
            if (dlgbookPaths.Length == 0)
                return;

            // Update any existing DialogueBook assets that reference the changed files
            UpdateExistingDialogueBooks(dlgbookPaths);
            
            // Auto-create DialogueBook assets for .dlgbook files that have no connected asset yet
            AutoCreateDialogueBooks(dlgbookPaths);
        }
        
        /// <summary>
        /// Finds all DialogueBook assets that reference one of the changed .dlgbook files and updates them.
        /// </summary>
        private static void UpdateExistingDialogueBooks(string[] dlgbookPaths)
        {
            string[] dialogueBookGuids = AssetDatabase.FindAssets("t:DialogueBook");
            
            foreach (string guid in dialogueBookGuids)
            {
                string dialogueBookPath = AssetDatabase.GUIDToAssetPath(guid);
                DialogueBook dialogueBook = AssetDatabase.LoadAssetAtPath<DialogueBook>(dialogueBookPath);
                
                if (dialogueBook == null || dialogueBook.DlgbookFile == null)
                    continue;
                
                string dlgbookFilePath = AssetDatabase.GetAssetPath(dialogueBook.DlgbookFile);
                
                if (string.IsNullOrEmpty(dlgbookFilePath))
                    continue;
                
                if (dlgbookPaths.Contains(dlgbookFilePath))
                {
                    Debug.Log($"[Signalia DialogueBook] Auto-updating '{dialogueBook.name}' from {dlgbookFilePath}");
                    
                    try
                    {
                        dialogueBook.UpdateAssetFromFile();
                        EditorUtility.SetDirty(dialogueBook);
                        AssetDatabase.SaveAssets();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[Signalia DialogueBook] Failed to auto-update {dialogueBook.name}: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// For any .dlgbook file that does not yet have a DialogueBook ScriptableObject alongside it,
        /// automatically create one, link them, and import the data.
        /// </summary>
        private static void AutoCreateDialogueBooks(string[] dlgbookPaths)
        {
            // Build a set of all .dlgbook paths already referenced by an existing DialogueBook
            var alreadyLinked = new System.Collections.Generic.HashSet<string>();
            string[] dialogueBookGuids = AssetDatabase.FindAssets("t:DialogueBook");
            
            foreach (string guid in dialogueBookGuids)
            {
                string dbPath = AssetDatabase.GUIDToAssetPath(guid);
                DialogueBook db = AssetDatabase.LoadAssetAtPath<DialogueBook>(dbPath);
                if (db != null && db.DlgbookFile != null)
                {
                    string linked = AssetDatabase.GetAssetPath(db.DlgbookFile);
                    if (!string.IsNullOrEmpty(linked))
                        alreadyLinked.Add(linked);
                }
            }
            
            foreach (string dlgbookPath in dlgbookPaths)
            {
                if (alreadyLinked.Contains(dlgbookPath))
                    continue;
                
                // Check if a .asset already exists beside it with the same name
                string dir = Path.GetDirectoryName(dlgbookPath);
                string nameNoExt = Path.GetFileNameWithoutExtension(dlgbookPath);
                string assetPath = Path.Combine(dir, nameNoExt + ".asset").Replace("\\", "/");
                
                if (AssetDatabase.LoadAssetAtPath<DialogueBook>(assetPath) != null)
                    continue; // already exists
                
                CreateDialogueBookFromDlgbook(dlgbookPath, assetPath);
            }
        }
        
        /// <summary>
        /// Creates a DialogueBook ScriptableObject from a .dlgbook file, links them, and imports.
        /// </summary>
        public static DialogueBook CreateDialogueBookFromDlgbook(string dlgbookPath, string assetPath = null)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                string dir = Path.GetDirectoryName(dlgbookPath);
                string nameNoExt = Path.GetFileNameWithoutExtension(dlgbookPath);
                assetPath = Path.Combine(dir, nameNoExt + ".asset").Replace("\\", "/");
            }
            
            // Ensure unique path
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            try
            {
                string json = File.ReadAllText(dlgbookPath);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[Signalia DialogueBook] .dlgbook file is empty: {dlgbookPath}");
                    return null;
                }
                
                // Create the ScriptableObject
                DialogueBook dialogueBook = ScriptableObject.CreateInstance<DialogueBook>();
                AssetDatabase.CreateAsset(dialogueBook, assetPath);
                
                // Link the .dlgbook file
                Object dlgbookAsset = AssetDatabase.LoadAssetAtPath<Object>(dlgbookPath);
                dialogueBook.DlgbookFile = dlgbookAsset;
                
                // Import the data
                dialogueBook.LoadFromJson(json);
                
                EditorUtility.SetDirty(dialogueBook);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[Signalia DialogueBook] Created '{Path.GetFileName(assetPath)}' from '{Path.GetFileName(dlgbookPath)}'");
                return dialogueBook;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Signalia DialogueBook] Failed to create DialogueBook from {dlgbookPath}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Right-click context menu: Create DialogueBook from selected .dlgbook file(s).
        /// </summary>
        [MenuItem("Assets/Signalia/Create Dialogue Book from .dlgbook", false, 50)]
        private static void CreateDialogueBookFromSelection()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".dlgbook"))
                {
                    CreateDialogueBookFromDlgbook(path);
                }
            }
        }
        
        /// <summary>
        /// Only show the menu item when a .dlgbook file is selected.
        /// </summary>
        [MenuItem("Assets/Signalia/Create Dialogue Book from .dlgbook", true)]
        private static bool CreateDialogueBookFromSelectionValidation()
        {
            return Selection.objects.Any(obj =>
            {
                string path = AssetDatabase.GetAssetPath(obj);
                return !string.IsNullOrEmpty(path) && path.EndsWith(".dlgbook");
            });
        }
    }
}
