#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.Utilities;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics.Editors
{
    [CustomEditor(typeof(Collectible)), CanEditMultipleObjects]
    public class CollectibleEditor : Editor
    {
        // Trigger properties
        private SerializedProperty triggerModeProp;
        private SerializedProperty triggerSpaceProp;
        private SerializedProperty raycastBoxSizeProp;
        private SerializedProperty raycastBoxOffsetProp;
        private SerializedProperty drawDebugGizmosProp;
        private SerializedProperty gizmoColorProp;

        // Filter properties
        private SerializedProperty useLayerFilterProp;
        private SerializedProperty allowedLayersProp;
        private SerializedProperty useTagFilterProp;
        private SerializedProperty requiredTagProp;

        // Collection type
        private SerializedProperty collectibleTypeProp;

        // Item properties
        private SerializedProperty itemDefinitionProp;
        private SerializedProperty itemQuantityProp;
        private SerializedProperty targetInventoryProp;
        private SerializedProperty autoSaveInventoryProp;

        // Currency properties
        private SerializedProperty currencyNameProp;
        private SerializedProperty currencyAmountProp;
        private SerializedProperty autoSaveCurrencyProp;
        private SerializedProperty notifyCurrencyChangeProp;

        // Effect properties
        private SerializedProperty effectModeProp;
        private SerializedProperty pooledEffectPrefabProp;
        private SerializedProperty pooledEffectLifetimeProp;
        private SerializedProperty effectOffsetProp;
        private SerializedProperty localEffectObjectProp;
        private SerializedProperty playLocalParticlesProp;

        // Audio properties
        private SerializedProperty collectAudioProp;

        // Disable properties
        private SerializedProperty visualObjectProp;
        private SerializedProperty hideVisualImmediatelyProp;
        private SerializedProperty disableGameObjectDelayProp;

        // Persistence properties
        private SerializedProperty persistentCollectionProp;
        private SerializedProperty persistentSaveKeyProp;

        private int selectedTab;
        private readonly string[] tabs = { "General", "Collection", "Effects", "Audio", "Persistence" };

        // Placeholder texture for visual coherence
        private Texture2D headerPlaceholder;

        private void OnEnable()
        {
            // Trigger
            triggerModeProp = serializedObject.FindProperty("triggerMode");
            triggerSpaceProp = serializedObject.FindProperty("triggerSpace");
            raycastBoxSizeProp = serializedObject.FindProperty("raycastBoxSize");
            raycastBoxOffsetProp = serializedObject.FindProperty("raycastBoxOffset");
            drawDebugGizmosProp = serializedObject.FindProperty("drawDebugGizmos");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");

            // Filters
            useLayerFilterProp = serializedObject.FindProperty("useLayerFilter");
            allowedLayersProp = serializedObject.FindProperty("allowedLayers");
            useTagFilterProp = serializedObject.FindProperty("useTagFilter");
            requiredTagProp = serializedObject.FindProperty("requiredTag");

            // Collection type
            collectibleTypeProp = serializedObject.FindProperty("collectibleType");

            // Item
            itemDefinitionProp = serializedObject.FindProperty("itemDefinition");
            itemQuantityProp = serializedObject.FindProperty("itemQuantity");
            targetInventoryProp = serializedObject.FindProperty("targetInventory");
            autoSaveInventoryProp = serializedObject.FindProperty("autoSaveInventory");

            // Currency
            currencyNameProp = serializedObject.FindProperty("currencyName");
            currencyAmountProp = serializedObject.FindProperty("currencyAmount");
            autoSaveCurrencyProp = serializedObject.FindProperty("autoSaveCurrency");
            notifyCurrencyChangeProp = serializedObject.FindProperty("notifyCurrencyChange");

            // Effects
            effectModeProp = serializedObject.FindProperty("effectMode");
            pooledEffectPrefabProp = serializedObject.FindProperty("pooledEffectPrefab");
            pooledEffectLifetimeProp = serializedObject.FindProperty("pooledEffectLifetime");
            effectOffsetProp = serializedObject.FindProperty("effectOffset");
            localEffectObjectProp = serializedObject.FindProperty("localEffectObject");
            playLocalParticlesProp = serializedObject.FindProperty("playLocalParticles");

            // Audio
            collectAudioProp = serializedObject.FindProperty("collectAudio");

            // Disable
            visualObjectProp = serializedObject.FindProperty("visualObject");
            hideVisualImmediatelyProp = serializedObject.FindProperty("hideVisualImmediately");
            disableGameObjectDelayProp = serializedObject.FindProperty("disableGameObjectDelay");

            // Persistence
            persistentCollectionProp = serializedObject.FindProperty("persistentCollection");
            persistentSaveKeyProp = serializedObject.FindProperty("persistentSaveKey");

            // Create placeholder header
            CreateHeaderPlaceholder();
        }

        private void CreateHeaderPlaceholder()
        {
            headerPlaceholder = new Texture2D(1, 1);
            headerPlaceholder.SetPixel(0, 0, new Color(0.15f, 0.4f, 0.3f, 0.3f));
            headerPlaceholder.Apply();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Collectible items and currency that can be picked up via trigger collision or raycast detection. Supports visual effects, audio feedback, and persistent collection state.", MessageType.Info);

            GUILayout.Space(6);
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;
            GUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawGeneralTab();
                    break;
                case 1:
                    DrawCollectionTab();
                    break;
                case 2:
                    DrawEffectsTab();
                    break;
                case 3:
                    DrawAudioTab();
                    break;
                case 4:
                    DrawPersistenceTab();
                    break;
            }

            GUILayout.Space(8);
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎯 Trigger Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("Trigger Mode", "Choose whether the trigger relies on collider callbacks or a raycast zone."));
            EditorGUILayout.PropertyField(triggerSpaceProp, new GUIContent("Trigger Space", "Switch between 3D and 2D physics."));

            if ((Collectible.TriggerMode)triggerModeProp.enumValueIndex == Collectible.TriggerMode.Raycast)
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField("Raycast Zone", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(raycastBoxSizeProp, new GUIContent("Size", "World size of the raycast box (local space)."));
                EditorGUILayout.PropertyField(raycastBoxOffsetProp, new GUIContent("Offset", "Local offset from the transform origin."));
                EditorGUILayout.PropertyField(drawDebugGizmosProp, new GUIContent("Draw Gizmos", "Toggle scene gizmos for the raycast zone."));
                if (drawDebugGizmosProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Gizmo Color"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔍 Filters", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useLayerFilterProp, new GUIContent("Use Layer Filter"));
            if (useLayerFilterProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(allowedLayersProp, new GUIContent("Allowed Layers"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(useTagFilterProp, new GUIContent("Use Tag Filter"));
            if (useTagFilterProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                string newTag = EditorGUILayout.TagField("Required Tag", requiredTagProp.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    requiredTagProp.stringValue = newTag;
                }
                EditorGUI.indentLevel--;

                if (string.IsNullOrEmpty(requiredTagProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Tag filter is enabled but no tag has been provided.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔄 Disable Behaviour", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(disableGameObjectDelayProp, new GUIContent("Disable Delay", "Delay before disabling the GameObject after collection. Useful for letting effects play."));

            float currentDelay = disableGameObjectDelayProp.floatValue;

            if (currentDelay > 0f)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hideVisualImmediatelyProp, new GUIContent("Hide Visual Immediately", "Hide the visual object immediately when collected (before the delay)."));

                if (hideVisualImmediatelyProp.boolValue)
                {
                    EditorGUILayout.PropertyField(visualObjectProp, new GUIContent("Visual Object", "Object to hide immediately. If null, uses the first child."));
                }

                EditorGUILayout.HelpBox("The collectible will wait for the delay before disabling the GameObject. This allows effects to play out.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Set a delay to allow effects to play before the object is disabled.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCollectionTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 Collection Type", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(collectibleTypeProp, new GUIContent("Type", "What this collectible awards: an Item or Currency."));

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            Collectible.CollectibleType type = (Collectible.CollectibleType)collectibleTypeProp.enumValueIndex;

            if (type == Collectible.CollectibleType.Item)
            {
                DrawItemSettings();
            }
            else
            {
                DrawCurrencySettings();
            }
        }

        private void DrawItemSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎁 Item Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(itemDefinitionProp, new GUIContent("Item Definition", "The ItemSO asset to give when collected."));

            if (itemDefinitionProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No item definition assigned. The collectible won't give any item.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(itemQuantityProp, new GUIContent("Quantity", "How many of this item to give."));

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Inventory Target", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(targetInventoryProp, new GUIContent("Target Inventory", "Specific inventory to add item to. If null, searches on collector."));

            if (targetInventoryProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No target inventory assigned. Will search for GameInventory component on the collecting object.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(autoSaveInventoryProp, new GUIContent("Auto Save", "Automatically save the inventory after adding the item."));

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrencySettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💰 Currency Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(currencyNameProp, new GUIContent("Currency Name", "Name of the currency to award (e.g., 'gold', 'coins')."));

            if (string.IsNullOrEmpty(currencyNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Currency name is required.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(currencyAmountProp, new GUIContent("Amount", "Amount of currency to award."));

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Saving Options", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(autoSaveCurrencyProp, new GUIContent("Auto Save", "Automatically save currency after modification."));
            EditorGUILayout.PropertyField(notifyCurrencyChangeProp, new GUIContent("Notify Change", "Send update event to currency listeners."));

            EditorGUILayout.EndVertical();
        }

        private void DrawEffectsTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("✨ Visual Effects", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(effectModeProp, new GUIContent("Effect Mode", "How to handle visual effects on collection."));

            Collectible.EffectMode mode = (Collectible.EffectMode)effectModeProp.enumValueIndex;

            if (mode == Collectible.EffectMode.Pooled)
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("Pooled Effect", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(pooledEffectPrefabProp, new GUIContent("Effect Prefab", "Prefab to spawn from pool."));

                if (pooledEffectPrefabProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("No effect prefab assigned.", MessageType.Info);
                }

                EditorGUILayout.PropertyField(pooledEffectLifetimeProp, new GUIContent("Lifetime", "How long the effect lives (-1 for manual control)."));
                EditorGUILayout.PropertyField(effectOffsetProp, new GUIContent("Offset", "Position offset for the spawned effect."));

                EditorGUILayout.HelpBox("Pooled effects spawn outside the collectible hierarchy and will persist after the collectible is disabled.", MessageType.Info);
            }
            else if (mode == Collectible.EffectMode.Local)
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("Local Effect", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(localEffectObjectProp, new GUIContent("Effect Object", "Local child object to enable when collected."));

                if (localEffectObjectProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("No local effect object assigned.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(playLocalParticlesProp, new GUIContent("Play Particles", "Auto-play particle systems on the effect object."));

                EditorGUILayout.HelpBox("When using local effects, set a Disable Delay in the General tab so the effect can play before the object is disabled.", MessageType.Info);

                // Check if delay is configured for local effects
                if (disableGameObjectDelayProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("⚠️ Disable Delay is 0. Local effects won't be visible! Set a delay in the General tab to allow the effect to play.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No visual effect will play on collection.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔊 Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure audio to play when this collectible is picked up.", MessageType.Info);

            GUILayout.Space(6);
            EditorHelpers.DrawAudioDropdown("Collection Audio", collectAudioProp, serializedObject);
            EditorGUILayout.HelpBox("Audio to play when the collectible is collected (uses SIGS.PlayAudio)", MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawPersistenceTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💾 Persistence Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(persistentCollectionProp, new GUIContent("Persistent Collection", "Remember that this collectible was collected across sessions."));

            if (persistentCollectionProp.boolValue)
            {
                EditorGUI.indentLevel++;

                GUILayout.Space(4);
                EditorGUILayout.LabelField("Save Key", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(persistentSaveKeyProp, new GUIContent("Save Key", "Unique key for persistent state."));
                if (GUILayout.Button("Generate", GUILayout.Width(90f)))
                {
                    GeneratePersistentKeys();
                }
                EditorGUILayout.EndHorizontal();

                if (string.IsNullOrWhiteSpace(persistentSaveKeyProp.stringValue))
                {
                    EditorGUILayout.HelpBox("Provide a unique save key so this collectible can remember its state across sessions.", MessageType.Error);
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Collection state will not be saved. The collectible will reset each time the scene loads.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnostics()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("📋 Diagnostics", EditorStyles.boldLabel);

                bool hasIssues = false;

                foreach (Object targetObject in targets)
                {
                    if (targetObject is not Collectible collectible)
                    {
                        continue;
                    }

                    if (collectible.Mode == Collectible.TriggerMode.Collider)
                    {
                        if (collectible.Space == Collectible.TriggerSpace.Space3D)
                        {
                            if (!collectible.TryGetComponent(out Collider collider))
                            {
                                EditorGUILayout.HelpBox($"{collectible.name} is missing a Collider component set as trigger.", MessageType.Warning);
                                hasIssues = true;
                                break;
                            }

                            if (!collider.isTrigger)
                            {
                                EditorGUILayout.HelpBox($"{collectible.name}'s Collider should be marked as a trigger for Collectible to work.", MessageType.Warning);
                                hasIssues = true;
                                break;
                            }
                        }
                        else
                        {
                            if (!collectible.TryGetComponent(out Collider2D collider2D))
                            {
                                EditorGUILayout.HelpBox($"{collectible.name} is missing a Collider2D component set as trigger.", MessageType.Warning);
                                hasIssues = true;
                                break;
                            }

                            if (!collider2D.isTrigger)
                            {
                                EditorGUILayout.HelpBox($"{collectible.name}'s Collider2D should be marked as a trigger for Collectible to work.", MessageType.Warning);
                                hasIssues = true;
                                break;
                            }
                        }
                    }

                    if (collectible.Mode == Collectible.TriggerMode.Raycast && !collectible.DrawsGizmos())
                    {
                        EditorGUILayout.HelpBox("Consider enabling gizmos while editing to visualize the raycast bounds.", MessageType.Info);
                    }

                    break;
                }

                if (!hasIssues)
                {
                    EditorGUILayout.HelpBox("✓ Configuration looks good!", MessageType.None);
                }

                // Pooling information
                GUILayout.Space(4);
                EditorGUILayout.LabelField("🔄 Pooling Support", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This collectible supports object pooling. When re-enabled, it automatically resets its state and can be collected again (unless persistently collected).", MessageType.Info);

                // Runtime info
                if (Application.isPlaying)
                {
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("🎮 Runtime Status", EditorStyles.boldLabel);
                    foreach (Object targetObject in targets)
                    {
                        if (targetObject is Collectible collectible)
                        {
                            string status = collectible.IsCollected ? "Collected" : "Available";
                            EditorGUILayout.LabelField("Status:", status);
                            
                            if (collectible.IsCollected)
                            {
                                if (GUILayout.Button("Force Reset (Runtime)", GUILayout.Height(22)))
                                {
                                    collectible.ForceReset();
                                    collectible.gameObject.SetActive(true);
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void GeneratePersistentKeys()
        {
            foreach (Object targetObject in targets)
            {
                if (targetObject is not Collectible collectible)
                {
                    continue;
                }

                Undo.RecordObject(collectible, "Generate Persistent Save Key");
                string generatedKey = collectible.GeneratePersistentKey();
                SerializedObject so = new SerializedObject(targetObject);
                so.Update();
                SerializedProperty keyProp = so.FindProperty("persistentSaveKey");
                keyProp.stringValue = generatedKey;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(collectible);
            }

            serializedObject.Update();
        }
    }
}
#endif
