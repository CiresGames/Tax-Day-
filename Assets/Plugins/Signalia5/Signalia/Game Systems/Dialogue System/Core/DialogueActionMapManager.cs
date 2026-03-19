using System.Collections.Generic;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities.SIGInput;
using DG.Tweening;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// Manages action map switching when dialogue begins and ends.
    /// Uses the InputStateModifier system for proper state management.
    /// Automatically disables gameplay action maps and enables GUI action maps during dialogue.
    /// </summary>
    internal static class DialogueActionMapManager
    {
        private const string DIALOGUE_ACTION_MODIFIER_ID = "DialogueSystem_ActionMaps";
        
        private static List<string> disabledMapNames = new List<string>();
        private static List<string> enabledMapNames = new List<string>();
        private static Tween reEnableDelayTween;

        /// <summary>
        /// Initializes the action map manager and sets up listeners for dialogue events.
        /// </summary>
        public static void Initialize()
        {
            var config = ConfigReader.GetConfig();
            if (config == null || !config.DialogueSystem.EnableActionMapSwitching)
            {
                return;
            }

            // Get map names from config, with defaults
            disabledMapNames.Clear();
            if (config.DialogueSystem.DisableActionMapNames != null && config.DialogueSystem.DisableActionMapNames.Length > 0)
            {
                disabledMapNames.AddRange(config.DialogueSystem.DisableActionMapNames.Where(name => !string.IsNullOrWhiteSpace(name)));
            }
            
            // If no maps specified, use default
            if (disabledMapNames.Count == 0)
            {
                disabledMapNames.Add("Default");
            }
            
            enabledMapNames.Clear();
            if (config.DialogueSystem.EnableActionMapNames != null && config.DialogueSystem.EnableActionMapNames.Length > 0)
            {
                enabledMapNames.AddRange(config.DialogueSystem.EnableActionMapNames.Where(name => !string.IsNullOrWhiteSpace(name)));
            }
            
            // If no maps specified, use default
            if (enabledMapNames.Count == 0)
            {
                enabledMapNames.Add("GUI");
            }

            // Listen to dialogue begin/end events
            SIGS.Listener(DialogueEventConsts.DialogueBeginEvent, OnDialogueBegin);
            SIGS.Listener(DialogueEventConsts.DialogueEndEvent, OnDialogueEnd);
        }

        /// <summary>
        /// Called when dialogue begins. Uses the modifier system to block gameplay maps
        /// and force-enable GUI maps.
        /// </summary>
        private static void OnDialogueBegin(object[] args)
        {
            // Cancel any pending re-enable delay if dialogue starts again
            if (reEnableDelayTween != null && reEnableDelayTween.IsActive())
            {
                reEnableDelayTween.Kill();
                reEnableDelayTween = null;
            }

            // Create and set the dialogue modifier
            // BlockedActionMaps = gameplay maps to disable
            // ForceEnabledActionMaps = GUI maps that should remain enabled
            var modifier = InputStateModifier.ForDialogue(
                DIALOGUE_ACTION_MODIFIER_ID,
                disabledMapNames.ToArray(),
                enabledMapNames.ToArray(),
                false, // Cursor is handled by InputMiceVisibilityController
                "DialogueSystem",
                10 // Higher priority for dialogue
            );

            SignaliaInputBridge.SetModifier(modifier);
        }

        /// <summary>
        /// Called when dialogue ends. Removes the dialogue modifier after the configured delay.
        /// </summary>
        private static void OnDialogueEnd(object[] args)
        {
            // Cancel any existing delay tween
            if (reEnableDelayTween != null && reEnableDelayTween.IsActive())
            {
                reEnableDelayTween.Kill();
            }

            // Get delay from config
            var config = ConfigReader.GetConfig();
            float delay = config != null ? config.DialogueSystem.ActionReEnableDelay : 0.1f;

            // Delay removing the modifier to allow for smooth transitions
            reEnableDelayTween = SIGS.DoIn(delay, () =>
            {
                SignaliaInputBridge.RemoveModifier(DIALOGUE_ACTION_MODIFIER_ID);
                reEnableDelayTween = null;
            });
        }
    }
}

