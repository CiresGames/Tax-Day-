using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;

using AHAKuo.Signalia.GameSystems.DialogueSystem;

namespace AHAKuo.Signalia.Utilities.SIGInput
{
    /// <summary>
    /// Controls cursor visibility for special cases like the Dialogue System.
    /// Uses the InputStateModifier system for proper state management.
    /// UIView cursor visibility is now handled directly by UIView via modifiers.
    /// Automatically added by Watchman when enabled in the config.
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Signalia | Input Mice Visibility Controller")]
    public class InputMiceVisibilityController : InstancerSingleton<InputMiceVisibilityController>
    {
        private const string DIALOGUE_MODIFIER_ID = "DialogueSystem_Cursor";
        private bool dialogueWasActive = false;

        /// <summary>
        /// Called by Watchman to add InputMiceVisibilityController to the scene.
        /// </summary>
        public static void AddMe()
        {
            if (Instance != null) return;

            var go = new GameObject("Signalia Input Mice Visibility Controller");
            go.AddComponent<InputMiceVisibilityController>();
        }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            // No more event subscriptions needed - UIView handles its own modifiers
            // This controller now only handles special cases like DialogueSystem
        }

        private void Update()
        {
            var config = ConfigReader.GetConfig();
            if (config == null) return;

            // Handle Dialogue System rule using modifiers
            if (config.InputSystem.CursorVisibility.EnableDialogueSystemRule)
            {
                bool dialogueIsActive = DialogueManager.InDialogueNow;

                if (dialogueIsActive != dialogueWasActive)
                {
                    if (dialogueIsActive)
                    {
                        ApplyDialogueModifier(config.InputSystem.CursorVisibility.OnDialogueStart);
                    }
                    else
                    {
                        // Remove dialogue modifier when dialogue ends
                        SignaliaInputBridge.RemoveModifier(DIALOGUE_MODIFIER_ID);
                    }
                    dialogueWasActive = dialogueIsActive;
                }
            }
        }

        /// <summary>
        /// Applies a cursor modifier for the dialogue system.
        /// </summary>
        private void ApplyDialogueModifier(CursorVisibilityAction action)
        {
            bool showCursor = action == CursorVisibilityAction.Show;
            CursorLockMode lockState = action == CursorVisibilityAction.HideAndLock 
                ? CursorLockMode.Locked 
                : CursorLockMode.None;

            var modifier = new InputStateModifier(
                DIALOGUE_MODIFIER_ID,
                "DialogueSystem",
                null, // No action map blocking
                null, // No action blocking
                showCursor,
                lockState,
                10 // Higher priority for dialogue system
            );

            SignaliaInputBridge.SetModifier(modifier);
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                // Remove dialogue modifier if it exists
                SignaliaInputBridge.RemoveModifier(DIALOGUE_MODIFIER_ID);
            }
            base.OnDestroy();
        }
    }
}
