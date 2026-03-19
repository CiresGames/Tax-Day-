using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using Debug = UnityEngine.Debug;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Editors
{
    /// <summary>
    /// Static helper class for DialogueBook context menu operations (right-click in Project window).
    /// Provides "Open DialogueBooks in Spokesman" functionality, mirroring LocBook's Lingramia integration.
    /// </summary>
    public static class DialogueBookContextHelper
    {
        [MenuItem("Assets/Open DialogueBooks in Spokesman", false, 201)]
        private static void OpenSelectedDialogueBooksInSpokesman()
        {
            var selectedBooks = GetSelectedDialogueBooks();
            
            if (selectedBooks.Count == 0)
            {
                EditorUtility.DisplayDialog("No DialogueBooks Selected", 
                    "Please select one or more DialogueBook assets in the Project window.", 
                    "OK");
                return;
            }
            
            if (!SpokesmanDownloader.IsSpokesmanDownloaded())
            {
                EditorUtility.DisplayDialog("Spokesman Not Installed", 
                    "Spokesman is not installed. Please download it first using:\n" +
                    "Tools > Signalia > Game Systems > Dialogue > Download Spokesman", 
                    "OK");
                return;
            }
            
            var validBooks = new List<DialogueBook>();
            var invalidBooks = new List<string>();
            
            foreach (var book in selectedBooks)
            {
                if (book.DlgbookFile == null)
                {
                    invalidBooks.Add(book.name);
                    continue;
                }
                
                string path = AssetDatabase.GetAssetPath(book.DlgbookFile);
                if (string.IsNullOrEmpty(path))
                {
                    invalidBooks.Add(book.name);
                    continue;
                }
                
                validBooks.Add(book);
            }
            
            if (validBooks.Count == 0)
            {
                string invalidList = invalidBooks.Count > 0 
                    ? $"\n\nDialogueBooks without valid .dlgbook file references:\n{string.Join("\n", invalidBooks)}"
                    : "";
                EditorUtility.DisplayDialog("No Valid DialogueBooks", 
                    "None of the selected DialogueBooks have valid .dlgbook file references." + invalidList, 
                    "OK");
                return;
            }
            
            int openedCount = 0;
            int failedCount = 0;
            
            foreach (var book in validBooks)
            {
                string path = AssetDatabase.GetAssetPath(book.DlgbookFile);
                string fullPath = Path.GetFullPath(path);
                
                bool success = SpokesmanDownloader.LaunchSpokesman(fullPath);
                if (success)
                {
                    openedCount++;
                    Debug.Log($"[Signalia DialogueBook] Opening in Spokesman: {fullPath}");
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"[Signalia DialogueBook] Failed to open in Spokesman: {fullPath}");
                }
            }
            
            if (invalidBooks.Count > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} DialogueBook(s) in Spokesman.\n" +
                    (failedCount > 0 ? $"Failed to open {failedCount} DialogueBook(s).\n" : "") +
                    $"\n{invalidBooks.Count} DialogueBook(s) skipped (no .dlgbook file reference):\n{string.Join("\n", invalidBooks)}", 
                    "OK");
            }
            else if (failedCount > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} DialogueBook(s) in Spokesman.\n" +
                    $"Failed to open {failedCount} DialogueBook(s).", 
                    "OK");
            }
            else if (openedCount > 0)
            {
                Debug.Log($"[Signalia DialogueBook] Successfully opened {openedCount} DialogueBook(s) in Spokesman.");
            }
        }
        
        [MenuItem("Assets/Open DialogueBooks in Spokesman", true)]
        private static bool ValidateOpenSelectedDialogueBooksInSpokesman()
        {
            return GetSelectedDialogueBooks().Count > 0;
        }
        
        private static List<DialogueBook> GetSelectedDialogueBooks()
        {
            var dialogueBooks = new List<DialogueBook>();
            foreach (var obj in Selection.objects)
            {
                if (obj is DialogueBook book)
                {
                    dialogueBooks.Add(book);
                }
            }
            return dialogueBooks;
        }
    }

    [CustomEditor(typeof(DialogueBook)), CanEditMultipleObjects]
    public class DialogueObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty dialogueName;
        private SerializedProperty dialogueContext;
        private SerializedProperty startEvent;
        private SerializedProperty endEvent;
        private SerializedProperty lines;
        private SerializedProperty exitDialogue;
        private SerializedProperty dlgbookFile;

        private int currentTab = 0;
        private readonly string[] tabs = new string[] { "General", "Dialogue Lines", "Tools" };
        
        // Filter / search variables
        private string branchFilter = "All";
        private List<string> availableBranches = new List<string>();
        private string lineSearchTerm = string.Empty;
        
        // Pagination
        private const int LinesPerPage = 10;
        private int linesCurrentPage = 0;
        private List<int> filteredLineIndices = new List<int>();
        
        // Scroll
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            dialogueName = serializedObject.FindProperty("dialogueName");
            dialogueContext = serializedObject.FindProperty("dialogueContext");
            startEvent = serializedObject.FindProperty("startEvent");
            endEvent = serializedObject.FindProperty("endEvent");
            lines = serializedObject.FindProperty("lines");
            exitDialogue = serializedObject.FindProperty("exitDialogue");
            dlgbookFile = serializedObject.FindProperty("dlgbookFile");
            
            RefreshBranches();
            UpdateFilteredLineIndices();
        }

        private void RefreshBranches()
        {
            availableBranches.Clear();
            availableBranches.Add("All");
            
            for (int i = 0; i < lines.arraySize; i++)
            {
                var line = lines.GetArrayElementAtIndex(i);
                var branchName = line.FindPropertyRelative("BranchName").stringValue;
                
                if (string.IsNullOrEmpty(branchName)) branchName = "default";
                
                if (!availableBranches.Contains(branchName))
                {
                    availableBranches.Add(branchName);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Header image (commented out until graphics are ready)
            // GUILayout.BeginHorizontal();
            // GUILayout.FlexibleSpace();
            // EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.DialogueBookHeader);
            // GUILayout.FlexibleSpace();
            // GUILayout.EndHorizontal();
            // GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "DialogueBook stores structured dialogue data including branches, speech lines, and player choices. " +
                "Data can be imported from an external .dlgbook file or authored directly in the inspector.",
                MessageType.Info);
            GUILayout.Space(6);
            
            // Source file bar — always visible at top
            DrawDlgbookBar();
            GUILayout.Space(6);

            // Tabs
            currentTab = EditorUtilityMethods.RenderToolbar(currentTab, tabs, 24);
            GUILayout.Space(6);

            switch (currentTab)
            {
                case 0:
                    DrawGeneralTab();
                    break;
                case 1:
                    DrawLinesTab();
                    break;
                case 2:
                    DrawToolsTab();
                    break;
            }
            
            GUILayout.Space(10);
            
            // Start Dialogue button — prominent, play-mode aware
            DrawStartDialogueSection();

            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Draws the .dlgbook source file field and refresh button, always visible at the top.
        /// </summary>
        private void DrawDlgbookBar()
        {
            EditorGUILayout.LabelField("Source File", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(dlgbookFile, new GUIContent("Source .dlgbook", "Reference to an external .dlgbook file (JSON format) for importing dialogue data"));
            
            DialogueBook targetObj = (DialogueBook)target;
            
            if (targetObj.DlgbookFile != null)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Refresh", GUILayout.Width(80), GUILayout.Height(20)))
                {
                    serializedObject.ApplyModifiedProperties();
                    targetObj.UpdateAssetFromFile();
                    serializedObject.Update();
                    RefreshBranches();
                    UpdateFilteredLineIndices();
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndHorizontal();
            
            if (dlgbookFile.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No .dlgbook file assigned. Drag and drop a .dlgbook file here, or author dialogue directly in the Dialogue Lines tab.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(6);
            
            // Spokesman Integration
            DrawSpokesmanIntegration(targetObj);
        }
        
        /// <summary>
        /// Draws the Spokesman integration section with download, open, and update controls.
        /// </summary>
        private void DrawSpokesmanIntegration(DialogueBook targetObj)
        {
            EditorGUILayout.LabelField("Spokesman Integration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Check if Spokesman is installed
            bool spokesmanInstalled = SpokesmanDownloader.IsSpokesmanDownloaded();
            
            if (!spokesmanInstalled)
            {
                EditorGUILayout.HelpBox("Spokesman is not installed. Download it to use the dialogue book editor.", MessageType.Warning);
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Download & Install Spokesman", GUILayout.Height(35)))
                {
                    SpokesmanDownloadWindow.ShowWindow();
                }
                GUI.backgroundColor = Color.white;
            }
            
            if (dlgbookFile.objectReferenceValue == null)
            {
                // No dlgbook file referenced — offer to generate one
                EditorGUI.BeginDisabledGroup(!spokesmanInstalled);
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Generate .dlgbook", GUILayout.Height(35)))
                {
                    GenerateDlgbook(targetObj);
                }
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
                
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Generate .dlgbook: Creates a new .dlgbook file from this asset's current dialogue data and assigns it as the reference.", 
                                       MessageType.Info);
            }
            else
            {
                // Show Open in Spokesman button when a dlgbook file is referenced
                EditorGUI.BeginDisabledGroup(!spokesmanInstalled);
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Open in Spokesman", GUILayout.Height(35)))
                {
                    // Check if multiple DialogueBooks are selected
                    if (targets.Length > 1)
                    {
                        OpenMultipleDialogueBooksInSpokesman();
                    }
                    else
                    {
                        OpenInSpokesman(targetObj);
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
                
                GUILayout.Space(5);
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Update Asset from .dlgbook File", GUILayout.Height(30)))
                {
                    serializedObject.ApplyModifiedProperties();
                    targetObj.UpdateAssetFromFile();
                    serializedObject.Update();
                    RefreshBranches();
                    UpdateFilteredLineIndices();
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.Space(5);
                
                if (spokesmanInstalled)
                {
                    EditorGUILayout.HelpBox("Open in Spokesman: Launches the Spokesman app with this DialogueBook's file.\n" +
                                           "Update Asset: Deserializes the .dlgbook file into this asset.", 
                                           MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Please download Spokesman to open and edit .dlgbook files.", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Generates a .dlgbook file from the current DialogueBook data and assigns it as the reference.
        /// </summary>
        private void GenerateDlgbook(DialogueBook dialogueBook)
        {
            // Get the directory where this DialogueBook asset is located
            string assetPath = AssetDatabase.GetAssetPath(dialogueBook);
            string directory = Path.GetDirectoryName(assetPath);
            
            // Suggest a filename based on the DialogueBook asset name
            string suggestedFilename = dialogueBook.name + ".dlgbook";
            
            // Open save file dialog
            string savePath = EditorUtility.SaveFilePanel(
                "Generate .dlgbook File",
                directory,
                suggestedFilename,
                "dlgbook"
            );
            
            if (string.IsNullOrEmpty(savePath))
            {
                return; // User cancelled
            }
            
            try
            {
                // Generate JSON from the current DialogueBook data (reflection-based so private fields and Line/Speech/Choice are serialized)
                string json = DialogueObjectParsing.ToJson(dialogueBook, true);
                
                // Write to file
                File.WriteAllText(savePath, json);
                
                // Convert to relative path if inside Assets folder
                string relativePath = savePath;
                string dataPath = Application.dataPath;
                if (savePath.StartsWith(dataPath))
                {
                    relativePath = "Assets" + savePath.Substring(dataPath.Length);
                }
                
                AssetDatabase.Refresh();
                
                // Load and assign the reference
                UnityEngine.Object dlgbookFileRef = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                if (dlgbookFileRef != null)
                {
                    dialogueBook.DlgbookFile = dlgbookFileRef;
                    EditorUtility.SetDirty(dialogueBook);
                    AssetDatabase.SaveAssets();
                    serializedObject.Update();
                    
                    EditorUtility.DisplayDialog("Dlgbook Generated", 
                        $"Successfully generated .dlgbook file at:\n{relativePath}\n\nThe file has been assigned as this DialogueBook's reference.", 
                        "OK");
                    
                    Debug.Log($"[Signalia DialogueBook] Generated .dlgbook file: {relativePath}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Success (Manual Assignment Needed)", 
                        $"Generated .dlgbook file at:\n{savePath}\n\nPlease manually assign it in the inspector if it's outside the Assets folder.", 
                        "OK");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Generation Failed", 
                    $"Failed to generate .dlgbook file:\n{e.Message}", 
                    "OK");
                Debug.LogError($"[Signalia DialogueBook] Generation error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Opens the referenced .dlgbook file in Spokesman, falling back to system default app.
        /// </summary>
        private void OpenInSpokesman(DialogueBook dialogueBook)
        {
            if (dialogueBook.DlgbookFile == null)
            {
                EditorUtility.DisplayDialog("No .dlgbook File", 
                    "Please assign a .dlgbook file reference before opening.", 
                    "OK");
                return;
            }
            
            string path = AssetDatabase.GetAssetPath(dialogueBook.DlgbookFile);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Invalid File", 
                    "Could not get path for the referenced .dlgbook file.", 
                    "OK");
                return;
            }
            
            string fullPath = Path.GetFullPath(path);
            
            // Try to use Spokesman if it's installed
            if (SpokesmanDownloader.IsSpokesmanDownloaded())
            {
                bool success = SpokesmanDownloader.LaunchSpokesman(fullPath);
                
                if (success)
                {
                    Debug.Log($"[Signalia DialogueBook] Opening in Spokesman: {fullPath}");
                    return;
                }
                else
                {
                    // Spokesman is installed but failed to launch - fall through to default app
                    Debug.LogWarning("[Signalia DialogueBook] Failed to launch Spokesman, falling back to system default app.");
                }
            }
            
            // Fall back to system default app
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
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Could Not Open File", 
                    $"Could not open the .dlgbook file with the system default application.\n\nError: {ex.Message}\n\n" +
                    "To use Spokesman (recommended), download it from:\n" +
                    "Tools > Signalia > Game Systems > Dialogue > Download Spokesman", 
                    "OK");
                Debug.LogError($"[Signalia DialogueBook] Failed to open file with default app: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Opens multiple selected DialogueBooks in Spokesman.
        /// </summary>
        private void OpenMultipleDialogueBooksInSpokesman()
        {
            if (!SpokesmanDownloader.IsSpokesmanDownloaded())
            {
                EditorUtility.DisplayDialog("Spokesman Not Installed", 
                    "Spokesman is not installed. Please download it first using:\n" +
                    "Tools > Signalia > Game Systems > Dialogue > Download Spokesman", 
                    "OK");
                return;
            }
            
            var validBooks = new List<DialogueBook>();
            var invalidBooks = new List<string>();
            
            // Get all selected DialogueBooks
            foreach (var t in targets)
            {
                if (t is DialogueBook book)
                {
                    if (book.DlgbookFile == null)
                    {
                        invalidBooks.Add(book.name);
                        continue;
                    }
                    
                    string path = AssetDatabase.GetAssetPath(book.DlgbookFile);
                    if (string.IsNullOrEmpty(path))
                    {
                        invalidBooks.Add(book.name);
                        continue;
                    }
                    
                    validBooks.Add(book);
                }
            }
            
            if (validBooks.Count == 0)
            {
                string invalidList = invalidBooks.Count > 0 
                    ? $"\n\nDialogueBooks without valid .dlgbook file references:\n{string.Join("\n", invalidBooks)}"
                    : "";
                EditorUtility.DisplayDialog("No Valid DialogueBooks", 
                    "None of the selected DialogueBooks have valid .dlgbook file references." + invalidList, 
                    "OK");
                return;
            }
            
            int openedCount = 0;
            int failedCount = 0;
            
            foreach (var book in validBooks)
            {
                string path = AssetDatabase.GetAssetPath(book.DlgbookFile);
                string fullPath = Path.GetFullPath(path);
                
                bool success = SpokesmanDownloader.LaunchSpokesman(fullPath);
                if (success)
                {
                    openedCount++;
                    Debug.Log($"[Signalia DialogueBook] Opening in Spokesman: {fullPath}");
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"[Signalia DialogueBook] Failed to open in Spokesman: {fullPath}");
                }
            }
            
            if (invalidBooks.Count > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} DialogueBook(s) in Spokesman.\n" +
                    (failedCount > 0 ? $"Failed to open {failedCount} DialogueBook(s).\n" : "") +
                    $"\n{invalidBooks.Count} DialogueBook(s) skipped (no .dlgbook file reference):\n{string.Join("\n", invalidBooks)}", 
                    "OK");
            }
            else if (failedCount > 0)
            {
                EditorUtility.DisplayDialog("Open Complete", 
                    $"Opened {openedCount} DialogueBook(s) in Spokesman.\n" +
                    $"Failed to open {failedCount} DialogueBook(s).", 
                    "OK");
            }
            else if (openedCount > 0)
            {
                Debug.Log($"[Signalia DialogueBook] Successfully opened {openedCount} DialogueBook(s) in Spokesman.");
            }
        }

        private void DrawGeneralTab()
        {
            // Metadata Section
            EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(dialogueName, new GUIContent("Dialogue Name", "Unique name used to reference this dialogue"));
            EditorGUILayout.PropertyField(dialogueContext, new GUIContent("Context", "Description or context for this dialogue (e.g., scene, quest)"));
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Events Section
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(startEvent, new GUIContent("Start Event", "Event triggered when dialogue starts"));
            EditorGUILayout.PropertyField(endEvent, new GUIContent("End Event", "Event triggered when dialogue ends"));
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Flow Section
            EditorGUILayout.LabelField("Flow", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(exitDialogue, new GUIContent("Exit Dialogue", "Optional DialogueBook to chain into when this dialogue ends"));
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Statistics Section
            DrawStatisticsSection();
        }
        
        private void DrawStatisticsSection()
        {
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int lineCount = lines.arraySize;
            int speechCount = 0;
            int choiceCount = 0;
            
            for (int i = 0; i < lineCount; i++)
            {
                var line = lines.GetArrayElementAtIndex(i);
                var lineType = line.FindPropertyRelative("LineType");
                if (lineType.enumValueIndex == 0)
                    speechCount++;
                else
                    choiceCount++;
            }
            
            RefreshBranches();
            int branchCount = availableBranches.Count - 1; // subtract "All"
            
            EditorGUILayout.LabelField($"Total Lines: {lineCount}");
            EditorGUILayout.LabelField($"Speech Lines: {speechCount}");
            EditorGUILayout.LabelField($"Choice Lines: {choiceCount}");
            EditorGUILayout.LabelField($"Branches: {branchCount}");
            
            if (branchCount > 0)
            {
                string branchNames = string.Join(", ", availableBranches.GetRange(1, branchCount));
                EditorGUILayout.LabelField($"Branch Names: {branchNames}", EditorStyles.miniLabel);
            }
            
            bool hasExit = exitDialogue.objectReferenceValue != null;
            string exitLabel = hasExit ? exitDialogue.objectReferenceValue.name : "None";
            EditorGUILayout.LabelField($"Exit Dialogue: {exitLabel}");
            
            EditorGUILayout.EndVertical();
        }

        private void DrawLinesTab()
        {
            // Branch Filtering + Search
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Branch filter row
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Branch:", GUILayout.Width(55));
            
            RefreshBranches();
            
            int currentIndex = availableBranches.IndexOf(branchFilter);
            if (currentIndex < 0) currentIndex = 0;
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(currentIndex, availableBranches.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                branchFilter = availableBranches[newIndex];
                linesCurrentPage = 0;
                UpdateFilteredLineIndices();
            }
            GUILayout.EndHorizontal();
            
            // Search row
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(55));
            EditorGUI.BeginChangeCheck();
            lineSearchTerm = EditorGUILayout.TextField(lineSearchTerm, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                linesCurrentPage = 0;
                UpdateFilteredLineIndices();
            }
            
            if (!string.IsNullOrEmpty(lineSearchTerm))
            {
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    lineSearchTerm = string.Empty;
                    GUI.FocusControl(null);
                    linesCurrentPage = 0;
                    UpdateFilteredLineIndices();
                }
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Update filtered indices
            UpdateFilteredLineIndices();
            int totalFiltered = filteredLineIndices.Count;
            
            // Pagination toolbar
            if (totalFiltered > LinesPerPage)
            {
                DrawPaginationToolbar(totalFiltered);
            }
            else if (totalFiltered > 0)
            {
                EditorGUILayout.LabelField($"Showing {totalFiltered} of {lines.arraySize} line(s)", EditorStyles.miniLabel);
            }
            
            GUILayout.Space(5);

            // Lines List — scrollable
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(200), GUILayout.MaxHeight(500));
            
            if (lines.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No dialogue lines yet. Add a line below or import from a .dlgbook file.", MessageType.Info);
            }
            else if (totalFiltered == 0)
            {
                EditorGUILayout.HelpBox("No lines match the current filter/search.", MessageType.Info);
            }
            else
            {
                int startIdx = linesCurrentPage * LinesPerPage;
                int endIdx = Mathf.Min(startIdx + LinesPerPage, totalFiltered);
                
                for (int displayIdx = startIdx; displayIdx < endIdx; displayIdx++)
                {
                    int actualIdx = filteredLineIndices[displayIdx];
                    SerializedProperty line = lines.GetArrayElementAtIndex(actualIdx);

                    if (DrawLine(line, actualIdx))
                    {
                        // Item was deleted — refresh and break out of draw loop
                        RefreshBranches();
                        UpdateFilteredLineIndices();
                        break;
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            
            // Add line button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+ Add New Line", GUILayout.Height(30)))
            {
                lines.arraySize++;
                var newLine = lines.GetArrayElementAtIndex(lines.arraySize - 1);
                ResetLineDefaults(newLine);
                RefreshBranches();
                UpdateFilteredLineIndices();
                
                // Jump to last page to see the new line
                int newTotal = filteredLineIndices.Count;
                linesCurrentPage = Mathf.Max(0, Mathf.CeilToInt(newTotal / (float)LinesPerPage) - 1);
            }
            GUI.backgroundColor = Color.white;
        }
        
        private void DrawPaginationToolbar(int totalEntries)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)LinesPerPage));
            linesCurrentPage = Mathf.Clamp(linesCurrentPage, 0, totalPages - 1);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            using (new EditorGUI.DisabledScope(linesCurrentPage <= 0))
            {
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    linesCurrentPage = Mathf.Max(0, linesCurrentPage - 1);
                }
            }
            
            using (new EditorGUI.DisabledScope(linesCurrentPage >= totalPages - 1))
            {
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    linesCurrentPage = Mathf.Min(totalPages - 1, linesCurrentPage + 1);
                }
            }
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField(
                $"Page {linesCurrentPage + 1} of {totalPages}  ·  {totalEntries} of {lines.arraySize} line(s)",
                EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void UpdateFilteredLineIndices()
        {
            filteredLineIndices.Clear();
            
            string searchLower = string.IsNullOrEmpty(lineSearchTerm) ? "" : lineSearchTerm.ToLowerInvariant();
            
            for (int i = 0; i < lines.arraySize; i++)
            {
                SerializedProperty line = lines.GetArrayElementAtIndex(i);
                SerializedProperty branchNameProp = line.FindPropertyRelative("BranchName");
                string branchName = string.IsNullOrEmpty(branchNameProp.stringValue) ? "default" : branchNameProp.stringValue;
                
                // Branch filter
                if (branchFilter != "All" && branchName != branchFilter)
                    continue;
                
                // Text search (search speaker, branch, speech text, choice text)
                if (!string.IsNullOrEmpty(searchLower))
                {
                    bool matchFound = false;
                    
                    string speaker = line.FindPropertyRelative("SpeakerName").stringValue ?? "";
                    if (speaker.ToLowerInvariant().Contains(searchLower)) matchFound = true;
                    
                    if (!matchFound && branchName.ToLowerInvariant().Contains(searchLower)) matchFound = true;
                    
                    if (!matchFound)
                    {
                        var speech = line.FindPropertyRelative("Speech");
                        if (speech != null)
                        {
                            string speechText = speech.FindPropertyRelative("speechText").stringValue ?? "";
                            if (speechText.ToLowerInvariant().Contains(searchLower)) matchFound = true;
                        }
                    }
                    
                    if (!matchFound)
                    {
                        var choices = line.FindPropertyRelative("Choices");
                        if (choices != null)
                        {
                            for (int c = 0; c < choices.arraySize; c++)
                            {
                                string choiceText = choices.GetArrayElementAtIndex(c).FindPropertyRelative("choiceText").stringValue ?? "";
                                if (choiceText.ToLowerInvariant().Contains(searchLower))
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (!matchFound) continue;
                }
                
                filteredLineIndices.Add(i);
            }
            
            int totalPages = filteredLineIndices.Count == 0 ? 1 : Mathf.CeilToInt(filteredLineIndices.Count / (float)LinesPerPage);
            linesCurrentPage = Mathf.Clamp(linesCurrentPage, 0, Mathf.Max(0, totalPages - 1));
        }

        private void ResetLineDefaults(SerializedProperty line)
        {
            line.FindPropertyRelative("BranchName").stringValue = "default";
            line.FindPropertyRelative("SpeakerName").stringValue = "";
            line.FindPropertyRelative("LineType").enumValueIndex = 0; // Speech
            line.FindPropertyRelative("ExitBranch").stringValue = "";
            
            // Reset Speech
            var speech = line.FindPropertyRelative("Speech");
            speech.FindPropertyRelative("speechText").stringValue = "";
            speech.FindPropertyRelative("animationOverride").stringValue = "";
            speech.FindPropertyRelative("audioEvent").stringValue = "";
            
            // Reset Choices
            line.FindPropertyRelative("Choices").arraySize = 0;
        }

        private bool DrawLine(SerializedProperty line, int index)
        {
            bool deleted = false;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            SerializedProperty lineType = line.FindPropertyRelative("LineType");
            SerializedProperty speakerName = line.FindPropertyRelative("SpeakerName");
            SerializedProperty branchName = line.FindPropertyRelative("BranchName");
            
            string branch = string.IsNullOrEmpty(branchName.stringValue) ? "default" : branchName.stringValue;
            string speaker = string.IsNullOrEmpty(speakerName.stringValue) ? "???" : speakerName.stringValue;
            bool isSpeech = lineType.enumValueIndex == 0;
            string typeIcon = isSpeech ? "💬" : "🔀";
            string typeName = lineType.enumDisplayNames[lineType.enumValueIndex];
            
            // Header row
            GUILayout.BeginHorizontal();
            
            string label = $"{typeIcon} Line {index}  [{branch}]  {speaker} — {typeName}";
            line.isExpanded = EditorGUILayout.Foldout(line.isExpanded, label, true);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog("Delete Line",
                    $"Delete Line {index} ({typeName} by {speaker} in [{branch}])?",
                    "Delete", "Cancel"))
                {
                    lines.DeleteArrayElementAtIndex(index);
                    deleted = true;
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.EndHorizontal();

            if (deleted)
            {
                EditorGUILayout.EndVertical();
                return true;
            }

            if (line.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.Space(3);
                
                // Core Properties
                EditorGUILayout.LabelField("Core", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(branchName, new GUIContent("Branch", "Which dialogue branch this line belongs to"));
                EditorGUILayout.PropertyField(speakerName, new GUIContent("Speaker", "Name of the character speaking"));
                EditorGUILayout.PropertyField(lineType, new GUIContent("Type", "Speech (single line) or Choice (player picks)"));
                EditorGUILayout.PropertyField(line.FindPropertyRelative("ExitBranch"), new GUIContent("Exit Branch", "Branch to jump to after this line (leave empty to continue sequentially)"));
                EditorGUILayout.PropertyField(line.FindPropertyRelative("LineEvent"), new GUIContent("Line Event", "Event triggered when this line is reached"));
                EditorGUILayout.EndVertical();

                GUILayout.Space(5);

                // Type Specific Drawing
                if (isSpeech)
                {
                    DrawSpeech(line.FindPropertyRelative("Speech"));
                }
                else
                {
                    DrawChoices(line.FindPropertyRelative("Choices"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
            return false;
        }

        private void DrawSpeech(SerializedProperty speech)
        {
            EditorGUILayout.LabelField("💬 Speech Content", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.PropertyField(speech.FindPropertyRelative("speechText"), new GUIContent("Text", "The dialogue text displayed to the player"));
            
            // Audio event with search helper
            SerializedProperty audioEvent = speech.FindPropertyRelative("audioEvent");
            Rect audioRect = EditorGUILayout.GetControlRect();
            PropertyHelpers.DrawAudioDropdownInline(audioRect, "Audio Event", audioEvent, serializedObject);

            EditorGUILayout.PropertyField(speech.FindPropertyRelative("animationOverride"), new GUIContent("Animation", "Animation override for the speaker during this line"));
            EditorGUILayout.PropertyField(speech.FindPropertyRelative("newSaveKey"), new GUIContent("Save Key", "Optional save key set when this line plays"));
            
            EditorGUILayout.EndVertical();
        }

        private void DrawChoices(SerializedProperty choices)
        {
            EditorGUILayout.LabelField($"🔀 Choices ({choices.arraySize})", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (choices.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No choices defined. Add at least one choice for the player.", MessageType.Warning);
            }
            
            for (int i = 0; i < choices.arraySize; i++)
            {
                SerializedProperty choice = choices.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");
                
                GUILayout.BeginHorizontal();
                
                string choiceText = choice.FindPropertyRelative("choiceText").stringValue;
                string choiceLabel = string.IsNullOrEmpty(choiceText) ? $"Choice {i + 1}" : choiceText;
                if (choiceLabel.Length > 40) choiceLabel = choiceLabel.Substring(0, 37) + "...";
                
                EditorGUILayout.LabelField($"  {i + 1}. {choiceLabel}", EditorStyles.miniBoldLabel);
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(16)))
                {
                    if (EditorUtility.DisplayDialog("Delete Choice",
                        $"Delete Choice {i + 1}?", "Delete", "Cancel"))
                    {
                        choices.DeleteArrayElementAtIndex(i);
                        GUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(choice.FindPropertyRelative("choiceText"), new GUIContent("Text", "The choice text shown to the player"));
                EditorGUILayout.PropertyField(choice.FindPropertyRelative("branchToSwitchTo"), new GUIContent("Branch Target", "Branch to switch to when this choice is picked"));
                EditorGUILayout.PropertyField(choice.FindPropertyRelative("choiceEvent"), new GUIContent("Event", "Event triggered when this choice is selected"));
                EditorGUILayout.PropertyField(choice.FindPropertyRelative("newSaveKey"), new GUIContent("Save Key", "Optional save key set when this choice is picked"));
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.Space(3);
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("+ Add Choice", GUILayout.Height(25)))
            {
                choices.arraySize++;
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }

        private void DrawToolsTab()
        {
            EditorGUILayout.HelpBox("Development and debugging tools for this DialogueBook.", MessageType.Info);
            GUILayout.Space(5);
            
            // Debug Section
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DialogueBook targetObj = (DialogueBook)target;

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Log Size", GUILayout.Height(25)))
            {
                targetObj.SizeLog();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Mock Data Section
            EditorGUILayout.LabelField("Mock Data Generators", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Generate sample data for testing. This will replace all existing lines.", MessageType.Warning);
            GUILayout.Space(3);
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Fill with Mock Speech (Neutral)", GUILayout.Height(25)))
            {
                if (lines.arraySize == 0 || EditorUtility.DisplayDialog("Replace Lines",
                    "This will replace all existing lines with mock speech data. Continue?", "Replace", "Cancel"))
                {
                    targetObj.FillWithMock("neutral_encounter", false);
                    EditorUtility.SetDirty(targetObj);
                    serializedObject.Update();
                    RefreshBranches();
                    UpdateFilteredLineIndices();
                }
            }
            
            if (GUILayout.Button("Fill with Mock Choice (Neutral)", GUILayout.Height(25)))
            {
                if (lines.arraySize == 0 || EditorUtility.DisplayDialog("Replace Lines",
                    "This will replace all existing lines with mock choice data. Continue?", "Replace", "Cancel"))
                {
                    targetObj.FillWithMock("neutral_encounter", true);
                    EditorUtility.SetDirty(targetObj);
                    serializedObject.Update();
                    RefreshBranches();
                    UpdateFilteredLineIndices();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(5);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All Lines", GUILayout.Height(25)))
            {
                if (lines.arraySize == 0 || EditorUtility.DisplayDialog("Clear Lines",
                    $"This will delete all {lines.arraySize} lines. This cannot be undone. Continue?",
                    "Clear", "Cancel"))
                {
                    lines.arraySize = 0;
                    RefreshBranches();
                    UpdateFilteredLineIndices();
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStartDialogueSection()
        {
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to start this dialogue.", MessageType.Info);
                
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Start Dialogue (Play Mode Only)", GUILayout.Height(30));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Start Dialogue", GUILayout.Height(30)))
                {
                    var targetObj = (DialogueBook)target;
                    targetObj.StartDialogue();
                }
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
