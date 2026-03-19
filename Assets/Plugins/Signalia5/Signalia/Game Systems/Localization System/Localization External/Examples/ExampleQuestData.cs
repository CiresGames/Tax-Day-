using UnityEngine;
using System.Collections.Generic;
using AHAKuo.Signalia.GameSystems.Localization.External;

namespace AHAKuo.Signalia.GameSystems.Localization.Examples
{
    /// <summary>
    /// Example implementation of ILocbookExtraction for a quest system.
    /// This demonstrates how to organize multiple types of text into separate pages.
    /// Place this asset in a Resources folder to enable extraction.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest", menuName = "Signalia/Examples/Example Quest Data", order = 999)]
    public class ExampleQuestData : ScriptableObject, ILocbookExtraction
    {
        [Header("Quest Information")]
        public string questTitle = "The Hero's Journey";
        
        [TextArea(2, 4)]
        public string questDescription = "Embark on an epic adventure to save the kingdom.";
        
        [Header("Objectives")]
        public List<string> objectives = new List<string>
        {
            "Find the ancient sword",
            "Defeat the dragon",
            "Return to the castle"
        };
        
        [Header("Rewards")]
        public List<string> rewardDescriptions = new List<string>
        {
            "1000 Gold",
            "Legendary Armor"
        };
        
        [Header("Dialogue")]
        [TextArea(3, 5)]
        public string questGiverDialogue = "Please, brave hero! Only you can save us!";
        
        [TextArea(3, 5)]
        public string completionDialogue = "You did it! The kingdom is saved!";
        
        /// <summary>
        /// Implementation of ILocbookExtraction.
        /// This example shows how to organize text into multiple logical pages.
        /// </summary>
        public ExtractionData GetExtractionData()
        {
            var data = new ExtractionData();
            
            // Page 1: Quest Info
            var infoPage = new ExtractionPage
            {
                pageId = $"quest_{name}_info",
                pageName = $"Quest: {questTitle} (Info)",
                about = "Quest title and description",
                fields = new List<ExtractionPageField>()
            };
            
            if (!string.IsNullOrEmpty(questTitle))
            {
                infoPage.fields.Add(new ExtractionPageField
                {
                    originalValue = questTitle,
                    key = $"quest_{name}_title"
                });
            }
            
            if (!string.IsNullOrEmpty(questDescription))
            {
                infoPage.fields.Add(new ExtractionPageField
                {
                    originalValue = questDescription,
                    key = $"quest_{name}_description"
                });
            }
            
            data.pages.Add(infoPage);
            
            // Page 2: Objectives
            var objectivesPage = new ExtractionPage
            {
                pageId = $"quest_{name}_objectives",
                pageName = $"Quest: {questTitle} (Objectives)",
                about = "Quest objectives that need to be completed",
                fields = new List<ExtractionPageField>()
            };
            
            for (int i = 0; i < objectives.Count; i++)
            {
                if (!string.IsNullOrEmpty(objectives[i]))
                {
                    objectivesPage.fields.Add(new ExtractionPageField
                    {
                        originalValue = objectives[i],
                        key = $"quest_{name}_objective_{i}"
                    });
                }
            }
            
            if (objectivesPage.fields.Count > 0)
            {
                data.pages.Add(objectivesPage);
            }
            
            // Page 3: Rewards
            var rewardsPage = new ExtractionPage
            {
                pageId = $"quest_{name}_rewards",
                pageName = $"Quest: {questTitle} (Rewards)",
                about = "Rewards description text",
                fields = new List<ExtractionPageField>()
            };
            
            for (int i = 0; i < rewardDescriptions.Count; i++)
            {
                if (!string.IsNullOrEmpty(rewardDescriptions[i]))
                {
                    rewardsPage.fields.Add(new ExtractionPageField
                    {
                        originalValue = rewardDescriptions[i],
                        key = $"quest_{name}_reward_{i}"
                    });
                }
            }
            
            if (rewardsPage.fields.Count > 0)
            {
                data.pages.Add(rewardsPage);
            }
            
            // Page 4: Dialogue
            var dialoguePage = new ExtractionPage
            {
                pageId = $"quest_{name}_dialogue",
                pageName = $"Quest: {questTitle} (Dialogue)",
                about = "Quest-related dialogue lines",
                fields = new List<ExtractionPageField>()
            };
            
            if (!string.IsNullOrEmpty(questGiverDialogue))
            {
                dialoguePage.fields.Add(new ExtractionPageField
                {
                    originalValue = questGiverDialogue,
                    key = $"quest_{name}_dialogue_start"
                });
            }
            
            if (!string.IsNullOrEmpty(completionDialogue))
            {
                dialoguePage.fields.Add(new ExtractionPageField
                {
                    originalValue = completionDialogue,
                    key = $"quest_{name}_dialogue_complete"
                });
            }
            
            if (dialoguePage.fields.Count > 0)
            {
                data.pages.Add(dialoguePage);
            }
            
            return data;
        }
    }
}
