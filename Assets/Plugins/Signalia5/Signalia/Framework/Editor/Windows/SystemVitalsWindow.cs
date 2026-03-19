using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities.SIGInput;

namespace AHAKuo.Signalia.Framework.Editors
{
    public class SystemVitalsWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private float maxWidthProperties => position.width - 20;
        private VersionInfo versionInfo;
        private string[] tabs = { "Overview", "Simple Radio", "Complex Radio", "Utilities", "Radio Control", "Input Actions" };
        private int selectedTab = 0;
        private bool autoRefresh = true;
        private double lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.5; // Refresh every 0.5 seconds
        
        // Search functionality fields
        private string searchQuery = "";
        private List<SearchResult> searchResults = new List<SearchResult>();
        private Vector2 searchScrollPosition;
        
        // Listener creator fields
        private string listenerCreatorEventName = "";
        private bool listenerCreatorIsOneShot = false;

        [MenuItem("Tools/Signalia/System Vitals")]
        public static void ShowWindow()
        {
            GetWindow<SystemVitalsWindow>("Signalia System Vitals").minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            LoadVersionInfo();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void LoadVersionInfo()
        {
            string relativePath = "Assets/AHAKuo Creations/Signalia/Framework/version.info";
            string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));
            
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    string jsonContent = System.IO.File.ReadAllText(fullPath);
                    versionInfo = JsonUtility.FromJson<VersionInfo>(jsonContent);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse version.info: {e.Message}");
                    versionInfo = new VersionInfo
                    {
                        package = "Signalia Engine",
                        version = "Version information not found",
                        date = "",
                        company = "AHAKuo Creations"
                    };
                }
            }
            else
            {
                versionInfo = new VersionInfo
                {
                    package = "Signalia Engine",
                    version = "Version information not found",
                    date = "",
                    company = "AHAKuo Creations"
                };
            }
        }

        private void OnGUI()
        {
            // Check if we're in play mode
            if (!Application.isPlaying)
            {
                // Show disabled state with explanation
                GUI.enabled = false;
                
                // Header with System Vitals logo and version
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(GraphicLoader.SystemVitals, GUILayout.Height(128), GUILayout.Width(128));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Version info
                GUIStyle disabledVersionStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
                GUILayout.Label($"{versionInfo.package} | {versionInfo.version}", disabledVersionStyle);
                GUIStyle disabledCompanyStyle = new GUIStyle(EditorStyles.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                GUILayout.Label($"by {versionInfo.company}", disabledCompanyStyle);

                GUILayout.Space(30);

                // Play mode message
                GUIStyle messageStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
                GUILayout.Label("System Vitals", messageStyle);
                
                GUIStyle subMessageStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                GUILayout.Label("This window is only available during Play Mode", subMessageStyle);
                
                GUILayout.Space(20);
                
                EditorGUILayout.HelpBox("System Vitals monitors runtime data and requires the game to be running. Please enter Play Mode to access system vitals, listener counts, responder analysis, and radio control utilities.", MessageType.Info);
                
                GUILayout.FlexibleSpace();
                
                // Play button
                GUI.enabled = true;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(40), GUILayout.Width(150)))
                {
                    EditorApplication.isPlaying = true;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                return;
            }

            // Auto-refresh if enabled
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
            {
                Repaint();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }

            // Header with System Vitals logo and version
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(GraphicLoader.SystemVitals, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Version info
            GUIStyle versionStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label($"{versionInfo.package} | {versionInfo.version}", versionStyle);
            GUIStyle companyStyle = new GUIStyle(EditorStyles.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label($"by {versionInfo.company}", companyStyle);

            GUILayout.Space(15);

            // Auto-refresh toggle
            GUILayout.BeginHorizontal();
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Now", GUILayout.Width(100)))
            {
                Repaint();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Tab selection
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            switch (selectedTab)
            {
                case 0:
                    DrawOverviewTab();
                    break;
                case 1:
                    DrawSimpleRadioTab();
                    break;
                case 2:
                    DrawComplexRadioTab();
                    break;
                case 3:
                    DrawUtilitiesTab();
                    break;
                case 4:
                    DrawExtraUtilitiesTab();
                    break;
                case 5:
                    DrawInputActionsTab();
                    break;
            }

            GUILayout.EndScrollView();
        }

        private void DrawOverviewTab()
        {
            EditorGUILayout.HelpBox("System Overview - Current runtime state of Signalia components", MessageType.Info);
            GUILayout.Space(10);

            // Runtime Status
            GUILayout.Label("Runtime Status", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            bool isWatchmanActive = Watchman.Instance != null;
            GUILayout.Label($"Watchman Active: {(isWatchmanActive ? "✓ Yes" : "✗ No")}", isWatchmanActive ? EditorStyles.boldLabel : EditorStyles.label);
            
            bool isDebuggingEnabled = RuntimeValues.Debugging.IsDebugging;
            GUILayout.Label($"Debugging Enabled: {(isDebuggingEnabled ? "✓ Yes" : "✗ No")}", isDebuggingEnabled ? EditorStyles.boldLabel : EditorStyles.label);
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Quick Stats
            GUILayout.Label("Quick Statistics", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            int totalTrackedListeners = SimpleRadio.TrackedListenerCount() + ComplexRadio.TrackedComplexListenerCount();
            int totalResponders = SimpleRadio.LiveKeyCount() + SimpleRadio.DeadKeyCount();
            int totalChannels = ComplexRadio.channels.Count;
            
            GUILayout.Label($"Total Tracked Listeners: {totalTrackedListeners}", EditorStyles.label);
            GUILayout.Label($"  - Simple Radio: {SimpleRadio.TrackedListenerCount()}", EditorStyles.miniLabel);
            
            // Show breakdown of Simple Radio listeners
            var allSimpleListeners = SimpleRadio.GetAllTrackedListeners();
            var simpleEventsCount = allSimpleListeners.Count(l => l.listenerType == ListenerInfo.ListenerType.SimpleEvent);
            var parameterEventsCount = allSimpleListeners.Count(l => l.listenerType == ListenerInfo.ListenerType.ParameterEvent);
            
            GUILayout.Label($"    • Simple Events: {simpleEventsCount}", EditorStyles.miniLabel);
            GUILayout.Label($"    • Parameter Events: {parameterEventsCount}", EditorStyles.miniLabel);
            GUILayout.Label($"  - Complex Radio: {ComplexRadio.TrackedComplexListenerCount()}", EditorStyles.miniLabel);
            GUILayout.Label($"Total Responders: {totalResponders}", EditorStyles.label);
            GUILayout.Label($"Active Channels: {totalChannels}", EditorStyles.label);
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Resource Assets Section
            GUILayout.Label("Resource Assets", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            var config = ConfigReader.GetConfig();
            if (config != null && config.ResourceAssets != null)
            {
                int resourceAssetCount = config.ResourceAssets.Length;
                int totalCachedResources = 0;
                
                // Count total cached resources across all ResourceAssets
                foreach (var resourceAsset in config.ResourceAssets)
                {
                    if (resourceAsset != null)
                    {
                        totalCachedResources += resourceAsset.GetCacheSize();
                    }
                }
                
                GUILayout.Label($"Resource Assets: {resourceAssetCount}", EditorStyles.label);
                GUILayout.Label($"Total Cached Resources: {totalCachedResources}", EditorStyles.label);
                
                if (resourceAssetCount > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Resource Asset Breakdown:", EditorStyles.miniLabel);
                    for (int i = 0; i < resourceAssetCount; i++)
                    {
                        var asset = config.ResourceAssets[i];
                        if (asset != null)
                        {
                            GUILayout.Label($"  • {asset.name}: {asset.GetCacheSize()} resources", EditorStyles.miniLabel);
                        }
                        else
                        {
                            GUILayout.Label($"  • Missing Reference", EditorStyles.miniLabel);
                        }
                    }
                }
            }
            else
            {
                GUILayout.Label("Resource Assets: 0", EditorStyles.label);
                GUILayout.Label("Total Cached Resources: 0", EditorStyles.label);
            }
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Audio Assets Section
            GUILayout.Label("Audio Assets", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            var audioConfig = ConfigReader.GetConfig();
            if (audioConfig != null && audioConfig.AudioAssets != null)
            {
                int audioAssetCount = audioConfig.AudioAssets.Length;
                int totalAudioEntries = 0;
                
                // Count total audio entries across all AudioAssets
                foreach (var audioAsset in audioConfig.AudioAssets)
                {
                    if (audioAsset != null)
                    {
                        totalAudioEntries += audioAsset.GetListKeys.Length;
                    }
                }
                
                GUILayout.Label($"Audio Assets: {audioAssetCount}", EditorStyles.label);
                GUILayout.Label($"Total Audio Entries: {totalAudioEntries}", EditorStyles.label);
                
                if (audioAssetCount > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Audio Asset Breakdown:", EditorStyles.miniLabel);
                    for (int i = 0; i < audioAssetCount; i++)
                    {
                        var asset = audioConfig.AudioAssets[i];
                        if (asset != null)
                        {
                            GUILayout.Label($"  • {asset.name}: {asset.GetListKeys.Length} entries", EditorStyles.miniLabel);
                        }
                        else
                        {
                            GUILayout.Label($"  • Missing Reference", EditorStyles.miniLabel);
                        }
                    }
                }
            }
            else
            {
                GUILayout.Label("Audio Assets: 0", EditorStyles.label);
                GUILayout.Label("Total Audio Entries: 0", EditorStyles.label);
            }
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Memory Usage (if available)
            GUILayout.Label("Memory Usage", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            long totalMemory = GC.GetTotalMemory(false);
            GUILayout.Label($"GC Memory: {FormatBytes(totalMemory)}", EditorStyles.label);
            
            GUILayout.EndVertical();
        }

        private void DrawSimpleRadioTab()
        {
            EditorGUILayout.HelpBox("Simple Radio System - Event listeners and responders", MessageType.Info);
            GUILayout.Space(10);

            // Listeners Section
            GUILayout.Label("Event Listeners", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            int trackedListeners = SimpleRadio.TrackedListenerCount();
            int simpleListeners = SimpleRadio.SimpleListenersCount();
            int parameterListeners = SimpleRadio.ParameterListenersCount();
            
            // Get tracked listener breakdown
            var allTrackedListeners = SimpleRadio.GetAllTrackedListeners();
            var trackedSimpleEvents = allTrackedListeners.Count(l => l.listenerType == ListenerInfo.ListenerType.SimpleEvent);
            var trackedParameterEvents = allTrackedListeners.Count(l => l.listenerType == ListenerInfo.ListenerType.ParameterEvent);
            
            GUILayout.Label($"Tracked Listeners: {trackedListeners}", EditorStyles.boldLabel);
            GUILayout.Label($"  • Simple Events: {trackedSimpleEvents}", EditorStyles.miniLabel);
            GUILayout.Label($"  • Parameter Events: {trackedParameterEvents}", EditorStyles.miniLabel);
            GUILayout.Label($"Simple Event Listeners: {simpleListeners}", EditorStyles.label);
            GUILayout.Label($"Parameter Event Listeners: {parameterListeners}", EditorStyles.label);
            GUILayout.Label($"Total Listeners: {trackedListeners}", EditorStyles.label);
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Responders Section
            GUILayout.Label("Event Responders", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            int staticResponders = SimpleRadio.LiveKeyCount();
            int nonStaticResponders = SimpleRadio.DeadKeyCount();
            int totalResponders = staticResponders + nonStaticResponders;
            
            GUILayout.Label($"LiveKey Responders: {staticResponders}", EditorStyles.label);
            GUILayout.Label($"DeadKey Responders: {nonStaticResponders}", EditorStyles.label);
            GUILayout.Label($"Total Responders: {totalResponders}", EditorStyles.boldLabel);
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Control Buttons
            GUILayout.Label("Controls", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Listeners", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All Listeners", 
                    "This will clear all event listeners. This action cannot be undone. Continue?", 
                    "Yes", "Cancel"))
                {
                    SimpleRadio.CleanUp();
                    Debug.Log("All Simple Radio listeners and responders cleared.");
                }
            }
            
            if (GUILayout.Button("Clear All Responders", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All Responders", 
                    "This will clear all responders. This action cannot be undone. Continue?", 
                    "Yes", "Cancel"))
                {
                    SimpleRadio.LiveKeyDictionary.Clear();
                    SimpleRadio.DeadKeyDictionary.Clear();
                    Debug.Log("All responders cleared.");
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }

        private void DrawComplexRadioTab()
        {
            EditorGUILayout.HelpBox("Complex Radio System - Channel-based communication", MessageType.Info);
            GUILayout.Space(10);

            // Channels Overview
            GUILayout.Label("Active Channels", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            int totalChannels = ComplexRadio.channels.Count;
            int trackedComplexListeners = ComplexRadio.TrackedComplexListenerCount();
            GUILayout.Label($"Total Channels: {totalChannels}", EditorStyles.boldLabel);
            GUILayout.Label($"Tracked Listeners: {trackedComplexListeners}", EditorStyles.label);
            
            if (totalChannels > 0)
            {
                GUILayout.Space(5);
                foreach (var channel in ComplexRadio.channels)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"• {channel.Key}", EditorStyles.label);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"({channel.Value.ListenerCount} listeners)", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No active channels", EditorStyles.miniLabel);
            }
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Channel Controls
            GUILayout.Label("Channel Controls", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Clear All Channels", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All Channels", 
                    "This will clear all channels and their listeners. This action cannot be undone. Continue?", 
                    "Yes", "Cancel"))
                {
                    ComplexRadio.CleanUp();
                    Debug.Log("All channels cleared.");
                }
            }
            
            GUILayout.EndVertical();
        }

        private void DrawUtilitiesTab()
        {
            EditorGUILayout.HelpBox("System Utilities - Advanced control and debugging tools", MessageType.Info);
            GUILayout.Space(10);

            // System Reset
            GUILayout.Label("System Reset", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.HelpBox("Reset all Signalia systems. This will clear all listeners, responders, channels, and reset runtime values.", MessageType.Warning);
            
            if (GUILayout.Button("Reset All Systems", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Reset All Systems", 
                    "This will completely reset all Signalia systems. This action cannot be undone. Continue?", 
                    "Yes", "Cancel"))
                {
                    Watchman.ResetEverything(false);
                    Debug.Log("All Signalia systems reset.");
                }
            }
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Debug Controls
            GUILayout.Label("Debug Controls", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            var config = ConfigReader.GetConfig();
            if (config != null)
            {
                bool debuggingEnabled = EditorGUILayout.Toggle("Enable Debugging", config.EnableDebugging);
                if (debuggingEnabled != config.EnableDebugging)
                {
                    config.EnableDebugging = debuggingEnabled;
                    EditorUtility.SetDirty(config);
                }
                
                bool introspectionEnabled = EditorGUILayout.Toggle(new GUIContent("Use Introspection", "Enables listener tracking for System Vitals. Disable for better performance."), config.UseIntrospection);
                if (introspectionEnabled != config.UseIntrospection)
                {
                    bool wasDisabled = !config.UseIntrospection; // Check the OLD value before changing it
                    config.UseIntrospection = introspectionEnabled;
                    EditorUtility.SetDirty(config);
                    
                    // Show info message if introspection was just enabled during play mode
                    if (wasDisabled && introspectionEnabled && Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog("Introspection Enabled", 
                            "Introspection has been enabled during play mode.\n\n" +
                            "Note: Existing listeners that were created before introspection was enabled will not appear in the System Vitals window.\n\n" +
                            "To see all listeners, restart the game or create new listeners after enabling introspection.", 
                            "OK");
                    }
                }
                
                if (config.EnableDebugging)
                {
                    GUILayout.Space(5);
                    config.LogListenerCreation = EditorGUILayout.Toggle("Log Listener Creation", config.LogListenerCreation);
                    config.LogListenerDisposal = EditorGUILayout.Toggle("Log Listener Disposal", config.LogListenerDisposal);
                    config.LogEventSend = EditorGUILayout.Toggle("Log Event Send", config.LogEventSend);
                    config.LogEventReceive = EditorGUILayout.Toggle("Log Event Receive", config.LogEventReceive);
                    config.LogLiveKeyCreation = EditorGUILayout.Toggle("Log LiveKey Creation", config.LogLiveKeyCreation);
                    config.LogLiveKeyRead = EditorGUILayout.Toggle("Log LiveKey Read", config.LogLiveKeyRead);
                    config.LogLiveKeyDisposal = EditorGUILayout.Toggle("Log LiveKey Disposal", config.LogLiveKeyDisposal);
                    config.LogDeadKeyCreation = EditorGUILayout.Toggle("Log DeadKey Creation", config.LogDeadKeyCreation);
                    config.LogDeadKeyRead = EditorGUILayout.Toggle("Log DeadKey Read", config.LogDeadKeyRead);
                    config.LogDeadKeyDisposal = EditorGUILayout.Toggle("Log DeadKey Disposal", config.LogDeadKeyDisposal);
                    
                    GUILayout.Space(5);
                    GUILayout.Label("ComplexRadio Debugging:", EditorStyles.boldLabel);
                    config.LogComplexListenerCreation = EditorGUILayout.Toggle("Log Complex Listener Creation", config.LogComplexListenerCreation);
                    config.LogComplexListenerDisposal = EditorGUILayout.Toggle("Log Complex Listener Disposal", config.LogComplexListenerDisposal);
                    config.LogChannelCreation = EditorGUILayout.Toggle("Log Channel Creation", config.LogChannelCreation);
                    config.LogChannelDisposal = EditorGUILayout.Toggle("Log Channel Disposal", config.LogChannelDisposal);
                    config.LogChannelSend = EditorGUILayout.Toggle("Log Channel Send", config.LogChannelSend);
                    config.LogChannelReceive = EditorGUILayout.Toggle("Log Channel Receive", config.LogChannelReceive);
                }
            }
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Performance Info
            GUILayout.Label("Performance Information", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label($"Frame Rate: {1.0f / Time.unscaledDeltaTime:F1} FPS", EditorStyles.label);
            GUILayout.Label($"Unscaled Delta Time: {Time.unscaledDeltaTime:F4}s", EditorStyles.label);
            
            if (Application.isPlaying)
            {
                GUILayout.Label($"Game Time: {Time.time:F2}s", EditorStyles.label);
                GUILayout.Label($"Game Delta Time: {Time.deltaTime:F4}s", EditorStyles.label);
            }
            
            GUILayout.EndVertical();
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        private void DrawExtraUtilitiesTab()
        {
            EditorGUILayout.HelpBox("Advanced Search and Analysis Tools - Find listeners, responders, and analyze their subscriptions", MessageType.Info);
            GUILayout.Space(10);

            GUILayout.Space(15);

            // Listener Creator
            GUILayout.Label("Listener Creator", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Create a test listener to verify event senders exist:", EditorStyles.miniLabel);
            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Event Name:", GUILayout.Width(80));
            listenerCreatorEventName = EditorGUILayout.TextField(listenerCreatorEventName, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            listenerCreatorIsOneShot = EditorGUILayout.Toggle("One Shot", listenerCreatorIsOneShot, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create Listener", GUILayout.Width(120)))
            {
                CreateTestListener(listenerCreatorEventName, listenerCreatorIsOneShot);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("This creates a listener that logs 'Event Logged' when triggered. Use it to test if event senders are working.", MessageType.Info);
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Search Section
            GUILayout.Label("Listener & Responder Search", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            var config = ConfigReader.GetConfig();
            bool introspectionEnabled = config != null && config.UseIntrospection;
            
            EditorGUI.BeginDisabledGroup(!introspectionEnabled);
            GUILayout.BeginHorizontal();
            searchQuery = EditorGUILayout.TextField("Search Query:", searchQuery, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Search", GUILayout.Width(80)))
            {
                PerformSearch();
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            
            if (!introspectionEnabled)
            {
                EditorGUILayout.HelpBox("Search is disabled because introspection is not enabled. Enable 'Use Introspection' to search listeners.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Search for event names, method names, or partial matches in listeners and responders.", MessageType.Info);
            }
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Live Listener Inspector
            GUILayout.Label("Live Listener Inspector", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            DrawLiveListeners();
            
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Analysis Tools
            GUILayout.Label("Analysis Tools", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            if (GUILayout.Button("Analyze All Listeners", GUILayout.Height(30)))
            {
                AnalyzeAllListeners();
            }
            
            if (GUILayout.Button("Analyze All Responders", GUILayout.Height(30)))
            {
                AnalyzeAllResponders();
            }
            
            if (GUILayout.Button("Generate System Report", GUILayout.Height(30)))
            {
                GenerateSystemReport();
            }
            
            GUILayout.EndVertical();
        }

        private void DrawLiveListeners()
        {
            var config = ConfigReader.GetConfig();
            
            // Check if introspection is enabled
            if (config != null && !config.UseIntrospection)
            {
                EditorGUILayout.HelpBox("Introspection is disabled. Listener tracking is not available for performance reasons.\n\nEnable 'Use Introspection' in the config to see detailed listener information.", MessageType.Warning);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable Introspection", GUILayout.Height(25)))
                {
                    config.UseIntrospection = true;
                    EditorUtility.SetDirty(config);
                    Debug.Log("[Signalia] Introspection enabled. Listener tracking is now active.");
                    
                    // Show info message if introspection was just enabled during play mode
                    if (Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog("Introspection Enabled", 
                            "Introspection has been enabled during play mode.\n\n" +
                            "Note: Existing listeners that were created before introspection was enabled will not appear in the System Vitals window.\n\n" +
                            "To see all listeners, restart the game or create new listeners after enabling introspection.", 
                            "OK");
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(10);
                
                // Show basic counts without detailed tracking
                GUILayout.Label("Basic System Information:", EditorStyles.boldLabel);
                GUILayout.Label($"Simple Radio Responders: {SimpleRadio.LiveKeyCount() + SimpleRadio.DeadKeyCount()}", EditorStyles.miniLabel);
                GUILayout.Label($"Complex Radio Channels: {ComplexRadio.channels.Count}", EditorStyles.miniLabel);
                
                return;
            }
            
            // Show current listener counts first
            GUILayout.Label($"Total Tracked Listeners: {SimpleRadio.TrackedListenerCount() + ComplexRadio.TrackedComplexListenerCount()}", EditorStyles.boldLabel);
            GUILayout.Label($"Simple Radio Listeners: {SimpleRadio.TrackedListenerCount()}", EditorStyles.miniLabel);
            GUILayout.Label($"Complex Radio Listeners: {ComplexRadio.TrackedComplexListenerCount()}", EditorStyles.miniLabel);
            
            // Show search filter status
            if (!string.IsNullOrEmpty(searchQuery))
            {
                GUILayout.Label($"Search Filter: '{searchQuery}'", EditorStyles.miniLabel);
            }
            
            GUILayout.Space(10);
            
            // Filter listeners based on search query
            string query = string.IsNullOrEmpty(searchQuery) ? "" : searchQuery.ToLower();
            bool hasSearchFilter = !string.IsNullOrEmpty(query);
            
            // Get all listeners for filtering
            var simpleListeners = SimpleRadio.GetAllTrackedListeners();
            var complexListeners = ComplexRadio.GetAllTrackedComplexListeners();
            
            int totalTrackedListeners = SimpleRadio.TrackedListenerCount() + ComplexRadio.TrackedComplexListenerCount();
            
            if (totalTrackedListeners == 0)
            {
                EditorGUILayout.HelpBox("No active listeners found. Create some listeners in your code to see them here.", MessageType.Info);
                GUILayout.Space(10);
                
                if (GUILayout.Button("Create Test Listener", GUILayout.Height(30)))
                {
                    CreateTestListener();
                }
                return;
            }
            
            // Show Simple Radio Listeners - Split by type
            if (simpleListeners.Count > 0)
            {
                // Filter listeners based on search query
                var filteredSimpleListeners = hasSearchFilter ? 
                    simpleListeners.Where(l => 
                        l.eventName.ToLower().Contains(query) ||
                        l.methodName.ToLower().Contains(query) ||
                        l.declaringType.ToLower().Contains(query) ||
                        l.targetObjectName.ToLower().Contains(query)
                    ).ToList() : 
                    simpleListeners;
                
                // Separate listeners by type
                var simpleEvents = filteredSimpleListeners.Where(l => l.listenerType == ListenerInfo.ListenerType.SimpleEvent).ToList();
                var parameterEvents = filteredSimpleListeners.Where(l => l.listenerType == ListenerInfo.ListenerType.ParameterEvent).ToList();
                
                // Show Simple Events
                if (simpleEvents.Count > 0)
                {
                    GUILayout.Label($"Simple Events ({simpleEvents.Count})", EditorStyles.boldLabel);
                    
                    for (int i = 0; i < simpleEvents.Count; i++)
                    {
                        var listener = simpleEvents[i];
                        GUILayout.BeginHorizontal();
                        
                        GUILayout.Label($"{i + 1}. [{listener.uniqueId}]", EditorStyles.miniLabel, GUILayout.Width(80));
                        GUILayout.Label($"Event: {listener.eventName}", EditorStyles.label, GUILayout.Width(150));
                        GUILayout.Label($"→ {listener.declaringType}.{listener.methodName}", EditorStyles.miniLabel, GUILayout.Width(200));
                        GUILayout.Label($"Target: {listener.targetObjectName}", EditorStyles.miniLabel, GUILayout.Width(100));
                        
                        if (GUILayout.Button("Inspect", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            ShowTrackedListenerDetails(listener);
                        }
                        
                        if (GUILayout.Button("Send", GUILayout.Width(50), GUILayout.Height(20)))
                        {
                            SimpleRadio.SendEvent(listener.eventName);
                        }
                        
                        if (GUILayout.Button("Highlight", GUILayout.Width(70), GUILayout.Height(20)))
                        {
                            HighlightListenerInHierarchy(listener);
                        }
                        
                        if (GUILayout.Button("Dispose", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            if (SimpleRadio.DisposeListener(listener.uniqueId))
                            {
                                Debug.Log($"Disposed listener [{listener.uniqueId}] for event '{listener.eventName}'");
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to dispose listener [{listener.uniqueId}]");
                            }
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(10);
                }
                
                // Show Parameter Events
                if (parameterEvents.Count > 0)
                {
                    GUILayout.Label($"Parameter Events ({parameterEvents.Count})", EditorStyles.boldLabel);
                    
                    for (int i = 0; i < parameterEvents.Count; i++)
                    {
                        var listener = parameterEvents[i];
                        GUILayout.BeginHorizontal();
                        
                        GUILayout.Label($"{i + 1}. [{listener.uniqueId}]", EditorStyles.miniLabel, GUILayout.Width(80));
                        GUILayout.Label($"Event: {listener.eventName}", EditorStyles.label, GUILayout.Width(150));
                        GUILayout.Label($"→ {listener.declaringType}.{listener.methodName}", EditorStyles.miniLabel, GUILayout.Width(200));
                        GUILayout.Label($"Target: {listener.targetObjectName}", EditorStyles.miniLabel, GUILayout.Width(100));
                        
                        if (GUILayout.Button("Inspect", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            ShowTrackedListenerDetails(listener);
                        }
                        
                        if (GUILayout.Button("Send", GUILayout.Width(50), GUILayout.Height(20)))
                        {
                            EditorUtility.DisplayDialog("Parameter Event", 
                                $"Cannot send parameter event '{listener.eventName}' without parameters.\n\n" +
                                $"Use SimpleRadio.SendEvent(\"{listener.eventName}\", parameters) in code.", 
                                "OK");
                        }
                        
                        if (GUILayout.Button("Highlight", GUILayout.Width(70), GUILayout.Height(20)))
                        {
                            HighlightListenerInHierarchy(listener);
                        }
                        
                        if (GUILayout.Button("Dispose", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            if (SimpleRadio.DisposeListener(listener.uniqueId))
                            {
                                Debug.Log($"Disposed listener [{listener.uniqueId}] for event '{listener.eventName}'");
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to dispose listener [{listener.uniqueId}]");
                            }
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(10);
                }
            }
            
            // Show Complex Radio Listeners
            if (complexListeners.Count > 0)
            {
                // Filter listeners based on search query
                var filteredComplexListeners = hasSearchFilter ? 
                    complexListeners.Where(l => 
                        l.eventName.ToLower().Contains(query) ||
                        l.methodName.ToLower().Contains(query) ||
                        l.declaringType.ToLower().Contains(query) ||
                        l.targetObjectName.ToLower().Contains(query)
                    ).ToList() : 
                    complexListeners;
                
                if (filteredComplexListeners.Count > 0)
                {
                    GUILayout.Label($"Complex Radio Listeners ({filteredComplexListeners.Count})", EditorStyles.boldLabel);
                    
                    for (int i = 0; i < filteredComplexListeners.Count; i++)
                    {
                        var listener = filteredComplexListeners[i];
                        GUILayout.BeginHorizontal();
                        
                        GUILayout.Label($"{i + 1}. [{listener.uniqueId}]", EditorStyles.miniLabel, GUILayout.Width(80));
                        GUILayout.Label($"Channel: {listener.eventName}", EditorStyles.label, GUILayout.Width(150));
                        GUILayout.Label($"→ {listener.declaringType}.{listener.methodName}", EditorStyles.miniLabel, GUILayout.Width(200));
                        GUILayout.Label($"Target: {listener.targetObjectName}", EditorStyles.miniLabel, GUILayout.Width(100));
                        
                        if (GUILayout.Button("Inspect", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            ShowTrackedListenerDetails(listener);
                        }
                        
                        if (GUILayout.Button("Send", GUILayout.Width(50), GUILayout.Height(20)))
                        {
                            ComplexRadio.Channel(listener.eventName).Send("TestMessage", "Test Data");
                        }
                        
                        if (GUILayout.Button("Highlight", GUILayout.Width(70), GUILayout.Height(20)))
                        {
                            HighlightListenerInHierarchy(listener);
                        }
                        
                        if (GUILayout.Button("Dispose", GUILayout.Width(60), GUILayout.Height(20)))
                        {
                            if (ComplexRadio.DisposeComplexListener(listener.uniqueId))
                            {
                                Debug.Log($"Disposed complex listener [{listener.uniqueId}] for channel '{listener.eventName}'");
                            }
                            else
                            {
                                Debug.LogWarning($"Failed to dispose complex listener [{listener.uniqueId}]");
                            }
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(10);
                }
            }
            
            // Show filtered results message
            if (hasSearchFilter)
            {
                int totalFiltered = (simpleListeners.Where(l => 
                    l.eventName.ToLower().Contains(query) ||
                    l.methodName.ToLower().Contains(query) ||
                    l.declaringType.ToLower().Contains(query) ||
                    l.targetObjectName.ToLower().Contains(query)
                ).Count()) + 
                (complexListeners.Where(l => 
                    l.eventName.ToLower().Contains(query) ||
                    l.methodName.ToLower().Contains(query) ||
                    l.declaringType.ToLower().Contains(query) ||
                    l.targetObjectName.ToLower().Contains(query)
                ).Count());
                
                if (totalFiltered == 0)
                {
                    EditorGUILayout.HelpBox($"No listeners found matching '{searchQuery}'. Try searching for event names, method names, or class names.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Showing {totalFiltered} listeners matching '{searchQuery}'", MessageType.Info);
                }
            }
            
            // Show responder information as well
            GUILayout.Space(15);
            GUILayout.Label("Active Responders:", EditorStyles.boldLabel);
            int staticCount = SimpleRadio.LiveKeyCount();
            int nonStaticCount = SimpleRadio.DeadKeyCount();
            
            if (staticCount > 0)
            {
                GUILayout.Label($"LiveKey Responders ({staticCount}):", EditorStyles.miniLabel);
                foreach (var responder in SimpleRadio.LiveKeyDictionary)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"• {responder.Key}", EditorStyles.miniLabel, GUILayout.Width(300));
                    if (GUILayout.Button("Inspect", GUILayout.Width(80), GUILayout.Height(20)))
                    {
                        ShowResponderDetails(responder.Key, "LiveKey Responder");
                    }
                    GUILayout.EndHorizontal();
                }
            }
            
            if (nonStaticCount > 0)
            {
                GUILayout.Label($"DeadKey Responders ({nonStaticCount}):", EditorStyles.miniLabel);
                foreach (var responder in SimpleRadio.DeadKeyDictionary)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"• {responder.Key}", EditorStyles.miniLabel, GUILayout.Width(300));
                    if (GUILayout.Button("Inspect", GUILayout.Width(80), GUILayout.Height(20)))
                    {
                        ShowResponderDetails(responder.Key, "DeadKey Responder");
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void CreateTestListener()
        {
            // Create a simple test listener to demonstrate the functionality
            new Listener("TestEvent", () => {
                Debug.Log("Test listener triggered!");
            });
            
            Debug.Log("Test listener created! Check the Live Listener Inspector.");
        }

        private void CreateTestListener(string eventName, bool oneShot = false)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                EditorUtility.DisplayDialog("Invalid Event Name", "Please enter an event name for the listener.", "OK");
                return;
            }
            
            // Create a test listener with the specified event name
            new Listener(eventName, () => {
                Debug.Log($"Event Logged: '{eventName}' was triggered!");
            }, oneShot);
            
            Debug.Log($"Test listener created for event '{eventName}'! Check the Live Listener Inspector.");
        }

        private void ShowTrackedListenerDetails(ListenerInfo listener)
        {
            string details = $"=== TRACKED LISTENER DETAILS ===\n" +
                           $"Unique ID: {listener.uniqueId}\n" +
                           $"Event Name: {listener.eventName}\n" +
                           $"Method: {listener.declaringType}.{listener.methodName}\n" +
                           $"Target Object: {listener.targetObjectName}\n" +
                           $"Listener Type: {listener.listenerType}\n" +
                           $"One Shot: {listener.isOneShot}\n" +
                           $"Creation Time: {listener.creationTime:yyyy-MM-dd HH:mm:ss}\n\n";
            
            Debug.Log(details);
            EditorUtility.DisplayDialog("Tracked Listener Details", 
                $"Listener details have been logged to the Console.\n\n" +
                $"ID: {listener.uniqueId}\n" +
                $"Event: {listener.eventName}\n" +
                $"Method: {listener.declaringType}.{listener.methodName}\n" +
                $"Target: {listener.targetObjectName}\n" +
                $"Type: {listener.listenerType}", 
                "OK");
        }

        private void ShowResponderDetails(string eventName, string responderType)
        {
            string details = $"=== RESPONDER DETAILS ===\n" +
                           $"Type: {responderType}\n" +
                           $"Event Name: {eventName}\n" +
                           $"Location: {(responderType == "LiveKey Responder" ? "SimpleRadio.LiveKeyDictionary" : "SimpleRadio.DeadKeyDictionary")}\n\n";
            
            // Try to get more information about the responder
            try
            {
                if (responderType == "LiveKey Responder" && SimpleRadio.LiveKeyDictionary.ContainsKey(eventName))
                {
                    var responder = SimpleRadio.LiveKeyDictionary[eventName];
                    details += $"Value Provider Type: {responder.GetType().Name}\n";
                    details += $"Value Provider Method: {responder.Method.Name}\n";
                    details += $"Declaring Type: {responder.Method.DeclaringType?.Name}\n";
                }
                else if (responderType == "DeadKey Responder" && SimpleRadio.DeadKeyDictionary.ContainsKey(eventName))
                {
                    var responder = SimpleRadio.DeadKeyDictionary[eventName];
                    details += $"Value Type: {responder.GetType().Name}\n";
                    details += $"Value: {responder}\n";
                }
            }
            catch (System.Exception ex)
            {
                details += $"Error getting responder details: {ex.Message}\n";
            }
            
            Debug.Log(details);
            EditorUtility.DisplayDialog("Responder Details", 
                $"Responder details have been logged to the Console.\n\n" +
                $"Event: {eventName}\n" +
                $"Type: {responderType}", 
                "OK");
        }

        private void ShowListenerDetails(System.Delegate listener, string listenerType)
        {
            string details = $"=== LISTENER DETAILS ===\n" +
                           $"Type: {listenerType}\n" +
                           $"Method: {listener.Method.DeclaringType?.Name}.{listener.Method.Name}\n" +
                           $"Target: {(listener.Target != null ? listener.Target.ToString() : "Static")}\n" +
                           $"Return Type: {listener.Method.ReturnType.Name}\n\n" +
                           $"Parameters:\n";
            
            var parameters = listener.Method.GetParameters();
            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    details += $"  {i + 1}. {param.ParameterType.Name} {param.Name}";
                    if (param.HasDefaultValue)
                        details += $" (default: {param.DefaultValue})";
                    details += "\n";
                }
            }
            else
            {
                details += "  None\n";
            }
            
            details += $"\nMethod Attributes:\n";
            var attributes = listener.Method.GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                foreach (var attr in attributes)
                {
                    details += $"  - {attr.GetType().Name}\n";
                }
            }
            else
            {
                details += "  None\n";
            }
            
            Debug.Log(details);
            EditorUtility.DisplayDialog("Listener Details", 
                $"Listener details have been logged to the Console.\n\n" +
                $"Method: {listener.Method.DeclaringType?.Name}.{listener.Method.Name}\n" +
                $"Parameters: {parameters.Length}", 
                "OK");
        }

        private void PerformSearch()
        {
            searchResults.Clear();
            
            if (string.IsNullOrEmpty(searchQuery))
                return;

            string query = searchQuery.ToLower();
            
            // Search in tracked Simple Radio listeners
            var simpleListeners = SimpleRadio.GetAllTrackedListeners();
            foreach (var listener in simpleListeners)
            {
                bool matches = listener.eventName.ToLower().Contains(query) ||
                              listener.methodName.ToLower().Contains(query) ||
                              listener.declaringType.ToLower().Contains(query) ||
                              listener.targetObjectName.ToLower().Contains(query);
                              
                if (matches)
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = SearchResultType.Listener,
                        EventName = listener.eventName,
                        MethodInfo = $"{listener.declaringType}.{listener.methodName}",
                        Context = listener.targetObjectName,
                        Parameters = new string[] { listener.listenerType.ToString() }
                    });
                }
            }
            
            // Search in tracked Complex Radio listeners
            var complexListeners = ComplexRadio.GetAllTrackedComplexListeners();
            foreach (var listener in complexListeners)
            {
                bool matches = listener.eventName.ToLower().Contains(query) ||
                              listener.methodName.ToLower().Contains(query) ||
                              listener.declaringType.ToLower().Contains(query) ||
                              listener.targetObjectName.ToLower().Contains(query);
                              
                if (matches)
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = SearchResultType.Listener,
                        EventName = listener.eventName,
                        MethodInfo = $"{listener.declaringType}.{listener.methodName}",
                        Context = listener.targetObjectName,
                        Parameters = new string[] { "ComplexChannel" }
                    });
                }
            }
            
            // Search in responders
            foreach (var responder in SimpleRadio.LiveKeyDictionary)
            {
                if (responder.Key.ToLower().Contains(query))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = SearchResultType.Responder,
                        EventName = responder.Key,
                        MethodInfo = "LiveKey Responder",
                        Context = "SimpleRadio.LiveKeyDictionary"
                    });
                }
            }
            
            foreach (var responder in SimpleRadio.DeadKeyDictionary)
            {
                if (responder.Key.ToLower().Contains(query))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = SearchResultType.Responder,
                        EventName = responder.Key,
                        MethodInfo = "DeadKey Responder",
                        Context = "SimpleRadio.DeadKeyDictionary"
                    });
                }
            }
            
            // Search in Complex Radio channels
            foreach (var channel in ComplexRadio.channels)
            {
                if (channel.Key.ToLower().Contains(query))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = SearchResultType.Channel,
                        EventName = channel.Key,
                        MethodInfo = $"Channel with {channel.Value.ListenerCount} listeners",
                        Context = "ComplexRadio.channels"
                    });
                }
            }
        }

        private void AnalyzeAllListeners()
        {
            string report = $"=== LISTENER ANALYSIS ===\n" +
                           $"Generated: {System.DateTime.Now}\n\n" +
                           $"Total Tracked Listeners: {SimpleRadio.TrackedListenerCount() + ComplexRadio.TrackedComplexListenerCount()}\n" +
                           $"- Simple Radio Listeners: {SimpleRadio.TrackedListenerCount()}\n" +
                           $"- Complex Radio Listeners: {ComplexRadio.TrackedComplexListenerCount()}\n\n";
            
            // Simple Radio Listeners
            var simpleListeners = SimpleRadio.GetAllTrackedListeners();
            if (simpleListeners.Count > 0)
            {
                report += "=== SIMPLE RADIO LISTENERS ===\n";
                for (int i = 0; i < simpleListeners.Count; i++)
                {
                    var listener = simpleListeners[i];
                    report += $"{i + 1}. [{listener.uniqueId}] Event: {listener.eventName}\n";
                    report += $"   Method: {listener.declaringType}.{listener.methodName}\n";
                    report += $"   Target: {listener.targetObjectName}\n";
                    report += $"   Type: {listener.listenerType}\n";
                    report += $"   One Shot: {listener.isOneShot}\n";
                    report += $"   Created: {listener.creationTime:yyyy-MM-dd HH:mm:ss}\n\n";
                }
            }
            
            // Complex Radio Listeners
            var complexListeners = ComplexRadio.GetAllTrackedComplexListeners();
            if (complexListeners.Count > 0)
            {
                report += "=== COMPLEX RADIO LISTENERS ===\n";
                for (int i = 0; i < complexListeners.Count; i++)
                {
                    var listener = complexListeners[i];
                    report += $"{i + 1}. [{listener.uniqueId}] Channel: {listener.eventName}\n";
                    report += $"   Method: {listener.declaringType}.{listener.methodName}\n";
                    report += $"   Target: {listener.targetObjectName}\n";
                    report += $"   Type: {listener.listenerType}\n";
                    report += $"   One Shot: {listener.isOneShot}\n";
                    report += $"   Created: {listener.creationTime:yyyy-MM-dd HH:mm:ss}\n\n";
                }
            }
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("Listener Analysis Complete", 
                $"Analysis complete! Check the Console for detailed listener information.\n\n" +
                $"Total Tracked Listeners: {SimpleRadio.TrackedListenerCount() + ComplexRadio.TrackedComplexListenerCount()}\n" +
                $"Simple Radio: {SimpleRadio.TrackedListenerCount()}\n" +
                $"Complex Radio: {ComplexRadio.TrackedComplexListenerCount()}", 
                "OK");
        }

        private void AnalyzeAllResponders()
        {
            int staticCount = SimpleRadio.LiveKeyCount();
            int nonStaticCount = SimpleRadio.DeadKeyCount();
            
            string report = $"Responder Analysis Complete:\n\n" +
                           $"LiveKey Responders ({staticCount}):\n";
            
            foreach (var responder in SimpleRadio.LiveKeyDictionary)
            {
                report += $"  • {responder.Key}\n";
            }
            
            report += $"\nDeadKey Responders ({nonStaticCount}):\n";
            foreach (var responder in SimpleRadio.DeadKeyDictionary)
            {
                report += $"  • {responder.Key}\n";
            }
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("Responder Analysis", 
                $"Analysis complete! Check the Console for detailed report.\n\n" +
                $"Total Responders: {staticCount + nonStaticCount}", 
                "OK");
        }

        private void GenerateSystemReport()
        {
            string report = $"=== SIGNALIA SYSTEM REPORT ===\n" +
                           $"Generated: {System.DateTime.Now}\n\n" +
                           $"=== RUNTIME STATUS ===\n" +
                           $"Watchman Active: {(Watchman.Instance != null ? "Yes" : "No")}\n" +
                           $"Debugging Enabled: {RuntimeValues.Debugging.IsDebugging}\n\n" +
                           $"=== SIMPLE RADIO ===\n" +
                           $"Total Tracked Listeners: {SimpleRadio.TrackedListenerCount()}\n" +
                           $"Total Responders: {SimpleRadio.LiveKeyCount() + SimpleRadio.DeadKeyCount()}\n" +
                           $"  - LiveKey Responders: {SimpleRadio.LiveKeyCount()}\n" +
                           $"  - DeadKey Responders: {SimpleRadio.DeadKeyCount()}\n\n";
            
            // Add detailed tracked listener information
            var simpleListeners = SimpleRadio.GetAllTrackedListeners();
            if (simpleListeners.Count > 0)
            {
                report += "=== SIMPLE RADIO LISTENERS DETAIL ===\n";
                for (int i = 0; i < simpleListeners.Count; i++)
                {
                    var listener = simpleListeners[i];
                    report += $"{i + 1}. [{listener.uniqueId}] Event: {listener.eventName}\n";
                    report += $"   Method: {listener.declaringType}.{listener.methodName}\n";
                    report += $"   Target: {listener.targetObjectName}\n";
                    report += $"   Type: {listener.listenerType}\n";
                    report += $"   Created: {listener.creationTime:yyyy-MM-dd HH:mm:ss}\n";
                }
                report += "\n";
            }
            
            report += $"=== COMPLEX RADIO ===\n" +
                      $"Active Channels: {ComplexRadio.channels.Count}\n" +
                      $"Total Tracked Listeners: {ComplexRadio.TrackedComplexListenerCount()}\n";
            
            foreach (var channel in ComplexRadio.channels)
            {
                report += $"  - {channel.Key}: {channel.Value.ListenerCount} listeners\n";
            }
            
            // Add detailed complex listener information
            var complexListeners = ComplexRadio.GetAllTrackedComplexListeners();
            if (complexListeners.Count > 0)
            {
                report += "\n=== COMPLEX RADIO LISTENERS DETAIL ===\n";
                for (int i = 0; i < complexListeners.Count; i++)
                {
                    var listener = complexListeners[i];
                    report += $"{i + 1}. [{listener.uniqueId}] Channel: {listener.eventName}\n";
                    report += $"   Method: {listener.declaringType}.{listener.methodName}\n";
                    report += $"   Target: {listener.targetObjectName}\n";
                    report += $"   Type: {listener.listenerType}\n";
                    report += $"   Created: {listener.creationTime:yyyy-MM-dd HH:mm:ss}\n";
                }
                report += "\n";
            }
            
            report += $"\n=== MEMORY USAGE ===\n" +
                      $"GC Memory: {FormatBytes(GC.GetTotalMemory(false))}\n" +
                      $"Frame Rate: {1.0f / Time.unscaledDeltaTime:F1} FPS\n";
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("System Report Generated", 
                "Complete system report with detailed tracked listener information has been generated and logged to the Console.", 
                "OK");
        }

        private void DrawInputActionsTab()
        {
            EditorGUILayout.HelpBox("Input Actions - Real-time tracking of all Signalia input actions and their current values", MessageType.Info);
            GUILayout.Space(10);

            // Check if SignaliaInputWrapper exists
            if (!SignaliaInputWrapper.Exists)
            {
                EditorGUILayout.HelpBox("No SignaliaInputWrapper found in the scene. Input actions cannot be tracked without a wrapper.", MessageType.Warning);
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("To track input actions:\n1. Add a SignaliaInputWrapper component to a GameObject in your scene\n2. Implement PollInput() to call SignaliaInputBridge methods", MessageType.Info);
                return;
            }

            // Get all action maps
            var actionMaps = ResourceHandler.GetInputActionMaps();
            if (actionMaps == null || actionMaps.Length == 0)
            {
                EditorGUILayout.HelpBox("No Input Action Maps found. Configure action maps in Tools > Signalia > Settings > Assets", MessageType.Warning);
                return;
            }

            // Input System Status
            GUILayout.Label("Input System Status", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            
            bool inputDisabled = SignaliaInputBridge.IsInputDisabled;
            GUILayout.Label($"Input System: {(inputDisabled ? "✗ Disabled" : "✓ Enabled")}", inputDisabled ? EditorStyles.label : EditorStyles.boldLabel);
            
            int totalActions = 0;
            int enabledActions = 0;
            int disabledActions = 0;
            int actionsOnCooldown = 0;
            
            foreach (var map in actionMaps)
            {
                if (map == null || map.Actions == null) continue;
                foreach (var action in map.Actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.ActionName)) continue;
                    totalActions++;
                    if (SIGS.IsInputActionEnabled(action.ActionName))
                    {
                        enabledActions++;
                    }
                    else
                    {
                        disabledActions++;
                    }
                    if (SIGS.IsInputOnCooldown(action.ActionName))
                    {
                        actionsOnCooldown++;
                    }
                }
            }
            
            GUILayout.Label($"Total Actions: {totalActions}", EditorStyles.label);
            GUILayout.Label($"  • Enabled: {enabledActions}", EditorStyles.miniLabel);
            GUILayout.Label($"  • Disabled: {disabledActions}", EditorStyles.miniLabel);
            if (actionsOnCooldown > 0)
            {
                GUILayout.Label($"  • On Cooldown: {actionsOnCooldown}", EditorStyles.miniLabel);
            }
            
            GUILayout.EndVertical();
            
            GUILayout.Space(15);

            // Display actions grouped by action map
            foreach (var map in actionMaps)
            {
                if (map == null || map.Actions == null || map.Actions.Count == 0) continue;
                
                string mapName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                bool mapEnabled = SIGS.IsInputActionMapEnabled(mapName);
                
                GUILayout.Label($"{mapName} ({(mapEnabled ? "Enabled" : "Disabled")})", EditorStyles.boldLabel);
                GUILayout.BeginVertical(GUI.skin.box);
                
                if (!mapEnabled)
                {
                    EditorGUILayout.HelpBox("This action map is disabled. Actions in this map will not respond to input.", MessageType.Info);
                }
                
                foreach (var action in map.Actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.ActionName)) continue;
                    
                    string actionName = action.ActionName;
                    bool actionEnabled = SIGS.IsInputActionEnabled(actionName);
                    bool onCooldown = SIGS.IsInputOnCooldown(actionName);
                    
                    GUILayout.BeginHorizontal();
                    
                    // Action name and type
                    GUILayout.Label($"{actionName}", EditorStyles.label, GUILayout.Width(200));
                    GUILayout.Label($"({action.ActionType})", EditorStyles.miniLabel, GUILayout.Width(80));
                    
                    // Status indicators
                    if (!actionEnabled)
                    {
                        GUILayout.Label("✗ Disabled", EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                    else if (onCooldown)
                    {
                        GUILayout.Label("⏱ Cooldown", EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                    else
                    {
                        GUILayout.Label("✓ Active", EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                    
                    GUILayout.FlexibleSpace();
                    
                    // Current value based on action type
                    if (actionEnabled && !onCooldown)
                    {
                        switch (action.ActionType)
                        {
                            case SignaliaActionType.Bool:
                                bool held = SIGS.GetInput(actionName);
                                bool down = SIGS.GetInputDown(actionName, false);
                                bool up = SIGS.GetInputUp(actionName, false);
                                
                                string boolStatus = "";
                                if (down) boolStatus = "↓ Down";
                                else if (up) boolStatus = "↑ Up";
                                else if (held) boolStatus = "● Held";
                                else boolStatus = "○ Released";
                                
                                GUILayout.Label(boolStatus, EditorStyles.miniLabel, GUILayout.Width(100));
                                break;
                                
                            case SignaliaActionType.Float:
                                float floatValue = SIGS.GetInputFloat(actionName);
                                GUILayout.Label($"Value: {floatValue:F3}", EditorStyles.miniLabel, GUILayout.Width(120));
                                break;
                                
                            case SignaliaActionType.Vector2:
                                Vector2 vectorValue = SIGS.GetInputVector2(actionName);
                                GUILayout.Label($"Value: ({vectorValue.x:F2}, {vectorValue.y:F2})", EditorStyles.miniLabel, GUILayout.Width(180));
                                break;
                        }
                    }
                    else
                    {
                        GUILayout.Label("—", EditorStyles.miniLabel, GUILayout.Width(50));
                    }
                    
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }
            
            // Legend
            GUILayout.Space(10);
            GUILayout.Label("Legend", EditorStyles.boldLabel);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("↓ Down: Pressed this frame", EditorStyles.miniLabel);
            GUILayout.Label("● Held: Currently held", EditorStyles.miniLabel);
            GUILayout.Label("↑ Up: Released this frame", EditorStyles.miniLabel);
            GUILayout.Label("○ Released: Not pressed", EditorStyles.miniLabel);
            GUILayout.Label("⏱ Cooldown: Action is on cooldown", EditorStyles.miniLabel);
            GUILayout.Label("✗ Disabled: Action or map is disabled", EditorStyles.miniLabel);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Highlights the GameObject associated with a listener in the hierarchy
        /// </summary>
        private void HighlightListenerInHierarchy(ListenerInfo listener)
        {
            GameObject targetObject = null;
            string searchMethod = "";

            // Method 1: Try to get the actual listener object and extract context
            if (listener.listenerObject != null)
            {
                try
                {
                    // For SimpleRadio listeners, try to get the target object from the listener
                    if (listener.listenerObject is Listener simpleListener)
                    {
                        // Use reflection to get the target object if available
                        var targetField = typeof(Listener).GetField("targetObject", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (targetField != null)
                        {
                            targetObject = targetField.GetValue(simpleListener) as GameObject;
                            if (targetObject != null)
                            {
                                searchMethod = "Listener Context";
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Signalia] Failed to extract context from listener object: {ex.Message}");
                }
            }

            // Method 2: Search by target object name if we have it
            if (targetObject == null && !string.IsNullOrEmpty(listener.targetObjectName) && listener.targetObjectName != "Static")
            {
                // Try to find GameObject by name
                targetObject = GameObject.Find(listener.targetObjectName);
                if (targetObject != null)
                {
                    searchMethod = "Object Name";
                }
            }

            // Method 3: Search by declaring type (class name)
            if (targetObject == null && !string.IsNullOrEmpty(listener.declaringType))
            {
                // Find all GameObjects with components of the declaring type
#if UNITY_6000_0_OR_NEWER
                var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var allObjects = FindObjectsOfType<GameObject>(true);
#endif
                foreach (var obj in allObjects)
                {
                    var component = obj.GetComponent(listener.declaringType);
                    if (component != null)
                    {
                        targetObject = obj;
                        searchMethod = "Component Type";
                        break;
                    }
                }
            }

            // Method 4: Search by method name pattern (for UI components)
            if (targetObject == null && !string.IsNullOrEmpty(listener.methodName))
            {
                // Look for common UI component patterns
                string[] uiPatterns = { "UIButton", "UIView", "UIFill", "UIAnimatable" };
                foreach (var pattern in uiPatterns)
                {
                    if (listener.methodName.Contains(pattern) || listener.declaringType.Contains(pattern))
                    {
#if UNITY_6000_0_OR_NEWER
                        var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                        var allObjects = FindObjectsOfType<GameObject>(true);
#endif
                        foreach (var obj in allObjects)
                        {
                            var component = obj.GetComponent(pattern);
                            if (component != null)
                            {
                                targetObject = obj;
                                searchMethod = "UI Component Pattern";
                                break;
                            }
                        }
                        if (targetObject != null) break;
                    }
                }
            }

            // Method 5: Search by event name pattern
            if (targetObject == null && !string.IsNullOrEmpty(listener.eventName))
            {
                // Look for GameObjects that might have this event name in their components
#if UNITY_6000_0_OR_NEWER
                var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var allObjects = FindObjectsOfType<GameObject>(true);
#endif
                foreach (var obj in allObjects)
                {
                    // Check if any component has a field or property with this event name
                    var components = obj.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null) continue;
                        
                        var fields = component.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            if (field.Name.Contains(listener.eventName) || 
                                (field.FieldType == typeof(string) && field.GetValue(component)?.ToString() == listener.eventName))
                            {
                                targetObject = obj;
                                searchMethod = "Event Name Field";
                                break;
                            }
                        }
                        if (targetObject != null) break;
                    }
                    if (targetObject != null) break;
                }
            }

            // Highlight the found object
            if (targetObject != null)
            {
                Selection.activeGameObject = targetObject;
                EditorGUIUtility.PingObject(targetObject);
                Debug.Log($"[Signalia] Highlighted GameObject '{targetObject.name}' for listener [{listener.uniqueId}] using method: {searchMethod}");
            }
            else
            {
                Debug.LogWarning($"[Signalia] Could not find GameObject for listener [{listener.uniqueId}] - Event: {listener.eventName}, Target: {listener.targetObjectName}, Type: {listener.declaringType}");
                EditorUtility.DisplayDialog("Highlight Failed", 
                    $"Could not find the GameObject associated with this listener.\n\n" +
                    $"Event: {listener.eventName}\n" +
                    $"Target: {listener.targetObjectName}\n" +
                    $"Type: {listener.declaringType}\n\n" +
                    $"This might be a static listener or the object may have been destroyed or is cross-scene or something else (I'm not sure).", 
                    "OK");
            }
        }
    }

    public enum SearchResultType
    {
        Listener,
        Responder,
        Channel
    }

    public class SearchResult
    {
        public SearchResultType Type;
        public string EventName;
        public string MethodInfo;
        public string Context;
        public string[] Parameters;
    }
}