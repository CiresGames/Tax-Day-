using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.GameSystems.DialogueSystem.TestingSamples;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem 
{
    /// <summary>
    /// A simple manager class for the dialogue system. Simply handles showing and moving through dialogue by finding the correct view and parsing continue button events and enter or exiting dialogue mode.
    /// </summary>
    public static class DialogueManager
    {
        public static bool DialogueInited { get; private set; } = false;
        public static bool InDialogueNow { get; private set; } = false; // could be a combination of bool + CurrentDialogue not null?
        
        // track views
        private static Dictionary<string, DialogueRenderer> dialogueRenderers = new();

        /// <summary>
        /// Currently active dialogue.
        /// </summary>
        public static DialogueBook CurrentDialogue { get; private set; } = null;

        public static DialogueRenderer CurrentRenderer { get; private set; } = null;
        
        /// <summary>
        /// Initializes the dialogue system.
        /// </summary>
        public static void Initialize()
        {
            if (DialogueInited) return;
            
            dialogueRenderers.Clear();
            
            // sampler init
            DialogueTestingObjects.InitializeTestingSample();

            DialogueContinuer.InitializeContinueEvent();
            
            // Initialize action map manager for automatic action map switching
            DialogueActionMapManager.Initialize();
            
            DialogueInited = true;
            
            DialogueEventConsts.DialogueInitEvent.SendEvent();
        }

        /// <summary>
        /// Finds the proper dialogue view and starts the dialogue with it.
        /// </summary>
        /// <param name="dg"></param>
        public static void StartDialogue(DialogueBook dg, string style = "")
        {
            if (!DialogueInited)
                Initialize();

            if (InDialogueNow)
            {
                Debug.LogWarning("Attempted to start dialogue while already in dialogue. This is not allowed.");
                return;
            }
            
            InDialogueNow = true;
            
            CurrentDialogue = dg;
            
            style = style.IsNullOrEmpty() ? "default" : style;
            
            if (!dialogueRenderers.TryGetValue(style, out var viewToUse) || viewToUse == null)
            {
                Debug.LogError($"The dialogue style '{style}' is not registered. Canceling dialogue.");
                return;
            }
            
            CurrentRenderer = viewToUse;
            
            viewToUse.BeginDialogueFlow(CurrentDialogue);
            
            DialogueEventConsts.DialogueBeginEvent.SendEvent(CurrentDialogue.ToPlug());
        }

        /// <summary>
        /// Abrupt, should not be called unless dialogue must end now. Might break things.
        /// It is called automatically on dialogue renderer
        /// </summary>
        public static void EndDialogue()
        {
            DialogueEventConsts.DialogueEndEvent.SendEvent(CurrentDialogue.ToPlug());

            AfterDialogueCleanup();
        }

        private static void AfterDialogueCleanup()
        {
            InDialogueNow = false;
            CurrentDialogue = null;
            CurrentRenderer = null;
        }

        public static void Cleanup()
        {
            // no need to dispose parser event as radio will handle it.
            CurrentDialogue = null;
            DialogueInited = false;
            InDialogueNow = false;
            CurrentRenderer = null;
            dialogueRenderers?.Clear();
        }

        public static void RegisterView(DialogueRenderer dialogueView)
        {
            var key = dialogueView.DialogueStyleName.IsNullOrEmpty() ? "default" : dialogueView.DialogueStyleName;
            
            var exists = dialogueRenderers.Any(x => x.Key == key);
            if (exists) return;
            
            dialogueRenderers[key] = dialogueView;
        }

        public static void ContinueTo(DialogueBook exit)
        {
            if (!DialogueInited)
                return;

            if (!InDialogueNow)
            {
                Debug.LogWarning("Attempted to continue dialogue while not in dialogue mode. This is not allowed.");
                return;
            }
            
            CurrentDialogue = exit;
            
            CurrentRenderer.BeginDialogueFlow(CurrentDialogue);
        }
    }
}
