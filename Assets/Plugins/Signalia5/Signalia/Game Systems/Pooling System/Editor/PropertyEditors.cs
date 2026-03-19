using UnityEngine;
using UnityEditor;

namespace AHAKuo.Signalia.GameSystems.PoolingSystem.Editors
{
    /// <summary>
    /// Property drawer for SpawnableObject to provide a clean inspector interface
    /// </summary>
    [CustomPropertyDrawer(typeof(PoolingSpawner.SpawnableObject))]
    public class SpawnableObjectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var prefabProp = property.FindPropertyRelative("prefab");
            var chanceProp = property.FindPropertyRelative("spawnChance");

            // Calculate rects with proper spacing
            float labelWidth = 60f;
            float spacing = 5f;
            float prefabWidth = position.width * 0.6f - spacing;
            float chanceWidth = position.width * 0.4f - labelWidth - spacing;

            var prefabRect = new Rect(position.x, position.y, prefabWidth, EditorGUIUtility.singleLineHeight);
            var chanceLabelRect = new Rect(position.x + prefabWidth + spacing, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
            var chanceRect = new Rect(position.x + prefabWidth + labelWidth + spacing, position.y, chanceWidth, EditorGUIUtility.singleLineHeight);

            // Draw fields with labels
            EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);
            
            // Chance label and field with slider
            EditorGUI.LabelField(chanceLabelRect, "Chance:", EditorStyles.miniLabel);
            chanceProp.floatValue = EditorGUI.Slider(chanceRect, chanceProp.floatValue, 0f, 1f);

            // Add tooltips
            if (prefabProp.objectReferenceValue != null)
            {
                var tooltipRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(tooltipRect, new GUIContent("", $"Prefab: {prefabProp.objectReferenceValue.name}\nChance: {chanceProp.floatValue}"));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
