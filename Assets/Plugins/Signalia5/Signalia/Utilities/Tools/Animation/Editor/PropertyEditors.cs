using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Signalia.Utilities.Animation;

namespace Signalia.Utilities.Animation.Editors
{
    /// <summary>
    /// Property drawer for AnimatableArrayable - displays animation entries with tabs for Main, Events, Unity Events, Audio, Haptics.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimatableArrayable))]
    public class AnimatableArrayableDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty labelProp = property.FindPropertyRelative("label");
            SerializedProperty animationAssetProp = property.FindPropertyRelative("animationAsset");
            SerializedProperty playOnEnableProp = property.FindPropertyRelative("playOnEnable");
            SerializedProperty differentTargetProp = property.FindPropertyRelative("differentTarget");
            SerializedProperty targetProp = property.FindPropertyRelative("target");
            SerializedProperty animationStartEvents = property.FindPropertyRelative("animationStartEvents");
            SerializedProperty animationStartUnityEvent = property.FindPropertyRelative("animationStartUnityEvent");
            SerializedProperty animationEndEvents = property.FindPropertyRelative("animationEndEvents");
            SerializedProperty animationEndUnityEvent = property.FindPropertyRelative("animationEndUnityEvent");
            SerializedProperty animationStartAudio = property.FindPropertyRelative("animationStartAudio");
            SerializedProperty animationEndAudio = property.FindPropertyRelative("animationEndAudio");
            SerializedProperty animationStartHaptics = property.FindPropertyRelative("animationStartHaptics");
            SerializedProperty animationEndHaptics = property.FindPropertyRelative("animationEndHaptics");

            string propertyKey = property.propertyPath;
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0;

