#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.Movement;

namespace AHAKuo.Signalia.GameSystems.Movement.Editors
{
    /// <summary>
    /// Custom property drawer for MovementModifierEvent to provide a clean inspector interface.
    /// </summary>
    [CustomPropertyDrawer(typeof(MovementModifierEvent))]
    public class MovementModifierEventDrawer : PropertyDrawer
    {
        private const float SPACING = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get all properties
            SerializedProperty eventNameProp = property.FindPropertyRelative("eventName");
            SerializedProperty velocityThresholdProp = property.FindPropertyRelative("velocityThreshold");
            SerializedProperty requiredStateProp = property.FindPropertyRelative("requiredState");
            SerializedProperty onEnterProp = property.FindPropertyRelative("onEnter");
            SerializedProperty onExitProp = property.FindPropertyRelative("onExit");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float currentY = position.y;
            float indentOffset = EditorGUI.indentLevel * 15f;
            float fieldWidth = position.width - indentOffset;

            // Draw foldout header
            Rect foldoutRect = new Rect(position.x, currentY, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GetEventLabel(eventNameProp), true);
            currentY += lineHeight + SPACING;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Event Name
                Rect nameRect = new Rect(position.x + indentOffset, currentY, fieldWidth, lineHeight);
                EditorGUI.PropertyField(nameRect, eventNameProp, new GUIContent("Event Name", "Name/identifier for this event (for organization)."));
                currentY += lineHeight + SPACING;

                // Velocity Threshold and Required State on same line
                float halfWidth = (fieldWidth - SPACING) * 0.5f;
                Rect velocityRect = new Rect(position.x + indentOffset, currentY, halfWidth, lineHeight);
                Rect stateRect = new Rect(position.x + indentOffset + halfWidth + SPACING, currentY, halfWidth, lineHeight);

                EditorGUI.PropertyField(velocityRect, velocityThresholdProp, new GUIContent("Velocity Threshold", "Minimum horizontal velocity magnitude (units/s) required to trigger this event."));
                EditorGUI.PropertyField(stateRect, requiredStateProp, new GUIContent("Required State", "The movement state required for this event to trigger."));
                currentY += lineHeight + SPACING;

                // Validation warning for velocity threshold
                if (velocityThresholdProp.floatValue < 0f)
                {
                    Rect warningRect = new Rect(position.x + indentOffset, currentY, fieldWidth, lineHeight);
                    EditorGUI.HelpBox(warningRect, "Velocity threshold should be positive.", MessageType.Warning);
                    currentY += lineHeight + SPACING;
                }

                // On Enter Event
                float onEnterHeight = EditorGUI.GetPropertyHeight(onEnterProp, true);
                Rect onEnterRect = new Rect(position.x + indentOffset, currentY, fieldWidth, onEnterHeight);
                EditorGUI.PropertyField(onEnterRect, onEnterProp, new GUIContent("On Enter", "Invoked when conditions are met (enter state)."), true);
                currentY += onEnterHeight + SPACING;

                // On Exit Event
                float onExitHeight = EditorGUI.GetPropertyHeight(onExitProp, true);
                Rect onExitRect = new Rect(position.x + indentOffset, currentY, fieldWidth, onExitHeight);
                EditorGUI.PropertyField(onExitRect, onExitProp, new GUIContent("On Exit", "Invoked when conditions are no longer met (exit state)."), true);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            
            if (!property.isExpanded)
            {
                return lineHeight;
            }

            float height = lineHeight + SPACING; // Foldout header

            SerializedProperty eventNameProp = property.FindPropertyRelative("eventName");
            SerializedProperty velocityThresholdProp = property.FindPropertyRelative("velocityThreshold");
            SerializedProperty requiredStateProp = property.FindPropertyRelative("requiredState");
            SerializedProperty onEnterProp = property.FindPropertyRelative("onEnter");
            SerializedProperty onExitProp = property.FindPropertyRelative("onExit");

            height += lineHeight + SPACING; // Event Name
            height += lineHeight + SPACING; // Velocity Threshold and Required State

            // Warning for negative velocity threshold
            if (velocityThresholdProp.floatValue < 0f)
            {
                height += lineHeight + SPACING;
            }

            height += EditorGUI.GetPropertyHeight(onEnterProp, true) + SPACING; // On Enter
            height += EditorGUI.GetPropertyHeight(onExitProp, true); // On Exit

            return height;
        }

        private string GetEventLabel(SerializedProperty eventNameProp)
        {
            string name = eventNameProp.stringValue;
            if (string.IsNullOrEmpty(name))
            {
                return "[Unnamed Event]";
            }
            return name;
        }
    }
}
#endif

