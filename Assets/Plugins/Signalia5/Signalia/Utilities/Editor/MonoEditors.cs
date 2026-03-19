using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using System.Linq; // Assuming SSColors and GraphicLoader are here

namespace AHAKuo.Signalia.Utilities.Editors
{
    /// <summary>
    /// Handles drawing the Notes icon in the hierarchy for GameObjects with Notes components.
    /// Also applies a slight yellow tint to the hierarchy item background.
    /// </summary>
    [InitializeOnLoad]
    public static class NotesHierarchyIcon
    {
        private static Texture2D notesIcon;
        private const float ICON_SIZE = 16f;
        // Offset from right edge to avoid overlapping Unity's built-in icons (prefab, visibility toggle, etc.)
        private const float ICON_OFFSET_FROM_RIGHT = 40f;

        static NotesHierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcon;
        }

        private static Object IdToObject(int id)
        {
#if UNITY_6000_0_OR_NEWER
            return EditorUtility.InstanceIDToObject(id) ;
#else
            return EditorUtility.InstanceIDToObject(id);
#endif
        }

        private static bool IsSelected(int id)
        {
#if UNITY_6000_0_OR_NEWER
            return Selection.instanceIDs.Contains(id);
#else
            return Selection.instanceIDs.Contains(id);
#endif
        }

        private static void DrawHierarchyIcon(int instanceID, Rect selectionRect)
        {
            // Get the GameObject from the instance ID
            GameObject gameObject = IdToObject(instanceID) as GameObject;
            if (gameObject == null) return;

            // Check if the GameObject has a Notes component
            Notes notesComponent = gameObject.GetComponent<Notes>();
            if (notesComponent == null) return;

            // Get colors from preferences
            SignaliaPreferences prefs = PreferencesReader.GetPreferences();
            Color backgroundColor = prefs.NotesHierarchyBackgroundColor;
            Color textColor = prefs.NotesHierarchyTextColor;

            // Draw background tint for the hierarchy item
            EditorGUI.DrawRect(selectionRect, backgroundColor);

            // Draw custom text color if specified (alpha > 0)
            // Note: We draw this with a style that matches Unity's hierarchy label
            if (textColor.a > 0f)
            {
                // Check if this item is selected to use appropriate style
                bool isSelected = IsSelected(instanceID);
                GUIStyle labelStyle = isSelected ? EditorStyles.whiteLabel : EditorStyles.label;
                
                // Calculate text rect (offset to account for hierarchy icons on the left)
                // Unity typically starts text around x + 16-20 pixels
                Rect textRect = new Rect(
                    selectionRect.x + 20f, // Offset for expand/collapse and icon
                    selectionRect.y,
                    selectionRect.width - 20f,
                    selectionRect.height
                );

                // Save original color
                Color originalColor = GUI.color;
                
                // Draw text with custom color
                // We use a semi-transparent overlay approach to avoid completely hiding Unity's text
                // This creates a tinted effect rather than full replacement
                if (textColor.a < 1f)
                {
                    // For semi-transparent, draw a tinted overlay
                    GUI.color = textColor;
                    GUI.Label(textRect, gameObject.name, labelStyle);
                }
                else
                {
                    // For fully opaque, replace the text
                    GUI.color = textColor;
                    GUI.Label(textRect, gameObject.name, labelStyle);
                }
                
                GUI.color = originalColor; // Reset color
            }

            // Load the icon if not already loaded
            if (notesIcon == null)
            {
                // Load the hierarchy icon specifically
                notesIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    FrameworkConstants.GraphicPaths.ICON_UTILITY_NOTES);
            }

            // If we still don't have an icon, use a Unity built-in icon as fallback
            if (notesIcon == null)
            {
                notesIcon = EditorGUIUtility.IconContent("d_TextAsset Icon").image as Texture2D;
            }

            // Calculate icon position (right side of the hierarchy item, before Unity's built-in icons)
            Rect iconRect = new Rect(
                selectionRect.xMax - ICON_OFFSET_FROM_RIGHT,
                selectionRect.y + (selectionRect.height - ICON_SIZE) * 0.5f,
                ICON_SIZE,
                ICON_SIZE
            );

