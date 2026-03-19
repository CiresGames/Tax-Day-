using System;
using System.Collections;
using AHAKuo.Signalia.GameSystems.Health;
using AHAKuo.Signalia.GameSystems.Movement;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Health
{
    /// <summary>
    /// Core health component that manages object health, damage handling, death logic, and system integration.
    /// Subscribes to Health Radio for decoupled damage events.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Health/Signalia | Object Health")]
    public class ObjectHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private bool canBeRevived = false;
        [SerializeField] private bool isInvulnerable = false;
        
        [SerializeField] private float invulnerabilityDuration = 0f;
        [SerializeField] private float invulnerabilityCooldown = 0.5f;
        
        [SerializeField] private MonoBehaviour inputWrapper;
        [SerializeField] private MonoBehaviour movementSystem;
        [SerializeField] private Rigidbody physicsBody;
        [SerializeField] private Rigidbody2D physicsBody2D;
        [SerializeField] private bool disableSystemsOnDeath = true;
        
        [SerializeField] private bool applyKnockback = true;
        [SerializeField] private float knockbackForce = 10f;
        [SerializeField] private bool overrideVelocity = false;
        
        [SerializeField] private UnityEvent<float> onDamageTaken;
        [SerializeField] private UnityEvent onDeath;
        [SerializeField] private UnityEvent onRevive;
        [SerializeField] private Renderer rendererForFlicker;
        [SerializeField] private float flickerInterval = 0.1f;
        
        [SerializeField] private string healthLiveKey;
        [SerializeField] private string maxHealthLiveKey;
        
        [SerializeField] private int teamId = 0;
        
        [SerializeField] private bool debugLogs = false;

        // Runtime state
        private bool isDead = false;
        private bool isTemporarilyInvulnerable = false;
        private Coroutine invulnerabilityCoroutine;
        private Coroutine flickerCoroutine;
        private string healthRadioListenerId;
        private HealthRadio.DamageEventListener damageListener;
        
        // Cached components
        private IMovementPhysicsAuthority movementPhysics;
        private bool systemsDisabled = false;

        #region Properties

        public float CurrentHealth
        {
            get => currentHealth;
            private set
            {
                currentHealth = Mathf.Clamp(value, 0f, maxHealth);
                UpdateUI();
            }
        }

        public float MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = Mathf.Max(1f, value);
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                UpdateUI();
            }
        }

        public bool IsDead => isDead;
        public bool CanBeRevived => canBeRevived;
        public bool IsInvulnerable => isInvulnerable || isTemporarilyInvulnerable;
        public int TeamId => teamId;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when damage is taken. Passes the damage amount.
        /// </summary>
        public event Action<float> OnDamage;

        /// <summary>
        /// Event fired when the object dies.
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Event fired when the object is revived.
        /// </summary>
        public event Action OnRevive;

        /// <summary>
        /// Event fired when healing occurs. Passes the heal amount.
        /// </summary>
        public event Action<float> OnHeal;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Cache movement physics if available
            movementPhysics = GetComponent<IMovementPhysicsAuthority>();
            
            // Initialize health
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        private void OnEnable()
        {
            // Subscribe to Health Radio
            damageListener = OnDamageReceived;
            healthRadioListenerId = HealthRadio.Subscribe(damageListener, gameObject);
            
            // Register UI keys
            RegisterUIKeys();
            
            if (debugLogs)
            {
                Debug.Log($"[ObjectHealth] {gameObject.name} subscribed to Health Radio.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from Health Radio
            if (!string.IsNullOrEmpty(healthRadioListenerId) && damageListener != null)
            {
                HealthRadio.Unsubscribe(healthRadioListenerId, damageListener);
                healthRadioListenerId = null;
                damageListener = null;
            }
            
            // Unregister UI keys
            UnregisterUIKeys();
            
            // Stop coroutines
            if (invulnerabilityCoroutine != null)
            {
                StopCoroutine(invulnerabilityCoroutine);
                invulnerabilityCoroutine = null;
            }
            
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            
            if (debugLogs)
            {
                Debug.Log($"[ObjectHealth] {gameObject.name} unsubscribed from Health Radio.", this);
            }
        }

        private void Start()
        {
            UpdateUI();
        }

        #endregion

        #region Health Radio Listener

        /// <summary>
        /// Called when a damage event is received from Health Radio.
        /// Performs spatial validation and applies damage if valid.
        /// </summary>
        private void OnDamageReceived(HealthRadio.DamageEvent damageEvent)
        {
            // Don't process damage if dead (unless can be revived)
            if (isDead && !canBeRevived)
                return;

            // Don't process damage if invulnerable
            if (IsInvulnerable)
                return;

            // Spatial validation
            if (damageEvent.damageRadius > 0f)
            {
                // For AOE/Here damagers: check if within radius
                float distance = Vector3.Distance(transform.position, damageEvent.hitPosition);
                if (distance > damageEvent.damageRadius)
                    return;
            }
            else
            {
                // For directional/There damagers: hit position is the collision point
                // Use a reasonable threshold to account for collider offsets
                float distance = Vector3.Distance(transform.position, damageEvent.hitPosition);
                if (distance > 2f) // 2 unit threshold for directional hits
                    return;
            }

            // Check if this object's layer is included in the damage event's target layers
            if ((damageEvent.sourceTargetLayers & (1 << gameObject.layer)) == 0)
                return;

            // Apply damage
            ApplyDamage(damageEvent.damageAmount, damageEvent.hitPosition);
        }

        #endregion

        #region Damage Application

        /// <summary>
        /// Apply damage to this health component. Can be called directly or via Health Radio.
        /// </summary>
        public void ApplyDamage(float damage, Vector3 hitPosition)
        {
            if (IsInvulnerable || (isDead && !canBeRevived))
                return;

            if (damage <= 0f)
                return;

            CurrentHealth -= damage;

            if (debugLogs)
            {
                Debug.Log($"[ObjectHealth] {gameObject.name} took {damage} damage. Health: {CurrentHealth}/{MaxHealth}", this);
            }

            // Apply knockback
            if (applyKnockback && damage > 0f)
            {
                ApplyKnockback(hitPosition, damage);
            }

            // Trigger damage feedback
            onDamageTaken?.Invoke(damage);
            OnDamage?.Invoke(damage);

            // Check for death
            if (CurrentHealth <= 0f && !isDead)
            {
                Die();
            }
            else if (invulnerabilityCooldown > 0f)
            {
                // Start temporary invulnerability
                StartTemporaryInvulnerability();
            }
        }

        /// <summary>
        /// Apply damage directly to this health component without requiring a hit position.
        /// </summary>
        public void ApplyDamage(float damage)
        {
            ApplyDamage(damage, transform.position);
        }

        /// <summary>
        /// Apply knockback force based on hit position.
        /// </summary>
        private void ApplyKnockback(Vector3 hitPosition, float damage)
        {
            Vector3 direction = (transform.position - hitPosition).normalized;
            direction.y = 0f; // Keep knockback horizontal
            direction.Normalize();

            float force = knockbackForce * (damage / maxHealth);

            // Apply to physics body
            if (physicsBody != null)
            {
                if (overrideVelocity)
                {
#if UNITY_6000_0_OR_NEWER
                    physicsBody.linearVelocity = direction * force;
#else
                    physicsBody.velocity = direction * force;
#endif
                }
                else
                {
                    physicsBody.AddForce(direction * force, ForceMode.Impulse);
                }
            }
            else if (physicsBody2D != null)
            {
                Vector2 direction2D = new Vector2(direction.x, direction.z);
                if (overrideVelocity)
                {
#if UNITY_6000_0_OR_NEWER
                    physicsBody2D.linearVelocity = direction2D * force;
#else
                    physicsBody2D.velocity = direction2D * force;
#endif
                }
                else
                {
                    physicsBody2D.AddForce(direction2D * force, ForceMode2D.Impulse);
                }
            }
            // Apply to movement system if available
            else if (movementPhysics != null)
            {
                movementPhysics.AddExternalForce(direction * force);
            }
        }

        #endregion

        #region Death Handling

        /// <summary>
        /// Mark this object as dead and handle death logic.
        /// </summary>
        private void Die()
        {
            if (isDead)
                return;

            isDead = true;
            CurrentHealth = 0f;

            if (debugLogs)
            {
                Debug.Log($"[ObjectHealth] {gameObject.name} died.", this);
            }

            // Disable systems
            if (disableSystemsOnDeath)
            {
                DisableSystems();
            }

            // Trigger death event
            onDeath?.Invoke();
            OnDeath?.Invoke();

            // Unsubscribe from Health Radio (no longer receives damage)
            if (!string.IsNullOrEmpty(healthRadioListenerId) && damageListener != null)
            {
                HealthRadio.Unsubscribe(healthRadioListenerId, damageListener);
                healthRadioListenerId = null;
            }
        }

        /// <summary>
        /// Revive this object if it can be revived.
        /// </summary>
        public void Revive(float healthPercent = 1f)
        {
            if (!isDead || !canBeRevived)
                return;

            isDead = false;
            CurrentHealth = maxHealth * healthPercent;

            // Re-enable systems
            if (disableSystemsOnDeath && systemsDisabled)
            {
                EnableSystems();
            }

            // Re-subscribe to Health Radio
            if (string.IsNullOrEmpty(healthRadioListenerId))
            {
                damageListener = OnDamageReceived;
                healthRadioListenerId = HealthRadio.Subscribe(damageListener, gameObject);
            }

            // Trigger revive event
            onRevive?.Invoke();
            OnRevive?.Invoke();

            if (debugLogs)
            {
                Debug.Log($"[ObjectHealth] {gameObject.name} revived with {CurrentHealth} health.", this);
            }
        }

        #endregion

        #region System Integration

        /// <summary>
        /// Disable connected systems on death.
        /// </summary>
        private void DisableSystems()
        {
            if (systemsDisabled)
                return;

            systemsDisabled = true;

            if (inputWrapper != null)
                inputWrapper.enabled = false;

            if (movementSystem != null)
                movementSystem.enabled = false;

            if (movementPhysics != null)
            {
                movementPhysics.SetInternalVelocity(Vector3.zero);
            }
        }

        /// <summary>
        /// Re-enable connected systems after revive.
        /// </summary>
        private void EnableSystems()
        {
            if (!systemsDisabled)
                return;

            systemsDisabled = false;

            if (inputWrapper != null)
                inputWrapper.enabled = true;

            if (movementSystem != null)
                movementSystem.enabled = true;
        }

        #endregion

        #region Invulnerability

        /// <summary>
        /// Start temporary invulnerability period.
        /// </summary>
        private void StartTemporaryInvulnerability()
        {
            if (invulnerabilityDuration <= 0f)
                return;

            if (invulnerabilityCoroutine != null)
            {
                StopCoroutine(invulnerabilityCoroutine);
            }

            invulnerabilityCoroutine = StartCoroutine(InvulnerabilityCoroutine());
        }

        private IEnumerator InvulnerabilityCoroutine()
        {
            isTemporarilyInvulnerable = true;

            // Start flicker effect
            if (rendererForFlicker != null)
            {
                flickerCoroutine = StartCoroutine(FlickerCoroutine());
            }

            yield return new WaitForSeconds(invulnerabilityDuration);

            isTemporarilyInvulnerable = false;

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
                
                // Restore renderer visibility
                if (rendererForFlicker != null)
                {
                    foreach (var material in rendererForFlicker.materials)
                    {
                        if (material.HasProperty("_Color"))
                        {
                            var color = material.color;
                            color.a = 1f;
                            material.color = color;
                        }
                    }
                }
            }

            invulnerabilityCoroutine = null;
        }

        private IEnumerator FlickerCoroutine()
        {
            bool visible = true;
            
            while (isTemporarilyInvulnerable && rendererForFlicker != null)
            {
                visible = !visible;
                
                foreach (var material in rendererForFlicker.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        var color = material.color;
                        color.a = visible ? 1f : 0.3f;
                        material.color = color;
                    }
                }
                
                yield return new WaitForSeconds(flickerInterval);
            }
        }

        #endregion

        #region UI Binding

        /// <summary>
        /// Register health values with Signalia UI key-value system.
        /// </summary>
        private void RegisterUIKeys()
        {
            if (!string.IsNullOrEmpty(healthLiveKey))
            {
                SimpleRadio.LiveKeyDictionary[healthLiveKey] = () => CurrentHealth;
            }

            if (!string.IsNullOrEmpty(maxHealthLiveKey))
            {
                SimpleRadio.LiveKeyDictionary[maxHealthLiveKey] = () => MaxHealth;
            }
        }

        /// <summary>
        /// Unregister health values from Signalia UI key-value system.
        /// </summary>
        private void UnregisterUIKeys()
        {
            if (!string.IsNullOrEmpty(healthLiveKey) && SimpleRadio.DoesLiveKeyExist(healthLiveKey))
            {
                SimpleRadio.LiveKeyDictionary.Remove(healthLiveKey);
            }

            if (!string.IsNullOrEmpty(maxHealthLiveKey) && SimpleRadio.DoesLiveKeyExist(maxHealthLiveKey))
            {
                SimpleRadio.LiveKeyDictionary.Remove(maxHealthLiveKey);
            }
        }

        /// <summary>
        /// Update UI bindings.
        /// </summary>
        private void UpdateUI()
        {
            // UI keys are live keys, so they update automatically when accessed
            // But we can trigger a refresh by re-registering
            if (!string.IsNullOrEmpty(healthLiveKey))
            {
                SimpleRadio.LiveKeyDictionary[healthLiveKey] = () => CurrentHealth;
            }

            if (!string.IsNullOrEmpty(maxHealthLiveKey))
            {
                SimpleRadio.LiveKeyDictionary[maxHealthLiveKey] = () => MaxHealth;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Heal this object by the specified amount.
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead && !canBeRevived)
                return;

            float healthBefore = currentHealth;
            CurrentHealth += amount;
            float actualHealAmount = currentHealth - healthBefore;

            if (debugLogs)
            {
                Debug.Log($"[ObjectHealth] {gameObject.name} healed for {amount}. Health: {CurrentHealth}/{MaxHealth}", this);
            }

            // Trigger heal event
            if (actualHealAmount > 0f)
            {
                OnHeal?.Invoke(actualHealAmount);
            }
        }

        /// <summary>
        /// Set invulnerability state.
        /// </summary>
        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        /// <summary>
        /// Set the team ID for team-based damage filtering.
        /// </summary>
        public void SetTeamId(int teamId)
        {
            this.teamId = teamId;
        }

        #endregion
    }
}