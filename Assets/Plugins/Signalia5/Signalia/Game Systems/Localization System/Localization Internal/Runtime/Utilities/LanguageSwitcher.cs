using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
	/// <summary>
	/// A utility component for switching languages without coding.
	/// Can be configured to switch languages on Awake or Start, or manually via UnityEvents.
	/// </summary>
	[AddComponentMenu("Signalia/Game Systems/Localization/Signalia | Language Switcher")]
	public class LanguageSwitcher : MonoBehaviour
	{
		//[Header("Automatic Language Switch")]
		[Tooltip("The language code to switch to (e.g., 'en', 'es', 'fr', 'ar').")]
		[SerializeField] private string languageCode = "en";
		
		[Tooltip("When to automatically switch the language.")]
		[SerializeField] private SwitchTiming switchTiming = SwitchTiming.None;
		
		[Tooltip("Whether to save the language change as a preference.")]
		[SerializeField] private bool savePreference = true;
		
		private void Awake()
		{
			// watchman init
			Watchman.Watch();
			
			if (switchTiming == SwitchTiming.OnAwake)
			{
				SwitchLanguage();
			}
		}
		
		private void Start()
		{
			if (switchTiming == SwitchTiming.OnStart)
			{
				SwitchLanguage();
			}
		}
		
		/// <summary>
		/// Switches to the configured language.
		/// Can be called from UnityEvents (e.g., button clicks).
		/// </summary>
		public void SwitchLanguage()
		{
			if (string.IsNullOrEmpty(languageCode))
			{
				Debug.LogWarning("[Signalia LanguageSwitcher] Language code is empty. Cannot switch language.", this);
				return;
			}
			
			LocalizationRuntime.ChangeLanguage(languageCode, savePreference);
			
			if (RuntimeValues.Debugging.IsDebugging)
			{
				Debug.Log($"[Signalia LanguageSwitcher] Switched language to '{languageCode}'. Save preference: {savePreference}", this);
			}
		}
		
		/// <summary>
		/// Switches to a specific language code.
		/// Useful for dynamic language switching from code or UnityEvents with parameters.
		/// </summary>
		/// <param name="newLanguageCode">The language code to switch to</param>
		public void SwitchToLanguage(string newLanguageCode)
		{
			if (string.IsNullOrEmpty(newLanguageCode))
			{
				Debug.LogWarning("[Signalia LanguageSwitcher] Language code parameter is empty. Cannot switch language.", this);
				return;
			}
			
			LocalizationRuntime.ChangeLanguage(newLanguageCode, savePreference);
			
			if (RuntimeValues.Debugging.IsDebugging)
			{
				Debug.Log($"[Signalia LanguageSwitcher] Switched language to '{newLanguageCode}'. Save preference: {savePreference}", this);
			}
		}
		
		/// <summary>
		/// Sets the language code without switching.
		/// </summary>
		/// <param name="newLanguageCode">The new language code</param>
		public void SetLanguageCode(string newLanguageCode)
		{
			languageCode = newLanguageCode;
		}
		
		/// <summary>
		/// Sets whether to save the language preference.
		/// </summary>
		/// <param name="save">True to save, false otherwise</param>
		public void SetSavePreference(bool save)
		{
			savePreference = save;
		}
		
		/// <summary>
		/// Resets to the default language configured in Signalia Config.
		/// </summary>
		public void ResetToDefault()
		{
			LocalizationRuntime.ResetToDefault(savePreference);
			
			if (RuntimeValues.Debugging.IsDebugging)
			{
				Debug.Log($"[Signalia LanguageSwitcher] Reset to default language. Save preference: {savePreference}", this);
			}
		}
		
		/// <summary>
		/// Defines when the language switch should occur automatically.
		/// </summary>
		public enum SwitchTiming
		{
			/// <summary>No automatic switching</summary>
			None,
			/// <summary>Switch on Awake</summary>
			OnAwake,
			/// <summary>Switch on Start</summary>
			OnStart
		}
		
		/// <summary>
		/// Gets the current language code configured on this switcher.
		/// </summary>
		public string LanguageCode => languageCode;
		
		/// <summary>
		/// Gets whether this switcher saves preferences.
		/// </summary>
		public bool SavePreference => savePreference;
	}
}
