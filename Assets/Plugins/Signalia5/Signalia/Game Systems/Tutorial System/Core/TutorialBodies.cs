using System;
using System.Collections;
using System.Collections.Generic;
using AHAKuo.Signalia.UI;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.TutorialSystem
{
    [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
    public struct HighlightedElement
    {
        public ElementHighlightProperties properties;
        public UIView blockerView;
        public UIView highlighterView;

        // one or the other of these will be set, depending on whether the element is a button or a generic UI element
        public UIButton button;
        public UIElement element;

        public readonly RectTransform ElementTarget()
        {
            if (element != null)
            {
                return element.RectTransform;
            }
            else if (button != null)
            {
                return button.RectTransform;
            }
            else
            {
                Debug.LogError("No UI Element or UIButton found in HighlightedElement.");
                return null;
            }
        }
    }
    /// <summary>
    /// Used to control how elements are highlighted in the tutorial system.
    /// </summary>
    [Obsolete("Tutorial System is deprecated and will be removed in a future release.")]
    public struct ElementHighlightProperties
    {
        /// <summary>
        /// The layer on which the highlighted element will be rendered, with its other components.
        /// </summary>
        public string sortingLayerName;

        public (float time, bool unscaled) timeBeforeProgressionAllowed;

        /// <summary>
        /// The name of the backing view that will be used to A. Block input behind the highlighted element, and B. emphasize the element.
        /// Must be a valid view name in the UI system, think "BlockerView" or "TutorialBlockerView".
        /// Design that view however you like, but it should be a full-screen view that blocks input and has a semi-transparent background (or not, depending on your design).
        /// A completely transparent background can be useful if you want to highlight an element without it being obscured by a background.
        /// </summary>
        public string blockerViewName;

        /// <summary>
        /// Any non-touch input will progress the tutorial.
        /// </summary>
        public bool anyInputToContinue;

        /// <summary>
        /// Clicking any UIButton on the screen will progress the tutorial. If false, only clicking the highlighted element will progress the tutorial.
        /// </summary>
        public bool clickAnywhereToContinue;

        /// <summary>
        /// Use the button component as the progression trigger, allowing the button to be clicked to progress the tutorial. If false, the button won't do anything.
        /// </summary>
        public bool useButton;

        /// <summary>
        /// The name of a highlighter that will be positioned over the element being highlighted.
        /// Can be something like a border, a glow, or any other visual effect that indicates the element is being highlighted.
        /// It will be sized and positioned to match the element being highlighted. This might need some tinkering to get the right effect.
        /// </summary>
        public string highlighterView;

        /// <summary>
        /// An extra action to perform when the element is clicked. This will be extra over the default action of progressing the tutorial.
        /// </summary>
        public Action onClickAction;
    }
}
