#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    /// <summary>
    /// Property drawer for EntityFSMState.
    /// Displays state name, actions array, and conditions array with proper foldouts.
    /// </summary>
    [CustomPropertyDrawer(typeof(EntityFSMState))]
    public class EntityFSMStateDrawer : PropertyDrawer
    {
        private const float SPACING = 4f;
        private const float INDENT_WIDTH = 0; // zero to avoid too much indentation

        // Cache for ReorderableList instances
        private Dictionary<string, ReorderableList> actionsListCache = new Dictionary<string, ReorderableList>();
        private Dictionary<string, ReorderableList> conditionsListCache = new Dictionary<string, ReorderableList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty stateNameProp = property.FindPropertyRelative("stateName");
            SerializedProperty actionsProp = property.FindPropertyRelative("actions");
            SerializedProperty conditionsProp = property.FindPropertyRelative("conditions");

            // Use the state name as the foldout label
            string foldoutLabel = !string.IsNullOrEmpty(stateNameProp?.stringValue) 
                ? $"⚙ {stateNameProp.stringValue}" 
                : "[Unnamed State]";

            // Add state info to label
            int actionsCount = actionsProp?.arraySize ?? 0;
            int conditionsCount = conditionsProp?.arraySize ?? 0;
            foldoutLabel += $" ({actionsCount} actions, {conditionsCount} conditions)";

            position.height = EditorGUIUtility.singleLineHeight;
            
            EditorGUI.indentLevel++;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);
            EditorGUI.indentLevel--;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float lineHeight = EditorGUIUtility.singleLineHeight + SPACING;
                position.y += lineHeight;

                // Draw background for expanded content
                float expandedHeight = GetExpandedContentHeight(property) - lineHeight;
                Rect bgRect = new Rect(position.x - INDENT_WIDTH, position.y - 2, position.width + INDENT_WIDTH, expandedHeight + 4);
                EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.2f, 0.2f, 0.15f));

                // State Name
                Rect stateNameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(stateNameRect, stateNameProp, new GUIContent("State Name", "The unique identifier for this state"));
                position.y += lineHeight;

                // Separator
                position.y += 2;
                Rect separatorRect = new Rect(position.x, position.y, position.width, 1);
                EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                position.y += 4;

                // Actions Section Header
                Rect actionsHeaderRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(actionsHeaderRect, "Actions", EditorStyles.boldLabel);
                position.y += lineHeight;

                // Actions Array
                if (actionsProp != null)
                {
                    float actionsHeight = EditorGUI.GetPropertyHeight(actionsProp, true);
                    Rect actionsRect = new Rect(position.x, position.y, position.width, actionsHeight);
                    EditorGUI.PropertyField(actionsRect, actionsProp, new GUIContent("State Actions", "Actions to execute during this state"), true);
                    position.y += actionsHeight + SPACING;
                }

                // Separator
                position.y += 2;
                separatorRect = new Rect(position.x, position.y, position.width, 1);
                EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                position.y += 4;

                // Conditions Section Header
                Rect conditionsHeaderRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(conditionsHeaderRect, "Condition Stacks", EditorStyles.boldLabel);
                position.y += lineHeight;

                // Conditions Array
                if (conditionsProp != null)
                {
                    float conditionsHeight = EditorGUI.GetPropertyHeight(conditionsProp, true);
                    Rect conditionsRect = new Rect(position.x, position.y, position.width, conditionsHeight);
                    EditorGUI.PropertyField(conditionsRect, conditionsProp, new GUIContent("Transition Conditions", "Condition stacks that determine state transitions"), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private float GetExpandedContentHeight(SerializedProperty property)
        {
            SerializedProperty actionsProp = property.FindPropertyRelative("actions");
            SerializedProperty conditionsProp = property.FindPropertyRelative("conditions");

            float lineHeight = EditorGUIUtility.singleLineHeight + SPACING;
            float totalHeight = lineHeight; // Foldout

            if (property.isExpanded)
            {
                totalHeight += lineHeight; // State Name
                totalHeight += 8; // Separator + padding
                totalHeight += lineHeight; // Actions header

                if (actionsProp != null)
                {
                    totalHeight += EditorGUI.GetPropertyHeight(actionsProp, true) + SPACING;
                }

                totalHeight += 8; // Separator + padding
                totalHeight += lineHeight; // Conditions header

                if (conditionsProp != null)
                {
                    totalHeight += EditorGUI.GetPropertyHeight(conditionsProp, true);
                }
            }

            return totalHeight;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + SPACING;

            if (property.isExpanded)
            {
                totalHeight = GetExpandedContentHeight(property);
            }

            return totalHeight;
        }
    }

    /// <summary>
    /// Property drawer for ConditionStack.
    /// Displays stack name, exit state, operator, and conditions array.
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionStack))]
    public class ConditionStackDrawer : PropertyDrawer
    {
        private const float SPACING = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty stackNameProp = property.FindPropertyRelative("stackName");
            SerializedProperty exitStateProp = property.FindPropertyRelative("exitState");
            SerializedProperty operatorProp = property.FindPropertyRelative("_operator");
            SerializedProperty conditionsProp = property.FindPropertyRelative("_conditions");

            // Use the stack name as the foldout label
            string stackName = !string.IsNullOrEmpty(stackNameProp?.stringValue) 
                ? stackNameProp.stringValue 
                : "[Unnamed Stack]";
            
            string exitState = !string.IsNullOrEmpty(exitStateProp?.stringValue) 
                ? $" → {exitStateProp.stringValue}" 
                : "";

            int conditionsCount = conditionsProp?.arraySize ?? 0;
            string operatorLabel = operatorProp != null ? ((ConditionStackOperator)operatorProp.enumValueIndex).ToString().ToUpper() : "?";
            
            string foldoutLabel = $"📋 {stackName}{exitState} [{operatorLabel}] ({conditionsCount} conditions)";

            position.height = EditorGUIUtility.singleLineHeight;
            
            // Color-coded background based on operator
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);

            if (property.isExpanded)
            {
                //EditorGUI.indentLevel++;
                float lineHeight = EditorGUIUtility.singleLineHeight + SPACING;
                position.y += lineHeight;

                // Stack Name
                Rect stackNameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(stackNameRect, stackNameProp, new GUIContent("Stack", "Identifier for this condition stack"));
                position.y += lineHeight;

                // Exit State
                Rect exitStateRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(exitStateRect, exitStateProp, new GUIContent("Exit", "State to transition to when conditions pass"));
                position.y += lineHeight;

                // Operator
                Rect operatorRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(operatorRect, operatorProp, new GUIContent("Type", "AND = all conditions must pass, OR = any condition can pass"));
                position.y += lineHeight;

                // Help box
                string helpText = operatorProp != null && operatorProp.enumValueIndex == 0 
                    ? "All conditions must be met for this stack to pass."
                    : "Any condition being met will cause this stack to pass.";
                Rect helpRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 1.5f);
                EditorGUI.HelpBox(helpRect, helpText, MessageType.Info);
                position.y += EditorGUIUtility.singleLineHeight * 1.5f + SPACING;

                // Conditions Array
                if (conditionsProp != null)
                {
                    float conditionsHeight = EditorGUI.GetPropertyHeight(conditionsProp, true);
                    Rect conditionsRect = new Rect(position.x, position.y, position.width, conditionsHeight);
                    EditorGUI.PropertyField(conditionsRect, conditionsProp, new GUIContent("Conditions", "Individual condition checks and their expected values"), true);
                }

                //EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + SPACING;

            if (property.isExpanded)
            {
                SerializedProperty conditionsProp = property.FindPropertyRelative("_conditions");
                float lineHeight = EditorGUIUtility.singleLineHeight + SPACING;
                
                totalHeight += lineHeight; // Stack Name
                totalHeight += lineHeight; // Exit State
                totalHeight += lineHeight; // Operator
                totalHeight += EditorGUIUtility.singleLineHeight * 1.5f + SPACING; // Help box

                if (conditionsProp != null)
                {
                    totalHeight += EditorGUI.GetPropertyHeight(conditionsProp, true);
                }
            }
            
            totalHeight += SPACING;

            return totalHeight;
        }
    }

    /// <summary>
    /// Property drawer for ConditionAndExpectation.
    /// Displays condition reference and expectation toggle on a single line when collapsed.
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionAndExpectation))]
    public class ConditionAndExpectationDrawer : PropertyDrawer
    {
        private const float SPACING = 4f;
        private const float EXPECT_TOGGLE_WIDTH = 70f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty conditionProp = property.FindPropertyRelative("_condition");
            SerializedProperty expectProp = property.FindPropertyRelative("_expect");

            // Get condition name for display
            string conditionName = "[None]";
            EntityCondition condition = conditionProp?.objectReferenceValue as EntityCondition;
            if (condition != null)
            {
                conditionName = EntityComponentDropdownUtilities.GetDisplayName(condition);
            }

            // Build label showing condition name and expectation
            string expectStr = expectProp != null && expectProp.boolValue ? "TRUE" : "FALSE";
            string foldoutLabel = $"🔍 {conditionName} → Expect: {expectStr}";

            position.height = EditorGUIUtility.singleLineHeight;
            
            // Color-coded based on expectation
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);

            if (property.isExpanded)
            {
                //EditorGUI.indentLevel++;
                float lineHeight = EditorGUIUtility.singleLineHeight + SPACING;
                position.y += lineHeight;

                // Condition Reference
                Rect conditionRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EntityLogic logic = property.serializedObject.targetObject as EntityLogic;
                IReadOnlyList<EntityCondition> availableConditions = EntityComponentDropdownUtilities.GetComponents<EntityCondition>(logic);
                EntityCondition selectedCondition = EntityComponentDropdownUtilities.DrawComponentPopup(conditionRect,
                    new GUIContent("", "The EntityCondition component to evaluate"),
                    availableConditions,
                    condition);
                if (selectedCondition != condition)
                {
                    conditionProp.objectReferenceValue = selectedCondition;
                }
                position.y += lineHeight;

                // Expectation Toggle with colored label
                Rect expectRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                
                // Draw colored indicator
                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = expectProp.boolValue ? Color.green : Color.red;
                EditorGUI.PropertyField(expectRect, expectProp, new GUIContent("Expect", "The expected result of the condition. If condition returns this value, it passes."));
                GUI.backgroundColor = prevColor;
                position.y += lineHeight;

                // Help text
                string helpText = expectProp.boolValue 
                    ? "Condition passes when it returns TRUE."
                    : "Condition passes when it returns FALSE.";
                Rect helpRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(helpRect, helpText, EditorStyles.miniLabel);

                //EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + SPACING;

            if (property.isExpanded)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight + SPACING;
                totalHeight += lineHeight; // Condition
                totalHeight += lineHeight; // Expect
                totalHeight += EditorGUIUtility.singleLineHeight; // Help text
            }

            return totalHeight;
        }
    }

    /// <summary>
    /// Property drawer for ConditionStackOperator enum.
    /// Provides a more visual representation of the operator choice.
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionStackOperator))]
    public class ConditionStackOperatorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            float labelWidth = EditorGUIUtility.labelWidth;
            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            // Color the field based on selection
            Color prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = property.enumValueIndex == 0 
                ? new Color(0.5f, 0.8f, 0.5f) // AND - green
                : new Color(0.8f, 0.6f, 0.3f); // OR - orange

            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
            
            GUI.backgroundColor = prevBgColor;

            EditorGUI.EndProperty();
        }
    }
}
#endif
