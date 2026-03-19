using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.Utilities.Editors
{
    /// <summary>
    /// Handles drawing the Todo icon in the hierarchy for GameObjects with Todo components.
    /// </summary>
    [InitializeOnLoad]
    public static class TodoHierarchyIcon
    {
        private static Texture2D todoIcon;
        private const float IconSize = 16f;
        private const float IconOffsetFromRight = 40f;
        private static readonly Color TodoBackgroundTint = new(1f, 0.8f, 0.2f, 0.2f);

        static TodoHierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcon;
        }

        private static void DrawHierarchyIcon(int instanceID, Rect selectionRect)
        {
            return; //todo: make play nice with notes icon
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;

            if (gameObject.GetComponent<Todo>() == null) return;

            EditorGUI.DrawRect(selectionRect, TodoBackgroundTint);

            if (todoIcon == null)
            {
                todoIcon = EditorGUIUtility.IconContent("d_console.warnicon").image as Texture2D;
                if (todoIcon == null)
                {
                    todoIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                }
            }

            Rect iconRect = new(
                selectionRect.xMax - IconOffsetFromRight,
                selectionRect.y + (selectionRect.height - IconSize) * 0.5f,
                IconSize,
                IconSize
            );

            if (todoIcon != null)
            {
                GUI.DrawTexture(iconRect, todoIcon, ScaleMode.ScaleToFit, true);
            }
        }
    }

    /// <summary>
    /// Custom Editor for the Todo component.
    /// </summary>
    [CustomEditor(typeof(Todo))]
    [CanEditMultipleObjects]
    public class TodoEditor : Editor
    {
        private SerializedProperty descriptionProperty;
        private Vector2 scrollPosition;

        private static GUIStyle containerStyle;
        private static GUIStyle textAreaStyle;
        private static Texture2D textAreaBackgroundTex;

        private static readonly Color todoTextColor = SSColors.PaleVioletRed;
        private static readonly Color textAreaBackgroundColor = SSColors.LightBlack;

        private void OnEnable()
        {
            descriptionProperty = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            EnsureStylesInitialized();
            serializedObject.Update();

            EditorGUILayout.BeginVertical(containerStyle);
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.MinHeight(60),
                GUILayout.MaxHeight(180)
            );

            EditorGUI.BeginChangeCheck();
            string currentText = EditorGUILayout.TextArea(
                descriptionProperty.stringValue,
                textAreaStyle,
                GUILayout.ExpandHeight(true)
            );
            if (EditorGUI.EndChangeCheck())
            {
                descriptionProperty.stringValue = currentText;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private static void EnsureStylesInitialized()
        {
            if (containerStyle == null)
            {
                containerStyle = new GUIStyle(GUIStyle.none)
                {
                    padding = new RectOffset(15, 15, 10, 15),
                    margin = new RectOffset(5, 5, 5, 5),
                    border = new RectOffset(3, 3, 3, 3),
                };
            }

            if (textAreaStyle == null || textAreaStyle.normal.background == null)
            {
                textAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    stretchHeight = true,
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 5, 0),
                    padding = new RectOffset(8, 8, 8, 8),
                    fontStyle = FontStyle.Bold,
                };

                textAreaStyle.normal.textColor = todoTextColor;
                textAreaStyle.active.textColor = todoTextColor;
                textAreaStyle.hover.textColor = todoTextColor;
                textAreaStyle.focused.textColor = todoTextColor;

                if (textAreaBackgroundTex == null)
                {
                    textAreaBackgroundTex = CreateTexture(1, 1, textAreaBackgroundColor);
                    textAreaBackgroundTex.hideFlags = HideFlags.HideAndDontSave;
                }

                textAreaStyle.normal.background = textAreaBackgroundTex;
                textAreaStyle.active.background = textAreaBackgroundTex;
                textAreaStyle.hover.background = textAreaBackgroundTex;
                textAreaStyle.focused.background = textAreaBackgroundTex;
            }
        }

        private static Texture2D CreateTexture(int width, int height, Color color)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }

    /// <summary>
    /// Editor window that finds Todo components in scene or project assets.
    /// </summary>
    public class TodoFinderWindow : EditorWindow
    {
        private enum SearchScope
        {
            Scene,
            Project
        }

        private class TodoEntry
        {
            public string Description;
            public GameObject Owner;
            public string RootParent;
            public string AssetPath;
        }

        private SearchScope searchScope = SearchScope.Scene;
        private Vector2 scrollPosition;
        private readonly List<TodoEntry> entries = new();

        [MenuItem("Tools/Signalia/Find Todo's")]
        private static void ShowWindow()
        {
            TodoFinderWindow window = GetWindow<TodoFinderWindow>("Signalia Todos");
            window.minSize = new Vector2(500f, 300f);
            window.RefreshResults();
        }

        private void OnEnable()
        {
            RefreshResults();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            SearchScope newScope = (SearchScope)EditorGUILayout.EnumPopup("Search In", searchScope);
            if (newScope != searchScope)
            {
                searchScope = newScope;
                RefreshResults();
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            {
                RefreshResults();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No Todo items found.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (TodoEntry entry in entries)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.Description) ? "(No description)" : entry.Description, EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Attached To", entry.Owner, typeof(GameObject), searchScope == SearchScope.Scene);
                EditorGUILayout.LabelField("Root Parent", entry.RootParent);
                if (!string.IsNullOrEmpty(entry.AssetPath))
                {
                    EditorGUILayout.LabelField("Asset", entry.AssetPath);
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Width(90f)))
                {
                    Selection.activeObject = entry.Owner;
                    EditorGUIUtility.PingObject(entry.Owner);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshResults()
        {
            entries.Clear();

            if (searchScope == SearchScope.Scene)
            {
                Todo[] todos = Object.FindObjectsOfType<Todo>(true);
                foreach (Todo todo in todos)
                {
                    entries.Add(new TodoEntry
                    {
                        Description = todo.Description,
                        Owner = todo.gameObject,
                        RootParent = GetRootParentName(todo.transform),
                        AssetPath = string.Empty
                    });
                }
            }
            else
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefabRoot == null) continue;

                    Todo[] todos = prefabRoot.GetComponentsInChildren<Todo>(true);
                    foreach (Todo todo in todos)
                    {
                        entries.Add(new TodoEntry
                        {
                            Description = todo.Description,
                            Owner = todo.gameObject,
                            RootParent = GetRootParentName(todo.transform),
                            AssetPath = path
                        });
                    }
                }
            }

            entries.Sort((a, b) => string.Compare(a.Description, b.Description, System.StringComparison.OrdinalIgnoreCase));
            Repaint();
        }

        private static string GetRootParentName(Transform transform)
        {
            Transform current = transform;
            while (current.parent != null)
            {
                current = current.parent;
            }

            return current.name;
        }
    }
}
