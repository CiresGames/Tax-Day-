#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;
using System.Text.RegularExpressions;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
	/// <summary>
	/// Context menu items for TMP_Text components to quickly localize UI text.
	/// Provides two workflows: fast localization and user-directed organization.
	/// </summary>
	public static class TMPTextLocalizationContextMenu
	{
		private const string SCENE_TMPS_LOCBOOK_NAME = "Scene TMPs";
		
		[MenuItem("CONTEXT/TMP_Text/Localization/Localize", false, 1000)]
		private static void LocalizeTMPText(MenuCommand command)
		{
			TMP_Text tmpText = command.context as TMP_Text;
			if (tmpText == null)
			{
				Debug.LogError("[Signalia TMP Localization] Failed to get TMP_Text component.");
				return;
			}
			
			string originalText = tmpText.text;
			if (string.IsNullOrEmpty(originalText))
			{
				EditorUtility.DisplayDialog("No Text Found", 
					"The TMP_Text component has no text to localize.", 
					"OK");
				return;
			}
			
			// Get active scene name
			string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			if (string.IsNullOrEmpty(sceneName))
			{
				sceneName = "DefaultScene";
			}
			
			// Find or create the "Scene TMPs" locbook
			LocBook locbook = FindOrCreateLocBook(SCENE_TMPS_LOCBOOK_NAME);
			if (locbook == null)
			{
				EditorUtility.DisplayDialog("Error", 
					"Failed to find or create the 'Scene TMPs' LocBook.", 
					"OK");
				return;
			}
			
			// Generate sanitized key (need to check existing entries first)
			string key = SanitizeKey(originalText, locbook);
			
			// Add entry to the .locbook file
			bool success = AddEntryToLocbookFile(locbook, sceneName, key, originalText);
			
			if (!success)
			{
				EditorUtility.DisplayDialog("Error", 
					$"Failed to add localization entry. Key '{key}' may already exist.", 
					"OK");
				return;
			}
			
			// Add or update SimpleLocalizedText component
			GameObject gameObject = tmpText.gameObject;
			SimpleLocalizedText localizedText = gameObject.GetComponent<SimpleLocalizedText>();
			
			if (localizedText == null)
			{
				Undo.AddComponent<SimpleLocalizedText>(gameObject);
				localizedText = gameObject.GetComponent<SimpleLocalizedText>();
			}
			else
			{
				Undo.RecordObject(localizedText, "Set Localization Key");
			}
			
			if (localizedText != null)
			{
				localizedText.SetKey(key);
				EditorUtility.SetDirty(localizedText);
			}
			
			AssetDatabase.SaveAssets();
			EditorUtility.DisplayDialog("Localized", 
				$"Text localized successfully!\n\nKey: {key}\nLocBook: {SCENE_TMPS_LOCBOOK_NAME}\nPage: {sceneName}", 
				"OK");
		}
		
		[MenuItem("CONTEXT/TMP_Text/Localization/Localize Into Page", false, 1001)]
		private static void LocalizeTMPTextIntoPage(MenuCommand command)
		{
			TMP_Text tmpText = command.context as TMP_Text;
			if (tmpText == null)
			{
				Debug.LogError("[Signalia TMP Localization] Failed to get TMP_Text component.");
				return;
			}
			
			string originalText = tmpText.text;
			if (string.IsNullOrEmpty(originalText))
			{
				EditorUtility.DisplayDialog("No Text Found", 
					"The TMP_Text component has no text to localize.", 
					"OK");
				return;
			}
			
			// Show dialog to select LocBook and Page
			LocBookPageSelectionWindow.ShowWindow(tmpText, originalText);
		}
		
		/// <summary>
		/// Finds an existing LocBook by name or creates a new one.
		/// </summary>
		private static LocBook FindOrCreateLocBook(string locbookName)
		{
			// Search for existing LocBook
			string[] guids = AssetDatabase.FindAssets("t:LocBook");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				LocBook locbook = AssetDatabase.LoadAssetAtPath<LocBook>(path);
				if (locbook != null && locbook.name == locbookName)
				{
					return locbook;
				}
			}
			
			// Create new LocBook
			string defaultPath = $"Assets/Resources/Signalia/Game Systems/Localizations/{locbookName}.asset";
			string directory = System.IO.Path.GetDirectoryName(defaultPath).Replace("\\", "/");
			
			// Ensure directory exists
			if (!AssetDatabase.IsValidFolder(directory))
			{
				string[] folders = directory.Split('/');
				string currentPath = "";
				foreach (string folder in folders)
				{
					if (string.IsNullOrEmpty(folder)) continue;
					string newPath = currentPath + (currentPath.Length > 0 ? "/" : "") + folder;
					if (!AssetDatabase.IsValidFolder(newPath))
					{
						string parentPath = currentPath.Length > 0 ? currentPath : "Assets";
						AssetDatabase.CreateFolder(parentPath, folder);
					}
					currentPath = newPath;
				}
			}
			
			LocBook newLocbook = ScriptableObject.CreateInstance<LocBook>();
			AssetDatabase.CreateAsset(newLocbook, defaultPath);
			AssetDatabase.SaveAssets();
			
			// Create the .locbook file for the new LocBook
			GetOrCreateLocbookFilePath(newLocbook);
			
			return newLocbook;
		}
		
		/// <summary>
		/// Adds an entry to the .locbook file (the main data source).
		/// Creates the file if it doesn't exist, then updates the LocBook asset from the file.
		/// </summary>
		public static bool AddEntryToLocbookFile(LocBook locbook, string pageId, string key, string originalValue)
		{
			if (locbook == null || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(originalValue))
			{
				return false;
			}
			
			// Get or create the .locbook file path
			string locbookFilePath = GetOrCreateLocbookFilePath(locbook);
			if (string.IsNullOrEmpty(locbookFilePath))
			{
				Debug.LogError("[Signalia TMP Localization] Failed to get or create .locbook file path.");
				return false;
			}
			
			// Read existing JSON or create new structure
			LocBook.ExternalLocBookData externalData;
			
			if (System.IO.File.Exists(locbookFilePath))
			{
				try
				{
					string json = System.IO.File.ReadAllText(locbookFilePath);
					externalData = JsonUtility.FromJson<LocBook.ExternalLocBookData>(json);
					
					// If parsing failed, create new structure
					if (externalData == null || externalData.pages == null)
					{
						externalData = new LocBook.ExternalLocBookData
						{
							pages = new System.Collections.Generic.List<LocBook.ExternalPage>()
						};
					}
				}
				catch (System.Exception e)
				{
					Debug.LogWarning($"[Signalia TMP Localization] Error reading .locbook file: {e.Message}. Creating new structure.");
					externalData = new LocBook.ExternalLocBookData
					{
						pages = new System.Collections.Generic.List<LocBook.ExternalPage>()
					};
				}
			}
			else
			{
				externalData = new LocBook.ExternalLocBookData
				{
					pages = new System.Collections.Generic.List<LocBook.ExternalPage>()
				};
			}
			
			// Check if key already exists across all pages
			foreach (var page in externalData.pages)
			{
				if (page.pageFiles != null)
				{
					foreach (var pageFile in page.pageFiles)
					{
						if (pageFile.key == key)
						{
							Debug.LogWarning($"[Signalia TMP Localization] Key '{key}' already exists in .locbook file.");
							return false;
						}
					}
				}
			}
			
			// Find or create the page
			LocBook.ExternalPage targetPage = externalData.pages.FirstOrDefault(p => p.pageId == pageId);
			if (targetPage == null)
			{
				targetPage = new LocBook.ExternalPage
				{
					pageId = pageId,
					aboutPage = $"Auto-created page: {pageId}",
					pageFiles = new System.Collections.Generic.List<LocBook.ExternalPageFile>()
				};
				externalData.pages.Add(targetPage);
			}
			
			if (targetPage.pageFiles == null)
			{
				targetPage.pageFiles = new System.Collections.Generic.List<LocBook.ExternalPageFile>();
			}
			
			// Add the new entry
			var newPageFile = new LocBook.ExternalPageFile
			{
				key = key,
				originalValue = originalValue,
				variants = new System.Collections.Generic.List<LocBook.ExternalVariant>()
			};
			
			targetPage.pageFiles.Add(newPageFile);
			
			// Write JSON back to file
			try
			{
				string json = JsonUtility.ToJson(externalData, true);
				System.IO.File.WriteAllText(locbookFilePath, json);
				
				// Refresh asset database to detect the change
				AssetDatabase.Refresh();
				
				// Update the LocBook asset from the file
				Undo.RecordObject(locbook, "Add Localization Entry to .locbook File");
				locbook.UpdateAssetFromFile();
				
				return true;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[Signalia TMP Localization] Error writing to .locbook file: {e.Message}");
				return false;
			}
		}
		
		/// <summary>
		/// Gets the .locbook file path for a LocBook, or creates it if it doesn't exist.
		/// </summary>
		private static string GetOrCreateLocbookFilePath(LocBook locbook)
		{
			if (locbook == null)
			{
				return null;
			}
			
			// Check if LocBook already has a .locbook file reference
			if (locbook.LocbookFile != null)
			{
				string path = AssetDatabase.GetAssetPath(locbook.LocbookFile);
				if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
				{
					return path;
				}
			}
			
			// Create .locbook file next to the LocBook asset
			string assetPath = AssetDatabase.GetAssetPath(locbook);
			if (string.IsNullOrEmpty(assetPath))
			{
				// LocBook hasn't been saved yet, need to save it first
				string defaultPath = $"Assets/Resources/Signalia/Game Systems/Localizations/{locbook.name}.asset";
				string directory = System.IO.Path.GetDirectoryName(defaultPath).Replace("\\", "/");
				
				// Ensure directory exists
				if (!AssetDatabase.IsValidFolder(directory))
				{
					string[] folders = directory.Split('/');
					string currentPath = "";
					foreach (string folder in folders)
					{
						if (string.IsNullOrEmpty(folder)) continue;
						string newPath = currentPath + (currentPath.Length > 0 ? "/" : "") + folder;
						if (!AssetDatabase.IsValidFolder(newPath))
						{
							string parentPath = currentPath.Length > 0 ? currentPath : "Assets";
							AssetDatabase.CreateFolder(parentPath, folder);
						}
						currentPath = newPath;
					}
				}
				
				// Save the LocBook asset
				AssetDatabase.CreateAsset(locbook, defaultPath);
				AssetDatabase.SaveAssets();
				assetPath = defaultPath;
			}
			
			// Create .locbook file path
			string locbookFilePath = System.IO.Path.ChangeExtension(assetPath, ".locbook");
			
			// If file doesn't exist, create it with empty structure
			if (!System.IO.File.Exists(locbookFilePath))
			{
				var emptyData = new LocBook.ExternalLocBookData
				{
					pages = new System.Collections.Generic.List<LocBook.ExternalPage>()
				};
				
				string json = JsonUtility.ToJson(emptyData, true);
				System.IO.File.WriteAllText(locbookFilePath, json);
				
				// Import and link the file
				AssetDatabase.Refresh();
				UnityEngine.Object locbookFileRef = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(locbookFilePath);
				if (locbookFileRef != null)
				{
					locbook.LocbookFile = locbookFileRef;
					EditorUtility.SetDirty(locbook);
					AssetDatabase.SaveAssets();
				}
			}
			
			return locbookFilePath;
		}
		
		/// <summary>
		/// Sanitizes text to create a valid localization key.
		/// Rules: lowercase, spaces to underscores, remove non-alphanumeric except underscores.
		/// </summary>
		public static string SanitizeKey(string text, LocBook locbook)
		{
			if (string.IsNullOrEmpty(text))
			{
				return "empty_text";
			}
			
			// Convert to lowercase
			string key = text.ToLower();
			
			// Replace spaces with underscores
			key = key.Replace(" ", "_");
			
			// Remove non-alphanumeric characters except underscores
			key = Regex.Replace(key, @"[^a-z0-9_]", "");
			
			// Remove multiple consecutive underscores
			key = Regex.Replace(key, @"_+", "_");
			
			// Remove leading/trailing underscores
			key = key.Trim('_');
			
			// Ensure it's not empty
			if (string.IsNullOrEmpty(key))
			{
				key = "text_entry";
			}
			
			// Limit length
			if (key.Length > 50)
			{
				key = key.Substring(0, 50);
			}
			
			// Check for uniqueness in the .locbook file
			string baseKey = key;
			int counter = 1;
			
			// Read the .locbook file to check for existing keys
			string locbookFilePath = GetOrCreateLocbookFilePath(locbook);
			if (!string.IsNullOrEmpty(locbookFilePath) && System.IO.File.Exists(locbookFilePath))
			{
				try
				{
					string json = System.IO.File.ReadAllText(locbookFilePath);
					var externalData = JsonUtility.FromJson<LocBook.ExternalLocBookData>(json);
					
					if (externalData != null && externalData.pages != null)
					{
						while (externalData.pages.Any(p => p.pageFiles != null && p.pageFiles.Any(pf => pf.key == key)))
						{
							key = $"{baseKey}_{counter}";
							counter++;
						}
					}
				}
				catch
				{
					// If we can't read the file, just use the base key
				}
			}
			
			return key;
		}
	}
	
	/// <summary>
	/// Window for selecting LocBook and Page when localizing TMP_Text.
	/// </summary>
	public class LocBookPageSelectionWindow : EditorWindow
	{
		private TMP_Text tmpText;
		private string originalText;
		private LocBook[] availableLocBooks;
		private int selectedLocBookIndex = 0;
		private string[] availablePageIds;
		private int selectedPageIndex = 0;
		private string newPageId = "";
		private bool createNewPage = false;
		private Vector2 scrollPosition;
		
		public static void ShowWindow(TMP_Text tmpText, string originalText)
		{
			LocBookPageSelectionWindow window = GetWindow<LocBookPageSelectionWindow>(true, "Localize Into Page");
			window.tmpText = tmpText;
			window.originalText = originalText;
			window.minSize = new Vector2(400, 300);
			window.LoadLocBooks();
		}
		
		private void LoadLocBooks()
		{
			string[] guids = AssetDatabase.FindAssets("t:LocBook");
			availableLocBooks = new LocBook[guids.Length];
			
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				availableLocBooks[i] = AssetDatabase.LoadAssetAtPath<LocBook>(path);
			}
			
			if (availableLocBooks.Length > 0)
			{
				UpdatePageList();
			}
		}
		
		private void UpdatePageList()
		{
			if (selectedLocBookIndex >= 0 && selectedLocBookIndex < availableLocBooks.Length)
			{
				LocBook selectedLocBook = availableLocBooks[selectedLocBookIndex];
				if (selectedLocBook != null)
				{
					availablePageIds = selectedLocBook.Pages.Select(p => p.pageId).ToArray();
					if (availablePageIds.Length == 0)
					{
						availablePageIds = new string[] { "(No pages)" };
					}
					selectedPageIndex = 0;
				}
			}
		}
		
		private void OnGUI()
		{
			EditorGUILayout.Space(10);
			
			EditorGUILayout.LabelField("Localize TMP_Text", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox($"Text to localize: \"{originalText}\"", MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// LocBook selection
			EditorGUILayout.LabelField("Select LocBook", EditorStyles.boldLabel);
			if (availableLocBooks == null || availableLocBooks.Length == 0)
			{
				EditorGUILayout.HelpBox("No LocBooks found in project. Please create a LocBook first.", MessageType.Warning);
				if (GUILayout.Button("Create New LocBook"))
				{
					CreateNewLocBook();
				}
				return;
			}
			
			int newLocBookIndex = EditorGUILayout.Popup("LocBook", selectedLocBookIndex, 
				availableLocBooks.Select(lb => lb != null ? lb.name : "Null").ToArray());
			
			if (newLocBookIndex != selectedLocBookIndex)
			{
				selectedLocBookIndex = newLocBookIndex;
				UpdatePageList();
			}
			
			EditorGUILayout.Space(10);
			
			// Page selection
			EditorGUILayout.LabelField("Select Page", EditorStyles.boldLabel);
			createNewPage = EditorGUILayout.Toggle("Create New Page", createNewPage);
			
			if (createNewPage)
			{
				newPageId = EditorGUILayout.TextField("Page ID", newPageId);
				if (string.IsNullOrEmpty(newPageId))
				{
					EditorGUILayout.HelpBox("Please enter a Page ID.", MessageType.Warning);
				}
			}
			else
			{
				if (availablePageIds != null && availablePageIds.Length > 0)
				{
					selectedPageIndex = EditorGUILayout.Popup("Page", selectedPageIndex, availablePageIds);
				}
				else
				{
					EditorGUILayout.HelpBox("No pages in selected LocBook. Create a new page.", MessageType.Info);
					createNewPage = true;
				}
			}
			
			EditorGUILayout.Space(20);
			
			// Buttons
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Cancel", GUILayout.Height(30)))
			{
				Close();
			}
			
			GUI.enabled = CanLocalize();
			if (GUILayout.Button("Localize", GUILayout.Height(30)))
			{
				PerformLocalization();
			}
			GUI.enabled = true;
			
			EditorGUILayout.EndHorizontal();
		}
		
		private bool CanLocalize()
		{
			if (availableLocBooks == null || selectedLocBookIndex < 0 || selectedLocBookIndex >= availableLocBooks.Length)
			{
				return false;
			}
			
			if (createNewPage)
			{
				return !string.IsNullOrEmpty(newPageId);
			}
			else
			{
				return availablePageIds != null && availablePageIds.Length > 0;
			}
		}
		
		private void PerformLocalization()
		{
			if (tmpText == null || availableLocBooks == null || selectedLocBookIndex < 0 || selectedLocBookIndex >= availableLocBooks.Length)
			{
				return;
			}
			
			LocBook locbook = availableLocBooks[selectedLocBookIndex];
			if (locbook == null)
			{
				return;
			}
			
			string pageId;
			if (createNewPage)
			{
				pageId = newPageId;
			}
			else
			{
				if (availablePageIds == null || selectedPageIndex < 0 || selectedPageIndex >= availablePageIds.Length)
				{
					return;
				}
				pageId = availablePageIds[selectedPageIndex];
			}
			
			// Generate sanitized key (need to check existing entries first)
			string key = TMPTextLocalizationContextMenu.SanitizeKey(originalText, locbook);
			
			// Add entry to the .locbook file
			bool success = TMPTextLocalizationContextMenu.AddEntryToLocbookFile(locbook, pageId, key, originalText);
			
			if (!success)
			{
				EditorUtility.DisplayDialog("Error", 
					$"Failed to add localization entry. Key '{key}' may already exist.", 
					"OK");
				return;
			}
			
			// Add or update SimpleLocalizedText component
			GameObject gameObject = tmpText.gameObject;
			SimpleLocalizedText localizedText = gameObject.GetComponent<SimpleLocalizedText>();
			
			if (localizedText == null)
			{
				Undo.AddComponent<SimpleLocalizedText>(gameObject);
				localizedText = gameObject.GetComponent<SimpleLocalizedText>();
			}
			else
			{
				Undo.RecordObject(localizedText, "Set Localization Key");
			}
			
			if (localizedText != null)
			{
				localizedText.SetKey(key);
				EditorUtility.SetDirty(localizedText);
			}
			
			AssetDatabase.SaveAssets();
			
			EditorUtility.DisplayDialog("Localized", 
				$"Text localized successfully!\n\nKey: {key}\nLocBook: {locbook.name}\nPage: {pageId}", 
				"OK");
			
			Close();
		}
		
		private void CreateNewLocBook()
		{
			string path = EditorUtility.SaveFilePanelInProject(
				"Create LocBook",
				"NewLocBook",
				"asset",
				"Choose where to save the new LocBook asset");
			
			if (!string.IsNullOrEmpty(path))
			{
				var locBook = ScriptableObject.CreateInstance<LocBook>();
				AssetDatabase.CreateAsset(locBook, path);
				AssetDatabase.SaveAssets();
				
				LoadLocBooks();
				
				// Select the newly created LocBook
				for (int i = 0; i < availableLocBooks.Length; i++)
				{
					if (AssetDatabase.GetAssetPath(availableLocBooks[i]) == path)
					{
						selectedLocBookIndex = i;
						UpdatePageList();
						break;
					}
				}
			}
		}
	}
}
#endif

