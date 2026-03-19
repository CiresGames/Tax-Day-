using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    public static class DialogueContinuer
    {
        private static DialogueRenderer CurrentRenderer => DialogueManager.CurrentRenderer;
        
        public static void InitializeContinueEvent()
        {
            SIGS.Listener(DialogueEventConsts.ContinueEventName, ContinueDialogue);
            SIGS.Listener(DialogueEventConsts.ChoiceChosenEvent, ContinueWithChoice);
        }
        
        /// <summary>
        /// Increase index, and then read again.
        /// </summary>
        private static void ContinueDialogue()
        {
            if (SIGS.CooldownGate("dg_continueBtn", ConfigReader.GetConfig().DialogueSystem.continueButtonDelay))
                CurrentRenderer.Continue();
        }
        
        /// <summary>
        /// Increase index, and move forward. Choice context driven.
        /// </summary>
        private static void ContinueWithChoice(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] is not Choice choice)
            {
                Debug.LogError("ContinueWithChoice called without a valid Choice object.");
                return;
            }

            // for now, just move forward like continue, but see if there is something important the choices continuation need that separates them from normal continue event
            if (SIGS.CooldownGate("dg_continueBtn", ConfigReader.GetConfig().DialogueSystem.continueButtonDelay))
                CurrentRenderer.Continue(choice);
        }
    }
}
