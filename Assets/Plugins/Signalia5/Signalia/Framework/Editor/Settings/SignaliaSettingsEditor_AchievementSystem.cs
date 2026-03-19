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
        #region Game Systems - Achievement System

        private void DrawAchievementSystemTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.AchievementSystemHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Achievements are defined using AchievementSO assets referenced in Signalia Settings. " +
                "Unlock them at runtime via SIGS.UnlockAchievement(\"your_id\") to persist via GameSaving and optionally show a notification + trigger backend calls.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Storage
            EditorGUILayout.LabelField("Storage", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            config.AchievementSystem.SaveFileName = EditorGUILayout.TextField(
                new GUIContent("Save File Name", "Save file used to persist unlocked achievements (without extension)."),
                config.AchievementSystem.SaveFileName,
                GUILayout.MaxWidth(maxWidthProperties)
            );
            config.AchievementSystem.SaveKeyPrefix = EditorGUILayout.TextField(
                new GUIContent("Save Key Prefix", "Prefix used for all achievement save keys (e.g., 'ach_')."),
                config.AchievementSystem.SaveKeyPrefix,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Notifications
            EditorGUILayout.LabelField("Notifications", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            config.AchievementSystem.ShowNotifications = EditorGUILayout.Toggle(
                new GUIContent("Show Notifications", "Show a Notification System message when an achievement is unlocked."),
                config.AchievementSystem.ShowNotifications
            );
            if (config.AchievementSystem.ShowNotifications)
            {
                EditorGUI.indentLevel++;
                config.AchievementSystem.NotificationSystemMessageName = EditorGUILayout.TextField(
                    new GUIContent("SystemMessage Name", "Name used by NotificationMethods.ShowNotification. Requires a SystemMessage registered as DeadKey 'SystemMessage_<Name>'."),
                    config.AchievementSystem.NotificationSystemMessageName,
                    GUILayout.MaxWidth(maxWidthProperties)
                );
                config.AchievementSystem.NotificationFormat = EditorGUILayout.TextField(
                    new GUIContent("Format", "String.Format pattern. {0} = achievement title."),
                    config.AchievementSystem.NotificationFormat,
                    GUILayout.MaxWidth(maxWidthProperties)
                );
                EditorGUI.indentLevel--;

            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Events + Backend
            EditorGUILayout.LabelField("Events & Backend", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            config.AchievementSystem.OnAchievementUnlockedEvent = EditorGUILayout.TextField(
                new GUIContent("On Unlocked Event", "Radio event sent when an achievement is unlocked. Parameter: AchievementSO"),
                config.AchievementSystem.OnAchievementUnlockedEvent,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            SerializedObject serializedConfig = new SerializedObject(config);
            SerializedProperty backendAdapterProp = serializedConfig.FindProperty("AchievementSystem.BackendAdapter");
            if (backendAdapterProp != null)
            {
                EditorGUILayout.PropertyField(backendAdapterProp, new GUIContent("Backend Adapter"), true);
                serializedConfig.ApplyModifiedProperties();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Display Options
            EditorGUILayout.LabelField("Display Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            config.AchievementSystem.ReshowUnlockedAchievements = EditorGUILayout.Toggle(
                new GUIContent("Reshow Unlocked Achievements", "If enabled, unlocked achievements will still be displayed in viewers. If disabled, they will be hidden after unlocking."),
                config.AchievementSystem.ReshowUnlockedAchievements
            );
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Definitions
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Scan & Cache AchievementSOs from Project", GUILayout.Height(28)))
                ScanAndCacheAchievementSOs();

            SerializedObject serializedConfig2 = new SerializedObject(config);
            SerializedProperty achievementsProp = serializedConfig2.FindProperty("AchievementSystem.Achievements");
            if (achievementsProp != null)
            {
                EditorGUILayout.PropertyField(achievementsProp, true);
                serializedConfig2.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Unable to display Achievements list. This may be a serialization issue.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();

            EditorUtility.SetDirty(config);
        }

        private void ScanAndCacheAchievementSOs()
        {
            try
            {
                SignaliaConfigAsset config = ConfigReader.GetConfig(true);
                if (config == null)
                {
                    EditorUtility.DisplayDialog("Error", "Signalia Config not found. Please ensure SignaliaConfig asset exists.", "OK");
                    return;
                }

                var found = new List<AchievementSO>();
                string[] guids = AssetDatabase.FindAssets("t:AchievementSO", new[] { "Assets" });

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AchievementSO asset = AssetDatabase.LoadAssetAtPath<AchievementSO>(path);
                    if (asset != null)
                        found.Add(asset);
                }

                config.AchievementSystem.Achievements = found.ToArray();
                EditorUtility.SetDirty(config);

                EditorUtility.DisplayDialog(
                    "Scan Complete",
                    $"Found and cached {found.Count} AchievementSO asset(s).\n\nThese achievements will now be available for runtime unlocking + persistence.",
                    "OK"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[Signalia Achievements] Failed to scan AchievementSO assets: {e.Message}");
                EditorUtility.DisplayDialog("Scan Failed", $"Failed to scan AchievementSO assets:\n\n{e.Message}", "OK");
            }
        }
        #endregion

    }
}
