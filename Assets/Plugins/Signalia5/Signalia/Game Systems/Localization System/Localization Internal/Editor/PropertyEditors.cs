#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.GameSystems.Localization.Internal;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal.Editors
{
    /// <summary>
    /// Property drawer for AudioPage - makes pages collapsible by default
    /// </summary>
    [CustomPropertyDrawer(typeof(LocBook.AudioPage))]
    public class AudioPagePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty pageIdProp = property.FindPropertyRelative("pageId");
            SerializedProperty aboutPageProp = property.FindPropertyRelative("aboutPage");
            SerializedProperty audioEntriesProp = property.FindPropertyRelative("audioEntries");
            
            string pageLabel = string.IsNullOrEmpty(pageIdProp.stringValue) ? "Audio Page" : pageIdProp.stringValue;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"📄 {pageLabel}", true);
            
            if (property.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 2;
                
                // Page ID
                Rect pageIdRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(pageIdRect, pageIdProp, new GUIContent("Page ID"));
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // About Page
                Rect aboutRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight * 3);
                EditorGUI.PropertyField(aboutRect, aboutPageProp, new GUIContent("About Page"), true);
                yOffset += EditorGUIUtility.singleLineHeight * 3 + 2;
                
                // Audio Entries (collapsible)
                Rect entriesLabelRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                audioEntriesProp.isExpanded = EditorGUI.Foldout(entriesLabelRect, audioEntriesProp.isExpanded, $"Audio Entries ({audioEntriesProp.arraySize})", true);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                if (audioEntriesProp.isExpanded)
                {
                    for (int i = 0; i < audioEntriesProp.arraySize; i++)
                    {
                        SerializedProperty entryProp = audioEntriesProp.GetArrayElementAtIndex(i);
                        Rect entryRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                        float entryHeight = GetAudioEntryHeight(entryProp);
                        entryRect.height = entryHeight;
                        DrawAudioEntry(entryRect, entryProp, i);
                        yOffset += entryHeight + 2;
                    }
                }
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout
            height += EditorGUIUtility.singleLineHeight + 2; // Page ID
            height += EditorGUIUtility.singleLineHeight * 3 + 2; // About Page
            
            SerializedProperty audioEntriesProp = property.FindPropertyRelative("audioEntries");
            height += EditorGUIUtility.singleLineHeight + 2; // Entries label
            
            if (audioEntriesProp.isExpanded)
            {
                for (int i = 0; i < audioEntriesProp.arraySize; i++)
                {
                    SerializedProperty entryProp = audioEntriesProp.GetArrayElementAtIndex(i);
                    height += GetAudioEntryHeight(entryProp) + 2;
                }
            }
            
            return height;
        }
        
        private void DrawAudioEntry(Rect position, SerializedProperty entryProp, int index)
        {
            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
            SerializedProperty variantsProp = entryProp.FindPropertyRelative("variants");
            
            string entryLabel = string.IsNullOrEmpty(keyProp.stringValue) ? $"Audio Entry {index + 1}" : keyProp.stringValue;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            entryProp.isExpanded = EditorGUI.Foldout(foldoutRect, entryProp.isExpanded, $"🎵 {entryLabel}", true);
            
            if (entryProp.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 2;
                
                // Key
                Rect keyRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, new GUIContent("Key"));
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // Variants (collapsible)
                Rect variantsLabelRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                variantsProp.isExpanded = EditorGUI.Foldout(variantsLabelRect, variantsProp.isExpanded, $"Variants ({variantsProp.arraySize})", true);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                if (variantsProp.isExpanded)
                {
                    for (int v = 0; v < variantsProp.arraySize; v++)
                    {
                        SerializedProperty variantProp = variantsProp.GetArrayElementAtIndex(v);
                        Rect variantRect = new Rect(position.x + 30, position.y + yOffset, position.width - 30, EditorGUIUtility.singleLineHeight * 2);
                        EditorGUI.PropertyField(variantRect, variantProp, new GUIContent($"Variant {v + 1}"), true);
                        yOffset += EditorGUIUtility.singleLineHeight * 2 + 2;
                    }
                }
            }
        }
        
        private float GetAudioEntryHeight(SerializedProperty entryProp)
        {
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout
            
            if (entryProp.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2; // Key
                height += EditorGUIUtility.singleLineHeight + 2; // Variants label
                
                SerializedProperty variantsProp = entryProp.FindPropertyRelative("variants");
                if (variantsProp.isExpanded)
                {
                    height += variantsProp.arraySize * (EditorGUIUtility.singleLineHeight * 2 + 2); // Variants
                }
            }
            
            return height;
        }
    }
    
    /// <summary>
    /// Property drawer for ImagePage - makes pages collapsible by default
    /// </summary>
    [CustomPropertyDrawer(typeof(LocBook.ImagePage))]
    public class ImagePagePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty pageIdProp = property.FindPropertyRelative("pageId");
            SerializedProperty aboutPageProp = property.FindPropertyRelative("aboutPage");
            SerializedProperty spriteEntriesProp = property.FindPropertyRelative("spriteEntries");
            
            string pageLabel = string.IsNullOrEmpty(pageIdProp.stringValue) ? "Image Page" : pageIdProp.stringValue;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"📄 {pageLabel}", true);
            
            if (property.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 2;
                
                // Page ID
                Rect pageIdRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(pageIdRect, pageIdProp, new GUIContent("Page ID"));
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // About Page
                Rect aboutRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight * 3);
                EditorGUI.PropertyField(aboutRect, aboutPageProp, new GUIContent("About Page"), true);
                yOffset += EditorGUIUtility.singleLineHeight * 3 + 2;
                
                // Sprite Entries (collapsible)
                Rect entriesLabelRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                spriteEntriesProp.isExpanded = EditorGUI.Foldout(entriesLabelRect, spriteEntriesProp.isExpanded, $"Image Entries ({spriteEntriesProp.arraySize})", true);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                if (spriteEntriesProp.isExpanded)
                {
                    for (int i = 0; i < spriteEntriesProp.arraySize; i++)
                    {
                        SerializedProperty entryProp = spriteEntriesProp.GetArrayElementAtIndex(i);
                        Rect entryRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                        float entryHeight = GetSpriteEntryHeight(entryProp);
                        entryRect.height = entryHeight;
                        DrawSpriteEntry(entryRect, entryProp, i);
                        yOffset += entryHeight + 2;
                    }
                }
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout
            height += EditorGUIUtility.singleLineHeight + 2; // Page ID
            height += EditorGUIUtility.singleLineHeight * 3 + 2; // About Page
            
            SerializedProperty spriteEntriesProp = property.FindPropertyRelative("spriteEntries");
            height += EditorGUIUtility.singleLineHeight + 2; // Entries label
            
            if (spriteEntriesProp.isExpanded)
            {
                for (int i = 0; i < spriteEntriesProp.arraySize; i++)
                {
                    SerializedProperty entryProp = spriteEntriesProp.GetArrayElementAtIndex(i);
                    height += GetSpriteEntryHeight(entryProp) + 2;
                }
            }
            
            return height;
        }
        
        private void DrawSpriteEntry(Rect position, SerializedProperty entryProp, int index)
        {
            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
            SerializedProperty variantsProp = entryProp.FindPropertyRelative("variants");
            
            string entryLabel = string.IsNullOrEmpty(keyProp.stringValue) ? $"Image Entry {index + 1}" : keyProp.stringValue;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            entryProp.isExpanded = EditorGUI.Foldout(foldoutRect, entryProp.isExpanded, $"🖼️ {entryLabel}", true);
            
            if (entryProp.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 2;
                
                // Key
                Rect keyRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, new GUIContent("Key"));
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // Variants (collapsible)
                Rect variantsLabelRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                variantsProp.isExpanded = EditorGUI.Foldout(variantsLabelRect, variantsProp.isExpanded, $"Variants ({variantsProp.arraySize})", true);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                if (variantsProp.isExpanded)
                {
                    for (int v = 0; v < variantsProp.arraySize; v++)
                    {
                        SerializedProperty variantProp = variantsProp.GetArrayElementAtIndex(v);
                        Rect variantRect = new Rect(position.x + 30, position.y + yOffset, position.width - 30, EditorGUIUtility.singleLineHeight * 2);
                        EditorGUI.PropertyField(variantRect, variantProp, new GUIContent($"Variant {v + 1}"), true);
                        yOffset += EditorGUIUtility.singleLineHeight * 2 + 2;
                    }
                }
            }
        }
        
        private float GetSpriteEntryHeight(SerializedProperty entryProp)
        {
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout
            
            if (entryProp.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2; // Key
                height += EditorGUIUtility.singleLineHeight + 2; // Variants label
                
                SerializedProperty variantsProp = entryProp.FindPropertyRelative("variants");
                if (variantsProp.isExpanded)
                {
                    height += variantsProp.arraySize * (EditorGUIUtility.singleLineHeight * 2 + 2); // Variants
                }
            }
            
            return height;
        }
    }
    
    /// <summary>
    /// Property drawer for AssetPage - makes pages collapsible by default
    /// </summary>
    [CustomPropertyDrawer(typeof(LocBook.AssetPage))]
    public class AssetPagePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty pageIdProp = property.FindPropertyRelative("pageId");
            SerializedProperty aboutPageProp = property.FindPropertyRelative("aboutPage");
            SerializedProperty assetEntriesProp = property.FindPropertyRelative("assetEntries");
            
            string pageLabel = string.IsNullOrEmpty(pageIdProp.stringValue) ? "Asset Page" : pageIdProp.stringValue;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"📄 {pageLabel}", true);
            
            if (property.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 2;
                
                // Page ID
                Rect pageIdRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(pageIdRect, pageIdProp, new GUIContent("Page ID"));
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // About Page
                Rect aboutRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight * 3);
                EditorGUI.PropertyField(aboutRect, aboutPageProp, new GUIContent("About Page"), true);
                yOffset += EditorGUIUtility.singleLineHeight * 3 + 2;
                
                // Asset Entries (collapsible)
                Rect entriesLabelRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                assetEntriesProp.isExpanded = EditorGUI.Foldout(entriesLabelRect, assetEntriesProp.isExpanded, $"Asset Entries ({assetEntriesProp.arraySize})", true);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                if (assetEntriesProp.isExpanded)
                {
                    for (int i = 0; i < assetEntriesProp.arraySize; i++)
                    {
                        SerializedProperty entryProp = assetEntriesProp.GetArrayElementAtIndex(i);
                        Rect entryRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                        float entryHeight = GetAssetEntryHeight(entryProp);
                        entryRect.height = entryHeight;
                        DrawAssetEntry(entryRect, entryProp, i);
                        yOffset += entryHeight + 2;
                    }
                }
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout
            height += EditorGUIUtility.singleLineHeight + 2; // Page ID
            height += EditorGUIUtility.singleLineHeight * 3 + 2; // About Page
            
            SerializedProperty assetEntriesProp = property.FindPropertyRelative("assetEntries");
            height += EditorGUIUtility.singleLineHeight + 2; // Entries label
            
            if (assetEntriesProp.isExpanded)
            {
                for (int i = 0; i < assetEntriesProp.arraySize; i++)
                {
                    SerializedProperty entryProp = assetEntriesProp.GetArrayElementAtIndex(i);
                    height += GetAssetEntryHeight(entryProp) + 2;
                }
            }
            
            return height;
        }
        
        private void DrawAssetEntry(Rect position, SerializedProperty entryProp, int index)
        {
            SerializedProperty keyProp = entryProp.FindPropertyRelative("key");
            SerializedProperty variantsProp = entryProp.FindPropertyRelative("variants");
            
            string entryLabel = string.IsNullOrEmpty(keyProp.stringValue) ? $"Asset Entry {index + 1}" : keyProp.stringValue;
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            entryProp.isExpanded = EditorGUI.Foldout(foldoutRect, entryProp.isExpanded, $"📦 {entryLabel}", true);
            
            if (entryProp.isExpanded)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 2;
                
                // Key
                Rect keyRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, new GUIContent("Key"));
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // Variants (collapsible)
                Rect variantsLabelRect = new Rect(position.x + 15, position.y + yOffset, position.width - 15, EditorGUIUtility.singleLineHeight);
                variantsProp.isExpanded = EditorGUI.Foldout(variantsLabelRect, variantsProp.isExpanded, $"Variants ({variantsProp.arraySize})", true);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                if (variantsProp.isExpanded)
                {
                    for (int v = 0; v < variantsProp.arraySize; v++)
                    {
                        SerializedProperty variantProp = variantsProp.GetArrayElementAtIndex(v);
                        Rect variantRect = new Rect(position.x + 30, position.y + yOffset, position.width - 30, EditorGUIUtility.singleLineHeight * 2);
                        EditorGUI.PropertyField(variantRect, variantProp, new GUIContent($"Variant {v + 1}"), true);
                        yOffset += EditorGUIUtility.singleLineHeight * 2 + 2;
                    }
                }
            }
        }
        
        private float GetAssetEntryHeight(SerializedProperty entryProp)
        {
            float height = EditorGUIUtility.singleLineHeight + 2; // Foldout
            
            if (entryProp.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 2; // Key
                height += EditorGUIUtility.singleLineHeight + 2; // Variants label
                
                SerializedProperty variantsProp = entryProp.FindPropertyRelative("variants");
                if (variantsProp.isExpanded)
                {
                    height += variantsProp.arraySize * (EditorGUIUtility.singleLineHeight * 2 + 2); // Variants
                }
            }
            
            return height;
        }
    }
}
#endif
