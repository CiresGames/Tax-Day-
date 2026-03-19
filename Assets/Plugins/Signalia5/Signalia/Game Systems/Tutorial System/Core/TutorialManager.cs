using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.TutorialSystem
{
    /// <summary>
    /// Handles the context and flow of the tutorial system.
    /// This class is responsible for managing the tutorial state, progression, and interactions with the UI elements.
    /// </summary>
    [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
    public static class TutorialManager
    {
        private static HighlightedElement currentlyHighlightedElement;
        public static HighlightedElement CurrentlyHighlightedElement => currentlyHighlightedElement;

        public static void RememberHighlightedElement(HighlightedElement element)
        {
            currentlyHighlightedElement = element;
        }

        public static void ReleaseHighlightedElement()
        {
            if (currentlyHighlightedElement.blockerView != null)
            {
                currentlyHighlightedElement.blockerView.Hide();
            }

            if (currentlyHighlightedElement.highlighterView != null)
            {
                currentlyHighlightedElement.highlighterView.Hide();
            }

            currentlyHighlightedElement.properties.onClickAction?.Invoke(); // Clear the action to prevent unintended behavior

            currentlyHighlightedElement = default;
        }

        public static void ResetAll()
        {
            currentlyHighlightedElement = default;
        }
    }
}
