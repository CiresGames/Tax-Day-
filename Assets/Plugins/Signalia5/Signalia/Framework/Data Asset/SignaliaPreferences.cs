using UnityEngine;
using System;

namespace AHAKuo.Signalia.Framework
{
    /// <summary>
    /// Editor-only preferences for Signalia. Stored as JSON in the Signalia folder.
    /// These settings are local to the editor and not included in builds.
    /// </summary>
    [Serializable]
    public class SignaliaPreferences
    {
        [Header("Notes Component - Hierarchy Window Colors")]
        [Tooltip("Background color tint for GameObjects with Notes components in the hierarchy window. Use semi-transparent colors (alpha < 1.0) for best results.")]
        public Color NotesHierarchyBackgroundColor = new Color(1f, 0.95f, 0.7f, 0.25f);
        
        [Tooltip("Foreground (text) color for GameObjects with Notes components in the hierarchy window. Set alpha to 0 to use Unity's default text color.")]
        public Color NotesHierarchyTextColor = new Color(0f, 0f, 0f, 0f); // Transparent by default (uses Unity's default)

        /// <summary>
        /// Creates a new instance with default values.
        /// </summary>
        public static SignaliaPreferences CreateDefault()
        {
            return new SignaliaPreferences();
        }
    }
}

