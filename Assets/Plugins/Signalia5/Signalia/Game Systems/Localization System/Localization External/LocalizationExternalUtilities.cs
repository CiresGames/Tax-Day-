using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AHAKuo.Signalia.GameSystems.Localization.External
{
    /* 
     * The new localization extraction workflow uses the ILocbookExtraction interface.
     * 
     * To extract localizable strings from your game:
     * 1. Implement ILocbookExtraction on your MonoBehaviours or ScriptableObjects
     * 2. Use Tools > Signalia > Game Systems > Localization > Extract Locbook
     * 3. The tool will scan open scenes and Resources folders for implementations
     * 4. A LocBook asset and .locbook file will be generated
     * 
     * See LOCBOOK_EXTRACTION_GUIDE.md for detailed documentation and examples.
     */

#if UNITY_EDITOR
    /// <summary>
    /// Utilities for localization extraction and external workflows.
    /// Use the ILocbookExtraction interface to enable automatic string extraction.
    /// Access the extractor via: Tools > Signalia > Game Systems > Localization > Extract Locbook
    /// </summary>
    public static class LocalizationExternalUtilities
    {
        // The extraction system is implemented in:
        // - ILocbookExtraction.cs (interface and data structures)
        // - Editor/LocbookExtractorWindow.cs (extraction tool)
        // - Examples/ (reference implementations)
    }
    #endif

    /// <summary>
    /// Extension method to generate a unique identifier for a ScriptableObject asset.
    /// This can be used in both editor and runtime contexts.
    /// </summary>
    public static class LocalizationExtensions
    {
        public static string LocalizationPageId(this ScriptableObject asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset), "Asset cannot be null.");
            }

            return $"{asset.name} ({asset.GetInstanceID()})";
        }
    }
}
