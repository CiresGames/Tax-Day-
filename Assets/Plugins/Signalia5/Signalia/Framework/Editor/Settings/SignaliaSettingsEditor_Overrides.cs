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
        #region Tabs - Overrides

        private void DrawOverridesTab()
        {
            EditorGUILayout.HelpBox("These settings override default UI behaviors.", MessageType.Warning);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.ConvertAllButtonsToUIButtons = EditorGUILayout.Toggle(new GUIContent("Convert All to UI Buttons", "Forces all buttons to be treated as UI Buttons."), config.ConvertAllButtonsToUIButtons);
            config.OverrideUiViewAnimateOnce = EditorGUILayout.Toggle(new GUIContent("Override View Animation (One-Time)", "Forces UI views to animate only once."), config.OverrideUiViewAnimateOnce);

            if (config.OverrideUiViewAnimateOnce)
            {
                config.UIViewsAnimateOnce = EditorGUILayout.Toggle(new GUIContent("UI Views Animate Once", "Ensures UI views animate only once if override is enabled."), config.UIViewsAnimateOnce);
            }

            // info box for button blockers
            EditorGUILayout.HelpBox("Disables button blockers for TMPro text and other potential blockers in UIButton children. This is useful if you want to allow interaction with text or other elements that would normally block button clicks.", MessageType.Info);
            config.DisableButtonBlockers_TMPText = EditorGUILayout.Toggle(new GUIContent("Disable Button Blockers (TMP_Text)", "Disables the would-be button blockers in each UIButton children."), config.DisableButtonBlockers_TMPText);

            EditorGUIUtility.labelWidth = 0;
        }


        #endregion

    }
}
