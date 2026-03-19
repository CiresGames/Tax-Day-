using System.Collections.Generic;

namespace AHAKuo.Signalia.GameSystems.Localization.External
{
    /// <summary>
    /// Interface for assets and MonoBehaviours that contain localizable text.
    /// Implement this interface to enable automatic extraction of localization data
    /// using the Locbook Extractor Tool (Tools > Signalia > Game Systems > Localization > Extract Locbook).
    /// </summary>
    public interface ILocbookExtraction
    {
        /// <summary>
        /// Returns the extraction data for this object.
        /// This includes pages of localizable content that can be converted into a LocBook.
        /// </summary>
        /// <returns>Extraction data containing pages with localizable strings</returns>
        ExtractionData GetExtractionData();
    }
    
    /// <summary>
    /// Container for all extraction data from a single source (MonoBehaviour or ScriptableObject).
    /// Can contain multiple pages of localizable content.
    /// </summary>
    [System.Serializable]
    public class ExtractionData
    {
        /// <summary>
        /// List of pages to extract. Each page represents a logical grouping of localization entries.
        /// </summary>
        public List<ExtractionPage> pages = new List<ExtractionPage>();

        /// <summary>
        /// Optional group name for organizing multiple sources.
        /// This can help categorize extracted data in the LocBook so they are added into locbooks of the same name.
        /// Leave empty for no grouping.
        /// </summary>
        public string groupName = string.Empty;
        
        public ExtractionData()
        {
            pages = new List<ExtractionPage>();
        }
        
        public ExtractionData(List<ExtractionPage> pages)
        {
            this.pages = pages ?? new List<ExtractionPage>();
        }
    }
    
    /// <summary>
    /// Represents a single page of localizable content.
    /// A page is a logical grouping of related localization entries.
    /// </summary>
    [System.Serializable]
    public class ExtractionPage
    {
        /// <summary>
        /// Unique identifier for this page.
        /// If left empty, will be auto-generated from the page name.
        /// </summary>
        public string pageId;
        
        /// <summary>
        /// Human-readable name for this page.
        /// Used for organization and identification in Lingramia.
        /// </summary>
        public string pageName;
        
        /// <summary>
        /// Description of this page and its contents.
        /// Helps developers understand what type of content this page contains.
        /// </summary>
        public string about;
        
        /// <summary>
        /// List of localizable fields in this page.
        /// Each field represents a single string that needs localization.
        /// </summary>
        public List<ExtractionPageField> fields = new List<ExtractionPageField>();
        
        public ExtractionPage()
        {
            fields = new List<ExtractionPageField>();
        }
        
        public ExtractionPage(string pageName, string about = "")
        {
            this.pageName = pageName;
            this.about = about;
            this.fields = new List<ExtractionPageField>();
        }
    }
    
    /// <summary>
    /// Represents a single localizable text field within a page.
    /// </summary>
    [System.Serializable]
    public class ExtractionPageField
    {
        /// <summary>
        /// Optional unique key for this entry.
        /// If left empty, will be auto-generated from the original value.
        /// If you plan to use keys in your code, set this manually.
        /// Otherwise, enable Hybrid Key mode in Signalia config to use original values as keys.
        /// </summary>
        public string key;
        
        /// <summary>
        /// The original text value in the source language.
        /// This is MANDATORY and will be used as the base for all translations.
        /// </summary>
        public string originalValue;
        
        /// <summary>
        /// Optional pre-existing language variants for this text.
        /// Most of the time this will be empty, as translations are added in Lingramia.
        /// Use this only if you already have translations available at extraction time.
        /// </summary>
        public List<ExtractionLanguageVariant> variants = new List<ExtractionLanguageVariant>();
        
        public ExtractionPageField()
        {
            variants = new List<ExtractionLanguageVariant>();
        }
        
        public ExtractionPageField(string originalValue, string key = "")
        {
            this.originalValue = originalValue;
            this.key = key;
            this.variants = new List<ExtractionLanguageVariant>();
        }
    }
    
    /// <summary>
    /// Represents a translation variant for a specific language.
    /// Typically only used if translations already exist at extraction time.
    /// </summary>
    [System.Serializable]
    public class ExtractionLanguageVariant
    {
        /// <summary>
        /// Language code (e.g., 'en', 'es', 'fr', 'ar', 'ja').
        /// </summary>
        public string languageCode;
        
        /// <summary>
        /// The translated text for this language.
        /// </summary>
        public string value;
        
        public ExtractionLanguageVariant()
        {
        }
        
        public ExtractionLanguageVariant(string languageCode, string value)
        {
            this.languageCode = languageCode;
            this.value = value;
        }
    }
}
