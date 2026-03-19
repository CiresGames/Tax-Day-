#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities.SIGInput.Editors
{
    [CustomEditor(typeof(SceneInputEventHelper)), CanEditMultipleObjects]
    public sealed class SceneInputEventHelperEditor : Editor
    {
        private SerializedProperty actionNameProp;
        private SerializedProperty triggerProp;
        private SerializedProperty oneFrameConsumeProp;
        private SerializedProperty requireActionEnabledProp;
        private SerializedProperty deadzoneProp;
        private SerializedProperty changeThresholdProp;
        private SerializedProperty onTriggeredProp;
        private SerializedProperty onFloatProp;
        private SerializedProperty onVector2Prop;
        private SerializedProperty sendSignaliaEventProp;
        private SerializedProperty signaliaEventKeyProp;
        private SerializedProperty includeActionNameArgProp;

        private void OnEnable()
        {
            actionNameProp = serializedObject.FindProperty("actionName");
            triggerProp = serializedObject.FindProperty("trigger");
            oneFrameConsumeProp = serializedObject.FindProperty("oneFrameConsume");
            requireActionEnabledProp = serializedObject.FindProperty("requireActionEnabled");
            deadzoneProp = serializedObject.FindProperty("deadzone");
            changeThresholdProp = serializedObject.FindProperty("changeThreshold");
            onTriggeredProp = serializedObject.FindProperty("onTriggered");
            onFloatProp = serializedObject.FindProperty("onFloat");
            onVector2Prop = serializedObject.FindProperty("onVector2");
            sendSignaliaEventProp = serializedObject.FindProperty("sendSignaliaEvent");
            signaliaEventKeyProp = serializedObject.FindProperty("signaliaEventKey");
            includeActionNameArgProp = serializedObject.FindProperty("includeActionNameArg");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "Scene Input Event Helper watches a Signalia input action and fires UnityEvents (and optionally a Signalia radio event). " +
                "Use it for scene-level input triggers without writing code.",
                MessageType.Info);

            GUILayout.Space(6);
            DrawInputSection();
            GUILayout.Space(6);
            DrawTriggerSection();
            GUILayout.Space(6);
            DrawAnalogSection();
            GUILayout.Space(6);
            DrawUnityEventsSection();
            GUILayout.Space(6);
            DrawSignaliaEventSection();
            GUILayout.Space(8);
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInputSection()
        {
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(
                        actionNameProp,
                        new GUIContent(
                            "Action Name",
                            "Must match an action defined in your Signalia Action Maps (Tools > Signalia > Settings > Assets)."));

                    if (GUILayout.Button(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(28), GUILayout.Height(18)))
                    {
                        ShowActionPickerForTargets();
                    }
                }

                var info = GetActionInfoForCurrentTarget();
                if (info.HasValue)
                {
                    string mapList = info.Value.MapNames.Count > 0 ? string.Join(", ", info.Value.MapNames) : "(unknown map)";
                    EditorGUILayout.HelpBox($"Resolved Action: {info.Value.ActionName}\nType: {info.Value.ActionType}\nMaps: {mapList}", MessageType.None);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Ping Action Maps", GUILayout.Width(140), GUILayout.Height(22)))
                        {
                            PingMapsContainingAction(info.Value.ActionName);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Pick an action from your configured Signalia Action Maps.", MessageType.None);
                }
            }
        }

        private void DrawTriggerSection()
        {
            EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(triggerProp, new GUIContent("Trigger Mode", "How this helper triggers events from the action."));

                var trigger = (SceneInputEventHelper.TriggerMode)triggerProp.enumValueIndex;
                bool isEdge = trigger is SceneInputEventHelper.TriggerMode.Down or SceneInputEventHelper.TriggerMode.Up;

                EditorGUILayout.PropertyField(requireActionEnabledProp, new GUIContent("Require Action Enabled", "Only triggers when the action is enabled and in an enabled action map."));

                if (isEdge)
                {
                    EditorGUILayout.PropertyField(oneFrameConsumeProp, new GUIContent("One Frame Consume", "Consumes the edge so only one helper can react per frame."));
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(oneFrameConsumeProp, new GUIContent("One Frame Consume", "Only used for Down/Up."));
                    }
                }
            }
        }

        private void DrawAnalogSection()
        {
            var trigger = (SceneInputEventHelper.TriggerMode)triggerProp.enumValueIndex;
            bool usesDeadzone = trigger is SceneInputEventHelper.TriggerMode.FloatNonZero or SceneInputEventHelper.TriggerMode.Vector2NonZero;
            bool usesChanged = trigger is SceneInputEventHelper.TriggerMode.FloatChanged or SceneInputEventHelper.TriggerMode.Vector2Changed;

            if (!usesDeadzone && !usesChanged)
            {
                return;
            }

            EditorGUILayout.LabelField("Analog Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (usesDeadzone)
                {
                    EditorGUILayout.PropertyField(deadzoneProp, new GUIContent("Deadzone", "Threshold for NonZero triggers."));
                }

                if (usesChanged)
                {
                    EditorGUILayout.PropertyField(changeThresholdProp, new GUIContent("Change Threshold", "Delta threshold for Changed triggers."));
                }
            }
        }

        private void DrawUnityEventsSection()
        {
            EditorGUILayout.LabelField("Unity Events", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(onTriggeredProp, new GUIContent("On Triggered"), true);

                var trigger = (SceneInputEventHelper.TriggerMode)triggerProp.enumValueIndex;
                bool showFloat = trigger is SceneInputEventHelper.TriggerMode.FloatNonZero or SceneInputEventHelper.TriggerMode.FloatChanged;
                bool showVector2 = trigger is SceneInputEventHelper.TriggerMode.Vector2NonZero or SceneInputEventHelper.TriggerMode.Vector2Changed;

                if (showFloat)
                {
                    EditorGUILayout.PropertyField(onFloatProp, new GUIContent("On Float"), true);
                }

                if (showVector2)
                {
                    EditorGUILayout.PropertyField(onVector2Prop, new GUIContent("On Vector2"), true);
                }
            }
        }

        private void DrawSignaliaEventSection()
        {
            EditorGUILayout.LabelField("Signalia Event (Optional)", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(sendSignaliaEventProp, new GUIContent("Send Signalia Event", "Also broadcast via SIGS.Send when triggered."));
                if (!sendSignaliaEventProp.boolValue)
                {
                    return;
                }

                EditorGUILayout.PropertyField(signaliaEventKeyProp, new GUIContent("Event Key", "String key used by SIGS.Send."));
                EditorGUILayout.PropertyField(includeActionNameArgProp, new GUIContent("Include Action Name Arg", "If true, includes the action name as the first argument."));

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Auto-fill Key", GUILayout.Width(120), GUILayout.Height(22)))
                    {
                        AutoFillEventKeyForTargets();
                    }
                }
            }
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                bool anyIssue = false;
                foreach (var obj in targets)
                {
                    if (obj is not SceneInputEventHelper helper)
                    {
                        continue;
                    }

                    if (DrawDiagnosticsFor(helper))
                    {
                        anyIssue = true;
                        break; // avoid flooding when multi-editing
                    }
                }

                if (!anyIssue)
                {
                    EditorGUILayout.HelpBox("No issues detected for the selected object(s).", MessageType.Info);
                }
            }
        }

        private bool DrawDiagnosticsFor(SceneInputEventHelper helper)
        {
            string actionName = GetPrivateStringField(helper, "actionName");
            var trigger = (SceneInputEventHelper.TriggerMode)GetPrivateEnumField(helper, "trigger");
            bool sendSignaliaEvent = GetPrivateBoolField(helper, "sendSignaliaEvent");
            string eventKey = GetPrivateStringField(helper, "signaliaEventKey");

            if (string.IsNullOrWhiteSpace(actionName))
            {
                EditorGUILayout.HelpBox("Action Name is empty. This helper will never trigger.", MessageType.Error);
                return true;
            }

            var actionMaps = ResourceHandler.GetInputActionMaps() ?? Array.Empty<SignaliaActionMap>();
            if (actionMaps.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Signalia Action Maps are available. Assign Input Action Maps in your Signalia Config (Tools > Signalia > Settings > Assets), " +
                    "or use the 'Load Input Action Maps' utility to auto-populate the config.",
                    MessageType.Warning);

#if UNITY_EDITOR
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Load Input Action Maps", GUILayout.Width(170), GUILayout.Height(22)))
                    {
                        ResourceHandler.LoadInputActionMaps();
                    }
                }
