#if UNITY_EDITOR
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Entities;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    /// <summary>
    /// Custom editor for EntityComponent base class.
    /// Provides a consistent look for all EntityAction and EntityCondition components.
    /// </summary>
    [CustomEditor(typeof(EntityComponent), true), CanEditMultipleObjects]
    public class EntityComponentEditor : Editor
    {
        private SerializedProperty nameProp;
        private SerializedProperty descriptionProp;
        private bool showComponentInfo = true;

        private void OnEnable()
        {
            try
            {
                if (targets == null || targets.Length == 0 || targets[0] == null)
                    return;

                nameProp = serializedObject.FindProperty("_name");
                descriptionProp = serializedObject.FindProperty("_description");
            }
            catch (System.Exception)
            {
                // Target is null or destroyed, ignore
            }
        }

        public override void OnInspectorGUI()
        {
            try
            {
                if (targets == null || targets.Length == 0 || targets[0] == null)
                    return;
            }
            catch (System.Exception)
            {
                return;
            }

            serializedObject.Update();

            // Draw main info in helpBox
            DrawMainInfoBox();

            GUILayout.Space(5);

            // Draw separator
            DrawSeparator();

            GUILayout.Space(5);

            // Draw remaining properties
            DrawRemainingProperties();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainInfoBox()
        {
            // Foldout header
            showComponentInfo = EditorGUILayout.Foldout(showComponentInfo, "Component Information", true, EditorStyles.foldoutHeader);
            
            if (!showComponentInfo)
                return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            // Name field
            if (nameProp != null)
            {
                EditorGUILayout.PropertyField(nameProp, new GUIContent("Name", "Display name for this component."));
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EntityComponent component = (EntityComponent)target;
                EditorGUILayout.TextField("Name", component != null ? component.Name : "N/A");
                EditorGUI.EndDisabledGroup();
            }

            // Description field
            if (descriptionProp != null)
            {
                EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Description", "Description of what this component does."));
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EntityComponent component = (EntityComponent)target;
                EditorGUILayout.TextField("Description", component != null ? component.Description : "N/A");
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(5);

            // Read-only info fields
            EditorGUI.BeginDisabledGroup(true);
            EntityComponent comp = (EntityComponent)target;
            if (comp != null)
            {
                EditorGUILayout.Toggle("Is Action", comp.IsAction);
                if (Application.isPlaying)
                {
                    EditorGUILayout.Toggle("Condition Function", comp.ConditionFunction);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            GUILayout.Space(2);
            
            // Draw a horizontal line
            Rect rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            GUILayout.Space(2);
            
            // Add a label to indicate implementation section
            EditorGUILayout.LabelField("Implementation", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawRemainingProperties()
        {
            // Draw all remaining properties that aren't already drawn
            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;

            // Move to first property
            if (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                
                do
                {
                    // Skip properties we've already drawn and the script reference
                    if (prop.name == "_name" || prop.name == "_description" || prop.name == "m_Script")
                    {
                        continue;
                    }

                    EditorGUILayout.PropertyField(prop, true);
                }
                while (prop.NextVisible(enterChildren));
            }
        }
    }
}
#endif