            // Draw the icon
            if (notesIcon != null)
            {
                GUI.DrawTexture(iconRect, notesIcon, ScaleMode.ScaleToFit, true);
            }
        }
    }
    /// <summary>
    /// Custom Editor for the Notes component. Provides a styled note-taking area
    /// in the Inspector with a header image and a custom theme.
    /// </summary>
    [CustomEditor(typeof(Notes))]
    [CanEditMultipleObjects]
    public class NotesEditor : Editor
    {
        private SerializedProperty noteContentProperty;
        private Vector2 scrollPosition;

        // --- Style Variables ---
        // Made static so they persist across inspector updates for the same session,
        // but will be null after domain reload, triggering reinitialization.
        private static GUIStyle containerStyle;
        private static GUIStyle textAreaStyle;

        // --- Configurable Colors (Example using SSColors) ---
        // Make these static if SSColors are constant, otherwise keep instance-level
        private static readonly Color noteTextColor = SSColors.Wheat; // Changed from Wheat to Black based on user code
        private static readonly Color separatorColor = SSColors.HotPink;
        private static readonly Color textAreaBackgroundColor = SSColors.LightBlack;

        // Store generated background texture - needs careful management
        // Made static to reduce recreation, but needs cleanup on domain reload/editor close.
        private static Texture2D textAreaBackgroundTex;

        private void OnEnable()
        {
            noteContentProperty = serializedObject.FindProperty("noteContent");
        }

        private static void EnsureStylesInitialized()
        {
            // Initialize only if styles haven't been created yet in this session
            // or if they were lost due to domain reload (static variables reset).

            // --- Container Box Style (Post-it Look) ---
            if (containerStyle == null)
            {
                containerStyle = new GUIStyle(GUIStyle.none) // Start clean
                {
                    padding = new RectOffset(15, 15, 10, 15), // More padding
                    margin = new RectOffset(5, 5, 5, 5),
                    border = new RectOffset(3, 3, 3, 3), // Slightly thicker border for definition
                };
            }

            // --- Text Area Style ---
            // Check if null or if the background texture reference is lost/invalid
            if (textAreaStyle == null || textAreaStyle.normal.background == null)
            {
                // Access EditorStyles.textArea *here* where it's safer (called from OnInspectorGUI)
                textAreaStyle = new GUIStyle(EditorStyles.textArea) // Now safe to access
                {
                    wordWrap = true,
                    stretchHeight = true,
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 5, 0), // Margin above text area
                    padding = new RectOffset(8, 8, 8, 8), // Padding inside text area
                    fontStyle = FontStyle.Bold, // Normal font style
                };


                // Set text color for different states
                textAreaStyle.normal.textColor = noteTextColor;
                textAreaStyle.active.textColor = noteTextColor;
                textAreaStyle.hover.textColor = noteTextColor;
                textAreaStyle.focused.textColor = noteTextColor;

                if (textAreaBackgroundTex == null)
                {
                    textAreaBackgroundTex = CreateTexture(1, 1, textAreaBackgroundColor); // Use helper
                    // Important: Hide flags for static textures to prevent saving/leaking
                    textAreaBackgroundTex.hideFlags = HideFlags.HideAndDontSave;
                }

                // Assign the background texture to all states
                textAreaStyle.normal.background = textAreaBackgroundTex;
                textAreaStyle.active.background = textAreaBackgroundTex;
                textAreaStyle.hover.background = textAreaBackgroundTex;
                textAreaStyle.focused.background = textAreaBackgroundTex;
            }
        }


        public override void OnInspectorGUI()
        {
            // Ensure styles are initialized before drawing the GUI that uses them
            EnsureStylesInitialized();

            serializedObject.Update();

            // --- Draw the Custom Inspector ---
            // Use BeginVertical with the ensured containerStyle
            EditorGUILayout.BeginVertical(containerStyle);

            // --- Note Taking Area ---
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.MinHeight(50), // Minimum height
                GUILayout.MaxHeight(150)  // Maximum height to prevent excessive expansion
                );

            EditorGUI.BeginChangeCheck(); // Check if the text area value changes

            // Use the ensured textAreaStyle
            string currentText = EditorGUILayout.TextArea(
                noteContentProperty.stringValue,
                textAreaStyle, // Use the initialized style
                GUILayout.ExpandHeight(true) // Allows vertical expansion within ScrollView limits
                );

            if (EditorGUI.EndChangeCheck()) // If changed, update the property
            {
                noteContentProperty.stringValue = currentText;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical(); // End main container

            serializedObject.ApplyModifiedProperties();
        }

        // Helper to create a simple 1x1 texture with a color
        // Made static as it doesn't depend on instance state
        private static Texture2D CreateTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            // Hide flags are set where it's called if the texture is static
            return texture;
        }

        // Helper function to draw a separator line
        // Made static as it doesn't depend on instance state
        private static void DrawSeparator(Color color, int thickness = 1, int padding = 5)
        {
            // Ensure containerStyle is initialized before accessing padding
            if (containerStyle == null) EnsureStylesInitialized();
            // Add extra null check for safety, though EnsureStylesInitialized should cover it
            if (containerStyle == null) return;

            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            // Use padding values safely
            r.x -= containerStyle.padding.left;
            r.width += containerStyle.padding.horizontal;
            EditorGUI.DrawRect(r, color);
        }
    }

    /// <summary>
    /// Custom Editor for SimpleHovering component. Provides a clean inspector interface
    /// for configuring the hovering animation settings.
    /// </summary>
    [CustomEditor(typeof(SimpleHovering))]
    [CanEditMultipleObjects]
    public class SimpleHoveringEditor : Editor
    {
        private SerializedProperty playOnStartProp;
        private SerializedProperty hoverDistanceProp;
        private SerializedProperty durationProp;
        private SerializedProperty useUnscaledTimeProp;
        private SerializedProperty useYoyoLoopProp;

        private void OnEnable()
        {
            playOnStartProp = serializedObject.FindProperty("playOnStart");
            hoverDistanceProp = serializedObject.FindProperty("hoverDistance");
            durationProp = serializedObject.FindProperty("duration");
            useUnscaledTimeProp = serializedObject.FindProperty("useUnscaledTime");
            useYoyoLoopProp = serializedObject.FindProperty("useYoyoLoop");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header image
            Texture2D headerImage = GraphicLoader.SmallAnimationHovering;
            if (headerImage != null)
            {
                GUILayout.Label(headerImage, GUILayout.Height(120));
            }
            else
            {
                EditorGUILayout.HelpBox("Simple Hovering Animation", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            if (playOnStartProp != null)
                EditorGUILayout.PropertyField(playOnStartProp, new GUIContent("Play On Start", "Automatically start the animation when the component is enabled"));
            if (hoverDistanceProp != null)
                EditorGUILayout.PropertyField(hoverDistanceProp, new GUIContent("Hover Distance", "Distance in units the object will move up and down"));
            if (durationProp != null)
                EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration", "Time in seconds for one complete hover cycle"));
            if (useYoyoLoopProp != null)
                EditorGUILayout.PropertyField(useYoyoLoopProp, new GUIContent("Use Yoyo Loop", "If true, animation loops back and forth (ping-pong). If false, restarts from beginning."));
            if (useUnscaledTimeProp != null)
                EditorGUILayout.PropertyField(useUnscaledTimeProp, new GUIContent("Use Unscaled Time", "If true, animation continues even when Time.timeScale is 0"));

            EditorGUILayout.Space(10);

            // Runtime controls
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                SimpleHovering hovering = (SimpleHovering)target;
                if (GUILayout.Button("Play"))
                {
                    hovering.Play();
                }
                if (GUILayout.Button("Stop"))
                {
                    hovering.Stop();
                }
                if (GUILayout.Button("Pause"))
                {
                    hovering.Pause();
                }
                if (GUILayout.Button("Resume"))
                {
                    hovering.Resume();
                }
                
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Custom Editor for SimpleRotation component. Provides a clean inspector interface
    /// for configuring the rotation animation settings.
    /// </summary>
    [CustomEditor(typeof(SimpleRotation))]
    [CanEditMultipleObjects]
    public class SimpleRotationEditor : Editor
    {
        private SerializedProperty playOnStartProp;
        private SerializedProperty rotationAxisProp;
        private SerializedProperty rotationSpeedProp;
        private SerializedProperty useUnscaledTimeProp;

        private void OnEnable()
        {
            playOnStartProp = serializedObject.FindProperty("playOnStart");
            rotationAxisProp = serializedObject.FindProperty("rotationAxis");
            rotationSpeedProp = serializedObject.FindProperty("rotationSpeed");
            useUnscaledTimeProp = serializedObject.FindProperty("useUnscaledTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header image
            Texture2D headerImage = GraphicLoader.SmallAnimationRotation;
            if (headerImage != null)
            {
                GUILayout.Label(headerImage, GUILayout.Height(120));
            }
            else
            {
                EditorGUILayout.HelpBox("Simple Rotation Animation", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            if (playOnStartProp != null)
                EditorGUILayout.PropertyField(playOnStartProp, new GUIContent("Play On Start", "Automatically start the animation when the component is enabled"));
            if (rotationAxisProp != null)
                EditorGUILayout.PropertyField(rotationAxisProp, new GUIContent("Rotation Axis", "Axis around which the object will rotate (e.g., Vector3.up for Y-axis)"));
            if (rotationSpeedProp != null)
                EditorGUILayout.PropertyField(rotationSpeedProp, new GUIContent("Rotation Speed", "Rotation speed in degrees per second"));
            if (useUnscaledTimeProp != null)
                EditorGUILayout.PropertyField(useUnscaledTimeProp, new GUIContent("Use Unscaled Time", "If true, animation continues even when Time.timeScale is 0"));

            EditorGUILayout.Space(10);

            // Runtime controls
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                SimpleRotation rotation = (SimpleRotation)target;
                if (GUILayout.Button("Play"))
                {
                    rotation.Play();
                }
                if (GUILayout.Button("Stop"))
                {
                    rotation.Stop();
                }
                if (GUILayout.Button("Pause"))
                {
                    rotation.Pause();
                }
                if (GUILayout.Button("Resume"))
                {
                    rotation.Resume();
                }
                
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Custom Editor for SimpleScale component. Provides a clean inspector interface
    /// for configuring the scale animation settings.
    /// </summary>
    [CustomEditor(typeof(SimpleScale))]
    [CanEditMultipleObjects]
    public class SimpleScaleEditor : Editor
    {
        private SerializedProperty playOnStartProp;
        private SerializedProperty targetScaleProp;
        private SerializedProperty durationProp;
        private SerializedProperty useUnscaledTimeProp;
        private SerializedProperty useYoyoLoopProp;

        private void OnEnable()
        {
            playOnStartProp = serializedObject.FindProperty("playOnStart");
            targetScaleProp = serializedObject.FindProperty("targetScale");
            durationProp = serializedObject.FindProperty("duration");
            useUnscaledTimeProp = serializedObject.FindProperty("useUnscaledTime");
            useYoyoLoopProp = serializedObject.FindProperty("useYoyoLoop");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header image
            Texture2D headerImage = GraphicLoader.SmallAnimationScale;
            if (headerImage != null)
            {
                GUILayout.Label(headerImage, GUILayout.Height(120));
            }
            else
            {
                EditorGUILayout.HelpBox("Simple Scale Animation", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            if (playOnStartProp != null)
                EditorGUILayout.PropertyField(playOnStartProp, new GUIContent("Play On Start", "Automatically start the animation when the component is enabled"));
            if (targetScaleProp != null)
                EditorGUILayout.PropertyField(targetScaleProp, new GUIContent("Target Scale", "Target scale the object will animate to"));
            if (durationProp != null)
                EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration", "Time in seconds for one complete scale cycle"));
            if (useYoyoLoopProp != null)
                EditorGUILayout.PropertyField(useYoyoLoopProp, new GUIContent("Use Yoyo Loop", "If true, animation loops back and forth (ping-pong). If false, restarts from beginning."));
            if (useUnscaledTimeProp != null)
                EditorGUILayout.PropertyField(useUnscaledTimeProp, new GUIContent("Use Unscaled Time", "If true, animation continues even when Time.timeScale is 0"));

            EditorGUILayout.Space(10);

            // Runtime controls
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                SimpleScale scale = (SimpleScale)target;
                if (GUILayout.Button("Play"))
                {
                    scale.Play();
                }
                if (GUILayout.Button("Stop"))
                {
                    scale.Stop();
                }
                if (GUILayout.Button("Pause"))
                {
                    scale.Pause();
                }
                if (GUILayout.Button("Resume"))
                {
                    scale.Resume();
                }
                
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}