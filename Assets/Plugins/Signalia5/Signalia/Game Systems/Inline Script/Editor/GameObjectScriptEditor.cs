#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.External.Examples;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Editor
{
    [CustomEditor(typeof(GameObjectScript))]
    internal class GameObjectScriptEditor : UnityEditor.Editor
    {
        private SerializedProperty onAwake;
        private SerializedProperty onEnable;
        private SerializedProperty onStart;
        private SerializedProperty onUpdate;
        private SerializedProperty onLateUpdate;
        private SerializedProperty onFixedUpdate;
        private SerializedProperty onBecameVisible;
        private SerializedProperty onBecameInvisible;
        private SerializedProperty onMouseEnter;
        private SerializedProperty onMouseExit;
        private SerializedProperty onMouseDown;
        private SerializedProperty onMouseUp;
        private SerializedProperty onDisable;
        private SerializedProperty onDestroy;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Init", "Runtime", "Interaction", "Cleanup" };

        private void OnEnable()
        {
            // Initialize properties
            onAwake = serializedObject.FindProperty("onAwake");
            onEnable = serializedObject.FindProperty("onEnable");
            onStart = serializedObject.FindProperty("onStart");
            onUpdate = serializedObject.FindProperty("onUpdate");
            onLateUpdate = serializedObject.FindProperty("onLateUpdate");
            onFixedUpdate = serializedObject.FindProperty("onFixedUpdate");
            onBecameVisible = serializedObject.FindProperty("onBecameVisible");
            onBecameInvisible = serializedObject.FindProperty("onBecameInvisible");
            onMouseEnter = serializedObject.FindProperty("onMouseEnter");
            onMouseExit = serializedObject.FindProperty("onMouseExit");
            onMouseDown = serializedObject.FindProperty("onMouseDown");
            onMouseUp = serializedObject.FindProperty("onMouseUp");
            onDisable = serializedObject.FindProperty("onDisable");
            onDestroy = serializedObject.FindProperty("onDestroy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            
            // Header
            EditorGUILayout.LabelField("Inline Script Behaviour", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure inline scripts for different Unity lifecycle events using the tabs below.", MessageType.Info);
            
            EditorGUILayout.Space();

            // Draw tabs
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: // Init
                    DrawInitTab();
                    break;
                case 1: // Runtime
                    DrawRuntimeTab();
                    break;
                case 2: // Interaction
                    DrawInteractionTab();
                    break;
                case 3: // Cleanup
                    DrawCleanupTab();
                    break;
            }

            EditorGUILayout.Space();

            // Footer
            EditorGUILayout.HelpBox("Tip: You can leave any field empty if you don't need that particular event. Only the events you configure will execute.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInitTab()
        {
            EditorGUILayout.LabelField("Initialization Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These events occur when the GameObject is first created or enabled.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            DrawInlineVoidField(onAwake, "On Awake", "Called when the GameObject is created. Use for initialization that doesn't depend on other objects.");
            DrawInlineVoidField(onEnable, "On Enable", "Called when the GameObject becomes active. Use for setup that needs to happen each time the object is enabled.");
            DrawInlineVoidField(onStart, "On Start", "Called before the first frame update. Use for initialization that depends on other objects being ready.");
        }

        private void DrawRuntimeTab()
        {
            EditorGUILayout.LabelField("Runtime Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These events occur continuously during gameplay.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            DrawInlineVoidField(onUpdate, "On Update", "Called every frame. Use for per-frame logic like input handling or movement.");
            DrawInlineVoidField(onLateUpdate, "On Late Update", "Called after all Update methods. Use for camera following or UI updates.");
            DrawInlineVoidField(onFixedUpdate, "On Fixed Update", "Called at fixed intervals for physics. Use for physics-based movement.");
        }

        private void DrawInteractionTab()
        {
            EditorGUILayout.LabelField("Visibility & Interaction Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These events occur based on visibility and user interaction.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Visibility", EditorStyles.miniBoldLabel);
            DrawInlineVoidField(onBecameVisible, "On Became Visible", "Called when the GameObject becomes visible to any camera.");
            DrawInlineVoidField(onBecameInvisible, "On Became Invisible", "Called when the GameObject is no longer visible to any camera.");
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Mouse Interaction", EditorStyles.miniBoldLabel);
            DrawInlineVoidField(onMouseEnter, "On Mouse Enter", "Called when the mouse enters the GameObject's collider.");
            DrawInlineVoidField(onMouseExit, "On Mouse Exit", "Called when the mouse leaves the GameObject's collider.");
            DrawInlineVoidField(onMouseDown, "On Mouse Down", "Called when a mouse button is pressed while over the GameObject.");
            DrawInlineVoidField(onMouseUp, "On Mouse Up", "Called when a mouse button is released while over the GameObject.");
        }

        private void DrawCleanupTab()
        {
            EditorGUILayout.LabelField("Cleanup Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These events occur when the GameObject is disabled or destroyed.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            DrawInlineVoidField(onDisable, "On Disable", "Called when the GameObject becomes inactive. Use for cleanup that should happen each time the object is disabled.");
            DrawInlineVoidField(onDestroy, "On Destroy", "Called when the GameObject is destroyed. Use for final cleanup and resource management.");
        }

        private void DrawInlineVoidField(SerializedProperty property, string label, string tooltip)
        {
            if (property == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Draw field with custom label and tooltip
            var content = new GUIContent(label, tooltip);
            EditorGUILayout.PropertyField(property, content, true);
            
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
