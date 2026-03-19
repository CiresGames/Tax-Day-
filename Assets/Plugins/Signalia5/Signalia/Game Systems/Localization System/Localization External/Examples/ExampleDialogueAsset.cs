using UnityEngine;
using System.Collections.Generic;
using AHAKuo.Signalia.GameSystems.Localization.External;

namespace AHAKuo.Signalia.GameSystems.Localization.Examples
{
    /// <summary>
    /// Example implementation of ILocbookExtraction for a dialogue ScriptableObject.
    /// This shows how to extract localizable text from a custom asset.
    /// Place this asset in a Resources folder to enable extraction.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Signalia/Examples/Example Dialogue Asset", order = 999)]
    public class ExampleDialogueAsset : ScriptableObject, ILocbookExtraction
    {
        [System.Serializable]
        public class DialogueLine
        {
            public string characterName;
            public string dialogueText;
        }
        
        [Tooltip("The dialogue lines in this conversation")]
        public List<DialogueLine> dialogueLines = new List<DialogueLine>();
        
        /// <summary>
        /// Implementation of ILocbookExtraction.
        /// This method tells the extractor how to retrieve localizable text from this asset.
        /// </summary>
        public ExtractionData GetExtractionData()
        {
            var data = new ExtractionData();
            data.groupName = "Dialogues"; // Group name for organizing in Locbook
            
            // Create a page for this dialogue asset
            var page = new ExtractionPage
            {
                pageName = $"Dialogue: {name}",
                about = $"Dialogue lines from {name} asset",
                fields = new List<ExtractionPageField>()
            };
            
            // Add each dialogue line as a field
            foreach (var line in dialogueLines)
            {
                // Add character name
                if (!string.IsNullOrEmpty(line.characterName))
                {
                    page.fields.Add(new ExtractionPageField
                    {
                        originalValue = line.characterName,
                        key = "" // Leave empty to auto-generate, or set manually for more control
                    });
                }
                
                // Add dialogue text
                if (!string.IsNullOrEmpty(line.dialogueText))
                {
                    page.fields.Add(new ExtractionPageField
                    {
                        originalValue = line.dialogueText,
                        key = "" // Leave empty to auto-generate
                    });
                }
            }
            
            data.pages.Add(page);
            return data;
        }
    }
}