            string foldoutLabel = string.IsNullOrEmpty(labelProp.stringValue) ? "[Unlabeled Animation]" : labelProp.stringValue;
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(foldoutLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;

                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                string[] toolbarOptions = { "Main", "Events", "Unity Events", "Audio", "Haptics" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30;

                switch (tabIndices[propertyKey])
                {
                    case 0: // Main tab
                        EditorGUI.PropertyField(position, labelProp, new GUIContent("Label"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, animationAssetProp, new GUIContent("Animation Asset"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, playOnEnableProp, new GUIContent("Play On Enable"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, differentTargetProp, new GUIContent("Different Target"));

                        if (differentTargetProp.boolValue)
                        {
                            position.y += lineHeight;
                            EditorGUI.PropertyField(position, targetProp, new GUIContent("Target"));
                        }

                        position.y += lineHeight;
                        EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, 40), "This animation will play on the specified target GameObject.", MessageType.Info);
                        position.y += 40;
                        break;

                    case 1: // Resonance Events tab
                        EditorGUI.PropertyField(position, animationStartEvents, new GUIContent("Start Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(animationStartEvents) + 4;
                        EditorGUI.PropertyField(position, animationEndEvents, new GUIContent("End Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(animationEndEvents) + 4;
                        break;

                    case 2: // Unity Events tab
                        EditorGUI.PropertyField(position, animationStartUnityEvent, new GUIContent("Start Unity Event"));
                        position.y += EditorGUI.GetPropertyHeight(animationStartUnityEvent) + 4;
                        EditorGUI.PropertyField(position, animationEndUnityEvent, new GUIContent("End Unity Event"));
                        break;

                    case 3: // Audio tab
                        float audioStartHeight = EditorGUI.GetPropertyHeight(animationStartAudio, true);
                        float audioEndHeight = EditorGUI.GetPropertyHeight(animationEndAudio, true);

                        PropertyHelpers.DrawAudioDropdownInline(position, "Start Audio", animationStartAudio, property.serializedObject);
                        position.y += audioStartHeight + 4;
                        PropertyHelpers.DrawAudioDropdownInline(position, "End Audio", animationEndAudio, property.serializedObject);
                        break;

                    case 4: // Haptics tab
                        float startHapticsHeight = EditorGUI.GetPropertyHeight(animationStartHaptics, true);
                        EditorGUI.PropertyField(position, animationStartHaptics, new GUIContent("Start Haptics"), true);
                        position.y += startHapticsHeight + 4;

                        float endHapticsHeight = EditorGUI.GetPropertyHeight(animationEndHaptics, true);
                        EditorGUI.PropertyField(position, animationEndHaptics, new GUIContent("End Haptics"), true);
                        break;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 4;

            if (property.isExpanded)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                totalHeight += 30; // Toolbar height

                string propertyKey = property.propertyPath;
                int selectedTab = tabIndices.ContainsKey(propertyKey) ? tabIndices[propertyKey] : 0;

                switch (selectedTab)
                {
                    case 0: // Main tab
                        totalHeight += lineHeight * 5;
                        totalHeight += 40; // Info box
                        SerializedProperty differentTargetProp = property.FindPropertyRelative("differentTarget");
                        if (differentTargetProp.boolValue)
                            totalHeight += lineHeight;
                        break;

                    case 1: // Resonance Events tab
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationStartEvents"), true) + 4;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationEndEvents"), true) + 4;
                        break;

                    case 2: // Unity Events tab
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationStartUnityEvent"), true);
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationEndUnityEvent"), true);
                        break;

                    case 3: // Audio tab
                        totalHeight += lineHeight * 2;
                        break;

                    case 4: // Haptics tab
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationStartHaptics"), true);
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationEndHaptics"), true);
                        break;
                }
            }

            totalHeight += 4;
            return totalHeight;
        }
    }

    /// <summary>
    /// Property drawer for AnimationSettings - displays animation configuration with tabs.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationSettings))]
    public class AnimationSettingsDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty labelProp = property.FindPropertyRelative("label");
            SerializedProperty disabledProp = property.FindPropertyRelative("disabled");
            SerializedProperty animationTypeProp = property.FindPropertyRelative("animationType");
            SerializedProperty tweenTypeProp = property.FindPropertyRelative("tweenType");
            SerializedProperty easingProp = property.FindPropertyRelative("easing");
            SerializedProperty durationProp = property.FindPropertyRelative("duration");
            SerializedProperty delayProp = property.FindPropertyRelative("delay");
            SerializedProperty dontUseSourceProp = property.FindPropertyRelative("dontUseSource");
            SerializedProperty useLocalSpaceProp = property.FindPropertyRelative("useLocalSpace");
            SerializedProperty loopsProp = property.FindPropertyRelative("loops");
            SerializedProperty loopTypeProp = property.FindPropertyRelative("loopType");
            SerializedProperty randomizeEndValue = property.FindPropertyRelative("randomizeEndValue");

            SerializedProperty comeBackProp = property.FindPropertyRelative("comeBack");
            SerializedProperty comeBackDurationProp = property.FindPropertyRelative("comeBackDuration");
            SerializedProperty comeBackDelayProp = property.FindPropertyRelative("comeBackDelay");
            SerializedProperty comeBackEasingProp = property.FindPropertyRelative("comeBackEasing");

            SerializedProperty startEventsProp = property.FindPropertyRelative("startEvents");
            SerializedProperty endEventsProp = property.FindPropertyRelative("endEvents");

            SerializedProperty startAudioProp = property.FindPropertyRelative("startAudio");
            SerializedProperty endAudioProp = property.FindPropertyRelative("endAudio");

            SerializedProperty startVectorProp = property.FindPropertyRelative("startVector");
            SerializedProperty endVectorProp = property.FindPropertyRelative("endVector");

            SerializedProperty vibratoProp = property.FindPropertyRelative("vibrato");
            SerializedProperty elasticityProp = property.FindPropertyRelative("elasticity");

            string propertyKey = property.propertyPath;
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0;

            string foldoutLabel = string.IsNullOrEmpty(labelProp.stringValue) ? "[Unnamed Animation]" : labelProp.stringValue;

            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(foldoutLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                string[] toolbarOptions = { "Main", "Comeback", "Events", "Audio", "Punch" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30;

                switch (tabIndices[propertyKey])
                {
                    case 0: // Main tab
                        EditorGUI.PropertyField(position, labelProp, new GUIContent("Label"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, disabledProp, new GUIContent("Disabled"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, animationTypeProp, new GUIContent("Animation Type"));
                        position.y += lineHeight;

                        var punch = (AnimationSettings.TweenType)tweenTypeProp.enumValueIndex == AnimationSettings.TweenType.Punch;
                        var typeIsPunchVector = ((AnimationSettings.AnimationTarget)animationTypeProp.enumValueIndex == AnimationSettings.AnimationTarget.Position ||
                            (AnimationSettings.AnimationTarget)animationTypeProp.enumValueIndex == AnimationSettings.AnimationTarget.Rotation ||
                            (AnimationSettings.AnimationTarget)animationTypeProp.enumValueIndex == AnimationSettings.AnimationTarget.Scale)
                            && (AnimationSettings.TweenType)tweenTypeProp.enumValueIndex == AnimationSettings.TweenType.Punch;

                        EditorGUI.PropertyField(position, tweenTypeProp, new GUIContent("Tween Type"));
                        position.y += lineHeight;

                        EditorGUI.PropertyField(position, easingProp, new GUIContent("Easing"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, durationProp, new GUIContent("Duration"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, delayProp, new GUIContent("Delay"));
                        position.y += lineHeight;

                        var comeBack = comeBackProp.boolValue;

                        if (comeBack)
                            DrawInfoBox(ref position, "Looping is not active due to Comeback option.");

                        EditorGUI.PropertyField(position, loopsProp, new GUIContent("Loops"));
                        position.y += lineHeight;

                        var loops = loopsProp.intValue > 0 || loopsProp.intValue == -1;

                        if (loops)
                        {
                            EditorGUI.PropertyField(position, loopTypeProp, new GUIContent("Loop Type"));
                            position.y += lineHeight;
                        }

                        if (randomizeEndValue.boolValue && typeIsPunchVector)
                        {
                            DrawInfoBox(ref position, "Randomize End Value, applicable only for Punch Vector animations.");
                        }

                        if (typeIsPunchVector)
                        {
                            EditorGUI.PropertyField(position, randomizeEndValue, new GUIContent("Randomize End Value"));
                            position.y += lineHeight;
                        }

                        var dontUseSource = dontUseSourceProp.boolValue;

                        if (!dontUseSource && punch)
                        {
                            DrawInfoBox(ref position, "In punches, the source becomes the position the object resets to before the punch.");
                        }

                        EditorGUI.PropertyField(position, dontUseSourceProp, new GUIContent("Don't Use Source"));
                        position.y += lineHeight;

                        EditorGUI.PropertyField(position, useLocalSpaceProp, new GUIContent("Use Local Space"));
                        position.y += lineHeight;

                        AnimationSettings.AnimationTarget animationType = (AnimationSettings.AnimationTarget)animationTypeProp.enumValueIndex;

                        switch (animationType)
                        {
                            case AnimationSettings.AnimationTarget.Position:
                                EditorGUI.LabelField(position, "Position Settings", EditorStyles.boldLabel);
                                position.y += lineHeight;
                                if (!dontUseSource)
                                {
                                    EditorGUI.PropertyField(position, startVectorProp, new GUIContent("[Source] Start Position"));
                                    position.y += lineHeight;
                                }
                                EditorGUI.PropertyField(position, endVectorProp, new GUIContent("End Position"));
                                position.y += lineHeight;
                                break;

                            case AnimationSettings.AnimationTarget.Rotation:
                                EditorGUI.LabelField(position, "Rotation Settings", EditorStyles.boldLabel);
                                position.y += lineHeight;
                                if (!dontUseSource)
                                {
                                    EditorGUI.PropertyField(position, startVectorProp, new GUIContent("[Source] Start Rotation"));
                                    position.y += lineHeight;
                                }
                                EditorGUI.PropertyField(position, endVectorProp, new GUIContent("End Rotation"));
                                position.y += lineHeight;
                                break;

                            case AnimationSettings.AnimationTarget.Scale:
                                EditorGUI.LabelField(position, "Scale Settings", EditorStyles.boldLabel);
                                position.y += lineHeight;
                                if (!dontUseSource)
                                {
                                    EditorGUI.PropertyField(position, startVectorProp, new GUIContent("[Source] Start Scale"));
                                    position.y += lineHeight;
                                }
                                EditorGUI.PropertyField(position, endVectorProp, new GUIContent("End Scale"));
                                position.y += lineHeight;
                                break;
                        }

                        break;

                    case 1: // Comeback tab
                        DrawInfoBox(ref position, "Comeback allows the animation to return to its original state after a set duration.");
                        EditorGUI.PropertyField(position, comeBackProp, new GUIContent("Enable Comeback"));
                        position.y += lineHeight;
                        if (comeBackProp.boolValue)
                        {
                            EditorGUI.PropertyField(position, comeBackDurationProp, new GUIContent("Comeback Duration"));
                            position.y += lineHeight;
                            EditorGUI.PropertyField(position, comeBackDelayProp, new GUIContent("Comeback Delay"));
                            position.y += lineHeight;
                            EditorGUI.PropertyField(position, comeBackEasingProp, new GUIContent("Comeback Easing"));
                        }
                        break;

                    case 2: // Events tab
                        GUIContent resonanceImage = new GUIContent(GraphicLoader.ResonanceSenders);
                        Rect imageRect = new Rect(position.x, position.y, 128, 128);
                        GUI.Label(imageRect, resonanceImage);
                        position.y += 128;

                        EditorGUI.PropertyField(position, startEventsProp, new GUIContent("Start Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(startEventsProp) + 4;
                        EditorGUI.PropertyField(position, endEventsProp, new GUIContent("End Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(endEventsProp) + 4;
                        break;

                    case 3: // Audio tab
                        PropertyHelpers.DrawAudioDropdownInline(position, "Start Audio", startAudioProp, property.serializedObject);
                        position.y += lineHeight;
                        PropertyHelpers.DrawAudioDropdownInline(position, "End Audio", endAudioProp, property.serializedObject);
                        break;

                    case 4: // Punch tab
                        EditorGUI.PropertyField(position, vibratoProp, new GUIContent("Vibrato"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, elasticityProp, new GUIContent("Elasticity"));
                        break;
                }
            }

            OverrideValues(property);

            EditorGUI.EndProperty();
        }

        private void OverrideValues(SerializedProperty property)
        {
            var loopsProp = property.FindPropertyRelative("loops");
            loopsProp.intValue = Mathf.Max(loopsProp.intValue, -1);

            var durationProp = property.FindPropertyRelative("duration");
            var delayProp = property.FindPropertyRelative("delay");
            var comeBackDurationProp = property.FindPropertyRelative("comeBackDuration");
            var comeBackDelayProp = property.FindPropertyRelative("comeBackDelay");

            durationProp.floatValue = Mathf.Max(durationProp.floatValue, 0);
            delayProp.floatValue = Mathf.Max(delayProp.floatValue, 0);
            comeBackDurationProp.floatValue = Mathf.Max(comeBackDurationProp.floatValue, 0);
            comeBackDelayProp.floatValue = Mathf.Max(comeBackDelayProp.floatValue, 0);
        }

        private void DrawInfoBox(ref Rect position, string message)
        {
            EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, 40), message, MessageType.Info);
            position.y += 50;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 4;

            if (property.isExpanded)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                float infoBoxHeight = 44;
                float extraHeight = lineHeight * 3;
                totalHeight += 30;

                SerializedProperty comeBackProp = property.FindPropertyRelative("comeBack");

                var randomizeEndValue = property.FindPropertyRelative("randomizeEndValue").boolValue;
                var typeIsPunchVector = ((AnimationSettings.AnimationTarget)property.FindPropertyRelative("animationType").enumValueIndex == AnimationSettings.AnimationTarget.Position ||
                    (AnimationSettings.AnimationTarget)property.FindPropertyRelative("animationType").enumValueIndex == AnimationSettings.AnimationTarget.Rotation ||
                    (AnimationSettings.AnimationTarget)property.FindPropertyRelative("animationType").enumValueIndex == AnimationSettings.AnimationTarget.Scale)
                    && (AnimationSettings.TweenType)property.FindPropertyRelative("tweenType").enumValueIndex == AnimationSettings.TweenType.Punch;

                string propertyKey = property.propertyPath;
                int selectedTab = tabIndices.ContainsKey(propertyKey) ? tabIndices[propertyKey] : 0;

                var main_totalHeightDeductions = new List<float>();
                var main_totalInfoBoxes = new List<float>();
                var comeBack_totalInfoBoxes = new List<float>();

                var doesntLoop = property.FindPropertyRelative("loops").intValue == 0;

                if (doesntLoop)
                    main_totalHeightDeductions.Add(1);

                var dontUseSource = property.FindPropertyRelative("dontUseSource").boolValue;

                if (dontUseSource)
                    main_totalHeightDeductions.Add(1);

                if (!typeIsPunchVector)
                    main_totalHeightDeductions.Add(1);

                if (typeIsPunchVector && randomizeEndValue)
                {
                    main_totalInfoBoxes.Add(1);
                }

                if (comeBackProp.boolValue)
                    main_totalInfoBoxes.Add(1);

                if (!dontUseSource && property.FindPropertyRelative("tweenType").enumValueIndex == 1)
                    main_totalInfoBoxes.Add(1);

                comeBack_totalInfoBoxes.Add(1);

                switch (selectedTab)
                {
                    case 0: // Main tab
                        totalHeight += infoBoxHeight * main_totalInfoBoxes.Count;
                        var propertiesHeight = 13 - main_totalHeightDeductions.Count; // Added 1 for useLocalSpace
                        totalHeight += lineHeight * propertiesHeight;
                        totalHeight += extraHeight;
                        break;

                    case 1: // Comeback tab
                        totalHeight += infoBoxHeight * comeBack_totalInfoBoxes.Count;
                        totalHeight += comeBackProp.boolValue ? lineHeight * 4 : lineHeight;
                        break;

                    case 2: // Events tab
                        totalHeight += 128;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("startEvents"), true) + 4;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("endEvents"), true) + 4;
                        break;

                    case 3: // Audio tab
                        totalHeight += lineHeight * 2;
                        break;

                    case 4: // Punch tab
                        totalHeight += lineHeight * 2;
                        break;
                }
            }

            totalHeight += 4;
            return totalHeight;
        }
    }
}
