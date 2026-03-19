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
        #region Game Systems - Common Mechanics

        private void DrawCommonMechanicsTab()
        {
            GUILayout.Space(10);
            EditorUtilityMethods.RenderSignaliaHeader(GraphicLoader.CommonMechanicsHeader, maxWidthProperties);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Configure common mechanics settings for your project. Each mechanic can be configured independently.", MessageType.Info);
            
            GUILayout.Space(10);

            // Subtabs
            GUI.backgroundColor = Color.gray;
            commonMechanicsSubtab = GUILayout.Toolbar(commonMechanicsSubtab, commonMechanicsSubtabs, GUILayout.Height(24), GUILayout.MaxWidth(maxWidthProperties));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            switch (commonMechanicsSubtab)
            {
                case 0:
                    DrawCurrenciesSubtab();
                    break;
                case 1:
                    DrawInteractiveZoneSubtab();
                    break;
            }
        }

        private void DrawCurrenciesSubtab()
        {
            EditorGUILayout.HelpBox("Configure currency system settings for your project. Define limits and behaviors for different currencies.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            // save file setting
            config.CurrencySystem.SaveFileName = EditorGUILayout.TextField(
                               new GUIContent("Save File", "Name of the save file where currency data is stored."),
                                              config.CurrencySystem.SaveFileName,
                                                             GUILayout.MaxWidth(maxWidthProperties)
                                                                        );

            GUILayout.Space(10);

            // Currency Limits Section
            EditorGUILayout.LabelField("Currency Limits", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.HelpBox("Define minimum and maximum limits for each currency. Each currency can have infinite limits or custom values.", MessageType.Info);
            
            // Show current currency limits count
            int currencyLimitCount = config.CurrencySystem.CurrencyLimits != null ? config.CurrencySystem.CurrencyLimits.Length : 0;
            EditorGUILayout.LabelField($"Current Currency Limits: {currencyLimitCount}", EditorStyles.boldLabel);
            
            GUILayout.Space(5);
            
            // Add new currency limit button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 30 };
            if (GUILayout.Button("Add Currency Limit", buttonStyle))
            {
                AddCurrencyLimit();
            }
            
            GUILayout.Space(10);
            
            // Display existing currency limits
            if (config.CurrencySystem.CurrencyLimits != null && config.CurrencySystem.CurrencyLimits.Length > 0)
            {
                EditorGUILayout.LabelField("Currency Limit Definitions:", EditorStyles.boldLabel);
                
                for (int i = 0; i < config.CurrencySystem.CurrencyLimits.Length; i++)
                {
                    var limit = config.CurrencySystem.CurrencyLimits[i];
                    
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Currency: {limit.CurrencyName}", EditorStyles.boldLabel, GUILayout.Width(200));
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(80), GUILayout.Height(20)))
                    {
                        RemoveCurrencyLimit(i);
                        break; // Break to avoid index issues after removal
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Space(5);
                    
                    // Currency name
                    limit.CurrencyName = EditorGUILayout.TextField("Currency Name", limit.CurrencyName, GUILayout.MaxWidth(maxWidthProperties));
                    
                    GUILayout.Space(5);
                    
                    // Minimum limit settings
                    EditorGUILayout.LabelField("Minimum Limit:", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    limit.MinLimitType = (CurrencyLimitType)EditorGUILayout.EnumPopup("Type", limit.MinLimitType, GUILayout.MaxWidth(maxWidthProperties));
                    
                    if (limit.MinLimitType == CurrencyLimitType.Custom)
                    {
                        limit.CustomMinValue = EditorGUILayout.FloatField("Custom Min Value", limit.CustomMinValue, GUILayout.MaxWidth(maxWidthProperties));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Value: Infinite (no minimum)", EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                    
                    GUILayout.Space(5);
                    
                    // Maximum limit settings
                    EditorGUILayout.LabelField("Maximum Limit:", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    limit.MaxLimitType = (CurrencyLimitType)EditorGUILayout.EnumPopup("Type", limit.MaxLimitType, GUILayout.MaxWidth(maxWidthProperties));
                    
                    if (limit.MaxLimitType == CurrencyLimitType.Custom)
                    {
                        limit.CustomMaxValue = EditorGUILayout.FloatField("Custom Max Value", limit.CustomMaxValue, GUILayout.MaxWidth(maxWidthProperties));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Value: Infinite (no maximum)", EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                    
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
                
                GUILayout.Space(5);
                
                // Clear all button
                if (GUILayout.Button("Clear All Currency Limits", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Currency Limits", 
                        "Are you sure you want to remove all currency limits?", 
                        "Yes", "Cancel"))
                    {
                        config.CurrencySystem.CurrencyLimits = new CurrencyLimit[0];
                        EditorUtility.SetDirty(config);
                        Debug.Log("[Signalia] Cleared all currency limits from config.");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No currency limits defined. Use 'Add Currency Limit' button above to create limits for your currencies.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUIUtility.labelWidth = 0;
        }

        private void AddCurrencyLimit()
        {
            if (config.CurrencySystem.CurrencyLimits == null)
            {
                config.CurrencySystem.CurrencyLimits = new CurrencyLimit[0];
            }
            
            var newLimits = new CurrencyLimit[config.CurrencySystem.CurrencyLimits.Length + 1];
            for (int i = 0; i < config.CurrencySystem.CurrencyLimits.Length; i++)
            {
                newLimits[i] = config.CurrencySystem.CurrencyLimits[i];
            }
            
            newLimits[config.CurrencySystem.CurrencyLimits.Length] = new CurrencyLimit
            {
                CurrencyName = "gold",
                MinLimitType = CurrencyLimitType.Infinite,
                MaxLimitType = CurrencyLimitType.Infinite,
                CustomMinValue = 0f,
                CustomMaxValue = 1000f
            };
            
            config.CurrencySystem.CurrencyLimits = newLimits;
            EditorUtility.SetDirty(config);
            Debug.Log("[Signalia] Added new currency limit for 'gold'.");
        }

        private void RemoveCurrencyLimit(int index)
        {
            if (config.CurrencySystem.CurrencyLimits == null || index < 0 || index >= config.CurrencySystem.CurrencyLimits.Length)
                return;
            
            var limit = config.CurrencySystem.CurrencyLimits[index];
            if (EditorUtility.DisplayDialog("Remove Currency Limit", 
                $"Are you sure you want to remove the currency limit for '{limit.CurrencyName}'?", 
                "Yes", "Cancel"))
            {
                var newLimits = new CurrencyLimit[config.CurrencySystem.CurrencyLimits.Length - 1];
                for (int i = 0, j = 0; i < config.CurrencySystem.CurrencyLimits.Length; i++)
                {
                    if (i != index)
                    {
                        newLimits[j++] = config.CurrencySystem.CurrencyLimits[i];
                    }
                }
                config.CurrencySystem.CurrencyLimits = newLimits;
                EditorUtility.SetDirty(config);
                Debug.Log($"[Signalia] Removed currency limit for '{limit.CurrencyName}'.");
            }
        }

        private void DrawInteractiveZoneSubtab()
        {
            EditorGUILayout.HelpBox("Configure how interactive zones display prompts and listen for input events.", MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            var settings = config.CommonMechanics.InteractiveZone;

            settings.InvokeType = (InteractiveZoneInvokeType)EditorGUILayout.EnumPopup(
                new GUIContent("Invoke Type", "Choose whether zones listen to a Signalia input action or a Signalia radio event."),
                settings.InvokeType,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(5);

            if (settings.InvokeType == InteractiveZoneInvokeType.SignaliaInputAction)
            {
                settings.InputActionName = EditorGUILayout.TextField(
                    new GUIContent("Input Action", "Signalia input action name (must exist in your Signalia Action Maps)."),
                    settings.InputActionName,
                    GUILayout.MaxWidth(maxWidthProperties)
                );

                settings.ActionTrigger = (InteractiveZoneInputTriggerMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Trigger Mode", "How the action is evaluated (Down/Held/Up)."),
                    settings.ActionTrigger,
                    GUILayout.MaxWidth(maxWidthProperties)
                );

                settings.OneFrameConsume = EditorGUILayout.Toggle(
                    new GUIContent("One Frame Consume", "For Down/Up: consumes the edge so only one listener can react per frame."),
                    settings.OneFrameConsume,
                    GUILayout.MaxWidth(maxWidthProperties)
                );

                settings.RequireActionEnabled = EditorGUILayout.Toggle(
                    new GUIContent("Require Action Enabled", "Only triggers when the action is enabled and readable by an input wrapper."),
                    settings.RequireActionEnabled,
                    GUILayout.MaxWidth(maxWidthProperties)
                );
            }
            else
            {
                settings.InputEventName = EditorGUILayout.TextField(
                    new GUIContent("Input Event", "Name of the Signalia event that triggers interactions when fired."),
                    settings.InputEventName,
                    GUILayout.MaxWidth(maxWidthProperties)
                );
            }

            GUILayout.Space(5);

            settings.UseLegacyInputFallback = EditorGUILayout.Toggle(
                new GUIContent("Legacy Input Fallback", "Fallback to Unity's legacy input key when waiting for interactions."),
                settings.UseLegacyInputFallback,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUI.BeginDisabledGroup(!settings.UseLegacyInputFallback);
            settings.LegacyFallbackKey = (KeyCode)EditorGUILayout.EnumPopup(
                new GUIContent("Legacy Key", "KeyCode checked each frame when the legacy fallback is active."),
                settings.LegacyFallbackKey,
                GUILayout.MaxWidth(maxWidthProperties)
            );
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            settings.BindingDisplayName = EditorGUILayout.TextField(
                new GUIContent("Binding Display Name", "Label used in prompts when no dynamic binding information is supplied."),
                settings.BindingDisplayName,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            settings.SaveFileName = EditorGUILayout.TextField(
                new GUIContent("Save File Name", "Save file used when interactive zones persist their completion state."),
                settings.SaveFileName,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            GUILayout.Space(10);

            settings.DisableDuringDialogue = EditorGUILayout.Toggle(
                new GUIContent("Disable During Dialogue", "If enabled, interactive zones will not allow interaction while DialogueManager.InDialogueNow is true."),
                settings.DisableDuringDialogue,
                GUILayout.MaxWidth(maxWidthProperties)
            );

            EditorGUILayout.HelpBox("Interactive zones can optionally persist their completion state using the configured save file. For SignaliaInputAction, ensure you have a SignaliaInputWrapper in the scene that forwards input states via SIGS.Pass...(). For SignaliaRadioEvent, send the configured event key when interaction controls are pressed.", MessageType.None);
        }


        #endregion

    }
}
