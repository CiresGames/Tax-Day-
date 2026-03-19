using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AHAKuo.Signalia.UI.UIAnimationSettings;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.UI.Editors
{
    [CustomPropertyDrawer(typeof(UIAnimatableArrayable))]
    public class UIAnimatableArrayableDrawer : PropertyDrawer
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
                tabIndices[propertyKey] = 0; // Default tab index to "Main"

            string foldoutLabel = string.IsNullOrEmpty(labelProp.stringValue) ? "[Unlabeled Animation]" : labelProp.stringValue;
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(foldoutLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;

                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                // **Toolbar with Five Tabs**
                string[] toolbarOptions = { "Main", "Events", "Unity Events", "Audio", "Haptics" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30; // Move below the toolbar

                // **Switch between tabs**
                switch (tabIndices[propertyKey])
                {
                    case 0: // **Main tab**
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
                        EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, 40), "Restore original transform values everytime it is animated.", MessageType.Info);
                        position.y += 40; // Adjust spacing after the info box
                        break;

                    case 1: // **Resonance Events tab**
                        EditorGUI.PropertyField(position, animationStartEvents, new GUIContent("Start Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(animationStartEvents) + 4;
                        EditorGUI.PropertyField(position, animationEndEvents, new GUIContent("End Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(animationEndEvents) + 4;
                        break;

                    case 2: // **Unity Events tab**
                        EditorGUI.PropertyField(position, animationStartUnityEvent, new GUIContent("Start Unity Event"));
                        position.y += EditorGUI.GetPropertyHeight(animationStartUnityEvent) + 4;
                        EditorGUI.PropertyField(position, animationEndUnityEvent, new GUIContent("End Unity Event"));
                        break;

                    case 3: // **Audio tab**
                            // Properly render Audio List
                        float audioStartHeight = EditorGUI.GetPropertyHeight(animationStartAudio, true);
                        float audioEndHeight = EditorGUI.GetPropertyHeight(animationEndAudio, true);

                        PropertyHelpers.DrawAudioDropdownInline(position, "Start Audio", animationStartAudio, property.serializedObject);
                        position.y += audioStartHeight + 4;
                        PropertyHelpers.DrawAudioDropdownInline(position, "End Audio", animationEndAudio, property.serializedObject);
                        break;

                    case 4: // **Haptics tab**
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
                    case 0: // **Main tab**
                        totalHeight += lineHeight * 5; // Label, Animation Asset, Play On Enable, Different Target, Restore Original
                        totalHeight += 40; // Extra height for Restore Original info box
                        SerializedProperty differentTargetProp = property.FindPropertyRelative("differentTarget");
                        if (differentTargetProp.boolValue)
                            totalHeight += lineHeight; // Extra height for Target property
                        break;

                    case 1: // **Resonance Events tab**
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationStartEvents"), true) + 4;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationEndEvents"), true) + 4;
                        break;

                    case 2: // **Unity Events tab**
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationStartUnityEvent"), true);
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationEndUnityEvent"), true);
                        break;

                    case 3: // **Audio tab**
                        totalHeight += lineHeight * 2; // Start Audio + End Audio
                        break;

                    case 4: // **Haptics tab**
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationStartHaptics"), true);
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("animationEndHaptics"), true);
                        break;
                }
            }

            // always add a little padding
            totalHeight += 4;

            return totalHeight;
        }
    }

    [CustomPropertyDrawer(typeof(Cascadable))]
    public class CascadableDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty targetProp = property.FindPropertyRelative("target");
            SerializedProperty targetViewProp = property.FindPropertyRelative("targetView");
            SerializedProperty showDelayProp = property.FindPropertyRelative("showDelay");
            SerializedProperty showOnEndProp = property.FindPropertyRelative("showOnEnd");
            SerializedProperty audioAtStartProp = property.FindPropertyRelative("audioAtStart");
            SerializedProperty audioAtEndProp = property.FindPropertyRelative("audioAtEnd");


            string propertyKey = property.propertyPath;
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0; // Default tab index to "Main"

            // if no target is assigned, use "No Target" if yes, use the target's gameobject name
            var foldOutLabel = targetProp.objectReferenceValue == null ? "[No Target]" : targetProp.objectReferenceValue.name;
            var targetViewLabel = targetViewProp.objectReferenceValue == null ? "[No Target]" : targetViewProp.objectReferenceValue.name;

            var finalTargetName = foldOutLabel == "[No Target]" ? targetViewLabel : foldOutLabel;

            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(finalTargetName), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;

                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                // **Toolbar with Two Tabs: Main, Audio**
                string[] toolbarOptions = { "Main", "Audio" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30; // Move below the toolbar

                // **Switch between tabs**
                switch (tabIndices[propertyKey])
                {
                    case 0: // **Main tab**
                        EditorGUI.PropertyField(position, targetProp, new GUIContent("Target"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, targetViewProp, new GUIContent("Target View"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, showDelayProp, new GUIContent("Show Delay"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, showOnEndProp, new GUIContent("Show On End"));
                        position.y += lineHeight;
                        break;

                    case 1: // **Audio tab**
                        PropertyHelpers.DrawAudioDropdownInline(position, "Show Audio", audioAtStartProp, property.serializedObject);
                        position.y += lineHeight;
                        PropertyHelpers.DrawAudioDropdownInline(position, "Hide Audio", audioAtEndProp, property.serializedObject);
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
                    case 0: // **Main tab**
                        totalHeight += lineHeight * 4; // Target, Show Delay, Show On End, Hide Delay, Hide On End + target view
                        break;

                    case 1: // **Audio tab**
                        totalHeight += lineHeight * 2; // Audio Settings label, Audio At Start, Audio At End
                        break;
                }
            }

            // always add a little padding
            totalHeight += 4;

            return totalHeight;
        }
    }

    [CustomPropertyDrawer(typeof(UIAnimationSettings))]
    public class UIAnimationSettingsDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Find serialized properties
            SerializedProperty labelProp = property.FindPropertyRelative("label");
            SerializedProperty disabledProp = property.FindPropertyRelative("disabled");
            SerializedProperty animationTypeProp = property.FindPropertyRelative("animationType");
            SerializedProperty tweenTypeProp = property.FindPropertyRelative("tweenType");
            SerializedProperty easingProp = property.FindPropertyRelative("easing");
            SerializedProperty durationProp = property.FindPropertyRelative("duration");
            SerializedProperty delayProp = property.FindPropertyRelative("delay");
            SerializedProperty dontUseSourceProp = property.FindPropertyRelative("dontUseSource");
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

            SerializedProperty startFloatProp = property.FindPropertyRelative("startFloat");
            SerializedProperty endFloatProp = property.FindPropertyRelative("endFloat");

            SerializedProperty vibratoProp = property.FindPropertyRelative("vibrato");
            SerializedProperty elasticityProp = property.FindPropertyRelative("elasticity");

            string propertyKey = property.propertyPath;
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0; // Default to "Main"

            // Use labelProp value as the foldout title, fallback to default if empty
            string foldoutLabel = string.IsNullOrEmpty(labelProp.stringValue) ? "[Unnamed Animation]" : labelProp.stringValue;

            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(foldoutLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                // **Toolbar with Tabs**
                string[] toolbarOptions = { "Main", "Comeback", "Events", "Audio", "Punch" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30;

                // **Switch Tabs**
                switch (tabIndices[propertyKey])
                {
                    case 0: // **Main tab**
                        EditorGUI.PropertyField(position, labelProp, new GUIContent("Label"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, disabledProp, new GUIContent("Disabled"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, animationTypeProp, new GUIContent("Animation Type"));
                        position.y += lineHeight;

                        var fading = (AnimationTarget)animationTypeProp.enumValueIndex == AnimationTarget.Fade;
                        var punch = (TweenType)tweenTypeProp.enumValueIndex == TweenType.Punch;
                        var typeIsPunchVector = ((AnimationTarget)animationTypeProp.enumValueIndex == AnimationTarget.Position || (AnimationTarget)animationTypeProp.enumValueIndex == AnimationTarget.Rotation || (AnimationTarget)animationTypeProp.enumValueIndex == AnimationTarget.Scale)
    && (TweenType)tweenTypeProp.enumValueIndex == TweenType.Punch;

                        if (fading && punch)
                        {
                            DrawInfoBox(ref position, "Fade animations are not affected by [Punch] tween type.");
                        }

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

                        // info box for randomize end value
                        if (randomizeEndValue.boolValue && typeIsPunchVector)
                        {
                            DrawInfoBox(ref position, "Randomize End Value, applicable only for Punch Vector animations. This will randomize the end value of the animation within a negative to positive range of the end value.");
                        }

                        if (typeIsPunchVector)
                        {
                            EditorGUI.PropertyField(position, randomizeEndValue, new GUIContent("Randomize End Value"));
                            position.y += lineHeight;
                        }

                        var tweenType = (TweenType)tweenTypeProp.enumValueIndex;

                        var dontUseSource = dontUseSourceProp.boolValue;

                        if (!dontUseSource && tweenType == TweenType.Punch)
                        {
                            DrawInfoBox(ref position, "In punches, the source becomes the position the object resets to before the punch.");
                        }

                        EditorGUI.PropertyField(position, dontUseSourceProp, new GUIContent("Don't Use Source"));
                        position.y += lineHeight;

                        // **Dynamic Settings Based on Animation Type**
                        AnimationTarget animationType = (AnimationTarget)animationTypeProp.enumValueIndex;

                        switch (animationType)
                        {
                            case AnimationTarget.Position:
                                EditorGUI.LabelField(position, "Position Settings", EditorStyles.boldLabel);
                                position.y += lineHeight;
                                DrawInfoBox(ref position, "Position vectors will use a value window from 0 to 1 or around that range. Think of it as RectSpace where 0 is the bottom left and 1 is the top right of the element's parent RectTransform. This is affected by anchor points and pivot points.");
                                if (!dontUseSource)
                                {
                                    EditorGUI.PropertyField(position, startVectorProp, new GUIContent("[Source] Start Position"));
                                    position.y += lineHeight;
                                }
                                EditorGUI.PropertyField(position, endVectorProp, new GUIContent("End Position"));
                                position.y += lineHeight;
                                break;

                            case AnimationTarget.Rotation:
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

                            case AnimationTarget.Scale:
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

                            case AnimationTarget.Fade:
                                EditorGUI.LabelField(position, "Fade Settings", EditorStyles.boldLabel);
                                position.y += lineHeight;
                                if (!dontUseSource)
                                {
                                    EditorGUI.PropertyField(position, startFloatProp, new GUIContent("[Source] Start Fade"));
                                    position.y += lineHeight;
                                }
                                EditorGUI.PropertyField(position, endFloatProp, new GUIContent("End Alpha"));
                                position.y += lineHeight;
                                break;
                        }

                        break;

                    case 1: // **Comeback tab**
                        DrawInfoBox(ref position, "Comeback is a feature that allows the animation to return to its original state after a set duration.");
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

                    case 2: // **Events tab**
                        // draw resonance image
                        GUIContent resonanceImage = new GUIContent(GraphicLoader.ResonanceSenders);
                        Rect imageRect = new Rect(position.x, position.y, 128, 128);
                        GUI.Label(imageRect, resonanceImage);
                        position.y += 128; // Adjusted height for the image

                        EditorGUI.PropertyField(position, startEventsProp, new GUIContent("Start Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(startEventsProp) + 4;
                        EditorGUI.PropertyField(position, endEventsProp, new GUIContent("End Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(endEventsProp) + 4;
                        break;

                    case 3: // **Audio tab**
                        PropertyHelpers.DrawAudioDropdownInline(position, "Start Audio", startAudioProp, property.serializedObject);
                        position.y += lineHeight;
                        PropertyHelpers.DrawAudioDropdownInline(position, "End Audio", endAudioProp, property.serializedObject);
                        break;

                    case 4: // **Punch tab**
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
            // make sure some values have clamps.

            var loopsProp = property.FindPropertyRelative("loops");

            // loop -1 minimum
            loopsProp.intValue = Mathf.Max(loopsProp.intValue, -1);

            // time values minimum 0
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
            position.y += 50; // Adjust spacing after the info box
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 4;

            if (property.isExpanded)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                float infoBoxHeight = 44; // Approximate height of an info box
                float extraHeight = lineHeight * 3; // Vector title and two vector values
                totalHeight += 30; // Toolbar height

                SerializedProperty comeBackProp = property.FindPropertyRelative("comeBack");

                // randomize
                var randomizeEndValue = property.FindPropertyRelative("randomizeEndValue").boolValue;
                var typeIsPunchVector = ((AnimationTarget)property.FindPropertyRelative("animationType").enumValueIndex == AnimationTarget.Position || (AnimationTarget)property.FindPropertyRelative("animationType").enumValueIndex == AnimationTarget.Rotation || (AnimationTarget)property.FindPropertyRelative("animationType").enumValueIndex == AnimationTarget.Scale)
                    && (TweenType)property.FindPropertyRelative("tweenType").enumValueIndex == TweenType.Punch;

                string propertyKey = property.propertyPath;
                int selectedTab = tabIndices.ContainsKey(propertyKey) ? tabIndices[propertyKey] : 0;

                // MAIN TAB
                var main_totalHeightDeductions = new List<float>();
                var main_totalInfoBoxes = new List<float>();

                // COMEBACK TAB
                var comeBack_totalHeightDeductions = new List<float>();
                var comeBack_totalInfoBoxes = new List<float>();

                // DEDUCTIONS
                var doesntLoop = property.FindPropertyRelative("loops").intValue == 0;

                if (doesntLoop)
                    main_totalHeightDeductions.Add(1);

                var dontUseSource = property.FindPropertyRelative("dontUseSource").boolValue;

                if (dontUseSource)
                    main_totalHeightDeductions.Add(1);

                if (!typeIsPunchVector)
                    main_totalHeightDeductions.Add(1); // because randomize would be hidden

                if (typeIsPunchVector
                    && randomizeEndValue)
                {
                    main_totalInfoBoxes.Add(1); // header info
                }

                // INFOBOXES (this is very vague, but bear with me)
                if (comeBackProp.boolValue)
                    main_totalInfoBoxes.Add(1);

                var positionAnimation = property.FindPropertyRelative("animationType").enumValueIndex == 0;

                if (positionAnimation)
                    main_totalInfoBoxes.Add(1);

                if (!dontUseSource && property.FindPropertyRelative("tweenType").enumValueIndex == 1)
                    main_totalInfoBoxes.Add(1);

                var punchAndFade = property.FindPropertyRelative("animationType").enumValueIndex == 3 && property.FindPropertyRelative("tweenType").enumValueIndex == 1;
                if (punchAndFade)
                    main_totalInfoBoxes.Add(1);

                comeBack_totalInfoBoxes.Add(1); // header info

                switch (selectedTab)
                {
                    case 0: // **Main tab**
                        totalHeight += infoBoxHeight * main_totalInfoBoxes.Total(); // Info box height
                        var propertiesHeight = 12 - main_totalHeightDeductions.Total();
                        totalHeight += lineHeight * propertiesHeight;
                        totalHeight += extraHeight;
                        break;

                    case 1: // **Comeback tab**
                        totalHeight += infoBoxHeight * comeBack_totalInfoBoxes.Total(); // Info box height
                        totalHeight += comeBackProp.boolValue ? lineHeight * 4 : lineHeight;
                        break;

                    case 2: // **Events tab**
                        totalHeight += 128; // Space for Resonance image
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("startEvents"), true) + 4;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("endEvents"), true) + 4;
                        break;

                    case 3: // **Audio tab**
                        totalHeight += lineHeight * 2;
                        break;

                    case 4: // **Punch tab**
                        totalHeight += lineHeight * 2;
                        break;
                }
            }

            // always add a little padding
            totalHeight += 4;

            return totalHeight;
        }
    }
}