using System.Collections;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.GameSystems.TutorialSystem;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    public class HighlightElementExample : MonoBehaviour
    {
        private void Awake()
        {
            SIGS.Listener("HighlightElement", () =>
            {
                // Example usage of HighlightElement
                var properties = new ElementHighlightProperties
                {
                    sortingLayerName = "UI",
                    blockerViewName = "TutorialBlocker",
                    anyInputToContinue = true,
                    clickAnywhereToContinue = false,
                    useButton = false,
                    highlighterView = "ElementHighlighter",
                    onClickAction = () => Debug.Log("Element clicked!"),
                    timeBeforeProgressionAllowed = (2f, true) // Allow progression after 2 seconds, using unscaled time
                };

                TutorialMethods.HighlightElement("Test Element", properties);
            });
        }
    }    
}