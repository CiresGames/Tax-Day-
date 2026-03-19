using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.GameSystems.ResourceCaching.Editors
{
    /// <summary>
    /// Custom editor for ResourceAsset ScriptableObjects.
    /// Provides a user-friendly interface for managing resource entries.
    /// </summary>
    [CustomEditor(typeof(ResourceAsset))]
    public class ResourceAssetEditor : Editor
    {
        private SerializedProperty resourcesProperty;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            resourcesProperty = serializedObject.FindProperty("resources");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Resource Asset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This asset contains cached resources accessible by string keys. " +
                                   "Use SIGS.GetResource<T>(key) or string.GetResource<T>() to access cached resources.", 
                                   MessageType.Info);

            EditorGUILayout.HelpBox("💡 Tip: You can drag and drop assets directly into this inspector to add them automatically!", 
                                   MessageType.Info);

            EditorGUILayout.Space(5);

            // Why Use This? button
            GUIStyle whyUseStyle = new GUIStyle(GUI.skin.button);
            whyUseStyle.fontSize = 11;
            
            if (GUILayout.Button("❓ Why Use This?", whyUseStyle, GUILayout.Height(25)))
            {
                ShowWhyUseThisDialog();
            }

            EditorGUILayout.Space(10);

            // Drag and Drop Area
            DrawDragAndDropArea();

            EditorGUILayout.Space(10);

            // Display resource count
            var resourceAsset = (ResourceAsset)target;
            EditorGUILayout.LabelField($"Cached Resources: {resourceAsset.GetCacheSize()}", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Draw the resources list
            EditorGUILayout.LabelField("Resource Entries", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            if (resourcesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No resources cached. Add resources using the buttons below.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < resourcesProperty.arraySize; i++)
                {
                    DrawResourceEntry(i);
                }
            }
            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Resource Entry"))
            {
                resourcesProperty.arraySize++;
                var newEntry = resourcesProperty.GetArrayElementAtIndex(resourcesProperty.arraySize - 1);
                newEntry.FindPropertyRelative("key").stringValue = "";
                newEntry.FindPropertyRelative("resource").objectReferenceValue = null;
            }

            if (GUILayout.Button("Clear All Resources"))
            {
                if (EditorUtility.DisplayDialog("Clear All Resources", 
                    "Are you sure you want to clear all resource entries? This action cannot be undone.", 
                    "Yes", "Cancel"))
                {
                    resourcesProperty.arraySize = 0;
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Config Management Section
            DrawConfigManagementSection();
            
            EditorGUILayout.Space(10);

            // Auto-populate button
            if (GUILayout.Button("Auto-Populate from Resources Folder"))
            {
                AutoPopulateFromResources();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawResourceEntry(int index)
        {
            var entry = resourcesProperty.GetArrayElementAtIndex(index);
            var keyProperty = entry.FindPropertyRelative("key");
            var resourceProperty = entry.FindPropertyRelative("resource");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            
            // Key field
            EditorGUILayout.LabelField("Key:", GUILayout.Width(40));
            keyProperty.stringValue = EditorGUILayout.TextField(keyProperty.stringValue);

            // Remove button
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                resourcesProperty.DeleteArrayElementAtIndex(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();

            // Resource field - allows all types with warnings
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Resource:", GUILayout.Width(60));
            
            // Create an object field that allows all types
            Object currentResource = resourceProperty.objectReferenceValue;
            Object newResource = EditorGUILayout.ObjectField(currentResource, typeof(Object), false);
            
            // Update property if changed
            if (newResource != currentResource)
            {
                resourceProperty.objectReferenceValue = newResource;
            }
            
            EditorGUILayout.EndHorizontal();

            // Validation
            if (string.IsNullOrEmpty(keyProperty.stringValue))
            {
                EditorGUILayout.HelpBox("Key cannot be empty", MessageType.Warning);
            }
            else if (resourceProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Resource reference is missing", MessageType.Warning);
            }
            else if (IsScriptFile(resourceProperty.objectReferenceValue))
            {
                EditorGUILayout.HelpBox("⚠️ Script files (.cs) might be problematic to load as resources at runtime. Consider using ScriptableObjects instead.", MessageType.Warning);
            }
            else if (resourceProperty.objectReferenceValue is AudioClip)
            {
                EditorGUILayout.HelpBox("🎵 For audio clips, consider using the AudioAsset pipeline instead for better audio management and features.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void AutoPopulateFromResources()
        {
            var resourceAsset = (ResourceAsset)target;
            
            // Find all assets in Resources folder
            string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/Resources" });
            
            int addedCount = 0;
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                
                if (asset != null && asset != resourceAsset)
                {
                    // Create a key from the asset name
                    string key = asset.name.ToLower().Replace(" ", "_");
                    
                    // Check if key already exists
                    if (!resourceAsset.HasResource(key))
                    {
                        resourceAsset.AddOrUpdateResource(key, asset);
                        addedCount++;
                    }
                }
            }

            EditorUtility.SetDirty(resourceAsset);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Auto-populated {addedCount} resources into ResourceAsset '{resourceAsset.name}'");
        }

        /// <summary>
        /// Draws a visual drag and drop area to indicate where users can drop assets.
        /// </summary>
        private void DrawDragAndDropArea()
        {
            // Create a visual drag and drop area
            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            
            // Draw the drop area background
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.3f); // Light grey tint
            GUI.Box(dropArea, "", "box");
            GUI.backgroundColor = originalColor;
            
            // Draw border
            Color borderColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey border
            EditorGUI.DrawRect(new Rect(dropArea.x, dropArea.y, dropArea.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(dropArea.x, dropArea.y + dropArea.height - 1, dropArea.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(dropArea.x, dropArea.y, 1, dropArea.height), borderColor);
            EditorGUI.DrawRect(new Rect(dropArea.x + dropArea.width - 1, dropArea.y, 1, dropArea.height), borderColor);
            
            // Draw text
            GUIStyle centerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.4f, 0.4f, 0.4f, 1f) } // Dark grey text
            };
            
            GUI.Label(dropArea, "📁 Drag & Drop Assets Here", centerStyle);
            
            // Handle drag and drop events for this specific area
            Event evt = Event.current;
            
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (dropArea.Contains(evt.mousePosition) && !DragAndDrop.paths.Length.Equals(0))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            var resourceAsset = (ResourceAsset)target;
                            int addedCount = 0;
                            
                            foreach (string path in DragAndDrop.paths)
                            {
                                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                                
                                if (asset != null)
                                {
                                    // Create a key from the asset name
                                    string key = asset.name.ToLower().Replace(" ", "_").Replace("-", "_");
                                    
                                    // Ensure key is unique
                                    string originalKey = key;
                                    int counter = 1;
                                    while (resourceAsset.HasResource(key))
                                    {
                                        key = $"{originalKey}_{counter}";
                                        counter++;
                                    }
                                    
                                    // Add the resource
                                    resourceAsset.AddOrUpdateResource(key, asset);
                                    addedCount++;
                                }
                            }
                            
                            if (addedCount > 0)
                            {
                                EditorUtility.SetDirty(resourceAsset);
                                AssetDatabase.SaveAssets();
                                Debug.Log($"Added {addedCount} resources to ResourceAsset '{resourceAsset.name}' via drag and drop");
                            }
                        }
                    }
                    break;
            }
        }


        /// <summary>
        /// Checks if the given object is a script file (.cs) that might be problematic to load as a resource.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if the object is a script file</returns>
        private bool IsScriptFile(Object obj)
        {
            if (obj == null) return false;

            // Check by asset path for reliable detection
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath))
            {
                // Check if the file extension is .cs
                if (assetPath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shows a dialog explaining why to use Resource Caching instead of Resources.Load
        /// </summary>
        private void ShowWhyUseThisDialog()
        {
            string message = "🚀 Why Use Resource Caching?\n\n" +
                           "✅ PERFORMANCE BENEFITS:\n" +
                           "• Preloaded assets = instant access (no loading delays)\n" +
                           "• No Resources.Load() calls during gameplay\n" +
                           "• Eliminates runtime loading freezes\n" +
                           "• Better memory management and GC performance\n\n" +
                           "📁 ORGANIZATION BENEFITS:\n" +
                           "• Keep prefabs anywhere in your project\n" +
                           "• No need to put everything in Resources/ folder\n" +
                           "• Better project structure and organization\n" +
                           "• Cleaner asset hierarchy\n\n" +
                           "🔧 DEVELOPER BENEFITS:\n" +
                           "• String-based access: SIGS.GetResource<GameObject>(\"player\")\n" +
                           "• Type-safe generic methods\n" +
                           "• Easy to refactor and maintain\n" +
                           "• Centralized resource management\n\n" +
                           "💡 PERFECT FOR:\n" +
                           "• Frequently used prefabs (enemies, UI elements)\n" +
                           "• Sprites and textures\n" +
                           "• ScriptableObject data assets\n" +
                           "• Materials and shaders\n\n" +
                           "⚠️ NOTE: For audio clips, consider using the AudioAsset pipeline instead for better audio management.\n\n" +
                           "Instead of: Resources.Load<GameObject>(\"Prefabs/Player\")\n" +
                           "Use: SIGS.GetResource<GameObject>(\"player\")";

            EditorUtility.DisplayDialog("Why Use Resource Caching?", message, "Got it!");
        }

        private void DrawConfigManagementSection()
        {
            EditorGUILayout.LabelField("⚙️ Config Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var resourceAsset = target as ResourceAsset;
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            if (config == null)
            {
                EditorGUILayout.HelpBox("Signalia Config not found. Cannot manage references.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            bool isInConfig = IsResourceAssetInConfig(config, resourceAsset);
            
            // Show current status
            string statusText = isInConfig ? "✓ In Config" : "✗ Not In Config";
            Color statusColor = isInConfig ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", statusStyle);
            
            // Show explanation
            if (isInConfig)
            {
                EditorGUILayout.HelpBox("This ResourceAsset is loaded in the Signalia config.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("This ResourceAsset is not in the Signalia config.", MessageType.Warning);
            }
            
            GUILayout.Space(5);
            
            // Toggle button
            string buttonText = isInConfig ? "Remove from Config" : "Add to Config";
            Color buttonColor = isInConfig ? Color.red : Color.green;
            
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (isInConfig)
                {
                    RemoveResourceAssetFromConfig(config, resourceAsset);
                    EditorUtility.DisplayDialog("ResourceAsset Removed", 
                        $"Successfully removed '{resourceAsset.name}' from the Signalia config. " +
                        "This asset is now removed from the pipeline and will not be used by the system.", 
                        "OK");
                }
                else
                {
                    AddResourceAssetToConfig(config, resourceAsset);
                    EditorUtility.DisplayDialog("ResourceAsset Added", 
                        $"Successfully added '{resourceAsset.name}' to the Signalia config. " +
                        "This asset is now in the pipeline and will be used by the system.", 
                        "OK");
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }

        private bool IsResourceAssetInConfig(SignaliaConfigAsset config, ResourceAsset resourceAsset)
        {
            if (config == null || config.ResourceAssets == null || resourceAsset == null)
                return false;

            foreach (var asset in config.ResourceAssets)
            {
                if (asset == resourceAsset)
                    return true;
            }

            return false;
        }

        private void AddResourceAssetToConfig(SignaliaConfigAsset config, ResourceAsset resourceAsset)
        {
            if (config == null || resourceAsset == null)
                return;

            var list = new System.Collections.Generic.List<ResourceAsset>(config.ResourceAssets ?? new ResourceAsset[0]);

            if (!list.Contains(resourceAsset))
            {
                list.Add(resourceAsset);
                config.ResourceAssets = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void RemoveResourceAssetFromConfig(SignaliaConfigAsset config, ResourceAsset resourceAsset)
        {
            if (config == null || resourceAsset == null)
                return;

            var list = new System.Collections.Generic.List<ResourceAsset>(config.ResourceAssets ?? new ResourceAsset[0]);

            if (list.Remove(resourceAsset))
            {
                config.ResourceAssets = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
    }
}
