using UnityEditor;
using UnityEngine;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.GameSystems.AudioLayering.Editors
{
    [CustomPropertyDrawer(typeof(LayerData))]
    public class LayerDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty idProp = property.FindPropertyRelative("id");
            SerializedProperty categoryProp = property.FindPropertyRelative("category");

            // Use the layer name as the foldout label, fallback to "Un-named Layer" if empty
            string foldoutLabel = !string.IsNullOrEmpty(idProp.stringValue) ? idProp.stringValue : "[Un-named Layer]";

            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(foldoutLabel), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;

                float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                EditorGUI.PropertyField(position, idProp, new GUIContent("Layer ID"));
                position.y += lineHeight;
                EditorGUI.PropertyField(position, categoryProp, new GUIContent("Mixer Category"));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 4;

            if (property.isExpanded)
            {
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                totalHeight += lineHeight * 2; // ID and Category fields
            }

            return totalHeight;
        }
    }

    [CustomPropertyDrawer(typeof(AudioLayeringRoom.RoomTracks))]
    public class RoomTracksDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty layerNameProp = property.FindPropertyRelative("layerName");
            SerializedProperty trackNameProp = property.FindPropertyRelative("trackName");
            SerializedProperty audioNameProp = property.FindPropertyRelative("audioName");
            SerializedProperty propertyOrderProp = property.FindPropertyRelative("propertyOrder");
            SerializedProperty useSilentTrackProp = property.FindPropertyRelative("useSilentTrack");
            SerializedProperty useAudioFiltersProp = property.FindPropertyRelative("useAudioFilters");
            SerializedProperty audioFiltersProp = property.FindPropertyRelative("audioFilters");

            // Check if we're inside a ReorderableList (no label means we're in a list)
            if (string.IsNullOrEmpty(label.text))
            {
                // We're in a ReorderableList, so just draw the properties without foldout
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                
                EditorGUI.indentLevel++;
                
                // Use layer dropdown for the layer name field
                PropertyHelpers.DrawLayerDropdownInline(position, "Layer Name", layerNameProp, property.serializedObject);
                position.y += lineHeight;
                
                EditorGUI.PropertyField(position, trackNameProp, new GUIContent("Track Name", "The specific track within the layer"));
                position.y += lineHeight;
                
                // Only show audio dropdown if silent track is not selected
                if (!useSilentTrackProp.boolValue)
                {
                    // Use audio dropdown for the audio name field
                    PropertyHelpers.DrawAudioDropdownInline(position, "Audio Name", audioNameProp, property.serializedObject);
                    position.y += lineHeight;
                }
                
                // Custom property order field with validation
                EditorGUI.BeginChangeCheck();
                int newPropertyOrder = EditorGUI.IntField(position, new GUIContent("Property Order", "The order priority for this track (0 = base, >0 = overlay)"), propertyOrderProp.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    // Clamp to minimum value of 0
                    newPropertyOrder = Mathf.Max(0, newPropertyOrder);
                    propertyOrderProp.intValue = newPropertyOrder;
                }
                position.y += lineHeight;
                
                // Show warning if property order is 0
                if (propertyOrderProp.intValue == 0)
                {
                    Rect warningRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2);
                    EditorGUI.HelpBox(warningRect, "Warning: Property Order 0 is not recommended for rooms. Use 1+ for overlay effects.", MessageType.Warning);
                    position.y += EditorGUIUtility.singleLineHeight * 2 + 4;
                }
                
                // Add silent track checkbox
                EditorGUI.PropertyField(position, useSilentTrackProp, new GUIContent("Use Silent Track", "When enabled, plays a silent track instead of the specified audio to create an overlay effect"));
                position.y += lineHeight;
                
                // Add audio filters checkbox
                EditorGUI.PropertyField(position, useAudioFiltersProp, new GUIContent("Use Audio Filters", "Apply low/high pass filters to the audio source when this track plays"));
                position.y += lineHeight;
                
                // Show audio filters settings if enabled
                if (useAudioFiltersProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(position, audioFiltersProp, new GUIContent("Filter Settings"), true);
                    position.y += EditorGUI.GetPropertyHeight(audioFiltersProp, true) + 4;
                    EditorGUI.indentLevel--;
                }
                
                // Add help box explaining property order
                Rect helpBoxRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(helpBoxRect, "Property Order: 0 = base track level, 1+ = overlay layer priority", MessageType.Info);

                EditorGUI.indentLevel--;
            }
            else
            {
                // We're not in a ReorderableList, so draw the foldout as before
                string foldoutLabel = !string.IsNullOrEmpty(audioNameProp.stringValue) ? audioNameProp.stringValue : "[Un-named Track]";

                if (useSilentTrackProp.boolValue)
                {
                    foldoutLabel = "[Silent Track]";
                }

                // add the ambience and track name to the foldout label at the end, if the track name is empty, use 'default'
                var trackName = !string.IsNullOrEmpty(trackNameProp.stringValue) ? trackNameProp.stringValue : "default";
                
                // ambience name + track name
                foldoutLabel += $" - {layerNameProp.stringValue} - {trackName}";

                position.height = EditorGUIUtility.singleLineHeight;
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, EditorGUIUtility.TrTextContent(foldoutLabel), true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    position.y += EditorGUIUtility.singleLineHeight + 2;

                    float lineHeight = EditorGUIUtility.singleLineHeight + 4;

                    // Use layer dropdown for the layer name field
                    PropertyHelpers.DrawLayerDropdownInline(position, "Layer Name", layerNameProp, property.serializedObject);
                    position.y += lineHeight;
                    
                    EditorGUI.PropertyField(position, trackNameProp, new GUIContent("Track Name", "The specific track within the layer"));
                    position.y += lineHeight;
                    
                    // Only show audio dropdown if silent track is not selected
                    if (!useSilentTrackProp.boolValue)
                    {
                        // Use audio dropdown for the audio name field
                        PropertyHelpers.DrawAudioDropdownInline(position, "Audio Name", audioNameProp, property.serializedObject);
                        position.y += lineHeight;
                    }
                    
                    // Custom property order field with validation
                    EditorGUI.BeginChangeCheck();
                    int newPropertyOrder = EditorGUI.IntField(position, new GUIContent("Property Order", "The order priority for this track (0 = base, >0 = overlay)"), propertyOrderProp.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Clamp to minimum value of 0
                        newPropertyOrder = Mathf.Max(0, newPropertyOrder);
                        propertyOrderProp.intValue = newPropertyOrder;
                    }
                    position.y += lineHeight;
                    
                    // Show warning if property order is 0
                    if (propertyOrderProp.intValue == 0)
                    {
                        Rect warningRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2);
                        EditorGUI.HelpBox(warningRect, "Warning: Property Order 0 is not recommended for rooms. Use 1+ for overlay effects.", MessageType.Warning);
                        position.y += EditorGUIUtility.singleLineHeight * 2 + 4;
                    }
                    
                    // Add silent track checkbox
                    EditorGUI.PropertyField(position, useSilentTrackProp, new GUIContent("Use Silent Track", "When enabled, plays a silent track instead of the specified audio, helpful when you just want to fake an overlaid source."));
                    position.y += lineHeight;
                    
                    // Add audio filters checkbox
                    EditorGUI.PropertyField(position, useAudioFiltersProp, new GUIContent("Use Audio Filters", "Apply low/high pass filters to the audio source when this track plays"));
                    position.y += lineHeight;
                    
                    // Show audio filters settings if enabled
                    if (useAudioFiltersProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.PropertyField(position, audioFiltersProp, new GUIContent("Filter Settings"), true);
                        position.y += EditorGUI.GetPropertyHeight(audioFiltersProp, true) + 4;
                        EditorGUI.indentLevel--;
                    }
                    
                    // Add help box explaining property order
                    Rect helpBoxRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2);
                    EditorGUI.HelpBox(helpBoxRect, "Property Order: 0 = base track level, 1+ = overlay layer priority", MessageType.Info);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty useSilentTrackProp = property.FindPropertyRelative("useSilentTrack");
            SerializedProperty propertyOrderProp = property.FindPropertyRelative("propertyOrder");
            SerializedProperty useAudioFiltersProp = property.FindPropertyRelative("useAudioFilters");
            SerializedProperty audioFiltersProp = property.FindPropertyRelative("audioFilters");
            bool showAudioDropdown = !useSilentTrackProp.boolValue;
            bool showWarning = propertyOrderProp.intValue == 0;
            bool showAudioFilters = useAudioFiltersProp.boolValue;
            float audioFiltersHeight = showAudioFilters ? (EditorGUI.GetPropertyHeight(audioFiltersProp, true) + 4) : 0;
            
            // Check if we're inside a ReorderableList
            if (string.IsNullOrEmpty(label.text))
            {
                // We're in a ReorderableList, so just return the height of the properties
                float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                float helpBoxHeight = EditorGUIUtility.singleLineHeight * 3; // Help box typically needs 3 lines
                float warningHeight = showWarning ? (EditorGUIUtility.singleLineHeight * 2 + 4) : 0; // Warning box height
                int fieldCount = showAudioDropdown ? 6 : 5; // Layer Name, Track Name, (Audio Name), Property Order, Silent Track, Use Audio Filters fields + help box
                return lineHeight * fieldCount + helpBoxHeight + warningHeight + audioFiltersHeight;
            }
            else
            {
                // We're not in a ReorderableList, so include foldout height
                float totalHeight = EditorGUIUtility.singleLineHeight + 4;

                if (property.isExpanded)
                {
                    float lineHeight = EditorGUIUtility.singleLineHeight + 4;
                    float helpBoxHeight = EditorGUIUtility.singleLineHeight * 3; // Help box typically needs 3 lines
                    float warningHeight = showWarning ? (EditorGUIUtility.singleLineHeight * 2 + 4) : 0; // Warning box height
                    int fieldCount = showAudioDropdown ? 6 : 5; // Layer Name, Track Name, (Audio Name), Property Order, Silent Track, Use Audio Filters fields + help box
                    totalHeight += lineHeight * fieldCount + helpBoxHeight + warningHeight + audioFiltersHeight;
                }

                return totalHeight;
            }
        }
    }
}
