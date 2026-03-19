using System;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities.SIGInput;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// Represents a UI button used to continue dialogue sequences within the
    /// dialogue system. This button is responsible for signaling the progression
    /// to the next part of the dialogue when interacted with by the user.
    /// </summary>
    /// <remarks>
    /// This class requires a UIButton component to properly function.
    /// Ensure the UIButton is configured with the desired animations and
    /// interactivity settings as required by your specific use case.
    /// </remarks>
    [RequireComponent(typeof(UIButton))]
    public class DialogueContinueButton : MonoBehaviour
    {
        private UIButton button;

        private static readonly string[] FallbackContinueActions = new[] { "Confirm", "Submit", "Interact" };
        
        private void Awake()
        {
            button = GetComponent<UIButton>();
        }

        private void Start()
        {
            button.AddNewAction(ContinueDialogue);
        }

        private void ContinueDialogue()
        {
            DialogueEventConsts.ContinueEventName.SendEvent();
        }

        private void Update()
        {
            // Only allow action-based continue when the dialogue system is active and the button itself is active.
            if (!DialogueManager.InDialogueNow) { return; }
            if (!isActiveAndEnabled) { return; }

            // Avoid spamming warnings when no wrapper exists.
            if (!SignaliaInputWrapper.Exists) { return; }

            var config = ConfigReader.GetConfig();
            var actions = (config != null
                           && config.DialogueSystem != null
                           && config.DialogueSystem.ContinueActionNames != null
                           && config.DialogueSystem.ContinueActionNames.Length > 0)
                ? config.DialogueSystem.ContinueActionNames
                : FallbackContinueActions;

            for (int i = 0; i < actions.Length; i++)
            {
                string actionName = actions[i];
                if (string.IsNullOrWhiteSpace(actionName)) { continue; }

                if (SIGS.GetInputDown(actionName, oneFrame: true))
                {
                    ContinueDialogue();
                    return;
                }
            }
        }
    }
}