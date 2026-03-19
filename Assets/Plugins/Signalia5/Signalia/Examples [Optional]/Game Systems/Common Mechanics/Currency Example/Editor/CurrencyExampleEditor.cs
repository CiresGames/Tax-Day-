#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.CommonMechanics.Examples;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Editors
{
    [CustomEditor(typeof(CurrencyExample))]
    public class CurrencyExampleEditor : Editor
    {
        private SerializedProperty currencyNameProp;
        private SerializedProperty testAmountProp;

        private void OnEnable()
        {
            currencyNameProp = serializedObject.FindProperty("currencyName");
            testAmountProp = serializedObject.FindProperty("testAmount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("This script demonstrates how to use the Signalia Currency System.", MessageType.Info);

            DrawSettingsSection();
            DrawActionsSection();
            DrawCodeExamplesSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("⚙️ Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(currencyNameProp, new GUIContent("Currency Name"));
            EditorGUILayout.PropertyField(testAmountProp, new GUIContent("Test Amount"));
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("🎮 Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            CurrencyExample example = (CurrencyExample)target;

            // Basic Operations
            EditorGUILayout.LabelField("Basic Operations", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Add 50 {currencyNameProp.stringValue}", GUILayout.Height(25))) example.AddGold();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button($"Remove 25 {currencyNameProp.stringValue}", GUILayout.Height(25))) example.RemoveGold();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.blue;
            if (GUILayout.Button($"Set {currencyNameProp.stringValue} to 1000", GUILayout.Height(25))) example.SetGoldTo1000();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button($"Show Current {currencyNameProp.stringValue}", GUILayout.Height(25))) example.ShowCurrentGold();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Custom Amount Operations
            EditorGUILayout.LabelField("Custom Amount Operations", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Add {testAmountProp.floatValue} {currencyNameProp.stringValue}", GUILayout.Height(25))) example.AddCustomAmount();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button($"Remove {testAmountProp.floatValue} {currencyNameProp.stringValue}", GUILayout.Height(25))) example.RemoveCustomAmount();
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Actions are only available in Play Mode.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawCodeExamplesSection()
        {
            EditorGUILayout.LabelField("📖 Code Examples", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Basic Usage:", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox($"var {currencyNameProp.stringValue} = SIGS.GetCurrency(\"{currencyNameProp.stringValue}\");\n{currencyNameProp.stringValue}.Modify(100);", MessageType.None);

            EditorGUILayout.LabelField("Event Listening:", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox($"private Listener {currencyNameProp.stringValue}UpdateListener;\n\nvoid Start() {{\n    {currencyNameProp.stringValue}UpdateListener = SIGS.Listener(\"{currencyNameProp.stringValue}sigs_u_pickedup\", On{currencyNameProp.stringValue}Updated);\n}}\n\nvoid OnDestroy() {{\n    {currencyNameProp.stringValue}UpdateListener?.Dispose();\n}}\n\nvoid On{currencyNameProp.stringValue}Updated(object newValue) {{\n    if (newValue is float value) Debug.Log($\"{currencyNameProp.stringValue}: {{value}}\");\n}}", MessageType.None);

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }
}
#endif