using UnityEngine;
using TMPro;
using System;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
    /// <summary>
    /// A ScriptableObject that defines text formatting and font settings for a specific language.
    /// Used by the localization system to automatically format text based on the active language.
    /// </summary>
    [CreateAssetMenu(fileName = "New Text Style", menuName = "Signalia/Game Systems/Localization/Text Style", order = 2)]
    public class TextStyle : ScriptableObject
    {
        //[Header("Language")]
        [Tooltip("The language code this style applies to (e.g., 'en', 'es', 'fr', 'ar')")]
        [SerializeField] private string languageCode = "en";
        
        [Tooltip("Optional paragraph style identifier (e.g., 'Header', 'Description', 'Body', 'Caption'). Leave empty for default style. You can use any custom string value.")]
        [SerializeField] private string paragraphStyle = "";
        
        //[Header("Font")]
        [Tooltip("The TMP Font Asset to use for this language")]
        [SerializeField] private TMP_FontAsset font;
        
        [Tooltip("Optional material override to apply after the font is set. Leave empty to use the font's default material.")]
        [SerializeField] private Material materialOverride;
        
        //[Header("Formatting Options")]
        [Tooltip("Select multiple formatting options to apply. Conflicting options will be prevented.")]
        [SerializeField] private FormattingOptions formattingOptions = FormattingOptions.None;
        
        //[Header("Writing Systems")]
        [Tooltip("Enable right-to-left text direction for languages like Arabic or Hebrew.")]
        [SerializeField] private bool enableRTL = false;

        [Tooltip("Format Arabic text so characters connect correctly. Disable if not using Arabic.")]
        [SerializeField] private bool enableArabicFormatting = false;
        
        /// <summary>
        /// The language code this style applies to.
        /// </summary>
        public string LanguageCode => languageCode;
        
        /// <summary>
        /// The paragraph style this text style applies to. Returns empty string if not set.
        /// </summary>
        public string ParagraphStyle => paragraphStyle ?? "";
        
        /// <summary>
        /// The TMP Font Asset for this language.
        /// </summary>
        public TMP_FontAsset Font => font;
        
        /// <summary>
        /// The material override for this text style, applied after the font is set.
        /// </summary>
        public Material MaterialOverride => materialOverride;
        
        /// <summary>
        /// The formatting options for this language.
        /// </summary>
        public FormattingOptions Formatting => formattingOptions;
        
        /// <summary>
        /// Whether right-to-left text direction is enabled.
        /// </summary>
        public bool EnableRTL => enableRTL;

        /// <summary>
        /// Whether Arabic text formatting is enabled.
        /// </summary>
        public bool EnableArabicFormatting => enableArabicFormatting;
        
        /// <summary>
        /// Applies this text style to a TMP_Text component.
        /// </summary>
        /// <param name="text">The TMP_Text component to apply styling to</param>
        public void ApplyToText(TMP_Text text)
        {
            if (text == null)
            {
                Debug.LogWarning("[Signalia TextStyle] Cannot apply style: TMP_Text is null.");
                return;
            }
            
            // Always reset RTL first to prevent it from persisting when switching languages
            text.isRightToLeftText = false;
            
            // Apply font if set
            if (font != null)
            {
                text.font = font;
            }
            
            // Apply material override after font is set
            if (materialOverride != null)
            {
                text.fontMaterial = materialOverride;
            }

            // Build font style from flags
            FontStyles fontStyle = FontStyles.Normal;
            
            if (HasFlag(FormattingOptions.Bold))
            {
                fontStyle |= FontStyles.Bold;
            }
            
            if (HasFlag(FormattingOptions.Italic))
            {
                fontStyle |= FontStyles.Italic;
            }
            
            if (HasFlag(FormattingOptions.Underline))
            {
                fontStyle |= FontStyles.Underline;
            }
            
            // Apply the combined font style
            text.fontStyle = fontStyle;

            // Apply string-based formatting
            string formattedText = ApplyFormattingToString(text.text);
            if (formattedText != text.text)
            {
                text.text = formattedText;
            }

            // Apply RTL from Writing Systems
            if (enableRTL)
            {
                text.isRightToLeftText = true;
            }
        }

        /// <summary>
        /// Applies string based formatting options and returns the formatted value.
        /// </summary>
        /// <param name="input">Text to format</param>
        /// <returns>Formatted text</returns>
        public string ApplyFormattingToString(string input)
        {
            if (input == null)
            {
                return null;
            }

            string result = input;

            // Apply text case transformations (mutually exclusive)
            if (HasFlag(FormattingOptions.AllCaps))
            {
                result = result.ToUpperInvariant();
            }
            else if (HasFlag(FormattingOptions.TitleCase))
            {
                result = ToTitleCase(result);
            }
            else if (HasFlag(FormattingOptions.LowerCase))
            {
                result = result.ToLowerInvariant();
            }

            if (enableArabicFormatting)
            {
                result = ArabicTextFormatter.Format(result, font);
            }

            return result;
        }
        
        /// <summary>
        /// Checks if a specific formatting option is enabled.
        /// </summary>
        private bool HasFlag(FormattingOptions flag)
        {
            return (formattingOptions & flag) == flag;
        }
        
        /// <summary>
        /// Converts a string to title case.
        /// </summary>
        private string ToTitleCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            
            var words = input.Split(' ');
            
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
                }
            }
            
            return string.Join(" ", words);
        }
        
        /// <summary>
        /// Validates that conflicting formatting options are not selected together.
        /// </summary>
        private void OnValidate()
        {
            // Check for conflicting case transformations
            int caseTransformCount = 0;
            if (HasFlag(FormattingOptions.AllCaps)) caseTransformCount++;
            if (HasFlag(FormattingOptions.TitleCase)) caseTransformCount++;
            if (HasFlag(FormattingOptions.LowerCase)) caseTransformCount++;
            
            if (caseTransformCount > 1)
            {
                Debug.LogWarning($"[Signalia TextStyle] Multiple case transformations selected in '{name}'. Only one will be applied (priority: AllCaps > TitleCase > LowerCase).", this);
            }
        }
    }
    
    /// <summary>
    /// Formatting options available for text styles. Can be combined using bitwise operations.
    /// </summary>
    [Flags]
    [Serializable]
    public enum FormattingOptions
    {
        None = 0,
        Bold = 1 << 0,          // 1
        Italic = 1 << 1,        // 2
        Underline = 1 << 2,     // 4
        AllCaps = 1 << 3,       // 8
        TitleCase = 1 << 4,     // 16
        LowerCase = 1 << 5      // 32
    }
}
