// EditorUtilityMethods.cs (same filename is fine)
// NOTE: All existing public method names and parameters are preserved.
//       Only the internal UI flow changed to use a mini search picker window.

using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Radio.Editors;
using DG.DOTweenEditor;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AHAKuo.Signalia.Framework.Editors
{
    public static class EditorUtilityMethods
    {
        // Helper function to create a solid color texture
        public static Texture2D RenderTex(int width, int height, Color col)
        {
            int len = Mathf.Max(1, width * height);
            Color[] pix = new Color[len];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;

            var result = new Texture2D(Mathf.Max(1, width), Mathf.Max(1, height));
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        /// <summary>
        /// Renders a toolbar with a custom height. Specific for Signalia.
        /// </summary>
        /// <param name="selected"></param>
        /// <param name="options"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int RenderToolbar(int selected, string[] options, Single height = 24)
        {
            GUI.backgroundColor = Color.gray;
            var result = GUILayout.Toolbar(selected, options, GUILayout.Height(height));
            GUI.backgroundColor = Color.white;
            return result;
        }

        /// <summary>
        /// Renders a Signalia header image with the standard header height.
        /// </summary>
        /// <param name="headerTexture">The header texture to render. Can be null.</param>
        public static void RenderSignaliaHeader(Texture2D headerTexture)
        {
            if (headerTexture != null)
            {
                GUILayout.Label(headerTexture, GUILayout.Height(EditorUtilityConstants.HeaderImageHeight));
            }
        }

        /// <summary>
        /// Renders a Signalia header image with the standard header height.
        /// </summary>
        /// <param name="headerContent">The header GUIContent to render. Can be null.</param>
        public static void RenderSignaliaHeader(GUIContent headerContent)
        {
            if (headerContent != null)
            {
                GUILayout.Label(headerContent, GUILayout.Height(EditorUtilityConstants.HeaderImageHeight));
            }
        }

        /// <summary>
        /// Renders a Signalia header image with optional max width constraint.
        /// </summary>
        /// <param name="headerTexture">The header texture to render. Can be null.</param>
        /// <param name="maxWidth">Optional maximum width constraint.</param>
        public static void RenderSignaliaHeader(Texture2D headerTexture, float maxWidth)
        {
            if (headerTexture != null)
            {
                GUILayout.Label(headerTexture, GUILayout.Height(EditorUtilityConstants.HeaderImageHeight), GUILayout.MaxWidth(maxWidth));
            }
        }

        /// <summary>
        /// Renders a Signalia header image with optional max width constraint.
        /// </summary>
        /// <param name="headerContent">The header GUIContent to render. Can be null.</param>
        /// <param name="maxWidth">Optional maximum width constraint.</param>
        public static void RenderSignaliaHeader(GUIContent headerContent, float maxWidth)
        {
            if (headerContent != null)
            {
                GUILayout.Label(headerContent, GUILayout.Height(EditorUtilityConstants.HeaderImageHeight), GUILayout.MaxWidth(maxWidth));
            }
        }

        [MenuItem("Tools/Signalia/Force Recompile Code")]
        public static void ForceRecompileCode()
        {
            CompilationPipeline.RequestScriptCompilation();
        }
    }

    // ---------------------------
    // Simple search helpers
    // ---------------------------
    internal static class SimpleSearchHelpers
    {
        /// <summary>
        /// Finds the closest match to the search term in the given list.
        /// Returns the index of the best match, or -1 if no good match found.
        /// Excludes "NOAUDIO" from search results.
        /// </summary>
        public static int FindClosestMatch(string searchTerm, IList<string> options, out List<string> multipleMatches)
        {
            multipleMatches = new List<string>();
            
            if (options == null || options.Count == 0)
                return -1;
            
            // If search term is empty, whitespace, or exact match, return all options (except NOAUDIO)
            if (string.IsNullOrWhiteSpace(searchTerm) || 
                options.Any(opt => string.Equals(opt, searchTerm, StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < options.Count; i++)
                {
                    string option = options[i] ?? "";
                    if (option != FrameworkConstants.StringConstants.NOAUDIO)
                    {
                        multipleMatches.Add(option);
                    }
                }
                return multipleMatches.Count > 0 ? 0 : -1; // Return first index if we have matches
            }

            string searchLower = searchTerm.ToLowerInvariant();
            var matches = new List<(int index, string option, int score)>();

            for (int i = 0; i < options.Count; i++)
            {
                string option = options[i] ?? "";
                string optionLower = option.ToLowerInvariant();
                
                // Skip NOAUDIO from search results
                if (option == FrameworkConstants.StringConstants.NOAUDIO)
                    continue;
                
                // Exact match gets highest score
                if (optionLower == searchLower)
                {
                    matches.Add((i, option, 1000));
                }
                // Starts with gets high score
                else if (optionLower.StartsWith(searchLower))
                {
                    matches.Add((i, option, 500));
                }
                // Contains gets medium score
                else if (optionLower.Contains(searchLower))
                {
                    matches.Add((i, option, 100));
                }
            }

            if (matches.Count == 0)
                return -1;

            // Sort by score (highest first)
            matches.Sort((a, b) => b.score.CompareTo(a.score));

            // If we have multiple high-scoring matches, collect them for dialog
            int topScore = matches[0].score;
            foreach (var match in matches)
            {
                if (match.score >= topScore - 50) // Within 50 points of top score
                {
                    multipleMatches.Add(match.option);
                }
            }

            return matches[0].index;
        }

        /// <summary>
        /// Shows a confirmation dialog for a single match.
        /// Returns the match if confirmed, null if cancelled.
        /// </summary>
        public static string ShowSingleMatchConfirmation(string title, string match, string searchTerm = "")
        {
            if (string.IsNullOrEmpty(match))
                return null;

            // Check if it's an exact match
            bool isExactMatch = !string.IsNullOrEmpty(searchTerm) && 
                               string.Equals(match, searchTerm, StringComparison.OrdinalIgnoreCase);

            string message;
            if (isExactMatch)
            {
                message = $"Found exact match:\n\n{match}\n\nThis is the same as your input. Use it anyway?";
            }
            else
            {
                message = $"Found this match:\n\n{match}\n\nExpected something else? Reword your search.";
            }

            bool confirmed = EditorUtility.DisplayDialog(
                title,
                message,
                "Use Match",
                "Cancel"
            );

            return confirmed ? match : null;
        }

        public static void ShowMultipleMatchDialog(string title, List<string> matches, Action<string> onPicked)
        {
            if (matches == null || matches.Count == 0)
            {
                onPicked?.Invoke(null);
                return;
            }

            if (matches.Count == 1)
            {
                string single = ShowSingleMatchConfirmation(title, matches[0], "");
                onPicked?.Invoke(single);
                return;
            }

            var dialog = ScriptableObject.CreateInstance<MultipleMatchDialog>();
            dialog.Initialize(title, matches, onPicked);
            dialog.ShowAuxWindow();
            dialog.Focus();
        }
    }

    // Custom dialog window for multiple match selection
    internal class MultipleMatchDialog : EditorWindow
    {
        private const string SearchFieldControlName = "AudioSearchFilter";

        private string _title;
        private List<string> _matches;
        private Vector2 _scrollPosition;
        private int _selectedIndex = 0;
        private string _searchQuery = "";
        private List<int> _filteredIndices = new();

        private int clickCount = 0;
        private bool _shouldClose = false;
        private Action<string> _onPicked;   // ✅ new
        private bool _focusSearchRequested = true;
        private bool _deferredFocusSearch = false;

        public string SelectedItem { get; private set; }

        public void Initialize(string title, List<string> matches, Action<string> onPicked)
        {
            _title = title;
            _matches = new List<string>(matches);
            _filteredIndices = Enumerable.Range(0, _matches.Count).ToList();
            SelectedItem = null;
            _selectedIndex = 0;
            _searchQuery = "";
            minSize = new(256, 256);
            clickCount = 0;
            _onPicked = onPicked; // ✅ store callback
            _focusSearchRequested = true;
            _deferredFocusSearch = false;
        }

        private void SelectAndClose(string value)
        {
            SelectedItem = value;
            try { _onPicked?.Invoke(value); } catch (Exception ex) { Debug.LogException(ex); }
            ScheduleClose();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent(_title);

            using (new EditorGUILayout.VerticalScope())
            {
                // --- Search field ---
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName(SearchFieldControlName);
                    _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarTextField);
                    if (EditorGUI.EndChangeCheck())
                        FilterMatches();
                    if (_focusSearchRequested)
                    {
                        _focusSearchRequested = false;
                        EditorApplication.delayCall += () =>
                        {
                            if (this != null && !_shouldClose)
                            {
                                _deferredFocusSearch = true;
                                Repaint();
                            }
                        };
                    }
                    if (_deferredFocusSearch)
                    {
                        _deferredFocusSearch = false;
                        GUI.FocusControl(SearchFieldControlName);
                    }
                }

                // --- Results list ---
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scroll.scrollPosition;

                    for (int i = 0; i < _filteredIndices.Count; i++)
                    {
                        int matchIndex = _filteredIndices[i];
                        string match = _matches[matchIndex];

                        var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));

                        if (i == _selectedIndex)
                            EditorGUI.DrawRect(rect, new Color(0.25f, 0.5f, 0.9f, 0.15f));

                        if (GUI.Button(rect, match, EditorStyles.label))
                        {
                            if (_selectedIndex != i)
                                clickCount = 0;

                            _selectedIndex = i;
                            clickCount++;

                            if (clickCount == 2)
                            {
                                SelectAndClose(_matches[_filteredIndices[_selectedIndex]]);
                            }
                        }
                    }
                }

                // --- Buttons ---
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                    {
                        SelectAndClose(null);
                    }

                    using (new EditorGUI.DisabledScope(_filteredIndices.Count == 0))
                    {
                        if (GUILayout.Button("Select", GUILayout.Width(80)))
                        {
                            if (_filteredIndices.Count > 0 &&
                                _selectedIndex >= 0 &&
                                _selectedIndex < _filteredIndices.Count)
                            {
                                SelectAndClose(_matches[_filteredIndices[_selectedIndex]]);
                            }
                        }
                    }
                }
            }

            HandleKeyboard();
        }


        private void ScheduleClose()
        {
            if (_shouldClose) return;
            _shouldClose = true;

            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    _shouldClose = false;
                    Close();
                }
            };
        }



        private void FilterMatches()
        {
            _filteredIndices.Clear();
            string query = _searchQuery?.ToLowerInvariant() ?? "";
            
            for (int i = 0; i < _matches.Count; i++)
            {
                if (string.IsNullOrEmpty(query) || _matches[i].ToLowerInvariant().Contains(query))
                {
                    _filteredIndices.Add(i);
                }
            }
            
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _filteredIndices.Count - 1);
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.DownArrow)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex + 1, 0, _filteredIndices.Count - 1);
                e.Use();
            }
            else if (e.keyCode == KeyCode.UpArrow)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex - 1, 0, _filteredIndices.Count - 1);
                e.Use();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (_filteredIndices.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _filteredIndices.Count)
                {
                    SelectAndClose(_matches[_filteredIndices[_selectedIndex]]);
                }
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                SelectAndClose(null);
                e.Use();
            }
        }

        private void OnLostFocus()
        {
            // Don't close on lost focus to allow proper interaction
        }
    }

    public static class EditorHelpers
    {
        // Keeps search queries per-field if needed later
        private static readonly Dictionary<string, string> SearchQueries = new();

        /// <summary>
        /// Shows the audio key text-field context menu with clipboard operations
        /// and the "Introduce New Key" shortcut.
        /// Call this when a right-click is detected on a text-field rect, BEFORE
        /// the TextField itself is drawn so the event is consumed first.
        /// </summary>
        /// <param name="currentValue">Current string value of the field.</param>
        /// <param name="onValueChanged">Callback invoked with the new string when Cut or Paste changes the value.</param>
        internal static void ShowAudioFieldContextMenu(string currentValue, System.Action<string> onValueChanged)
        {
            GenericMenu menu = new GenericMenu();

            // -- Introduce New Key (top item) – passes the current field text as a pre-filled key name --
            string capturedValue = currentValue; // capture for lambda
            menu.AddItem(new GUIContent("Introduce New Key"), false, () =>
            {
                IntroduceNewAudioWindow.ShowWindowForKeyCreation(capturedValue);
            });

            menu.AddSeparator("");

            // -- Standard clipboard operations --
            bool hasSelection = !string.IsNullOrEmpty(currentValue);

            if (hasSelection)
            {
                menu.AddItem(new GUIContent("Cut"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = currentValue;
                    onValueChanged?.Invoke("");
                });
                menu.AddItem(new GUIContent("Copy"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = currentValue;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Cut"));
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            bool hasClipboard = !string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer);
            if (hasClipboard)
            {
                menu.AddItem(new GUIContent("Paste"), false, () =>
                {
                    onValueChanged?.Invoke(EditorGUIUtility.systemCopyBuffer);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Replaces dropdown with a search button that opens a mini picker.
        /// Adds the picked UIView.MenuName into targetList.
        /// </summary>
        public static void DrawMenuDropdown(string label, ReorderableList targetList, SerializedObject serializedObject)
        {
            if (targetList == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Invalid target list or serialized object.", MessageType.Warning);
                return;
            }

#if UNITY_6000_0_OR_NEWER
            UIView[] allMenus = Object.FindObjectsByType<UIView>(FindObjectsSortMode.None);
#else
            UIView[] allMenus = Object.FindObjectsOfType<UIView>(true);
#endif
            string[] fullMenuNames = allMenus?.Select(m => m?.MenuName ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToArray() ?? Array.Empty<string>();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField("Click the search to add...", EditorStyles.helpBox);

                if (GUILayout.Button(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(28), GUILayout.Height(18)))
                {
                    if (fullMenuNames.Length == 0)
                    {
                        EditorUtility.DisplayDialog("No Menus", "No UIView instances with MenuName found.", "OK");
                        return;
                    }

                    // Find closest match to current field value
                    var prop = targetList.serializedProperty;
                    string currentValue = "";
                    if (prop.arraySize > 0)
                    {
                        currentValue = prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue ?? "";
                    }

                    // For menus, always search with empty string to show all menus
                    int bestMatch = SimpleSearchHelpers.FindClosestMatch("", fullMenuNames, out List<string> multipleMatches);
                    
                    if (bestMatch >= 0)
                    {
                        if (multipleMatches.Count > 1)
                        {
                            SimpleSearchHelpers.ShowMultipleMatchDialog("Select Menu", multipleMatches, (result) =>
                            {
                                if (!string.IsNullOrEmpty(result))
                                {
                                    Undo.RecordObject(serializedObject.targetObject, $"Add Menu to {label}");
                                    prop.arraySize++;
                                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = result;
                                    serializedObject.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(serializedObject.targetObject);
                                    
                                    // Unfocus the text field to show search completed
                                    GUI.FocusControl(null);
                                }
                            });
                        }
                        else if (multipleMatches.Count == 1)
                        {
                            string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Menu", multipleMatches[0], "");
                            if (!string.IsNullOrEmpty(selected))
                            {
                                Undo.RecordObject(serializedObject.targetObject, $"Add Menu to {label}");
                                prop.arraySize++;
                                prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = selected;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // Unfocus the text field to show search completed
                                GUI.FocusControl(null);
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Menus", "No menus are available.", "OK");
                    }
                }
            }
        }

        /// <summary>
        /// Writable text field with search button; sets string property from UIView.MenuName.
        /// </summary>
        public static void DrawMenuDropdownForProperty(string label, SerializedProperty targetProperty, SerializedObject serializedObject)
        {
            if (targetProperty == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Invalid property or serialized object.", MessageType.Warning);
                return;
            }

#if UNITY_6000_0_OR_NEWER
            UIView[] allMenus = Object.FindObjectsByType<UIView>(FindObjectsSortMode.None);
#else
            UIView[] allMenus = Object.FindObjectsOfType<UIView>(true);
#endif
            string[] fullMenuNames = allMenus?.Select(m => m?.MenuName ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToArray() ?? Array.Empty<string>();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUILayout.TextField(targetProperty.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                    targetProperty.stringValue = newValue;
                    serializedObject.ApplyModifiedProperties();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(28), GUILayout.Height(18)))
                {
                    if (fullMenuNames.Length == 0)
                    {
                        EditorUtility.DisplayDialog("No Menus", "No UIView instances with MenuName found.", "OK");
                        return;
                    }

                    // For menus, always search with empty string to show all menus
                    int bestMatch = SimpleSearchHelpers.FindClosestMatch("", fullMenuNames, out List<string> multipleMatches);
                    
                    if (bestMatch >= 0)
                    {
                        if (multipleMatches.Count > 1)
                        {
                            SimpleSearchHelpers.ShowMultipleMatchDialog("Select Menu", multipleMatches, (result) =>
                            {
                                if (!string.IsNullOrEmpty(result))
                                {
                                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                    targetProperty.stringValue = result;
                                    serializedObject.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(serializedObject.targetObject);
                                    
                                    // Unfocus the text field to show search completed
                                    GUI.FocusControl(null);
                                }
                            });
                        }
                        else if (multipleMatches.Count == 1)
                        {
                            string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Menu", multipleMatches[0], "");
                            if (!string.IsNullOrEmpty(selected))
                            {
                                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                targetProperty.stringValue = selected;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // Unfocus the text field to show search completed
                                GUI.FocusControl(null);
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Menus", "No menus are available.", "OK");
                    }
                }
            }
        }

        /// <summary>
        /// Writable audio key field with play and search buttons (writes into targetProperty).
        /// </summary>
        public static void DrawAudioDropdown(string label, SerializedProperty targetProperty, SerializedObject serializedObject)
        {
            if (targetProperty == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Invalid property or serialized object.", MessageType.Warning);
                return;
            }

            // Build full lists
            List<string> rawKeys = new() { FrameworkConstants.StringConstants.NOAUDIO };
            List<string> displayNames = new() { "(None)" };

            var audioAssets = ResourceHandler.GetAudioAssets() ?? Array.Empty<Radio.AudioAsset>();
            foreach (var asset in audioAssets)
            {
                if (asset == null) continue;
                string assetName = asset.name ?? "AudioAsset";
                foreach (var key in asset.GetListKeys ?? Array.Empty<string>())
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    rawKeys.Add(key);
                    displayNames.Add($"{assetName}/{key}");
                }
            }

            int fullIndex = Mathf.Max(0, rawKeys.IndexOf(targetProperty.stringValue));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                
                // Reserve the text field rect so we can intercept right-click before the field consumes it
                Rect textFieldRect = EditorGUILayout.GetControlRect();
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && textFieldRect.Contains(Event.current.mousePosition))
                {
                    ShowAudioFieldContextMenu(targetProperty.stringValue, (pasted) =>
                    {
                        Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                        targetProperty.stringValue = pasted;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    });
                    Event.current.Use();
                }
                
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUI.TextField(textFieldRect, targetProperty.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                    targetProperty.stringValue = newValue;
                    serializedObject.ApplyModifiedProperties();
                }

                // Play button if valid key selected
                bool canPlay = !string.IsNullOrEmpty(targetProperty.stringValue) && targetProperty.stringValue != FrameworkConstants.StringConstants.NOAUDIO;
                using (new EditorGUI.DisabledScope(!canPlay))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(24)))
                        PlayAudioPreview(targetProperty.stringValue);
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(28), GUILayout.Height(18)))
                {
                    if (displayNames.Count == 0)
                    {
                        EditorUtility.DisplayDialog("No Audio", "No audio keys found.", "OK");
                        return;
                    }

                    // Find closest match to current field value
                    string currentValue = targetProperty.stringValue ?? "";
                    int bestMatch = SimpleSearchHelpers.FindClosestMatch(currentValue, rawKeys, out List<string> multipleMatches);
                    
                    if (bestMatch >= 0)
                    {
                        if (multipleMatches.Count > 1)
                        {
                            SimpleSearchHelpers.ShowMultipleMatchDialog("Select Audio", multipleMatches, (result) =>
                            {
                                if (!string.IsNullOrEmpty(result))
                                {
                                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                    targetProperty.stringValue = result;
                                    serializedObject.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(serializedObject.targetObject);
                                    
                                    // Unfocus the text field to show search completed
                                    GUI.FocusControl(null);
                                }
                            });
                        }
                        else if (multipleMatches.Count == 1)
                        {
                            string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Audio", multipleMatches[0], currentValue);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                targetProperty.stringValue = selected;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // Unfocus the text field to show search completed
                                GUI.FocusControl(null);
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Match", $"No audio found matching '{currentValue}'.", "OK");
                    }
                }
            }
        }

        /// <summary>Plays an audio preview for the given audio key.</summary>
        public static void PlayAudioPreview(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey) || audioKey == FrameworkConstants.StringConstants.NOAUDIO)
            {
                Debug.LogWarning("No audio selected for preview.");
                return;
            }

            var audioData = ResourceHandler.GetAudio(audioKey);
            if (audioData == null)
            {
                Debug.LogWarning($"Audio '{audioKey}' not found.");
                return;
            }

            if (audioData.clips == null || audioData.clips.Length == 0)
            {
                Debug.LogWarning($"No audio clips found for '{audioKey}'.");
                return;
            }

            AudioClip clip = audioData.clips[Random.Range(0, audioData.clips.Length)];
            if (clip == null)
            {
                Debug.LogWarning($"Audio clip is null for '{audioKey}'.");
                return;
            }

            GameObject audioObject = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave);
            var audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = Mathf.Clamp01(audioData.volume);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.loop = false;
            audioSource.Play();

            float clipLength = Mathf.Min(3f, Mathf.Max(0.05f, clip.length));

            if (clip.length > 3f)
                Debug.Log($"Previewing '{audioKey}' for {clipLength:0.##}s (full clip {clip.length:0.##}s).");

            double destroyTime = EditorApplication.timeSinceStartup + clipLength;
            void CheckForDeletion()
            {
                if (EditorApplication.timeSinceStartup >= destroyTime)
                {
                    if (audioObject) Object.DestroyImmediate(audioObject);
                    EditorApplication.update -= CheckForDeletion;
                }
            }
            EditorApplication.update += CheckForDeletion;
        }

        /// <summary>
        /// Writable Audio Layer field with search-button flow (writes into targetProperty).
        /// </summary>
        public static void DrawLayerDropdown(string label, SerializedProperty targetProperty, SerializedObject serializedObject)
        {
            if (targetProperty == null || serializedObject == null)
            {
                EditorGUILayout.HelpBox("Invalid property or serialized object.", MessageType.Warning);
                return;
            }

            List<string> layers = new();
            var availableLayers = ResourceHandler.GetAudioLayeringNames() ?? Array.Empty<string>();
            layers.AddRange(availableLayers.Where(s => !string.IsNullOrEmpty(s)));

            int fullIndex = Mathf.Max(0, layers.IndexOf(targetProperty.stringValue));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUILayout.TextField(targetProperty.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                    targetProperty.stringValue = newValue;
                    serializedObject.ApplyModifiedProperties();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(28), GUILayout.Height(18)))
                {
                    // Find closest match to current field value
                    string currentValue = targetProperty.stringValue ?? "";
                    int bestMatch = SimpleSearchHelpers.FindClosestMatch(currentValue, layers, out List<string> multipleMatches);
                    
                    if (bestMatch >= 0)
                    {
                        if (multipleMatches.Count > 1)
                        {
                            SimpleSearchHelpers.ShowMultipleMatchDialog("Select Layer", multipleMatches, (result) =>
                            {
                                if (!string.IsNullOrEmpty(result))
                                {
                                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                    targetProperty.stringValue = result;
                                    serializedObject.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(serializedObject.targetObject);
                                    
                                    // Unfocus the text field to show search completed
                                    GUI.FocusControl(null);
                                }
                            });
                        }
                        else if (multipleMatches.Count == 1)
                        {
                            string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Layer", multipleMatches[0], currentValue);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                targetProperty.stringValue = selected;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // Unfocus the text field to show search completed
                                GUI.FocusControl(null);
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Match", $"No layer found matching '{currentValue}'.", "OK");
                    }
                }
            }
        }

        /// <summary>
        /// Returns the picked audio key as string using the search-button flow.
        /// </summary>
        public static void DrawAudioDropdown_SettingsWindow(string label, string currentValue, FrameworkSettings setWindow, SignaliaConfigAsset config)
        {
            // Build audio lists
            List<string> rawKeys = new() { FrameworkConstants.StringConstants.NOAUDIO };
            List<string> displayNames = new() { "(None)" };

            var audioAssets = ResourceHandler.GetAudioAssets() ?? Array.Empty<Radio.AudioAsset>();
            foreach (var asset in audioAssets)
            {
                if (asset == null) continue;
                string assetName = asset.name ?? "AudioAsset";
                foreach (var key in asset.GetListKeys ?? Array.Empty<string>())
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    rawKeys.Add(key);
                    displayNames.Add($"{assetName}/{key}");
                }
            }

            int fullIndex = Mathf.Max(0, rawKeys.IndexOf(currentValue));
            string shown = (fullIndex >= 0 && fullIndex < displayNames.Count) ? displayNames[fullIndex] : "(None)";
            string result = currentValue; // Initialize with current value

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                
                // Reserve the text field rect so we can intercept right-click before the field consumes it
                Rect settingsTextFieldRect = EditorGUILayout.GetControlRect();
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && settingsTextFieldRect.Contains(Event.current.mousePosition))
                {
                    ShowAudioFieldContextMenu(currentValue, (pasted) =>
                    {
                        config.ClickBackAudio = pasted;
                        EditorUtility.SetDirty(config);
                        if (setWindow != null) setWindow.Repaint();
                    });
                    Event.current.Use();
                }

                // Use TextField instead of LabelField to allow manual editing
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUI.TextField(settingsTextFieldRect, currentValue);
                if (EditorGUI.EndChangeCheck())
                {
                    result = newValue;

                    config.ClickBackAudio = result;
                    if (setWindow != null)
                    {
                        setWindow.Repaint();
                    }
                    // dirty the config
                    EditorUtility.SetDirty(config);
                }

                // Play button if valid key selected (use currentValue for better consistency)
                bool canPlay = !string.IsNullOrEmpty(currentValue) && currentValue != FrameworkConstants.StringConstants.NOAUDIO;
                using (new EditorGUI.DisabledScope(!canPlay))
                {
                    if (GUILayout.Button("▶", GUILayout.Width(24)))
                        PlayAudioPreview(currentValue);
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(28), GUILayout.Height(18)))
                {
                    if (rawKeys.Count <= 1) // Only "(None)" entry
                    {
                        EditorUtility.DisplayDialog("No Audio", "No audio keys found.", "OK");
                        // set result to empty
                        result = "";
                        config.ClickBackAudio = result;

                        if (setWindow != null)
                        {
                            setWindow.Repaint();
                        }

                        // dirty the config
                        EditorUtility.SetDirty(config);
                        return;
                    }

                    // Find closest match to current field value
                    int bestMatch = SimpleSearchHelpers.FindClosestMatch(currentValue, rawKeys, out List<string> multipleMatches);
                    
                    if (bestMatch >= 0)
                    {
                        if (multipleMatches.Count > 1)
                        {
                            // Use the multiple match dialog like the working version
                            SimpleSearchHelpers.ShowMultipleMatchDialog("Select Audio", multipleMatches, (selectedResult) =>
                            {
                                if (!string.IsNullOrEmpty(selectedResult))
                                {
                                    result = selectedResult;
                                    GUI.FocusControl(null);

                                    // dirty the config
                                    config.ClickBackAudio = result;
                                    EditorUtility.SetDirty(config);
                                }
                            });
                        }
                        else if (multipleMatches.Count == 1)
                        {
                            string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Audio", multipleMatches[0], currentValue);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                result = selected;
                                GUI.FocusControl(null);
                                // dirty the config
                                config.ClickBackAudio = result;
                            }
                        }

                        // update the editor window
                        if (setWindow != null)
                        {
                            setWindow.Repaint();
                        }

                        // Force GUI repaint to show the change immediately
                        GUI.changed = true;
                        
                        // Unfocus the text field to show search completed
                        GUI.FocusControl(null);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Match", $"No audio found matching '{currentValue}'.", "OK");
                    }
                }
            }
        }
    }

    public static class PropertyHelpers
    {
        private static readonly Dictionary<string, string> SearchQueries = new();

        /// <summary>
        /// Inline version: label + writable field + play (if audio) + search button opening the mini picker.
        /// </summary>
        public static void DrawAudioDropdownInline(Rect position, string label, SerializedProperty targetProperty, SerializedObject serializedObject)
        {
            if (targetProperty == null || serializedObject == null)
            {
                EditorGUI.HelpBox(position, "Invalid property or serialized object.", MessageType.Warning);
                return;
            }

            // Layout calc
            float labelWidth = EditorGUIUtility.labelWidth;
            float playW = 24f;
            float searchW = 28f;
            float padding = 4f;

            Rect labelRect = new(position.x, position.y, labelWidth, position.height);
            Rect fieldRect = new(position.x + labelWidth, position.y, position.width - labelWidth - playW - searchW - padding * 2, position.height);
            Rect playRect = new(fieldRect.xMax + padding, position.y, playW, position.height);
            Rect searchRect = new(playRect.xMax + padding, position.y, searchW, position.height);

            // Build audio lists for display
            List<string> rawKeys = new() { FrameworkConstants.StringConstants.NOAUDIO };
            List<string> displayNames = new() { "(None)" };
            var audioAssets = ResourceHandler.GetAudioAssets() ?? Array.Empty<Radio.AudioAsset>();
            foreach (var asset in audioAssets)
            {
                if (asset == null) continue;
                string assetName = asset.name ?? "AudioAsset";
                foreach (var key in asset.GetListKeys ?? Array.Empty<string>())
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    displayNames.Add($"{assetName}/{key}");
                    rawKeys.Add(key);
                }
            }

            EditorGUI.LabelField(labelRect, label);

            // Right-click context menu - intercept before TextField consumes the event
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && fieldRect.Contains(Event.current.mousePosition))
            {
                EditorHelpers.ShowAudioFieldContextMenu(targetProperty.stringValue, (pasted) =>
                {
                    Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                    targetProperty.stringValue = pasted;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                });
                Event.current.Use();
            }

            EditorGUI.BeginChangeCheck();
            string newValue = EditorGUI.TextField(fieldRect, targetProperty.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                targetProperty.stringValue = newValue;
                serializedObject.ApplyModifiedProperties();
            }

            bool canPlay = !string.IsNullOrEmpty(targetProperty.stringValue) && targetProperty.stringValue != FrameworkConstants.StringConstants.NOAUDIO;
            using (new EditorGUI.DisabledScope(!canPlay))
            {
                if (GUI.Button(playRect, "▶"))
                    EditorHelpers.PlayAudioPreview(targetProperty.stringValue);
            }

            if (GUI.Button(searchRect, EditorGUIUtility.IconContent("Search Icon")))
            {
                // Find closest match to current field value
                string currentValue = targetProperty.stringValue ?? "";
                int bestMatch = SimpleSearchHelpers.FindClosestMatch(currentValue, rawKeys, out List<string> multipleMatches);
                
                if (bestMatch >= 0)
                {
                    if (multipleMatches.Count > 1)
                    {
                        SimpleSearchHelpers.ShowMultipleMatchDialog("Select Audio", multipleMatches, (result) =>
                        {
                            if (!string.IsNullOrEmpty(result))
                            {
                                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                targetProperty.stringValue = result;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // Unfocus the text field to show search completed
                                GUI.FocusControl(null);
                            }
                        });
                    }
                    else if (multipleMatches.Count == 1)
                    {
                        string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Audio", multipleMatches[0]);
                        if (!string.IsNullOrEmpty(selected))
                        {
                            Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                            targetProperty.stringValue = selected;
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(serializedObject.targetObject);
                            
                            // Unfocus the text field to show search completed
                            GUI.FocusControl(null);
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("No Match", $"No audio found matching '{currentValue}'.", "OK");
                }
            }
        }

        public static void DrawLayerDropdownInline(Rect position, string label, SerializedProperty targetProperty, SerializedObject serializedObject)
        {
            if (targetProperty == null || serializedObject == null)
            {
                EditorGUI.HelpBox(position, "Invalid property or serialized object.", MessageType.Warning);
                return;
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            float searchW = 28f;
            float padding = 4f;

            Rect labelRect = new(position.x, position.y, labelWidth, position.height);
            Rect fieldRect = new(position.x + labelWidth, position.y, position.width - labelWidth - searchW - padding, position.height);
            Rect searchRect = new(fieldRect.xMax + padding, position.y, searchW, position.height);

            List<string> layers = new();
            layers.AddRange(ResourceHandler.GetAudioLayeringNames() ?? Array.Empty<string>());

            EditorGUI.LabelField(labelRect, label);
            
            EditorGUI.BeginChangeCheck();
            string newValue = EditorGUI.TextField(fieldRect, targetProperty.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                targetProperty.stringValue = newValue;
                serializedObject.ApplyModifiedProperties();
            }

            if (GUI.Button(searchRect, EditorGUIUtility.IconContent("Search Icon")))
            {
                // Find closest match to current field value
                string currentValue = targetProperty.stringValue ?? "";
                int bestMatch = SimpleSearchHelpers.FindClosestMatch(currentValue, layers, out List<string> multipleMatches);
                
                if (bestMatch >= 0)
                {
                    if (multipleMatches.Count > 1)
                    {
                        SimpleSearchHelpers.ShowMultipleMatchDialog("Select Layer", multipleMatches, (result) =>
                        {
                            if (!string.IsNullOrEmpty(result))
                            {
                                Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                                targetProperty.stringValue = result;
                                serializedObject.ApplyModifiedProperties();
                                EditorUtility.SetDirty(serializedObject.targetObject);
                                
                                // Unfocus the text field to show search completed
                                GUI.FocusControl(null);
                            }
                        });
                    }
                    else if (multipleMatches.Count == 1)
                    {
                        string selected = SimpleSearchHelpers.ShowSingleMatchConfirmation("Select Layer", multipleMatches[0]);
                        if (!string.IsNullOrEmpty(selected))
                        {
                            Undo.RecordObject(serializedObject.targetObject, $"Change {label}");
                            targetProperty.stringValue = selected;
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(serializedObject.targetObject);
                            
                            // Unfocus the text field to show search completed
                            GUI.FocusControl(null);
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("No Match", $"No layer found matching '{currentValue}'.", "OK");
                }
            }
        }
    }

    public static class EditorPreviewInjector
    {
        public static UIAnimationAsset currentPreview;
        public static GameObject currentTarget;
        public static Tween currentTween;

        public static void Start(UIAnimationAsset animationAsset, GameObject target)
        {
            // Stop any currently running preview first
            if (currentPreview != null && currentTarget != null)
            {
                DOTweenEditorPreview.Stop(true, true);
                currentPreview.StopPreview(currentTarget);
                currentPreview.StopAnimations();
            }

            currentTween?.Kill();

            if (animationAsset == null)
            {
                Debug.LogWarning("AnimationAsset is null.");
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("Target is null.");
                return;
            }

            currentPreview = animationAsset;
            currentTarget = target;

            var preview = Create(target, animationAsset);
            animationAsset.PreviewAnimation(target, preview);
        }

        public static UIAnimationAsset.EditorPreview Create(GameObject previewTarget, UIAnimationAsset animationAsset)
        {
            return (tween) =>
            {
                if (tween == null)
                {
                    Debug.LogWarning("Tween is null.");
                    return;
                }

                currentTween = tween;

                // Always add completion callback to ensure proper cleanup
                if (tween.Loops() >= 0)
                {
                    tween.OnComplete(() =>
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        animationAsset.StopPreview(previewTarget);
                        animationAsset.StopAnimations();
                        
                        // Clear current references after cleanup
                        if (currentPreview == animationAsset && currentTarget == previewTarget)
                        {
                            currentPreview = null;
                            currentTarget = null;
                            currentTween = null;
                        }
                    });
                }

                DOTweenEditorPreview.PrepareTweenForPreview(tween, false, true, true);
                DOTweenEditorPreview.Start();
            };
        }
    }

    public static class EditorHelpersAudioPreview // (unchanged functional API)
    {
        /// <summary>Plays an audio preview for the given audio key.</summary>
        public static void PlayAudioPreview(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey) || audioKey == FrameworkConstants.StringConstants.NOAUDIO)
            {
                Debug.LogWarning("No audio selected for preview.");
                return;
            }

            var audioData = ResourceHandler.GetAudio(audioKey);
            if (audioData == null)
            {
                Debug.LogWarning($"Audio '{audioKey}' not found.");
                return;
            }

            if (audioData.clips == null || audioData.clips.Length == 0)
            {
                Debug.LogWarning($"No audio clips found for '{audioKey}'.");
                return;
            }

            AudioClip clip = audioData.clips[Random.Range(0, audioData.clips.Length)];
            if (clip == null)
            {
                Debug.LogWarning($"Audio clip is null for '{audioKey}'.");
                return;
            }

            GameObject audioObject = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave);
            var audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = Mathf.Clamp01(audioData.volume);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.loop = false;
            audioSource.Play();

            float clipLength = Mathf.Min(3f, Mathf.Max(0.05f, clip.length));

            if (clip.length > 3f)
                Debug.Log($"Previewing '{audioKey}' for {clipLength:0.##}s (full clip {clip.length:0.##}s).");

            double destroyTime = EditorApplication.timeSinceStartup + clipLength;
            void CheckForDeletion()
            {
                if (EditorApplication.timeSinceStartup >= destroyTime)
                {
                    if (audioObject) Object.DestroyImmediate(audioObject);
                    EditorApplication.update -= CheckForDeletion;
                }
            }
            EditorApplication.update += CheckForDeletion;
        }
    }
}
