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
	/// Custom editor for SimpleLocalizedText components.
	/// Provides a minimal, clean interface for quick localization.
	/// </summary>
	[CustomEditor(typeof(SimpleLocalizedText))]
	public class SimpleLocalizedTextEditor : Editor
	{
		private SerializedProperty keyProp;
		private SerializedProperty paragraphStyleProp;
		
		private void OnEnable()
		{
			keyProp = serializedObject.FindProperty("key");
			paragraphStyleProp = serializedObject.FindProperty("paragraphStyle");
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var simpleLocalizedText = (SimpleLocalizedText)target;
			
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Simple Localized Text", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("A streamlined localization component that only requires a key. " +
			                       "Perfect for quick localization without complex settings.", 
			                       MessageType.Info);
			
			EditorGUILayout.Space(10);
			
			// Main Key Field
			EditorGUILayout.LabelField("Localization Key", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			EditorGUILayout.PropertyField(keyProp, new GUIContent("Key", "The localization key to retrieve the text."));
			
			EditorGUILayout.Space(5);
			EditorGUILayout.PropertyField(paragraphStyleProp, new GUIContent("Paragraph Style", "Optional paragraph style identifier (e.g., 'Header', 'Description', 'Body'). Leave empty for default style. You can use any custom string value."));
			
			if (!string.IsNullOrEmpty(paragraphStyleProp.stringValue))
			{
				EditorGUILayout.HelpBox($"This text will use TextStyle assets with paragraph style '{paragraphStyleProp.stringValue}' for the current language. " +
				                       "You can use any custom string - it doesn't need to match predefined values.", 
				                       MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("Empty paragraph style means this will use the default TextStyle for the current language (one with empty paragraph style).", 
				                       MessageType.Info);
			}
			
			if (string.IsNullOrEmpty(keyProp.stringValue))
			{
				EditorGUILayout.HelpBox("No localization key set. Please enter a key to enable localization.", MessageType.Warning);
			}
			else
			{
				EditorGUILayout.HelpBox("Key configured. Text will auto-update on language change.", MessageType.Info);
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// Quick Action
			EditorGUILayout.LabelField("Quick Action", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			if (GUILayout.Button("Update Text Now", GUILayout.Height(30)))
			{
				simpleLocalizedText.UpdateText();
				var textComponent = simpleLocalizedText.GetComponent<TMP_Text>();
				if (textComponent != null)
				{
					EditorUtility.SetDirty(textComponent);
				}
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space(10);
			
			// System Info
			EditorGUILayout.LabelField("Current Language", EditorStyles.boldLabel);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			
			if (Application.isPlaying)
			{
				EditorGUILayout.LabelField($"Runtime Language: {LocalizationRuntime.CurrentLanguageCode}", EditorStyles.miniLabel);
			}
			else
			{
				SignaliaConfigAsset config = ConfigReader.GetConfig();
				if (config != null)
				{
					EditorGUILayout.LabelField($"Default Language: {config.LocalizationSystem.DefaultStartingLanguageCode}", EditorStyles.miniLabel);
				}
				EditorGUILayout.LabelField("(Enter Play Mode to see runtime language)", EditorStyles.miniLabel);
			}
			
			EditorGUILayout.EndVertical();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
