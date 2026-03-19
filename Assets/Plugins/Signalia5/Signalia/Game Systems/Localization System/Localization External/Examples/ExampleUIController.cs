using UnityEngine;
using System.Collections.Generic;
using AHAKuo.Signalia.GameSystems.Localization.External;

namespace AHAKuo.Signalia.GameSystems.Localization.Examples
{
    /// <summary>
    /// Example implementation of ILocbookExtraction for a MonoBehaviour.
    /// This shows how to extract localizable text from UI elements or game objects.
    /// Add this component to a GameObject in your scene to enable extraction.
    /// </summary>
    public class ExampleUIController : MonoBehaviour, ILocbookExtraction
    {
        [Header("UI Text Content")]
        [Tooltip("Title text that should be localized")]
        public string titleText = "Welcome";
        
        [Tooltip("Description text that should be localized")]
        [TextArea(3, 5)]
        public string descriptionText = "This is an example of extractable text.";
        
        [Tooltip("Button labels that should be localized")]
        public List<string> buttonLabels = new List<string> { "Start", "Options", "Quit" };
        
        /// <summary>
        /// Implementation of ILocbookExtraction.
        /// This method tells the extractor how to retrieve localizable text from this component.
        /// </summary>
        public ExtractionData GetExtractionData()
        {
            var data = new ExtractionData();
            
            // Create a page for this UI controller
            var page = new ExtractionPage
            {
                pageName = $"UI: {gameObject.name}",
                about = $"UI text from {gameObject.name} in scene {gameObject.scene.name}",
                fields = new List<ExtractionPageField>()
            };
            
            // Add title
            if (!string.IsNullOrEmpty(titleText))
            {
                page.fields.Add(new ExtractionPageField
                {
                    originalValue = titleText,
                    key = $"ui_{gameObject.name}_title" // Optional: specify a custom key
                });
            }
            
            // Add description
            if (!string.IsNullOrEmpty(descriptionText))
            {
                page.fields.Add(new ExtractionPageField
                {
                    originalValue = descriptionText,
                    key = $"ui_{gameObject.name}_description"
                });
            }
            
            // Add button labels
            for (int i = 0; i < buttonLabels.Count; i++)
            {
                if (!string.IsNullOrEmpty(buttonLabels[i]))
                {
                    page.fields.Add(new ExtractionPageField
                    {
                        originalValue = buttonLabels[i],
                        key = $"ui_{gameObject.name}_button_{i}"
                    });
                }
            }
            
            data.pages.Add(page);
            return data;
        }
        
        // Example method showing how you would use the localized text after extraction
        // BEFORE extraction: tmpText.text = titleText;
        // AFTER extraction: tmpText.SetLocalizedText(titleText);
        //   OR with key: tmpText.SetLocalizedText("ui_" + gameObject.name + "_title");
        private void ExampleUsage()
        {
            // This is just to show the pattern - not meant to be called
            // var tmpText = GetComponent<TMPro.TMP_Text>();
            // tmpText.SetLocalizedText(titleText); // If using Hybrid Key mode
            // OR
            // tmpText.SetLocalizedText($"ui_{gameObject.name}_title"); // If using keys
        }
    }
}
