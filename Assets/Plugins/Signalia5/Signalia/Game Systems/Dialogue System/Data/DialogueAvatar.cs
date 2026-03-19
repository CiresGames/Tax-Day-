using System;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// Contains information about the speaker. Referenceable through dialogue or where-ever else.
    /// </summary>
    public class DialogueAvatar : ScriptableObject
    {
        [SerializeField] private string characterName;
        [SerializeField] private string characterDescription;
        [SerializeField] private SpeakerColorPalette speakerColorPalette;
        [SerializeField] private SpeakerExpressions[] speakerExpressions;
    }
}


