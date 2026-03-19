using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Radio;

namespace AHAKuo.Signalia.Radio.Editors
{
    [CustomEditor(typeof(SimpleEventListener))]
    public class EventListenerEditor : Editor
    {
        private SerializedProperty listenersProp;
        private ReorderableList listenerList;

        private void OnEnable()
        {
            listenersProp = serializedObject.FindProperty("listeners");

            listenerList = new ReorderableList(serializedObject, listenersProp, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "Event Listeners");
                },

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.y += 2;
                    var element = listenersProp.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                },

                elementHeightCallback = index =>
                {
                    var element = listenersProp.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element, true) + 4f; // a little padding
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);

            listenerList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(SimpleEventSender))]
    public class EventSenderEditor : Editor
    {
        private SerializedProperty sentEventsProp;
        private ReorderableList eventList;
        private string manualEvent = "";

        private GUIContent sendIcon;

        private void OnEnable()
        {
            sentEventsProp = serializedObject.FindProperty("sentEvents");

            // Load Send Icon (Unity built-in)
            sendIcon = EditorGUIUtility.IconContent("d_PlayButton");

            // **Setup Reorderable List**
            eventList = new ReorderableList(serializedObject, sentEventsProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Sent Events"),

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = sentEventsProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element, GUIContent.none
                    );
                },

                elementHeightCallback = index => EditorGUIUtility.singleLineHeight + 4
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("This component sends events using the event system.", MessageType.Info);

            GUILayout.Space(5);

            DrawActionsSection(); // Manual Send Events

            eventList.DoLayoutList(); // Sent Events List

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActionsSection()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // **Send Event in Script**
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Send Events from List", GUILayout.Width(170));

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(sendIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                SendEventInScript();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // **Send Event by Name**
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Send Event by Name", GUILayout.Width(170));
            manualEvent = EditorGUILayout.TextField(manualEvent);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(sendIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                SendEvent(manualEvent);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void SendEventInScript()
        {
            SimpleEventSender sender = (SimpleEventSender)target;
            sender.SendEventInScript();
            Debug.Log("📢 Sent all listed events!");
        }

        private void SendEvent(string eventName)
        {
            if (!string.IsNullOrEmpty(eventName))
            {
                SimpleEventSender sender = (SimpleEventSender)target;
                sender.SendEvent(eventName);
                Debug.Log($"📢 Sent event: {eventName}");
            }
            else
            {
                Debug.LogWarning("⚠ Event name is empty!");
            }
        }
    }

    [CustomEditor(typeof(SimpleStaticObject))]
    public class UIResponderObjectEditor : Editor
    {
        private SerializedProperty responderObjectsProp;

        private void OnEnable()
        {
            responderObjectsProp = serializedObject.FindProperty("responderObjects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("LiveKey converts objects into retrievable values using a key-value system. " +
                "These values are 'live' because they use functions to dynamically fetch the current value each time they're accessed. " +
                "Perfect for storing references to components, GameObjects, or any Unity Object that might change over time.", MessageType.Info);

            // if (GUILayout.Button("Assign Scripts"))
            // {
            //     AssignScripts(); // doesnt seem to be useful for anything, but keep it here for reference
            // }

            EditorGUILayout.PropertyField(responderObjectsProp, new GUIContent("Keys"), true);

            serializedObject.ApplyModifiedProperties();
        }

        private void AssignScripts()
        {
            if (Application.isPlaying) return;

            SimpleStaticObject responderObj = (SimpleStaticObject)target;
            foreach (var obj in responderObj.GetType().GetField("responderObjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                            .GetValue(responderObj) as System.Collections.IList)
            {
                var assignMethod = obj.GetType().GetMethod("AssignUIScript");
                assignMethod?.Invoke(obj, new object[] { responderObj.gameObject });
            }
        }
    }

    [CustomEditor(typeof(SimpleNonStaticObject))]
    public class NonStaticResponderObjectEditor : Editor
    {
        private SerializedProperty responderObjectsProp;

        private void OnEnable()
        {
            responderObjectsProp = serializedObject.FindProperty("responderObjects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("DeadKey converts objects into retrievable values using a key-value system. " +
                "These values are 'dead' because they store static references that don't change once set. " +
                "Perfect for storing primitive values, configuration data, or any value that remains constant during runtime.", MessageType.Info);

            EditorGUILayout.PropertyField(responderObjectsProp, new GUIContent("Keys"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(EventionBox)), CanEditMultipleObjects]
    public class EventionBoxEditor : Editor
    {
        private SerializedProperty eventsProp;
        private int eventIndex = 0;
        private string eventName = "";

        private GUIContent playIcon;
        private GUIContent searchIcon;

        private void OnEnable()
        {
            eventsProp = serializedObject.FindProperty("events");

            // Load icons (Unity built-in icons)
            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
            searchIcon = EditorGUIUtility.IconContent("d_ViewToolZoom");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);

            serializedObject.Update();

            EditorGUILayout.HelpBox("Evention Box is a collection of events that can be invoked from anywhere.", MessageType.Info);

            DrawActionsSection();

            EditorGUILayout.PropertyField(eventsProp, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActionsSection()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            GUILayout.Space(5);

            var script = target as EventionBox;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // **Fire by Index**
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fire Event by Index", GUILayout.Width(150));
            eventIndex = EditorGUILayout.IntField(eventIndex, GUILayout.Width(50));

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.FireEventManuallyByIndex(eventIndex);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // **Fire by Name**
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fire Event by Name", GUILayout.Width(150));
            eventName = EditorGUILayout.TextField(eventName);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(searchIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.FireEventManuallyByName(eventName);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }

    [CustomEditor(typeof(VFXSFX)), CanEditMultipleObjects]
    public class VFXSFXEditor : Editor
    {
        private SerializedProperty entriesProp;

        private SerializedProperty rememberMeProp;
        private SerializedProperty use3DAudioProp;
        private SerializedProperty parentedProp;
        private SerializedProperty offsetProp;
        private SerializedProperty minDistanceProp;
        private SerializedProperty maxDistanceProp;
        private SerializedProperty rolloffModeProp;
        private SerializedProperty spatialBlendProp;
        private SerializedProperty dopplerLevelProp;
        private SerializedProperty spreadProp;
        private SerializedProperty velocityUpdateModeProp;
        private SerializedProperty useAudioFiltersProp;
        private SerializedProperty audioFiltersProp;

        private int selectedTab = 0;
        private int entryIndex = 0;
        private string entryName = "";

        private GUIContent playIcon;
        private GUIContent resetIcon;

        private void OnEnable()
        {
            entriesProp = serializedObject.FindProperty("entries");

            rememberMeProp = serializedObject.FindProperty("rememberMe");
            use3DAudioProp = serializedObject.FindProperty("use3DAudio");
            parentedProp = serializedObject.FindProperty("parented");
            offsetProp = serializedObject.FindProperty("offset");
            minDistanceProp = serializedObject.FindProperty("minDistance");
            maxDistanceProp = serializedObject.FindProperty("maxDistance");
            rolloffModeProp = serializedObject.FindProperty("rolloffMode");
            spatialBlendProp = serializedObject.FindProperty("spatialBlend");
            dopplerLevelProp = serializedObject.FindProperty("dopplerLevel");
            spreadProp = serializedObject.FindProperty("spread");
            velocityUpdateModeProp = serializedObject.FindProperty("velocityUpdateMode");
            useAudioFiltersProp = serializedObject.FindProperty("useAudioFilters");
            audioFiltersProp = serializedObject.FindProperty("audioFilters");

            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
            resetIcon = EditorGUIUtility.IconContent("d_RotateTool");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("VFXSFX is a convenience tool that triggers VFX + SFX together. " +
                                    "It integrates Evention-style actions (UnityEvent / events / menus) and AudioPlayer-style audio entries (with optional haptics).", MessageType.Info);

            GUILayout.Space(10);

            string[] tabNames = { "Feedback", "Audio Settings" };
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            switch (selectedTab)
            {
                case 0:
                    DrawFeedbackTab();
                    break;
                case 1:
                    DrawAudioSettingsTab();
                    break;
            }

            GUILayout.Space(10);
            DrawActionsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFeedbackTab()
        {
            EditorGUILayout.LabelField("✨ Entries", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(entriesProp, true);
            EditorGUILayout.EndVertical();
        }

        private void DrawAudioSettingsTab()
        {
            EditorGUILayout.LabelField("🔊 Shared Audio Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(rememberMeProp, new GUIContent("Remember Me", "Prevents re-triggering the same audio from this component instance when already playing (Signalia remembrance)"));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(use3DAudioProp, new GUIContent("Use 3D Audio", "Plays audio at this component's position with spatial settings (2D uses spatialBlend=0)"));

            if (use3DAudioProp.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(parentedProp, new GUIContent("Parented", "Whether the pooled audio source follows this transform"));
                if (parentedProp.boolValue)
                {
                    EditorGUILayout.PropertyField(offsetProp, new GUIContent("Offset", "Positional offset when parented"));
                }

                EditorGUILayout.PropertyField(minDistanceProp, new GUIContent("Min Distance"));
                EditorGUILayout.PropertyField(maxDistanceProp, new GUIContent("Max Distance"));
                EditorGUILayout.PropertyField(rolloffModeProp, new GUIContent("Rolloff Mode"));
                EditorGUILayout.PropertyField(spatialBlendProp, new GUIContent("Spatial Blend"));
                EditorGUILayout.PropertyField(dopplerLevelProp, new GUIContent("Doppler Level"));
                EditorGUILayout.PropertyField(spreadProp, new GUIContent("Spread"));
                EditorGUILayout.PropertyField(velocityUpdateModeProp, new GUIContent("Velocity Update Mode"));

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("3D Audio is disabled: audio plays as 2D (spatialBlend forced to 0).", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("🎛 Audio Filters", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(useAudioFiltersProp, new GUIContent("Use Audio Filters", "Applies filters to the pooled audio source when playing"));
            if (useAudioFiltersProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(audioFiltersProp, new GUIContent("Filter Settings"), true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("🎮 Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var script = target as VFXSFX;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fire by Index", GUILayout.Width(120));
            entryIndex = EditorGUILayout.IntField(entryIndex, GUILayout.Width(50));
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.FireManuallyByIndex(entryIndex);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fire by Name", GUILayout.Width(120));
            entryName = EditorGUILayout.TextField(entryName);
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.FireManuallyByName(entryName);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            if (GUILayout.Button(new GUIContent("Reset Runtime State", resetIcon.image, "Resets 'once' runtime state so entries can fire again"), GUILayout.Height(24)))
            {
                script.ResetEntryRuntimeState();
            }
            GUI.backgroundColor = Color.white;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Actions can be used in edit mode, but audio playback and pooled effects may not behave as expected outside Play Mode.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }

    [CustomEditor(typeof(AudioAsset)), CanEditMultipleObjects]
    public class AudioAssetEditor : Editor
    {
        private SerializedProperty audioEntriesProp;
        private SerializedProperty preloadProp;
        private string newKey = "";

        private const int EntriesPerPage = 10;
        private readonly List<int> filteredIndices = new List<int>();
        private string searchTerm = string.Empty;
        private int currentPage = 0;

        private void OnEnable()
        {
            audioEntriesProp = serializedObject.FindProperty("audioEntries");
            preloadProp = serializedObject.FindProperty("preload");
            UpdateFilteredIndices();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Manage audio settings that can be attached to buttons and played via the audio manager.", MessageType.Info);

            GUILayout.Space(10);

            // Config Management Section
            DrawConfigManagementSection();

            GUILayout.Space(10);
            //DrawErrorWarnings(); // sort of bugged, they cause annoying unfocus when they disappear while writing a key
            
            GUILayout.Space(10);
            DrawAddKeySection();

            DrawAudioEntriesSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawErrorWarnings()
        {
            AudioAsset audioAsset = (AudioAsset)target;

            if (audioAsset.DuplicateStringError())
            {
                EditorGUILayout.HelpBox("🚨 Duplicate keys detected! Please ensure all keys are unique.", MessageType.Error);
            }

            if (audioAsset.EmptyStringError())
            {
                EditorGUILayout.HelpBox("⚠️ Some keys are empty. Please give all keys a name.", MessageType.Warning);
            }

            if (audioAsset.EmptyClipError())
            {
                EditorGUILayout.HelpBox("⚠️ Some audio clips are missing. Please assign all audio clips.", MessageType.Warning);
            }

            if (audioAsset.WrongKey())
            {
                EditorGUILayout.HelpBox("⚠️ Some keys contain invalid keys. Please remove any special key like 'NOAUDIO' as its for system.", MessageType.Warning);
            }
        }

        private void DrawConfigManagementSection()
        {
            EditorGUILayout.LabelField("⚙️ Config Management", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var audioAsset = target as AudioAsset;
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            if (config == null)
            {
                EditorGUILayout.HelpBox("Signalia Config not found. Cannot manage references.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            bool isInConfig = IsAudioAssetInConfig(config, audioAsset);

            // Show current status
            string statusText = isInConfig ? "✓ In Config" : "✗ Not In Config";
            Color statusColor = isInConfig ? Color.green : Color.red;

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", statusStyle);

            // Show explanation
            if (isInConfig)
            {
                EditorGUILayout.HelpBox("This AudioAsset is loaded in the Signalia config.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("This AudioAsset is not in the Signalia config.", MessageType.Warning);
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
                    RemoveAudioAssetFromConfig(config, audioAsset);
                    EditorUtility.DisplayDialog("AudioAsset Removed",
                        $"Successfully removed '{audioAsset.name}' from the Signalia config. " +
                        "This asset is now removed from the pipeline and will not be used by the system.",
                        "OK");
                }
                else
                {
                    AddAudioAssetToConfig(config, audioAsset);
                    EditorUtility.DisplayDialog("AudioAsset Added",
                        $"Successfully added '{audioAsset.name}' to the Signalia config. " +
                        "This asset is now in the pipeline and will be used by the system.",
                        "OK");
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(preloadProp, new GUIContent("Preload Audio Data", "When enabled, all clips contained in this asset will be loaded into memory as the Signalia systems start up."));

            EditorGUILayout.EndVertical();
        }

        private bool IsAudioAssetInConfig(SignaliaConfigAsset config, AudioAsset audioAsset)
        {
            if (config == null || config.AudioAssets == null || audioAsset == null)
                return false;

            foreach (var asset in config.AudioAssets)
            {
                if (asset == audioAsset)
                    return true;
            }

            return false;
        }

        private void AddAudioAssetToConfig(SignaliaConfigAsset config, AudioAsset audioAsset)
        {
            if (config == null || audioAsset == null)
                return;

            var list = new List<AudioAsset>(config.AudioAssets ?? new AudioAsset[0]);

            if (!list.Contains(audioAsset))
            {
                list.Add(audioAsset);
                config.AudioAssets = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void RemoveAudioAssetFromConfig(SignaliaConfigAsset config, AudioAsset audioAsset)
        {
            if (config == null || audioAsset == null)
                return;

            var list = new List<AudioAsset>(config.AudioAssets ?? new AudioAsset[0]);

            if (list.Remove(audioAsset))
            {
                config.AudioAssets = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawAddKeySection()
        {
            EditorGUILayout.LabelField("➕ Add New Entry", EditorStyles.boldLabel);

            newKey = EditorGUILayout.TextField("New Key", newKey);

            GUI.enabled = !string.IsNullOrEmpty(newKey) && !EntryExists(newKey);

            if (GUILayout.Button("➕ Add Audio Entry", GUILayout.Height(30)))
            {
                AddNewEntry(newKey);
                newKey = "";
            }

            GUI.enabled = true;

            if (EntryExists(newKey))
            {
                EditorGUILayout.HelpBox("⚠️ Key already exists! Choose a unique key.", MessageType.Warning);
            }
        }

        private bool EntryExists(string key)
        {
            for (int i = 0; i < audioEntriesProp.arraySize; i++)
            {
                if (audioEntriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
                {
                    return true;
                }
            }
            return false;
        }

        private void AddNewEntry(string key)
        {
            audioEntriesProp.arraySize++;
            SerializedProperty newEntry = audioEntriesProp.GetArrayElementAtIndex(audioEntriesProp.arraySize - 1);
            newEntry.FindPropertyRelative("key").stringValue = key;
            UpdateFilteredIndices();
            FocusOnIndex(audioEntriesProp.arraySize - 1);
        }

        private void DrawAudioEntriesSection()
        {
            UpdateFilteredIndices();

            EditorGUILayout.LabelField("🎵 Audio Clips", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawSearchToolbar();

            int totalFilteredEntries = filteredIndices.Count;

            if (totalFilteredEntries == 0)
            {
                if (audioEntriesProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("There are no audio entries yet. Add a new entry below to get started.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("No audio entries match the current search.", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
                return;
            }

            DrawPaginationToolbar(totalFilteredEntries);

            GUILayout.Space(5);

            int startIndex = currentPage * EntriesPerPage;
            int endIndex = Mathf.Min(startIndex + EntriesPerPage, totalFilteredEntries);

            for (int displayIndex = startIndex; displayIndex < endIndex; displayIndex++)
            {
                int actualIndex = filteredIndices[displayIndex];
                SerializedProperty element = audioEntriesProp.GetArrayElementAtIndex(actualIndex);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                SerializedProperty keyProp = element.FindPropertyRelative("key");
                string keyLabel = keyProp != null && !string.IsNullOrEmpty(keyProp.stringValue)
                    ? keyProp.stringValue
                    : $"Entry {actualIndex + 1}";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🎧 {keyLabel}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                bool removed = DrawEntryToolbarButtons(actualIndex);
                EditorGUILayout.EndHorizontal();

                if (!removed)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);

                if (removed)
                {
                    break;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSearchToolbar()
        {
            GUIStyle searchFieldStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarSearchField;
            GUIStyle fallbackMiniButton = EditorStyles.miniButton;
            GUIStyle cancelButtonStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? GUI.skin.FindStyle("ToolbarCancelButton") ?? fallbackMiniButton;
            bool usingFallbackCancel = cancelButtonStyle == fallbackMiniButton || cancelButtonStyle.name == fallbackMiniButton.name;
            GUIContent cancelContent = usingFallbackCancel ? new GUIContent("x", "Clear search") : GUIContent.none;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            searchTerm = EditorGUILayout.TextField(searchTerm, searchFieldStyle, GUILayout.ExpandWidth(true));
            float cancelWidth = cancelButtonStyle.fixedWidth > 0 ? cancelButtonStyle.fixedWidth : 18f;

            if (GUILayout.Button(cancelContent, cancelButtonStyle, GUILayout.Width(cancelWidth)))
            {
                searchTerm = string.Empty;
                GUI.FocusControl(null);
                EditorGUI.EndChangeCheck();
                UpdateFilteredIndices();
                currentPage = 0;
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (EditorGUI.EndChangeCheck())
            {
                currentPage = 0;
                UpdateFilteredIndices();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPaginationToolbar(int totalEntries)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)EntriesPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(currentPage <= 0))
            {
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    currentPage = Mathf.Max(0, currentPage - 1);
                }
            }

            using (new EditorGUI.DisabledScope(currentPage >= totalPages - 1))
            {
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    currentPage = Mathf.Min(totalPages - 1, currentPage + 1);
                }
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Page {currentPage + 1} of {totalPages} · {totalEntries} {(totalEntries == 1 ? "Entry" : "Entries")}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawEntryToolbarButtons(int actualIndex)
        {
            bool removed = false;

            using (new EditorGUI.DisabledScope(actualIndex <= 0))
            {
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                {
                    MoveEntry(actualIndex, actualIndex - 1);
                }
            }

            using (new EditorGUI.DisabledScope(actualIndex >= audioEntriesProp.arraySize - 1))
            {
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24)))
                {
                    MoveEntry(actualIndex, actualIndex + 1);
                }
            }

            if (GUILayout.Button("✖", EditorStyles.miniButtonRight, GUILayout.Width(24)))
            {
                RemoveEntry(actualIndex);
                removed = true;
            }

            return removed;
        }

        private void MoveEntry(int fromIndex, int toIndex)
        {
            audioEntriesProp.MoveArrayElement(fromIndex, toIndex);
            UpdateFilteredIndices();
            FocusOnIndex(toIndex);
        }

        private void RemoveEntry(int index)
        {
            audioEntriesProp.DeleteArrayElementAtIndex(index);
            UpdateFilteredIndices();
        }

        private void UpdateFilteredIndices()
        {
            filteredIndices.Clear();

            if (audioEntriesProp != null)
            {
                string comparisonTerm = searchTerm ?? string.Empty;

                for (int i = 0; i < audioEntriesProp.arraySize; i++)
                {
                    SerializedProperty element = audioEntriesProp.GetArrayElementAtIndex(i);
                    SerializedProperty keyProp = element.FindPropertyRelative("key");
                    string keyValue = keyProp != null ? keyProp.stringValue : string.Empty;

                    if (string.IsNullOrEmpty(comparisonTerm) || (!string.IsNullOrEmpty(keyValue) && keyValue.IndexOf(comparisonTerm, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        filteredIndices.Add(i);
                    }
                }
            }

            int totalPages = filteredIndices.Count == 0 ? 1 : Mathf.CeilToInt(filteredIndices.Count / (float)EntriesPerPage);
            currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));
        }

        private void FocusOnIndex(int index)
        {
            int filteredIndex = filteredIndices.IndexOf(index);
            if (filteredIndex >= 0)
            {
                currentPage = Mathf.Clamp(filteredIndex / EntriesPerPage, 0, Mathf.Max(0, Mathf.CeilToInt(filteredIndices.Count / (float)EntriesPerPage) - 1));
            }
        }
    }

    [CustomEditor(typeof(AudioMixerAsset)), CanEditMultipleObjects]
    public class AudioMixerAssetEditor : Editor
    {
        private SerializedProperty mixerDefinitionsProp;
        private ReorderableList reorderableList;

        private void OnEnable()
        {
            mixerDefinitionsProp = serializedObject.FindProperty("mixerDefinitions");

            reorderableList = new ReorderableList(serializedObject, mixerDefinitionsProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "🎛️ Audio Mixers", EditorStyles.boldLabel),

                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty element = mixerDefinitionsProp.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                },

                elementHeightCallback = index =>
                {
                    return EditorGUI.GetPropertyHeight(mixerDefinitionsProp.GetArrayElementAtIndex(index), true) + 4;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Manage your audio mixers. Assign Unity Mixer Groups and categorize them.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawErrorWarnings()
        {
            AudioMixerAsset asset = (AudioMixerAsset)target;

            if (asset.MixerHasDuplicateCategory)
            {
                EditorGUILayout.HelpBox("🚨 Duplicate categories detected! Each mixer category should be unique.", MessageType.Error);
            }

            if (asset.MixerCount == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No valid mixers assigned. Assign at least one Unity AudioMixerGroup.", MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(AudioPlayer)), CanEditMultipleObjects]
    public class AudioPlayerEditor : Editor
    {
        private SerializedProperty audioListsProp;
        private SerializedProperty audioEntriesProp;
        private SerializedProperty legacyModeProp;
        private SerializedProperty playOnEnableProp;
        private SerializedProperty playOnEnableAllProp;
        private SerializedProperty rememberMe;
        private SerializedProperty use3DAudioProp;
        private SerializedProperty parented;
        private SerializedProperty offset;
        private SerializedProperty minDistanceProp;
        private SerializedProperty maxDistanceProp;
        private SerializedProperty rolloffModeProp;
        private SerializedProperty spatialBlendProp;
        private SerializedProperty dopplerLevelProp;
        private SerializedProperty spreadProp;
        private SerializedProperty velocityUpdateModeProp;
        private SerializedProperty useAudioFiltersProp;
        private SerializedProperty audioFiltersProp;

        private GUIContent playIcon;
        private GUIContent stopIcon;

        // Editor state variables
        private int audioIndex = 0;
        private float fadeTime = 0.5f;
        private int selectedTab = 0;

        private void OnEnable()
        {
            audioListsProp = serializedObject.FindProperty("audioLists");
            audioEntriesProp = serializedObject.FindProperty("audioEntries");
            legacyModeProp = serializedObject.FindProperty("legacyMode");
            playOnEnableProp = serializedObject.FindProperty("playOnEnable");
            playOnEnableAllProp = serializedObject.FindProperty("playOnEnableAll");
            rememberMe = serializedObject.FindProperty("rememberMe");
            use3DAudioProp = serializedObject.FindProperty("use3DAudio");
            parented = serializedObject.FindProperty("parented");
            offset = serializedObject.FindProperty("offset");
            minDistanceProp = serializedObject.FindProperty("minDistance");
            maxDistanceProp = serializedObject.FindProperty("maxDistance");
            rolloffModeProp = serializedObject.FindProperty("rolloffMode");
            spatialBlendProp = serializedObject.FindProperty("spatialBlend");
            dopplerLevelProp = serializedObject.FindProperty("dopplerLevel");
            spreadProp = serializedObject.FindProperty("spread");
            velocityUpdateModeProp = serializedObject.FindProperty("velocityUpdateMode");
            useAudioFiltersProp = serializedObject.FindProperty("useAudioFilters");
            audioFiltersProp = serializedObject.FindProperty("audioFilters");

            // Load icons (Unity built-in icons)
            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
            stopIcon = EditorGUIUtility.IconContent("d_PauseButton");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Audio Player component for playing audio through Signalia's audio system.", MessageType.Info);

            GUILayout.Space(10);

            // Tab System
            string[] tabNames = { "Audio", "Settings" };
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            switch (selectedTab)
            {
                case 0: // Audio Tab
                    DrawAudioTab();
                    break;
                case 1: // Settings Tab
                    DrawSettingsTab();
                    break;
            }

            DrawActionsTab();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsTab()
        {
            // Play Settings
            EditorGUILayout.LabelField("🎮 Play Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(playOnEnableProp, new GUIContent("Play on Enable", "Automatically play audio when this component is enabled"));

            if (playOnEnableProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(playOnEnableAllProp, new GUIContent("Play All on Enable", "Play ALL audio entries when enabled (instead of just the first one)"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            // Persistence Settings
            EditorGUILayout.LabelField("💾 Persistence Settings", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(rememberMe, new GUIContent("Remember Me", "Keep this component alive across scene loads"));
            if (rememberMe.boolValue)
            {
                EditorGUILayout.HelpBox("This audio player will use its object instance id to prevent itself from playing again when already playing.", MessageType.Info);
            }
            GUILayout.EndVertical();


            GUILayout.Space(10);

            // 3D Audio Settings
            EditorGUILayout.LabelField("🔊 3D Audio Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(use3DAudioProp, new GUIContent("Use 3D Audio", "Enable 3D spatial audio at this component's position"));

            if (use3DAudioProp.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(parented, new GUIContent("Parented", "Whether the audio source should follow this component"));
                if (parented.boolValue)
                {
                    EditorGUILayout.HelpBox("When parented, the audio will move with this component. When unparented, it will stay at the initial position.", MessageType.Info);
                    EditorGUILayout.PropertyField(offset, new GUIContent("Offset", "Positional offset from this component"));
                }

                EditorGUILayout.PropertyField(minDistanceProp, new GUIContent("Min Distance", "Distance within which the volume stays at maximum"));
                EditorGUILayout.PropertyField(maxDistanceProp, new GUIContent("Max Distance", "Distance beyond which the volume stops attenuating"));
                EditorGUILayout.PropertyField(rolloffModeProp, new GUIContent("Rolloff Mode", "How the volume attenuates with distance"));
                EditorGUILayout.PropertyField(spatialBlendProp, new GUIContent("Spatial Blend", "0 = 2D, 1 = 3D"));
                EditorGUILayout.PropertyField(dopplerLevelProp, new GUIContent("Doppler Level", "How much the pitch changes based on velocity"));
                EditorGUILayout.PropertyField(spreadProp, new GUIContent("Spread", "How much the sound spreads in 3D space"));
                EditorGUILayout.PropertyField(velocityUpdateModeProp, new GUIContent("Velocity Update Mode", "When to update the velocity for doppler effects"));

                EditorGUI.indentLevel--;

                EditorGUILayout.HelpBox("When 3D Audio is enabled, all audio will play at this component's transform position with the above settings.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("🎛 Audio Filters", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(useAudioFiltersProp, new GUIContent("Use Audio Filters", "Apply low/high pass filters to the pooled audio source when this AudioPlayer plays"));
            if (useAudioFiltersProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(audioFiltersProp, new GUIContent("Filter Settings"), true);
                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("Low-pass filters are great for muffled room ambience or behind-wall effects. High-pass filters can trim rumble from airy ambient tracks.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Filters will be removed from pooled sources when disabled, preventing unintended carry-over between clips.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.LabelField("🎵 Audio Selection", EditorStyles.boldLabel);

            // Legacy Mode Toggle
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(legacyModeProp, new GUIContent("Legacy Mode", "Use legacy string-based audio lists (Pre-3.0.0 compatibility)"));
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            if (legacyModeProp.boolValue)
            {
                // Show legacy field
                EditorGUILayout.LabelField("📜 Legacy Audio Lists", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(audioListsProp, new GUIContent("Audio Lists", "Legacy string-based audio list"));
                EditorGUILayout.EndVertical();
            }
            else
            {
                // Show new field
                EditorGUILayout.LabelField("🎵 Audio Entries (3.0.0+)", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(audioEntriesProp, new GUIContent("Audio Entries", "Audio entries with haptic support"));
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawActionsTab()
        {
            EditorGUILayout.LabelField("🎮 Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var script = target as AudioPlayer;

            // Disable the entire Actions section when not in play mode
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            // Play First Audio
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Play First Audio", GUILayout.Width(150));

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.PlayFirstAudioIndex();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Play Audio by Index
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Play Audio Index", GUILayout.Width(150));

            var currentList = legacyModeProp.boolValue ? audioListsProp : audioEntriesProp;
            if (currentList.arraySize > 0)
            {
                audioIndex = EditorGUILayout.IntSlider(audioIndex, 0, currentList.arraySize - 1);
            }
            else
            {
                EditorGUILayout.LabelField("No audio lists available");
                audioIndex = 0; // Reset to 0 when no audio lists
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)) && currentList.arraySize > 0)
            {
                script.PlayAudio(audioIndex);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Play with Fade In
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Play with Fade In", GUILayout.Width(150));

            fadeTime = EditorGUILayout.Slider(fadeTime, 0.1f, 3f);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)) && currentList.arraySize > 0)
            {
                script.PlayAudioWithGentleFadeIn(audioIndex);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Stop Audio by Index
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stop Audio Index", GUILayout.Width(150));

            if (currentList.arraySize > 0)
            {
                audioIndex = EditorGUILayout.IntSlider(audioIndex, 0, currentList.arraySize - 1);
            }
            else
            {
                EditorGUILayout.LabelField("No audio lists available");
                audioIndex = 0; // Reset to 0 when no audio lists
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(stopIcon, GUILayout.Width(30), GUILayout.Height(20)) && currentList.arraySize > 0)
            {
                script.StopAudio(audioIndex);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Play All Audio
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Play All Audio", GUILayout.Width(150));

            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)) && currentList.arraySize > 0)
            {
                script.PlayAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Play All with Fade In
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Play All with Fade In", GUILayout.Width(150));

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)) && currentList.arraySize > 0)
            {
                script.PlayAllWithGentleFadeIn(fadeTime);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Haptic Actions
            EditorGUILayout.LabelField("🎮 Haptic Actions", EditorStyles.boldLabel);

            // Stop All Haptics
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stop All Haptics", GUILayout.Width(150));

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(stopIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                HapticsManager.StopAllHaptics();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            // Show info when not in play mode
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Actions are only available in Play Mode.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

    }

    [CustomEditor(typeof(HapticScript)), CanEditMultipleObjects]
    public class HapticScriptEditor : Editor
    {
        private SerializedProperty hapticSettingsProp;
        private SerializedProperty enableHapticsProp;
        private SerializedProperty triggerOnStartProp;
        private SerializedProperty triggerOnEnableProp;
        private SerializedProperty triggerOnDisableProp;
        private SerializedProperty eventNamesToListenProp;
        private SerializedProperty listenToEventsProp;

        private GUIContent playIcon;
        private GUIContent stopIcon;
        private GUIContent testIcon;

        // Editor state variables
        private HapticType testHapticType = HapticType.Light;
        private float testIntensity = 1f;
        private float testDuration = 0.1f;
        private int selectedTab = 0;

        private void OnEnable()
        {
            hapticSettingsProp = serializedObject.FindProperty("hapticSettings");
            enableHapticsProp = serializedObject.FindProperty("enableHaptics");
            triggerOnStartProp = serializedObject.FindProperty("triggerOnStart");
            triggerOnEnableProp = serializedObject.FindProperty("triggerOnEnable");
            triggerOnDisableProp = serializedObject.FindProperty("triggerOnDisable");
            eventNamesToListenProp = serializedObject.FindProperty("eventNamesToListen");
            listenToEventsProp = serializedObject.FindProperty("listenToEvents");

            // Load icons (Unity built-in icons)
            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
            stopIcon = EditorGUIUtility.IconContent("d_PauseButton");
            testIcon = EditorGUIUtility.IconContent("d_PlayButton On");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Haptic Script component for triggering haptic feedback. Can be used independently of audio or attached to UI elements.", MessageType.Info);

            GUILayout.Space(10);

            // Tab System
            string[] tabNames = { "Settings", "Triggers", "Events", "Actions" };
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            switch (selectedTab)
            {
                case 0: // Settings Tab
                    DrawSettingsTab();
                    break;
                case 1: // Triggers Tab
                    DrawTriggersTab();
                    break;
                case 2: // Events Tab
                    DrawEventsTab();
                    break;
                case 3: // Actions Tab
                    DrawActionsTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("🎮 Haptic Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(hapticSettingsProp);
            EditorGUILayout.EndVertical();
        }

        private void DrawTriggersTab()
        {
            EditorGUILayout.LabelField("⚡ Trigger Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(triggerOnStartProp, new GUIContent("On Start", "Trigger haptics when the component starts"));
            EditorGUILayout.PropertyField(triggerOnEnableProp, new GUIContent("On Enable", "Trigger haptics when the component is enabled"));
            EditorGUILayout.PropertyField(triggerOnDisableProp, new GUIContent("On Disable", "Trigger haptics when the component is disabled"));
            EditorGUILayout.EndVertical();
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.LabelField("📡 Event Integration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(listenToEventsProp, new GUIContent("Listen to Events", "Automatically trigger haptics when specific events are received"));

            if (listenToEventsProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(eventNamesToListenProp, new GUIContent("Event Names", "Event names to listen for"), true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionsTab()
        {
            EditorGUILayout.LabelField("🎮 Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var script = target as HapticScript;

            // Disable the entire Actions section when not in play mode
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            // Trigger Haptic with Current Settings
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trigger Current Settings", GUILayout.Width(150));

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.TriggerHaptic();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Test Haptic Type
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test Haptic Type", GUILayout.Width(150));

            testHapticType = (HapticType)EditorGUILayout.EnumPopup(testHapticType, GUILayout.Width(100));

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button(testIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.TriggerHapticPreset(testHapticType);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Test Custom Haptic
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test Custom Haptic", GUILayout.Width(150));

            testIntensity = EditorGUILayout.Slider(testIntensity, 0f, 1f, GUILayout.Width(80));
            testDuration = EditorGUILayout.Slider(testDuration, 0.01f, 1f, GUILayout.Width(80));

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button(playIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.TriggerHaptic(testHapticType, testIntensity, testDuration);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Stop All Haptics
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stop All Haptics", GUILayout.Width(150));

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(stopIcon, GUILayout.Width(30), GUILayout.Height(20)))
            {
                script.StopAllHaptics();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Get Device Info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Device Info", GUILayout.Width(150));

            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("Info", GUILayout.Width(50), GUILayout.Height(20)))
            {
                string deviceInfo = script.GetHapticDeviceInfo();
                Debug.Log(deviceInfo);
                EditorUtility.DisplayDialog("Haptic Device Info", deviceInfo, "OK");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            // Show info when not in play mode
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Actions are only available in Play Mode.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }
}