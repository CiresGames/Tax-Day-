using System;
using System.Collections;
using System.Collections.Generic;
using AHAKuo.Signalia.UI;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// Contains information about the animation to play when reading dialogue. Uses AnimationAsset from the UI System.
    /// </summary>
    public class DialogueReadingAnimation : ScriptableObject
    {
        [SerializeField] private string readerAnimationName; // referenceable to allow the developer to change reader animations mid-dialogue.
        [SerializeField] private float readingSpeed;
        [SerializeField] private CharStop[] charStops;
        [SerializeField] private string tickerAudio;
        [SerializeField] private UIAnimationAsset animationPerCharacter; // note that this doesn't use end vectors, only start and duration. idk, we'll work on it.
        
        /// <summary>
        /// Defines a character stop in the animation. So when reading, it adds extra time to the animation for each character. Perfect for stuff like periods.
        /// </summary>
        [Serializable]
        public struct CharStop
        {
            public char character;
            public float stopTime;
        }
    }    
}

