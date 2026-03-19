using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.ResourceCaching;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.ResourceCaching.Editors
{
    /// <summary>
    /// Property drawer for ResourceAsset references in the SignaliaConfigAsset.
    /// Provides a user-friendly interface for managing ResourceAsset arrays.
    /// </summary>
    [CustomPropertyDrawer(typeof(ResourceAsset))]
    public class ResourceAssetPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the object field
            property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, 
                typeof(ResourceAsset), false);

            // Add context menu for quick actions
            if (property.objectReferenceValue != null)
            {
                var resourceAsset = (ResourceAsset)property.objectReferenceValue;
                var buttonRect = new Rect(position.xMax - 20, position.y, 20, position.height);
                
                if (GUI.Button(buttonRect, "⚙"))
                {
                    ShowContextMenu(property, resourceAsset);
                }
            }

            EditorGUI.EndProperty();
        }

        private void ShowContextMenu(SerializedProperty property, ResourceAsset resourceAsset)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Open Resource Asset"), false, () => {
                Selection.activeObject = resourceAsset;
                EditorGUIUtility.PingObject(resourceAsset);
            });
            
            menu.AddItem(new GUIContent("View Cache Info"), false, () => {
                ShowCacheInfo(resourceAsset);
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Create New Resource Asset"), false, () => {
                CreateNewResourceAsset(property);
            });

            menu.ShowAsContext();
        }

        private void ShowCacheInfo(ResourceAsset resourceAsset)
        {
            var keys = resourceAsset.GetAllKeys();
            var message = $"Resource Asset: {resourceAsset.name}\n" +
                         $"Cached Resources: {keys.Length}\n" +
                         $"Keys: {string.Join(", ", keys)}";
            
            EditorUtility.DisplayDialog("Resource Cache Info", message, "OK");
        }

        private void CreateNewResourceAsset(SerializedProperty property)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Resource Asset",
                "New Resource Asset",
                "asset",
                "Choose where to save the new Resource Asset");

            if (!string.IsNullOrEmpty(path))
            {
                var newAsset = ScriptableObject.CreateInstance<ResourceAsset>();
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                property.objectReferenceValue = newAsset;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    /// <summary>
    /// Custom property drawer for ResourceAsset arrays in SignaliaConfigAsset.
    /// Provides better visualization and management of ResourceAsset collections.
    /// </summary>
    [CustomPropertyDrawer(typeof(ResourceAsset[]))]
    public class ResourceAssetArrayPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the array property
            EditorGUI.PropertyField(position, property, label, true);

            // Add helpful info
            if (property.isExpanded)
            {
                var infoRect = new Rect(position.x, position.yMax - 20, position.width, 20);
                EditorGUI.HelpBox(infoRect, 
                    "Resource Assets contain cached resources accessible by string keys. " +
                    "Use SIGS.GetResource<T>(key) to access them.", 
                    MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);
            
            if (property.isExpanded)
            {
                height += 25; // Extra space for help box
            }
            
            return height;
        }
    }
}
