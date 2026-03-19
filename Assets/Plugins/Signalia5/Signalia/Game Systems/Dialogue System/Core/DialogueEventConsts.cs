namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// Retains the names of the dialogue events used in the engine.
    /// </summary>
    public class DialogueEventConsts
    {
        /// <summary>
        /// This is called when the dialogue begins. Listen to it to add extra functionality.
        /// Use SIGS.Listener to listen to this event.
        /// Packs in the dialogue object that started as well.
        /// </summary>
        public const string DialogueBeginEvent = "SIGSDGSYS_BeginDialogue";
        
        /// <summary>
        /// This is called when the dialogue ends. Listen to it to add extra functionality.
        /// Use SIGS.Listener to listen to this event.
        /// Packs in the dialogue object that ended as well.
        /// </summary>
        public const string DialogueEndEvent = "SIGSDGSYS_EndDialogue";
        
        /// <summary>
        /// Fired when DialogueManager finishes initializing.
        /// </summary>
        public const string DialogueInitEvent = "SIGSDGSYS_Init";
        
        /// <summary>
        /// Is listened to by the engine to continue the dialogue. Can be fired from anywhere.
        /// </summary>
        public const string ContinueEventName = "SIGSDGSYS_Continue";
        
        /// <summary>
        /// Is listened to by the engine to choose a choice. Can be fired from anywhere. Differs from continue in that it continues through choice.
        /// </summary>
        public const string ChoiceChosenEvent = "SIGSDGSYS_ChoiceChosen";
        
        /// <summary>
        /// Sample dialogue event. Use this to fire a sample dialogue.
        /// </summary>
        public const string SampleDialogueEvent = "SIGSDGSYS_SampleDialogue";
    }
}