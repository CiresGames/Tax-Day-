#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using UnityEditor;
using UnityEngine;
using TMPro;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Editors
{
    [CustomEditor(typeof(CurrencyHelper)), CanEditMultipleObjects]
    public class CurrencyHelperEditor : Editor
    {
        private SerializedProperty currencyNameProp;
        private SerializedProperty targetTextProp;
        private SerializedProperty listenForUpdatesProp;
        private SerializedProperty displayFormatProp;
        private SerializedProperty useLocalizationProp;
        private SerializedProperty localizationPrefixProp;
        private SerializedProperty useCommaFormattingProp;
        private SerializedProperty increaseAudioKeyProp;
        private SerializedProperty decreaseAudioKeyProp;
        private SerializedProperty playHapticsOnIncreaseProp;
        private SerializedProperty playHapticsOnDecreaseProp;
        private SerializedProperty increaseHapticTypeProp;
        private SerializedProperty decreaseHapticTypeProp;
        private SerializedProperty increaseAnimationProp;
        private SerializedProperty decreaseAnimationProp;
        private SerializedProperty decimalPlacesProp;
        private SerializedProperty alwaysShowDecimalsProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Main", "Display", "Audio", "Haptics", "Animations" };

        private void OnEnable()
        {
            currencyNameProp = serializedObject.FindProperty("currencyName");
            targetTextProp = serializedObject.FindProperty("targetText");
            listenForUpdatesProp = serializedObject.FindProperty("listenForUpdates");
            displayFormatProp = serializedObject.FindProperty("displayFormat");
            useLocalizationProp = serializedObject.FindProperty("useLocalization");
            localizationPrefixProp = serializedObject.FindProperty("localizationPrefix");
            useCommaFormattingProp = serializedObject.FindProperty("useCommaFormatting");
            increaseAudioKeyProp = serializedObject.FindProperty("increaseAudioKey");
            decreaseAudioKeyProp = serializedObject.FindProperty("decreaseAudioKey");
            playHapticsOnIncreaseProp = serializedObject.FindProperty("playHapticsOnIncrease");
            playHapticsOnDecreaseProp = serializedObject.FindProperty("playHapticsOnDecrease");
            increaseHapticTypeProp = serializedObject.FindProperty("increaseHapticType");
            decreaseHapticTypeProp = serializedObject.FindProperty("decreaseHapticType");
            increaseAnimationProp = serializedObject.FindProperty("increaseAnimation");
            decreaseAnimationProp = serializedObject.FindProperty("decreaseAnimation");
            decimalPlacesProp = serializedObject.FindProperty("decimalPlaces");
            alwaysShowDecimalsProp = serializedObject.FindProperty("alwaysShowDecimals");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = SSColors.Gold },
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.HelpBox("Automatically updates TMPText fields with currency values and supports audio/haptic feedback for currency changes.", MessageType.Info);
            GUILayout.Space(5);

            // Tab Selection
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            // Tab Content
            switch (selectedTab)
            {
                case 0: DrawMainSettings(); break;
                case 1: DrawDisplaySettings(); break;
                case 2: DrawAudioSettings(); break;
                case 3: DrawHapticsSettings(); break;
                case 4: DrawAnimationsSettings(); break;
                //case 5: DrawActionsTab(); break;
            }

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainSettings()
        {
            EditorGUILayout.LabelField("💰 Currency Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(currencyNameProp, new GUIContent("Currency Name", "The name of the currency to track (e.g., 'gold', 'coins', 'gems')"));
            
            if (string.IsNullOrEmpty(currencyNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Currency Name is required. This should match the currency name used in your game.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(targetTextProp, new GUIContent("Target Text", "The TextMeshProUGUI component to update with currency values"));
            
            if (targetTextProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Target Text is not assigned. The component will try to find a TextMeshProUGUI on this GameObject.", MessageType.Warning);
                
                if (GUILayout.Button("Auto-Find TextMeshProUGUI"))
                {
                    var helper = (CurrencyHelper)target;
                    var tmpText = helper.GetComponent<TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        targetTextProp.objectReferenceValue = tmpText;
                        EditorUtility.SetDirty(helper);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No TextMeshProUGUI Found", 
                            "No TextMeshProUGUI component found on this GameObject. Please assign one manually or add a TextMeshProUGUI component.", 
                            "OK");
                    }
                }
            }

            EditorGUILayout.PropertyField(listenForUpdatesProp, new GUIContent("Listen for Updates", "Automatically update display when currency value changes"));

            EditorGUILayout.EndVertical();
        }

        private void DrawDisplaySettings()
        {
            EditorGUILayout.LabelField("📱 Display Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(displayFormatProp, new GUIContent("Display Format", "Format string for displaying the currency value (e.g., '{0}' for just the number, 'Gold: {0}' for 'Gold: 100')"));
            
            if (string.IsNullOrEmpty(displayFormatProp.stringValue))
            {
                EditorGUILayout.HelpBox("Display Format is empty. Using default format '{0}'.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(useCommaFormattingProp, new GUIContent("Use Comma Formatting", "Add commas to numbers for better readability (e.g., 1,000 instead of 1000)"));

            EditorGUILayout.PropertyField(decimalPlacesProp, new GUIContent("Decimal Places", "Number of decimal places to show (0 = no decimals)"));
            
            if (decimalPlacesProp.intValue > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(alwaysShowDecimalsProp, new GUIContent("Always Show Decimals", "Always show decimal places even when they are zero (e.g., 100.00 vs 100)"));
                EditorGUI.indentLevel--;
            }

            // Disable localization with work in progress note
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(useLocalizationProp, new GUIContent("Use Localization", "Whether to use localization for the currency display (Work in Progress)"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.HelpBox("Localization support is currently work in progress and will be available in a future update.", MessageType.Info);

            if (useLocalizationProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(localizationPrefixProp, new GUIContent("Localization Prefix", "Prefix for localization key (e.g., 'currency_' for 'currency_gold')"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAudioSettings()
        {
            EditorGUILayout.LabelField("🔊 Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorHelpers.DrawAudioDropdown("Increase Audio", increaseAudioKeyProp, serializedObject);
            if (string.IsNullOrEmpty(increaseAudioKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox("Assign an audio key to play a sound when the currency increases.", MessageType.Info);
            }

            EditorHelpers.DrawAudioDropdown("Decrease Audio", decreaseAudioKeyProp, serializedObject);
            if (string.IsNullOrEmpty(decreaseAudioKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox("Assign an audio key to play a sound when the currency decreases.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHapticsSettings()
        {
            EditorGUILayout.LabelField("⚡ Haptic Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(playHapticsOnIncreaseProp, new GUIContent("Play Haptics on Increase", "Play haptic feedback when currency value increases"));
            
            if (playHapticsOnIncreaseProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(increaseHapticTypeProp, new GUIContent("Increase Haptic Type", "Type of haptic feedback for currency increase"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(playHapticsOnDecreaseProp, new GUIContent("Play Haptics on Decrease", "Play haptic feedback when currency value decreases"));
            
            if (playHapticsOnDecreaseProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(decreaseHapticTypeProp, new GUIContent("Decrease Haptic Type", "Type of haptic feedback for currency decrease"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationsSettings()
        {
            EditorGUILayout.LabelField("🎬 Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(increaseAnimationProp, new GUIContent("Increase Animation", "Animation to play when currency value increases"));
            
            if (increaseAnimationProp.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("This animation will play on the GameObject when currency increases.", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(decreaseAnimationProp, new GUIContent("Decrease Animation", "Animation to play when currency value decreases"));
            
            if (decreaseAnimationProp.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("This animation will play on the GameObject when currency decreases.", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            if (increaseAnimationProp.objectReferenceValue == null && decreaseAnimationProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No animations assigned. Animations will play on the GameObject when currency values change.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionsTab()
        {
            EditorGUILayout.LabelField("🎮 Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var helper = (CurrencyHelper)target;

            // Runtime Controls
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Refresh Display", GUILayout.Height(25)))
                {
                    helper.RefreshDisplay();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Get Current Value", GUILayout.Height(25)))
                {
                    float value = helper.GetCurrentValue();
                    Debug.Log($"Current {currencyNameProp.stringValue} value: {value}");
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Test Currency Modification", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("+10", GUILayout.Height(25)))
                {
                    helper.ModifyCurrency(10f);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("-10", GUILayout.Height(25)))
                {
                    helper.ModifyCurrency(-10f);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Set to 100", GUILayout.Height(25)))
                {
                    helper.SetCurrencyValue(100f);
                }
                GUI.backgroundColor = Color.magenta;
                if (GUILayout.Button("Set to 0", GUILayout.Height(25)))
                {
                    helper.SetCurrencyValue(0f);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("🛠️ Editor Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.blue;
                if (GUILayout.Button("Initialize Helper", GUILayout.Height(25)))
                {
                    helper.Initialize();
                    EditorUtility.SetDirty(helper);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("Actions are available in Play Mode. Enter Play Mode to test currency modifications.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
