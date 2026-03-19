#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.GameSystems.Localization.External;

namespace AHAKuo.Signalia.GameSystems.Localization.External.Editors
{
    /// <summary>
    /// Editor window for extracting non-localized strings from scenes and ScriptableObjects.
    /// Creates a LocBook asset and .locbook file ready for use with Lingramia.
    /// </summary>
    public class LocbookExtractorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool isProcessing = false;
        private string statusMessage = "";
        private float progressValue = 0f;
        private string progressLabel = "";
        private bool updateMode = false; // Toggle between Extract and Update modes
        
        private const string WINDOW_TITLE = "Locbook Extractor";
        private const string DEFAULT_LOCBOOK_NAME = "Extracted Strings";
        
        [MenuItem("Tools/Signalia/Game Systems/Localization/Extract Locbook", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<LocbookExtractorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void OnGUI()
        {
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (!isProcessing)
            {
                DrawInstructions();
                EditorGUILayout.Space(10);
                DrawActionButton();
            }
            else
            {
                DrawProgress();
            }
            
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(10);
                DrawStatusMessage();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Locbook Extraction Tool", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Extract non-localized strings and prepare them for localization", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("Mode Selection", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Operation Mode:", GUILayout.Width(120));
            updateMode = EditorGUILayout.ToggleLeft("Update Existing Locbooks", updateMode, GUILayout.Width(180));
            EditorGUILayout.ToggleLeft("Extract New Locbooks", !updateMode, GUILayout.Width(180));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            if (updateMode)
            {
                EditorGUILayout.HelpBox(
                    "Update Mode:\n\n" +
                    "• Extracts new strings from your code\n" +
                    "• Finds existing .locbook files with matching names\n" +
                    "• Updates Original Value and/or About fields (your choice)\n" +
                    "• Preserves all translations/variants (never overwrites)\n" +
                    "• Preserves all existing keys\n" +
                    "• Adds new entries that didn't exist before",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Extract Mode:\n\n" +
                    "• Creates new LocBook assets and .locbook files\n" +
                    "• Use this when starting fresh or creating new locbooks",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("How This Works", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (updateMode)
            {
                EditorGUILayout.HelpBox(
                    "This tool will:\n\n" +
                    "1. Scan all open scenes for MonoBehaviours implementing ILocbookExtraction\n" +
                    "2. Scan all ScriptableObjects in Resources folders implementing ILocbookExtraction\n" +
                    "3. Collect all extraction data into pages\n" +
                    "4. Group pages by their groupName (if specified)\n" +
                    "5. Search for existing .locbook files with matching group names\n" +
                    "6. Merge new data with existing locbooks (preserving translations and keys)\n" +
                    "7. Update Original Value and/or About fields based on your selection",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "This tool will:\n\n" +
                    "1. Scan all open scenes for MonoBehaviours implementing ILocbookExtraction\n" +
                    "2. Scan all ScriptableObjects in Resources folders implementing ILocbookExtraction\n" +
                    "3. Collect all extraction data into pages\n" +
                    "4. Group pages by their groupName (if specified)\n" +
                    "5. Create separate LocBook assets for each group\n" +
                    "6. Generate .locbook files that can be opened with Lingramia\n" +
                    "7. Provide instructions for updating your code to use localization",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Before You Start", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox(
                "Make sure the scenes you want to extract from are currently open\n" +
                "Ensure your assets implementing ILocbookExtraction are in Resources folders\n" +
                "The extraction process may take a few moments depending on project size\n" +
                "You'll be asked where to save each LocBook asset when extraction completes\n" +
                "Pages with the same groupName will be combined into one LocBook",
                MessageType.Warning);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("After Extraction", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox(
                "After the LocBook is created, you'll need to update your code:\n\n" +
                "Replace:\n" +
                "    tmpText.text = someText;\n\n" +
                "With:\n" +
                "    tmpText.SetLocalizedText(someText);\n\n" +
                "TIP: If you don't want to create keys for every string, enable 'Hybrid Key Mode' " +
                "in your Signalia config. This allows you to use the original text value as the key.",
                MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButton()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            string buttonText = updateMode ? "Update Locbooks" : "Start Extraction";
            if (GUILayout.Button(buttonText, GUILayout.Height(40), GUILayout.Width(200)))
            {
                if (updateMode)
                {
                    StartUpdate();
                }
                else
                {
                    StartExtraction();
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawProgress()
        {
            EditorGUILayout.LabelField("Processing...", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.Space(10);
            
            Rect progressRect = EditorGUILayout.GetControlRect(false, 30);
            EditorGUI.ProgressBar(progressRect, progressValue, progressLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Please wait while the extraction process completes...", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusMessage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(statusMessage, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }
        
        private void StartExtraction()
        {
            isProcessing = true;
            statusMessage = "";
            progressValue = 0f;
            progressLabel = "Initializing...";
            Repaint();
            
            EditorApplication.delayCall += PerformExtraction;
        }
        
        private void StartUpdate()
        {
            isProcessing = true;
            statusMessage = "";
            progressValue = 0f;
            progressLabel = "Initializing...";
            Repaint();
            
            EditorApplication.delayCall += PerformUpdate;
        }

        private struct LocbookData
        {
            public List<ExtractionPage> pages;
            public string groupName; 

            public LocbookData(List<ExtractionPage> pages, string groupName = DEFAULT_LOCBOOK_NAME)
            {
                this.pages = pages;
                this.groupName = groupName;
            }
        }
        
        private void PerformExtraction()
        {
            try
            {
                var allLocbookData = new List<LocbookData>();
                int totalSteps = 3; // Scene scan, Resources scan, LocBook creation
                int currentStep = 0;
                
                // Step 1: Scan open scenes
                currentStep++;
                progressValue = (float)currentStep / totalSteps;
                progressLabel = "Scanning open scenes...";
                Repaint();
                
                var sceneLocbookData = ScanOpenScenes();
                if (sceneLocbookData != null && sceneLocbookData.Count > 0)
                {
                    allLocbookData.AddRange(sceneLocbookData);
                }
                
                // Step 2: Scan Resources folders
                currentStep++;
                progressValue = (float)currentStep / totalSteps;
                progressLabel = "Scanning ScriptableObjects in Resources...";
                Repaint();
                
                var resourceLocbookData = ScanResourcesFolder();
                if (resourceLocbookData != null && resourceLocbookData.Count > 0)
                {
                    allLocbookData.AddRange(resourceLocbookData);
                }
                
                // Check if we found anything
                if (allLocbookData.Count == 0)
                {
                    statusMessage = "No objects implementing ILocbookExtraction were found.\n\n" +
                                  "Make sure:\n" +
                                  "Your MonoBehaviours/ScriptableObjects implement ILocbookExtraction\n" +
                                  "Scenes with these objects are currently open\n" +
                                  "ScriptableObjects are located in Resources folders";
                    isProcessing = false;
                    Repaint();
                    return;
                }
                
                // Step 3: Group by groupName and create LocBooks
                currentStep++;
                progressValue = (float)currentStep / totalSteps;
                progressLabel = "Grouping pages and creating LocBooks...";
                Repaint();
                
                // Group pages by groupName (use DEFAULT_LOCBOOK_NAME for empty groupName)
                var groupedData = new Dictionary<string, List<ExtractionPage>>();
                
                foreach (var locbookData in allLocbookData)
                {
                    string groupName = string.IsNullOrEmpty(locbookData.groupName) 
                        ? DEFAULT_LOCBOOK_NAME 
                        : locbookData.groupName;
                    
                    if (!groupedData.ContainsKey(groupName))
                    {
                        groupedData[groupName] = new List<ExtractionPage>();
                    }
                    
                    groupedData[groupName].AddRange(locbookData.pages);
                }
                
                // Create a LocBook for each group
                var createdLocbooks = new List<LocBook>();
                var createdPaths = new List<string>();
                int groupIndex = 0;
                int totalGroups = groupedData.Count;
                
                foreach (var kvp in groupedData)
                {
                    string groupName = kvp.Key;
                    List<ExtractionPage> pages = kvp.Value;
                    
                    progressLabel = $"Creating LocBook '{groupName}' ({groupIndex + 1}/{totalGroups})...";
                    Repaint();
                    
                    // Ask user where to save this LocBook
                    string defaultFileName = SanitizeFileName(groupName);
                    string savePath = EditorUtility.SaveFilePanelInProject(
                        $"Save LocBook Asset: {groupName}",
                        defaultFileName,
                        "asset",
                        $"Choose where to save the LocBook asset for group '{groupName}'");
                    
                    if (string.IsNullOrEmpty(savePath))
                    {
                        statusMessage = $"Extraction cancelled by user while saving group '{groupName}'.";
                        isProcessing = false;
                        Repaint();
                        return;
                    }
                    
                    // Create the LocBook for this group
                    var locbookData = new LocbookData(pages, groupName);
                    var locbook = CreateLocBook(locbookData, savePath);
                    
                    if (locbook != null)
                    {
                        createdLocbooks.Add(locbook);
                        createdPaths.Add(savePath);
                    }
                    else
                    {
                        Debug.LogError($"[Locbook Extractor] Failed to create LocBook for group '{groupName}'");
                    }
                    
                    groupIndex++;
                }
                
                // Success message
                if (createdLocbooks.Count > 0)
                {
                    int totalPages = allLocbookData.Sum(ld => ld.pages.Count);
                    int totalFields = allLocbookData.Sum(ld => ld.pages.Sum(p => p.fields.Count));
                    
                    string locbookList = string.Join("\n", createdPaths.Select(p => $"   • {Path.GetFileName(p)}"));
                    
                    statusMessage = $"Extraction Complete!\n\n" +
                                  $"Statistics:\n" +
                                  $"   Groups created: {createdLocbooks.Count}\n" +
                                  $"   Total pages extracted: {totalPages}\n" +
                                  $"   Total text fields: {totalFields}\n\n" +
                                  $"Created LocBooks:\n{locbookList}\n\n" +
                                  $"Next Steps:\n" +
                                  $"1. Open the .locbook files in Lingramia to add translations\n" +
                                  $"2. Load the LocBook assets into your Signalia config\n" +
                                  $"3. Update your code to use .SetLocalizedText() instead of .text\n" +
                                  $"4. Consider enabling Hybrid Key mode in Signalia config if you want to use original values as keys";
                    
                    // Select the last created asset
                    Selection.activeObject = createdLocbooks[createdLocbooks.Count - 1];
                    EditorGUIUtility.PingObject(createdLocbooks[createdLocbooks.Count - 1]);
                }
                else
                {
                    statusMessage = "Failed to create any LocBook assets.";
                }
            }
            catch (System.Exception e)
            {
                statusMessage = $"Error during extraction:\n{e.Message}\n\nSee console for details.";
                Debug.LogError($"[Locbook Extractor] Error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isProcessing = false;
                progressValue = 1f;
                progressLabel = "Complete";
                Repaint();
            }
        }
        
        private List<LocbookData> ScanOpenScenes()
        {
            var allLocbookData = new List<LocbookData>();
            
            // Get all open scenes
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                
                // Find all root GameObjects
                var rootObjects = scene.GetRootGameObjects();
                
                foreach (var rootObject in rootObjects)
                {
                    // Get all MonoBehaviours implementing ILocbookExtraction (including children)
                    var extractables = rootObject.GetComponentsInChildren<ILocbookExtraction>(true);
                    
                    foreach (var extractable in extractables)
                    {
                        try
                        {
                            var data = extractable.GetExtractionData();
                            if (data != null && data.pages != null && data.pages.Count > 0)
                            {
                                string groupName = string.IsNullOrEmpty(data.groupName) 
                                    ? DEFAULT_LOCBOOK_NAME 
                                    : data.groupName;
                                
                                allLocbookData.Add(new LocbookData(data.pages, groupName));
                            }
                        }
                        catch (System.Exception e)
                        {
                            MonoBehaviour mb = extractable as MonoBehaviour;
                            string objName = mb != null ? mb.gameObject.name : extractable.GetType().Name;
                            Debug.LogWarning($"[Locbook Extractor] Failed to extract from {objName}: {e.Message}");
                        }
                    }
                }
            }
            
            return allLocbookData;
        }
        
        private List<LocbookData> ScanResourcesFolder()
        {
            var allLocbookData = new List<LocbookData>();
            
            // Find all ScriptableObjects in Resources folders
            string[] resourcesFolders = Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories);
            
            foreach (var resourcesFolder in resourcesFolders)
            {
                string[] assetPaths = Directory.GetFiles(resourcesFolder, "*.asset", SearchOption.AllDirectories);
                
                foreach (var assetPath in assetPaths)
                {
                    // Normalize path for Unity
                    string unityPath = assetPath.Replace("\\", "/");
                    
                    // Load the asset
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(unityPath);
                    
                    if (asset != null && asset is ILocbookExtraction extractable)
                    {
                        try
                        {
                            var data = extractable.GetExtractionData();
                            if (data != null && data.pages != null && data.pages.Count > 0)
                            {
                                string groupName = string.IsNullOrEmpty(data.groupName) 
                                    ? DEFAULT_LOCBOOK_NAME 
                                    : data.groupName;
                                
                                allLocbookData.Add(new LocbookData(data.pages, groupName));
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[Locbook Extractor] Failed to extract from {asset.name}: {e.Message}");
                        }
                    }
                }
            }
            
            return allLocbookData;
        }
        
        private LocBook CreateLocBook(LocbookData locbookData, string assetPath)
        {
            // Create the LocBook asset
            var locbook = ScriptableObject.CreateInstance<LocBook>();
            
            // Convert extraction pages to LocBook pages
            foreach (var extractionPage in locbookData.pages)
            {
                var page = new LocBook.Page
                {
                    pageId = !string.IsNullOrEmpty(extractionPage.pageId) 
                        ? extractionPage.pageId 
                        : GeneratePageId(extractionPage.pageName),
                    aboutPage = extractionPage.about,
                    entries = new List<LocBook.LocalizationEntry>()
                };
                
                // Convert fields to entries
                for (int i = 0; i < extractionPage.fields.Count; i++)
                {
                    var field = extractionPage.fields[i];
                    
                    if (string.IsNullOrEmpty(field.originalValue))
                    {
                        Debug.LogWarning($"[Locbook Extractor] Skipping field with empty originalValue in page '{extractionPage.pageName}'");
                        continue;
                    }
                    
                    var entry = new LocBook.LocalizationEntry
                    {
                        key = !string.IsNullOrEmpty(field.key) 
                            ? field.key 
                            : GenerateKey(field.originalValue, i),
                        originalValue = field.originalValue,
                        variants = new List<LocBook.LanguageVariant>()
                    };
                    
                    // Add any pre-existing variants
                    if (field.variants != null)
                    {
                        foreach (var variant in field.variants)
                        {
                            entry.variants.Add(new LocBook.LanguageVariant
                            {
                                languageCode = variant.languageCode,
                                value = variant.value
                            });
                        }
                    }
                    
                    page.entries.Add(entry);
                }
                
                locbook.Pages.Add(page);
            }
            
            // Save the LocBook asset
            AssetDatabase.CreateAsset(locbook, assetPath);
            
            // Create the .locbook JSON file
            string jsonPath = Path.ChangeExtension(assetPath, ".locbook");
            string json = locbook.GetExternalJsonData();
            File.WriteAllText(jsonPath, json);
            
            // Import the JSON file as TextAsset and link it to the LocBook
            AssetDatabase.ImportAsset(jsonPath);
            var textAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(jsonPath);
            locbook.LocbookFile = textAsset;
            
            // Save changes
            EditorUtility.SetDirty(locbook);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[Locbook Extractor] Created LocBook '{locbookData.groupName}' at: {assetPath}");
            Debug.Log($"[Locbook Extractor] Created .locbook file at: {jsonPath}");
            
            return locbook;
        }
        
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return DEFAULT_LOCBOOK_NAME;
            }
            
            // Remove invalid file name characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            // Replace spaces with underscores
            sanitized = sanitized.Replace(" ", "_");
            
            return sanitized;
        }
        
        private string GeneratePageId(string pageName)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                return "page_" + System.Guid.NewGuid().ToString().Substring(0, 8);
            }
            
            // Convert to lowercase and replace spaces/special chars
            return pageName.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
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
        }
        
        private string GenerateKey(string originalValue, int index)
        {
            if (string.IsNullOrEmpty(originalValue))
            {
                return $"entry_{index}";
            }
            
            // Convert to lowercase, replace spaces and special characters
            string key = originalValue.ToLower()
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
            
            return key;
        }
        
        /// <summary>
        /// Performs the update operation: extracts data and merges with existing locbooks.
        /// </summary>
        private void PerformUpdate()
        {
            try
            {
                // Ask user what they want to update
                bool updateOriginalValues = EditorUtility.DisplayDialog(
                    "Update Original Values?",
                    "Do you want to update Original Value fields in existing entries?\n\n" +
                    "Yes: Update original values from extracted data\n" +
                    "No: Keep existing original values unchanged",
                    "Yes", "No");
                
                bool updateAbout = EditorUtility.DisplayDialog(
                    "Update About Fields?",
                    "Do you want to update About/Description fields for pages?\n\n" +
                    "Yes: Update about fields from extracted data\n" +
                    "No: Keep existing about fields unchanged",
                    "Yes", "No");
                
                var allLocbookData = new List<LocbookData>();
                int totalSteps = 3; // Scene scan, Resources scan, Update
                int currentStep = 0;
                
                // Step 1: Scan open scenes
                currentStep++;
                progressValue = (float)currentStep / totalSteps;
                progressLabel = "Scanning open scenes...";
                Repaint();
                
                var sceneLocbookData = ScanOpenScenes();
                if (sceneLocbookData != null && sceneLocbookData.Count > 0)
                {
                    allLocbookData.AddRange(sceneLocbookData);
                }
                
                // Step 2: Scan Resources folders
                currentStep++;
                progressValue = (float)currentStep / totalSteps;
                progressLabel = "Scanning ScriptableObjects in Resources...";
                Repaint();
                
                var resourceLocbookData = ScanResourcesFolder();
                if (resourceLocbookData != null && resourceLocbookData.Count > 0)
                {
                    allLocbookData.AddRange(resourceLocbookData);
                }
                
                // Check if we found anything
                if (allLocbookData.Count == 0)
                {
                    statusMessage = "No objects implementing ILocbookExtraction were found.\n\n" +
                                  "Make sure:\n" +
                                  "Your MonoBehaviours/ScriptableObjects implement ILocbookExtraction\n" +
                                  "Scenes with these objects are currently open\n" +
                                  "ScriptableObjects are located in Resources folders";
                    isProcessing = false;
                    Repaint();
                    return;
                }
                
                // Step 3: Group by groupName and update LocBooks
                currentStep++;
                progressValue = (float)currentStep / totalSteps;
                progressLabel = "Grouping pages and updating LocBooks...";
                Repaint();
                
                // Group pages by groupName (use DEFAULT_LOCBOOK_NAME for empty groupName)
                var groupedData = new Dictionary<string, List<ExtractionPage>>();
                
                foreach (var locbookData in allLocbookData)
                {
                    string groupName = string.IsNullOrEmpty(locbookData.groupName) 
                        ? DEFAULT_LOCBOOK_NAME 
                        : locbookData.groupName;
                    
                    if (!groupedData.ContainsKey(groupName))
                    {
                        groupedData[groupName] = new List<ExtractionPage>();
                    }
                    
                    groupedData[groupName].AddRange(locbookData.pages);
                }
                
                // Update LocBooks for each group
                var updatedLocbooks = new List<LocBook>();
                var updatedPaths = new List<string>();
                int groupIndex = 0;
                int totalGroups = groupedData.Count;
                
                foreach (var kvp in groupedData)
                {
                    string groupName = kvp.Key;
                    List<ExtractionPage> pages = kvp.Value;
                    
                    progressLabel = $"Updating LocBook '{groupName}' ({groupIndex + 1}/{totalGroups})...";
                    Repaint();
                    
                    // Find existing .locbook file
                    string locbookFilePath = FindExistingLocbookFile(groupName);
                    
                    if (string.IsNullOrEmpty(locbookFilePath))
                    {
                        Debug.LogWarning($"[Locbook Extractor] No existing .locbook file found for group '{groupName}'. Skipping update. Use Extract mode to create a new one.");
                        groupIndex++;
                        continue;
                    }
                    
                    // Load existing locbook
                    var existingLocbook = LoadExistingLocbook(locbookFilePath);
                    
                    if (existingLocbook == null)
                    {
                        Debug.LogError($"[Locbook Extractor] Failed to load existing locbook at: {locbookFilePath}");
                        groupIndex++;
                        continue;
                    }
                    
                    // Merge new data with existing locbook
                    var locbookData = new LocbookData(pages, groupName);
                    bool success = UpdateLocBook(existingLocbook, locbookData, updateOriginalValues, updateAbout);
                    
                    if (success)
                    {
                        updatedLocbooks.Add(existingLocbook);
                        updatedPaths.Add(locbookFilePath);
                    }
                    else
                    {
                        Debug.LogError($"[Locbook Extractor] Failed to update LocBook for group '{groupName}'");
                    }
                    
                    groupIndex++;
                }
                
                // Success message
                if (updatedLocbooks.Count > 0)
                {
                    int totalPages = allLocbookData.Sum(ld => ld.pages.Count);
                    int totalFields = allLocbookData.Sum(ld => ld.pages.Sum(p => p.fields.Count));
                    
                    string locbookList = string.Join("\n", updatedPaths.Select(p => $"   • {Path.GetFileName(p)}"));
                    
                    statusMessage = $"Update Complete!\n\n" +
                                  $"Statistics:\n" +
                                  $"   Groups updated: {updatedLocbooks.Count}\n" +
                                  $"   Total pages processed: {totalPages}\n" +
                                  $"   Total text fields processed: {totalFields}\n\n" +
                                  $"Updated LocBooks:\n{locbookList}\n\n" +
                                  $"Note: All translations and keys were preserved.";
                    
                    // Select the last updated asset
                    Selection.activeObject = updatedLocbooks[updatedLocbooks.Count - 1];
                    EditorGUIUtility.PingObject(updatedLocbooks[updatedLocbooks.Count - 1]);
                }
                else
                {
                    statusMessage = "No LocBooks were updated.\n\n" +
                                  "Make sure:\n" +
                                  "• Existing .locbook files exist with matching group names\n" +
                                  "• The files are located in your project's Assets folder";
                }
            }
            catch (System.Exception e)
            {
                statusMessage = $"Error during update:\n{e.Message}\n\nSee console for details.";
                Debug.LogError($"[Locbook Extractor] Error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isProcessing = false;
                progressValue = 1f;
                progressLabel = "Complete";
                Repaint();
            }
        }
        
        /// <summary>
        /// Finds an existing .locbook file by group name.
        /// Searches for files with matching sanitized names.
        /// </summary>
        private string FindExistingLocbookFile(string groupName)
        {
            string sanitizedGroupName = SanitizeFileName(groupName);
            
            // Search for .locbook files in Assets folder
            string[] allLocbookFiles = Directory.GetFiles("Assets", "*.locbook", SearchOption.AllDirectories);
            
            foreach (var filePath in allLocbookFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string sanitizedFileName = SanitizeFileName(fileName);
                
                // Check if the sanitized names match
                if (string.Equals(sanitizedFileName, sanitizedGroupName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return filePath;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Loads an existing LocBook from a .locbook file path.
        /// First tries to find the associated .asset file, then loads the JSON.
        /// </summary>
        private LocBook LoadExistingLocbook(string locbookFilePath)
        {
            if (string.IsNullOrEmpty(locbookFilePath) || !File.Exists(locbookFilePath))
            {
                Debug.LogError($"[Locbook Extractor] .locbook file not found at: {locbookFilePath}");
                return null;
            }
            
            // Try to find the associated .asset file
            string assetPath = Path.ChangeExtension(locbookFilePath, ".asset");
            
            LocBook locbook = null;
            
            // Try loading the asset if it exists
            if (File.Exists(assetPath))
            {
                locbook = AssetDatabase.LoadAssetAtPath<LocBook>(assetPath);
            }
            
            // If no asset exists, create a new one (will be saved in UpdateLocBook)
            if (locbook == null)
            {
                locbook = ScriptableObject.CreateInstance<LocBook>();
            }
            
            // Load the JSON data from the .locbook file
            string json = File.ReadAllText(locbookFilePath);
            locbook.LoadFromJson(json);
            
            // Link the JSON file to the LocBook if not already linked
            if (locbook.LocbookFile == null)
            {
                AssetDatabase.ImportAsset(locbookFilePath);
                var textAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(locbookFilePath);
                locbook.LocbookFile = textAsset;
            }
            
            return locbook;
        }
        
        /// <summary>
        /// Updates an existing LocBook with new extraction data.
        /// Preserves keys, variants, and only updates specified fields.
        /// </summary>
        private bool UpdateLocBook(LocBook existingLocbook, LocbookData newData, bool updateOriginalValues, bool updateAbout)
        {
            if (existingLocbook == null || newData.pages == null)
            {
                return false;
            }
            
            int newEntriesAdded = 0;
            int existingEntriesUpdated = 0;
            
            // Process each page from the new extraction data
            foreach (var extractionPage in newData.pages)
            {
                // Generate page ID if not provided
                string pageId = !string.IsNullOrEmpty(extractionPage.pageId) 
                    ? extractionPage.pageId 
                    : GeneratePageId(extractionPage.pageName);
                
                // Find or create the page in the existing locbook
                LocBook.Page existingPage = existingLocbook.Pages.FirstOrDefault(p => p.pageId == pageId);
                
                if (existingPage == null)
                {
                    // Create new page
                    existingPage = new LocBook.Page
                    {
                        pageId = pageId,
                        aboutPage = extractionPage.about,
                        entries = new List<LocBook.LocalizationEntry>()
                    };
                    existingLocbook.Pages.Add(existingPage);
                }
                else
                {
                    // Update about field if requested
                    if (updateAbout && !string.IsNullOrEmpty(extractionPage.about))
                    {
                        existingPage.aboutPage = extractionPage.about;
                    }
                }
                
                // Process each field from the extraction page
                for (int i = 0; i < extractionPage.fields.Count; i++)
                {
                    var field = extractionPage.fields[i];
                    
                    if (string.IsNullOrEmpty(field.originalValue))
                    {
                        continue;
                    }
                    
                    // Generate key if not provided
                    string key = !string.IsNullOrEmpty(field.key) 
                        ? field.key 
                        : GenerateKey(field.originalValue, i);
                    
                    // Find existing entry by key
                    LocBook.LocalizationEntry existingEntry = existingPage.entries.FirstOrDefault(e => e.key == key);
                    
                    if (existingEntry == null)
                    {
                        // Add new entry
                        var newEntry = new LocBook.LocalizationEntry
                        {
                            key = key,
                            originalValue = field.originalValue,
                            variants = new List<LocBook.LanguageVariant>()
                        };
                        
                        // Add any pre-existing variants from extraction data
                        if (field.variants != null)
                        {
                            foreach (var variant in field.variants)
                            {
                                newEntry.variants.Add(new LocBook.LanguageVariant
                                {
                                    languageCode = variant.languageCode,
                                    value = variant.value
                                });
                            }
                        }
                        
                        existingPage.entries.Add(newEntry);
                        newEntriesAdded++;
                    }
                    else
                    {
                        // Update existing entry
                        if (updateOriginalValues)
                        {
                            existingEntry.originalValue = field.originalValue;
                        }
                        // Variants are always preserved - never overwritten
                        existingEntriesUpdated++;
                    }
                }
            }
            
            // Save the updated locbook
            string assetPath = AssetDatabase.GetAssetPath(existingLocbook);
            string jsonPath = null;
            
            // If no asset path exists, try to find it from the locbook file reference
            if (string.IsNullOrEmpty(assetPath) && existingLocbook.LocbookFile != null)
            {
                jsonPath = AssetDatabase.GetAssetPath(existingLocbook.LocbookFile);
                assetPath = Path.ChangeExtension(jsonPath, ".asset");
            }
            
            // If still no path, we need to save it somewhere
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"[Locbook Extractor] Could not determine asset path for LocBook. Changes may not be saved.");
                return false;
            }
            
            // Update the JSON file
            if (string.IsNullOrEmpty(jsonPath))
            {
                jsonPath = Path.ChangeExtension(assetPath, ".locbook");
            }
            
            string json = existingLocbook.GetExternalJsonData();
            File.WriteAllText(jsonPath, json);
            
            // Create or update the asset
            if (!File.Exists(assetPath))
            {
                AssetDatabase.CreateAsset(existingLocbook, assetPath);
            }
            
            // Link the JSON file to the LocBook
            AssetDatabase.ImportAsset(jsonPath);
            var textAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(jsonPath);
            existingLocbook.LocbookFile = textAsset;
            
            // Save changes
            EditorUtility.SetDirty(existingLocbook);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[Locbook Extractor] Updated LocBook '{newData.groupName}': Added {newEntriesAdded} new entries, Updated {existingEntriesUpdated} existing entries");
            
            return true;
        }
    }
}
#endif
