using System;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.TutorialSystem
{
    /// <summary>
    /// Methods that extend on Signalia UI and allow control of elements.
    /// </summary>
    [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
    public class TutorialMethods
    {
        private static (UIElement, UIButton) GetElementAndButtonByName(string elementName)
        {
            UIElement element = SIGS.GetElement(elementName);
            UIButton button = SIGS.GetButton(elementName);

            if (element == null && button == null)
            {
                Debug.LogError($"No UI Element or Button found with the name: {elementName}");
                return (null, null);
            }

            return (element, button);
        }

        /// <summary>
        /// Hightlights a UI element by name. Uses properties to determine how the element should be highlighted and where to go from there.
        /// </summary>
        /// <param name="elementName"></param>
        [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
        public static void HighlightElement(string elementName, ElementHighlightProperties properties)
        {
            (UIElement element, UIButton button) targets = GetElementAndButtonByName(elementName);

            if (properties.useButton
            && targets.button == null)
            {
                Debug.LogError($"No UIButton found with the name: {elementName} for highlighting.");
                return;
            }

            // show the blocker view first
            if (properties.blockerViewName.IsNullOrEmpty())
            {
                Debug.LogError("Blocker view name is null or empty. Cannot highlight element without a blocker view.");
                return;
            }

            var blockerView = SIGS.GetView(properties.blockerViewName);

            if (blockerView == null)
            {
                Debug.LogError($"No UI View found with the name: {properties.blockerViewName} for highlighting.");
                return;
            }

            blockerView.Show(); // this should immediately block input

            var highlighterView = properties.highlighterView.HasValue()
                ? SIGS.GetView(properties.highlighterView)
                : null;

            TutorialManager.RememberHighlightedElement(new HighlightedElement
            {
                properties = properties,
                blockerView = blockerView,
                highlighterView = highlighterView,
                button = targets.button,
                element = targets.element,
            });

            ElementHighlighting(properties, targets.element);
        }

        private static void ElementHighlighting(ElementHighlightProperties properties, UIElement element)
        {
            if (!element.RectTransform.gameObject.TryGetComponent<Canvas>(out var cnvs))
            {
                cnvs = element.RectTransform.gameObject.AddComponent<Canvas>();
            }

            cnvs.overrideSorting = true;
            cnvs.sortingLayerName = properties.sortingLayerName.HasValue() ? properties.sortingLayerName : cnvs.sortingLayerName; // Ensure the highlighter is on the UI layer
            cnvs.sortingOrder = 9999; // Ensure the highlighter is on top

            var highlighterView = TutorialManager.CurrentlyHighlightedElement.highlighterView;

            if (TutorialManager.CurrentlyHighlightedElement.highlighterView != null)
            {
                var rect = highlighterView.uiRect;

                // size the rect to match the element
                rect.sizeDelta = element.RectTransform.sizeDelta;
                rect.anchoredPosition = element.RectTransform.anchoredPosition;
                rect.localScale = element.RectTransform.localScale;
                rect.localRotation = element.RectTransform.localRotation;
                highlighterView.Show();
            }

            var timeBeforeAllowed = properties.timeBeforeProgressionAllowed.time;
            var unscaled = properties.timeBeforeProgressionAllowed.unscaled;

            SIGS.DoIn(timeBeforeAllowed, () =>
            {
                if (properties.anyInputToContinue)
                {
                    SIGS.OnAnyInputDown(TutorialManager.ReleaseHighlightedElement, true);
                    return;
                }

                if (properties.clickAnywhereToContinue)
                {
                    SIGS.OnClickAnywhere(TutorialManager.ReleaseHighlightedElement, true);
                    return;
                }
            }, unscaled);
        }
    }  
}
