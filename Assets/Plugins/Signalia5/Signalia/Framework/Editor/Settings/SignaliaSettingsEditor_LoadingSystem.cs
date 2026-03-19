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
        #region Game Systems - Loading System

        private void DrawLoadingTab()
        {
            GUILayout.Space(10);
            // TODO: Add Loading Screen header graphic when available
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.LoadingScreenHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Work in progress.", MessageType.Info);
            config.LoadingScreen.LoadingScreenPrefab = (UIView)EditorGUILayout.ObjectField(new GUIContent("Loading Screen Prefab", "Prefab for the loading screen."), config.LoadingScreen.LoadingScreenPrefab, typeof(UIView), false);
            var menuComp = config.LoadingScreen.LoadingScreenPrefab != null ? config.LoadingScreen.LoadingScreenPrefab.GetComponent<UIView>() : null;

            if (menuComp == null)
            {
                EditorGUILayout.HelpBox("The selected prefab does not have a UIView component. Please add one.", MessageType.Error);
                return;
            }

            if (menuComp.MenuName.IsNullOrEmpty())
            {
                EditorGUILayout.HelpBox("The selected prefab does not have a MenuName. Please add one.", MessageType.Error);
                return;
            }

            config.LoadingScreen.EventOnLoadInitialComplete = EditorGUILayout.TextField(new GUIContent("Event On Load Initial Complete", "Event triggered when the loading screen is done loading first 90%."), config.LoadingScreen.EventOnLoadInitialComplete);
            config.LoadingScreen.EventOnLoadFullyComplete = EditorGUILayout.TextField(new GUIContent("Event On Load Fully Complete", "Event triggered when the loading screen is done loading 100%."), config.LoadingScreen.EventOnLoadFullyComplete);
            // simulate fake loading and fake loading time fields
            config.LoadingScreen.SimulateFakeLoading = EditorGUILayout.Toggle(new GUIContent("Fake Loading", "Simulates extra loading after the actual loading completes."), config.LoadingScreen.SimulateFakeLoading);
            if (config.LoadingScreen.SimulateFakeLoading)
            {
                config.LoadingScreen.FakeLoadingTime = EditorGUILayout.FloatField(new GUIContent("Fake Loading Time (secs)", "Time to simulate loading from end of true loading."), config.LoadingScreen.FakeLoadingTime);
            }

            config.LoadingScreen.ClickToProgress = EditorGUILayout.Toggle(new GUIContent("Click to Progress", "Allows clicking to progress the loading screen. This works by sending a progression event to which the loading screen will listen to."), config.LoadingScreen.ClickToProgress);

            if (config.LoadingScreen.ClickToProgress)
            {
                EditorGUILayout.HelpBox("Click to Progress is enabled. Simply send an event with the name below to progress the loading screen.", MessageType.Info);
                config.LoadingScreen.ProgressionEvent = EditorGUILayout.TextField(new GUIContent("Progression Event", "Event name to listen for progression."), config.LoadingScreen.ProgressionEvent);

                GUILayout.Space(8);
                EditorGUILayout.LabelField("Click To Continue Input Actions", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "When 'Click to Progress' is enabled, the loading screen can also be progressed using Signalia input actions.\n\n" +
                    "Add one or more action names (e.g., 'Confirm', 'Submit'). These must exist in your SignaliaActionMap assets.",
                    MessageType.Info);

                if (config.LoadingScreen.ClickToProgressActionNames == null)
                {
                    config.LoadingScreen.ClickToProgressActionNames = new[] { "Confirm", "Submit", "Interact" };
                    EditorUtility.SetDirty(config);
                }

                int actionCount = config.LoadingScreen.ClickToProgressActionNames.Length;
                EditorGUILayout.LabelField($"Configured Progress Actions: {actionCount}", EditorStyles.miniBoldLabel);

                for (int i = 0; i < config.LoadingScreen.ClickToProgressActionNames.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    config.LoadingScreen.ClickToProgressActionNames[i] = EditorGUILayout.TextField(
                        new GUIContent($"Action {i + 1}", "Input action name to treat as a loading screen progress trigger."),
                        config.LoadingScreen.ClickToProgressActionNames[i]);

                    if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        var newList = new List<string>(config.LoadingScreen.ClickToProgressActionNames);
                        if (i >= 0 && i < newList.Count)
                        {
                            newList.RemoveAt(i);
                            config.LoadingScreen.ClickToProgressActionNames = newList.ToArray();
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
                    var newList = new List<string>(config.LoadingScreen.ClickToProgressActionNames ?? Array.Empty<string>());
                    newList.Add("");
                    config.LoadingScreen.ClickToProgressActionNames = newList.ToArray();
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Reset Defaults", GUILayout.Height(22)))
                {
                    config.LoadingScreen.ClickToProgressActionNames = new[] { "Confirm", "Submit", "Interact" };
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Clear", GUILayout.Height(22)))
                {
                    config.LoadingScreen.ClickToProgressActionNames = Array.Empty<string>();
                    EditorUtility.SetDirty(config);
                }
                GUILayout.EndHorizontal();
            }

            if (!config.LoadingScreen.preloadLoadingScreen)
            {
                EditorGUILayout.HelpBox("Preload Loading Screen is disabled. This may cause a delay when showing the loading screen for the first time. Therefore, please call LoadingScreen.PrepareLoadingScreen() before showing it for the first time.", MessageType.Warning);
            }

            config.LoadingScreen.preloadLoadingScreen = EditorGUILayout.Toggle(new GUIContent("Preload Loading Screen", "Preloads the loading screen prefab to avoid delays when showing it."), config.LoadingScreen.preloadLoadingScreen);
        }

        private int localizationTabIndex = 0;
        private readonly string[] localizationTabs = new string[] { "Internal", "External" };


        #endregion

    }
}
