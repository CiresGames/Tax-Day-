#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Entities;
using AHAKuo.Signalia.Utilities;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    [CustomEditor(typeof(Entity)), CanEditMultipleObjects]
    public class EntityEditor : Editor
    {
        private SerializedProperty entityTypeProp, firstLogicProp, stopMode;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Configuration", "Runtime" };

        private void OnEnable()
        {
            entityTypeProp = serializedObject.FindProperty("entityType");
            firstLogicProp = serializedObject.FindProperty("firstLogic");
            stopMode = serializedObject.FindProperty("stopMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(5);

            EditorGUILayout.HelpBox("A representation of an entity in the game. Placed on an object so it can be logic controlled or Player driven. This object also handles the Tick frame of Entity Logic and the Enter and Exit.", MessageType.Info);

            GUILayout.Space(5);

            // Toolbar - use RenderToolbar helper method
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabNames, 24);

            GUILayout.Space(5);

            // Tab content
            switch (selectedTab)
            {
                case 0:
                    DrawConfigurationTab();
                    break;
                case 1:
                    DrawRuntimeTab();
                    break;
            }

            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigurationTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entityTypeProp, new GUIContent("Entity Type", "Determines if this entity is controlled by AI or Player. Player entities skip logic processing."));
            
            EditorGUILayout.EndVertical();
            
            // Check if player is selected - if so, don't show logic options
            var isPlayer = entityTypeProp.enumValueIndex == (int)EntityType.Player;
            if (isPlayer)
                return; // no need to draw logic properties for player entities
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("First Logic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(firstLogicProp, new GUIContent("First Logic", "The initial EntityLogic to use. If left empty, the first EntityLogic found in children will be used."));
            
            if (firstLogicProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No First Logic assigned. The first EntityLogic found in children will be used automatically.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Logic Stop Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Logic Stop Mode will prevent logic processing and will not tick the EntityLogic when its inside that mode.", MessageType.Info);
            EditorGUILayout.PropertyField(stopMode, new GUIContent("Stop Mode", "Determines if the entity should be frozen in time or allow logic processing."));
            
            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime information is only available during play mode. Press Play to see entity state.", MessageType.Info);
                return;
            }

            Entity entity = (Entity)target;
            if (entity == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            // Entity Type
            EditorGUILayout.LabelField("Entity Type:", entity.EntityType.ToString());

            // Central System
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Central System", EditorStyles.boldLabel);
            if (entity.Central != null)
            {
                EditorGUILayout.LabelField("Central:", "Initialized");
                
                // Health System Status
                if (entity.Central.Health != null)
                {
                    EditorGUILayout.LabelField("Health Status:", entity.IsDead ? "Dead" : "Alive");
                    EditorGUILayout.LabelField("Is Dead:", entity.IsDead ? "Yes" : "No");
                    EditorGUILayout.LabelField("Is Alive:", entity.IsAlive ? "Yes" : "No");
                }
                else
                {
                    EditorGUILayout.LabelField("Health System:", "Not Available (No ObjectHealth on entity)");
                }

                // Movement System Status
                if (entity.Central.PhysicsAuthority != null)
                {
                    EditorGUILayout.LabelField("Grounded Status:", entity.IsGrounded ? "Grounded" : "Aerial");
                    EditorGUILayout.LabelField("Is Grounded:", entity.IsGrounded ? "Yes" : "No");
                    EditorGUILayout.LabelField("Is Aerial:", entity.IsAerial ? "Yes" : "No");
                }
                else
                {
                    EditorGUILayout.LabelField("Movement System:", "Not Available (No PhysicsAuthority on entity)");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Central system not initialized. This should happen in Awake().", MessageType.Warning);
            }

            // Current Logic
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Current Logic", EditorStyles.boldLabel);
            if (entity.CurrentEntityLogic != null)
            {
                EditorGUILayout.LabelField("Logic:", entity.CurrentEntityLogic.name);
                
                if (entity.CurrentEntityState != null)
                {
                    EditorGUILayout.LabelField("Current State:", entity.CurrentEntityState.stateName ?? "Unnamed State");
                }
                else
                {
                    EditorGUILayout.LabelField("Current State:", "None (Unready)");
                }

                // Logic Time Information
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Logic Timing", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField("Time in State:", entity.CurrentEntityLogic.TimeInState);
                EditorGUILayout.FloatField("Time in Logic:", entity.CurrentEntityLogic.TimeInLogic);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.LabelField("Logic:", "None");
                EditorGUILayout.HelpBox("No EntityLogic is currently active. Logic processing may be disabled or no logic components found.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);

            if (entity.CurrentEntityLogic != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Enter State (Index 0)", GUILayout.Height(25)))
                {
                    entity.CurrentEntityLogic.EnterState(0);
                }
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Refresh", GUILayout.Height(25)))
                {
                    EditorUtility.SetDirty(entity);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Exit State", GUILayout.Height(25)))
                {
                    if (entity.CurrentEntityLogic.CurrentState != null)
                    {
                        entity.CurrentEntityLogic.ExitState();
                    }
                }
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Force Tick", GUILayout.Height(25)))
                {
                    entity.CurrentEntityLogic.TickFrame();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No active logic available for debugging actions.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            // Force repaint for live updates
            Repaint();
        }
    }
}
#endif
