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
        #region Tabs - Radio and Effects

        private void DrawRadioAndEffectsTab()
        {
            EditorGUILayout.HelpBox("Set event names that are triggered when specific UI actions occur.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Event Configuration - Senders", EditorStyles.boldLabel);

            config.UIViewAnimatingIn = EditorGUILayout.TextField(new GUIContent("Event: View Animating In", "Event triggered when a UI view starts animating in."), config.UIViewAnimatingIn);
            config.UIViewAnimatingOut = EditorGUILayout.TextField(new GUIContent("Event: View Animating Out", "Event triggered when a UI view starts animating out."), config.UIViewAnimatingOut);
            config.UIButtonClicked = EditorGUILayout.TextField(new GUIContent("Event: Button Clicked", "Event triggered when a UI button is clicked."), config.UIButtonClicked);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Event Configuration - Receivers", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Define actions to be taken when certain UI events are received.", MessageType.Info);

            config.UIButtonsDisabler = EditorGUILayout.TextField(new GUIContent("Receiver: Disable Buttons", "Disables UI buttons upon event trigger."), config.UIButtonsDisabler);
            config.UIButtonsEnabler = EditorGUILayout.TextField(new GUIContent("Receiver: Enable Buttons", "Enables UI buttons upon event trigger."), config.UIButtonsEnabler);
            config.UnityEventSystemOff = EditorGUILayout.TextField(new GUIContent("Receiver: Event System Off", "Turns off Unity's event system upon event trigger."), config.UnityEventSystemOff);
            config.UnityEventSystemOn = EditorGUILayout.TextField(new GUIContent("Receiver: Event System On", "Turns on Unity's event system upon event trigger."), config.UnityEventSystemOn);

            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Clickback Audio Settings:", EditorStyles.boldLabel);
            
            EditorGUIUtility.labelWidth = LABEL_WIDTH;
            
            // audio dropdown for clickback button
            EditorHelpers.DrawAudioDropdown_SettingsWindow("Clickback Clip", config.ClickBackAudio, this, config);
            config.AlwaysClickBackAudio = EditorGUILayout.Toggle(new GUIContent("Always Play Clickback Audio", "Always plays the clickback audio when the back button is clicked, if not, plays only when it did something."), config.AlwaysClickBackAudio);
            
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Auto-Add Settings:", EditorStyles.boldLabel);
            
            EditorGUIUtility.labelWidth = LABEL_WIDTH;
            
            config.AutoAddEffector = EditorGUILayout.Toggle(new GUIContent("Auto-Add Effector", "Automatically adds an effector to UI elements."), config.AutoAddEffector);
            config.DisableAudioMixerLoading = EditorGUILayout.Toggle(new GUIContent("Disable Audio Mixer Loading", "Prevents automatic loading of audio mixer settings on startup."), config.DisableAudioMixerLoading);
            
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Radio Logic Settings:", EditorStyles.boldLabel);
            
            EditorGUIUtility.labelWidth = LABEL_WIDTH;
            
            config.TwoSideListeners = EditorGUILayout.Toggle(new GUIContent("Two Side Listeners", "Creates both simple and parameter listeners for each event, allowing both parameter and non-parameter sends to invoke the same listener. This exists because some games rely on the old behavior where simple listeners also responded to parameter events. Keeping this OFF improves performance by avoiding duplicate listener creation."), config.TwoSideListeners);
            
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Haptics Settings:", EditorStyles.boldLabel);
            
            // Haptics settings
            config.EnableHaptics = EditorGUILayout.Toggle(new GUIContent("Enable Haptics", "Enables haptic feedback system."), config.EnableHaptics);

            config.Haptics_GlobalIntensityMultiplier = EditorGUILayout.Slider(new GUIContent("Global Intensity Multiplier", "Global multiplier for haptic intensity."), config.Haptics_GlobalIntensityMultiplier, 0f, 1f);

            config.Haptics_GlobalDurationMultiplier = EditorGUILayout.Slider(new GUIContent("Global Duration Multiplier", "Global multiplier for haptic duration."), config.Haptics_GlobalDurationMultiplier, 0.1f, 3f);
            
            config.HapticsSaveKey = EditorGUILayout.TextField(new GUIContent("Haptic Save Key", "The save key used by the game saving system or the player prefs when the haptic option is disabled or enabled at runtime."), config.HapticsSaveKey);

            EditorGUILayout.Space(5);
            
            // Haptic testing section
            EditorGUILayout.LabelField("Haptic Testing:", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test Haptic:", GUILayout.Width(100));
            
            HapticType testHapticType = (HapticType)EditorGUILayout.EnumPopup(HapticType.Light, GUILayout.Width(120));
            
            if (GUILayout.Button("Test", GUILayout.Width(60)))
            {
                HapticsManager.TriggerHapticPreset(testHapticType);
            }
            
            if (GUILayout.Button("Stop All", GUILayout.Width(80)))
            {
                HapticsManager.StopAllHaptics();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Get Device Info", GUILayout.Width(150)))
            {
                string deviceInfo = HapticsManager.GetHapticDeviceInfo();
                Debug.Log(deviceInfo);
                EditorUtility.DisplayDialog("Haptic Device Info", deviceInfo, "OK");
            }
            
            EditorGUI.EndDisabledGroup();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Haptic testing is only available in Play Mode.", MessageType.Info);
            }

            EditorGUIUtility.labelWidth = 0;
        }

        #endregion

    }
}
