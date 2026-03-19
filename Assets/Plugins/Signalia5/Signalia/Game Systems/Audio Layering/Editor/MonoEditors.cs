using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditorInternal;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.GameSystems.AudioLayering.Editors
{
    [CustomEditor(typeof(AudioLayeringLayerData))]
    public class AudioLayeringDataAssetEditor : Editor
    {
        private SerializedProperty layersProp;

        private void OnEnable()
        {
            layersProp = serializedObject.FindProperty("layers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Manage your audio layers. Define layer IDs and their associated mixer categories.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            EditorGUILayout.PropertyField(layersProp, new GUIContent("Audio Layers"), true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawErrorWarnings()
        {
            AudioLayeringLayerData asset = (AudioLayeringLayerData)target;

            var layers = asset.GetLayersForLoading();
            if (layers.GroupBy(x => x.id).Any(g => g.Count() > 1))
            {
                EditorGUILayout.HelpBox("🚨 Duplicate layer IDs detected! Each layer ID should be unique.", MessageType.Error);
            }

            if (layers.Count == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No layers assigned. Add at least one layer with a unique ID and mixer category.", MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(AudioLayeringRoom))]
    public class AudioLayeringRoomEditor : Editor
    {
        private SerializedProperty triggeringLayerProp;
        private SerializedProperty mustBeInvokedProp;
        private SerializedProperty roomEnterEventProp;
        private SerializedProperty roomTracksProp;

        private void OnEnable()
        {
            triggeringLayerProp = serializedObject.FindProperty("triggeringLayer");
            mustBeInvokedProp = serializedObject.FindProperty("mustBeInvoked");
            roomEnterEventProp = serializedObject.FindProperty("roomEnterEvent");
            roomTracksProp = serializedObject.FindProperty("roomTracks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Configure audio tracks that will play when objects enter this room's trigger zone.", MessageType.Info);

            GUILayout.Space(10);

            // Must Be Invoked toggle with explanation
            EditorGUILayout.PropertyField(mustBeInvokedProp, new GUIContent("Must Be Invoked", "When enabled, the room will not use trigger zones and must be manually controlled via code"));
            
            if (mustBeInvokedProp.boolValue)
            {
                EditorGUILayout.HelpBox("📋 Manual Control Mode: Use EnterRoom() and ExitRoom() methods to control audio playback. No collider required.", MessageType.Info);
            }

            GUILayout.Space(5);

            // Room Enter Event field
            EditorGUILayout.PropertyField(roomEnterEventProp, new GUIContent("Room Enter Event", "Event string to send when entering this room (uses SendEvent extension)"));
            
            if (!string.IsNullOrEmpty(roomEnterEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"📡 Will send event '{roomEnterEventProp.stringValue}' when entering this room", MessageType.Info);
            }

            GUILayout.Space(5);

            // Only show triggering layer if not must be invoked
            if (!mustBeInvokedProp.boolValue)
            {
                EditorGUILayout.PropertyField(triggeringLayerProp, new GUIContent("Triggering Layer", "Layer mask for objects that can trigger audio changes"));
                GUILayout.Space(10);
            }

            DrawErrorWarnings();

            EditorGUILayout.HelpBox("Room tracks support optional low/high pass filters so you can quickly create muffled interior ambience.", MessageType.Info);
            EditorGUILayout.PropertyField(roomTracksProp, new GUIContent("Room Audio Tracks"), true);

            // Runtime controls for manual invocation
            if (Application.isPlaying && mustBeInvokedProp.boolValue)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("🎮 Manual Control", EditorStyles.boldLabel);
                
                AudioLayeringRoom room = (AudioLayeringRoom)target;
                
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Enter Room", GUILayout.Height(25)))
                {
                    room.EnterRoom();
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("⏹ Exit Room", GUILayout.Height(25)))
                {
                    room.ExitRoom();
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Individual track controls
                if (roomTracksProp.arraySize > 0)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Individual Tracks:", EditorStyles.boldLabel);
                    
                    for (int i = 0; i < roomTracksProp.arraySize; i++)
                    {
                        var trackProp = roomTracksProp.GetArrayElementAtIndex(i);
                        var trackName = trackProp.FindPropertyRelative("trackName").stringValue;
                        var audioName = trackProp.FindPropertyRelative("audioName").stringValue;
                        
                        if (string.IsNullOrEmpty(trackName)) trackName = "default";
                        if (string.IsNullOrEmpty(audioName)) audioName = "unnamed";
                        
                        string displayName = $"Track {i}: {trackName} - {audioName}";
                        
                        GUILayout.BeginHorizontal();
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button($"▶ {displayName}", GUILayout.Height(20)))
                        {
                            room.EnterRoomTrack(i);
                        }
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("⏹", GUILayout.Height(20), GUILayout.Width(30)))
                        {
                            room.ExitRoomTrack(i);
                        }
                        GUI.backgroundColor = Color.white;
                        GUILayout.EndHorizontal();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawErrorWarnings()
        {
            AudioLayeringRoom room = (AudioLayeringRoom)target;

            // Only check for collider if must be invoked is false
            if (!mustBeInvokedProp.boolValue)
            {
                // Check if the GameObject has a collider
                Collider collider = room.GetComponent<Collider>();
                Collider2D collider2D = room.GetComponent<Collider2D>();
                
                if (collider == null && collider2D == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No collider found! Add a Collider or Collider2D component and set it as a trigger.", MessageType.Warning);
                }
                else if (collider != null && !collider.isTrigger)
                {
                    EditorGUILayout.HelpBox("⚠️ Collider is not set as trigger! Enable 'Is Trigger' on the Collider component.", MessageType.Warning);
                }
                else if (collider2D != null && !collider2D.isTrigger)
                {
                    EditorGUILayout.HelpBox("⚠️ Collider2D is not set as trigger! Enable 'Is Trigger' on the Collider2D component.", MessageType.Warning);
                }
            }

            // Check if room tracks are configured
            if (roomTracksProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No room tracks configured. Add at least one track to play audio when entering this room.", MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(AudioLayeringAmbient))]
    public class AudioLayeringAmbientEditor : Editor
    {
        private SerializedProperty layerNameProp;
        private SerializedProperty trackNameProp;
        private SerializedProperty audioNameProp;
        private SerializedProperty propertyOrderProp;
        private SerializedProperty autoPlayProp;
        private SerializedProperty useAudioFiltersProp;
        private SerializedProperty audioFiltersProp;

        private void OnEnable()
        {
            layerNameProp = serializedObject.FindProperty("layerName");
            trackNameProp = serializedObject.FindProperty("trackName");
            audioNameProp = serializedObject.FindProperty("audioName");
            propertyOrderProp = serializedObject.FindProperty("propertyOrder");
            autoPlayProp = serializedObject.FindProperty("autoPlay");
            useAudioFiltersProp = serializedObject.FindProperty("useAudioFilters");
            audioFiltersProp = serializedObject.FindProperty("audioFilters");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Configure ambient audio that will play in the background. This is typically used for ambient sounds or background music.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            // Use layer dropdown for the layer name field
            PropertyHelpers.DrawLayerDropdownInline(EditorGUILayout.GetControlRect(), "Layer Name", layerNameProp, serializedObject);
            EditorGUILayout.PropertyField(trackNameProp, new GUIContent("Track Name", "The specific track within the layer"));
            
            // Use audio dropdown for the audio name field
            PropertyHelpers.DrawAudioDropdownInline(EditorGUILayout.GetControlRect(), "Audio Name", audioNameProp, serializedObject);
            
            EditorGUILayout.PropertyField(propertyOrderProp, new GUIContent("Property Order", "The order priority for this track (0 = base, >0 = overlay)"));
            EditorGUILayout.PropertyField(autoPlayProp, new GUIContent("Auto Play", "Automatically play the audio when the component starts"));

            EditorGUILayout.PropertyField(useAudioFiltersProp, new GUIContent("Use Audio Filters", "Toggle low/high pass filters when this ambient track is played"));
            if (useAudioFiltersProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(audioFiltersProp, new GUIContent("Filter Settings"), true);
                EditorGUI.indentLevel--;
            }

            // Add help box explaining property order
            EditorGUILayout.HelpBox("Property Order: 0 = base track level, 1+ = overlay layer priority", MessageType.Info);

            GUILayout.Space(10);

            // Runtime controls
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);
                
                AudioLayeringAmbient ambient = (AudioLayeringAmbient)target;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Play Now", GUILayout.Height(25)))
                {
                    ambient.PlayNow();
                }
                GUI.backgroundColor = Color.white;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawErrorWarnings()
        {
            AudioLayeringAmbient ambient = (AudioLayeringAmbient)target;

            // Check if required fields are configured
            if (string.IsNullOrEmpty(layerNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Layer Name is not configured. Please set a valid layer name.", MessageType.Warning);
            }

            if (string.IsNullOrEmpty(trackNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Empty track name uses 'default' as the track name.", MessageType.Info);
            }

            if (string.IsNullOrEmpty(audioNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Audio Name is not configured. Please select an audio clip.", MessageType.Warning);
            }

            // Check if property order is negative
            if (propertyOrderProp.intValue < 0)
            {
                EditorGUILayout.HelpBox("⚠️ Property Order should not be negative. Setting to 0.", MessageType.Warning);
                propertyOrderProp.intValue = 0;
            }
        }
    }
}
