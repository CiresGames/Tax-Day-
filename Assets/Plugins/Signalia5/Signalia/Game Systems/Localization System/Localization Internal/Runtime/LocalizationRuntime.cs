using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;

using AHAKuo.Signalia.GameSystems.SaveSystem;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
    /// <summary>
    /// Manages runtime state for the localization system, including current language code,
    /// preference saving, and language change events.
    /// </summary>
    public static class LocalizationRuntime
    {
        private static string currentLanguageCode;
        private static bool isInitialized = false;
        
        /// <summary>
        /// Gets the current language code. Initializes with default if not set.
        /// </summary>
        public static string CurrentLanguageCode
        {
            get
            {
                if (!isInitialized)
                {
                    Initialize();
                }
                return currentLanguageCode;
            }
            private set
            {
                currentLanguageCode = value;
            }
        }
        
        /// <summary>
        /// Initializes the runtime system with the default or saved language.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }
            
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            if (config == null)
            {
                Debug.LogWarning("[Signalia Localization] Config not found. Using fallback language 'en'.");
                currentLanguageCode = "en";
                isInitialized = true;
                return;
            }
            
            string defaultLanguage = config.LocalizationSystem.DefaultStartingLanguageCode;
            
            if (string.IsNullOrEmpty(defaultLanguage))
            {
                defaultLanguage = "en";
            }
            
            // Try to load saved language preference
            string saveKey = config.LocalizationSystem.LanguageOptionSaveKey;
            
            if (!string.IsNullOrEmpty(saveKey))
            {
                try
                {
                    string savedLanguage = GameSaving.LoadPreference<string>(saveKey, defaultLanguage);
                    currentLanguageCode = savedLanguage;
                }
                catch
                {
                    currentLanguageCode = defaultLanguage;
                }
            }
            else
            {
                currentLanguageCode = defaultLanguage;
            }
            
            isInitialized = true;
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.Log($"[Signalia Localization Runtime] Initialized with language: {currentLanguageCode}");
            }
        }
        
        /// <summary>
        /// Changes the current language and optionally saves the preference.
        /// </summary>
        /// <param name="languageCode">The new language code (e.g., "en", "es", "fr")</param>
        /// <param name="save">Whether to save this preference for future sessions</param>
        public static void ChangeLanguage(string languageCode, bool save = true)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                Debug.LogWarning("[Signalia Localization] Cannot change to null or empty language code.");
                return;
            }
            
            string previousLanguage = currentLanguageCode;
            currentLanguageCode = languageCode;
            
            if (save)
            {
                SignaliaConfigAsset config = ConfigReader.GetConfig();
                
                if (config != null)
                {
                    string saveKey = config.LocalizationSystem.LanguageOptionSaveKey;
                    
                    if (!string.IsNullOrEmpty(saveKey))
                    {
                        GameSaving.SavePreference(saveKey, languageCode);
                    }
                }
            }
            
            // Fire language change event
            FireLanguageChangedEvent();
            
            if (RuntimeValues.Debugging.IsDebugging)
            {
                Debug.Log($"[Signalia Localization Runtime] Language changed from '{previousLanguage}' to '{currentLanguageCode}'. Saved: {save}");
            }
        }
        
        /// <summary>
        /// Fires the language changed event through the Radio system.
        /// This allows UI and other systems to respond to language changes.
        /// </summary>
        public static void FireLanguageChangedEvent()
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            if (config != null)
            {
                string eventName = config.LocalizationSystem.LanguageChangedEvent;
                
                if (!string.IsNullOrEmpty(eventName))
                {
                    SimpleRadio.SendEvent(eventName, currentLanguageCode);
                    
                    if (RuntimeValues.Debugging.IsDebugging)
                    {
                        Debug.Log($"[Signalia Localization Runtime] Fired language changed event: {eventName}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Resets the language to the default starting language from config.
        /// </summary>
        /// <param name="save">Whether to save this reset</param>
        public static void ResetToDefault(bool save = true)
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig();
            
            if (config != null)
            {
                string defaultLanguage = config.LocalizationSystem.DefaultStartingLanguageCode;
                
                if (string.IsNullOrEmpty(defaultLanguage))
                {
                    defaultLanguage = "en";
                }
                
                ChangeLanguage(defaultLanguage, save);
            }
            else
            {
                ChangeLanguage("en", save);
            }
        }
        
        /// <summary>
        /// Gets the TextStyle asset for the current language from the config cache.
        /// </summary>
        /// <param name="paragraphStyle">Optional paragraph style to filter by (e.g., "Header", "Body"). Leave empty or null for default style.</param>
        /// <returns>TextStyle asset for the current language, or null if not found</returns>
        public static TextStyle GetCurrentTextStyle(string paragraphStyle = "")
        {
            return GetTextStyle(currentLanguageCode, paragraphStyle);
        }
        
        /// <summary>
        /// Gets the TextStyle asset for a specific language code from the config cache.
        /// When a paragraph style is specified, prioritizes styles matching that paragraph style.
        /// If no matching paragraph style is found, falls back to empty paragraph style, then any style with matching language.
        /// Paragraph style is treated as a preference - the language code match is the primary requirement.
        /// </summary>
        /// <param name="languageCode">The language code</param>
        /// <param name="paragraphStyle">Optional paragraph style to filter by (e.g., "Header", "Body"). Leave empty or null for default style.</param>
        /// <returns>TextStyle asset for the language, or null if not found</returns>
        public static TextStyle GetTextStyle(string languageCode, string paragraphStyle = "")
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig();

            if (config == null || config.LocalizationSystem.TextStyleCache == null)
            {
                return null;
            }

            // Normalize paragraph style (treat null as empty)
            string normalizedParagraphStyle = string.IsNullOrEmpty(paragraphStyle) ? "" : paragraphStyle;

            TextStyle emptyStyleFallback = null;
            TextStyle anyStyleFallback = null;

            foreach (var style in config.LocalizationSystem.TextStyleCache)
            {
                if (style != null && style.LanguageCode == languageCode)
                {
                    string styleParagraphStyle = style.ParagraphStyle ?? "";
                    
                    // If we find an exact match (language + paragraph style), return it immediately
                    if (styleParagraphStyle == normalizedParagraphStyle)
                    {
                        return style;
                    }
                    
                    // Keep track of the empty paragraph style as preferred fallback
                    if (string.IsNullOrEmpty(styleParagraphStyle) && emptyStyleFallback == null)
                    {
                        emptyStyleFallback = style;
                    }
                    
                    // Keep track of ANY style with matching language as ultimate fallback
                    if (anyStyleFallback == null)
                    {
                        anyStyleFallback = style;
                    }
                }
            }

            // Return in order of preference: empty fallback > any language match
            return emptyStyleFallback ?? anyStyleFallback;
        }

        /// <summary>
        /// Applies language-specific formatting (such as Arabic shaping) to a string using the language's text style.
        /// </summary>
        /// <param name="value">Text to format</param>
        /// <param name="languageCode">Language code the text is intended for</param>
        /// <returns>Formatted text, or the original value if no formatting is required</returns>
        public static string FormatForLanguage(string value, string languageCode, string paragraphStyle = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            TextStyle style = GetTextStyle(languageCode, paragraphStyle);
            return style != null ? style.ApplyFormattingToString(value) : value;
        }

        /// <summary>
        /// Returns whether the runtime has been initialized.
        /// </summary>
        public static bool IsInitialized => isInitialized;
    }
}
