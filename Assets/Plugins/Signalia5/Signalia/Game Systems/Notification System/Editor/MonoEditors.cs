#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Notifications;

namespace AHAKuo.Signalia.GameSystems.Notifications.Editors
{
    /// <summary>
    /// Custom editor for SystemMessage component
    /// </summary>
    [CustomEditor(typeof(SystemMessage))]
    [CanEditMultipleObjects]
    public class SystemMessageEditor : Editor
    {
        private SerializedProperty uiViewProp;
        private SerializedProperty messageTextProp;
        private SerializedProperty audioOnStartProp;
        private SerializedProperty audioOnHideProp;
        private SerializedProperty onShowRadioEventProp;
        private SerializedProperty onHideRadioEventProp;
        private SerializedProperty onShowUnityEventProp;
        private SerializedProperty onHideUnityEventProp;
        private SerializedProperty messageNameProp;
        private SerializedProperty defaultDisplayDurationProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "🎯 Configuration", "🔲 UI References", "🔊 Audio", "📡 Events", "🎮 Runtime" };

        private void OnEnable()
        {
            uiViewProp = serializedObject.FindProperty("uiView");
            messageTextProp = serializedObject.FindProperty("messageText");
            audioOnStartProp = serializedObject.FindProperty("audioOnStart");
            audioOnHideProp = serializedObject.FindProperty("audioOnHide");
            onShowRadioEventProp = serializedObject.FindProperty("onShowRadioEvent");
            onHideRadioEventProp = serializedObject.FindProperty("onHideRadioEvent");
            onShowUnityEventProp = serializedObject.FindProperty("onShowUnityEvent");
            onHideUnityEventProp = serializedObject.FindProperty("onHideUnityEvent");
            messageNameProp = serializedObject.FindProperty("messageName");
            defaultDisplayDurationProp = serializedObject.FindProperty("defaultDisplayDuration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("System Message: Displays queued notification messages with UIView animations. Messages are shown in sequence with OnShow and OnHide events.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            GUILayout.Space(5);

            // Tab selection
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: // Configuration
                    DrawConfigurationTab();
                    break;
                case 1: // UI References
                    DrawUIReferencesTab();
                    break;
                case 2: // Audio
                    DrawAudioTab();
                    break;
                case 3: // Events
                    DrawEventsTab();
                    break;
                case 4: // Runtime
                    DrawRuntimeTab();
                    break;
            }

            GUILayout.Space(15);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigurationTab()
        {
            EditorGUILayout.LabelField("Message Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(messageNameProp, new GUIContent("Message Name", "Unique identifier used by SIGS.ShowNotification(). Auto-generated from GameObject name if empty."));

            if (string.IsNullOrEmpty(messageNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Message Name will be auto-generated from GameObject name on Awake() if left empty.", MessageType.Info);
            }

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(defaultDisplayDurationProp, new GUIContent("Default Display Duration", "How long to show the message before auto-hiding. Set to -1 to keep showing until manually hidden."));

            if (defaultDisplayDurationProp.floatValue < 0 && defaultDisplayDurationProp.floatValue != -1)
            {
                EditorGUILayout.HelpBox("Duration should be -1 (infinite) or a positive value.", MessageType.Warning);
            }
            else if (defaultDisplayDurationProp.floatValue == -1)
            {
                EditorGUILayout.HelpBox("Messages will stay visible until manually hidden via HideNotification() or UIView.Hide().", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Messages will auto-hide after {defaultDisplayDurationProp.floatValue} seconds.", MessageType.Info);
            }
        }

        private void DrawUIReferencesTab()
        {
            EditorGUILayout.LabelField("UI Component References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(uiViewProp, new GUIContent("UI View", "The UIView component that will be shown/hidden. Auto-found in parent if not assigned."));
            EditorGUILayout.PropertyField(messageTextProp, new GUIContent("Message Text", "The TMP_Text component that displays the notification. Auto-found in children if not assigned."));

            if (uiViewProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("UIView will be auto-found in parent GameObject on Awake() if not assigned.", MessageType.Info);
            }

            if (messageTextProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("TMP_Text will be auto-found in children GameObjects on Awake() if not assigned.", MessageType.Info);
            }
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            
            EditorHelpers.DrawAudioDropdown("Audio On Start", audioOnStartProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when notification starts showing (uses SIGS.PlayAudio)", MessageType.None);
            
            GUILayout.Space(5);
            
            EditorHelpers.DrawAudioDropdown("Audio On Hide", audioOnHideProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when notification hides (uses SIGS.PlayAudio)", MessageType.None);
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.LabelField("Radio Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onShowRadioEventProp, new GUIContent("On Show Radio Event", "Event string sent via SIGS.Send() when notification shows"));
            EditorGUILayout.PropertyField(onHideRadioEventProp, new GUIContent("On Hide Radio Event", "Event string sent via SIGS.Send() when notification hides"));

            if (!string.IsNullOrEmpty(onShowRadioEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Will send event '{onShowRadioEventProp.stringValue}' when notification shows.", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(onHideRadioEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Will send event '{onHideRadioEventProp.stringValue}' when notification hides.", MessageType.Info);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onShowUnityEventProp, new GUIContent("On Show Unity Event", "Unity event invoked when notification shows"));
            EditorGUILayout.PropertyField(onHideUnityEventProp, new GUIContent("On Hide Unity Event", "Unity event invoked when notification hides"));
        }

        private void DrawRuntimeTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test notifications!", MessageType.Info);
                return;
            }

            SystemMessage systemMessage = (SystemMessage)target;

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            GUILayout.Space(5);

            // Status info
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Is Displaying: {(systemMessage.IsDisplaying ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"  Queue Count: {systemMessage.QueueCount}");

            GUILayout.Space(10);

            // Test notification button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            if (GUILayout.Button("📢 TEST NOTIFICATION", GUILayout.Height(30)))
            {
                systemMessage.ShowNotification("Test notification message!");
            }
            GUI.backgroundColor = originalColor;

            GUILayout.Space(5);

            // Hide button
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            if (GUILayout.Button("❌ HIDE NOTIFICATION", GUILayout.Height(25)))
            {
                systemMessage.HideNotification();
            }
            GUI.backgroundColor = originalColor;

            GUILayout.Space(5);

            // Clear queue button
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.2f, 1f);
            if (GUILayout.Button("🗑️ CLEAR QUEUE", GUILayout.Height(25)))
            {
                systemMessage.ClearQueue();
            }
            GUI.backgroundColor = originalColor;

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            SystemMessage systemMessage = (SystemMessage)target;

            if (uiViewProp.objectReferenceValue == null)
            {
                var parentView = systemMessage.GetComponentInParent<AHAKuo.Signalia.UI.UIView>();
                if (parentView == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No UIView assigned and none found in parent. Please assign a UIView.", MessageType.Warning);
                }
            }

            if (messageTextProp.objectReferenceValue == null)
            {
                var childText = systemMessage.GetComponentInChildren<TMPro.TMP_Text>();
                if (childText == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No TMP_Text assigned and none found in children. Please assign a TMP_Text.", MessageType.Warning);
                }
            }
        }
    }

    /// <summary>
    /// Custom editor for BurnerSpot component
    /// </summary>
    [CustomEditor(typeof(BurnerSpot))]
    [CanEditMultipleObjects]
    public class BurnerSpotEditor : Editor
    {
        private SerializedProperty spotNameProp;
        private SerializedProperty burnerPrefabProp;
        private SerializedProperty spawnOffsetProp;
        private SerializedProperty useWorldSpaceProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "🎯 Configuration", "📍 Spawn Settings", "🎮 Runtime" };

        private void OnEnable()
        {
            spotNameProp = serializedObject.FindProperty("spotName");
            burnerPrefabProp = serializedObject.FindProperty("burnerPrefab");
            spawnOffsetProp = serializedObject.FindProperty("spawnOffset");
            useWorldSpaceProp = serializedObject.FindProperty("useWorldSpace");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Burner Spot: Manages burner notifications at a specific location. Burners are pooled objects that float upward and disappear.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            GUILayout.Space(5);

            // Tab selection
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: // Configuration
                    DrawConfigurationTab();
                    break;
                case 1: // Spawn Settings
                    DrawSpawnSettingsTab();
                    break;
                case 2: // Runtime
                    DrawRuntimeTab();
                    break;
            }

            GUILayout.Space(15);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigurationTab()
        {
            EditorGUILayout.LabelField("Spot Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spotNameProp, new GUIContent("Spot Name", "Unique identifier used by SIGS.ShowBurner(). Auto-generated from GameObject name if empty."));

            if (string.IsNullOrEmpty(spotNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Spot Name will be auto-generated from GameObject name on Awake() if left empty.", MessageType.Info);
            }

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(burnerPrefabProp, new GUIContent("Burner Prefab", "Prefab with BurnerObject component that will be spawned from pool"));

            if (burnerPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Burner Prefab is required. Create a prefab with a BurnerObject component.", MessageType.Warning);
            }
            else
            {
                var prefab = burnerPrefabProp.objectReferenceValue as GameObject;
                if (prefab != null && prefab.GetComponent<BurnerObject>() == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Prefab does not have a BurnerObject component. Add one to the prefab.", MessageType.Warning);
                }
            }
        }

        private void DrawSpawnSettingsTab()
        {
            EditorGUILayout.LabelField("Spawn Position Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useWorldSpaceProp, new GUIContent("Use World Space", "If true, offset is applied in world space. If false, offset is applied in local space."));
            EditorGUILayout.PropertyField(spawnOffsetProp, new GUIContent("Spawn Offset", "Offset from this transform's position where burners will spawn"));

            if (useWorldSpaceProp.boolValue)
            {
                EditorGUILayout.HelpBox("Offset will be applied in world space: transform.position + spawnOffset", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Offset will be applied in local space: transform.TransformPoint(spawnOffset)", MessageType.Info);
            }
        }

        private void DrawRuntimeTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test burners!", MessageType.Info);
                return;
            }

            BurnerSpot burnerSpot = (BurnerSpot)target;

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            GUILayout.Space(5);

            // Test burner button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
            if (GUILayout.Button("🔥 SHOW BURNER", GUILayout.Height(30)))
            {
                burnerSpot.ShowBurner("Test burner message!");
            }
            GUI.backgroundColor = originalColor;

            GUILayout.Space(5);

            // Show burner without message
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
            if (GUILayout.Button("🔥 SHOW BURNER (No Message)", GUILayout.Height(25)))
            {
                burnerSpot.ShowBurner();
            }
            GUI.backgroundColor = originalColor;

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            if (burnerPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Burner Prefab is required. Please assign a prefab with a BurnerObject component.", MessageType.Warning);
            }
        }
    }

    /// <summary>
    /// Custom editor for BurnerObject component
    /// </summary>
    [CustomEditor(typeof(BurnerObject))]
    [CanEditMultipleObjects]
    public class BurnerObjectEditor : Editor
    {
        private SerializedProperty uiViewProp;
        private SerializedProperty messageTextProp;
        private SerializedProperty floatDistanceProp;
        private SerializedProperty floatDurationProp;
        private SerializedProperty audioOnStartProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "🔲 UI References", "🎬 Animation", "🔊 Audio" };

        private void OnEnable()
        {
            uiViewProp = serializedObject.FindProperty("uiView");
            messageTextProp = serializedObject.FindProperty("messageText");
            floatDistanceProp = serializedObject.FindProperty("floatDistance");
            floatDurationProp = serializedObject.FindProperty("floatDuration");
            audioOnStartProp = serializedObject.FindProperty("audioOnStart");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Burner Object: Pooled notification object that floats upward and fades out. Spawned by BurnerSpot.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            GUILayout.Space(5);

            // Tab selection
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: // UI References
                    DrawUIReferencesTab();
                    break;
                case 1: // Animation
                    DrawAnimationTab();
                    break;
                case 2: // Audio
                    DrawAudioTab();
                    break;
            }

            GUILayout.Space(15);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUIReferencesTab()
        {
            EditorGUILayout.LabelField("UI Component References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(uiViewProp, new GUIContent("UI View", "The UIView component for this burner. Auto-found in children if not assigned."));
            EditorGUILayout.PropertyField(messageTextProp, new GUIContent("Message Text", "The TMP_Text component that displays the message. Auto-found in children if not assigned."));

            if (uiViewProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("UIView will be auto-found in children GameObjects on Awake() if not assigned.", MessageType.Info);
            }

            if (messageTextProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("TMP_Text will be auto-found in children GameObjects on Awake() if not assigned.", MessageType.Info);
            }
        }

        private void DrawAnimationTab()
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(floatDistanceProp, new GUIContent("Float Distance", "Distance to float upward in world units"));
            EditorGUILayout.PropertyField(floatDurationProp, new GUIContent("Float Duration", "Duration of the float animation in seconds"));

            EditorGUILayout.HelpBox($"Burner will float upward {floatDistanceProp.floatValue} units over {floatDurationProp.floatValue} seconds, then hide.", MessageType.Info);
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            
            EditorHelpers.DrawAudioDropdown("Audio On Start", audioOnStartProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when burner appears (uses SIGS.PlayAudio)", MessageType.None);
        }

        private void DrawErrorWarnings()
        {
            BurnerObject burnerObject = (BurnerObject)target;

            if (uiViewProp.objectReferenceValue == null)
            {
                var childView = burnerObject.GetComponentInChildren<AHAKuo.Signalia.UI.UIView>();
                if (childView == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No UIView assigned and none found in children. Please assign a UIView.", MessageType.Warning);
                }
            }

            if (messageTextProp.objectReferenceValue == null)
            {
                var childText = burnerObject.GetComponentInChildren<TMPro.TMP_Text>();
                if (childText == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No TMP_Text assigned and none found in children. Please assign a TMP_Text.", MessageType.Warning);
                }
            }
        }
    }
}
#endif

