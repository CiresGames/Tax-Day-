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
        #region Game Systems - Inline Script

        private void DrawInlineScriptTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.InlineScriptHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Configure InlineScript settings. These namespaces will be appended to every generated inline script. Add one using directive per line.",
                MessageType.Info
            );

            GUILayout.Space(10);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            // Global Imports Section
            EditorGUILayout.LabelField("Global Imports", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These namespaces will be automatically available in all your inline scripts. Add one using directive per line.",
                MessageType.Info
            );

            EditorGUI.BeginChangeCheck();
            config.InlineScript.GlobalUsings = EditorGUILayout.TextArea(
                config.InlineScript.GlobalUsings,
                GUILayout.Height(120),
                GUILayout.MaxWidth(maxWidthProperties)
            );
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }

            GUILayout.Space(20);

            // Cache Settings Section
            EditorGUILayout.LabelField("Cache Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure where InlineScript stores its generated scripts. " +
                "These paths are used by the InlineScript compiler to locate generated scripts.",
                MessageType.Info
            );

            config.InlineScript.RootPath_Cache = EditorGUILayout.TextField(
                new GUIContent("Script Cache Path", "Root path for user-generated script cache"),
                config.InlineScript.RootPath_Cache,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);

            // Helper buttons
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, fixedHeight = 25 };
            
            if (GUILayout.Button("Open Cache Folder", buttonStyle))
            {
                string cachePath = config.InlineScript.RootPath_Cache.TrimEnd('/');
                if (System.IO.Directory.Exists(cachePath))
                {
                    EditorUtility.RevealInFinder(cachePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Directory Not Found",
                        $"Cache directory does not exist: {cachePath}",
                        "OK");
                }
            }
        }


        #endregion

    }
}
