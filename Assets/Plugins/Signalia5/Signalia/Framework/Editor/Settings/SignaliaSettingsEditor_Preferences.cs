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
        #region Tabs - Preferences

        private void DrawPreferencesTab()
        {
            EditorGUILayout.HelpBox("Editor-only preferences for Signalia. These settings are stored locally and not included in builds.", MessageType.Info);

            if (preferences == null)
            {
                preferences = PreferencesReader.GetPreferences();
            }

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Notes Component - Hierarchy Window Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize the appearance of GameObjects with Notes components in the hierarchy window.", MessageType.None);

            EditorGUI.BeginChangeCheck();

            preferences.NotesHierarchyBackgroundColor = EditorGUILayout.ColorField(
                new GUIContent("Hierarchy Background Color", 
                    "Background color tint for GameObjects with Notes components in the hierarchy window. Use semi-transparent colors (alpha < 1.0) for best results."), 
                preferences.NotesHierarchyBackgroundColor);

            preferences.NotesHierarchyTextColor = EditorGUILayout.ColorField(
                new GUIContent("Hierarchy Text Color (Foreground)", 
                    "Foreground (text) color for GameObjects with Notes components in the hierarchy window. Set alpha to 0 to use Unity's default text color."), 
                preferences.NotesHierarchyTextColor);

            if (EditorGUI.EndChangeCheck())
            {
                PreferencesReader.SavePreferences();
                // Force reload of preferences in components that use them
                PreferencesReader.ReloadPreferences();
                preferences = PreferencesReader.GetPreferences();
            }

            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(20);
            EditorGUILayout.HelpBox("Preferences are automatically saved when changed. Changes take effect immediately in the editor.", MessageType.Info);
        }


        #endregion

    }
}
