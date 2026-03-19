using UnityEngine;
using TMPro;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
	/// <summary>
	/// A simplified localized text component that requires only a key.
	/// This is a streamlined version of LocalizedText with minimal configuration.
	/// Perfect for quick localization without needing overrides or complex settings.
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	[AddComponentMenu("Signalia/Game Systems/Localization/Signalia | Localized Text (Simple)")]
	public class SimpleLocalizedText : MonoBehaviour
	{
		//[Header("Localization Key")]
		[Tooltip("The localization key to retrieve the text.")]
		[SerializeField] private string key;
		
		[Tooltip("Optional paragraph style identifier (e.g., 'Header', 'Description', 'Body'). Leave empty for default style. You can use any custom string value.")]
		[SerializeField] private string paragraphStyle = "";
		
		private TMP_Text textComponent;
		private AHAKuo.Signalia.Radio.Listener languageChangeListener;
		
		private void Awake()
		{
			// watchman init
			Watchman.Watch();
			
			textComponent = GetComponent<TMP_Text>();
			
			if (textComponent == null)
			{
				Debug.LogError("[Signalia SimpleLocalizedText] No TMP_Text component found on this GameObject!", this);
			}
		}
		
		private void Start()
		{
			UpdateText();
			SubscribeToLanguageChange();
		}
		
		private void OnDestroy()
		{
			languageChangeListener?.Dispose();
		}
		
		/// <summary>
		/// Updates the text with the current localization key.
		/// </summary>
		public void UpdateText()
		{
			if (textComponent == null || string.IsNullOrEmpty(key))
			{
				return;
			}
			
			// Get the localized string using the current language
			string localizedString = Localization.ReadKey(key, LocalizationRuntime.CurrentLanguageCode, paragraphStyle);
			
			// Apply text to component
			textComponent.text = localizedString;
			
			// Apply automatic text style for the current language with paragraph style filtering
			TextStyle autoStyle = LocalizationRuntime.GetCurrentTextStyle(paragraphStyle ?? "");
			if (autoStyle != null)
			{
				autoStyle.ApplyToText(textComponent);
			}
		}
		
		/// <summary>
		/// Sets a new localization key and updates the text.
		/// </summary>
		/// <param name="newKey">The new localization key</param>
		public void SetKey(string newKey)
		{
			key = newKey;
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
		public string Key => key;
		
		/// <summary>
		/// Gets the current paragraph style. Returns empty string if not set.
		/// </summary>
		public string ParagraphStyle => paragraphStyle ?? "";
	}
}
