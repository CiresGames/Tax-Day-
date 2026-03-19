#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.Inventory.Data;

namespace AHAKuo.Signalia.GameSystems.Inventory.Editors
{
    /// <summary>
    /// Property drawer for CustomProperty to provide a clean inspector interface
    /// for defining custom item properties like price, rarity, health restoration, etc.
    /// </summary>
    [CustomPropertyDrawer(typeof(ItemSO.CustomProperty))]
    public class ItemCustomPropertyDrawer : PropertyDrawer
    {
        private const float SPACING = 4f;
        private const float LABEL_WIDTH = 45f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get properties
            SerializedProperty propertyNameProp = property.FindPropertyRelative("propertyName");
            SerializedProperty propertyTypeProp = property.FindPropertyRelative("propertyType");
            SerializedProperty intValueProp = property.FindPropertyRelative("intValue");
            SerializedProperty floatValueProp = property.FindPropertyRelative("floatValue");
            SerializedProperty stringValueProp = property.FindPropertyRelative("stringValue");

            // Calculate dimensions
            float currentX = position.x;
            float availableWidth = position.width;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Calculate widths for each section: Name (40%), Type (25%), Value (35%)
            float nameFieldWidth = availableWidth * 0.40f - LABEL_WIDTH - SPACING;
            float typeFieldWidth = availableWidth * 0.25f - LABEL_WIDTH - SPACING;
            float valueFieldWidth = availableWidth * 0.35f - LABEL_WIDTH;

            // === NAME SECTION ===
            // Draw "Name:" label
            Rect nameLabelRect = new Rect(currentX, position.y, LABEL_WIDTH, lineHeight);
            EditorGUI.LabelField(nameLabelRect, "Name:", EditorStyles.miniLabel);
            currentX += LABEL_WIDTH;

            // Draw name field
            Rect nameRect = new Rect(currentX, position.y, nameFieldWidth, lineHeight);
            EditorGUI.PropertyField(nameRect, propertyNameProp, GUIContent.none);
            currentX += nameFieldWidth + SPACING;

            // === TYPE SECTION ===
            // Draw "Type:" label
            Rect typeLabelRect = new Rect(currentX, position.y, LABEL_WIDTH, lineHeight);
            EditorGUI.LabelField(typeLabelRect, "Type:", EditorStyles.miniLabel);
            currentX += LABEL_WIDTH;

            // Draw type field
            Rect typeRect = new Rect(currentX, position.y, typeFieldWidth, lineHeight);
            EditorGUI.PropertyField(typeRect, propertyTypeProp, GUIContent.none);
            currentX += typeFieldWidth + SPACING;

            // === VALUE SECTION ===
            // Draw "Value:" label
            Rect valueLabelRect = new Rect(currentX, position.y, LABEL_WIDTH, lineHeight);
            EditorGUI.LabelField(valueLabelRect, "Value:", EditorStyles.miniLabel);
            currentX += LABEL_WIDTH;

            // Draw value field based on type
            Rect valueRect = new Rect(currentX, position.y, valueFieldWidth, lineHeight);
            ItemSO.PropertyType selectedType = (ItemSO.PropertyType)propertyTypeProp.enumValueIndex;

            switch (selectedType)
            {
                case ItemSO.PropertyType.Int:
                    EditorGUI.PropertyField(valueRect, intValueProp, GUIContent.none);
                    break;

                case ItemSO.PropertyType.Float:
                    EditorGUI.PropertyField(valueRect, floatValueProp, GUIContent.none);
                    break;

                case ItemSO.PropertyType.String:
                    EditorGUI.PropertyField(valueRect, stringValueProp, GUIContent.none);
                    break;
            }

            // Add tooltip showing the property information
            string propertyDisplayName = string.IsNullOrEmpty(propertyNameProp.stringValue) 
                ? "[Unnamed Property]" 
                : propertyNameProp.stringValue;

            string tooltipText = $"Property: {propertyDisplayName}\nType: {selectedType}";
            EditorGUI.LabelField(position, new GUIContent("", tooltipText));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
