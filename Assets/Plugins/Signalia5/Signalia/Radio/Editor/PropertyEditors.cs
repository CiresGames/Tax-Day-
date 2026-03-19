using AHAKuo.Signalia.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Radio;

namespace AHAKuo.Signalia.Radio.Editors
{
    [CustomPropertyDrawer(typeof(SimpleEventListener.ListenerAndCallback))]
    public class ListenerAndCallbackDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var eventNameProp = property.FindPropertyRelative("eventName");
            var oneShotProp = property.FindPropertyRelative("oneShot");
            var callbackProp = property.FindPropertyRelative("callback");

            float spacing = 2f;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Event name field
            var eventRect = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.PropertyField(eventRect, eventNameProp, new GUIContent("Event Name"));

            // One-shot toggle
            var oneShotRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            EditorGUI.PropertyField(oneShotRect, oneShotProp, new GUIContent("One Shot"));

            // Callback field (multi-line)
            float callbackHeight = EditorGUI.GetPropertyHeight(callbackProp, true);
            var callbackRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, callbackHeight);
            EditorGUI.PropertyField(callbackRect, callbackProp, new GUIContent("Callback"), true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var callbackHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("callback"), true);
            return EditorGUIUtility.singleLineHeight * 2 + 2f + callbackHeight;
        }
    }

    [CustomPropertyDrawer(typeof(SimpleStaticObject.StaticObjectElement))]
    public class LiveKeyElementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty otherGameObjectProp = property.FindPropertyRelative("otherGameObject");
            SerializedProperty referenceableObjectProp = property.FindPropertyRelative("referenceableObject");
            SerializedProperty keyNameProp = property.FindPropertyRelative("staticName");

            float yOffset = position.y;
            float fieldHeight = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(new Rect(position.x, yOffset, position.width, fieldHeight), label);
            yOffset += fieldHeight + 2;

            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, fieldHeight), keyNameProp, new GUIContent("Key Name"));
            yOffset += fieldHeight + 2;

            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width - 80, fieldHeight), otherGameObjectProp, new GUIContent("Target Object"));
            if (GUI.Button(new Rect(position.x + position.width - 75, yOffset, 75, fieldHeight), "Unselect"))
            {
                otherGameObjectProp.objectReferenceValue = null;
            }
            yOffset += fieldHeight + 2;

            if (otherGameObjectProp.objectReferenceValue != null)
            {
                GameObject targetObj = otherGameObjectProp.objectReferenceValue as GameObject;
                string targetName = targetObj != null ? targetObj.name : "Unknown";
                EditorGUI.HelpBox(new Rect(position.x, yOffset, position.width, fieldHeight * 2), $"Now showing components from '{targetName}', unselect to set from something else", MessageType.Info);
                yOffset += fieldHeight * 2 + 2;
            }
            else
            {
                EditorGUI.HelpBox(new Rect(position.x, yOffset, position.width, fieldHeight * 2), "If Target Object is not defined, it will use this object as the component source.", MessageType.Info);
                yOffset += fieldHeight * 2 + 2;
            }

            GameObject targetGameObject = otherGameObjectProp.objectReferenceValue as GameObject;
            if (targetGameObject == null)
            {
                SerializedObject serializedObject = property.serializedObject;
                if (serializedObject.targetObject is SimpleStaticObject responderObj)
                {
                    targetGameObject = responderObj.gameObject;
                }
            }

            if (targetGameObject != null)
            {
                List<UnityEngine.Object> availableComponents = GetComponentsList(targetGameObject);

                int selectedIndex = availableComponents.IndexOf(referenceableObjectProp.objectReferenceValue);
                selectedIndex = EditorGUI.Popup(new Rect(position.x, yOffset, position.width, fieldHeight), "Object Reference", selectedIndex, GetComponentNames(availableComponents));

                if (selectedIndex >= 0)
                {
                    referenceableObjectProp.objectReferenceValue = availableComponents[selectedIndex];
                }
            }

            EditorGUI.EndProperty();
        }

        private List<UnityEngine.Object> GetComponentsList(GameObject target)
        {
            List<UnityEngine.Object> list = new List<UnityEngine.Object> { target };

            foreach (var component in target.GetComponents<Component>())
            {
                if (component.GetType() != typeof(SimpleStaticObject))
                {
                    list.Add(component);
                }
            }

            return list;
        }

        private string[] GetComponentNames(List<UnityEngine.Object> components)
        {
            List<string> names = new List<string>();

            foreach (var component in components)
            {
                if (component is GameObject go)
                    names.Add($"GameObject: {go.name}");
                else
                    names.Add($"Component: {component.GetType().Name}");
            }

            return names.ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var targetGameObject = property.FindPropertyRelative("otherGameObject").objectReferenceValue as GameObject;
            if (targetGameObject != null)
            {
                return EditorGUIUtility.singleLineHeight * 7; // 7 lines when target is selected
            }
            return EditorGUIUtility.singleLineHeight * 7; // 7 lines when target is not selected (includes help box)
        }
    }

    [CustomPropertyDrawer(typeof(SimpleNonStaticObject.NonStaticObjectElement))]
    public class DeadKeyElementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty otherGameObjectProp = property.FindPropertyRelative("otherGameObject");
            SerializedProperty referenceableObjectProp = property.FindPropertyRelative("referenceableObject");
            SerializedProperty keyNameProp = property.FindPropertyRelative("nonStaticName");

            float yOffset = position.y;
            float fieldHeight = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(new Rect(position.x, yOffset, position.width, fieldHeight), label);
            yOffset += fieldHeight + 2;

            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, fieldHeight), keyNameProp, new GUIContent("Key Name"));
            yOffset += fieldHeight + 2;

            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width - 80, fieldHeight), otherGameObjectProp, new GUIContent("Target Object"));
            if (GUI.Button(new Rect(position.x + position.width - 75, yOffset, 75, fieldHeight), "Unselect"))
            {
                otherGameObjectProp.objectReferenceValue = null;
            }
            yOffset += fieldHeight + 2;

            if (otherGameObjectProp.objectReferenceValue != null)
            {
                GameObject targetObj = otherGameObjectProp.objectReferenceValue as GameObject;
                string targetName = targetObj != null ? targetObj.name : "Unknown";
                EditorGUI.HelpBox(new Rect(position.x, yOffset, position.width, fieldHeight * 2), $"Now showing components from '{targetName}', unselect to set from something else", MessageType.Info);
                yOffset += fieldHeight * 2 + 2;
            }
            else
            {
                EditorGUI.HelpBox(new Rect(position.x, yOffset, position.width, fieldHeight * 2), "If Target Object is not defined, it will use this object as the component source.", MessageType.Info);
                yOffset += fieldHeight * 2 + 2;
            }

            GameObject targetGameObject = otherGameObjectProp.objectReferenceValue as GameObject;
            if (targetGameObject == null)
            {
                SerializedObject serializedObject = property.serializedObject;
                if (serializedObject.targetObject is SimpleNonStaticObject responderObj)
                {
                    targetGameObject = responderObj.gameObject;
                }
            }

            if (targetGameObject != null)
            {
                List<UnityEngine.Object> availableComponents = GetComponentsList(targetGameObject);

                int selectedIndex = availableComponents.IndexOf(referenceableObjectProp.objectReferenceValue);
                selectedIndex = EditorGUI.Popup(new Rect(position.x, yOffset, position.width, fieldHeight), "Object Reference", selectedIndex, GetComponentNames(availableComponents));

                if (selectedIndex >= 0)
                {
                    referenceableObjectProp.objectReferenceValue = availableComponents[selectedIndex];
                }
            }

            EditorGUI.EndProperty();
        }

        private List<UnityEngine.Object> GetComponentsList(GameObject target)
        {
            List<UnityEngine.Object> list = new List<UnityEngine.Object> { target };

            foreach (var component in target.GetComponents<Component>())
            {
                if (component.GetType() != typeof(SimpleNonStaticObject))
                {
                    list.Add(component);
                }
            }

            return list;
        }

        private string[] GetComponentNames(List<UnityEngine.Object> components)
        {
            List<string> names = new List<string>();

            foreach (var component in components)
            {
                if (component is GameObject go)
                    names.Add($"GameObject: {go.name}");
                else
                    names.Add($"Component: {component.GetType().Name}");
            }

            return names.ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var targetGameObject = property.FindPropertyRelative("otherGameObject").objectReferenceValue as GameObject;
            if (targetGameObject != null)
            {
                return EditorGUIUtility.singleLineHeight * 7; // 7 lines when target is selected
            }
            return EditorGUIUtility.singleLineHeight * 7; // 7 lines when target is not selected (includes help box)
        }
    }

    [CustomPropertyDrawer(typeof(EventionBox.EventionEvent))]
    public class EventionEventDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, int> tabIndices = new(); // Track tab selection per property

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty labelProp = property.FindPropertyRelative("label");

            // Unique property key for tracking tab selection
            string propertyKey = property.propertyPath;
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0;

            // Ensure label is set properly
            string eventLabel = string.IsNullOrEmpty(labelProp.stringValue) ? "[Unnamed Event]" : labelProp.stringValue;

            // Draw foldout
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(eventLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                // **Toolbar with Four Tabs: Main, Events, Audio, Menus**
                string[] toolbarOptions = { "Main", "Events", "Audio", "Menus" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30; // Move below the toolbar

                // **Retrieve Properties**
                SerializedProperty unityEventProp = property.FindPropertyRelative("unityEvent");
                SerializedProperty eventsToFireProp = property.FindPropertyRelative("eventsToFire");
                SerializedProperty menusToShowProp = property.FindPropertyRelative("menusToShow");
                SerializedProperty menusToHideProp = property.FindPropertyRelative("menusToHide");
                SerializedProperty audioEventsProp = property.FindPropertyRelative("audioEvents");
                SerializedProperty timingProp = property.FindPropertyRelative("timing");
                SerializedProperty delayProp = property.FindPropertyRelative("delay");
                SerializedProperty useTimeScaleProp = property.FindPropertyRelative("useTimeScale");
                SerializedProperty createListenerProp = property.FindPropertyRelative("createListener");
                SerializedProperty onceProp = property.FindPropertyRelative("once");

                // **Switch Between Tabs**
                switch (tabIndices[propertyKey])
                {
                    case 0: // **Main Tab**
                        EditorGUI.PropertyField(position, labelProp, new GUIContent("Label"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, timingProp, new GUIContent("Timing"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, delayProp, new GUIContent("Delay"));
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, useTimeScaleProp, new GUIContent("Use Time Scale"));
                        position.y += lineHeight;
                        EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, 40), "Create a listener for this event. This will allow you to call the event from other scripts. Works if Manual.", MessageType.Info);
                        position.y += 40; // Adjust spacing after the info box
                        EditorGUI.PropertyField(position, createListenerProp, new GUIContent("Create Listener"));
                        position.y += lineHeight;
                        var compundedString = labelProp.stringValue.HasValue() ? "EB#_" + labelProp.stringValue : "No Label";
                        EditorGUI.LabelField(position, "Listener Name: [Copy it to call this event]", EditorStyles.boldLabel);
                        position.y += lineHeight;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.TextField(position, compundedString);
                        position.y += lineHeight;
                        EditorGUI.EndDisabledGroup();
                        if (GUI.Button(new Rect(position.x + 15, position.y, 150, 20), "Copy Listener Name"))
                        {
                            EditorGUIUtility.systemCopyBuffer = compundedString;
                            EditorUtility.DisplayDialog("Copied!", "Listener name copied to clipboard.", "OK");
                        }
                        position.y += lineHeight;
                        EditorGUI.PropertyField(position, onceProp, new GUIContent("Once"));
                        break;

                    case 1: // **Events Tab**

                        // draw resonance image
                        GUIContent resonanceImage = new(GraphicLoader.ResonanceSenders);
                        Rect imageRect = new(position.x, position.y, 128, 128);
                        GUI.Label(imageRect, resonanceImage);
                        position.y += 128; // Adjusted height for the image

                        EditorGUI.PropertyField(position, eventsToFireProp, new GUIContent("Events To Fire"), true);
                        position.y += EditorGUI.GetPropertyHeight(eventsToFireProp, true) + 2;
                        EditorGUI.PropertyField(position, unityEventProp, new GUIContent("Unity Event"), true);
                        position.y += EditorGUI.GetPropertyHeight(unityEventProp, true) + 2;
                        break;

                    case 2: // **Audio Tab**
                        EditorGUI.PropertyField(position, audioEventsProp, new GUIContent("Audio Events"), true);
                        position.y += EditorGUI.GetPropertyHeight(audioEventsProp, true) + 2;
                        break;

                    case 3: // **Menus Tab**
                        EditorGUI.PropertyField(position, menusToShowProp, new GUIContent("Menus To Show"), true);
                        position.y += EditorGUI.GetPropertyHeight(menusToShowProp, true) + 2;
                        EditorGUI.PropertyField(position, menusToHideProp, new GUIContent("Menus To Hide"), true);
                        position.y += EditorGUI.GetPropertyHeight(menusToHideProp, true) + 2;
                        break;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 4; // Foldout height

            if (property.isExpanded)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                totalHeight += 30; // Toolbar height

                string propertyKey = property.propertyPath;
                int selectedTab = tabIndices.ContainsKey(propertyKey) ? tabIndices[propertyKey] : 0;

                switch (selectedTab)
                {
                    case 0: // **Main Tab**
                        totalHeight += lineHeight * 9; // Label, Timing, Delay, Use Time Scale, Create Listener, Once
                        totalHeight += 40; // Extra height for Create Listener info box
                        break;

                    case 1: // **Events Tab**
                        totalHeight += 128; // Adjusted height for the image
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("unityEvent"), true) + 2;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("eventsToFire"), true) + 2;
                        break;

                    case 2: // **Audio Tab**
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("audioEvents"), true) + 2;
                        break;

                    case 3: // **Menus Tab**
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("menusToShow"), true) + 2;
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("menusToHide"), true) + 2;
                        break;
                }
            }

            return totalHeight + 4; // Padding
        }
    }

    [CustomPropertyDrawer(typeof(VFXSFX.VFXSFXEntry))]
    public class VFXSFXEntryDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, int> tabIndices = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var labelProp = property.FindPropertyRelative("label");
            var entryLabel = string.IsNullOrEmpty(labelProp.stringValue) ? "[Unnamed Entry]" : labelProp.stringValue;

            // Track selected tab per property instance
            var propertyKey = property.propertyPath;
            if (!tabIndices.ContainsKey(propertyKey))
                tabIndices[propertyKey] = 0;

            // Foldout
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(entryLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;

                // Toolbar
                string[] toolbarOptions = { "Main", "SFX", "VFX", "Evention" };
                tabIndices[propertyKey] = GUI.Toolbar(new Rect(position.x, position.y, position.width, 25), tabIndices[propertyKey], toolbarOptions);
                position.y += 30;

                // Main props
                var timingProp = property.FindPropertyRelative("timing");
                var delayProp = property.FindPropertyRelative("delay");
                var useTimeScaleProp = property.FindPropertyRelative("useTimeScale");
                var createListenerProp = property.FindPropertyRelative("createListener");
                var onceProp = property.FindPropertyRelative("once");

                // SFX props
                var audioEntriesProp = property.FindPropertyRelative("audioEntries");
                var playAllAudioEntriesProp = property.FindPropertyRelative("playAllAudioEntries");
                var audioIndexProp = property.FindPropertyRelative("audioIndex");

                // VFX props
                var pooledEffectPrefabProp = property.FindPropertyRelative("pooledEffectPrefab");
                var pooledEffectLifetimeProp = property.FindPropertyRelative("pooledEffectLifetime");
                var pooledEffectEnabledProp = property.FindPropertyRelative("pooledEffectEnabled");
                var pooledEffectParentToThisProp = property.FindPropertyRelative("pooledEffectParentToThis");
                var pooledEffectOffsetProp = property.FindPropertyRelative("pooledEffectOffset");
                var autoPlaySpawnedParticlesProp = property.FindPropertyRelative("autoPlaySpawnedParticles");
                var playSpawnedParticlesFromChildrenProp = property.FindPropertyRelative("playSpawnedParticlesFromChildren");
                var particleSystemsProp = property.FindPropertyRelative("particleSystems");
                var unityEventProp = property.FindPropertyRelative("unityEvent");

                // Evention props
                var eventsToFireProp = property.FindPropertyRelative("eventsToFire");
                var menusToShowProp = property.FindPropertyRelative("menusToShow");
                var menusToHideProp = property.FindPropertyRelative("menusToHide");

                float lineHeight = EditorGUIUtility.singleLineHeight + 2;

                switch (tabIndices[propertyKey])
                {
                    case 0: // Main
                        position.height = EditorGUI.GetPropertyHeight(labelProp, true);
                        EditorGUI.PropertyField(position, labelProp, new GUIContent("Label"));
                        position.y += position.height + 2;

                        position.height = EditorGUI.GetPropertyHeight(timingProp, true);
                        EditorGUI.PropertyField(position, timingProp, new GUIContent("Timing"));
                        position.y += position.height + 2;

                        position.height = EditorGUI.GetPropertyHeight(delayProp, true);
                        EditorGUI.PropertyField(position, delayProp, new GUIContent("Delay"));
                        position.y += position.height + 2;

                        position.height = EditorGUI.GetPropertyHeight(useTimeScaleProp, true);
                        EditorGUI.PropertyField(position, useTimeScaleProp, new GUIContent("Use Time Scale"));
                        position.y += position.height + 2;

                        float helpBoxHeight = 40;
                        EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, helpBoxHeight),
                            "Enable 'Create Listener' to call this entry from anywhere using a string event. Works when Timing is Manual.",
                            MessageType.Info);
                        position.y += helpBoxHeight + 2;

                        position.height = EditorGUI.GetPropertyHeight(createListenerProp, true);
                        EditorGUI.PropertyField(position, createListenerProp, new GUIContent("Create Listener"));
                        position.y += position.height + 2;

                        var listenerName = labelProp.stringValue.HasValue() ? "VFXSFX#_" + labelProp.stringValue : "No Label";
                        position.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.LabelField(position, "Listener Name: [Copy it to call this entry]", EditorStyles.boldLabel);
                        position.y += position.height + 2;

                        EditorGUI.BeginDisabledGroup(true);
                        position.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.TextField(position, listenerName);
                        position.y += position.height + 2;
                        EditorGUI.EndDisabledGroup();

                        float buttonHeight = 20;
                        if (GUI.Button(new Rect(position.x + 15, position.y, 150, buttonHeight), "Copy Listener Name"))
                        {
                            EditorGUIUtility.systemCopyBuffer = listenerName;
                            EditorUtility.DisplayDialog("Copied!", "Listener name copied to clipboard.", "OK");
                        }
                        position.y += buttonHeight + 2;

                        position.height = EditorGUI.GetPropertyHeight(onceProp, true);
                        EditorGUI.PropertyField(position, onceProp, new GUIContent("Once"));
                        position.y += position.height + 8; // Extra whitespace after Once
                        break;

                    case 1: // SFX
                        position.height = EditorGUI.GetPropertyHeight(playAllAudioEntriesProp, true);
                        EditorGUI.PropertyField(position, playAllAudioEntriesProp, new GUIContent("Play All"));
                        position.y += position.height + 2;
                        if (!playAllAudioEntriesProp.boolValue)
                        {
                            position.height = EditorGUI.GetPropertyHeight(audioIndexProp, true);
                            EditorGUI.PropertyField(position, audioIndexProp, new GUIContent("Audio Index"));
                            position.y += position.height + 2;
                        }
                        position.height = EditorGUI.GetPropertyHeight(audioEntriesProp, true);
                        EditorGUI.PropertyField(position, audioEntriesProp, new GUIContent("Audio Entries"), true);
                        break;

                    case 2: // VFX
                        position.height = EditorGUI.GetPropertyHeight(pooledEffectPrefabProp, true);
                        EditorGUI.PropertyField(position, pooledEffectPrefabProp, new GUIContent("Pooled Effect Prefab"));
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(pooledEffectLifetimeProp, true);
                        EditorGUI.PropertyField(position, pooledEffectLifetimeProp, new GUIContent("Pooled Lifetime"));
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(pooledEffectEnabledProp, true);
                        EditorGUI.PropertyField(position, pooledEffectEnabledProp, new GUIContent("Spawn Enabled"));
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(pooledEffectParentToThisProp, true);
                        EditorGUI.PropertyField(position, pooledEffectParentToThisProp, new GUIContent("Parent To This"));
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(pooledEffectOffsetProp, true);
                        EditorGUI.PropertyField(position, pooledEffectOffsetProp, new GUIContent("Offset"));
                        position.y += position.height + 2;

                        position.height = EditorGUI.GetPropertyHeight(autoPlaySpawnedParticlesProp, true);
                        EditorGUI.PropertyField(position, autoPlaySpawnedParticlesProp, new GUIContent("Auto Play Spawned Particles"));
                        position.y += position.height + 2;
                        if (autoPlaySpawnedParticlesProp.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            position.height = EditorGUI.GetPropertyHeight(playSpawnedParticlesFromChildrenProp, true);
                            EditorGUI.PropertyField(position, playSpawnedParticlesFromChildrenProp, new GUIContent("Search Children"));
                            position.y += position.height + 2;
                            EditorGUI.indentLevel--;
                        }

                        position.height = EditorGUI.GetPropertyHeight(particleSystemsProp, true);
                        EditorGUI.PropertyField(position, particleSystemsProp, new GUIContent("Particle Systems"), true);
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(unityEventProp, true);
                        EditorGUI.PropertyField(position, unityEventProp, new GUIContent("Unity Event"), true);
                        break;

                    case 3: // Evention
                        position.height = EditorGUI.GetPropertyHeight(eventsToFireProp, true);
                        EditorGUI.PropertyField(position, eventsToFireProp, new GUIContent("Events To Fire"), true);
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(menusToShowProp, true);
                        EditorGUI.PropertyField(position, menusToShowProp, new GUIContent("Menus To Show"), true);
                        position.y += position.height + 2;
                        position.height = EditorGUI.GetPropertyHeight(menusToHideProp, true);
                        EditorGUI.PropertyField(position, menusToHideProp, new GUIContent("Menus To Hide"), true);
                        break;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 2; // Foldout

            if (!property.isExpanded)
                return totalHeight;

            totalHeight += 30; // Toolbar
            totalHeight += 2; // Spacing after toolbar

            var propertyKey = property.propertyPath;
            int selectedTab = tabIndices.ContainsKey(propertyKey) ? tabIndices[propertyKey] : 0;

            float spacing = 2f;

            switch (selectedTab)
            {
                case 0: // Main
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("label"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("timing"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("delay"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("useTimeScale"), true) + spacing;
                    totalHeight += 40 + spacing; // HelpBox
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("createListener"), true) + spacing;
                    totalHeight += EditorGUIUtility.singleLineHeight + spacing; // Listener name label
                    totalHeight += EditorGUIUtility.singleLineHeight + spacing; // Listener name text field
                    totalHeight += 20 + spacing; // Copy button
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("once"), true) + 8; // Once field + extra whitespace
                    break;

                case 1: // SFX
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("playAllAudioEntries"), true) + spacing;
                    if (!property.FindPropertyRelative("playAllAudioEntries").boolValue)
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("audioIndex"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("audioEntries"), true);
                    break;

                case 2: // VFX
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("pooledEffectPrefab"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("pooledEffectLifetime"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("pooledEffectEnabled"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("pooledEffectParentToThis"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("pooledEffectOffset"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("autoPlaySpawnedParticles"), true) + spacing;
                    var autoPlaySpawnedParticlesProp = property.FindPropertyRelative("autoPlaySpawnedParticles");
                    if (autoPlaySpawnedParticlesProp.boolValue)
                        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("playSpawnedParticlesFromChildren"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("particleSystems"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("unityEvent"), true);
                    break;

                case 3: // Evention
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("eventsToFire"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("menusToShow"), true) + spacing;
                    totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("menusToHide"), true);
                    break;
            }

            return totalHeight + 2; // Final padding
        }
    }

    [CustomPropertyDrawer(typeof(AudioAsset.AudioEntry))]
    public class AudioEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty keyProp = property.FindPropertyRelative("key");
            SerializedProperty dataProp = property.FindPropertyRelative("data");

            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;
            float yOffset = position.y;

            // **Label For Key Field**
            EditorGUI.LabelField(new Rect(position.x + 2, yOffset + 3, position.width, fieldHeight),
                "Key", EditorStyles.boldLabel);
            yOffset += fieldHeight + spacing;

            // **Key Field (Smaller Width)**
            Rect keyRect = new Rect(position.x + 35, yOffset - fieldHeight, position.width * 0.4f, fieldHeight);
            EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);

            // **Audio Data Field (Expands to Full Width)**
            Rect dataRect = new Rect(position.x, yOffset - 5, position.width, EditorGUI.GetPropertyHeight(dataProp, true));
            EditorGUI.PropertyField(dataRect, dataProp, GUIContent.none, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = 4f;
            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float dataHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("data"), true);
            float keyLabelHeight = EditorGUIUtility.singleLineHeight;

            return fieldHeight + spacing + dataHeight + keyLabelHeight;
        }
    }

    [CustomPropertyDrawer(typeof(AudioAsset.AudioData))]
    public class AudioDataDrawer : PropertyDrawer
    {
        private GUIContent playIcon;

        public AudioDataDrawer()
        {
            playIcon = EditorGUIUtility.IconContent("d_PlayButton");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty clipsProp = property.FindPropertyRelative("clips");
            SerializedProperty volumeProp = property.FindPropertyRelative("volume");
            SerializedProperty loopingProp = property.FindPropertyRelative("looping");
            SerializedProperty categoryProp = property.FindPropertyRelative("category");
            SerializedProperty hapticSettingsProp = property.FindPropertyRelative("hapticSettings");

            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;
            float yOffset = position.y;

            // 🎵 Play Button (Only in Edit Mode)
            if (!Application.isPlaying)
            {
                Rect playButtonRect = new Rect(position.x + position.width - 22, yOffset, 20, 20);
                if (GUI.Button(playButtonRect, playIcon))
                {
                    PlayAudioClip(clipsProp, volumeProp);
                }
                yOffset += 20 + spacing;
            }

            // **Volume (Full Width) with Slider**
            Rect volumeRect = new Rect(position.x, yOffset, position.width, fieldHeight);
            EditorGUI.LabelField(new Rect(position.x, yOffset, 100, fieldHeight), new GUIContent("🔊 Volume"));
            volumeProp.floatValue = EditorGUI.Slider(new Rect(position.x + 100, yOffset, position.width - 100, fieldHeight), volumeProp.floatValue, 0f, 1f);
            yOffset += fieldHeight + spacing;

            // **Looping (Full Width)**
            Rect loopingRect = new Rect(position.x, yOffset, position.width, fieldHeight);
            EditorGUI.PropertyField(loopingRect, loopingProp, new GUIContent("🔁 Looping"));
            yOffset += fieldHeight + spacing;

            // **Mixer Category (Full Width)**
            Rect categoryRect = new Rect(position.x, yOffset, position.width, fieldHeight);
            EditorGUI.PropertyField(categoryRect, categoryProp, new GUIContent("🎛️ Category"));
            yOffset += fieldHeight + spacing;

            // **Haptic Settings (Inline)**
            SerializedProperty hapticEnabledProp = hapticSettingsProp.FindPropertyRelative("enabled");
            
            // Draw the "Enable Haptics" toggle
            Rect enableHapticsRect = new Rect(position.x, yOffset, position.width, fieldHeight);
            EditorGUI.PropertyField(enableHapticsRect, hapticEnabledProp, new GUIContent("⚡ Enable Haptics"));
            yOffset += fieldHeight + spacing;
            
            // If haptics are enabled, draw the rest of the settings inline
            if (hapticEnabledProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                Rect typeRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(typeRect, hapticSettingsProp.FindPropertyRelative("hapticType"), new GUIContent("⚡ Haptic Type"));
                yOffset += fieldHeight + spacing;
                
                Rect intensityRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(intensityRect, hapticSettingsProp.FindPropertyRelative("intensity"), new GUIContent("🔊 Intensity"));
                yOffset += fieldHeight + spacing;
                
                Rect durationRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(durationRect, hapticSettingsProp.FindPropertyRelative("duration"), new GUIContent("⏱️ Duration"));
                yOffset += fieldHeight + spacing;
                
                Rect overrideRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(overrideRect, hapticSettingsProp.FindPropertyRelative("overrideAudioSettings"), new GUIContent("🎵 Override Audio Settings"));
                yOffset += fieldHeight + spacing;
                
                // Test button (only in play mode)
                if (Application.isPlaying)
                {
                    Rect testButtonRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                    if (GUI.Button(testButtonRect, "🎮 Test Haptic"))
                    {
                        HapticType type = (HapticType)hapticSettingsProp.FindPropertyRelative("hapticType").enumValueIndex;
                        float intensity = hapticSettingsProp.FindPropertyRelative("intensity").floatValue;
                        float duration = hapticSettingsProp.FindPropertyRelative("duration").floatValue;
                        HapticsManager.TriggerHaptic(type, intensity, duration);
                        Debug.Log($"[Haptics] Test triggered from AudioData - Type: {type}, Intensity: {intensity:F2}, Duration: {duration:F2}s");
                    }
                    yOffset += fieldHeight + spacing;
                }
                EditorGUI.indentLevel--;
            }

            // **Audio Clips (Full Width, Below Everything)**
            Rect clipsRect = new Rect(position.x, yOffset, position.width, EditorGUI.GetPropertyHeight(clipsProp, true));
            EditorGUI.PropertyField(clipsRect, clipsProp, new GUIContent("🎶 Clips"), true);

            // **Button for clearing all clips**
            if (clipsProp.arraySize > 0)
            {
                yOffset += EditorGUI.GetPropertyHeight(clipsProp, true) + spacing;
                Rect clearButtonRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                if (GUI.Button(clearButtonRect, "Clear Clips"))
                {
                    clipsProp.ClearArray();
                }
            }
            yOffset += fieldHeight + spacing;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = 5f;
            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float clipsHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("clips"), true);
            
            // Calculate haptic settings height based on enabled state
            SerializedProperty hapticSettingsProp = property.FindPropertyRelative("hapticSettings");
            SerializedProperty hapticEnabledProp = hapticSettingsProp.FindPropertyRelative("enabled");
            
            float hapticSettingsHeight = fieldHeight; // Enable Haptics toggle
            
            if (hapticEnabledProp.boolValue)
            {
                // Add height for: Type, Intensity, Duration, Override, Test button
                hapticSettingsHeight += (fieldHeight * 5) + spacing; // 5 fields (including test button)
            }

            float clearButtonHeight = property.FindPropertyRelative("clips").arraySize > 0 ? fieldHeight : 0;

            return (fieldHeight * 3) + (spacing * 3) + clipsHeight + hapticSettingsHeight + clearButtonHeight;
        }

        public void PlayAudioClip(SerializedProperty clipsProp, SerializedProperty volumeProp)
        {
            if (clipsProp.arraySize == 0)
            {
                Debug.LogWarning("No audio clips to play.");
                return;
            }

            AudioClip clip = clipsProp.GetArrayElementAtIndex(Random.Range(0, clipsProp.arraySize)).objectReferenceValue as AudioClip;
            if (clip == null)
            {
                Debug.LogWarning("No audio clip to play.");
                return;
            }

            // Create a hidden GameObject with an AudioSource
            GameObject audioObject = EditorUtility.CreateGameObjectWithHideFlags("Audio Source", HideFlags.HideAndDontSave);
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volumeProp.floatValue;
            audioSource.Play();

            float clipLength = clip.length > 3 ? 3 : clip.length; // Get the clip's duration

            // log an info that the clip will stop early if it is longer than 3 seconds for preview
            if (clip.length > 3)
            {
                Debug.Log($"Previewing audio for {clipLength} seconds instead of {clip.length} seconds.");
            }

            // Simulate a coroutine: Use EditorApplication.update to check when the clip ends
            double destroyTime = EditorApplication.timeSinceStartup + clipLength; // length limit in case we played a long clip, so it doesn't take too long to destroy
            EditorApplication.update += CheckForDeletion;

            void CheckForDeletion()
            {
                if (EditorApplication.timeSinceStartup >= destroyTime)
                {
                    if (audioObject) Object.DestroyImmediate(audioObject);
                    EditorApplication.update -= CheckForDeletion; // Remove update callback
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(MixerDefinition))]
    public class MixerDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty categoryProp = property.FindPropertyRelative("category");
            SerializedProperty volumeParameter = property.FindPropertyRelative("volumeParameter");
            SerializedProperty defaultVolume = property.FindPropertyRelative("defaultVolume");
            SerializedProperty mixerProp = property.FindPropertyRelative("mixer");
            SerializedProperty ignoreListenerProp = property.FindPropertyRelative("ignoreListenerPause");

            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;
            float yOffset = position.y;

            // **Category Dropdown**
            EditorGUI.LabelField(new Rect(position.x + 2, yOffset + 3, position.width, fieldHeight), "Category", EditorStyles.boldLabel);
            yOffset += fieldHeight + spacing;
            Rect categoryRect = new Rect(position.x, yOffset - fieldHeight, position.width, fieldHeight);
            EditorGUI.PropertyField(categoryRect, categoryProp, GUIContent.none);

            // **Volume Parameter Field**
            yOffset += fieldHeight + spacing;
            Rect volumeRect = new Rect(position.x, yOffset - fieldHeight, position.width, fieldHeight);
            EditorGUI.PropertyField(volumeRect, volumeParameter, new GUIContent("🔊 Volume Parameter"));

            // **Default Volume Field**
            yOffset += fieldHeight + spacing;
            Rect defaultVolumeRect = new Rect(position.x, yOffset - fieldHeight, position.width, fieldHeight);
            EditorGUI.PropertyField(defaultVolumeRect, defaultVolume, new GUIContent("🔊 Default Volume"));

            // **Mixer Field**
            yOffset += fieldHeight + spacing;
            Rect mixerRect = new Rect(position.x, yOffset - fieldHeight, position.width, fieldHeight);
            EditorGUI.PropertyField(mixerRect, mixerProp, new GUIContent("🎚️ Audio Mixer Group"));

            // **Ignore Listener Pause Toggle**
            yOffset += fieldHeight + spacing;
            Rect ignoreRect = new Rect(position.x, yOffset - fieldHeight, position.width, fieldHeight);
            EditorGUI.PropertyField(ignoreRect, ignoreListenerProp, new GUIContent("🔇 Ignore Listener Pause"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = 4f;
            float fieldHeight = EditorGUIUtility.singleLineHeight;
            return (fieldHeight + spacing) * 5;
        }
    }

    /// <summary>
    /// Custom property drawer for AudioEntry that uses audio dropdown with integrated haptic settings.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioPlayer.AudioEntry))]
    public class AudioPlayerEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty audioKeyProp = property.FindPropertyRelative("audioKey");
            SerializedProperty hapticSettingsProp = property.FindPropertyRelative("hapticSettings");
            SerializedProperty enableHapticsProp = property.FindPropertyRelative("enableHaptics");

            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;
            float yOffset = position.y;

            // Draw foldout
            position.height = fieldHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                yOffset += fieldHeight + spacing;

                // Audio dropdown
                Rect audioRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                PropertyHelpers.DrawAudioDropdownInline(audioRect, "🎵 Audio", audioKeyProp, property.serializedObject);
                yOffset += fieldHeight + spacing;

                // Enable Haptics toggle
                Rect enableHapticsRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(enableHapticsRect, enableHapticsProp, new GUIContent("🎮 Enable Haptics"));
                yOffset += fieldHeight + spacing;

                // Haptic Settings (only show if haptics are enabled)
                if (enableHapticsProp.boolValue)
                {
                    Rect hapticSettingsRect = new Rect(position.x, yOffset, position.width, EditorGUI.GetPropertyHeight(hapticSettingsProp, true));
                    EditorGUI.PropertyField(hapticSettingsRect, hapticSettingsProp, new GUIContent("⚡ Haptic Settings"), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;
            float bottomMargin = 8f; // Extra margin at the bottom

            if (property.isExpanded)
            {
                SerializedProperty enableHapticsProp = property.FindPropertyRelative("enableHaptics");
                SerializedProperty hapticSettingsProp = property.FindPropertyRelative("hapticSettings");
                
                float expandedHeight = fieldHeight * 2; // Audio dropdown + Enable Haptics toggle
                
                // Add haptic settings height if enabled
                if (enableHapticsProp.boolValue)
                {
                    expandedHeight += EditorGUI.GetPropertyHeight(hapticSettingsProp, true) + spacing;
                }
                
                return fieldHeight + spacing + expandedHeight + bottomMargin; // Foldout + expanded content + bottom margin
            }

            return fieldHeight; // Just the foldout
        }
    }

    /// <summary>
    /// Custom property drawer for HapticSettings
    /// </summary>
    [CustomPropertyDrawer(typeof(HapticSettings))]
    public class HapticSettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty enabledProp = property.FindPropertyRelative("enabled");
            SerializedProperty hapticTypeProp = property.FindPropertyRelative("hapticType");
            SerializedProperty intensityProp = property.FindPropertyRelative("intensity");
            SerializedProperty durationProp = property.FindPropertyRelative("duration");
            SerializedProperty overrideAudioSettingsProp = property.FindPropertyRelative("overrideAudioSettings");

            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;
            float yOffset = position.y;

            // Draw foldout
            position.height = fieldHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                yOffset += fieldHeight + spacing;

                // Haptic Type dropdown
                Rect typeRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(typeRect, hapticTypeProp, new GUIContent("⚡ Haptic Type"));
                yOffset += fieldHeight + spacing;

                // Intensity slider
                Rect intensityRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(intensityRect, intensityProp, new GUIContent("🔊 Intensity"));
                yOffset += fieldHeight + spacing;

                // Duration slider
                Rect durationRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(durationRect, durationProp, new GUIContent("⏱️ Duration"));
                yOffset += fieldHeight + spacing;

                // Override Audio Settings toggle
                Rect overrideRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                EditorGUI.PropertyField(overrideRect, overrideAudioSettingsProp, new GUIContent("🎵 Override Audio Settings"));
                yOffset += fieldHeight + spacing;

                // Test button (only in play mode)
                if (Application.isPlaying)
                {
                    Rect testButtonRect = new Rect(position.x, yOffset, position.width, fieldHeight);
                    if (GUI.Button(testButtonRect, "🎮 Test Haptic"))
                    {
                        TestHaptic(property);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 4f;

            if (property.isExpanded)
            {
                float expandedHeight = fieldHeight * 5; // Type, Intensity, Duration, Override, Test button
                return fieldHeight + spacing + expandedHeight; // Foldout + expanded content
            }

            return fieldHeight; // Just the foldout
        }

        private void TestHaptic(SerializedProperty property)
        {
            SerializedProperty enabledProp = property.FindPropertyRelative("enabled");
            SerializedProperty hapticTypeProp = property.FindPropertyRelative("hapticType");
            SerializedProperty intensityProp = property.FindPropertyRelative("intensity");
            SerializedProperty durationProp = property.FindPropertyRelative("duration");

            if (enabledProp.boolValue)
            {
                HapticType type = (HapticType)hapticTypeProp.enumValueIndex;
                float intensity = intensityProp.floatValue;
                float duration = durationProp.floatValue;

                HapticsManager.TriggerHaptic(type, intensity, duration);
                Debug.Log($"[Haptics] Test triggered - Type: {type}, Intensity: {intensity:F2}, Duration: {duration:F2}s");
            }
            else
            {
                Debug.LogWarning("[Haptics] Cannot test haptic - haptics are disabled");
            }
        }
    }
}