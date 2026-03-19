using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.GameSystems.Localization.External;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using TMPro;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
    /// <summary>
    /// Static helper class for LocBook export functionality.
    /// </summary>
    public static class LocBookExportHelper
    {
        [MenuItem("Assets/Export LocBooks to Lingramia Format", false, 200)]
        private static void ExportSelectedLocBooks()
        {
            var selectedLocBooks = GetSelectedLocBooks();
            
            if (selectedLocBooks.Count == 0)
            {
                EditorUtility.DisplayDialog("No LocBooks Selected", 
                    "Please select one or more LocBook assets in the Project window.", 
                    "OK");
                return;
            }
            
            string fileName = selectedLocBooks.Count == 1 ? selectedLocBooks[0].name : "MultipleLocBooks";
            string path = EditorUtility.SaveFilePanel("Export LocBooks to Lingramia Format", Application.dataPath, fileName, "locbook");
            
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = GenerateLingramiaJson(selectedLocBooks);
                    File.WriteAllText(path, json);
                    
                    EditorUtility.DisplayDialog("Export Complete", 
                        $"Exported {selectedLocBooks.Count} LocBook(s) ({GetTotalEntryCount(selectedLocBooks)} entries) to:\\n{path}", 
                        "OK");
                    
                    Debug.Log($"[Signalia LocBook] Exported {selectedLocBooks.Count} LocBook(s) ({GetTotalEntryCount(selectedLocBooks)} entries) to: {path}");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Export Failed", 
                        $"Failed to export file:\\n{e.Message}", 
                        "OK");
                    Debug.LogError($"[Signalia LocBook] Export error: {e.Message}");
                }
            }
        }
        
        [MenuItem("Assets/Export LocBooks to Lingramia Format", true)]
        private static bool ValidateExportSelectedLocBooks()
        {
            return GetSelectedLocBooks().Count > 0;
        }
        
        [MenuItem("Assets/Open LocBooks in Lingramia", false, 201)]
        private static void OpenSelectedLocBooksInLingramia()
        {
            var selectedLocBooks = GetSelectedLocBooks();
            
            if (selectedLocBooks.Count == 0)
            {
                EditorUtility.DisplayDialog("No LocBooks Selected", 
                    "Please select one or more LocBook assets in the Project window.", 
                    "OK");
                return;
            }
            
            if (!LingramiaDownloader.IsLingramiaDownloaded())
            {
                EditorUtility.DisplayDialog("Lingramia Not Installed", 
                    "Lingramia is not installed. Please download it first using:\n" +
                    "Tools > Signalia > Game Systems > Localization > Download Lingramia", 
                    "OK");
                return;
            }
            
            var validLocBooks = new List<LocBook>();
            var invalidLocBooks = new List<string>();
            
            foreach (var locBook in selectedLocBooks)
            {
                if (locBook.LocbookFile == null)
                {
                    invalidLocBooks.Add(locBook.name);
                    continue;
                }
                
                string path = UnityEditor.AssetDatabase.GetAssetPath(locBook.LocbookFile);
                if (string.IsNullOrEmpty(path))
                {
                    invalidLocBooks.Add(locBook.name);
                    continue;
                }
                
                validLocBooks.Add(locBook);
            }
            
            if (validLocBooks.Count == 0)
            {
                string invalidList = invalidLocBooks.Count > 0 
                    ? $"\n\nLocBooks without valid .locbook file references:\n{string.Join("\n", invalidLocBooks)}"
                    : "";
                EditorUtility.DisplayDialog("No Valid LocBooks", 
                    "None of the selected LocBooks have valid .locbook file references." + invalidList, 
                    "OK");
                return;
            }
            
            int openedCount = 0;
            int failedCount = 0;
            
            foreach (var locBook in validLocBooks)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(locBook.LocbookFile);
                string fullPath = Path.GetFullPath(path);
                
                bool success = LingramiaDownloader.LaunchLingramia(fullPath);
                if (success)
                {
                    openedCount++;
                    Debug.Log($"[Signalia LocBook] Opening in Lingramia: {fullPath}");
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"[Signalia LocBook] Failed to open in Lingramia: {fullPath}");
                }
            }
            
            if (invalidLocBooks.Count > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} LocBook(s) in Lingramia.\n" +
                    (failedCount > 0 ? $"Failed to open {failedCount} LocBook(s).\n" : "") +
                    $"\n{invalidLocBooks.Count} LocBook(s) skipped (no .locbook file reference):\n{string.Join("\n", invalidLocBooks)}", 
                    "OK");
            }
            else if (failedCount > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} LocBook(s) in Lingramia.\n" +
                    $"Failed to open {failedCount} LocBook(s).", 
                    "OK");
            }
            else if (openedCount > 0)
            {
                Debug.Log($"[Signalia LocBook] Successfully opened {openedCount} LocBook(s) in Lingramia.");
            }
        }
        
        [MenuItem("Assets/Open LocBooks in Lingramia", true)]
        private static bool ValidateOpenSelectedLocBooksInLingramia()
        {
            return GetSelectedLocBooks().Count > 0;
        }
        
        private static List<LocBook> GetSelectedLocBooks()
        {
            var locBooks = new List<LocBook>();
            
            foreach (var obj in Selection.objects)
            {
                if (obj is LocBook locBook)
                {
                    locBooks.Add(locBook);
                }
            }
            
            return locBooks;
        }
        
        private static int GetTotalEntryCount(List<LocBook> locBooks)
        {
            int total = 0;
            foreach (var locBook in locBooks)
            {
                total += locBook.EntryCount;
            }
            return total;
        }
        
        private static string GenerateLingramiaJson(List<LocBook> locBooks)
        {
            var pages = new List<LocBook.ExternalPage>();
            
            foreach (var locBook in locBooks)
            {
                var page = new LocBook.ExternalPage
                {
                    aboutPage = "",
                    pageId = locBook.LocalizationPageId(),
                    pageFiles = new List<LocBook.ExternalPageFile>()
                };
                
                foreach (var entry in locBook.Entries)
                {
                    var pageFile = new LocBook.ExternalPageFile
                    {
                        originalValue = entry.originalValue,
                        variants = new List<LocBook.ExternalVariant>()
                    };
                    
                    foreach (var variant in entry.variants)
                    {
                        pageFile.variants.Add(new LocBook.ExternalVariant
                        {
                            language = variant.languageCode,
                            _value = variant.value
                        });
                    }
                    
                    page.pageFiles.Add(pageFile);
                }
                
                pages.Add(page);
            }
            
            var externalData = new LocBook.ExternalLocBookData
            {
                pages = pages
            };
            
            return JsonUtility.ToJson(externalData, true);
        }
    }
    
    /// <summary>
    /// Custom property drawer for FormattingOptions to prevent conflicting selections.
    /// </summary>
    [CustomPropertyDrawer(typeof(FormattingOptions))]
    public class FormattingOptionsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            FormattingOptions currentValue = (FormattingOptions)property.intValue;
            
            // Draw label
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);
            
            // Draw the enum as mask field with conflict prevention
            Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 
                                     position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.BeginChangeCheck();
            FormattingOptions newValue = (FormattingOptions)EditorGUI.EnumFlagsField(fieldRect, currentValue);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Prevent conflicting case transformations
                newValue = PreventCaseConflicts(newValue, currentValue);
                property.intValue = (int)newValue;
            }
            
            // Show warning if conflicts exist
            if (HasCaseConflicts(newValue))
            {
                Rect warningRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, 
                                           position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.HelpBox(warningRect, "⚠️ Multiple case options selected. Priority: AllCaps > TitleCase > LowerCase", MessageType.Warning);
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            FormattingOptions value = (FormattingOptions)property.intValue;
            float height = EditorGUIUtility.singleLineHeight;
            
            // Add space for warning if conflicts exist
            if (HasCaseConflicts(value))
            {
                height += EditorGUIUtility.singleLineHeight + 2;
            }
            
            return height;
        }
        
        private bool HasCaseConflicts(FormattingOptions value)
        {
            int caseCount = 0;
            if ((value & FormattingOptions.AllCaps) != 0) caseCount++;
            if ((value & FormattingOptions.TitleCase) != 0) caseCount++;
            if ((value & FormattingOptions.LowerCase) != 0) caseCount++;
            return caseCount > 1;
        }
        
        private FormattingOptions PreventCaseConflicts(FormattingOptions newValue, FormattingOptions oldValue)
        {
            // Identify which case option was just added (if any)
            FormattingOptions addedFlags = newValue & ~oldValue;
            
            // If a case transformation was added, remove other case transformations
            if ((addedFlags & FormattingOptions.AllCaps) != 0)
            {
                newValue &= ~(FormattingOptions.TitleCase | FormattingOptions.LowerCase);
            }
            else if ((addedFlags & FormattingOptions.TitleCase) != 0)
            {
                newValue &= ~(FormattingOptions.AllCaps | FormattingOptions.LowerCase);
            }
            else if ((addedFlags & FormattingOptions.LowerCase) != 0)
            {
                newValue &= ~(FormattingOptions.AllCaps | FormattingOptions.TitleCase);
            }
            
            return newValue;
        }
    }
    
    /// <summary>
    /// Custom editor for LocBook ScriptableObjects.
    /// Provides a user-friendly interface for managing localization entries.
    /// </summary>
    [CustomEditor(typeof(LocBook))]
    [CanEditMultipleObjects]
    public class LocBookEditor : Editor
    {
        private SerializedProperty pagesProperty;
        private SerializedProperty audioPagesProperty;
        private SerializedProperty imagePagesProperty;
        private SerializedProperty assetPagesProperty;
        private SerializedProperty locbookFileProperty;
        private Vector2 scrollPosition;
        private bool showPages = false;
        private int selectedTab = 0;
        private readonly string[] tabNames = new string[] { "📝 Text", "🎵 Audio", "🖼️ Images", "📦 Assets" };
        
        // Search and pagination state for each tab
        private const int EntriesPerPage = 10;
        private string audioSearchTerm = string.Empty;
        private int audioCurrentPage = 0;
        private List<int> audioFilteredIndices = new List<int>();
        
        private string imageSearchTerm = string.Empty;
        private int imageCurrentPage = 0;
        private List<int> imageFilteredIndices = new List<int>();
        
        private string assetSearchTerm = string.Empty;
        private int assetCurrentPage = 0;
        private List<int> assetFilteredIndices = new List<int>();
        
        private void OnEnable()
        {
            pagesProperty = serializedObject.FindProperty("pages");
            audioPagesProperty = serializedObject.FindProperty("audioPages");
            imagePagesProperty = serializedObject.FindProperty("imagePages");
            assetPagesProperty = serializedObject.FindProperty("assetPages");
            locbookFileProperty = serializedObject.FindProperty("locbookFile");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var locBook = (LocBook)target;
            
            // Header
            //GUILayout.Label(GraphicLoader.LocalizationLocbook, GUILayout.Height(150));
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("LocBook Asset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This asset contains localization data that can be used to initialize the localization system. " +
                                   "Text entries are readonly in Unity and must be edited in Lingramia. " +
                                   "Audio, Image, and Asset entries are managed separately in Unity.", 
                                   MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Content Tabs (Text, Audio, Images, Assets)
            EditorGUILayout.LabelField("Localization Content", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames, 30);
            
            EditorGUILayout.Space(5);
            
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.fontSize = 12;
            
            switch (selectedTab)
            {
                case 0: // Text Tab
                    DrawTextTab(locBook);
                    break;
                    
                case 1: // Audio Tab
                    DrawAudioTab();
                    break;
                    
                case 2: // Images Tab
                    DrawImagesTab();
                    break;
                    
                case 3: // Assets Tab
                    DrawAssetsTab();
                    break;
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Config Management
            DrawConfigManagementSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawTextTab(LocBook locBook)
        {
            // LocBook File Reference
            EditorGUILayout.LabelField(".locbook File Reference (Text Only)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(locbookFileProperty, new GUIContent("LocBook File", "Reference to the external .locbook file (mainly for text entries)"));
            //if (GraphicLoader.LocalizationLingramiaIcon != null)
            //{
            //    GUILayout.Label(GraphicLoader.LocalizationLingramiaIcon, GUILayout.Width(40), GUILayout.Height(40));
            //}
            EditorGUILayout.EndHorizontal();
            
            if (locbookFileProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No .locbook file Selected. Drag and drop a .locbook file here or click the circle button to select one.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Lingramia Integration
            EditorGUILayout.LabelField("Lingramia Integration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Check if Lingramia is installed
            bool lingramiaInstalled = LingramiaDownloader.IsLingramiaDownloaded();
            
            if (!lingramiaInstalled)
            {
                EditorGUILayout.HelpBox("Lingramia is not installed. Download it to use the localization editor.", MessageType.Warning);
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("⬇️ Download & Install Lingramia", GUILayout.Height(35)))
                {
                    LingramiaDownloadWindow.ShowWindow();
                }
                GUI.backgroundColor = Color.white;
            }
            
            // Only show "Generate Locbook" button when no locbook file is referenced
            if (locbookFileProperty.objectReferenceValue == null)
            {
                EditorGUI.BeginDisabledGroup(!lingramiaInstalled);
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("📝 Generate Locbook", GUILayout.Height(35)))
                {
                    GenerateLocbook(locBook);
                }
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Generate Locbook: Creates a new .locbook file from this asset's current text data and assigns it as the reference.", 
                                       MessageType.Info);
            }
            else
            {
                // Show Open in Lingramia button when a locbook file is referenced
                EditorGUI.BeginDisabledGroup(!lingramiaInstalled);
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🚀 Open Locbook Text", GUILayout.Height(35)))
                {
                    // Check if multiple LocBooks are selected
                    if (targets.Length > 1)
                    {
                        OpenMultipleLocBooksInLingramia();
                    }
                    else
                    {
                        OpenInLingramia(locBook);
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space(5);
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("🔄 Update Asset from .locbook File", GUILayout.Height(30)))
                {
                    UpdateAsset(locBook);
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.Space(5);
                
                if (lingramiaInstalled)
                {
                    EditorGUILayout.HelpBox("Open in Lingramia: Launches the Lingramia app with this LocBook's file.\n" +
                                           "Update Asset: Deserializes the .locbook file into this asset's text pages.", 
                                           MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Please download Lingramia to open and edit .locbook files.", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Stats
            EditorGUILayout.LabelField("Text Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Text Pages: {locBook.PageCount}");
            EditorGUILayout.LabelField($"Total Text Entries: {locBook.EntryCount}");
            
            // Get default language code from config
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            string defaultLang = config != null ? config.LocalizationSystem.DefaultStartingLanguageCode : "en";
            
            var languages = locBook.GetAllLanguageCodes();
            string languageDisplay = languages.Count > 0 ? string.Join(", ", languages) : "None";
            EditorGUILayout.LabelField($"Original/Default: {defaultLang}");
            EditorGUILayout.LabelField($"Translations: {languageDisplay}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Text Pages
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.fontSize = 12;
            showPages = EditorGUILayout.Foldout(showPages, $"📚 Text Pages ({pagesProperty.arraySize}) [READONLY]", true, foldoutStyle);
            
            if (!showPages)
            {
                EditorGUILayout.HelpBox($"Text pages are readonly in Unity and editable via Lingramia. Click to expand and view all {pagesProperty.arraySize} pages.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("These pages cannot be edited in Unity. Use Lingramia to edit text localization data.", MessageType.Warning);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
                
                if (pagesProperty.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No pages. Open in Lingramia to add pages.", MessageType.Info);
                }
                else
                {
                    GUI.enabled = false;
                    for (int i = 0; i < pagesProperty.arraySize; i++)
                    {
                        DrawPage(i);
                    }
                    GUI.enabled = true;
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void GenerateLocbook(LocBook locBook)
        {
            // Get the directory where this LocBook asset is located
            string assetPath = AssetDatabase.GetAssetPath(locBook);
            string directory = Path.GetDirectoryName(assetPath);
            
            // Suggest a filename based on the LocBook asset name
            string suggestedFilename = locBook.name + ".locbook";
            
            // Open save file dialog
            string savePath = EditorUtility.SaveFilePanel(
                "Generate .locbook File",
                directory,
                suggestedFilename,
                "locbook"
            );
            
            if (string.IsNullOrEmpty(savePath))
            {
                return; // User cancelled
            }
            
            try
            {
                // Generate JSON from the current LocBook data
                string json = locBook.GetExternalJsonData();
                
                // Write to file
                File.WriteAllText(savePath, json);
                
                // Convert to relative path if inside Assets folder
                string relativePath = savePath;
                if (savePath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + savePath.Substring(Application.dataPath.Length);
                }
                
                // Refresh to import the new file
                AssetDatabase.Refresh();
                
                // Load and assign the reference
                UnityEngine.Object locbookFileRef = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                if (locbookFileRef != null)
                {
                    locBook.LocbookFile = locbookFileRef;
                    EditorUtility.SetDirty(locBook);
                    AssetDatabase.SaveAssets();
                    
                    EditorUtility.DisplayDialog("Locbook Generated", 
                        $"Successfully generated .locbook file at:\n{relativePath}\n\nThe file has been assigned as this LocBook's reference.", 
                        "OK");
                    
                    Debug.Log($"[Signalia LocBook] Generated .locbook file: {relativePath}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Success (Manual Assignment Needed)", 
                        $"Generated .locbook file at:\n{savePath}\n\nPlease manually assign it in the inspector if it's outside the Assets folder.", 
                        "OK");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Generation Failed", 
                    $"Failed to generate .locbook file:\n{e.Message}", 
                    "OK");
                Debug.LogError($"[Signalia LocBook] Generation error: {e.Message}");
            }
        }
        
        private void OpenInLingramia(LocBook locBook)
        {
            if (locBook.LocbookFile == null)
            {
                EditorUtility.DisplayDialog("No .locbook File", 
                    "Please assign a .locbook file reference before opening.", 
                    "OK");
                return;
            }
            
            string path = AssetDatabase.GetAssetPath(locBook.LocbookFile);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Invalid File", 
                    "Could not get path for the referenced .locbook file.", 
                    "OK");
                return;
            }
            
            string fullPath = Path.GetFullPath(path);
            
            // Try to use Lingramia if it's installed
            if (LingramiaDownloader.IsLingramiaDownloaded())
            {
                bool success = LingramiaDownloader.LaunchLingramia(fullPath);
                
                if (success)
                {
                    Debug.Log($"[Signalia LocBook] Opening in Lingramia: {fullPath}");
                    return;
                }
                else
                {
                    // Lingramia is installed but failed to launch - fall through to default app
                    Debug.LogWarning("[Signalia LocBook] Failed to launch Lingramia, falling back to system default app.");
                }
            }
            
            // Fall back to system default app (asks user which app to use)
            try
            {
                ProcessStartInfo startInfo;
                
                // On macOS, use the 'open' command to open files with the default application
                if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{fullPath}\"",
                        UseShellExecute = true
                    };
                }
                else
                {
                    // Windows and Linux can use the file path directly
                    startInfo = new ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    };
                }
                
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Could Not Open File", 
                    $"Could not open the .locbook file with the system default application.\n\nError: {ex.Message}\n\n" +
                    "To use Lingramia (recommended), download it from:\n" +
                    "Tools > Signalia Localization > Download Lingramia", 
                    "OK");
                Debug.LogError($"[Signalia LocBook] Failed to open file with default app: {ex.Message}");
            }
        }
        
        private void OpenMultipleLocBooksInLingramia()
        {
            if (!LingramiaDownloader.IsLingramiaDownloaded())
            {
                EditorUtility.DisplayDialog("Lingramia Not Installed", 
                    "Lingramia is not installed. Please download it first using:\n" +
                    "Tools > Signalia > Game Systems > Localization > Download Lingramia", 
                    "OK");
                return;
            }
            
            var validLocBooks = new List<LocBook>();
            var invalidLocBooks = new List<string>();
            
            // Get all selected LocBooks
            foreach (var target in targets)
            {
                if (target is LocBook locBook)
                {
                    if (locBook.LocbookFile == null)
                    {
                        invalidLocBooks.Add(locBook.name);
                        continue;
                    }
                    
                    string path = AssetDatabase.GetAssetPath(locBook.LocbookFile);
                    if (string.IsNullOrEmpty(path))
                    {
                        invalidLocBooks.Add(locBook.name);
                        continue;
                    }
                    
                    validLocBooks.Add(locBook);
                }
            }
            
            if (validLocBooks.Count == 0)
            {
                string invalidList = invalidLocBooks.Count > 0 
                    ? $"\n\nLocBooks without valid .locbook file references:\n{string.Join("\n", invalidLocBooks)}"
                    : "";
                EditorUtility.DisplayDialog("No Valid LocBooks", 
                    "None of the selected LocBooks have valid .locbook file references." + invalidList, 
                    "OK");
                return;
            }
            
            int openedCount = 0;
            int failedCount = 0;
            
            foreach (var locBook in validLocBooks)
            {
                string path = AssetDatabase.GetAssetPath(locBook.LocbookFile);
                string fullPath = Path.GetFullPath(path);
                
                bool success = LingramiaDownloader.LaunchLingramia(fullPath);
                if (success)
                {
                    openedCount++;
                    Debug.Log($"[Signalia LocBook] Opening in Lingramia: {fullPath}");
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"[Signalia LocBook] Failed to open in Lingramia: {fullPath}");
                }
            }
            
            if (invalidLocBooks.Count > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} LocBook(s) in Lingramia.\n" +
                    (failedCount > 0 ? $"Failed to open {failedCount} LocBook(s).\n" : "") +
                    $"\n{invalidLocBooks.Count} LocBook(s) skipped (no .locbook file reference):\n{string.Join("\n", invalidLocBooks)}", 
                    "OK");
            }
            else if (failedCount > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} LocBook(s) in Lingramia.\n" +
                    $"Failed to open {failedCount} LocBook(s).", 
                    "OK");
            }
            else if (openedCount > 0)
            {
                Debug.Log($"[Signalia LocBook] Successfully opened {openedCount} LocBook(s) in Lingramia.");
            }
        }
        
        private void UpdateAsset(LocBook locBook)
        {
            if (locBook.LocbookFile == null)
            {
                EditorUtility.DisplayDialog("No .locbook File", 
                    "Please assign a .locbook file reference before updating the asset.", 
                    "OK");
                return;
            }
            
            try
            {
                locBook.UpdateAssetFromFile();
                serializedObject.Update();
                EditorUtility.SetDirty(locBook);
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog("Update Complete", 
                    $"Successfully updated asset with {locBook.EntryCount} entries.", 
                    "OK");

                // trigger language change event for localization data updated
                if (Application.isPlaying)
                    SIGS.TriggerLanguageChange();

                Repaint();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Update Failed", 
                    $"Failed to update asset:\n{e.Message}", 
                    "OK");
                Debug.LogError($"[Signalia LocBook] Update error: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void DrawPage(int pageIndex)
        {
            // Safety check
            if (pageIndex < 0 || pageIndex >= pagesProperty.arraySize)
                return;
            
            var page = pagesProperty.GetArrayElementAtIndex(pageIndex);
            var pageIdProp = page.FindPropertyRelative("pageId");
            var aboutPageProp = page.FindPropertyRelative("aboutPage");
            var entriesProp = page.FindPropertyRelative("entries");
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"📄 Page: {pageIdProp.stringValue}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"About: {aboutPageProp.stringValue}", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Entries: {entriesProp.arraySize}", EditorStyles.miniLabel);
            
            // Draw entries for this page
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                DrawEntry(entriesProp, i);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawEntry(SerializedProperty entriesProp, int index)
        {
            // Safety check
            if (index < 0 || index >= entriesProp.arraySize)
                return;
            
            var entry = entriesProp.GetArrayElementAtIndex(index);
            var keyProp = entry.FindPropertyRelative("key");
            var originalValueProp = entry.FindPropertyRelative("originalValue");
            var variantsProp = entry.FindPropertyRelative("variants");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"  Entry {index + 1}", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(keyProp.stringValue))
            {
                EditorGUILayout.TextField("  Key", keyProp.stringValue);
            }
            EditorGUILayout.TextField("  Original", originalValueProp.stringValue);
            
            if (variantsProp.arraySize > 0)
            {
                EditorGUILayout.LabelField($"  Variants: {variantsProp.arraySize}", EditorStyles.miniLabel);
                
                for (int v = 0; v < variantsProp.arraySize; v++)
                {
                    var variant = variantsProp.GetArrayElementAtIndex(v);
                    var langCodeProp = variant.FindPropertyRelative("languageCode");
                    var valueProp = variant.FindPropertyRelative("value");
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"    [{langCodeProp.stringValue}]", GUILayout.Width(60));
                    EditorGUILayout.TextField(valueProp.stringValue);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAudioTab()
        {
            EditorGUILayout.LabelField("🎵 Audio Localization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio clips that vary by language. Each entry has a key and variants for different language codes. This is managed exclusively in Unity.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // Copy Text Pages button
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("📋 Copy Text Pages", GUILayout.Height(30)))
            {
                CopyTextPagesToAudio();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Copy Text Pages: Creates audio pages matching the structure of text pages for convenience.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Add new page button
            if (GUILayout.Button("➕ Add Audio Page", GUILayout.Height(30)))
            {
                audioPagesProperty.arraySize++;
                var newPage = audioPagesProperty.GetArrayElementAtIndex(audioPagesProperty.arraySize - 1);
                newPage.FindPropertyRelative("pageId").stringValue = $"AudioPage_{audioPagesProperty.arraySize}";
                newPage.FindPropertyRelative("aboutPage").stringValue = "";
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(5);
            
            // Search and pagination
            UpdateAudioFilteredIndices();
            DrawAudioSearchToolbar();
            
            int totalFilteredEntries = audioFilteredIndices.Count;
            
            if (totalFilteredEntries > 0)
            {
                DrawAudioPaginationToolbar(totalFilteredEntries);
                EditorGUILayout.Space(5);
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            
            if (audioPagesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No audio pages. Add a page or copy text pages to get started.", MessageType.Info);
            }
            else if (totalFilteredEntries == 0)
            {
                EditorGUILayout.HelpBox("No audio pages match the current search.", MessageType.Info);
            }
            else
            {
                int startIndex = audioCurrentPage * EntriesPerPage;
                int endIndex = Mathf.Min(startIndex + EntriesPerPage, totalFilteredEntries);
                
                for (int displayIndex = startIndex; displayIndex < endIndex; displayIndex++)
                {
                    int actualIndex = audioFilteredIndices[displayIndex];
                    DrawAudioPage(actualIndex);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawAudioSearchToolbar()
        {
            GUIStyle searchFieldStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarSearchField;
            GUIStyle fallbackMiniButton = EditorStyles.miniButton;
            GUIStyle cancelButtonStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? GUI.skin.FindStyle("ToolbarCancelButton") ?? fallbackMiniButton;
            bool usingFallbackCancel = cancelButtonStyle == fallbackMiniButton || cancelButtonStyle.name == fallbackMiniButton.name;
            GUIContent cancelContent = usingFallbackCancel ? new GUIContent("x", "Clear search") : GUIContent.none;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUI.BeginChangeCheck();
            audioSearchTerm = EditorGUILayout.TextField(audioSearchTerm, searchFieldStyle, GUILayout.ExpandWidth(true));
            float cancelWidth = cancelButtonStyle.fixedWidth > 0 ? cancelButtonStyle.fixedWidth : 18f;
            
            if (GUILayout.Button(cancelContent, cancelButtonStyle, GUILayout.Width(cancelWidth)))
            {
                audioSearchTerm = string.Empty;
                GUI.FocusControl(null);
                EditorGUI.EndChangeCheck();
                UpdateAudioFilteredIndices();
                audioCurrentPage = 0;
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                audioCurrentPage = 0;
                UpdateAudioFilteredIndices();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAudioPaginationToolbar(int totalEntries)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));
            audioCurrentPage = Mathf.Clamp(audioCurrentPage, 0, totalPages - 1);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            using (new EditorGUI.DisabledScope(audioCurrentPage <= 0))
            {
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    audioCurrentPage = Mathf.Max(0, audioCurrentPage - 1);
                }
            }
            
            using (new EditorGUI.DisabledScope(audioCurrentPage >= totalPages - 1))
            {
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    audioCurrentPage = Mathf.Min(totalPages - 1, audioCurrentPage + 1);
                }
            }
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Page {audioCurrentPage + 1} of {totalPages} · {totalEntries} {(totalEntries == 1 ? "Page" : "Pages")}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void UpdateAudioFilteredIndices()
        {
            audioFilteredIndices.Clear();
            
            if (audioPagesProperty != null)
            {
                string comparisonTerm = audioSearchTerm ?? string.Empty;
                
                for (int i = 0; i < audioPagesProperty.arraySize; i++)
                {
                    SerializedProperty pageProp = audioPagesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty pageIdProp = pageProp.FindPropertyRelative("pageId");
                    string pageIdValue = pageIdProp != null ? pageIdProp.stringValue : string.Empty;
                    
                    bool matches = string.IsNullOrEmpty(comparisonTerm) || 
                                  (!string.IsNullOrEmpty(pageIdValue) && pageIdValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    if (!matches)
                    {
                        // Also check entries
                        SerializedProperty entriesProp = pageProp.FindPropertyRelative("audioEntries");
                        for (int e = 0; e < entriesProp.arraySize; e++)
                        {
                            SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(e);
                            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
                            string keyValue = keyProp != null ? keyProp.stringValue : string.Empty;
                            
                            if (!string.IsNullOrEmpty(keyValue) && keyValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                    
                    if (matches)
                    {
                        audioFilteredIndices.Add(i);
                    }
                }
            }
            
            int totalPages = audioFilteredIndices.Count == 0 ? 1 : Mathf.CeilToInt(audioFilteredIndices.Count / (float)EntriesPerPage);
            audioCurrentPage = Mathf.Clamp(audioCurrentPage, 0, Mathf.Max(0, totalPages - 1));
        }
        
        private void DrawAudioPage(int pageIndex)
        {
            var page = audioPagesProperty.GetArrayElementAtIndex(pageIndex);
            var pageIdProp = page.FindPropertyRelative("pageId");
            var aboutPageProp = page.FindPropertyRelative("aboutPage");
            var audioEntriesProp = page.FindPropertyRelative("audioEntries");
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📄 Audio Page: {pageIdProp.stringValue}", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ Delete Page", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Delete Audio Page", 
                    $"Are you sure you want to delete this audio page?", "Yes", "No"))
                {
                    audioPagesProperty.DeleteArrayElementAtIndex(pageIndex);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            pageIdProp.stringValue = EditorGUILayout.TextField("Page ID", pageIdProp.stringValue);
            aboutPageProp.stringValue = EditorGUILayout.TextArea(aboutPageProp.stringValue, GUILayout.Height(40));
            
            EditorGUILayout.Space(5);
            
            // Entries foldout
            audioEntriesProp.isExpanded = EditorGUILayout.Foldout(audioEntriesProp.isExpanded, $"Audio Entries ({audioEntriesProp.arraySize})", true);
            
            if (audioEntriesProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Add new audio entry button
                if (GUILayout.Button("➕ Add Audio Entry", GUILayout.Height(25)))
                {
                    audioEntriesProp.arraySize++;
                    var newEntry = audioEntriesProp.GetArrayElementAtIndex(audioEntriesProp.arraySize - 1);
                    newEntry.FindPropertyRelative("key").stringValue = $"audio_key_{audioEntriesProp.arraySize}";
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space(5);
                
                // Draw audio entries
                for (int i = audioEntriesProp.arraySize - 1; i >= 0; i--)
                {
                    DrawAudioEntry(audioEntriesProp, i, pageIndex);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void CopyTextPagesToAudio()
        {
            var locBook = (LocBook)target;
            audioPagesProperty.ClearArray();
            
            foreach (var textPage in locBook.Pages)
            {
                audioPagesProperty.arraySize++;
                var newPage = audioPagesProperty.GetArrayElementAtIndex(audioPagesProperty.arraySize - 1);
                newPage.FindPropertyRelative("pageId").stringValue = textPage.pageId;
                newPage.FindPropertyRelative("aboutPage").stringValue = textPage.aboutPage;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("Copied", $"Copied {locBook.Pages.Count} text page(s) to audio pages.", "OK");
        }
        
        private void DrawAudioEntry(SerializedProperty audioEntriesProp, int index, int pageIndex)
        {
            var entry = audioEntriesProp.GetArrayElementAtIndex(index);
            var keyProp = entry.FindPropertyRelative("key");
            var variantsProp = entry.FindPropertyRelative("variants");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            string entryLabel = string.IsNullOrEmpty(keyProp.stringValue) ? $"🎵 Audio Entry {index + 1}" : $"🎵 {keyProp.stringValue}";
            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, entryLabel, true);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ Delete", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Delete Audio Entry", 
                    $"Are you sure you want to delete this audio entry?", "Yes", "No"))
                {
                    audioEntriesProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            if (entry.isExpanded)
            {
                keyProp.stringValue = EditorGUILayout.TextField("Key", keyProp.stringValue);
                
                EditorGUILayout.Space(5);
                
                // Variants foldout
                variantsProp.isExpanded = EditorGUILayout.Foldout(variantsProp.isExpanded, $"Language Variants ({variantsProp.arraySize})", true);
                
                if (variantsProp.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    
                    // Add variant button
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("➕ Add Language Variant", GUILayout.Height(20)))
                    {
                        variantsProp.arraySize++;
                        var newVariant = variantsProp.GetArrayElementAtIndex(variantsProp.arraySize - 1);
                        newVariant.FindPropertyRelative("languageCode").stringValue = "en";
                        serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(3);
                    
                    // Draw variants
                    for (int v = variantsProp.arraySize - 1; v >= 0; v--)
                    {
                        var variant = variantsProp.GetArrayElementAtIndex(v);
                        var langCodeProp = variant.FindPropertyRelative("languageCode");
                        var audioClipProp = variant.FindPropertyRelative("audioClip");
                        
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.BeginHorizontal();
                        
                        EditorGUILayout.LabelField("Language:", GUILayout.Width(70));
                        langCodeProp.stringValue = EditorGUILayout.TextField(langCodeProp.stringValue, GUILayout.Width(50));
                        
                        GUILayout.FlexibleSpace();
                        
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("✖", GUILayout.Width(30)))
                        {
                            variantsProp.DeleteArrayElementAtIndex(v);
                            serializedObject.ApplyModifiedProperties();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            continue;
                        }
                        GUI.backgroundColor = Color.white;
                        
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUILayout.PropertyField(audioClipProp, new GUIContent("Audio Clip"));
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawImagesTab()
        {
            EditorGUILayout.LabelField("🖼️ Image Localization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sprites/Images that vary by language. Each entry has a key and variants for different language codes. This is managed exclusively in Unity.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // Copy Text Pages button
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("📋 Copy Text Pages", GUILayout.Height(30)))
            {
                CopyTextPagesToImages();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Copy Text Pages: Creates image pages matching the structure of text pages for convenience.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Add new page button
            if (GUILayout.Button("➕ Add Image Page", GUILayout.Height(30)))
            {
                imagePagesProperty.arraySize++;
                var newPage = imagePagesProperty.GetArrayElementAtIndex(imagePagesProperty.arraySize - 1);
                newPage.FindPropertyRelative("pageId").stringValue = $"ImagePage_{imagePagesProperty.arraySize}";
                newPage.FindPropertyRelative("aboutPage").stringValue = "";
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(5);
            
            // Search and pagination
            UpdateImageFilteredIndices();
            DrawImageSearchToolbar();
            
            int totalFilteredEntries = imageFilteredIndices.Count;
            
            if (totalFilteredEntries > 0)
            {
                DrawImagePaginationToolbar(totalFilteredEntries);
                EditorGUILayout.Space(5);
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            
            if (imagePagesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No image pages. Add a page or copy text pages to get started.", MessageType.Info);
            }
            else if (totalFilteredEntries == 0)
            {
                EditorGUILayout.HelpBox("No image pages match the current search.", MessageType.Info);
            }
            else
            {
                int startIndex = imageCurrentPage * EntriesPerPage;
                int endIndex = Mathf.Min(startIndex + EntriesPerPage, totalFilteredEntries);
                
                for (int displayIndex = startIndex; displayIndex < endIndex; displayIndex++)
                {
                    int actualIndex = imageFilteredIndices[displayIndex];
                    DrawImagePage(actualIndex);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawImageSearchToolbar()
        {
            GUIStyle searchFieldStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarSearchField;
            GUIStyle fallbackMiniButton = EditorStyles.miniButton;
            GUIStyle cancelButtonStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? GUI.skin.FindStyle("ToolbarCancelButton") ?? fallbackMiniButton;
            bool usingFallbackCancel = cancelButtonStyle == fallbackMiniButton || cancelButtonStyle.name == fallbackMiniButton.name;
            GUIContent cancelContent = usingFallbackCancel ? new GUIContent("x", "Clear search") : GUIContent.none;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUI.BeginChangeCheck();
            imageSearchTerm = EditorGUILayout.TextField(imageSearchTerm, searchFieldStyle, GUILayout.ExpandWidth(true));
            float cancelWidth = cancelButtonStyle.fixedWidth > 0 ? cancelButtonStyle.fixedWidth : 18f;
            
            if (GUILayout.Button(cancelContent, cancelButtonStyle, GUILayout.Width(cancelWidth)))
            {
                imageSearchTerm = string.Empty;
                GUI.FocusControl(null);
                EditorGUI.EndChangeCheck();
                UpdateImageFilteredIndices();
                imageCurrentPage = 0;
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                imageCurrentPage = 0;
                UpdateImageFilteredIndices();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawImagePaginationToolbar(int totalEntries)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));
            imageCurrentPage = Mathf.Clamp(imageCurrentPage, 0, totalPages - 1);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            using (new EditorGUI.DisabledScope(imageCurrentPage <= 0))
            {
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    imageCurrentPage = Mathf.Max(0, imageCurrentPage - 1);
                }
            }
            
            using (new EditorGUI.DisabledScope(imageCurrentPage >= totalPages - 1))
            {
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    imageCurrentPage = Mathf.Min(totalPages - 1, imageCurrentPage + 1);
                }
            }
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Page {imageCurrentPage + 1} of {totalPages} · {totalEntries} {(totalEntries == 1 ? "Page" : "Pages")}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void UpdateImageFilteredIndices()
        {
            imageFilteredIndices.Clear();
            
            if (imagePagesProperty != null)
            {
                string comparisonTerm = imageSearchTerm ?? string.Empty;
                
                for (int i = 0; i < imagePagesProperty.arraySize; i++)
                {
                    SerializedProperty pageProp = imagePagesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty pageIdProp = pageProp.FindPropertyRelative("pageId");
                    string pageIdValue = pageIdProp != null ? pageIdProp.stringValue : string.Empty;
                    
                    bool matches = string.IsNullOrEmpty(comparisonTerm) || 
                                  (!string.IsNullOrEmpty(pageIdValue) && pageIdValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    if (!matches)
                    {
                        // Also check entries
                        SerializedProperty entriesProp = pageProp.FindPropertyRelative("spriteEntries");
                        for (int e = 0; e < entriesProp.arraySize; e++)
                        {
                            SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(e);
                            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
                            string keyValue = keyProp != null ? keyProp.stringValue : string.Empty;
                            
                            if (!string.IsNullOrEmpty(keyValue) && keyValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                    
                    if (matches)
                    {
                        imageFilteredIndices.Add(i);
                    }
                }
            }
            
            int totalPages = imageFilteredIndices.Count == 0 ? 1 : Mathf.CeilToInt(imageFilteredIndices.Count / (float)EntriesPerPage);
            imageCurrentPage = Mathf.Clamp(imageCurrentPage, 0, Mathf.Max(0, totalPages - 1));
        }
        
        private void DrawImagePage(int pageIndex)
        {
            var page = imagePagesProperty.GetArrayElementAtIndex(pageIndex);
            var pageIdProp = page.FindPropertyRelative("pageId");
            var aboutPageProp = page.FindPropertyRelative("aboutPage");
            var spriteEntriesProp = page.FindPropertyRelative("spriteEntries");
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📄 Image Page: {pageIdProp.stringValue}", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ Delete Page", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Delete Image Page", 
                    $"Are you sure you want to delete this image page?", "Yes", "No"))
                {
                    imagePagesProperty.DeleteArrayElementAtIndex(pageIndex);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            pageIdProp.stringValue = EditorGUILayout.TextField("Page ID", pageIdProp.stringValue);
            aboutPageProp.stringValue = EditorGUILayout.TextArea(aboutPageProp.stringValue, GUILayout.Height(40));
            
            EditorGUILayout.Space(5);
            
            // Entries foldout
            spriteEntriesProp.isExpanded = EditorGUILayout.Foldout(spriteEntriesProp.isExpanded, $"Image Entries ({spriteEntriesProp.arraySize})", true);
            
            if (spriteEntriesProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Add new sprite entry button
                if (GUILayout.Button("➕ Add Image Entry", GUILayout.Height(25)))
                {
                    spriteEntriesProp.arraySize++;
                    var newEntry = spriteEntriesProp.GetArrayElementAtIndex(spriteEntriesProp.arraySize - 1);
                    newEntry.FindPropertyRelative("key").stringValue = $"sprite_key_{spriteEntriesProp.arraySize}";
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space(5);
                
                // Draw sprite entries
                for (int i = spriteEntriesProp.arraySize - 1; i >= 0; i--)
                {
                    DrawSpriteEntry(spriteEntriesProp, i, pageIndex);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void CopyTextPagesToImages()
        {
            var locBook = (LocBook)target;
            imagePagesProperty.ClearArray();
            
            foreach (var textPage in locBook.Pages)
            {
                imagePagesProperty.arraySize++;
                var newPage = imagePagesProperty.GetArrayElementAtIndex(imagePagesProperty.arraySize - 1);
                newPage.FindPropertyRelative("pageId").stringValue = textPage.pageId;
                newPage.FindPropertyRelative("aboutPage").stringValue = textPage.aboutPage;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("Copied", $"Copied {locBook.Pages.Count} text page(s) to image pages.", "OK");
        }
        
        private void DrawSpriteEntry(SerializedProperty spriteEntriesProp, int index, int pageIndex)
        {
            var entry = spriteEntriesProp.GetArrayElementAtIndex(index);
            var keyProp = entry.FindPropertyRelative("key");
            var variantsProp = entry.FindPropertyRelative("variants");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            string entryLabel = string.IsNullOrEmpty(keyProp.stringValue) ? $"🖼️ Image Entry {index + 1}" : $"🖼️ {keyProp.stringValue}";
            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, entryLabel, true);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ Delete", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Delete Image Entry", 
                    $"Are you sure you want to delete this image entry?", "Yes", "No"))
                {
                    spriteEntriesProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            if (entry.isExpanded)
            {
                keyProp.stringValue = EditorGUILayout.TextField("Key", keyProp.stringValue);
                
                EditorGUILayout.Space(5);
                
                // Variants foldout
                variantsProp.isExpanded = EditorGUILayout.Foldout(variantsProp.isExpanded, $"Language Variants ({variantsProp.arraySize})", true);
                
                if (variantsProp.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    
                    // Add variant button
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("➕ Add Language Variant", GUILayout.Height(20)))
                    {
                        variantsProp.arraySize++;
                        var newVariant = variantsProp.GetArrayElementAtIndex(variantsProp.arraySize - 1);
                        newVariant.FindPropertyRelative("languageCode").stringValue = "en";
                        serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(3);
                    
                    // Draw variants
                    for (int v = variantsProp.arraySize - 1; v >= 0; v--)
                    {
                        var variant = variantsProp.GetArrayElementAtIndex(v);
                        var langCodeProp = variant.FindPropertyRelative("languageCode");
                        var spriteProp = variant.FindPropertyRelative("sprite");
                        
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.BeginHorizontal();
                        
                        EditorGUILayout.LabelField("Language:", GUILayout.Width(70));
                        langCodeProp.stringValue = EditorGUILayout.TextField(langCodeProp.stringValue, GUILayout.Width(50));
                        
                        GUILayout.FlexibleSpace();
                        
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("✖", GUILayout.Width(30)))
                        {
                            variantsProp.DeleteArrayElementAtIndex(v);
                            serializedObject.ApplyModifiedProperties();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            continue;
                        }
                        GUI.backgroundColor = Color.white;
                        
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUILayout.PropertyField(spriteProp, new GUIContent("Sprite"));
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawAssetsTab()
        {
            EditorGUILayout.LabelField("📦 Asset Localization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generic Unity Objects that vary by language. Each entry has a key and variants for different language codes. This is managed exclusively in Unity.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // Copy Text Pages button
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("📋 Copy Text Pages", GUILayout.Height(30)))
            {
                CopyTextPagesToAssets();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Copy Text Pages: Creates asset pages matching the structure of text pages for convenience.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Add new page button
            if (GUILayout.Button("➕ Add Asset Page", GUILayout.Height(30)))
            {
                assetPagesProperty.arraySize++;
                var newPage = assetPagesProperty.GetArrayElementAtIndex(assetPagesProperty.arraySize - 1);
                newPage.FindPropertyRelative("pageId").stringValue = $"AssetPage_{assetPagesProperty.arraySize}";
                newPage.FindPropertyRelative("aboutPage").stringValue = "";
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(5);
            
            // Search and pagination
            UpdateAssetFilteredIndices();
            DrawAssetSearchToolbar();
            
            int totalFilteredEntries = assetFilteredIndices.Count;
            
            if (totalFilteredEntries > 0)
            {
                DrawAssetPaginationToolbar(totalFilteredEntries);
                EditorGUILayout.Space(5);
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            
            if (assetPagesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No asset pages. Add a page or copy text pages to get started.", MessageType.Info);
            }
            else if (totalFilteredEntries == 0)
            {
                EditorGUILayout.HelpBox("No asset pages match the current search.", MessageType.Info);
            }
            else
            {
                int startIndex = assetCurrentPage * EntriesPerPage;
                int endIndex = Mathf.Min(startIndex + EntriesPerPage, totalFilteredEntries);
                
                for (int displayIndex = startIndex; displayIndex < endIndex; displayIndex++)
                {
                    int actualIndex = assetFilteredIndices[displayIndex];
                    DrawAssetPage(actualIndex);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawAssetSearchToolbar()
        {
            GUIStyle searchFieldStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarSearchField;
            GUIStyle fallbackMiniButton = EditorStyles.miniButton;
            GUIStyle cancelButtonStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? GUI.skin.FindStyle("ToolbarCancelButton") ?? fallbackMiniButton;
            bool usingFallbackCancel = cancelButtonStyle == fallbackMiniButton || cancelButtonStyle.name == fallbackMiniButton.name;
            GUIContent cancelContent = usingFallbackCancel ? new GUIContent("x", "Clear search") : GUIContent.none;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUI.BeginChangeCheck();
            assetSearchTerm = EditorGUILayout.TextField(assetSearchTerm, searchFieldStyle, GUILayout.ExpandWidth(true));
            float cancelWidth = cancelButtonStyle.fixedWidth > 0 ? cancelButtonStyle.fixedWidth : 18f;
            
            if (GUILayout.Button(cancelContent, cancelButtonStyle, GUILayout.Width(cancelWidth)))
            {
                assetSearchTerm = string.Empty;
                GUI.FocusControl(null);
                EditorGUI.EndChangeCheck();
                UpdateAssetFilteredIndices();
                assetCurrentPage = 0;
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                assetCurrentPage = 0;
                UpdateAssetFilteredIndices();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAssetPaginationToolbar(int totalEntries)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));
            assetCurrentPage = Mathf.Clamp(assetCurrentPage, 0, totalPages - 1);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            using (new EditorGUI.DisabledScope(assetCurrentPage <= 0))
            {
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    assetCurrentPage = Mathf.Max(0, assetCurrentPage - 1);
                }
            }
            
            using (new EditorGUI.DisabledScope(assetCurrentPage >= totalPages - 1))
            {
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    assetCurrentPage = Mathf.Min(totalPages - 1, assetCurrentPage + 1);
                }
            }
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Page {assetCurrentPage + 1} of {totalPages} · {totalEntries} {(totalEntries == 1 ? "Page" : "Pages")}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void UpdateAssetFilteredIndices()
        {
            assetFilteredIndices.Clear();
            
            if (assetPagesProperty != null)
            {
                string comparisonTerm = assetSearchTerm ?? string.Empty;
                
                for (int i = 0; i < assetPagesProperty.arraySize; i++)
                {
                    SerializedProperty pageProp = assetPagesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty pageIdProp = pageProp.FindPropertyRelative("pageId");
                    string pageIdValue = pageIdProp != null ? pageIdProp.stringValue : string.Empty;
                    
                    bool matches = string.IsNullOrEmpty(comparisonTerm) || 
                                  (!string.IsNullOrEmpty(pageIdValue) && pageIdValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    if (!matches)
                    {
                        // Also check entries
                        SerializedProperty entriesProp = pageProp.FindPropertyRelative("assetEntries");
                        for (int e = 0; e < entriesProp.arraySize; e++)
                        {
                            SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(e);
                            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
                            string keyValue = keyProp != null ? keyProp.stringValue : string.Empty;
                            
                            if (!string.IsNullOrEmpty(keyValue) && keyValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                    
                    if (matches)
                    {
                        assetFilteredIndices.Add(i);
                    }
                }
            }
            
            int totalPages = assetFilteredIndices.Count == 0 ? 1 : Mathf.CeilToInt(assetFilteredIndices.Count / (float)EntriesPerPage);
            assetCurrentPage = Mathf.Clamp(assetCurrentPage, 0, Mathf.Max(0, totalPages - 1));
        }
        
        private void DrawAssetPage(int pageIndex)
        {
            var page = assetPagesProperty.GetArrayElementAtIndex(pageIndex);
            var pageIdProp = page.FindPropertyRelative("pageId");
            var aboutPageProp = page.FindPropertyRelative("aboutPage");
            var assetEntriesProp = page.FindPropertyRelative("assetEntries");
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📄 Asset Page: {pageIdProp.stringValue}", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ Delete Page", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Delete Asset Page", 
                    $"Are you sure you want to delete this asset page?", "Yes", "No"))
                {
                    assetPagesProperty.DeleteArrayElementAtIndex(pageIndex);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            pageIdProp.stringValue = EditorGUILayout.TextField("Page ID", pageIdProp.stringValue);
            aboutPageProp.stringValue = EditorGUILayout.TextArea(aboutPageProp.stringValue, GUILayout.Height(40));
            
            EditorGUILayout.Space(5);
            
            // Entries foldout
            assetEntriesProp.isExpanded = EditorGUILayout.Foldout(assetEntriesProp.isExpanded, $"Asset Entries ({assetEntriesProp.arraySize})", true);
            
            if (assetEntriesProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Add new asset entry button
                if (GUILayout.Button("➕ Add Asset Entry", GUILayout.Height(25)))
                {
                    assetEntriesProp.arraySize++;
                    var newEntry = assetEntriesProp.GetArrayElementAtIndex(assetEntriesProp.arraySize - 1);
                    newEntry.FindPropertyRelative("key").stringValue = $"asset_key_{assetEntriesProp.arraySize}";
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space(5);
                
                // Draw asset entries
                for (int i = assetEntriesProp.arraySize - 1; i >= 0; i--)
                {
                    DrawAssetEntry(assetEntriesProp, i, pageIndex);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void CopyTextPagesToAssets()
        {
            var locBook = (LocBook)target;
            assetPagesProperty.ClearArray();
            
            foreach (var textPage in locBook.Pages)
            {
                assetPagesProperty.arraySize++;
                var newPage = assetPagesProperty.GetArrayElementAtIndex(assetPagesProperty.arraySize - 1);
                newPage.FindPropertyRelative("pageId").stringValue = textPage.pageId;
                newPage.FindPropertyRelative("aboutPage").stringValue = textPage.aboutPage;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("Copied", $"Copied {locBook.Pages.Count} text page(s) to asset pages.", "OK");
        }
        
        private void DrawAssetEntry(SerializedProperty assetEntriesProp, int index, int pageIndex)
        {
            var entry = assetEntriesProp.GetArrayElementAtIndex(index);
            var keyProp = entry.FindPropertyRelative("key");
            var variantsProp = entry.FindPropertyRelative("variants");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            string entryLabel = string.IsNullOrEmpty(keyProp.stringValue) ? $"📦 Asset Entry {index + 1}" : $"📦 {keyProp.stringValue}";
            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, entryLabel, true);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖ Delete", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Delete Asset Entry", 
                    $"Are you sure you want to delete this asset entry?", "Yes", "No"))
                {
                    assetEntriesProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            if (entry.isExpanded)
            {
                keyProp.stringValue = EditorGUILayout.TextField("Key", keyProp.stringValue);
                
                EditorGUILayout.Space(5);
                
                // Variants foldout
                variantsProp.isExpanded = EditorGUILayout.Foldout(variantsProp.isExpanded, $"Language Variants ({variantsProp.arraySize})", true);
                
                if (variantsProp.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    
                    // Add variant button
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("➕ Add Language Variant", GUILayout.Height(20)))
                    {
                        variantsProp.arraySize++;
                        var newVariant = variantsProp.GetArrayElementAtIndex(variantsProp.arraySize - 1);
                        newVariant.FindPropertyRelative("languageCode").stringValue = "en";
                        serializedObject.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(3);
                    
                    // Draw variants
                    for (int v = variantsProp.arraySize - 1; v >= 0; v--)
                    {
                        var variant = variantsProp.GetArrayElementAtIndex(v);
                        var langCodeProp = variant.FindPropertyRelative("languageCode");
                        var assetProp = variant.FindPropertyRelative("asset");
                        
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.BeginHorizontal();
                        
                        EditorGUILayout.LabelField("Language:", GUILayout.Width(70));
                        langCodeProp.stringValue = EditorGUILayout.TextField(langCodeProp.stringValue, GUILayout.Width(50));
                        
                        GUILayout.FlexibleSpace();
                        
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("✖", GUILayout.Width(30)))
                        {
                            variantsProp.DeleteArrayElementAtIndex(v);
                            serializedObject.ApplyModifiedProperties();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            continue;
                        }
                        GUI.backgroundColor = Color.white;
                        
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUILayout.PropertyField(assetProp, new GUIContent("Asset (Object)"));
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawConfigManagementSection()
        {
            EditorGUILayout.LabelField("⚙️ Config Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var locBook = target as LocBook;
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            bool isInConfig = IsInConfig(config, locBook);
            
            string statusText = isInConfig ? "✓ Active in Config" : "✗ Not Active";
            Color statusColor = isInConfig ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", statusStyle);
            
            if (isInConfig)
            {
                EditorGUILayout.HelpBox("This LocBook is currently set as the active localization data source in Signalia's Lingramia config.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("This LocBook is not set in the Signalia's Lingramia config.", MessageType.Warning);
            }
            
            GUILayout.Space(5);
            
            string buttonText = isInConfig ? "Remove from Config" : "Set as Active LocBook";
            Color buttonColor = isInConfig ? Color.red : Color.green;
            
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (config == null)
                {
                    EditorUtility.DisplayDialog("Config Not Found", 
                        "Signalia's Lingramia config asset not found. Please ensure it exists in your project.", 
                        "OK");
                }
                else
                {
                    if (isInConfig)
                    {
                        RemoveFromConfig(config, locBook);
                        EditorUtility.DisplayDialog("LocBook Removed", 
                            $"'{locBook.name}' has been removed from the config.", 
                            "OK");
                    }
                    else
                    {
                        AddToConfig(config, locBook);
                        EditorUtility.DisplayDialog("LocBook Added", 
                            $"'{locBook.name}' has been added to the config.", 
                            "OK");
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }
        
        private bool IsInConfig(SignaliaConfigAsset config, LocBook locBook)
        {
            if (config == null || locBook == null || config.LocalizationSystem.LocBooks == null)
                return false;
            
            foreach (var lb in config.LocalizationSystem.LocBooks)
            {
                if (lb == locBook)
                    return true;
            }
            
            return false;
        }
        
        private void AddToConfig(SignaliaConfigAsset config, LocBook locBook)
        {
            if (config == null || locBook == null)
                return;
            
            var list = new List<LocBook>(config.LocalizationSystem.LocBooks ?? new LocBook[0]);
            
            if (!list.Contains(locBook))
            {
                list.Add(locBook);
                config.LocalizationSystem.LocBooks = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
        
        private void RemoveFromConfig(SignaliaConfigAsset config, LocBook locBook)
        {
            if (config == null || locBook == null || config.LocalizationSystem.LocBooks == null)
                return;
            
            var list = new List<LocBook>(config.LocalizationSystem.LocBooks);
            
            if (list.Remove(locBook))
            {
                config.LocalizationSystem.LocBooks = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
    }
    
    /// <summary>
    /// Custom editor for TextStyle ScriptableObjects.
    /// Provides a user-friendly interface for managing text formatting settings.
    /// </summary>
    [CustomEditor(typeof(TextStyle))]
    public class TextStyleEditor : Editor
    {
        private SerializedProperty languageCodeProp;
        private SerializedProperty paragraphStyleProp;
        private SerializedProperty fontProp;
        private SerializedProperty materialOverrideProp;
        private SerializedProperty formattingOptionsProp;
        private SerializedProperty enableRTLProp;
        private SerializedProperty enableArabicFormattingProp;
        
        private void OnEnable()
        {
            languageCodeProp = serializedObject.FindProperty("languageCode");
            paragraphStyleProp = serializedObject.FindProperty("paragraphStyle");
            fontProp = serializedObject.FindProperty("font");
            materialOverrideProp = serializedObject.FindProperty("materialOverride");
            formattingOptionsProp = serializedObject.FindProperty("formattingOptions");
            enableRTLProp = serializedObject.FindProperty("enableRTL");
            enableArabicFormattingProp = serializedObject.FindProperty("enableArabicFormatting");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var textStyle = (TextStyle)target;
            
            // Header
            //GUILayout.Label(GraphicLoader.LocalizationTextStyle, GUILayout.Height(150));
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Text Style Asset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This asset defines text formatting and font settings for a specific language. " +
                                   "It will be automatically applied when displaying localized text.", 
                                   MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Language Section
            EditorGUILayout.LabelField("Language", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(languageCodeProp, new GUIContent("Language Code", "The language code this style applies to (e.g., 'en', 'es', 'fr', 'ar')"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(paragraphStyleProp, new GUIContent("Paragraph Style", "Optional paragraph style identifier (e.g., 'Header', 'Description', 'Body'). Leave empty for default style. You can use any custom string value."));
            
            if (!string.IsNullOrEmpty(paragraphStyleProp.stringValue))
            {
                EditorGUILayout.HelpBox($"This style will be used when paragraph style '{paragraphStyleProp.stringValue}' is requested. " +
                                       "You can use any custom string - it doesn't need to match predefined values.", 
                                       MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Empty paragraph style means this is the default style for this language. " +
                                       "It will be used when no specific paragraph style is requested or when the requested style doesn't exist.", 
                                       MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Font Section
            EditorGUILayout.LabelField("Font Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(fontProp, new GUIContent("Font Asset", "The TMP Font Asset to use for this language"));
            EditorGUILayout.PropertyField(materialOverrideProp, new GUIContent("Material Override", "Optional material override to apply after the font is set. Leave empty to use the font's default material."));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Formatting Options Section
            EditorGUILayout.LabelField("Formatting Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(formattingOptionsProp, new GUIContent("Formatting Options", "Select multiple formatting options to apply. Conflicting options will be prevented."));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Writing Systems Section
            EditorGUILayout.LabelField("Writing Systems", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(enableRTLProp, new GUIContent("Enable RTL", "Enable right-to-left text direction for languages like Arabic or Hebrew."));
            EditorGUILayout.PropertyField(enableArabicFormattingProp, new GUIContent("Enable Arabic Formatting", "Format Arabic text so characters connect correctly. Disable if not using Arabic."));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // TMP Font Generation
            DrawTMPFontGeneration(textStyle);
            
            EditorGUILayout.Space(10);
            
            // Show active formatting summary
            DrawFormattingSummary(textStyle);
            
            EditorGUILayout.Space(10);
            
            // Config Management
            DrawConfigManagementSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawTMPFontGeneration(TextStyle textStyle)
        {
            EditorGUILayout.LabelField("🔄 TMP Font Generation", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox("Generate a TextMeshPro font asset from a Unity Font. The generated font will be saved in the same directory as the source font and automatically assigned to this text style.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            SerializedProperty fontProperty = serializedObject.FindProperty("font");
            TMP_FontAsset currentFont = fontProperty.objectReferenceValue as TMP_FontAsset;
            
            if (currentFont != null)
            {
                EditorGUILayout.LabelField($"Current Font: {currentFont.name}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("No TMP font assigned. Select a Unity Font below to generate one.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(5);
            
            // Select Unity Font button
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("🎨 Select Unity Font & Generate TMP Font", GUILayout.Height(30)))
            {
                SelectAndGenerateTMPFont(textStyle);
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            // Generate button for already assigned font
            if (currentFont != null)
            {
                EditorGUILayout.HelpBox("To regenerate this font, select a Unity Font above.", MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void SelectAndGenerateTMPFont(TextStyle textStyle)
        {
            // Let user select a Unity Font
            string selectedPath = EditorUtility.OpenFilePanel("Select Unity Font", "Assets", "ttf");
            
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }
            
            // Convert to assets path
            if (selectedPath.StartsWith(Application.dataPath))
            {
                selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path", "Please select a font within the Assets folder.", "OK");
                return;
            }
            
            // Load the font
            Font selectedFont = AssetDatabase.LoadAssetAtPath<Font>(selectedPath);
            
            if (selectedFont == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to load font from the selected path.", "OK");
                return;
            }
            
            // Generate save path in the same directory as the font
            string fontDirectory = Path.GetDirectoryName(selectedPath).Replace('\\', '/');
            string fileName = Path.GetFileNameWithoutExtension(selectedPath);
            string savePath = Path.Combine(fontDirectory, fileName + "_SDF.asset").Replace('\\', '/');
            
            // Generate TMP Font using the factory
            var generatedFont = SignaliaTMPFontFactory.GenerateTMPFont(selectedFont, savePath);
            
            if (generatedFont != null)
            {
                // Assign the generated font to the text style
                serializedObject.FindProperty("font").objectReferenceValue = generatedFont;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(textStyle);
                
                EditorUtility.DisplayDialog("Success", $"TMP font generated and assigned: {generatedFont.name}", "OK");
                Selection.activeObject = generatedFont;
            }
        }
        
        private void DrawFormattingSummary(TextStyle textStyle)
        {
            EditorGUILayout.LabelField("Active Formatting Summary", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var formatting = textStyle.Formatting;
            
            if (formatting == FormattingOptions.None)
            {
                EditorGUILayout.LabelField("No formatting applied", EditorStyles.miniLabel);
            }
            else
            {
                System.Text.StringBuilder summary = new System.Text.StringBuilder();
                
                // Font Styles
                if ((formatting & FormattingOptions.Bold) != 0) summary.Append("Bold, ");
                if ((formatting & FormattingOptions.Italic) != 0) summary.Append("Italic, ");
                if ((formatting & FormattingOptions.Underline) != 0) summary.Append("Underline, ");
                
                // Case Transformations
                if ((formatting & FormattingOptions.AllCaps) != 0) summary.Append("ALL CAPS, ");
                else if ((formatting & FormattingOptions.TitleCase) != 0) summary.Append("Title Case, ");
                else if ((formatting & FormattingOptions.LowerCase) != 0) summary.Append("lower case, ");
                
                string result = summary.ToString().TrimEnd(',', ' ');
                
                if (!string.IsNullOrEmpty(result))
                {
                    EditorGUILayout.LabelField($"Applied: {result}", EditorStyles.miniLabel);
                }
            }
            
            if (textStyle.Font != null)
            {
                EditorGUILayout.LabelField($"Font: {textStyle.Font.name}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Font: (None - will use existing font)", EditorStyles.miniLabel);
            }
            
            // Material Override
            if (textStyle.MaterialOverride != null)
            {
                EditorGUILayout.LabelField($"Material: {textStyle.MaterialOverride.name}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Material: (None - will use font's default material)", EditorStyles.miniLabel);
            }
            
            // Writing Systems
            if (textStyle.EnableRTL)
            {
                EditorGUILayout.LabelField("Writing System: RTL (Right-to-Left)", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Writing System: LTR (Left-to-Right)", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawConfigManagementSection()
        {
            EditorGUILayout.LabelField("⚙️ Config Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var textStyle = target as TextStyle;
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            bool isInConfig = IsInConfig(config, textStyle);
            
            string statusText = isInConfig ? "✓ In Cache" : "✗ Not In Cache";
            Color statusColor = isInConfig ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", statusStyle);
            
            if (isInConfig)
            {
                EditorGUILayout.HelpBox("This TextStyle is in the config cache and will be used by the localization system.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("This TextStyle is not in the config cache.", MessageType.Warning);
            }
            
            GUILayout.Space(5);
            
            string buttonText = isInConfig ? "Remove from Config" : "Add to Config";
            Color buttonColor = isInConfig ? Color.red : Color.green;
            
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (config == null)
                {
                    EditorUtility.DisplayDialog("Config Not Found", 
                        "Signalia's Lingramia config asset not found. Please ensure it exists in your project.", 
                        "OK");
                }
                else
                {
                    if (isInConfig)
                    {
                        RemoveFromConfig(config, textStyle);
                        EditorUtility.DisplayDialog("TextStyle Removed", 
                            $"'{textStyle.name}' has been removed from the config cache.", 
                            "OK");
                    }
                    else
                    {
                        AddToConfig(config, textStyle);
                        EditorUtility.DisplayDialog("TextStyle Added", 
                            $"'{textStyle.name}' has been added to the config cache.", 
                            "OK");
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }
        
        private bool IsInConfig(SignaliaConfigAsset config, TextStyle style)
        {
            if (config == null || config.LocalizationSystem.TextStyleCache == null)
                return false;
            
            foreach (var s in config.LocalizationSystem.TextStyleCache)
            {
                if (s == style)
                    return true;
            }
            
            return false;
        }
        
        private void AddToConfig(SignaliaConfigAsset config, TextStyle style)
        {
            var list = new System.Collections.Generic.List<TextStyle>(config.LocalizationSystem.TextStyleCache ?? new TextStyle[0]);
            
            if (!list.Contains(style))
            {
                list.Add(style);
                config.LocalizationSystem.TextStyleCache = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
        
        private void RemoveFromConfig(SignaliaConfigAsset config, TextStyle style)
        {
            var list = new System.Collections.Generic.List<TextStyle>(config.LocalizationSystem.TextStyleCache ?? new TextStyle[0]);
            
            if (list.Remove(style))
            {
                config.LocalizationSystem.TextStyleCache = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
    }
}