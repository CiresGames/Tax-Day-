using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.TestingSamples
{
    /// <summary>
    /// A utility class for initializing and managing dialogue testing samples
    /// within the dialogue system. It provides functionality to handle and
    /// test dialogue-related events for development and testing purposes.
    /// </summary>
    internal class DialogueTestingObjects
    {
        internal static void InitializeTestingSample()
        {
            if (DialogueManager.DialogueInited) return; // follow parent init
            
            SIGS.Listener(DialogueEventConsts.SampleDialogueEvent, (s) =>
            {
                // check number
                var key = (string)s[0];

                PlayMockDialogue(key); //todo: right now this will play the same dialogue. No real use behind the number, but can be used to play different dialogues for testing.
            });
        }

        private static void PlayMockDialogue(string key)
        {
            var dialogSample = DialogueBook.MakeSample(true);
            dialogSample.StartDialogue(); //using default style
        }
    }
}