using System;
using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem.Utilities
{
    /// <summary>
    /// Starts dialogue. Simple as that. Works like DialogueManager.StartDialogue.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Dialogue/Dialogue Starter")]
    [Icon("Assets/AHAKuo Creations/Signalia/Framework/Graphics/Icons/SIGS_EDITOR_ICON_DIALOGUE_STARTER.png")]
    public sealed class DialogueStarter : MonoBehaviour
    {
        [Tooltip("The dialogue style to use when starting dialogue.")]
        [SerializeField] private string styleName = "default";
        [SerializeField] private DialogueBook dg;
        
        /// <summary>
        /// Start the dialogue defined in the inspector.
        /// </summary>
        public void StartDialogue() => DialogueManager.StartDialogue(dg, styleName);
        
        /// <summary>
        /// Start the dialogue passed as parameter using style defined in the inspector.
        /// </summary>
        /// <param name="ob"></param>
        public void StartDialogueCustom(DialogueBook ob) => DialogueManager.StartDialogue(ob, styleName);
    }
}