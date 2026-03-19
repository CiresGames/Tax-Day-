using AHAKuo.Signalia.Radio;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.TestingSamples
{
    /// <summary>
    /// The DialogueSampler class is part of the dialogue system testing samples within the Signalia game system.
    /// </summary>
    /// <remarks>
    /// This class is designed to facilitate the creation, manipulation, or testing of dialogue components within the game.
    /// It acts as a utility or helper for testing dialogue-related features in a controlled environment.
    /// </remarks>
    public class DialogueSampler : MonoBehaviour
    {
        public string dialogueSampleName = "neutral_encounter";
        
        public void TestSample()
        {
            DialogueEventConsts.SampleDialogueEvent.SendEvent(dialogueSampleName);
        }
    }
}