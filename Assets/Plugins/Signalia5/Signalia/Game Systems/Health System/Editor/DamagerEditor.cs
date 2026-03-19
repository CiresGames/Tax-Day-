#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Health;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Health.Editors
{
    [CustomEditor(typeof(Damager)), CanEditMultipleObjects]
    public class DamagerEditor : Editor
    {
        private SerializedProperty damageAmountProp;
        private SerializedProperty damageableLayersProp;
        private SerializedProperty damagerTypeProp;
        private SerializedProperty hereShapeProp;
        private SerializedProperty sphereRadiusProp;
        private SerializedProperty boxSizeProp;
        private SerializedProperty capsuleRadiusProp;
        private SerializedProperty capsuleHeightProp;
        private SerializedProperty damagerColliderProp;
        private SerializedProperty triggerOnEnableProp;
        private SerializedProperty enableMultiHitProp;
        private SerializedProperty activeDamageDurationProp;
        private SerializedProperty damageIntervalProp;
        private SerializedProperty castDirectionProp;
        private SerializedProperty castDistanceProp;
        private SerializedProperty castRadiusProp;
        private SerializedProperty useDamageFalloffProp;
        private SerializedProperty falloffCurveProp;
        private SerializedProperty debugLogsProp;
        private SerializedProperty showGizmosProp;

        private void OnEnable()
        {
            damageAmountProp = serializedObject.FindProperty("damageAmount");
            damageableLayersProp = serializedObject.FindProperty("damageableLayers");
            damagerTypeProp = serializedObject.FindProperty("damagerType");
            hereShapeProp = serializedObject.FindProperty("hereShape");
            sphereRadiusProp = serializedObject.FindProperty("sphereRadius");
            boxSizeProp = serializedObject.FindProperty("boxSize");
            capsuleRadiusProp = serializedObject.FindProperty("capsuleRadius");
            capsuleHeightProp = serializedObject.FindProperty("capsuleHeight");
            damagerColliderProp = serializedObject.FindProperty("damagerCollider");
            triggerOnEnableProp = serializedObject.FindProperty("triggerOnEnable");
            enableMultiHitProp = serializedObject.FindProperty("enableMultiHit");
            activeDamageDurationProp = serializedObject.FindProperty("activeDamageDuration");
            damageIntervalProp = serializedObject.FindProperty("damageInterval");
            castDirectionProp = serializedObject.FindProperty("castDirection");
            castDistanceProp = serializedObject.FindProperty("castDistance");
            castRadiusProp = serializedObject.FindProperty("castRadius");
            useDamageFalloffProp = serializedObject.FindProperty("useDamageFalloff");
            falloffCurveProp = serializedObject.FindProperty("falloffCurve");
            debugLogsProp = serializedObject.FindProperty("debugLogs");
            showGizmosProp = serializedObject.FindProperty("showGizmos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(5);

            EditorGUILayout.HelpBox("Damager broadcasts damage events through Health Radio. Supports Here (AOE/melee) and There (ranged/directional) types. No GetComponent calls - fully decoupled.", MessageType.Info);

            GUILayout.Space(5);

            // Core Settings
            EditorGUILayout.LabelField("Damage Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(damageAmountProp, new GUIContent("Damage Amount", "Base damage amount to deal."));
            EditorGUILayout.PropertyField(damageableLayersProp, new GUIContent("Damageable Layers", "Layer mask for what can be damaged."));
            EditorGUILayout.PropertyField(damagerTypeProp, new GUIContent("Damager Type", "Here = AOE at position, There = directional raycast."));

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // Type-specific settings
            var damagerType = (DamagerType)damagerTypeProp.enumValueIndex;
            
            if (damagerType == DamagerType.Here)
            {
                DrawHereDamagerSettings();
            }
            else
            {
                DrawThereDamagerSettings();
            }

            GUILayout.Space(5);

            // Damage Falloff
            EditorGUILayout.LabelField("Damage Falloff", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(useDamageFalloffProp, new GUIContent("Use Damage Falloff", "Apply damage falloff based on distance."));
            
            if (useDamageFalloffProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(falloffCurveProp, new GUIContent("Falloff Curve", "Animation curve defining damage multiplier over normalized distance (0 = full damage, 1 = no damage)."), true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // Source & Debug
            EditorGUILayout.LabelField("Source & Debug", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(debugLogsProp, new GUIContent("Debug Logs", "Enable verbose logging for debugging."));
            EditorGUILayout.PropertyField(showGizmosProp, new GUIContent("Show Gizmos", "Display gizmos in scene view for visualization."));

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Runtime Actions
            if (Application.isPlaying)
            {
                DrawRuntimeActions();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHereDamagerSettings()
        {
            EditorGUILayout.LabelField("Here Damager (AOE/Melee)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(hereShapeProp, new GUIContent("Shape", "Shape used for the Here damager overlap."));

            var hereShape = (DamagerShape)hereShapeProp.enumValueIndex;
            EditorGUI.indentLevel++;
            switch (hereShape)
            {
                case DamagerShape.Sphere:
                    EditorGUILayout.PropertyField(sphereRadiusProp, new GUIContent("Sphere Radius", "Radius of the damage sphere."));
                    break;
                case DamagerShape.Box:
                    EditorGUILayout.PropertyField(boxSizeProp, new GUIContent("Box Size", "Size of the damage box."));
                    break;
                case DamagerShape.Capsule:
                    EditorGUILayout.PropertyField(capsuleRadiusProp, new GUIContent("Capsule Radius", "Radius of the capsule damage area."));
                    EditorGUILayout.PropertyField(capsuleHeightProp, new GUIContent("Capsule Height", "Height of the capsule damage area."));
                    break;
                case DamagerShape.Collider:
                    EditorGUILayout.PropertyField(damagerColliderProp, new GUIContent("Damager Collider", "Collider used as the damage area."));
                    EditorGUILayout.HelpBox("Assign a Collider to use its shape for the Here damage query.", MessageType.Info);
                    break;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(triggerOnEnableProp, new GUIContent("Trigger On Enable", "Automatically deal damage when component is enabled."));

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Multi-Hit Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableMultiHitProp, new GUIContent("Enable Multi-Hit", "Enable continuous damage over time. Only works for Here-type damage."));
            
            if (enableMultiHitProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(activeDamageDurationProp, new GUIContent("Active Damage Duration", "How long to deal damage continuously (seconds). Set to 0 to deal damage until manually stopped or disabled."));
                EditorGUILayout.PropertyField(damageIntervalProp, new GUIContent("Damage Interval", "How often to deal damage (seconds). Lower values = more frequent hits."));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.HelpBox("Multi-hit mode will continuously deal damage at the specified interval for the duration. Use this for attacks that should hit multiple times during an animation.", MessageType.Info);
            }

            EditorGUILayout.HelpBox("Here damagers deal damage to all objects within the selected shape at the damager's position. Useful for melee attacks, explosions, and AOE effects.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawThereDamagerSettings()
        {
            EditorGUILayout.LabelField("There Damager (Ranged/Directional)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(castDirectionProp, new GUIContent("Cast Direction", "Local direction for the sphere cast."));
            EditorGUILayout.PropertyField(castDistanceProp, new GUIContent("Cast Distance", "Maximum distance for the sphere cast."));
            EditorGUILayout.PropertyField(castRadiusProp, new GUIContent("Cast Radius", "Radius of the sphere cast."));

            EditorGUILayout.HelpBox("There damagers perform a directional sphere cast and hit the first valid target. Useful for bullets, gunshots, and ranged attacks.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime Actions", EditorStyles.boldLabel);

            var damager = (Damager)target;
            if (damager != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Deal Damage", GUILayout.Height(30)))
                {
                    damager.DealDamage();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // Multi-hit controls for Here-type damagers
                if ((DamagerType)damagerTypeProp.enumValueIndex == DamagerType.Here && enableMultiHitProp.boolValue)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Start Active Damage", GUILayout.Height(25)))
                    {
                        damager.StartActiveDamage();
                    }
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button("Stop Active Damage", GUILayout.Height(25)))
                    {
                        damager.StopActiveDamage();
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif