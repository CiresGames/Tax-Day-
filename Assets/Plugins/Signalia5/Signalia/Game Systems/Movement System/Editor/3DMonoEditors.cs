#if UNITY_EDITOR
using System;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Movement;
using AHAKuo.Signalia.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AHAKuo.Signalia.GameSystems.Movement.Editors
{
    /// <summary>
    /// Base editor class for MovementPhysicsModifier components.
    /// Provides common functionality like drawing the enabled toggle and events.
    /// </summary>
    public abstract class MovementPhysicsModifierEditor : Editor
    {
        private SerializedProperty modifierEnabledProp;
        private SerializedProperty onModifierBeginProp;
        private SerializedProperty onModifierEndProp;

        protected virtual void OnEnable()
        {
            modifierEnabledProp = serializedObject.FindProperty("modifierEnabled");
            onModifierBeginProp = serializedObject.FindProperty("onModifierBegin");
            onModifierEndProp = serializedObject.FindProperty("onModifierEnd");
        }

        /// <summary>
        /// Draws the enabled toggle for the modifier.
        /// Should be called at the beginning of OnInspectorGUI.
        /// </summary>
        protected void DrawModifierEnabledToggle()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Store original color
            Color originalColor = GUI.color;
            
            // Set color based on enabled state (green for enabled, red for disabled)
            bool isEnabled = modifierEnabledProp.boolValue;
            GUI.color = isEnabled ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            
            EditorGUILayout.PropertyField(modifierEnabledProp, new GUIContent("Modifier Enabled", "Enable or disable this modifier. When disabled, the modifier will not perform any actions."));
            
            // Reset color
            GUI.color = originalColor;
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
        }

        /// <summary>
        /// Draws the Unity Events for modifier begin/end.
        /// Should be called at the end of OnInspectorGUI, before ApplyModifiedProperties.
        /// </summary>
        protected void DrawModifierEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Modifier Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Events triggered when the modifier becomes enabled or disabled.", MessageType.Info);
            
            EditorGUILayout.PropertyField(onModifierBeginProp, new GUIContent("On Modifier Begin", "Invoked when the modifier becomes enabled/activated."));
            EditorGUILayout.PropertyField(onModifierEndProp, new GUIContent("On Modifier End", "Invoked when the modifier becomes disabled/deactivated."));
            
            EditorGUILayout.EndVertical();
        }
    }

    [CustomEditor(typeof(MovementModifier3D)), CanEditMultipleObjects]
    public class MovementModifierEditor : MovementPhysicsModifierEditor
    {
        private SerializedProperty moveSpeedProp;
        private SerializedProperty sprintMultiplierProp;
        private SerializedProperty airControlMultiplierProp;
        private SerializedProperty moveActionNameProp;
        private SerializedProperty sprintActionNameProp;
        private SerializedProperty movementModifierEventsProp;

        private int selectedTab;
        private readonly string[] tabs = { "Movement", "Input", "Events" };

        protected override void OnEnable()
        {
            base.OnEnable();
            moveSpeedProp = serializedObject.FindProperty("moveSpeed");
            sprintMultiplierProp = serializedObject.FindProperty("sprintMultiplier");
            airControlMultiplierProp = serializedObject.FindProperty("airControlMultiplier");
            moveActionNameProp = serializedObject.FindProperty("moveActionName");
            sprintActionNameProp = serializedObject.FindProperty("sprintActionName");
            movementModifierEventsProp = serializedObject.FindProperty("movementModifierEvents");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            DrawModifierEnabledToggle();

            EditorGUILayout.HelpBox("A movement modifier for 3D character movement. Provides basic functionalities: moving and sprinting. For jumping and dashing, add JumpModifier3D and DashModifier3D components separately.\n\nThis modifier uses MovementPhysics3D for all physics calculations. Configure physics settings on the MovementPhysics3D component.", MessageType.Info);
            
            GUILayout.Space(6);
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabs);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawMovementTab();
                    break;
                case 1:
                    DrawInputTab();
                    break;
                case 2:
                    DrawMovementEventsTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            GUILayout.Space(8);
            DrawModifierEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMovementTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("These values can be modified at runtime via the public API: MoveSpeed, SprintMultiplier, and AirControlMultiplier properties.", MessageType.Info);
                GUILayout.Space(4);
            }
            
            EditorGUILayout.PropertyField(moveSpeedProp, new GUIContent("Move Speed", "Base movement speed in units per second. Can be modified at runtime via MoveSpeed property."));
            
            if (moveSpeedProp.floatValue < 0f)
            {
                EditorGUILayout.HelpBox("Move speed should be positive.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(sprintMultiplierProp, new GUIContent("Sprint Multiplier", "Speed multiplier applied when sprinting. 1.5 means 50% faster. Can be modified at runtime via SprintMultiplier property."));
            
            if (sprintMultiplierProp.floatValue < 1f)
            {
                EditorGUILayout.HelpBox("Sprint multiplier is less than 1.0, which means sprinting will be slower than normal movement.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(airControlMultiplierProp, new GUIContent("Air Control Multiplier", "Movement speed multiplier while airborne. 0.5 means 50% control in air. Can be modified at runtime via AirControlMultiplier property."));
            
            if (airControlMultiplierProp.floatValue < 0f || airControlMultiplierProp.floatValue > 1f)
            {
                EditorGUILayout.HelpBox("Air control multiplier is typically between 0 and 1. Values outside this range may feel unnatural.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInputTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Input Action Names", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These action names must match the input actions configured in your Signalia Input Action Maps.", MessageType.Info);
            
            EditorGUILayout.PropertyField(moveActionNameProp, new GUIContent("Move Action", "Input action name for movement. Should return a Vector2 value (e.g., \"Move\")."));
            
            if (string.IsNullOrEmpty(moveActionNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Move action name is required. This should match an action in your Input Action Maps that returns Vector2.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(sprintActionNameProp, new GUIContent("Sprint Action", "Input action name for sprinting. Should return a bool value (e.g., \"Sprint\")."));
            
            if (string.IsNullOrEmpty(sprintActionNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Sprint action name is required. This should match an action in your Input Action Maps that returns bool.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMovementEventsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Movement Modifier Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure events that trigger based on velocity thresholds and movement state (grounded/aerial). Each event has enter and exit callbacks that fire when conditions are met or no longer met.", MessageType.Info);
            
            GUILayout.Space(4);

            if (movementModifierEventsProp == null)
            {
                EditorGUILayout.HelpBox("Movement modifier events property not found.", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.PropertyField(movementModifierEventsProp, new GUIContent("Movement Events", "Array of movement-based events that trigger based on velocity and state conditions."), true);

            if (movementModifierEventsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No movement events configured. Click the '+' button above to add events.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not MovementModifier3D modifier)
                    {
                        continue;
                    }

                    if (!modifier.TryGetComponentInParent(out IMovementPhysicsAuthority physics))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} is missing an IMovementPhysicsAuthority component (e.g., MovementPhysics3D) on this GameObject or a parent GameObject. This component is required for the modifier to function.", MessageType.Error);
                        if (GUILayout.Button("Add MovementPhysics3D Component"))
                        {
                            modifier.gameObject.AddComponent<MovementPhysics3D>();
                        }
                        break;
                    }

                    if (!modifier.TryGetComponent(out Rigidbody rb))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} is missing a Rigidbody component. MovementPhysics3D will add one automatically.", MessageType.Info);
                        break;
                    }

                    if (modifier.TryGetComponent(out Rigidbody2D rb2D))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} has a Rigidbody2D component. Remove it when using 3D physics.", MessageType.Warning);
                        break;
                    }

                    if (physics is MovementPhysics3D && !modifier.TryGetComponent(out CapsuleCollider capsule))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} is missing a CapsuleCollider component. MovementPhysics3D requires a CapsuleCollider.", MessageType.Warning);
                        break;
                    }

                    // Runtime diagnostics
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Move Speed: {modifier.MoveSpeed:F2} units/s");
                        EditorGUILayout.LabelField($"Sprint Multiplier: {modifier.SprintMultiplier:F2}x");
                        EditorGUILayout.LabelField($"Air Control Multiplier: {modifier.AirControlMultiplier:F2}x");
                        EditorGUILayout.LabelField($"Current Speed: {modifier.CurrentSpeed:F2} units/s");
                        EditorGUILayout.LabelField($"Grounded: {(modifier.IsGrounded ? "Yes" : "No")}");
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(MovementPhysics3D)), CanEditMultipleObjects]
    public class SimpleCharacterPhysicsEditor : Editor
    {
        // Collision
        private SerializedProperty collisionMaskProp;
        private SerializedProperty triggerInteractionProp;
        
        // Ground Detection
        private SerializedProperty skinWidthProp;
        private SerializedProperty maxSlopeAngleProp;
        
        // Gravity
        private SerializedProperty useCustomGravityProp;
        private SerializedProperty gravityMultiplierProp;
        private SerializedProperty maxFallSpeedProp;
        
        // Mass & Dynamics
        private SerializedProperty massProp;
        private SerializedProperty kineticEnergyDecayRateProp;
        private SerializedProperty kineticEnergyBuildupRateProp;
        private SerializedProperty maxKineticEnergyProp;
        
        // Velocity Limits
        private SerializedProperty maxHorizontalVelocityProp;
        private SerializedProperty maxVerticalVelocityProp;
        private SerializedProperty maxTotalVelocityProp;
        
        // Resolution
        private SerializedProperty moveIterationsProp;
        private SerializedProperty depenetrationIterationsProp;
        
        // External Forces
        private SerializedProperty externalVelocityDecayRateProp;
        
        // State
        private SerializedProperty frozenProp;
        private SerializedProperty isEnabledProp;
        
        // Events
        private SerializedProperty onCollisionProp;
        private SerializedProperty onGroundedProp;
        private SerializedProperty onAirborneProp;
        private SerializedProperty onConstraintBrokenProp;
        
        // Debug
        private SerializedProperty showDebugProp;
        private SerializedProperty showVelocityVectorsProp;
        private SerializedProperty showConstraintsProp;
        private SerializedProperty showKineticEnergyProp;

        private int selectedTab;
        private readonly string[] tabs = { "Layers", "Grounding", "Gravity", "Mass", "Velocity", "Resolution", "State", "Events", "Debug" };

        private void OnEnable()
        {
            // Collision
            collisionMaskProp = serializedObject.FindProperty("collisionMask");
            triggerInteractionProp = serializedObject.FindProperty("triggerInteraction");
            
            // Ground Detection
            skinWidthProp = serializedObject.FindProperty("skinWidth");
            maxSlopeAngleProp = serializedObject.FindProperty("maxSlopeAngle");
            
            // Gravity
            useCustomGravityProp = serializedObject.FindProperty("useCustomGravity");
            gravityMultiplierProp = serializedObject.FindProperty("gravityMultiplier");
            maxFallSpeedProp = serializedObject.FindProperty("maxFallSpeed");
            
            // Mass & Dynamics
            massProp = serializedObject.FindProperty("mass");
            kineticEnergyDecayRateProp = serializedObject.FindProperty("kineticEnergyDecayRate");
            kineticEnergyBuildupRateProp = serializedObject.FindProperty("kineticEnergyBuildupRate");
            maxKineticEnergyProp = serializedObject.FindProperty("maxKineticEnergy");
            
            // Velocity Limits
            maxHorizontalVelocityProp = serializedObject.FindProperty("maxHorizontalVelocity");
            maxVerticalVelocityProp = serializedObject.FindProperty("maxVerticalVelocity");
            maxTotalVelocityProp = serializedObject.FindProperty("maxTotalVelocity");
            
            // Resolution
            moveIterationsProp = serializedObject.FindProperty("moveIterations");
            depenetrationIterationsProp = serializedObject.FindProperty("depenetrationIterations");
            
            // External Forces
            externalVelocityDecayRateProp = serializedObject.FindProperty("externalVelocityDecayRate");
            
            // State
            frozenProp = serializedObject.FindProperty("frozen");
            isEnabledProp = serializedObject.FindProperty("isEnabled");
            
            // Events
            onCollisionProp = serializedObject.FindProperty("onCollision");
            onGroundedProp = serializedObject.FindProperty("onGrounded");
            onAirborneProp = serializedObject.FindProperty("onAirborne");
            onConstraintBrokenProp = serializedObject.FindProperty("onConstraintBroken");
            
            // Debug
            showDebugProp = serializedObject.FindProperty("showDebug");
            showVelocityVectorsProp = serializedObject.FindProperty("showVelocityVectors");
            showConstraintsProp = serializedObject.FindProperty("showConstraints");
            showKineticEnergyProp = serializedObject.FindProperty("showKineticEnergy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox("Deterministic kinematic character physics using capsule casts and penetration resolution. Supports internal velocity, external forces, kinetic energy buildup, and constraint-based motion. Requires a CapsuleCollider on the same GameObject.", MessageType.Info);

            GUILayout.Space(6);
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabs);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawLayersTab();
                    break;
                case 1:
                    DrawGroundingTab();
                    break;
                case 2:
                    DrawGravityTab();
                    break;
                case 3:
                    DrawMassTab();
                    break;
                case 4:
                    DrawVelocityLimitsTab();
                    break;
                case 5:
                    DrawResolutionTab();
                    break;
                case 6:
                    DrawStateTab();
                    break;
                case 7:
                    DrawEventsTab();
                    break;
                case 8:
                    DrawDebugTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayersTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Collision Layers", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(collisionMaskProp, new GUIContent("Collision Mask", "Physics layers that the character can collide with."));
            
            if (collisionMaskProp.intValue == 0)
            {
                EditorGUILayout.HelpBox("Collision mask is set to 'Nothing'. The character will not collide with anything.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(triggerInteractionProp, new GUIContent("Trigger Interaction", "How to handle trigger colliders during physics checks."));

            EditorGUILayout.EndVertical();
        }

        private void DrawGroundingTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Ground Detection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Ground detection uses the CapsuleCollider geometry directly. The character will land exactly at the collider boundary.", MessageType.Info);
            
            EditorGUILayout.PropertyField(skinWidthProp, new GUIContent("Skin Width", "Small offset to keep capsule above ground (prevents jitter). Ground detection range is skinWidth * 2."));
            EditorGUILayout.PropertyField(maxSlopeAngleProp, new GUIContent("Max Slope Angle", "Maximum slope angle considered as 'ground'."));

            // Show the capsule collider reference
            foreach (Object targetObject in targets)
            {
                if (targetObject is MovementPhysics3D physics && physics.TryGetComponent(out CapsuleCollider capsule))
                {
                    GUILayout.Space(6);
                    EditorGUILayout.LabelField("Collider Reference", EditorStyles.boldLabel);
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField("Capsule Collider", capsule, typeof(CapsuleCollider), true);
                    GUI.enabled = true;
                    break;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGravityTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Gravity Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(useCustomGravityProp, new GUIContent("Use Custom Gravity", "Enable custom gravity instead of Unity's default."));
            
            if (useCustomGravityProp.boolValue)
            {
                EditorGUILayout.PropertyField(gravityMultiplierProp, new GUIContent("Gravity Multiplier", "Multiplier applied to Physics.gravity.y."));
                EditorGUILayout.PropertyField(maxFallSpeedProp, new GUIContent("Max Fall Speed", "Maximum downward velocity."));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMassTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Mass & Dynamics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Mass influences how external forces and kinetic energy behave. Heavier objects have slower decay and more stable momentum.", MessageType.Info);
            
            EditorGUILayout.PropertyField(massProp, new GUIContent("Mass", "Mass of the object. Influences force decay rates."));
            
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Kinetic Energy", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Kinetic energy builds up from motion and adds momentum-like behavior while maintaining deterministic control.", MessageType.Info);
            
            EditorGUILayout.PropertyField(kineticEnergyBuildupRateProp, new GUIContent("Buildup Rate", "How fast kinetic energy builds from motion."));
            EditorGUILayout.PropertyField(kineticEnergyDecayRateProp, new GUIContent("Decay Rate", "How fast kinetic energy decays."));
            EditorGUILayout.PropertyField(maxKineticEnergyProp, new GUIContent("Max Kinetic Energy", "Maximum kinetic energy magnitude."));
            
            GUILayout.Space(6);
            EditorGUILayout.LabelField("External Forces", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(externalVelocityDecayRateProp, new GUIContent("External Force Decay Rate", "How quickly external forces (dash, knockback) decay."));

            EditorGUILayout.EndVertical();
        }

        private void DrawVelocityLimitsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Velocity Limits", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Maximum velocity clamping to prevent excessive speeds. Set to 0 to disable clamping.", MessageType.Info);
            
            EditorGUILayout.PropertyField(maxHorizontalVelocityProp, new GUIContent("Max Horizontal Velocity", "Maximum horizontal velocity (X and Z)."));
            EditorGUILayout.PropertyField(maxVerticalVelocityProp, new GUIContent("Max Vertical Velocity", "Maximum upward velocity."));
            EditorGUILayout.PropertyField(maxTotalVelocityProp, new GUIContent("Max Total Velocity", "Maximum total velocity magnitude."));

            EditorGUILayout.EndVertical();
        }

        private void DrawResolutionTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Movement Resolution", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(moveIterationsProp, new GUIContent("Move Iterations", "Iterations for capsule casts and sliding per frame."));
            EditorGUILayout.PropertyField(depenetrationIterationsProp, new GUIContent("Depenetration Iterations", "Overlap resolve passes after moving."));

            EditorGUILayout.EndVertical();
        }

        private void DrawStateTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("State Control", EditorStyles.boldLabel);
            
            // Enabled toggle with color
            Color originalColor = GUI.color;
            GUI.color = isEnabledProp.boolValue ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            EditorGUILayout.PropertyField(isEnabledProp, new GUIContent("Enabled", "Enable/disable physics processing."));
            GUI.color = originalColor;
            
            GUILayout.Space(4);
            
            // Frozen toggle with color
            GUI.color = frozenProp.boolValue ? new Color(0.6f, 0.8f, 1f) : Color.white;
            EditorGUILayout.PropertyField(frozenProp, new GUIContent("Frozen", "When frozen, no velocities are applied."));
            GUI.color = originalColor;
            
            if (frozenProp.boolValue)
            {
                EditorGUILayout.HelpBox("Object is frozen. All velocities are paused.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Physics Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Events triggered by physics state changes.", MessageType.Info);
            
            EditorGUILayout.PropertyField(onCollisionProp, new GUIContent("On Collision", "Fired when the object collides with something."));
            EditorGUILayout.PropertyField(onGroundedProp, new GUIContent("On Grounded", "Fired when becoming grounded."));
            EditorGUILayout.PropertyField(onAirborneProp, new GUIContent("On Airborne", "Fired when becoming airborne."));
            EditorGUILayout.PropertyField(onConstraintBrokenProp, new GUIContent("On Constraint Broken", "Fired when a Sticky constraint breaks."));

            EditorGUILayout.EndVertical();
        }

        private void DrawDebugTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Visualization", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(showDebugProp, new GUIContent("Show Debug Gizmos", "Display gizmos in Scene view."));
            
            if (showDebugProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(showVelocityVectorsProp, new GUIContent("Show Velocity Vectors", "Display velocity vectors."));
                EditorGUILayout.PropertyField(showConstraintsProp, new GUIContent("Show Constraints", "Display constraint connections."));
                EditorGUILayout.PropertyField(showKineticEnergyProp, new GUIContent("Show Kinetic Energy", "Display kinetic energy vector."));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.HelpBox("Gizmo Colors:\n- Green: Internal velocity\n- Blue: External force\n- Magenta: Kinetic energy\n- Cyan/Yellow/Orange: Constraints (Metallic/Rope/Sticky)", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not MovementPhysics3D physics)
                        continue;

                    if (!physics.TryGetComponent(out Rigidbody rb))
                    {
                        EditorGUILayout.HelpBox($"{physics.name} is missing a Rigidbody. One will be added automatically.", MessageType.Warning);
                        break;
                    }

                    if (!physics.TryGetComponent(out CapsuleCollider capsule))
                    {
                        EditorGUILayout.HelpBox($"{physics.name} is missing a CapsuleCollider. This is required.", MessageType.Error);
                        if (GUILayout.Button("Add CapsuleCollider"))
                        {
                            physics.gameObject.AddComponent<CapsuleCollider>();
                        }
                        break;
                    }

                    if (Application.isPlaying)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                        
                        // State
                        EditorGUILayout.LabelField($"Grounded: {(physics.IsGrounded ? "Yes" : "No")}");
                        EditorGUILayout.LabelField($"Frozen: {(physics.Frozen ? "Yes" : "No")}");
                        EditorGUILayout.LabelField($"Mass: {physics.Mass:F2}");
                        
                        // Velocities
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Velocities", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Internal: {physics.InternalVelocity} ({physics.InternalVelocity.magnitude:F2})");
                        EditorGUILayout.LabelField($"External: {physics.ExternalVelocity} ({physics.ExternalVelocity.magnitude:F2})");
                        EditorGUILayout.LabelField($"Kinetic: {physics.KineticEnergy} ({physics.KineticEnergy.magnitude:F2})");
                        EditorGUILayout.LabelField("Late (prev frame):", EditorStyles.miniLabel);
                        var late = physics.LateVelocities;
                        EditorGUILayout.LabelField($"  Internal: {late.InternalVelocity} ({late.InternalVelocity.magnitude:F2})");
                        EditorGUILayout.LabelField($"  Total: {late.TotalVelocity} ({late.TotalSpeed:F2})");
                        
                        // Constraints
                        var constraints = physics.CurrentConstraints;
                        if (constraints.Length > 0)
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField($"Constraints ({constraints.Length})", EditorStyles.boldLabel);
                            foreach (var c in constraints)
                            {
                                EditorGUILayout.LabelField($"  [{c.type}] {c.id} - Distance: {c.distance:F2}");
                            }
                        }
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(JumpModifier3D)), CanEditMultipleObjects]
    public class JumpModifier3DEditor : MovementPhysicsModifierEditor
    {
        private SerializedProperty jumpForceProp;
        private SerializedProperty maxJumpsProp;
        private SerializedProperty jumpCooldownProp;
        private SerializedProperty coyoteTimeProp;
        private SerializedProperty jumpActionNameProp;
        private SerializedProperty onJumpBeginProp;
        private SerializedProperty onJumpEndProp;

        private int selectedTab;
        private readonly string[] tabs = { "Jump Settings", "Input" };

        protected override void OnEnable()
        {
            base.OnEnable();
            jumpForceProp = serializedObject.FindProperty("jumpForce");
            maxJumpsProp = serializedObject.FindProperty("maxJumps");
            jumpCooldownProp = serializedObject.FindProperty("jumpCooldown");
            coyoteTimeProp = serializedObject.FindProperty("coyoteTime");
            jumpActionNameProp = serializedObject.FindProperty("jumpActionName");
            onJumpBeginProp = serializedObject.FindProperty("onJumpBegin");
            onJumpEndProp = serializedObject.FindProperty("onJumpEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            DrawModifierEnabledToggle();

            EditorGUILayout.HelpBox("Handles jumping mechanics for 3D character movement. Supports multiple jumps, coyote time, and jump cooldown.", MessageType.Info);

            GUILayout.Space(6);
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabs);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawJumpSettingsTab();
                    break;
                case 1:
                    DrawInputTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            GUILayout.Space(8);
            DrawModifierEvents();

            GUILayout.Space(8);
            DrawJumpEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawJumpEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Jump Action Events", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(onJumpBeginProp, new GUIContent("On Jump Begin", "Invoked when a jump is performed."));
            EditorGUILayout.PropertyField(onJumpEndProp, new GUIContent("On Jump End", "Invoked when the character lands."));
            
            EditorGUILayout.EndVertical();
        }

        private void DrawJumpSettingsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Jump Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(jumpForceProp, new GUIContent("Jump Force", "Upward force applied when jumping."));
            EditorGUILayout.PropertyField(maxJumpsProp, new GUIContent("Max Jumps", "Maximum jumps before landing. 1 = single, 2 = double."));
            EditorGUILayout.PropertyField(jumpCooldownProp, new GUIContent("Jump Cooldown", "Minimum time between jumps."));
            EditorGUILayout.PropertyField(coyoteTimeProp, new GUIContent("Coyote Time", "Time after leaving ground where jump is still allowed."));

            EditorGUILayout.EndVertical();
        }

        private void DrawInputTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Input Action Names", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(jumpActionNameProp, new GUIContent("Jump Action", "Input action name for jumping."));

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not JumpModifier3D modifier)
                        continue;

                    if (!modifier.TryGetComponentInParent(out IMovementPhysicsAuthority physics))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} is missing an IMovementPhysicsAuthority component.", MessageType.Error);
                        if (GUILayout.Button("Add MovementPhysics3D"))
                        {
                            modifier.gameObject.AddComponent<MovementPhysics3D>();
                        }
                        break;
                    }

                    if (Application.isPlaying)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Jump Count: {modifier.JumpCount}");
                        EditorGUILayout.LabelField($"Can Jump: {(modifier.CanJump ? "Yes" : "No")}");
                        EditorGUILayout.LabelField($"Grounded: {(physics.IsGrounded ? "Yes" : "No")}");
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(DashModifier3D)), CanEditMultipleObjects]
    public class DashModifier3DEditor : MovementPhysicsModifierEditor
    {
        private SerializedProperty dashForceProp;
        private SerializedProperty dashCooldownProp;
        private SerializedProperty dashDirectionModeProp;
        private SerializedProperty dashActionNameProp;
        private SerializedProperty onDashBeginProp;
        private SerializedProperty onDashEndProp;

        private int selectedTab;
        private readonly string[] tabs = { "Dash Settings", "Input" };

        protected override void OnEnable()
        {
            base.OnEnable();
            dashForceProp = serializedObject.FindProperty("dashForce");
            dashCooldownProp = serializedObject.FindProperty("dashCooldown");
            dashDirectionModeProp = serializedObject.FindProperty("dashDirectionMode");
            dashActionNameProp = serializedObject.FindProperty("dashActionName");
            onDashBeginProp = serializedObject.FindProperty("onDashBegin");
            onDashEndProp = serializedObject.FindProperty("onDashEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            DrawModifierEnabledToggle();

            EditorGUILayout.HelpBox("Handles dashing mechanics for 3D character movement. Applies external force that decays over time.", MessageType.Info);

            GUILayout.Space(6);
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabs);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawDashSettingsTab();
                    break;
                case 1:
                    DrawInputTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            GUILayout.Space(8);
            DrawModifierEvents();

            GUILayout.Space(8);
            DrawDashEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDashEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dash Action Events", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(onDashBeginProp, new GUIContent("On Dash Begin", "Invoked when a dash is performed."));
            EditorGUILayout.PropertyField(onDashEndProp, new GUIContent("On Dash End", "Invoked when cooldown ends."));
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDashSettingsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dash Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(dashForceProp, new GUIContent("Dash Force", "Force applied when dashing."));
            EditorGUILayout.PropertyField(dashCooldownProp, new GUIContent("Dash Cooldown", "Minimum time between dashes."));
            EditorGUILayout.PropertyField(dashDirectionModeProp, new GUIContent("Direction Mode", "CameraForward or TransformForward."));

            EditorGUILayout.EndVertical();
        }

        private void DrawInputTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Input Action Names", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(dashActionNameProp, new GUIContent("Dash Action", "Input action name for dashing."));

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not DashModifier3D modifier)
                        continue;

                    if (!modifier.TryGetComponentInParent(out IMovementPhysicsAuthority physics))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} is missing an IMovementPhysicsAuthority component.", MessageType.Error);
                        if (GUILayout.Button("Add MovementPhysics3D"))
                        {
                            modifier.gameObject.AddComponent<MovementPhysics3D>();
                        }
                        break;
                    }

                    if (Application.isPlaying)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"On Cooldown: {(modifier.IsOnCooldown ? "Yes" : "No")}");
                        if (modifier.IsOnCooldown)
                        {
                            EditorGUILayout.LabelField($"Remaining: {modifier.RemainingCooldown:F2}s");
                        }
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(ConstraintModifier3D)), CanEditMultipleObjects]
    public class ConstraintModifier3DEditor : MovementPhysicsModifierEditor
    {
        // Anchor
        private SerializedProperty anchorTargetProp;
        private SerializedProperty anchorOffsetProp;
        private SerializedProperty useCurrentDistanceOnActivateProp;
        
        // Constraint
        private SerializedProperty constraintTypeProp;
        private SerializedProperty distanceProp;
        private SerializedProperty elasticityProp;
        private SerializedProperty breakForceProp;
        
        // Activation
        private SerializedProperty autoActivateProp;
        private SerializedProperty constraintIdProp;
        
        // Events
        private SerializedProperty onConstraintActivatedProp;
        private SerializedProperty onConstraintDeactivatedProp;
        private SerializedProperty onConstraintBrokenProp;
        
        // Debug
        private SerializedProperty showDebugGizmosProp;

        private int selectedTab;
        private readonly string[] tabs = { "Anchor", "Constraint", "Activation", "Events" };

        protected override void OnEnable()
        {
            base.OnEnable();
            
            anchorTargetProp = serializedObject.FindProperty("anchorTarget");
            anchorOffsetProp = serializedObject.FindProperty("anchorOffset");
            useCurrentDistanceOnActivateProp = serializedObject.FindProperty("useCurrentDistanceOnActivate");
            
            constraintTypeProp = serializedObject.FindProperty("constraintType");
            distanceProp = serializedObject.FindProperty("distance");
            elasticityProp = serializedObject.FindProperty("elasticity");
            breakForceProp = serializedObject.FindProperty("breakForce");
            
            autoActivateProp = serializedObject.FindProperty("autoActivate");
            constraintIdProp = serializedObject.FindProperty("constraintId");
            
            onConstraintActivatedProp = serializedObject.FindProperty("onConstraintActivated");
            onConstraintDeactivatedProp = serializedObject.FindProperty("onConstraintDeactivated");
            onConstraintBrokenProp = serializedObject.FindProperty("onConstraintBroken");
            
            showDebugGizmosProp = serializedObject.FindProperty("showDebugGizmos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            DrawModifierEnabledToggle();

            EditorGUILayout.HelpBox("Adds physics constraints (Metallic/Rope/Sticky) to the movement physics system. Perfect for grappling hooks, tethers, and sticky surfaces.", MessageType.Info);

            GUILayout.Space(6);
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabs);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawAnchorTab();
                    break;
                case 1:
                    DrawConstraintTab();
                    break;
                case 2:
                    DrawActivationTab();
                    break;
                case 3:
                    DrawEventsTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            GUILayout.Space(8);
            DrawModifierEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAnchorTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Anchor Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define where the constraint attaches. Use a target transform for dynamic anchors, or set a world position directly.", MessageType.Info);
            
            EditorGUILayout.PropertyField(anchorTargetProp, new GUIContent("Anchor Target", "Optional transform to use as anchor. Offset is applied relative to this."));
            EditorGUILayout.PropertyField(anchorOffsetProp, new GUIContent("Anchor Offset", "World offset from target, or absolute position if no target."));
            EditorGUILayout.PropertyField(useCurrentDistanceOnActivateProp, new GUIContent("Use Current Distance", "If true, uses distance to anchor when activated."));

            EditorGUILayout.EndVertical();
        }

        private void DrawConstraintTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Constraint Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(constraintTypeProp, new GUIContent("Constraint Type", "Type of constraint behavior."));
            
            ConstraintType type = (ConstraintType)constraintTypeProp.enumValueIndex;
            
            switch (type)
            {
                case ConstraintType.Metallic:
                    EditorGUILayout.HelpBox("Metallic: Maintains fixed distance from anchor. Object can only orbit around the anchor point.", MessageType.Info);
                    EditorGUILayout.PropertyField(distanceProp, new GUIContent("Fixed Distance", "Exact distance maintained from anchor."));
                    break;
                    
                case ConstraintType.Rope:
                    EditorGUILayout.HelpBox("Rope: Object can move freely within distance, but is pulled back with elasticity when exceeding.", MessageType.Info);
                    EditorGUILayout.PropertyField(distanceProp, new GUIContent("Max Distance", "Maximum distance before pull-back."));
                    EditorGUILayout.PropertyField(elasticityProp, new GUIContent("Elasticity", "Pull-back strength when exceeding distance."));
                    break;
                    
                case ConstraintType.Sticky:
                    EditorGUILayout.HelpBox("Sticky: Holds object but can break when kinetic energy exceeds threshold. Builds up energy while constrained.", MessageType.Info);
                    EditorGUILayout.PropertyField(distanceProp, new GUIContent("Distance", "Distance threshold before resistance."));
                    EditorGUILayout.PropertyField(breakForceProp, new GUIContent("Break Force", "Kinetic energy threshold to break constraint."));
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActivationTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Activation Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(constraintIdProp, new GUIContent("Constraint ID", "Unique identifier for this constraint."));
            EditorGUILayout.PropertyField(autoActivateProp, new GUIContent("Auto Activate", "Automatically activate when component is enabled."));
            
            GUILayout.Space(6);
            EditorGUILayout.PropertyField(showDebugGizmosProp, new GUIContent("Show Debug Gizmos", "Display constraint visualization in Scene view."));
            
            // Runtime controls
            if (Application.isPlaying)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                ConstraintModifier3D modifier = target as ConstraintModifier3D;
                if (modifier != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    GUI.backgroundColor = modifier.IsConstraintActive ? Color.red : Color.green;
                    if (GUILayout.Button(modifier.IsConstraintActive ? "Deactivate" : "Activate", GUILayout.Height(28)))
                    {
                        if (modifier.IsConstraintActive)
                            modifier.DeactivateConstraint();
                        else
                            modifier.ActivateConstraint();
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEventsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Constraint Events", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(onConstraintActivatedProp, new GUIContent("On Activated", "Invoked when constraint is activated."));
            EditorGUILayout.PropertyField(onConstraintDeactivatedProp, new GUIContent("On Deactivated", "Invoked when constraint is deactivated."));
            EditorGUILayout.PropertyField(onConstraintBrokenProp, new GUIContent("On Broken", "Invoked when Sticky constraint breaks."));

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not ConstraintModifier3D modifier)
                        continue;

                    if (!modifier.TryGetComponentInParent(out IMovementPhysicsAuthority physics))
                    {
                        EditorGUILayout.HelpBox($"{modifier.name} is missing an IMovementPhysicsAuthority component.", MessageType.Error);
                        if (GUILayout.Button("Add MovementPhysics3D"))
                        {
                            modifier.gameObject.AddComponent<MovementPhysics3D>();
                        }
                        break;
                    }

                    if (Application.isPlaying)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Constraint Active: {(modifier.IsConstraintActive ? "Yes" : "No")}");
                        EditorGUILayout.LabelField($"Anchor Position: {modifier.CurrentAnchorPosition}");
                        
                        float currentDist = Vector3.Distance(modifier.transform.position, modifier.CurrentAnchorPosition);
                        EditorGUILayout.LabelField($"Current Distance: {currentDist:F2}");
                        EditorGUILayout.LabelField($"Constraint Distance: {modifier.Distance:F2}");
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(DebugModifier3D)), CanEditMultipleObjects]
    public class DebugModifier3DEditor : MovementPhysicsModifierEditor
    {
        private SerializedProperty testVelocityProp;
        private SerializedProperty testJumpForceProp;
        private SerializedProperty testDashForceProp;
        private SerializedProperty testPositionProp;
        private SerializedProperty testKineticEnergyProp;
        private SerializedProperty testConstraintTypeProp;
        private SerializedProperty testConstraintDistanceProp;
        private SerializedProperty testConstraintElasticityProp;
        private SerializedProperty testConstraintBreakForceProp;
        private SerializedProperty testConstraintAnchorProp;
        private SerializedProperty testMassProp;

        private int selectedTab;
        private readonly string[] tabs = { "Velocity", "Forces", "Kinetic", "Constraints", "State", "Quick Actions" };

        protected override void OnEnable()
        {
            base.OnEnable();
            testVelocityProp = serializedObject.FindProperty("testVelocity");
            testJumpForceProp = serializedObject.FindProperty("testJumpForce");
            testDashForceProp = serializedObject.FindProperty("testDashForce");
            testPositionProp = serializedObject.FindProperty("testPosition");
            testKineticEnergyProp = serializedObject.FindProperty("testKineticEnergy");
            testConstraintTypeProp = serializedObject.FindProperty("testConstraintType");
            testConstraintDistanceProp = serializedObject.FindProperty("testConstraintDistance");
            testConstraintElasticityProp = serializedObject.FindProperty("testConstraintElasticity");
            testConstraintBreakForceProp = serializedObject.FindProperty("testConstraintBreakForce");
            testConstraintAnchorProp = serializedObject.FindProperty("testConstraintAnchor");
            testMassProp = serializedObject.FindProperty("testMass");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(6);

            DrawModifierEnabledToggle();

            EditorGUILayout.HelpBox("Debug modifier for testing MovementPhysics3D features. Provides runtime testing for velocity, forces, kinetic energy, constraints, and more.", MessageType.Info);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use debug features.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            GUILayout.Space(6);
            selectedTab = EditorUtilityMethods.RenderToolbar(selectedTab, tabs);
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawVelocityTab();
                    break;
                case 1:
                    DrawForcesTab();
                    break;
                case 2:
                    DrawKineticTab();
                    break;
                case 3:
                    DrawConstraintsTab();
                    break;
                case 4:
                    DrawStateTab();
                    break;
                case 5:
                    DrawQuickActionsTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            GUILayout.Space(8);
            DrawModifierEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVelocityTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Velocity Tests", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(testVelocityProp, new GUIContent("Test Velocity", "Velocity value for testing."));

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Forward", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugSetVelocity(m.transform.forward * testVelocityProp.floatValue);
            }
            if (GUILayout.Button("Up", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugSetVelocity(Vector3.up * testVelocityProp.floatValue);
            }
            if (GUILayout.Button("Zero", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugSetVelocity(Vector3.zero);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawForcesTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Force Tests", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(testDashForceProp, new GUIContent("Dash Force"));
            EditorGUILayout.PropertyField(testJumpForceProp, new GUIContent("Jump Force"));

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dash Forward", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugDash(testDashForceProp.floatValue);
            }
            if (GUILayout.Button("Jump", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugJump(testJumpForceProp.floatValue);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear External", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugClearExternalVelocity();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawKineticTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Kinetic Energy Tests", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(testKineticEnergyProp, new GUIContent("Test Energy"));

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugAddTestKineticEnergy();
            }
            if (GUILayout.Button("Clear", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugClearKineticEnergy();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawConstraintsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Constraint Tests", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(testConstraintTypeProp, new GUIContent("Type"));
            EditorGUILayout.PropertyField(testConstraintDistanceProp, new GUIContent("Distance"));
            EditorGUILayout.PropertyField(testConstraintElasticityProp, new GUIContent("Elasticity"));
            EditorGUILayout.PropertyField(testConstraintBreakForceProp, new GUIContent("Break Force"));
            EditorGUILayout.PropertyField(testConstraintAnchorProp, new GUIContent("Anchor"));

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Above", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugAddConstraintAbove(testConstraintDistanceProp.floatValue);
            }
            if (GUILayout.Button("Add Test", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugAddTestConstraint();
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugClearAllConstraints();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStateTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("State Control", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(testMassProp, new GUIContent("Test Mass"));

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Frozen", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Freeze", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugFreeze();
            }
            if (GUILayout.Button("Unfreeze", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugUnfreeze();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Mass", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Test", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugSetTestMass();
            }
            if (GUILayout.Button("Double", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugDoubleMass();
            }
            if (GUILayout.Button("Halve", GUILayout.Height(24)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugHalveMass();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickActionsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(testPositionProp, new GUIContent("Teleport Position"));

            GUILayout.Space(4);
            if (GUILayout.Button("Teleport to Position", GUILayout.Height(28)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugTeleportToTestPosition();
            }

            if (GUILayout.Button("Reset All Velocities", GUILayout.Height(28)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugResetVelocities();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reset Everything", GUILayout.Height(28)))
            {
                foreach (DebugModifier3D m in targets)
                    m.DebugResetAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not DebugModifier3D modifier)
                        continue;

                    if (!modifier.TryGetComponentInParent(out IMovementPhysicsAuthority physics))
                    {
                        EditorGUILayout.HelpBox("Missing IMovementPhysicsAuthority.", MessageType.Error);
                        break;
                    }

                    EditorGUILayout.LabelField($"Grounded: {(modifier.IsGrounded ? "Yes" : "No")}");
                    EditorGUILayout.LabelField($"Frozen: {(modifier.IsFrozen ? "Yes" : "No")}");
                    EditorGUILayout.LabelField($"Mass: {modifier.CurrentMass:F2}");
                    EditorGUILayout.LabelField($"Constraints: {modifier.ConstraintCount}");
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Internal: {modifier.CurrentInternalVelocity.magnitude:F2} m/s");
                    EditorGUILayout.LabelField($"External: {modifier.CurrentExternalVelocity.magnitude:F2} m/s");
                    EditorGUILayout.LabelField($"Kinetic: {modifier.CurrentKineticEnergy.magnitude:F2}");
                    EditorGUILayout.LabelField($"Combined: {modifier.CombinedVelocityMagnitude:F2} m/s");
                }
            }
        }
    }
}
#endif