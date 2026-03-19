using UnityEngine;
using UnityEditor;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.GameSystems.PoolingSystem.Editors
{
    /// <summary>
    /// Custom editor for PoolingSpawner component
    /// </summary>
    [CustomEditor(typeof(PoolingSpawner))]
    public class PoolingSpawnerEditor : Editor
    {
        private SerializedProperty spawnableObjectsProp;
        private SerializedProperty useRandomizationProp;
        private SerializedProperty spawnCountProp;
        private SerializedProperty targetTransformProp;
        private SerializedProperty worldPositionProp;
        private SerializedProperty spawnModeProp;
        private SerializedProperty spawnAsChildProp;
        private SerializedProperty positionOffsetProp;

        // Reset options
        private SerializedProperty resetPositionProp;
        private SerializedProperty resetRotationProp;
        private SerializedProperty resetScaleProp;

        // Rotation options
        private SerializedProperty overrideRotationProp;
        private SerializedProperty useRotationOffsetProp;
        private SerializedProperty rotationOffsetProp;

        // Scale options
        private SerializedProperty useScaleOverrideProp;
        private SerializedProperty scaleOverrideProp;

        // Fill target by name options
        private SerializedProperty fillTargetByNameProp;
        private SerializedProperty useLiveKeyProp;
        private SerializedProperty targetNameKeyProp;
        private SerializedProperty lifetimeProp;
        private SerializedProperty cooldownProp;
        private SerializedProperty spawnEnabledProp;
        private SerializedProperty afterSpawnEventProp;
        private SerializedProperty listenerEventProp;
        private SerializedProperty enableWarmingProp;
        private SerializedProperty warmupCountProp;
        private SerializedProperty warmupOnStartProp;
        private SerializedProperty warmupOnAwakeProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Main", "Positioning", "Options" };

        private void OnEnable()
        {
            spawnableObjectsProp = serializedObject.FindProperty("spawnableObjects");
            useRandomizationProp = serializedObject.FindProperty("useRandomization");
            spawnCountProp = serializedObject.FindProperty("spawnCount");
            targetTransformProp = serializedObject.FindProperty("targetTransform");
            worldPositionProp = serializedObject.FindProperty("worldPosition");
            spawnModeProp = serializedObject.FindProperty("spawnMode");
            spawnAsChildProp = serializedObject.FindProperty("spawnAsChild");
            positionOffsetProp = serializedObject.FindProperty("positionOffset");

            // Reset options
            resetPositionProp = serializedObject.FindProperty("resetPosition");
            resetRotationProp = serializedObject.FindProperty("resetRotation");
            resetScaleProp = serializedObject.FindProperty("resetScale");

            // Rotation options
            overrideRotationProp = serializedObject.FindProperty("overrideRotation");
            useRotationOffsetProp = serializedObject.FindProperty("useRotationOffset");
            rotationOffsetProp = serializedObject.FindProperty("rotationOffset");

            // Scale options
            useScaleOverrideProp = serializedObject.FindProperty("useScaleOverride");
            scaleOverrideProp = serializedObject.FindProperty("scaleOverride");

            // Fill target by name options
            fillTargetByNameProp = serializedObject.FindProperty("fillTargetByName");
            useLiveKeyProp = serializedObject.FindProperty("useLiveKey");
            targetNameKeyProp = serializedObject.FindProperty("targetNameKey");

            lifetimeProp = serializedObject.FindProperty("lifetime");
            cooldownProp = serializedObject.FindProperty("cooldown");
            spawnEnabledProp = serializedObject.FindProperty("spawnEnabled");
            afterSpawnEventProp = serializedObject.FindProperty("afterSpawnEvent");
            listenerEventProp = serializedObject.FindProperty("listenerEvent");
            enableWarmingProp = serializedObject.FindProperty("enableWarming");
            warmupCountProp = serializedObject.FindProperty("warmupCount");
            warmupOnStartProp = serializedObject.FindProperty("warmupOnStart");
            warmupOnAwakeProp = serializedObject.FindProperty("warmupOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Configure object spawning using Signalia's pooling system. Supports randomization, positioning options, lifetime control, and after-spawn events.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: // Main
                    DrawMainTab();
                    break;
                case 1: // Positioning
                    DrawPositioningTab();
                    break;
                case 2: // Options
                    DrawOptionsTab();
                    break;
            }

            GUILayout.Space(15);

            // Runtime Controls - Always visible outside tabs
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainTab()
        {
            EditorGUILayout.LabelField("Spawn Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnableObjectsProp, new GUIContent("Spawnable Objects", "List of prefabs that can be spawned with their spawn chances"), true);
        }

        private void DrawPositioningTab()
        {
            EditorGUILayout.LabelField("Positioning Settings", EditorStyles.boldLabel);

            // Spawn Mode selection
            EditorGUILayout.PropertyField(spawnModeProp, new GUIContent("Spawn Mode", "Select how objects should be positioned when spawned"));

            // Get current spawn mode
            int currentSpawnMode = spawnModeProp.enumValueIndex;
            var spawnMode = (PoolingSpawner.SpawnMode)currentSpawnMode;

            switch (spawnMode)
            {
                case PoolingSpawner.SpawnMode.Here:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("Objects will spawn at this component's transform position with the specified offset.", MessageType.Info);

                    EditorGUILayout.PropertyField(spawnAsChildProp, new GUIContent("Spawn As Child", "When enabled, spawned objects become children of this transform"));

                    // Reset options when spawning as child
                    if (spawnAsChildProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Reset Options (When As Child)", EditorStyles.miniLabel);

                        EditorGUILayout.PropertyField(resetPositionProp, new GUIContent("Reset Position", "Reset local position to (0,0,0) when spawning as child"));
                        EditorGUILayout.PropertyField(resetRotationProp, new GUIContent("Reset Rotation", "Reset local rotation to identity when spawning as child"));
                        EditorGUILayout.PropertyField(resetScaleProp, new GUIContent("Reset Scale", "Reset local scale to (1,1,1) when spawning as child"));

                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                    break;

                case PoolingSpawner.SpawnMode.Target:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("Objects will spawn at the target transform position with the specified offset.", MessageType.Info);

                    // Fill Target By Name section
                    EditorGUILayout.PropertyField(fillTargetByNameProp, new GUIContent("Fill Target By Name", "Fill the target transform reference using LiveKey or DeadKey key name on Awake()"));

                    if (fillTargetByNameProp.boolValue)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.PropertyField(useLiveKeyProp, new GUIContent("Use LiveKey", "Use LiveKey system instead of DeadKey"));

                        EditorGUILayout.PropertyField(targetNameKeyProp, new GUIContent("Target Name Key", "Key name to look up in LiveKey/DeadKey system"));

                        if (string.IsNullOrEmpty(targetNameKeyProp.stringValue))
                        {
                            EditorGUILayout.HelpBox("Target Name Key is required when Fill Target By Name is enabled.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox($"Will look for transform using {(useLiveKeyProp.boolValue ? "LiveKey" : "DeadKey")} '{targetNameKeyProp.stringValue}' on Awake()", MessageType.Info);
                        }

                        EditorGUI.indentLevel--;
                        GUILayout.Space(5);
                    }

                    // Only show target transform field if Fill Target By Name is disabled
                    if (!fillTargetByNameProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(targetTransformProp, new GUIContent("Target Transform", "Transform to use as spawn position reference"));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Target Transform", "Will be filled automatically using LiveKey/DeadKey");
                    }

                    EditorGUILayout.PropertyField(spawnAsChildProp, new GUIContent("Spawn As Child", "When enabled, spawned objects become children of target transform"));

                    // Reset options when spawning as child
                    if (spawnAsChildProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Reset Options (When As Child)", EditorStyles.miniLabel);

                        EditorGUILayout.PropertyField(resetPositionProp, new GUIContent("Reset Position", "Reset local position to (0,0,0) when spawning as child"));
                        EditorGUILayout.PropertyField(resetRotationProp, new GUIContent("Reset Rotation", "Reset local rotation to identity when spawning as child"));
                        EditorGUILayout.PropertyField(resetScaleProp, new GUIContent("Reset Scale", "Reset local scale to (1,1,1) when spawning as child"));

                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                    break;

                case PoolingSpawner.SpawnMode.WorldPosition:
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("Objects will spawn at the specified world position with the specified offset.", MessageType.Info);

                    EditorGUILayout.PropertyField(worldPositionProp, new GUIContent("World Position", "Fixed world position for spawning"));
                    EditorGUI.indentLevel--;
                    break;
            }

            EditorGUILayout.PropertyField(positionOffsetProp, new GUIContent("Position Offset", "Additional offset applied to spawn position"));

            GUILayout.Space(10);

            // Rotation section
            EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(overrideRotationProp, new GUIContent("Override Rotation", "When enabled, overrides the spawned object's rotation with custom settings"));

            if (overrideRotationProp.boolValue)
            {
                EditorGUI.indentLevel++;

                switch (spawnMode)
                {
                    case PoolingSpawner.SpawnMode.Here:
                        EditorGUILayout.PropertyField(useRotationOffsetProp, new GUIContent("Use Rotation Offset", "Apply rotation as offset to this transform's rotation"));

                        if (useRotationOffsetProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(rotationOffsetProp, new GUIContent("Rotation Offset", "Euler angles offset applied to this transform's rotation"));
                            EditorGUILayout.HelpBox("Rotation will be: This Transform's Rotation * Rotation Offset", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(rotationOffsetProp, new GUIContent("Rotation Override", "Euler angles used as absolute rotation"));
                            EditorGUILayout.HelpBox("Rotation will be: Rotation Override (ignores this transform's rotation)", MessageType.Info);
                        }
                        break;

                    case PoolingSpawner.SpawnMode.Target:
                        EditorGUILayout.PropertyField(useRotationOffsetProp, new GUIContent("Use Rotation Offset", "Apply rotation as offset to target transform rotation"));

                        if (useRotationOffsetProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(rotationOffsetProp, new GUIContent("Rotation Offset", "Euler angles offset applied to target transform rotation"));
                            EditorGUILayout.HelpBox("Rotation will be: Target Rotation * Rotation Offset", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(rotationOffsetProp, new GUIContent("Rotation Override", "Euler angles used as absolute rotation"));
                            EditorGUILayout.HelpBox("Rotation will be: Rotation Override (ignores target rotation)", MessageType.Info);
                        }
                        break;

                    case PoolingSpawner.SpawnMode.WorldPosition:
                    default:
                        EditorGUILayout.PropertyField(rotationOffsetProp, new GUIContent("Rotation", "Euler angles for spawned object rotation"));
                        EditorGUILayout.HelpBox("Rotation will be: Absolute rotation (ignores any transform)", MessageType.Info);
                        break;
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Rotation will use the prefab's original rotation", MessageType.Info);
            }

            GUILayout.Space(10);

            // Scale section
            EditorGUILayout.LabelField("Scale Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useScaleOverrideProp, new GUIContent("Use Scale Override", "Override the scale of spawned objects"));

            if (useScaleOverrideProp.boolValue)
            {
                EditorGUILayout.PropertyField(scaleOverrideProp, new GUIContent("Scale Override", "Scale applied to spawned objects"));
                EditorGUILayout.HelpBox("Scale will be overridden to the specified value", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Scale will use the prefab's original scale", MessageType.Info);
            }
        }

        private void DrawOptionsTab()
        {
            EditorGUILayout.LabelField("Spawn Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useRandomizationProp, new GUIContent("Use Randomization", "When enabled, uses spawn chances for weighted random selection"));

            if (useRandomizationProp.boolValue)
            {
                EditorGUILayout.HelpBox("Randomization enabled: Objects will be selected based on their spawn chances using SIGS.ThrowDice().", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Randomization disabled: First valid prefab will always be spawned.", MessageType.Info);
            }

            // Spawn Count field with minimum value enforcement
            EditorGUI.BeginChangeCheck();
            int newSpawnCount = EditorGUILayout.IntField(new GUIContent("Spawn Count", "Number of objects to spawn per Spawn() call"), spawnCountProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                spawnCountProp.intValue = Mathf.Max(1, newSpawnCount); // Enforce minimum of 1
            }

            GUILayout.Space(5);

            // Cooldown field with slider
            EditorGUI.BeginChangeCheck();
            float newCooldown = EditorGUILayout.Slider(new GUIContent("Cooldown", "Time in seconds between spawns (0 = no cooldown)"), cooldownProp.floatValue, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                cooldownProp.floatValue = newCooldown;
            }

            if (cooldownProp.floatValue > 0f)
            {
                EditorGUILayout.HelpBox($"Spawn cooldown: {cooldownProp.floatValue} seconds between spawns", MessageType.Info);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Lifetime & Events", EditorStyles.boldLabel);

            // Lifetime field with visual limit
            EditorGUI.BeginChangeCheck();
            float newLifetime = EditorGUILayout.FloatField(new GUIContent("Lifetime", "Auto-deactivate spawned objects after this time (-1 = no auto-deactivation)"), lifetimeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                lifetimeProp.floatValue = Mathf.Max(-1f, newLifetime); // Enforce minimum of -1
            }

            if (lifetimeProp.floatValue > 0f)
            {
                EditorGUILayout.HelpBox($"Objects will be automatically deactivated after {lifetimeProp.floatValue} seconds.", MessageType.Info);
            }
            else if (lifetimeProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Lifetime set to 0 or less: Objects won't disable.", MessageType.Warning);
            }

            GUILayout.Space(5);

            // Spawn Enabled field
            EditorGUILayout.PropertyField(spawnEnabledProp, new GUIContent("Spawn Enabled", "Whether spawned objects should be active/enabled when spawned"));

            if (spawnEnabledProp.boolValue)
            {
                EditorGUILayout.HelpBox("Spawned objects will be active/enabled when spawned.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Spawned objects will be inactive/disabled when spawned.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(afterSpawnEventProp, new GUIContent("After Spawn Event", "Event string to send after each object is spawned (uses SIGS.Event system)"));

            if (!string.IsNullOrEmpty(afterSpawnEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Will send event '{afterSpawnEventProp.stringValue}' after spawning each object", MessageType.Info);
            }

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(listenerEventProp, new GUIContent("Listener Event", "Event string to listen for - automatically spawns objects when this event is received via SIGS.Sender (uses normal spawn configuration)"));

            if (!string.IsNullOrEmpty(listenerEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Will automatically spawn objects when event '{listenerEventProp.stringValue}' is received", MessageType.Info);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Pool Warming", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(enableWarmingProp, new GUIContent("Enable Warming", "Pre-allocate objects in pools for better performance"));

            if (enableWarmingProp.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(warmupCountProp, new GUIContent("Warmup Count", "Number of objects to pre-allocate per pool"));

                if (warmupCountProp.intValue < 1)
                {
                    EditorGUILayout.HelpBox("Warmup count should be at least 1.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(warmupOnAwakeProp, new GUIContent("Warmup On Awake", "Automatically warm pools when component awakes"));
                EditorGUILayout.PropertyField(warmupOnStartProp, new GUIContent("Warmup On Start", "Automatically warm pools when component starts"));

                if (warmupOnAwakeProp.boolValue || warmupOnStartProp.boolValue)
                {
                    EditorGUILayout.HelpBox($"Pools will be warmed with {warmupCountProp.intValue} objects per prefab.", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Pool warming disabled. Objects will be instantiated on-demand when spawned.", MessageType.Info);
            }
        }

        private void DrawRuntimeControls()
        {
            // Create a colorful background box for runtime controls
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);

            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            // Header with play mode indicator
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("● PLAYING", EditorStyles.miniLabel);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.LabelField("● EDIT MODE", EditorStyles.miniLabel);
            }
            GUI.backgroundColor = originalColor;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test spawning!", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            PoolingSpawner spawner = (PoolingSpawner)target;

            // Main spawn button - Large and prominent
            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Bright green
            if (GUILayout.Button("🎲 SPAWN RANDOM", GUILayout.Height(30)))
            {
                spawner.Spawn();
            }
            GUI.backgroundColor = originalColor;

            // Individual spawn buttons for each prefab
            if (spawnableObjectsProp.arraySize > 0)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Spawn Specific Prefabs:", EditorStyles.boldLabel);

                for (int i = 0; i < spawnableObjectsProp.arraySize; i++)
                {
                    var spawnableProp = spawnableObjectsProp.GetArrayElementAtIndex(i);
                    var prefabProp = spawnableProp.FindPropertyRelative("prefab");

                    string prefabName = prefabProp.objectReferenceValue != null ? prefabProp.objectReferenceValue.name : "None";

                    GUI.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f); // Bright cyan
                    if (GUILayout.Button($"▶ {prefabName}", GUILayout.Height(22)))
                    {
                        spawner.SpawnSpecific(i);
                    }
                    GUI.backgroundColor = originalColor;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No spawnable objects configured. Add prefabs in the Main tab.", MessageType.Warning);
            }

            // Pool Warming Controls
            if (enableWarmingProp.boolValue)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Pool Warming Controls:", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f, 1f); // Orange
                if (GUILayout.Button("🔥 WARMUP ALL POOLS", GUILayout.Height(25)))
                {
                    spawner.WarmupPools();
                }
                GUI.backgroundColor = originalColor;

                // Individual pool warming buttons
                if (spawnableObjectsProp.arraySize > 0)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Warmup Individual Pools:", EditorStyles.miniLabel);

                    for (int i = 0; i < spawnableObjectsProp.arraySize; i++)
                    {
                        var spawnableProp = spawnableObjectsProp.GetArrayElementAtIndex(i);
                        var prefabProp = spawnableProp.FindPropertyRelative("prefab");

                        if (prefabProp.objectReferenceValue != null)
                        {
                            string prefabName = prefabProp.objectReferenceValue.name;

                            GUI.backgroundColor = new Color(0.6f, 0.4f, 0.1f, 1f); // Dark orange
                            if (GUILayout.Button($"🔥 {prefabName}", GUILayout.Height(20)))
                            {
                                spawner.WarmupPool(i);
                            }
                            GUI.backgroundColor = originalColor;
                        }
                    }
                }

                // Warming status info
                GUILayout.Space(5);
                EditorGUILayout.HelpBox($"Warming enabled: {warmupCountProp.intValue} objects per pool", MessageType.Info);
            }
            else
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Pool Warming Controls:", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
                if (GUILayout.Button("🔥 ENABLE WARMING", GUILayout.Height(25)))
                {
                    spawner.EnableWarmingAndWarmup();
                }
                GUI.backgroundColor = originalColor;

                EditorGUILayout.HelpBox("Pool warming is disabled. Enable it in the Options tab for better performance.", MessageType.Info);
            }

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            PoolingSpawner spawner = (PoolingSpawner)target;

            // Check spawnable objects
            if (spawnableObjectsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("⚠️ No spawnable objects configured. Add at least one prefab to spawn.", MessageType.Warning);
            }
            else
            {
                int validPrefabs = 0;
                int totalChance = 0;

                for (int i = 0; i < spawnableObjectsProp.arraySize; i++)
                {
                    var spawnableProp = spawnableObjectsProp.GetArrayElementAtIndex(i);
                    var prefabProp = spawnableProp.FindPropertyRelative("prefab");
                    var chanceProp = spawnableProp.FindPropertyRelative("spawnChance");

                    if (prefabProp.objectReferenceValue != null)
                    {
                        validPrefabs++;
                        totalChance += Mathf.RoundToInt(chanceProp.floatValue * 100);
                    }
                }

                if (validPrefabs == 0)
                {
                    EditorGUILayout.HelpBox("🚨 No valid prefabs found in spawnable objects list!", MessageType.Error);
                }
                else if (validPrefabs < spawnableObjectsProp.arraySize)
                {
                    EditorGUILayout.HelpBox($"⚠️ {spawnableObjectsProp.arraySize - validPrefabs} prefab(s) are null or missing.", MessageType.Warning);
                }

                if (useRandomizationProp.boolValue && totalChance == 0)
                {
                    EditorGUILayout.HelpBox("⚠️ Randomization enabled but all spawn chances are 0. No objects will spawn.", MessageType.Warning);
                }
            }

            // Check positioning
            int currentSpawnMode = spawnModeProp.enumValueIndex;
            var spawnMode = (PoolingSpawner.SpawnMode)currentSpawnMode;

            if (spawnMode == PoolingSpawner.SpawnMode.Target && !fillTargetByNameProp.boolValue && targetTransformProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Target Transform mode is selected but no transform is assigned.", MessageType.Warning);
            }

            // Check Fill Target By Name
            if (fillTargetByNameProp.boolValue && string.IsNullOrEmpty(targetNameKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Fill Target By Name is enabled but Target Name Key is empty.", MessageType.Warning);
            }

            // Spawn count is now enforced to be at least 1 in the editor
        }
    }

    /// <summary>
    /// Custom editor for BlowUpPool component
    /// </summary>
    [CustomEditor(typeof(BlowUpPool))]
    public class BlowUpPoolEditor : Editor
    {
        private SerializedProperty prefabProp;
        private SerializedProperty explosionShapeProp;
        private SerializedProperty physicsModeProp;
        private SerializedProperty spawnCountProp;
        private SerializedProperty explosionForceProp;
        private SerializedProperty explosionRadiusProp;
        private SerializedProperty explosionDirectionProp;
        private SerializedProperty sphereAngleProp;
        private SerializedProperty imperfectSpreadProp;
        private SerializedProperty coneAngleProp;
        private SerializedProperty ringRadiusProp;
        private SerializedProperty lineLengthProp;
        private SerializedProperty lineDirectionProp;
        private SerializedProperty useRandomRotationProp;
        private SerializedProperty useRandomForceProp;
        private SerializedProperty forceVariationProp;
        private SerializedProperty lifetimeProp;
        private SerializedProperty spawnEnabledProp;
        private SerializedProperty afterSpawnEventProp;
        private SerializedProperty listenerEventProp;
        private SerializedProperty enableWarmingProp;
        private SerializedProperty warmupCountProp;
        private SerializedProperty warmupOnStartProp;
        private SerializedProperty warmupOnAwakeProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Main", "Explosion", "Options" };

        private void OnEnable()
        {
            prefabProp = serializedObject.FindProperty("prefab");
            explosionShapeProp = serializedObject.FindProperty("explosionShape");
            physicsModeProp = serializedObject.FindProperty("physicsMode");
            spawnCountProp = serializedObject.FindProperty("spawnCount");
            explosionForceProp = serializedObject.FindProperty("explosionForce");
            explosionRadiusProp = serializedObject.FindProperty("explosionRadius");
            explosionDirectionProp = serializedObject.FindProperty("explosionDirection");
            sphereAngleProp = serializedObject.FindProperty("sphereAngle");
            imperfectSpreadProp = serializedObject.FindProperty("imperfectSpread");
            coneAngleProp = serializedObject.FindProperty("coneAngle");
            ringRadiusProp = serializedObject.FindProperty("ringRadius");
            lineLengthProp = serializedObject.FindProperty("lineLength");
            lineDirectionProp = serializedObject.FindProperty("lineDirection");
            useRandomRotationProp = serializedObject.FindProperty("useRandomRotation");
            useRandomForceProp = serializedObject.FindProperty("useRandomForce");
            forceVariationProp = serializedObject.FindProperty("forceVariation");
            lifetimeProp = serializedObject.FindProperty("lifetime");
            spawnEnabledProp = serializedObject.FindProperty("spawnEnabled");
            afterSpawnEventProp = serializedObject.FindProperty("afterSpawnEvent");
            listenerEventProp = serializedObject.FindProperty("listenerEvent");
            enableWarmingProp = serializedObject.FindProperty("enableWarming");
            warmupCountProp = serializedObject.FindProperty("warmupCount");
            warmupOnStartProp = serializedObject.FindProperty("warmupOnStart");
            warmupOnAwakeProp = serializedObject.FindProperty("warmupOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Spawns pooled prefabs and applies explosive forces in various shapes. Supports dynamic rigidbodies (2D/3D) or FakePhysics for kinematic objects.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: // Main
                    DrawMainTab();
                    break;
                case 1: // Explosion
                    DrawExplosionTab();
                    break;
                case 2: // Options
                    DrawOptionsTab();
                    break;
            }

            GUILayout.Space(15);

            // Runtime Controls - Always visible outside tabs
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainTab()
        {
            EditorGUILayout.LabelField("Prefab Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab", "Prefab to spawn. Must have a dynamic Rigidbody (3D), Rigidbody2D (2D), or FakePhysics component."));

            EditorGUILayout.PropertyField(physicsModeProp, new GUIContent("Physics Mode", "Select physics mode: 3D, 2D, or FakePhysics (for kinematic objects)"));

            EditorGUILayout.PropertyField(spawnCountProp, new GUIContent("Spawn Count", "Number of objects to spawn per BlowUp() call"));

            if (spawnCountProp.intValue < 1)
            {
                EditorGUILayout.HelpBox("Spawn count must be at least 1.", MessageType.Warning);
            }
        }

        private void DrawExplosionTab()
        {
            EditorGUILayout.LabelField("Explosion Shape", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(explosionShapeProp, new GUIContent("Shape", "Type of explosion pattern"));

            // NOTE: BlowUpPool.ExplosionShape uses explicit numeric values to preserve backwards-compatibility.
            // enumValueIndex is the UI index (0..N), not the underlying enum int value. Use intValue here.
            var shape = (BlowUpPool.ExplosionShape)explosionShapeProp.intValue;

            EditorGUILayout.PropertyField(explosionForceProp, new GUIContent("Explosion Force", "Force applied to spawned objects"));

            EditorGUILayout.PropertyField(useRandomForceProp, new GUIContent("Use Random Force", "Add variation to explosion force"));

            if (useRandomForceProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(forceVariationProp, new GUIContent("Force Variation", "Random variation range (0-1)"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(useRandomRotationProp, new GUIContent("Use Random Rotation", "Apply random rotation to spawned objects"));

            GUILayout.Space(10);

            // Shape-specific settings
            switch (shape)
            {
                case BlowUpPool.ExplosionShape.Sphere:
                    EditorGUILayout.HelpBox("Sphere: Objects spawn on a spherical cap around Explosion Direction. Set Sphere Angle to 90 for a hemisphere.", MessageType.Info);
                    EditorGUILayout.PropertyField(explosionRadiusProp, new GUIContent("Radius", "Radius of the sphere"));
                    EditorGUILayout.PropertyField(explosionDirectionProp, new GUIContent("Direction", "Axis direction for the spherical cap (used when Sphere Angle < 180)"));
                    EditorGUILayout.PropertyField(sphereAngleProp, new GUIContent("Sphere Angle", "0-180 degrees. 180 = full sphere, 90 = hemisphere."));
                    EditorGUILayout.PropertyField(imperfectSpreadProp, new GUIContent("Imperfect Spread", "Randomize sphere spawn positions (less uniform / more natural)."));
                    break;

                case BlowUpPool.ExplosionShape.Cone:
                    EditorGUILayout.HelpBox("Cone: Objects spawn in a cone shape extending from the center.", MessageType.Info);
                    EditorGUILayout.PropertyField(explosionDirectionProp, new GUIContent("Direction", "Direction vector for the cone"));
                    EditorGUILayout.PropertyField(explosionRadiusProp, new GUIContent("Length", "Length of the cone"));
                    EditorGUILayout.PropertyField(coneAngleProp, new GUIContent("Angle", "Cone angle in degrees"));
                    break;

                case BlowUpPool.ExplosionShape.Ring:
                    EditorGUILayout.HelpBox("Ring: Objects spawn in a horizontal ring around the center.", MessageType.Info);
                    EditorGUILayout.PropertyField(ringRadiusProp, new GUIContent("Radius", "Radius of the ring"));
                    break;

                case BlowUpPool.ExplosionShape.Line:
                    EditorGUILayout.HelpBox("Line: Objects spawn along a line extending from the center.", MessageType.Info);
                    EditorGUILayout.PropertyField(lineDirectionProp, new GUIContent("Direction", "Direction vector for the line"));
                    EditorGUILayout.PropertyField(lineLengthProp, new GUIContent("Length", "Length of the line"));
                    break;
            }
        }

        private void DrawOptionsTab()
        {
            EditorGUILayout.LabelField("Lifetime & Events", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float newLifetime = EditorGUILayout.FloatField(new GUIContent("Lifetime", "Auto-deactivate spawned objects after this time (-1 = no auto-deactivation)"), lifetimeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                lifetimeProp.floatValue = Mathf.Max(-1f, newLifetime);
            }

            if (lifetimeProp.floatValue > 0f)
            {
                EditorGUILayout.HelpBox($"Objects will be automatically deactivated after {lifetimeProp.floatValue} seconds.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(spawnEnabledProp, new GUIContent("Spawn Enabled", "Whether spawned objects should be active/enabled when spawned"));

            EditorGUILayout.PropertyField(afterSpawnEventProp, new GUIContent("After Spawn Event", "Event string to send after each object is spawned (uses SIGS.Event system)"));

            if (!string.IsNullOrEmpty(afterSpawnEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Will send event '{afterSpawnEventProp.stringValue}' after spawning each object", MessageType.Info);
            }

            EditorGUILayout.PropertyField(listenerEventProp, new GUIContent("Listener Event", "Event string to listen for - automatically triggers BlowUp() when this event is received"));

            if (!string.IsNullOrEmpty(listenerEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"Will automatically trigger BlowUp() when event '{listenerEventProp.stringValue}' is received", MessageType.Info);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Pool Warming", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(enableWarmingProp, new GUIContent("Enable Warming", "Pre-allocate objects in pools for better performance"));

            if (enableWarmingProp.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(warmupCountProp, new GUIContent("Warmup Count", "Number of objects to pre-allocate"));

                if (warmupCountProp.intValue < 1)
                {
                    EditorGUILayout.HelpBox("Warmup count should be at least 1.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(warmupOnAwakeProp, new GUIContent("Warmup On Awake", "Automatically warm pool when component awakes"));
                EditorGUILayout.PropertyField(warmupOnStartProp, new GUIContent("Warmup On Start", "Automatically warm pool when component starts"));

                if (warmupOnAwakeProp.boolValue || warmupOnStartProp.boolValue)
                {
                    EditorGUILayout.HelpBox($"Pool will be warmed with {warmupCountProp.intValue} objects.", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Pool warming disabled. Objects will be instantiated on-demand when spawned.", MessageType.Info);
            }
        }

        private void DrawRuntimeControls()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);

            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("● PLAYING", EditorStyles.miniLabel);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.LabelField("● EDIT MODE", EditorStyles.miniLabel);
            }
            GUI.backgroundColor = originalColor;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test!", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            BlowUpPool blowUpPool = (BlowUpPool)target;

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
            if (GUILayout.Button("💥 BLOW UP", GUILayout.Height(30)))
            {
                blowUpPool.BlowUp();
            }
            GUI.backgroundColor = originalColor;

            if (enableWarmingProp.boolValue)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Pool Warming Controls:", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f, 1f); // Orange
                if (GUILayout.Button("🔥 WARMUP POOL", GUILayout.Height(25)))
                {
                    blowUpPool.WarmupPool();
                }
                GUI.backgroundColor = originalColor;

                EditorGUILayout.HelpBox($"Warming enabled: {warmupCountProp.intValue} objects", MessageType.Info);
            }
            else
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Pool Warming Controls:", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
                if (GUILayout.Button("🔥 ENABLE WARMING", GUILayout.Height(25)))
                {
                    blowUpPool.EnableWarmingAndWarmup();
                }
                GUI.backgroundColor = originalColor;

                EditorGUILayout.HelpBox("Pool warming is disabled. Enable it in the Options tab for better performance.", MessageType.Info);
            }

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            BlowUpPool blowUpPool = (BlowUpPool)target;

            if (prefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Prefab is not assigned. Assign a prefab with a dynamic rigidbody to use BlowUpPool.", MessageType.Warning);
                return;
            }

            GameObject prefab = prefabProp.objectReferenceValue as GameObject;
            if (prefab == null) return;

            int physicsMode = physicsModeProp.enumValueIndex;
            var mode = (BlowUpPool.PhysicsMode)physicsMode;

            if (mode == BlowUpPool.PhysicsMode.Physics3D)
            {
                Rigidbody rb = prefab.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    EditorGUILayout.HelpBox("🚨 Prefab must have a Rigidbody component for 3D physics mode!", MessageType.Error);
                }
                else if (rb.isKinematic)
                {
                    EditorGUILayout.HelpBox("🚨 Prefab has a kinematic Rigidbody. It must be dynamic (isKinematic = false) for BlowUpPool to work!", MessageType.Error);
                }
            }
            else if (mode == BlowUpPool.PhysicsMode.Physics2D)
            {
                Rigidbody2D rb2D = prefab.GetComponent<Rigidbody2D>();
                if (rb2D == null)
                {
                    EditorGUILayout.HelpBox("🚨 Prefab must have a Rigidbody2D component for 2D physics mode!", MessageType.Error);
                }
                else if (rb2D.isKinematic)
                {
                    EditorGUILayout.HelpBox("🚨 Prefab has a kinematic Rigidbody2D. It must be dynamic (isKinematic = false) for BlowUpPool to work!", MessageType.Error);
                }
            }
            else if (mode == BlowUpPool.PhysicsMode.FakePhysics)
            {
                // FakePhysics mode: Check for FakePhysics component first, then fallback to dynamic Rigidbody
                FakePhysics fakePhysics = prefab.GetComponent<FakePhysics>();
                if (fakePhysics != null)
                {
                    // Valid - has FakePhysics component
                    EditorGUILayout.HelpBox("✓ Using FakePhysics component for kinematic physics simulation.", MessageType.Info);
                }
                else
                {
                    // Check for dynamic Rigidbody fallback
                    Rigidbody rb = prefab.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        EditorGUILayout.HelpBox("🚨 Prefab must have either a FakePhysics component or a dynamic Rigidbody for FakePhysics mode!", MessageType.Error);
                    }
                    else if (rb.isKinematic)
                    {
                        EditorGUILayout.HelpBox("🚨 Prefab has a kinematic Rigidbody without FakePhysics. Add a FakePhysics component or use a dynamic Rigidbody!", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("ℹ️ No FakePhysics component found. Will use dynamic Rigidbody as fallback.", MessageType.Info);
                    }
                }
            }

            if (spawnCountProp.intValue < 1)
            {
                EditorGUILayout.HelpBox("⚠️ Spawn count must be at least 1.", MessageType.Warning);
            }
        }
    }
}
