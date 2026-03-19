using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Provides simplified physics simulation for kinematic scene objects.
    /// Unlike MovementPhysics3D (designed for character controllers), this component
    /// is meant for world objects that need basic physics behavior while remaining kinematic.
    /// Supports gravity, bounce, drag, and basic collision response.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Signalia | Fake Physics")]
    [RequireComponent(typeof(Rigidbody))]
    public class FakePhysics : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Physics Settings")]
        [Tooltip("Enable or disable the physics simulation.")]
        [SerializeField] private bool isEnabled = true;

        [Tooltip("Use Unity's global gravity or specify custom gravity.")]
        [SerializeField] private bool useGlobalGravity = true;

        [Tooltip("Custom gravity when not using global gravity.")]
        [SerializeField] private Vector3 customGravity = new Vector3(0f, -9.81f, 0f);

        [Tooltip("Multiplier applied to gravity.")]
        [SerializeField] private float gravityScale = 1f;

        [Header("Collision Settings")]
        [Tooltip("Layers to collide with.")]
        [SerializeField] private LayerMask collisionMask = ~0;

        [Tooltip("How to interact with triggers.")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("Small offset to keep object slightly separated from surfaces.")]
        [SerializeField] private float skinWidth = 0.01f;

        [Header("Response Settings")]
        [Tooltip("Bounciness coefficient (0 = no bounce, 1 = full bounce).")]
        [Range(0f, 1f)]
        [SerializeField] private float bounciness = 0.3f;

        [Tooltip("Velocity damping applied each frame (simulates air resistance).")]
        [Range(0f, 10f)]
        [SerializeField] private float drag = 0.1f;

        [Tooltip("Additional damping applied on collision (simulates friction).")]
        [Range(0f, 1f)]
        [SerializeField] private float friction = 0.2f;

        [Tooltip("Minimum velocity magnitude before object is considered at rest.")]
        [SerializeField] private float sleepThreshold = 0.05f;

        [Tooltip("Maximum velocity magnitude (prevents runaway speeds).")]
        [SerializeField] private float maxVelocity = 50f;

        [Header("Events")]
        [Tooltip("Called when the object collides with something.")]
        [SerializeField] private UnityEvent<Collision> onCollision;

        [Tooltip("Called when the object comes to rest.")]
        [SerializeField] private UnityEvent onSleep;

        [Tooltip("Called when the object starts moving after being at rest.")]
        [SerializeField] private UnityEvent onWake;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private Collider _collider;
        private Vector3 _velocity;
        private bool _isAsleep;
        private bool _wasAsleep;


        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            // Setup rigidbody for kinematic control
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Validate settings
            skinWidth = Mathf.Max(0.001f, skinWidth);
            sleepThreshold = Mathf.Max(0.001f, sleepThreshold);
            maxVelocity = Mathf.Max(0f, maxVelocity);
        }

        private void FixedUpdate()
        {
            if (!isEnabled) return;

            float dt = Time.fixedDeltaTime;

            // Track sleep state for events
            _wasAsleep = _isAsleep;

            // Apply gravity
            ApplyGravity(dt);

            // Apply drag
            ApplyDrag(dt);

            // Clamp velocity
            ClampVelocity();

            // Check for sleep
            CheckSleep();

            if (!_isAsleep)
            {
                // Move and handle collisions
                MoveAndCollide(dt);

                // Fire wake event if just woke up
                if (_wasAsleep)
                {
                    onWake?.Invoke();
                }
            }
        }

        #endregion

        #region Physics Core

        private void ApplyGravity(float dt)
        {
            Vector3 gravity = useGlobalGravity ? Physics.gravity : customGravity;
            _velocity += gravity * gravityScale * dt;
        }

        private void ApplyDrag(float dt)
        {
            if (drag > 0f)
            {
                // Exponential decay for stable damping
                float dampFactor = 1f / (1f + drag * dt);
                _velocity *= dampFactor;
            }
        }

        private void ClampVelocity()
        {
            if (maxVelocity > 0f && _velocity.sqrMagnitude > maxVelocity * maxVelocity)
            {
                _velocity = _velocity.normalized * maxVelocity;
            }
        }

        private void CheckSleep()
        {
            if (_velocity.sqrMagnitude < sleepThreshold * sleepThreshold)
            {
                if (!_isAsleep)
                {
                    _isAsleep = true;
                    _velocity = Vector3.zero;
                    onSleep?.Invoke();
                }
            }
            else
            {
                _isAsleep = false;
            }
        }

        private void MoveAndCollide(float dt)
        {
            Vector3 pos = _rb.position;
            Vector3 displacement = _velocity * dt;
            float distance = displacement.magnitude;

            if (distance < 0.0001f) return;

            Vector3 direction = displacement / distance;

            // Use rigidbody sweep test for accurate collision detection
            RaycastHit[] hits = _rb.SweepTestAll(direction, distance + skinWidth, triggerInteraction);

            if (hits.Length > 0)
            {
                // Find closest valid hit
                float closestDist = float.MaxValue;
                RaycastHit closestHit = default;
                bool hasValidHit = false;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];

                    // Skip self
                    if (hit.collider == _collider) continue;

                    // Check layer mask
                    if ((collisionMask & (1 << hit.collider.gameObject.layer)) == 0) continue;

                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        closestHit = hit;
                        hasValidHit = true;
                    }
                }

                if (hasValidHit)
                {
                    // Move to just before contact
                    float moveDist = Mathf.Max(0f, closestHit.distance - skinWidth);
                    pos += direction * moveDist;

                    // Calculate bounce response
                    Vector3 normal = closestHit.normal;
                    Vector3 reflected = Vector3.Reflect(_velocity, normal);

                    // Apply bounciness
                    _velocity = reflected * bounciness;

                    // Apply friction (reduce tangential velocity)
                    if (friction > 0f)
                    {
                        Vector3 tangent = _velocity - Vector3.Dot(_velocity, normal) * normal;
                        _velocity -= tangent * friction;
                    }

                    // Fire collision event
                    onCollision?.Invoke(CreateCollisionData(closestHit));
                }
                else
                {
                    // No valid hit, move full distance
                    pos += displacement;
                }
            }
            else
            {
                // No collision, move full distance
                pos += displacement;
            }

            _rb.MovePosition(pos);
        }

        private Collision CreateCollisionData(RaycastHit hit)
        {
            // Note: We can't create a real Collision object directly,
            // so we use null here. The event is mainly for notification.
            // For detailed collision info, users should use the hit data.
            return null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets or sets whether physics simulation is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <summary>
        /// Gets whether the object is currently at rest.
        /// </summary>
        public bool IsAsleep => _isAsleep;

        /// <summary>
        /// Gets or sets the current velocity.
        /// </summary>
        public Vector3 Velocity
        {
            get => _velocity;
            set
            {
                _velocity = value;
                _isAsleep = false;
            }
        }

        /// <summary>
        /// Applies an instant force (impulse) to the object.
        /// </summary>
        /// <param name="force">The force to apply.</param>
        public void AddForce(Vector3 force)
        {
            _velocity += force;
            _isAsleep = false;
        }

        /// <summary>
        /// Applies an instant force at a point (rotation not simulated).
        /// </summary>
        /// <param name="force">The force to apply.</param>
        /// <param name="point">The world point to apply force at (unused for linear physics).</param>
        public void AddForceAtPoint(Vector3 force, Vector3 point)
        {
            // Simple implementation - just add the force
            // A more complex version could simulate rotation
            AddForce(force);
        }

        /// <summary>
        /// Wakes up the object if it's asleep.
        /// </summary>
        public void Wake()
        {
            if (_isAsleep)
            {
                _isAsleep = false;
                onWake?.Invoke();
            }
        }

        /// <summary>
        /// Puts the object to sleep immediately.
        /// </summary>
        public void Sleep()
        {
            if (!_isAsleep)
            {
                _velocity = Vector3.zero;
                _isAsleep = true;
                onSleep?.Invoke();
            }
        }

        /// <summary>
        /// Resets velocity and sleep state.
        /// </summary>
        public void Reset()
        {
            _velocity = Vector3.zero;
            _isAsleep = true;
        }

        #region Property Accessors

        /// <summary>
        /// Gets or sets the gravity scale.
        /// </summary>
        public float GravityScale
        {
            get => gravityScale;
            set => gravityScale = value;
        }

        /// <summary>
        /// Gets or sets whether to use global gravity.
        /// </summary>
        public bool UseGlobalGravity
        {
            get => useGlobalGravity;
            set => useGlobalGravity = value;
        }

        /// <summary>
        /// Gets or sets the custom gravity vector.
        /// </summary>
        public Vector3 CustomGravity
        {
            get => customGravity;
            set => customGravity = value;
        }

        /// <summary>
        /// Gets or sets the bounciness coefficient.
        /// </summary>
        public float Bounciness
        {
            get => bounciness;
            set => bounciness = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Gets or sets the drag coefficient.
        /// </summary>
        public float Drag
        {
            get => drag;
            set => drag = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the friction coefficient.
        /// </summary>
        public float Friction
        {
            get => friction;
            set => friction = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Gets or sets the collision layer mask.
        /// </summary>
        public LayerMask CollisionMask
        {
            get => collisionMask;
            set => collisionMask = value;
        }

        /// <summary>
        /// Gets or sets the maximum velocity.
        /// </summary>
        public float MaxVelocity
        {
            get => maxVelocity;
            set => maxVelocity = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the sleep threshold.
        /// </summary>
        public float SleepThreshold
        {
            get => sleepThreshold;
            set => sleepThreshold = Mathf.Max(0.001f, value);
        }

        #endregion

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!showDebug) return;

            // Draw velocity vector
            Vector3 pos = Application.isPlaying && _rb != null ? _rb.position : transform.position;

            Gizmos.color = _isAsleep ? Color.gray : Color.cyan;
            Gizmos.DrawLine(pos, pos + _velocity * 0.5f);

            // Draw gravity direction
            Vector3 gravity = useGlobalGravity ? Physics.gravity : customGravity;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + gravity.normalized * 0.3f);
        }

        #endregion
    }
}
