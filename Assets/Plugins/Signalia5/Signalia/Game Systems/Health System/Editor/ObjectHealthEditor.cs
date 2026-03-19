#if UNITY_EDITOR
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.Editors;
using AHAKuo.Signalia.GameSystems.Health;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Health.Editors
{
    [CustomEditor(typeof(ObjectHealth)), CanEditMultipleObjects]
    public class ObjectHealthEditor : Editor
    {
        private SerializedProperty maxHealthProp;
        private SerializedProperty currentHealthProp;
        private SerializedProperty canBeRevivedProp;
        private SerializedProperty isInvulnerableProp;
        private SerializedProperty invulnerabilityDurationProp;
        private SerializedProperty invulnerabilityCooldownProp;
        private SerializedProperty inputWrapperProp;
        private SerializedProperty movementSystemProp;
        private SerializedProperty physicsBodyProp;
        private SerializedProperty physicsBody2DProp;
        private SerializedProperty disableSystemsOnDeathProp;
        private SerializedProperty applyKnockbackProp;
        private SerializedProperty knockbackForceProp;
        private SerializedProperty overrideVelocityProp;
        private SerializedProperty onDamageTakenProp;
        private SerializedProperty onDeathProp;
        private SerializedProperty onReviveProp;
        private SerializedProperty rendererForFlickerProp;
        private SerializedProperty flickerIntervalProp;
        private SerializedProperty healthLiveKeyProp;
        private SerializedProperty maxHealthLiveKeyProp;
        private SerializedProperty debugLogsProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Health", "Systems", "Feedback", "UI", "Advanced" };

        private void OnEnable()
        {
            maxHealthProp = serializedObject.FindProperty("maxHealth");
            currentHealthProp = serializedObject.FindProperty("currentHealth");
            canBeRevivedProp = serializedObject.FindProperty("canBeRevived");
            isInvulnerableProp = serializedObject.FindProperty("isInvulnerable");
            invulnerabilityDurationProp = serializedObject.FindProperty("invulnerabilityDuration");
            invulnerabilityCooldownProp = serializedObject.FindProperty("invulnerabilityCooldown");
            inputWrapperProp = serializedObject.FindProperty("inputWrapper");
            movementSystemProp = serializedObject.FindProperty("movementSystem");
            physicsBodyProp = serializedObject.FindProperty("physicsBody");
            physicsBody2DProp = serializedObject.FindProperty("physicsBody2D");
            disableSystemsOnDeathProp = serializedObject.FindProperty("disableSystemsOnDeath");
            applyKnockbackProp = serializedObject.FindProperty("applyKnockback");
            knockbackForceProp = serializedObject.FindProperty("knockbackForce");
            overrideVelocityProp = serializedObject.FindProperty("overrideVelocity");
            onDamageTakenProp = serializedObject.FindProperty("onDamageTaken");
            onDeathProp = serializedObject.FindProperty("onDeath");
            onReviveProp = serializedObject.FindProperty("onRevive");
            rendererForFlickerProp = serializedObject.FindProperty("rendererForFlicker");
            flickerIntervalProp = serializedObject.FindProperty("flickerInterval");
            healthLiveKeyProp = serializedObject.FindProperty("healthLiveKey");
            maxHealthLiveKeyProp = serializedObject.FindProperty("maxHealthLiveKey");
            debugLogsProp = serializedObject.FindProperty("debugLogs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(5);

            EditorGUILayout.HelpBox("Object Health manages health state, damage handling, death logic, and system integration. Subscribes to Health Radio for decoupled damage events.", MessageType.Info);

            GUILayout.Space(5);

            // Tab Selection
            GUI.backgroundColor = Color.gray;
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(24));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            // Tab Content
            switch (selectedTab)
            {
                case 0: DrawHealthTab(); break;
                case 1: DrawSystemsTab(); break;
                case 2: DrawFeedbackTab(); break;
                case 3: DrawUITab(); break;
                case 4: DrawAdvancedTab(); break;
            }

            GUILayout.Space(10);

            // Runtime Info
            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHealthTab()
        {
            EditorGUILayout.LabelField("Core Health", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(maxHealthProp, new GUIContent("Max Health", "Maximum health value."));
            EditorGUILayout.PropertyField(currentHealthProp, new GUIContent("Current Health", "Current health value. Clamped to Max Health."));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(canBeRevivedProp, new GUIContent("Can Be Revived", "Whether this object can be revived after death."));
            EditorGUILayout.PropertyField(isInvulnerableProp, new GUIContent("Is Invulnerable", "Permanent invulnerability state."));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Temporary Invulnerability", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(invulnerabilityDurationProp, new GUIContent("Duration", "Duration of temporary invulnerability after taking damage."));
            EditorGUILayout.PropertyField(invulnerabilityCooldownProp, new GUIContent("Cooldown", "Cooldown before temporary invulnerability can trigger again."));

            EditorGUILayout.EndVertical();
        }

        private void DrawSystemsTab()
        {
            EditorGUILayout.LabelField("System Integration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(inputWrapperProp, new GUIContent("Input Wrapper", "Optional input wrapper component to disable on death."));
            EditorGUILayout.PropertyField(movementSystemProp, new GUIContent("Movement System", "Optional movement system component to disable on death."));
            EditorGUILayout.PropertyField(physicsBodyProp, new GUIContent("Physics Body (3D)", "Optional Rigidbody for knockback and velocity override."));
            EditorGUILayout.PropertyField(physicsBody2DProp, new GUIContent("Physics Body (2D)", "Optional Rigidbody2D for knockback and velocity override."));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(disableSystemsOnDeathProp, new GUIContent("Disable Systems On Death", "Automatically disable connected systems when object dies."));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Knockback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(applyKnockbackProp, new GUIContent("Apply Knockback", "Apply knockback force when taking damage."));
            
            if (applyKnockbackProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(knockbackForceProp, new GUIContent("Knockback Force", "Force multiplier for knockback."));
                EditorGUILayout.PropertyField(overrideVelocityProp, new GUIContent("Override Velocity", "Override velocity instead of adding force."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFeedbackTab()
        {
            EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(onDamageTakenProp, new GUIContent("On Damage Taken", "UnityEvent invoked when damage is taken. Passes damage amount."), true);
            EditorGUILayout.PropertyField(onDeathProp, new GUIContent("On Death", "UnityEvent invoked when object dies."), true);
            EditorGUILayout.PropertyField(onReviveProp, new GUIContent("On Revive", "UnityEvent invoked when object is revived."), true);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rendererForFlickerProp, new GUIContent("Renderer For Flicker", "Renderer component to flicker during temporary invulnerability."));
            
            if (rendererForFlickerProp.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(flickerIntervalProp, new GUIContent("Flicker Interval", "Time between flicker state changes."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUITab()
        {
            EditorGUILayout.LabelField("UI Binding", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox("Bind health values to Signalia UI key-value entries for health bars and indicators.", MessageType.Info);

            EditorGUILayout.PropertyField(healthLiveKeyProp, new GUIContent("Health LiveKey", "Signalia LiveKey for current health value."));
            EditorGUILayout.PropertyField(maxHealthLiveKeyProp, new GUIContent("Max Health LiveKey", "Signalia LiveKey for maximum health value."));

            if (string.IsNullOrEmpty(healthLiveKeyProp.stringValue) && string.IsNullOrEmpty(maxHealthLiveKeyProp.stringValue))
            {
                EditorGUILayout.HelpBox("No UI keys assigned. Health values will not be available to UI components.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedTab()
        {
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(debugLogsProp, new GUIContent("Debug Logs", "Enable verbose logging for debugging."));

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            var health = (ObjectHealth)target;
            if (health != null)
            {
                EditorGUILayout.LabelField($"Current Health: {health.CurrentHealth:F1} / {health.MaxHealth:F1}");
                EditorGUILayout.LabelField($"Is Dead: {health.IsDead}");
                EditorGUILayout.LabelField($"Is Invulnerable: {health.IsInvulnerable}");
                EditorGUILayout.LabelField($"Can Be Revived: {health.CanBeRevived}");
                EditorGUILayout.LabelField($"Team ID: {health.TeamId}");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Heal 10", GUILayout.Height(25)))
                {
                    health.Heal(10f);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Damage 10", GUILayout.Height(25)))
                {
                    health.ApplyDamage(10f, health.transform.position);
                }
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Revive", GUILayout.Height(25)))
                {
                    health.Revive();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif