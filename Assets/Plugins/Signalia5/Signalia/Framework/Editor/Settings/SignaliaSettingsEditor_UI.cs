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
        #region Tabs - UI Config

        private void DrawUIConfigTab()
        {
            EditorGUILayout.HelpBox("Configure how UI elements behave during animations and interactions.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.DefaultButtonsCooldown = EditorGUILayout.FloatField(new GUIContent("Button Cooldown (secs)", "Time before buttons can be clicked again."), config.DefaultButtonsCooldown);
            config.PreventButtonsClickingWhenViewAnimating = EditorGUILayout.Toggle(new GUIContent("Disable Clicking (View Animating)", "Prevents clicking while the view is animating."), config.PreventButtonsClickingWhenViewAnimating);
            config.DisableEventSystemWhenViewAnimating = EditorGUILayout.Toggle(new GUIContent("Disable Event System (View Animating)", "Disables event system while the view is animating."), config.DisableEventSystemWhenViewAnimating);
            config.PreventButtonsClickingWhenAnimatableAnimating = EditorGUILayout.Toggle(new GUIContent("Disable Clicking (Animatable)", "Prevents clicking when an animatable element is animating."), config.PreventButtonsClickingWhenAnimatableAnimating);
            config.DisableEventSystemWhenAnimatableAnimating = EditorGUILayout.Toggle(new GUIContent("Disable Event System (Animatable)", "Disables the event system when an animatable element is animating."), config.DisableEventSystemWhenAnimatableAnimating);
            config.KeepManagerAlive = EditorGUILayout.Toggle(new GUIContent("Keep Manager Alive", "Keeps the Signalia manager alive between scenes. Make sure to dispose of created statics yourself if this is enabled."), config.KeepManagerAlive);

            config.AutoAddBackButton = EditorGUILayout.Toggle(new GUIContent("Auto-Add Back Button", "Automatically adds a back button to UI views."), config.AutoAddBackButton);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Back Button Input Actions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "UIBackButton listens for Signalia input actions and calls SIGS.Clickback() when any configured action is pressed.\n\n" +
                "Add one or more action names (e.g., 'Back', 'Cancel'). These must exist in your SignaliaActionMap assets.",
                MessageType.Info);

            if (config.BackButtonActionNames == null)
            {
                config.BackButtonActionNames = new string[] { "Back", "Cancel" };
                EditorUtility.SetDirty(config);
            }

            int backActionCount = config.BackButtonActionNames.Length;
            EditorGUILayout.LabelField($"Configured Back Actions: {backActionCount}", EditorStyles.miniBoldLabel);

            for (int i = 0; i < config.BackButtonActionNames.Length; i++)
            {
                GUILayout.BeginHorizontal();
                config.BackButtonActionNames[i] = EditorGUILayout.TextField(new GUIContent($"Action {i + 1}", "Input action name to treat as a back/cancel trigger."), config.BackButtonActionNames[i]);

                if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    var newList = new List<string>(config.BackButtonActionNames);
                    if (i >= 0 && i < newList.Count)
                    {
                        newList.RemoveAt(i);
                        config.BackButtonActionNames = newList.ToArray();
                        EditorUtility.SetDirty(config);
                    }
                    GUILayout.EndHorizontal();
                    break;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Action", GUILayout.Height(22)))
            {
                var newList = new List<string>(config.BackButtonActionNames ?? Array.Empty<string>());
                newList.Add("");
                config.BackButtonActionNames = newList.ToArray();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Reset Defaults", GUILayout.Height(22)))
            {
                config.BackButtonActionNames = new string[] { "Back", "Cancel" };
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Clear", GUILayout.Height(22)))
            {
                config.BackButtonActionNames = Array.Empty<string>();
                EditorUtility.SetDirty(config);
            }
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0;
        }


        #endregion

    }
}
