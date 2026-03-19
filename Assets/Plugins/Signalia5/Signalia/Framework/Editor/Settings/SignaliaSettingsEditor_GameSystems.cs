using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Radio;
using System.Collections.Generic;
using System.IO;
using System;
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
        #region Tabs - Game Systems

        private void DrawGameSystemsTabs()
        {
            DrawMultiRowToolbar();
            GUILayout.Space(10);

            switch (selectedGameSystemTab)
            {
                case 0:
                    DrawDialogueTab();
                    break;
                case 1:
                    DrawSaveSystemTab();
                    break;
                case 2:
                    DrawInventoryTab();
                    break;
                case 3:
                    DrawPoolingTab();
                    break;
                case 4:
                    DrawLoadingTab();
                    break;
                case 5:
                    DrawLocalizationTab();
                    break;
                case 6:
                    DrawAudioLayeringTab();
                    break;
                case 7:
                    DrawTutorialTab();
                    break;
                case 8:
                    DrawResourceCachingTab();
                    break;
                case 9:
                    DrawCommonMechanicsTab();
                    break;
                case 10:
                    DrawInlineScriptTab();
                    break;
                case 11:
                    DrawAchievementSystemTab();
                    break;
            }
        }

        private void DrawMultiRowToolbar()
        {
            const int maxColumnsPerRow = 5;
            int totalTabs = gameSystemTabs.Length;
            int rows = Mathf.CeilToInt((float)totalTabs / maxColumnsPerRow);

            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                
                int startIndex = row * maxColumnsPerRow;
                int endIndex = Mathf.Min(startIndex + maxColumnsPerRow, totalTabs);
                int tabsInThisRow = endIndex - startIndex;

                // Calculate button width to fill the available space
                float buttonWidth = (maxWidthProperties - (tabsInThisRow - 1) * 2) / tabsInThisRow; // 2 is spacing between buttons

                for (int i = startIndex; i < endIndex; i++)
                {
                    bool isSelected = selectedGameSystemTab == i;
                    bool wasSelected = isSelected;
                    
                    // Create button style
                    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.fixedHeight = 25;
                    buttonStyle.fixedWidth = buttonWidth;
                    
                    // Change appearance for selected button
                    if (isSelected)
                    {
                        buttonStyle.normal.background = buttonStyle.active.background;
                        buttonStyle.normal.textColor = Color.white;
                    }

                    isSelected = GUILayout.Toggle(isSelected, gameSystemTabs[i], buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(25));
                    
                    if (isSelected && !wasSelected)
                    {
                        selectedGameSystemTab = i;
                    }
                }

                GUILayout.EndHorizontal();
                
                // Add small spacing between rows
                if (row < rows - 1)
                {
                    GUILayout.Space(2);
                }
            }
        }

        #endregion

    }
}
