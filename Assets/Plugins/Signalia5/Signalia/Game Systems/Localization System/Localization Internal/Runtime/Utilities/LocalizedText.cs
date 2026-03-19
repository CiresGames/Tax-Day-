using UnityEngine;
using TMPro;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
	/// <summary>
	/// Helper component that automatically sets localized text on a TMP_Text component.
	/// Attach this to any GameObject with a TMP_Text component to enable automatic localization.
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	[AddComponentMenu("Signalia/Game Systems/Localization/Signalia | Localized Text")]
	public class LocalizedText : MonoBehaviour
	{
		//[Header("Localization Settings")]
		[Tooltip("The localization key to retrieve the text. Can also be the source string if Hybrid Key is enabled.")]
		[SerializeField] private string localizationKey;
		
		//[Header("Optional Overrides")]
		[Tooltip("Override the language code. Leave empty to use the current system language.")]
		[SerializeField] private string languageCodeOverride = "";
		
		[Tooltip("Override the text style. Leave empty to use the automatic style for the language.")]
		[SerializeField] private TextStyle textStyleOverride;
		
		[Tooltip("Optional paragraph style identifier (e.g., 'Header', 'Description', 'Body'). Leave empty for default style. You can use any custom string value.")]
		[SerializeField] private string paragraphStyle = "";
		
		//[Header("Update Settings")]
		[Tooltip("Automatically update the text when the language changes.")]
		[SerializeField] private bool updateOnLanguageChange = true;
		
		[Tooltip("Set the localized text on Start.")]
		[SerializeField] private bool setOnStart = true;
		
		private TMP_Text textComponent;
		private AHAKuo.Signalia.Radio.Listener languageChangeListener;
		
		private void Awake()
		{
			// watchman init
			Watchman.Watch();

			textComponent = GetComponent<TMP_Text>();
			
			if (textComponent == null)
			{
				Debug.LogError("[Signalia LocalizedText] No TMP_Text component found on this GameObject!", this);
			}
		}
		
		private void Start()
		{
			if (setOnStart)
			{
				UpdateText();
			}
			
			// Subscribe to language change events if enabled
			if (updateOnLanguageChange)
			{
				SubscribeToLanguageChange();
			}
		}
		
		private void OnDestroy()
		{
			// Clean up listener
			languageChangeListener?.Dispose();
		}
		
		/// <summary>
		/// Updates the text with the current localization settings.
		/// Call this manually if you need to refresh the text.
		/// </summary>
		public void UpdateText()
		{
			if (textComponent == null || string.IsNullOrEmpty(localizationKey))
			{
				return;
			}
			
			// Determine which language to use
			string targetLanguage = string.IsNullOrEmpty(languageCodeOverride) 
				? LocalizationRuntime.CurrentLanguageCode 
				: languageCodeOverride;
			
			// Get the localized string
			string localizedString = Localization.ReadKey(localizationKey, targetLanguage, paragraphStyle);
			
			// Apply text to component
			textComponent.text = localizedString;
			
			// Apply text style
			if (textStyleOverride != null)
			{
				// Use the override style
				textStyleOverride.ApplyToText(textComponent);
			}
			else
			{
				// Use the automatic style from the system with paragraph style filtering
				TextStyle autoStyle = LocalizationRuntime.GetTextStyle(targetLanguage, paragraphStyle);
				if (autoStyle != null)
				{
					autoStyle.ApplyToText(textComponent);
				}
			}
		}
		
		/// <summary>
		/// Sets a new localization key and updates the text.
		/// </summary>
		/// <param name="key">The new localization key</param>
		public void SetKey(string key)
		{
			localizationKey = key;
			UpdateText();
		}
		
		/// <summary>
		/// Sets a new language override and updates the text.
		/// </summary>
		/// <param name="languageCode">The language code to use, or empty string to use system language</param>
		public void SetLanguageOverride(string languageCode)
		{
			languageCodeOverride = languageCode;
			UpdateText();
		}
		
		/// <summary>
		/// Sets a new text style override and updates the text.
		/// </summary>
		/// <param name="style">The text style to use, or null to use automatic style</param>
		public void SetTextStyleOverride(TextStyle style)
		{
			textStyleOverride = style;
			UpdateText();
		}
		
		/// <summary>
		/// Sets a new paragraph style and updates the text.
		/// </summary>
		/// <param name="style">The paragraph style to use (e.g., "Header", "Description"). Leave empty or null for default style.</param>
		public void SetParagraphStyle(string style)
		{
			paragraphStyle = style ?? "";
			UpdateText();
		}
		
		/// <summary>
		/// Clears all overrides and uses system defaults.
		/// </summary>
		public void ClearOverrides()
		{
			languageCodeOverride = "";
			textStyleOverride = null;
			paragraphStyle = "";
			UpdateText();
		}
		
		private void SubscribeToLanguageChange()
		{
			var config = AHAKuo.Signalia.Framework.ConfigReader.GetConfig();
			if (config != null && !string.IsNullOrEmpty(config.LocalizationSystem.LanguageChangedEvent))
			{
				languageChangeListener = new AHAKuo.Signalia.Radio.Listener(
					config.LocalizationSystem.LanguageChangedEvent,
					() => UpdateText(),
					false,
					gameObject
				);
			}
		}
		
		/// <summary>
		/// Gets the current localization key.
		/// </summary>
		public string LocalizationKey => localizationKey;
		
		/// <summary>
		/// Gets the current language code override.
		/// </summary>
		public string LanguageCodeOverride => languageCodeOverride;
		
		/// <summary>
		/// Gets the current text style override.
		/// </summary>
		public TextStyle TextStyleOverride => textStyleOverride;
		
		/// <summary>
		/// Gets the current paragraph style. Returns empty string if not set.
		/// </summary>
		public string ParagraphStyle => paragraphStyle ?? "";
	}
}
