using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using System.Linq;

namespace AHAKuo.Signalia.Radio.Editors
{
    /// <summary>
    /// Editor window for quickly introducing new audio keys from clips.
    /// Accessible via Tools/Signalia/Introduce New Audio, right-click on AudioClips,
    /// or right-click on the audio key text field in inspectors.
    /// </summary>
    public class IntroduceNewAudioWindow : EditorWindow
    {
        // ── Constants ──
        private const string DefaultNewAssetFolder = "Assets/Resources/CreatedAudioAssets";
        private const float MinWindowWidth = 420f;
        private const float MinWindowHeight = 520f;

        // ── State ──
        private List<AudioClip> selectedClips = new();
        private string keyName = "";
        private int assetMode = 0; // 0 = existing, 1 = new
        private AudioAsset selectedExistingAsset;
        private string newAssetName = "";
        private MixerDefinition.MixerCategory mixerCategory = MixerDefinition.MixerCategory.Master;
        private float volume = 1f;
        private bool looping = false;

        // Haptic settings
        private bool hapticEnabled = false;
        private HapticType hapticType = HapticType.None;
        private float hapticIntensity = 1f;
        private float hapticDuration = 0.1f;

        // UI scroll
        private Vector2 scrollPosition;
        private Vector2 clipScrollPosition;

        // Cached asset list
        private AudioAsset[] cachedAudioAssets;
        private string[] cachedAssetNames;

        // ── Menu Item ──
        [MenuItem("Tools/Signalia/Introduce New Audio")]
        public static void ShowWindow()
        {
            var window = GetWindow<IntroduceNewAudioWindow>("Introduce New Audio");
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.Show();
        }

        /// <summary>
        /// Opens the window pre-populated with the given AudioClips.
        /// </summary>
        public static void ShowWindowWithClips(AudioClip[] clips)
        {
            var window = GetWindow<IntroduceNewAudioWindow>("Introduce New Audio");
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.selectedClips = new List<AudioClip>(clips.Where(c => c != null));
            window.Show();
            window.Focus();
        }

        /// <summary>
        /// Opens the window pre-filled with an optional key name (from audio selection helper right-click).
        /// </summary>
        public static void ShowWindowForKeyCreation(string prefilledKeyName = "")
        {
            var window = GetWindow<IntroduceNewAudioWindow>("Introduce New Audio");
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            if (!string.IsNullOrEmpty(prefilledKeyName) && prefilledKeyName != FrameworkConstants.StringConstants.NOAUDIO)
            {
                window.keyName = prefilledKeyName;
            }
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshAssetCache();
        }

        private void OnFocus()
        {
            RefreshAssetCache();
        }

        private void RefreshAssetCache()
        {
            // Find all AudioAsset instances in the project
            string[] guids = AssetDatabase.FindAssets("t:AudioAsset");
            var assets = new List<AudioAsset>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioAsset asset = AssetDatabase.LoadAssetAtPath<AudioAsset>(path);
                if (asset != null)
                    assets.Add(asset);
            }
            cachedAudioAssets = assets.ToArray();
            cachedAssetNames = assets.Select(a => a.name).ToArray();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header image (commented out until graphics are ready)
            // GUILayout.BeginHorizontal();
            // GUILayout.FlexibleSpace();
            // EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.IntroduceNewAudioHeader);
            // GUILayout.FlexibleSpace();
            // GUILayout.EndHorizontal();
            // GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Quickly create a new audio key from one or more AudioClips and add it to an existing or new AudioAsset.",
                MessageType.Info);

            GUILayout.Space(10);

            DrawClipsSection();
            GUILayout.Space(10);
            DrawKeyNameSection();
            GUILayout.Space(10);
            DrawTargetAssetSection();
            GUILayout.Space(10);
            DrawAudioSettingsSection();
            GUILayout.Space(10);
            DrawHapticSettingsSection();
            GUILayout.Space(15);
            DrawCreateButton();

            GUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────
        // Audio Clips Section
        // ─────────────────────────────────────────────
        private void DrawClipsSection()
        {
            EditorGUILayout.LabelField("Audio Clips", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Drag and drop AudioClips here, or use the + button to add slots. Multiple clips will be added to the same key.",
                MessageType.None);

            GUILayout.Space(5);

            clipScrollPosition = EditorGUILayout.BeginScrollView(clipScrollPosition, GUILayout.MaxHeight(150));

            for (int i = 0; i < selectedClips.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                selectedClips[i] = (AudioClip)EditorGUILayout.ObjectField(
                    selectedClips[i], typeof(AudioClip), false);

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("x", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    selectedClips.RemoveAt(i);
                    i--;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+ Add Clip Slot", GUILayout.Height(22)))
            {
                selectedClips.Add(null);
            }
            GUI.backgroundColor = Color.white;

            // Drag-drop area
            var dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop AudioClips Here", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is AudioClip clip && !selectedClips.Contains(clip))
                            {
                                selectedClips.Add(clip);
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        // ─────────────────────────────────────────────
        // Key Name Section
        // ─────────────────────────────────────────────
        private void DrawKeyNameSection()
        {
            EditorGUILayout.LabelField("Key Name", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            keyName = EditorGUILayout.TextField("Audio Key", keyName);

            if (!string.IsNullOrEmpty(keyName) && keyName == FrameworkConstants.StringConstants.NOAUDIO)
            {
                EditorGUILayout.HelpBox("'NOAUDIO' is a reserved system key and cannot be used.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Target Asset Section
        // ─────────────────────────────────────────────
        private void DrawTargetAssetSection()
        {
            EditorGUILayout.LabelField("Target Audio Asset", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string[] modeLabels = { "Existing Asset", "New Asset" };
            assetMode = EditorUtilityMethods.RenderToolbar(assetMode, modeLabels, 24);

            GUILayout.Space(5);

            if (assetMode == 0)
            {
                // Existing asset mode
                if (cachedAudioAssets == null || cachedAudioAssets.Length == 0)
                {
                    EditorGUILayout.HelpBox("No AudioAsset files found in the project. Switch to 'New Asset' to create one.", MessageType.Warning);
                }
                else
                {
                    selectedExistingAsset = (AudioAsset)EditorGUILayout.ObjectField(
                        "Audio Asset", selectedExistingAsset, typeof(AudioAsset), false);

                    if (selectedExistingAsset == null)
                    {
                        EditorGUILayout.HelpBox("Select an existing AudioAsset to add the key to.", MessageType.Info);
                    }
                    else
                    {
                        // Check if key already exists
                        if (!string.IsNullOrEmpty(keyName) && selectedExistingAsset.GetListKeys != null
                            && selectedExistingAsset.GetListKeys.Contains(keyName))
                        {
                            EditorGUILayout.HelpBox(
                                $"Key '{keyName}' already exists in '{selectedExistingAsset.name}'. " +
                                "The clips will be appended to the existing key's clip array.",
                                MessageType.Warning);
                        }
                    }
                }
            }
            else
            {
                // New asset mode
                newAssetName = EditorGUILayout.TextField("New Asset Name", newAssetName);

                EditorGUILayout.HelpBox(
                    $"A new AudioAsset will be created at:\n{DefaultNewAssetFolder}/{newAssetName}.asset\n\n" +
                    "It will also be automatically registered in the Signalia config.",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Audio Settings Section
        // ─────────────────────────────────────────────
        private void DrawAudioSettingsSection()
        {
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            mixerCategory = (MixerDefinition.MixerCategory)EditorGUILayout.EnumPopup("Mixer Group", mixerCategory);
            volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);
            looping = EditorGUILayout.Toggle("Loop", looping);

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Haptic Settings Section
        // ─────────────────────────────────────────────
        private void DrawHapticSettingsSection()
        {
            EditorGUILayout.LabelField("Haptic Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            hapticEnabled = EditorGUILayout.Toggle("Enable Haptics", hapticEnabled);

            if (hapticEnabled)
            {
                EditorGUI.indentLevel++;
                hapticType = (HapticType)EditorGUILayout.EnumPopup("Haptic Type", hapticType);
                hapticIntensity = EditorGUILayout.Slider("Intensity", hapticIntensity, 0f, 1f);
                hapticDuration = EditorGUILayout.Slider("Duration", hapticDuration, 0.01f, 1f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Create Button
        // ─────────────────────────────────────────────
        private void DrawCreateButton()
        {
            // Validation
            bool hasClips = selectedClips.Any(c => c != null);
            bool hasKeyName = !string.IsNullOrEmpty(keyName) && keyName != FrameworkConstants.StringConstants.NOAUDIO;
            bool hasTarget = assetMode == 0 ? selectedExistingAsset != null : !string.IsNullOrEmpty(newAssetName);

            List<string> errors = new();
            if (!hasClips) errors.Add("Add at least one AudioClip.");
            if (!hasKeyName) errors.Add("Enter a valid key name.");
            if (!hasTarget)
            {
                if (assetMode == 0) errors.Add("Select a target AudioAsset.");
                else errors.Add("Enter a name for the new AudioAsset.");
            }

            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Warning);
            }

            bool canCreate = hasClips && hasKeyName && hasTarget;
            EditorGUI.BeginDisabledGroup(!canCreate);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Audio Key", GUILayout.Height(35)))
            {
                CreateAudioKey();
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.EndDisabledGroup();
        }

        // ─────────────────────────────────────────────
        // Core Creation Logic
        // ─────────────────────────────────────────────
        private void CreateAudioKey()
        {
            AudioAsset targetAsset;

            if (assetMode == 0)
            {
                // Use existing asset
                targetAsset = selectedExistingAsset;
            }
            else
            {
                // Create new asset
                targetAsset = CreateNewAudioAsset(newAssetName);
                if (targetAsset == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to create the new AudioAsset.", "OK");
                    return;
                }
            }

            // Get valid clips
            AudioClip[] validClips = selectedClips.Where(c => c != null).ToArray();
            if (validClips.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No valid AudioClips selected.", "OK");
                return;
            }

            // Add entry to the asset
            bool keyExisted = AddEntryToAsset(targetAsset, keyName, validClips);

            // Ensure the asset is in the config
            EnsureAssetInConfig(targetAsset);

            // Mark dirty and save
            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Show success message
            string message;
            if (keyExisted)
            {
                message = $"Key '{keyName}' already existed in '{targetAsset.name}'.\n" +
                          $"{validClips.Length} clip(s) were appended to the existing key.";
            }
            else
            {
                message = $"Successfully created key '{keyName}' in '{targetAsset.name}' " +
                          $"with {validClips.Length} clip(s).";
            }

            EditorUtility.DisplayDialog("Audio Key Created", message, "OK");

            // Ping the asset in project
            EditorGUIUtility.PingObject(targetAsset);
            Selection.activeObject = targetAsset;

            // Reset form
            keyName = "";
            selectedClips.Clear();
            RefreshAssetCache();
        }

        /// <summary>
        /// Creates a new AudioAsset at the default folder path.
        /// </summary>
        private AudioAsset CreateNewAudioAsset(string assetName)
        {
            // Ensure folder exists
            string folderPath = DefaultNewAssetFolder;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                CreateFolderRecursive(folderPath);
            }

            string fullPath = $"{folderPath}/{assetName}.asset";

            // Check if asset already exists at this path
            AudioAsset existing = AssetDatabase.LoadAssetAtPath<AudioAsset>(fullPath);
            if (existing != null)
            {
                bool useExisting = EditorUtility.DisplayDialog(
                    "Asset Already Exists",
                    $"An AudioAsset named '{assetName}' already exists at:\n{fullPath}\n\nUse the existing asset instead?",
                    "Use Existing", "Cancel");

                return useExisting ? existing : null;
            }

            // Create the ScriptableObject
            AudioAsset newAsset = ScriptableObject.CreateInstance<AudioAsset>();
            AssetDatabase.CreateAsset(newAsset, fullPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Signalia] Created new AudioAsset at: {fullPath}");
            return newAsset;
        }

        /// <summary>
        /// Creates folders recursively if they don't exist.
        /// </summary>
        private void CreateFolderRecursive(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        /// <summary>
        /// Adds or appends an audio entry to the target asset via SerializedObject.
        /// Returns true if the key already existed (clips were appended).
        /// </summary>
        private bool AddEntryToAsset(AudioAsset asset, string key, AudioClip[] clips)
        {
            SerializedObject so = new SerializedObject(asset);
            so.Update();

            SerializedProperty entriesProp = so.FindProperty("audioEntries");

            // Check if key already exists
            int existingIndex = -1;
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty entry = entriesProp.GetArrayElementAtIndex(i);
                string existingKey = entry.FindPropertyRelative("key").stringValue;
                if (existingKey == key)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                // Key exists - append clips
                SerializedProperty existingEntry = entriesProp.GetArrayElementAtIndex(existingIndex);
                SerializedProperty existingData = existingEntry.FindPropertyRelative("data");
                SerializedProperty existingClips = existingData.FindPropertyRelative("clips");

                foreach (var clip in clips)
                {
                    existingClips.arraySize++;
                    existingClips.GetArrayElementAtIndex(existingClips.arraySize - 1).objectReferenceValue = clip;
                }

                so.ApplyModifiedProperties();
                return true;
            }
            else
            {
                // Create new entry
                entriesProp.arraySize++;
                SerializedProperty newEntry = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);

                // Set key
                newEntry.FindPropertyRelative("key").stringValue = key;

                // Set data
                SerializedProperty dataProp = newEntry.FindPropertyRelative("data");
                dataProp.FindPropertyRelative("volume").floatValue = volume;
                dataProp.FindPropertyRelative("looping").boolValue = looping;
                dataProp.FindPropertyRelative("category").enumValueIndex = (int)mixerCategory;

                // Set clips
                SerializedProperty clipsProp = dataProp.FindPropertyRelative("clips");
                clipsProp.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++)
                {
                    clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                }

                // Set haptic settings
                SerializedProperty hapticProp = dataProp.FindPropertyRelative("hapticSettings");
                if (hapticProp != null)
                {
                    var enabledProp = hapticProp.FindPropertyRelative("enabled");
                    if (enabledProp != null) enabledProp.boolValue = hapticEnabled;

                    var typeProp = hapticProp.FindPropertyRelative("hapticType");
                    if (typeProp != null) typeProp.enumValueIndex = (int)(hapticEnabled ? hapticType : HapticType.None);

                    var intensityProp = hapticProp.FindPropertyRelative("intensity");
                    if (intensityProp != null) intensityProp.floatValue = hapticIntensity;

                    var durationProp = hapticProp.FindPropertyRelative("duration");
                    if (durationProp != null) durationProp.floatValue = hapticDuration;

                    var overrideProp = hapticProp.FindPropertyRelative("overrideAudioSettings");
                    if (overrideProp != null) overrideProp.boolValue = false;
                }

                so.ApplyModifiedProperties();
                return false;
            }
        }

        /// <summary>
        /// Ensures the AudioAsset is registered in the Signalia config.
        /// </summary>
        private void EnsureAssetInConfig(AudioAsset asset)
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            if (config == null)
            {
                Debug.LogWarning("[Signalia] Could not find SignaliaConfigAsset. The new AudioAsset will not be auto-registered.");
                return;
            }

            if (config.AudioAssets == null)
            {
                config.AudioAssets = new AudioAsset[] { asset };
                EditorUtility.SetDirty(config);
                return;
            }

            // Check if already in config
            foreach (var existing in config.AudioAssets)
            {
                if (existing == asset) return;
            }

            // Add to config
            var list = new List<AudioAsset>(config.AudioAssets);
            list.Add(asset);
            config.AudioAssets = list.ToArray();
            EditorUtility.SetDirty(config);

            Debug.Log($"[Signalia] AudioAsset '{asset.name}' was automatically added to the Signalia config.");
        }

        // ─────────────────────────────────────────────
        // Context Menu: Right-click on AudioClips
        // ─────────────────────────────────────────────
        [MenuItem("Assets/Signalia/Introduce New Key", false, 200)]
        private static void IntroduceNewKeyFromAssets()
        {
            var clips = Selection.objects
                .Where(o => o is AudioClip)
                .Cast<AudioClip>()
                .ToArray();

            ShowWindowWithClips(clips);
        }

        [MenuItem("Assets/Signalia/Introduce New Key", true)]
        private static bool IntroduceNewKeyFromAssetsValidation()
        {
            // Only show when at least one AudioClip is selected
            return Selection.objects.Any(o => o is AudioClip);
        }
    }
}
