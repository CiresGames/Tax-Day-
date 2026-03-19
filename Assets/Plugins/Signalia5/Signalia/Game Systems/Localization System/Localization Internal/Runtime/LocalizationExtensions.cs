using System;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
    /// <summary>
    /// Extension methods for TextMeshPro components to support localized text with automatic formatting.
    /// </summary>
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Sets localized text on a TMP_Text component with automatic text style formatting.
        /// This is the primary method for displaying localized text in your UI.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="key">The localization key or source string (if hybrid key is enabled)</param>
        /// <param name="textStyleOverride">Optional text style to override automatic formatting</param>
        /// <param name="languageCode">Optional language code override (uses current language if empty)</param>
        /// <param name="paragraphStyle">Optional paragraph style to filter text styles (e.g., "Header", "Description"). Leave empty or null for default style.</param>
        public static void SetLocalizedText(this TMP_Text text, string key, TextStyle textStyleOverride = null, string languageCode = "", string paragraphStyle = "")
        {
            if (text == null)
            {
                Debug.LogWarning("[Signalia Localization] Cannot set localized text: TMP_Text component is null.");
                return;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                text.text = string.Empty;
                return;
            }
            
            // Determine which language to use
            string targetLanguage = string.IsNullOrEmpty(languageCode) ? LocalizationRuntime.CurrentLanguageCode : languageCode;
            
            // Get the localized string
            string localizedString = Localization.ReadKey(key, targetLanguage, paragraphStyle);
            
            // Apply text to the component
            text.text = localizedString;
            
            // Determine which text style to use
            TextStyle styleToApply = textStyleOverride ?? LocalizationRuntime.GetTextStyle(targetLanguage, paragraphStyle ?? "");
            
            // Apply formatting
            if (styleToApply != null)
            {
                styleToApply.ApplyToText(text);
            }
        }
        
        /// <summary>
        /// Sets localized text on a TMP_Text component with string formatting support.
        /// Arguments are automatically localized before formatting is applied.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="formatKey">The localization key for the format string (e.g., "greeting_hello" for "Hello {0}, welcome {1}!")</param>
        /// <param name="args">Localization keys for the format arguments. Each argument will be localized before formatting.</param>
        /// <param name="textStyleOverride">Optional text style to override automatic formatting</param>
        /// <param name="languageCode">Optional language code override (uses current language if empty)</param>
        /// <example>
        /// // If "greeting_hello" = "Hello {0}, welcome {1}!"
        /// // And "player_name" = "John", "world_name" = "Earth"
        /// text.SetLocalizedTextFormat("greeting_hello", "player_name", "world_name");
        /// // Result: "Hello John, welcome Earth!"
        /// </example>
        public static void SetLocalizedTextFormat(this TMP_Text text, string formatKey, params string[] args)
        {
            SetLocalizedTextFormat(text, formatKey, null, "", "", args);
        }
        
        /// <summary>
        /// Sets localized text on a TMP_Text component with string formatting support.
        /// Arguments are automatically localized before formatting is applied.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="formatKey">The localization key for the format string</param>
        /// <param name="textStyleOverride">Optional text style to override automatic formatting</param>
        /// <param name="args">Localization keys for the format arguments. Each argument will be localized before formatting.</param>
        public static void SetLocalizedTextFormat(this TMP_Text text, string formatKey, TextStyle textStyleOverride, params string[] args)
        {
            SetLocalizedTextFormat(text, formatKey, textStyleOverride, "", "", args);
        }
        
        /// <summary>
        /// Sets localized text on a TMP_Text component with string formatting support.
        /// Arguments are automatically localized before formatting is applied.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="formatKey">The localization key for the format string</param>
        /// <param name="textStyleOverride">Optional text style to override automatic formatting</param>
        /// <param name="languageCode">Optional language code override (uses current language if empty)</param>
        /// <param name="paragraphStyle">Optional paragraph style to filter text styles (e.g., "Header", "Description"). Leave empty or null for default style.</param>
        /// <param name="args">Localization keys for the format arguments. Each argument will be localized before formatting.</param>
        public static void SetLocalizedTextFormat(this TMP_Text text, string formatKey, TextStyle textStyleOverride, string languageCode, string paragraphStyle, params string[] args)
        {
            if (text == null)
            {
                Debug.LogWarning("[Signalia Localization] Cannot set localized text: TMP_Text component is null.");
                return;
            }
            
            if (string.IsNullOrEmpty(formatKey))
            {
                text.text = string.Empty;
                return;
            }
            
            // Determine which language to use
            string targetLanguage = string.IsNullOrEmpty(languageCode) ? LocalizationRuntime.CurrentLanguageCode : languageCode;
            
            // Get the raw localized format string (without formatting applied)
            string rawFormat = Localization.ReadKey(formatKey, targetLanguage, paragraphStyle);
            
            // Localize all arguments as raw strings (without formatting)
            object[] rawArgs = null;
            if (args != null && args.Length > 0)
            {
                rawArgs = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    // Each argument is a localization key that gets localized as raw text
                    rawArgs[i] = Localization.ReadKey(args[i], targetLanguage, paragraphStyle);
                }
            }
            
            // Apply string formatting (this replaces {0}, {1}, etc. with actual values)
            string formattedText;
            try
            {
                if (rawArgs != null && rawArgs.Length > 0)
                {
                    formattedText = string.Format(rawFormat, rawArgs);
                }
                else
                {
                    formattedText = rawFormat;
                }
            }
            catch (FormatException e)
            {
                Debug.LogError($"[Signalia Localization] Format string error for key '{formatKey}': {e.Message}");
                formattedText = rawFormat;
            }
            
            // Apply language-specific formatting (e.g., Arabic shaping) AFTER format placeholders are replaced
            string finalText = Localization.ApplyFormatting(formattedText, targetLanguage, paragraphStyle);
            
            // Apply text to the component
            text.text = finalText;
            
            // Determine which text style to use
            TextStyle styleToApply = textStyleOverride ?? LocalizationRuntime.GetTextStyle(targetLanguage, paragraphStyle);
            
            // Apply formatting
            if (styleToApply != null)
            {
                styleToApply.ApplyToText(text);
            }
        }
        
        /// <summary>
        /// Sets localized text with dynamic format arguments (numbers, runtime values).
        /// Use this when values are NOT localization keys. For Arabic, values are pre-reversed
        /// so they display correctly after RTL formatting reverses them.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="formatKey">The localization key for the format string (e.g., "gold_label" for "Gold: {0}")</param>
        /// <param name="args">Dynamic values to insert (numbers, strings, etc.) - NOT localization keys</param>
        /// <example>
        /// // If "gold_label" = "Gold: {0}"
        /// goldText.SetLocalizedStringFormat_D("gold_label", playerGold);
        /// // Result in English: "Gold: 1234" | In Arabic: "ذهب: 1234" (numbers display correctly)
        /// </example>
        public static void SetLocalizedStringFormat_D(this TMP_Text text, string formatKey, params object[] args)
        {
            SetLocalizedStringFormat_D(text, formatKey, null, "", "", args);
        }

        /// <summary>
        /// Sets localized text with dynamic format arguments (numbers, runtime values).
        /// Use this when values are NOT localization keys. For Arabic, values are pre-reversed
        /// so they display correctly after RTL formatting reverses them.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="formatKey">The localization key for the format string</param>
        /// <param name="textStyleOverride">Optional text style to override automatic formatting</param>
        /// <param name="args">Dynamic values to insert - NOT localization keys</param>
        public static void SetLocalizedStringFormat_D(this TMP_Text text, string formatKey, TextStyle textStyleOverride, params object[] args)
        {
            SetLocalizedStringFormat_D(text, formatKey, textStyleOverride, "", "", args);
        }

        /// <summary>
        /// Sets localized text with dynamic format arguments (numbers, runtime values).
        /// Use this when values are NOT localization keys. For Arabic, values are pre-reversed
        /// so they display correctly after RTL formatting reverses them.
        /// </summary>
        /// <param name="text">The TMP_Text component to set text on</param>
        /// <param name="formatKey">The localization key for the format string</param>
        /// <param name="textStyleOverride">Optional text style to override automatic formatting</param>
        /// <param name="languageCode">Optional language code override (uses current language if empty)</param>
        /// <param name="paragraphStyle">Optional paragraph style to filter text styles</param>
        /// <param name="args">Dynamic values to insert - NOT localization keys</param>
        public static void SetLocalizedStringFormat_D(this TMP_Text text, string formatKey, TextStyle textStyleOverride, string languageCode, string paragraphStyle, params object[] args)
        {
            if (text == null)
            {
                Debug.LogWarning("[Signalia Localization] Cannot set localized text: TMP_Text component is null.");
                return;
            }

            if (string.IsNullOrEmpty(formatKey))
            {
                text.text = string.Empty;
                return;
            }

            string targetLanguage = string.IsNullOrEmpty(languageCode) ? LocalizationRuntime.CurrentLanguageCode : languageCode;
            string rawFormat = Localization.ReadKey(formatKey, targetLanguage, paragraphStyle);

            bool useArabicPreReverse = false;
            TextStyle style = LocalizationRuntime.GetTextStyle(targetLanguage, paragraphStyle ?? "");
            if (style != null && style.EnableArabicFormatting)
            {
                useArabicPreReverse = true;
            }

            string formattedText;
            try
            {
                if (args != null && args.Length > 0)
                {
                    // Parse placeholders and format each arg (supports {0}, {0:N0}, {0,10}, etc.)
                    // Pre-reverse for Arabic so FixContinuousLTR displays values correctly
                    formattedText = FormatWithDynamicArgs(rawFormat, args, useArabicPreReverse);
                }
                else
                {
                    formattedText = rawFormat;
                }
            }
            catch (FormatException e)
            {
                Debug.LogError($"[Signalia Localization] Format string error for key '{formatKey}': {e.Message}");
                formattedText = rawFormat;
            }

            string finalText = Localization.ApplyFormatting(formattedText, targetLanguage, paragraphStyle);
            text.text = finalText;

            TextStyle styleToApply = textStyleOverride ?? LocalizationRuntime.GetTextStyle(targetLanguage, paragraphStyle);
            if (styleToApply != null)
            {
                styleToApply.ApplyToText(text);
            }
        }

        /// <summary>
        /// Formats a string with dynamic args, optionally pre-reversing each value for Arabic RTL display.
        /// Supports full format specifiers: {0}, {0:N0}, {0,10}, {0,10:N0}, etc.
        /// </summary>
        private static string FormatWithDynamicArgs(string format, object[] args, bool preReverseForArabic)
        {
            // Matches {0}, {0:N0}, {0,10}, {0,10:N0}, etc.
            var regex = new Regex(@"\{(\d+)(?:,([^:}]*))?(?::([^}]*))?\}");
            return regex.Replace(format, match =>
            {
                int index = int.Parse(match.Groups[1].Value);
                string alignment = match.Groups[2].Success ? match.Groups[2].Value : "";
                string formatSpec = match.Groups[3].Success ? match.Groups[3].Value : "";

                if (index < 0 || index >= args.Length)
                    return match.Value;

                object arg = args[index];
                string placeholder = "{0" + (string.IsNullOrEmpty(alignment) ? "" : "," + alignment) + (string.IsNullOrEmpty(formatSpec) ? "" : ":" + formatSpec) + "}";
                string formatted = string.Format(placeholder, arg);

                return preReverseForArabic ? ArabicTextFormatter.ReverseForRTLDisplay(formatted) : formatted;
            });
        }

        /// <summary>
        /// Updates the localized text on this component using the current language.
        /// Useful for responding to language change events.
        /// </summary>
        /// <param name="text">The TMP_Text component</param>
        /// <param name="key">The localization key</param>
        public static void RefreshLocalizedText(this TMP_Text text, string key)
        {
            SetLocalizedText(text, key);
        }
    }
}
