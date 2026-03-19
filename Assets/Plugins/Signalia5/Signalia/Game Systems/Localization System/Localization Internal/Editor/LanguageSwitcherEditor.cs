#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.Localization.Internal;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
	/// <summary>
	/// Custom editor for LanguageSwitcher components.
	/// Provides a clean interface for configuring language switching behavior.
	/// </summary>
	[CustomEditor(typeof(LanguageSwitcher))]
	public class LanguageSwitcherEditor : Editor
	{
		private SerializedProperty languageCodeProp;
		private SerializedProperty switchTimingProp;
		private SerializedProperty savePreferenceProp;
		
		private void OnEnable()
		{
			languageCodeProp = serializedObject.FindProperty("languageCode");
			switchTimingProp = serializedObject.FindProperty("switchTiming");
			savePreferenceProp = serializedObject.FindProperty("savePreference");
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var languageSwitcher = (LanguageSwitcher)target;
			
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Language Switcher", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Switches the game's language without coding. " +
			                       "Can be configured to switch automatically or called from UnityEvents.", 
			                       MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// Language Settings
			EditorGUILayout.LabelField("Language Settings", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(languageCodeProp, new GUIContent("Language Code", "The language code to switch to (e.g., 'en', 'es', 'fr', 'ar')."));
			
			if (string.IsNullOrEmpty(languageCodeProp.stringValue))
			{
				EditorGUILayout.HelpBox("No language code set. Please enter a valid language code.", MessageType.Warning);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Timing Settings
			EditorGUILayout.LabelField("Automatic Switch", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(switchTimingProp, new GUIContent("Switch Timing", "When to automatically switch the language."));
			
			var timing = (LanguageSwitcher.SwitchTiming)switchTimingProp.enumValueIndex;
			if (timing == LanguageSwitcher.SwitchTiming.None)
			{
				EditorGUILayout.HelpBox("Manual mode. Call SwitchLanguage() from code or UnityEvents.", MessageType.Info);
			}
			else
			{
				string timingText = timing == LanguageSwitcher.SwitchTiming.OnAwake ? "Awake" : "Start";
				EditorGUILayout.HelpBox($"Will automatically switch to '{languageCodeProp.stringValue}' on {timingText}.", MessageType.Info);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Save Settings
			EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(savePreferenceProp, new GUIContent("Save Preference", "Whether to save the language choice for future sessions."));
			
			if (savePreferenceProp.boolValue)
			{
				EditorGUILayout.HelpBox("Language preference will be saved and persist across game sessions.", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("Language will not be saved. It will reset on next game launch.", MessageType.Warning);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Quick Actions
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Switch Language Now", GUILayout.Height(30)))
			{
				if (Application.isPlaying)
				{
					languageSwitcher.SwitchLanguage();
				}
				else
				{
					EditorUtility.DisplayDialog("Not in Play Mode", 
					                           "Language switching only works in Play Mode.", 
					                           "OK");
				}
			}
			
			if (GUILayout.Button("Reset to Default", GUILayout.Height(30)))
			{
				if (Application.isPlaying)
				{
					languageSwitcher.ResetToDefault();
				}
				else
				{
					EditorUtility.DisplayDialog("Not in Play Mode", 
					                           "Language switching only works in Play Mode.", 
					                           "OK");
				}
			}
			
			EditorGUILayout.EndHorizontal();
			
			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Enter Play Mode to test language switching.", MessageType.Info);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Available Methods
			EditorGUILayout.LabelField("Public Methods (for UnityEvents)", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("SwitchLanguage() - Switch to configured language", EditorStyles.miniLabel);
			EditorGUILayout.LabelField("SwitchToLanguage(string) - Switch to specific language", EditorStyles.miniLabel);
			EditorGUILayout.LabelField("ResetToDefault() - Reset to default language", EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