#endif

                return true;
            }

            if (!TryResolveAction(actionName, out var actionType, out var mapNames))
            {
                EditorGUILayout.HelpBox(
                    $"Action '{actionName}' was not found in your configured Input Action Maps. " +
                    "Signalia will not read this action until it exists in a map.",
                    MessageType.Error);
                return true;
            }

            // Type compatibility checks
            if (trigger is SceneInputEventHelper.TriggerMode.Down or SceneInputEventHelper.TriggerMode.Up)
            {
                if (actionType != SignaliaActionType.Bool)
                {
                    EditorGUILayout.HelpBox(
                        $"Trigger '{trigger}' expects a Bool action, but '{actionName}' is '{actionType}'. " +
                        "Use a Bool action or switch to a Float/Vector2 trigger mode.",
                        MessageType.Error);
                    return true;
                }
            }

            if (trigger is SceneInputEventHelper.TriggerMode.FloatNonZero or SceneInputEventHelper.TriggerMode.FloatChanged)
            {
                if (actionType != SignaliaActionType.Float)
                {
                    EditorGUILayout.HelpBox(
                        $"Trigger '{trigger}' expects a Float action, but '{actionName}' is '{actionType}'.",
                        MessageType.Error);
                    return true;
                }

                float deadzone = GetPrivateFloatField(helper, "deadzone");
                float changeThreshold = GetPrivateFloatField(helper, "changeThreshold");
                if (trigger == SceneInputEventHelper.TriggerMode.FloatNonZero && deadzone < 0f)
                {
                    EditorGUILayout.HelpBox("Deadzone is negative. This will behave like always-trigger. Use a non-negative deadzone.", MessageType.Error);
                    return true;
                }
                if (trigger == SceneInputEventHelper.TriggerMode.FloatChanged && changeThreshold < 0f)
                {
                    EditorGUILayout.HelpBox("Change Threshold is negative. This will behave like always-trigger. Use a non-negative threshold.", MessageType.Error);
                    return true;
                }
            }

            if (trigger is SceneInputEventHelper.TriggerMode.Vector2NonZero or SceneInputEventHelper.TriggerMode.Vector2Changed)
            {
                if (actionType != SignaliaActionType.Vector2)
                {
                    EditorGUILayout.HelpBox(
                        $"Trigger '{trigger}' expects a Vector2 action, but '{actionName}' is '{actionType}'.",
                        MessageType.Error);
                    return true;
                }

                float deadzone = GetPrivateFloatField(helper, "deadzone");
                float changeThreshold = GetPrivateFloatField(helper, "changeThreshold");
                if (trigger == SceneInputEventHelper.TriggerMode.Vector2NonZero && deadzone < 0f)
                {
                    EditorGUILayout.HelpBox("Deadzone is negative. This will behave like always-trigger. Use a non-negative deadzone.", MessageType.Error);
                    return true;
                }
                if (trigger == SceneInputEventHelper.TriggerMode.Vector2Changed && changeThreshold < 0f)
                {
                    EditorGUILayout.HelpBox("Change Threshold is negative. This will behave like always-trigger. Use a non-negative threshold.", MessageType.Error);
                    return true;
                }
            }

            if (trigger == SceneInputEventHelper.TriggerMode.Held && actionType != SignaliaActionType.Bool)
            {
                EditorGUILayout.HelpBox(
                    $"Trigger '{trigger}' uses the bool held-state. For '{actionType}' actions, prefer NonZero/Changed triggers for more intuitive behavior.",
                    MessageType.Info);
            }

            if (sendSignaliaEvent && string.IsNullOrWhiteSpace(eventKey))
            {
                EditorGUILayout.HelpBox("Send Signalia Event is enabled, but Event Key is empty.", MessageType.Error);
                return true;
            }

            // Runtime hints
            if (Application.isPlaying)
            {
                if (!SignaliaInputWrapper.Exists)
                {
                    EditorGUILayout.HelpBox("Play Mode: No SignaliaInputWrapper exists in the scene, so input cannot be read.", MessageType.Warning);
                    return true;
                }

                bool enabled = SIGS.IsInputActionEnabled(actionName);
                if (!enabled)
                {
                    string mapList = mapNames.Count > 0 ? string.Join(", ", mapNames) : "(unknown map)";
                    EditorGUILayout.HelpBox($"Play Mode: '{actionName}' is currently disabled (or all containing maps are disabled/suppressed).\nMaps: {mapList}", MessageType.Warning);
                }

                if (SIGS.IsInputOnCooldown(actionName))
                {
                    EditorGUILayout.HelpBox($"Play Mode: '{actionName}' is on cooldown.", MessageType.Info);
                }
            }

            return false;
        }

        private void ShowActionPickerForTargets()
        {
            var allActions = GetAllConfiguredActionNames();
            if (allActions.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Input Actions",
                    "No input actions found.\n\nMake sure your Signalia Config has Input Action Maps assigned (Tools > Signalia > Settings > Assets).",
                    "OK");
                return;
            }

            // Always show full list (dialog includes its own filter UI)
            _ = SimpleSearchHelpers.FindClosestMatch("", allActions, out List<string> matches);
            SimpleSearchHelpers.ShowMultipleMatchDialog("Select Input Action", matches, selected =>
            {
                if (string.IsNullOrWhiteSpace(selected))
                {
                    return;
                }

                foreach (var t in targets)
                {
                    if (t is not SceneInputEventHelper)
                    {
                        continue;
                    }

                    Undo.RecordObject(t, "Set Input Action Name");
                    var so = new SerializedObject(t);
                    so.Update();
                    var prop = so.FindProperty("actionName");
                    prop.stringValue = selected;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(t);
                }
            });
        }

        private void AutoFillEventKeyForTargets()
        {
            foreach (var t in targets)
            {
                if (t is not SceneInputEventHelper helper)
                {
                    continue;
                }

                string actionName = GetPrivateStringField(helper, "actionName");
                var trigger = (SceneInputEventHelper.TriggerMode)GetPrivateEnumField(helper, "trigger");

                string suffix = string.IsNullOrWhiteSpace(actionName) ? "Action" : actionName.Trim();
                string suggested = $"Input/{suffix}/{trigger}";

                Undo.RecordObject(t, "Auto-fill Signalia Event Key");
                var so = new SerializedObject(t);
                so.Update();
                var prop = so.FindProperty("signaliaEventKey");
                prop.stringValue = suggested;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(t);
            }
        }

        private ActionInfo? GetActionInfoForCurrentTarget()
        {
            if (target is not SceneInputEventHelper helper)
            {
                return null;
            }

            string actionName = GetPrivateStringField(helper, "actionName");
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return null;
            }

            if (!TryResolveAction(actionName, out var actionType, out var mapNames))
            {
                return null;
            }

            return new ActionInfo(actionName, actionType, mapNames);
        }

        private static List<string> GetAllConfiguredActionNames()
        {
            var maps = ResourceHandler.GetInputActionMaps() ?? Array.Empty<SignaliaActionMap>();
            var set = new HashSet<string>();

            foreach (var map in maps)
            {
                if (map == null || map.Actions == null)
                {
                    continue;
                }

                foreach (var action in map.Actions)
                {
                    if (action == null || string.IsNullOrWhiteSpace(action.ActionName))
                    {
                        continue;
                    }

                    set.Add(action.ActionName);
                }
            }

            var list = set.ToList();
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private static bool TryResolveAction(string actionName, out SignaliaActionType actionType, out List<string> mapNames)
        {
            actionType = SignaliaActionType.Bool;
            mapNames = new List<string>();

            if (string.IsNullOrWhiteSpace(actionName))
            {
                return false;
            }

            var maps = ResourceHandler.GetInputActionMaps() ?? Array.Empty<SignaliaActionMap>();
            bool found = false;

            foreach (var map in maps)
            {
                if (map == null)
                {
                    continue;
                }

                if (map.TryGetAction(actionName, out var def) && def != null)
                {
                    found = true;
                    actionType = def.ActionType;

                    string mapName = string.IsNullOrWhiteSpace(map.MapName) ? map.name : map.MapName;
                    if (!string.IsNullOrWhiteSpace(mapName) && !mapNames.Contains(mapName))
                    {
                        mapNames.Add(mapName);
                    }
                }
            }

            return found;
        }

        private static void PingMapsContainingAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return;
            }

            var maps = ResourceHandler.GetInputActionMaps() ?? Array.Empty<SignaliaActionMap>();
            var foundMaps = new List<SignaliaActionMap>();

            foreach (var map in maps)
            {
                if (map == null)
                {
                    continue;
                }

                if (map.TryGetAction(actionName, out _))
                {
                    foundMaps.Add(map);
                }
            }

            if (foundMaps.Count == 0)
            {
                return;
            }

            // Prefer pinging the first, but select all.
            Selection.objects = foundMaps.Cast<UnityEngine.Object>().ToArray();
            EditorGUIUtility.PingObject(foundMaps[0]);
        }

        private static string GetPrivateStringField(SceneInputEventHelper helper, string fieldName)
        {
            var so = new SerializedObject(helper);
            so.Update();
            var prop = so.FindProperty(fieldName);
            return prop != null ? prop.stringValue : "";
        }

        private static bool GetPrivateBoolField(SceneInputEventHelper helper, string fieldName)
        {
            var so = new SerializedObject(helper);
            so.Update();
            var prop = so.FindProperty(fieldName);
            return prop != null && prop.boolValue;
        }

        private static float GetPrivateFloatField(SceneInputEventHelper helper, string fieldName)
        {
            var so = new SerializedObject(helper);
            so.Update();
            var prop = so.FindProperty(fieldName);
            return prop != null ? prop.floatValue : 0f;
        }

        private static int GetPrivateEnumField(SceneInputEventHelper helper, string fieldName)
        {
            var so = new SerializedObject(helper);
            so.Update();
            var prop = so.FindProperty(fieldName);
            return prop != null ? prop.enumValueIndex : 0;
        }

        private readonly struct ActionInfo
        {
            public ActionInfo(string actionName, SignaliaActionType actionType, List<string> mapNames)
            {
                ActionName = actionName;
                ActionType = actionType;
                MapNames = mapNames ?? new List<string>();
            }

            public string ActionName { get; }
            public SignaliaActionType ActionType { get; }
            public List<string> MapNames { get; }
        }
    }
}
#endif

