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
        #region Game Systems - Pooling System

        private void DrawPoolingTab()
        {
            GUILayout.Space(10);
            // TODO: Add Pooling header graphic when available
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.PoolingHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Smart Kill Waiter:\n\n" +
                "If enabled, pooled objects will cancel their lifetime timers if they are manually deactivated before the timer ends.\n" +
                "This helps avoid unintended behavior like objects reactivating themselves.\n\n" +
                "⚠ Slight performance cost: This feature tracks each pooled object's state and cancels timers early when needed.",
                MessageType.Info
            );

            config.PoolingSystem.SmartPoolLifetimeKill = EditorGUILayout.Toggle(
                new GUIContent("Enable Smart Kill Waiter", "Cancels the lifetime timer if the object is manually disabled before timeout."),
                config.PoolingSystem.SmartPoolLifetimeKill
            );

            GUILayout.Space(15);

            EditorGUILayout.HelpBox(
                "Pool Ceiling Limit:\n\n" +
                "Sets a maximum number of objects allowed in each pool. When the ceiling is reached, the system will:\n" +
                "• Recycle the oldest active object (if recycling is enabled)\n" +
                "• Refuse to create new objects and log a warning (if recycling is disabled)\n\n" +
                "Set to 0 for unlimited objects (default behavior).",
                MessageType.Info
            );

            config.PoolingSystem.CeilingLimit = EditorGUILayout.IntField(
                new GUIContent("Ceiling Limit", "Maximum number of objects allowed in each pool. Set to 0 for unlimited."),
                config.PoolingSystem.CeilingLimit,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Object Recycling:\n\n" +
                "When enabled and ceiling limit is reached, the system will recycle the oldest active object instead of refusing to create new ones.\n" +
                "This allows continued object creation while maintaining memory usage within the specified ceiling limit.\n\n" +
                "⚠ Only effective when Ceiling Limit > 0",
                MessageType.Info
            );

            EditorGUI.BeginDisabledGroup(config.PoolingSystem.CeilingLimit <= 0);
            config.PoolingSystem.EnableRecycling = EditorGUILayout.Toggle(
                new GUIContent("Enable Recycling", "When ceiling is reached, recycle oldest objects instead of refusing to create new ones."),
                config.PoolingSystem.EnableRecycling,
                GUILayout.MaxWidth(maxWidthProperties)
            );
            EditorGUI.EndDisabledGroup();

            if (config.PoolingSystem.CeilingLimit <= 0)
            {
                EditorGUILayout.HelpBox("Recycling is only available when Ceiling Limit is greater than 0.", MessageType.Warning);
            }
        }


        #endregion

    }
}
