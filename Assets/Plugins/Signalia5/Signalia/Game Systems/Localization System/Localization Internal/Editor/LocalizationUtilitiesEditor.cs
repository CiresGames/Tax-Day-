#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using TMPro;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
	/// <summary>
	/// Custom editor for LocalizedText components.
	/// Provides a clean interface for setting localization keys and overrides.
	/// </summary>
	[CustomEditor(typeof(LocalizedText))]
	public class LocalizedTextEditor : Editor
	{
		private SerializedProperty localizationKeyProp;
		private SerializedProperty languageCodeOverrideProp;
		private SerializedProperty textStyleOverrideProp;
		private SerializedProperty paragraphStyleProp;
		private SerializedProperty updateOnLanguageChangeProp;
		private SerializedProperty setOnStartProp;
		
		private int selectedTab = 0;
		private readonly string[] tabs = { "Settings", "Info" };
		
		private void OnEnable()
		{
			localizationKeyProp = serializedObject.FindProperty("localizationKey");
			languageCodeOverrideProp = serializedObject.FindProperty("languageCodeOverride");
			textStyleOverrideProp = serializedObject.FindProperty("textStyleOverride");
			paragraphStyleProp = serializedObject.FindProperty("paragraphStyle");
			updateOnLanguageChangeProp = serializedObject.FindProperty("updateOnLanguageChange");
			setOnStartProp = serializedObject.FindProperty("setOnStart");
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var localizedText = (LocalizedText)target;
			
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Localized Text Component", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Automatically sets localized text on the TMP_Text component. " +
			                       "The text will update based on the localization key and current language.", 
			                       MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// Quick Actions (before tabs for convenience)
			DrawQuickActions(localizedText);
			
			EditorGUILayout.Space(10);
			
			// Tabs
			GUI.backgroundColor = Color.gray;
			selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
			GUI.backgroundColor = Color.white;
			
			EditorGUILayout.Space(10);
			
			switch (selectedTab)
			{
				case 0:
					DrawSettingsTab();
					break;
				case 1:
					DrawInfoTab();
					break;
			}
			
			serializedObject.ApplyModifiedProperties();
		}
		
		private void DrawQuickActions(LocalizedText localizedText)
		{
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			var textComponent = localizedText.GetComponent<TMP_Text>();
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Update Text Now", GUILayout.Height(30)))
			{
				localizedText.UpdateText();
				EditorUtility.SetDirty(textComponent);
			}
			
			if (GUILayout.Button("Clear Overrides", GUILayout.Height(30)))
			{
				localizedText.ClearOverrides();
				serializedObject.Update();
			}
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.EndVertical();
		}
		
		private void DrawSettingsTab()
		{
			// Localization Key
			EditorGUILayout.LabelField("Localization Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(localizationKeyProp, new GUIContent("Localization Key", "The key to retrieve localized text. Can also be source string if Hybrid Key is enabled."));
			
			if (string.IsNullOrEmpty(localizationKeyProp.stringValue))
			{
				EditorGUILayout.HelpBox("⚠️ No localization key set. Please enter a key to enable localization.", MessageType.Warning);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Optional Overrides
			EditorGUILayout.LabelField("Optional Overrides", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(languageCodeOverrideProp, new GUIContent("Language Override", "Override the language. Leave empty to use system language."));
			EditorGUILayout.PropertyField(textStyleOverrideProp, new GUIContent("Text Style Override", "Override the text style. Leave empty for automatic style."));
			
			EditorGUILayout.Space(5);
			EditorGUILayout.PropertyField(paragraphStyleProp, new GUIContent("Paragraph Style", "Optional paragraph style identifier (e.g., 'Header', 'Description', 'Body'). Leave empty for default style. You can use any custom string value."));
			
			if (!string.IsNullOrEmpty(paragraphStyleProp.stringValue))
			{
				EditorGUILayout.HelpBox($"This text will use TextStyle assets with paragraph style '{paragraphStyleProp.stringValue}' for the target language. " +
				                       "You can use any custom string - it doesn't need to match predefined values.", 
				                       MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("Empty paragraph style means this will use the default TextStyle for the language (one with empty paragraph style).", 
				                       MessageType.Info);
			}
			
			if (string.IsNullOrEmpty(languageCodeOverrideProp.stringValue) && textStyleOverrideProp.objectReferenceValue == null)
			{
				EditorGUILayout.HelpBox("✓ Using system defaults (current language and automatic text style).", MessageType.Info);
			}
			else
			{
				string overrideInfo = "";
				if (!string.IsNullOrEmpty(languageCodeOverrideProp.stringValue))
				{
					overrideInfo += $"Language: {languageCodeOverrideProp.stringValue}  ";
				}
				if (textStyleOverrideProp.objectReferenceValue != null)
				{
					overrideInfo += "TextStyle: Custom";
				}
				EditorGUILayout.HelpBox($"⚙️ Overrides active: {overrideInfo}", MessageType.Info);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Update Settings
			EditorGUILayout.LabelField("Update Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(setOnStartProp, new GUIContent("Set On Start", "Automatically set the text when the component starts."));
			EditorGUILayout.PropertyField(updateOnLanguageChangeProp, new GUIContent("Update On Language Change", "Automatically update when the system language changes."));
			
			EditorGUILayout.EndVertical();
		}
		
		private void DrawInfoTab()
		{
			// System Info
			EditorGUILayout.LabelField("System Information", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			SignaliaConfigAsset config = ConfigReader.GetConfig();
			if (config != null)
			{
				EditorGUILayout.LabelField($"Default Language: {config.LocalizationSystem.DefaultStartingLanguageCode}", EditorStyles.miniLabel);
				
				if (Application.isPlaying)
				{
					EditorGUILayout.LabelField($"Current Runtime Language: {LocalizationRuntime.CurrentLanguageCode}", EditorStyles.miniLabel);
				}
				else
				{
					EditorGUILayout.LabelField("Current Runtime Language: (Enter Play Mode to see)", EditorStyles.miniLabel);
				}
				
				EditorGUILayout.Space(5);
				EditorGUILayout.LabelField($"Language Change Event: {config.LocalizationSystem.LanguageChangedEvent}", EditorStyles.miniLabel);
				EditorGUILayout.LabelField($"Hybrid Key Mode: {(config.LocalizationSystem.HybridKey ? "Enabled" : "Disabled")}", EditorStyles.miniLabel);
				
				EditorGUILayout.Space(5);
				
				if (config.LocalizationSystem.LocBooks != null && config.LocalizationSystem.LocBooks.Length > 0)
				{
					int totalEntries = 0;
					foreach (var lb in config.LocalizationSystem.LocBooks)
					{
						if (lb != null)
							totalEntries += lb.EntryCount;
					}
					EditorGUILayout.LabelField($"LocBooks: {config.LocalizationSystem.LocBooks.Length}", EditorStyles.miniLabel);
					EditorGUILayout.LabelField($"Total Entries: {totalEntries}", EditorStyles.miniLabel);
				}
				else
				{
					EditorGUILayout.HelpBox("⚠️ No LocBooks assigned in config.", MessageType.Warning);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("⚠️ Signalia config not found.", MessageType.Warning);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Usage Help
			EditorGUILayout.LabelField("Usage Guide", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.HelpBox(
				"1. Set the Localization Key in the Settings tab\n" +
				"2. Configure optional overrides if needed\n" +
				"3. Use the Preview tab to test the localization\n" +
				"4. The text will auto-update when the language changes",
				MessageType.Info);
			
			EditorGUILayout.EndVertical();
		}
	}
	
	/// <summary>
	/// Custom editor for LocalizedImage components.
	/// </summary>
	[CustomEditor(typeof(LocalizedImage))]
	public class LocalizedImageEditor : Editor
	{
		private SerializedProperty spriteKeyProp;
		private SerializedProperty updateOnLanguageChangeProp;
		private SerializedProperty setOnStartProp;
		
		private void OnEnable()
		{
			spriteKeyProp = serializedObject.FindProperty("spriteKey");
			updateOnLanguageChangeProp = serializedObject.FindProperty("updateOnLanguageChange");
			setOnStartProp = serializedObject.FindProperty("setOnStart");
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var localizedImage = (LocalizedImage)target;
			
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Localized Image", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Automatically sets localized sprites on the UI Image component. " +
			                       "The sprite will update based on the key and current language.", 
			                       MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// Sprite Key
			EditorGUILayout.LabelField("Sprite Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(spriteKeyProp, new GUIContent("Sprite Key", "The localization key for the sprite (must match a sprite entry in LocBook)."));
			
			if (string.IsNullOrEmpty(spriteKeyProp.stringValue))
			{
				EditorGUILayout.HelpBox("No sprite key set. Please enter a key to enable localization.", MessageType.Warning);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Update Settings
			EditorGUILayout.LabelField("Update Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(setOnStartProp, new GUIContent("Set On Start", "Set the sprite when the component starts."));
			EditorGUILayout.PropertyField(updateOnLanguageChangeProp, new GUIContent("Update On Language Change", "Automatically update when the language changes."));
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Quick Action
			EditorGUILayout.LabelField("Quick Action", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			if (GUILayout.Button("Update Sprite Now", GUILayout.Height(30)))
			{
				localizedImage.UpdateSprite();
			}
			
			EditorGUILayout.EndVertical();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
	
	/// <summary>
	/// Custom editor for LocalizedAudioSource components.
	/// </summary>
	[CustomEditor(typeof(LocalizedAudioSource))]
	public class LocalizedAudioSourceEditor : Editor
	{
		private SerializedProperty audioKeyProp;
		private SerializedProperty updateOnLanguageChangeProp;
		private SerializedProperty setOnStartProp;
		private SerializedProperty playOnUpdateProp;
		private SerializedProperty stopBeforeUpdateProp;
		
		private void OnEnable()
		{
			audioKeyProp = serializedObject.FindProperty("audioKey");
			updateOnLanguageChangeProp = serializedObject.FindProperty("updateOnLanguageChange");
			setOnStartProp = serializedObject.FindProperty("setOnStart");
			playOnUpdateProp = serializedObject.FindProperty("playOnUpdate");
			stopBeforeUpdateProp = serializedObject.FindProperty("stopBeforeUpdate");
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var localizedAudio = (LocalizedAudioSource)target;
			
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Localized Audio Source", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Automatically sets localized audio clips on the AudioSource component. " +
			                       "Perfect for localized voice-overs and narration.", 
			                       MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// Audio Key
			EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(audioKeyProp, new GUIContent("Audio Key", "The localization key for the audio clip (must match an audio entry in LocBook)."));
			
			if (string.IsNullOrEmpty(audioKeyProp.stringValue))
			{
				EditorGUILayout.HelpBox("No audio key set. Please enter a key to enable localization.", MessageType.Warning);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Update Settings
			EditorGUILayout.LabelField("Update Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(setOnStartProp, new GUIContent("Set On Start", "Set the audio clip when the component starts."));
			EditorGUILayout.PropertyField(updateOnLanguageChangeProp, new GUIContent("Update On Language Change", "Automatically update when the language changes."));
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Playback Settings
			EditorGUILayout.LabelField("Playback Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(playOnUpdateProp, new GUIContent("Play On Update", "Automatically play the clip after updating."));
			EditorGUILayout.PropertyField(stopBeforeUpdateProp, new GUIContent("Stop Before Update", "Stop the current audio before switching clips."));
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Quick Actions
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Update Clip Now", GUILayout.Height(30)))
			{
				localizedAudio.UpdateAudioClip();
			}
			
			if (GUILayout.Button("Update & Play", GUILayout.Height(30)))
			{
				localizedAudio.UpdateAndPlay();
			}
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.EndVertical();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
	
	/// <summary>
	/// Custom editor for LocalizationRefresher components.
	/// </summary>
	[CustomEditor(typeof(LocalizationRefresher))]
	public class LocalizationRefresherEditor : Editor
	{
		private SerializedProperty refreshTimingProp;
		
		private void OnEnable()
		{
			refreshTimingProp = serializedObject.FindProperty("refreshTiming");
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var refresher = (LocalizationRefresher)target;
			
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Localization Refresher", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Manually triggers localization refresh events. " +
			                       "Forces all localized components to update their content.", 
			                       MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// Timing Settings
			EditorGUILayout.LabelField("Automatic Refresh", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(refreshTimingProp, new GUIContent("Refresh Timing", "When to automatically trigger a refresh."));
			
			var timing = (LocalizationRefresher.RefreshTiming)refreshTimingProp.enumValueIndex;
			if (timing == LocalizationRefresher.RefreshTiming.None)
			{
				EditorGUILayout.HelpBox("Manual mode. Call Refresh() from code or UnityEvents.", MessageType.Info);
			}
			else
			{
				string timingText = timing == LocalizationRefresher.RefreshTiming.OnAwake ? "Awake" : "Start";
				EditorGUILayout.HelpBox($"Will automatically trigger refresh on {timingText}.", MessageType.Info);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Quick Actions
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			if (GUILayout.Button("Refresh Now", GUILayout.Height(30)))
			{
				if (Application.isPlaying)
				{
					refresher.Refresh();
				}
				else
				{
					EditorUtility.DisplayDialog("Not in Play Mode", 
					                           "Refresh only works in Play Mode.", 
					                           "OK");
				}
			}
			
			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Enter Play Mode to test refresh functionality.", MessageType.Info);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Available Methods
			EditorGUILayout.LabelField("Public Methods (for UnityEvents)", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Refresh() - Trigger immediate refresh", EditorStyles.miniLabel);
			EditorGUILayout.LabelField("RefreshDelayed(float) - Refresh after delay", EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
