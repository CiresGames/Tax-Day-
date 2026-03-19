using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.Framework.Editors
{
    public partial class FrameworkSettings : EditorWindow
    {
        #region Tabs - Time

        private void DrawTimeTab()
        {
            DrawSignaliaTimeSection();
        }

        private void DrawSignaliaTimeSection()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Signalia Time System:", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "SignaliaTime tracks scaled time in the scene. Time modifiers (0-1) are multiplied together to produce the final time scale. " +
                "UIViews can automatically apply time modifiers when visible based on the configuration below.", 
                MessageType.Info);

            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            config.SignaliaTime.AutoAddSignaliaTime = EditorGUILayout.Toggle(
                new GUIContent("Auto-Add SignaliaTime", 
                    "Automatically add the SignaliaTime component to the scene when Watchman initializes."), 
                config.SignaliaTime.AutoAddSignaliaTime);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("UIView Time Modifiers:", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "Configure UIViews that should modify time when visible. When a view with a matching Menu Name becomes visible, " +
                "its time modifier is applied. When hidden, the modifier is removed.\n\n" +
                "Example: Add your pause menu's Menu Name here with 'Pause Time' enabled to pause the game when the menu opens.", 
                MessageType.Info);

            if (config.SignaliaTime.UIViewTimeModifiers == null)
            {
                config.SignaliaTime.UIViewTimeModifiers = new UIViewTimeModifier[0];
                EditorUtility.SetDirty(config);
            }

            int modifierCount = config.SignaliaTime.UIViewTimeModifiers.Length;
            EditorGUILayout.LabelField($"Configured View Modifiers: {modifierCount}", EditorStyles.miniBoldLabel);

            // Draw existing modifiers
            for (int i = 0; i < config.SignaliaTime.UIViewTimeModifiers.Length; i++)
            {
                var modifier = config.SignaliaTime.UIViewTimeModifiers[i];
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Modifier {i + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Remove", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    var newList = new System.Collections.Generic.List<UIViewTimeModifier>(config.SignaliaTime.UIViewTimeModifiers);
                    if (i >= 0 && i < newList.Count)
                    {
                        newList.RemoveAt(i);
                        config.SignaliaTime.UIViewTimeModifiers = newList.ToArray();
                        EditorUtility.SetDirty(config);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();

                modifier.ViewName = EditorGUILayout.TextField(
                    new GUIContent("View Name", "The Menu Name of the UIView (must match exactly)."), 
                    modifier.ViewName);

                modifier.PauseTime = EditorGUILayout.Toggle(
                    new GUIContent("Pause Time", "If enabled, completely pauses time (modifier value = 0)."), 
                    modifier.PauseTime);

                EditorGUI.BeginDisabledGroup(modifier.PauseTime);
                modifier.TimeModifierValue = EditorGUILayout.Slider(
                    new GUIContent("Time Modifier", "Time scale modifier (0 = paused, 1 = normal). Only used if 'Pause Time' is disabled."), 
                    modifier.TimeModifierValue, 0f, 1f);
                EditorGUI.EndDisabledGroup();

                modifier.Description = EditorGUILayout.TextField(
                    new GUIContent("Description", "Optional description for debugging."), 
                    modifier.Description);

                // Show effective value
                EditorGUILayout.LabelField($"Effective Value: {modifier.EffectiveValue:F2}", EditorStyles.miniLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    config.SignaliaTime.UIViewTimeModifiers[i] = modifier;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add View Modifier", GUILayout.Height(22)))
            {
                var newList = new System.Collections.Generic.List<UIViewTimeModifier>(config.SignaliaTime.UIViewTimeModifiers ?? System.Array.Empty<UIViewTimeModifier>());
                newList.Add(new UIViewTimeModifier { ViewName = "", PauseTime = true, TimeModifierValue = 1f, Description = "" });
                config.SignaliaTime.UIViewTimeModifiers = newList.ToArray();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Clear All", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Clear All View Modifiers", 
                    "Are you sure you want to remove all UIView time modifiers?", 
                    "Yes", "Cancel"))
                {
                    config.SignaliaTime.UIViewTimeModifiers = System.Array.Empty<UIViewTimeModifier>();
                    EditorUtility.SetDirty(config);
                }
            }
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0;
        }

        #endregion
    }
}
