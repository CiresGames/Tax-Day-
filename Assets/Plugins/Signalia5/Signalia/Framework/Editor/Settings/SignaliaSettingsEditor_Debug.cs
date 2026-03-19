using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Utilities.SIGInput;
using AHAKuo.Signalia.Framework;

// GAME SYSTEMS
using AHAKuo.Signalia.GameSystems.AudioLayering;
using AHAKuo.Signalia.GameSystems.SaveSystem;
using AHAKuo.Signalia.GameSystems.DialogueSystem;
using AHAKuo.Signalia.GameSystems.Inventory;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.GameSystems.TutorialSystem;
using AHAKuo.Signalia.GameSystems.Localization;
using AHAKuo.Signalia.GameSystems.Localization.External;
using AHAKuo.Signalia.GameSystems.LoadingScreens;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.GameSystems.PoolingSystem;
using AHAKuo.Signalia.GameSystems.Notifications;
using AHAKuo.Signalia.GameSystems.AchievementSystem;

namespace AHAKuo.Signalia.Framework.Editors
{
    public partial class FrameworkSettings : EditorWindow
    {
        #region Tabs - Debug

        private void DrawDebugging()
        {
            EditorGUILayout.HelpBox("Debugging settings for Signalia.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.EnableDebugging = EditorGUILayout.Toggle(new GUIContent("Enable Debugging", "Enables debugging for Signalia."), config.EnableDebugging);
            
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

                config.LogListenerCreation = EditorGUILayout.Toggle(new GUIContent("Log Listener Creation", "Logs listener creation."), config.LogListenerCreation);
                config.LogListenerDisposal = EditorGUILayout.Toggle(new GUIContent("Log Listener Disposal", "Logs listener disposal."), config.LogListenerDisposal);
                config.LogEventSend = EditorGUILayout.Toggle(new GUIContent("Log Event Send", "Logs event sending."), config.LogEventSend);
                config.LogEventReceive = EditorGUILayout.Toggle(new GUIContent("Log Event Receive", "Logs event receiving."), config.LogEventReceive);
                config.LogLiveKeyCreation = EditorGUILayout.Toggle(new GUIContent("Log LiveKey Creation", "Logs live key creation."), config.LogLiveKeyCreation);
                config.LogLiveKeyRead = EditorGUILayout.Toggle(new GUIContent("Log LiveKey Read", "Logs live key read."), config.LogLiveKeyRead);
                config.LogLiveKeyDisposal = EditorGUILayout.Toggle(new GUIContent("Log LiveKey Disposal", "Logs live key disposal."), config.LogLiveKeyDisposal);
                config.LogDeadKeyCreation = EditorGUILayout.Toggle(new GUIContent("Log DeadKey Creation", "Logs dead key creation."), config.LogDeadKeyCreation);
                config.LogDeadKeyRead = EditorGUILayout.Toggle(new GUIContent("Log DeadKey Read", "Logs dead key read."), config.LogDeadKeyRead);
                config.LogDeadKeyDisposal = EditorGUILayout.Toggle(new GUIContent("Log DeadKey Disposal", "Logs dead key disposal."), config.LogDeadKeyDisposal);
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("ComplexRadio Debugging:", EditorStyles.boldLabel);
                config.LogComplexListenerCreation = EditorGUILayout.Toggle(new GUIContent("Log Complex Listener Creation", "Logs complex listener creation."), config.LogComplexListenerCreation);
                config.LogComplexListenerDisposal = EditorGUILayout.Toggle(new GUIContent("Log Complex Listener Disposal", "Logs complex listener disposal."), config.LogComplexListenerDisposal);
                config.LogChannelCreation = EditorGUILayout.Toggle(new GUIContent("Log Channel Creation", "Logs channel creation."), config.LogChannelCreation);
                config.LogChannelDisposal = EditorGUILayout.Toggle(new GUIContent("Log Channel Disposal", "Logs channel disposal."), config.LogChannelDisposal);
                config.LogChannelSend = EditorGUILayout.Toggle(new GUIContent("Log Channel Send", "Logs channel message sending."), config.LogChannelSend);
                config.LogChannelReceive = EditorGUILayout.Toggle(new GUIContent("Log Channel Receive", "Logs channel message receiving."), config.LogChannelReceive);
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Haptics Debugging:", EditorStyles.boldLabel);
                config.LogHaptics = EditorGUILayout.Toggle(new GUIContent("Log Haptics", "Logs haptic feedback events."), config.LogHaptics);
            }

            EditorGUIUtility.labelWidth = 0;
        }


        #endregion

    }
}
