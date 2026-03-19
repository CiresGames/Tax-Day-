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
        #region Game Systems - Audio Layering

        private void DrawAudioLayeringTab()
        {
            GUILayout.Space(10);
            // TODO: Add Audio Layering header graphic when available
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.AudioLayeringHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Configure audio layering settings for your project. Audio layering allows you to create complex audio compositions with multiple layers that can be played simultaneously.", MessageType.Info);
            
            if (config.AudioLayering.LayerData == null)
            {
                EditorGUILayout.HelpBox("⚠️ No Audio Layering Layer Data assigned. Please create and assign an AudioLayeringLayerData asset to configure your audio layers.", MessageType.Warning);
            }
            
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.AudioLayering.LayerData = (AudioLayeringLayerData)EditorGUILayout.ObjectField(
                new GUIContent("Audio Layering Layer Data", "Asset containing the layer definitions for audio layering system."),
                config.AudioLayering.LayerData,
                typeof(AudioLayeringLayerData),
                false,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Fade Duration is the duration of the fade transition between audio clips in audio layers.", MessageType.Info);
            
            config.AudioLayering.FadeDuration = EditorGUILayout.FloatField(
                new GUIContent("Fade Duration (seconds)", "Duration of fade transitions between audio clips in audio layers."),
                config.AudioLayering.FadeDuration,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Continuous Playing allows audio layers to keep playing without stopping when new clips are played. Instead of stopping and restarting, it will smoothly fade between clips.", MessageType.Info);
            
            config.AudioLayering.ContinuousPlaying = EditorGUILayout.Toggle(
                new GUIContent("Continuous Playing", "Enables smooth transitions between audio clips without stopping playback."),
                config.AudioLayering.ContinuousPlaying,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUIUtility.labelWidth = 0;
        }


        #endregion

    }
}
