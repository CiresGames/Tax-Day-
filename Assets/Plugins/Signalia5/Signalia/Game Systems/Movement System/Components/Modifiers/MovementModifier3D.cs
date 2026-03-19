using AHAKuo.Signalia.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Defines a movement-based event that triggers when velocity and state conditions are met.
    /// </summary>
    [System.Serializable]
    public class MovementModifierEvent
    {
        /// <summary>
        /// The state requirement for this event to trigger.
        /// </summary>
        public enum MovementState
        {
            Any,
            Grounded,
            Aerial
        }

        [Tooltip("Name/identifier for this event (for organization purposes).")]
        public string eventName = "New Event";

        [Tooltip("Minimum horizontal velocity magnitude required to trigger this event.")]
        public float velocityThreshold = 1f;

        [Tooltip("The movement state required for this event to trigger.")]
        public MovementState requiredState = MovementState.Any;

        [Tooltip("Event invoked when the conditions are met (enter state).")]
        public UnityEvent onEnter = new UnityEvent();

        [Tooltip("Event invoked when the conditions are no longer met (exit state).")]
        public UnityEvent onExit = new UnityEvent();
    }

    /// <summary>
    /// A simplified character controller for 3D environments.
    /// Provides basic functionalities: moving and sprinting.
    /// For jumping and dashing, use JumpModifier3D and DashModifier3D components.
    /// Integrates with the Signalia controller wrapper system using constant actions: 
    /// "Move" (returns a Vector2) and "Sprint."
    /// 
    /// This controller uses MovementPhysics3D for all physics calculations including
    /// collision detection, gravity, and movement. The controller focuses solely on input handling
    /// and applies forces through the physics component's velocity system.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Movement/Signalia | Movement Modifier 3D")]
    public class MovementModifier3D : MovementPhysicsModifier
    {
        #region Serialized Fields

        #region Movement
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float airControlMultiplier = 0.5f;
        #endregion

        #region Input Actions
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string sprintActionName = "Sprint";
        #endregion

        #region Movement Events
        [Tooltip("Events that trigger based on velocity thresholds and movement state (grounded/aerial).")]
        [SerializeField] private MovementModifierEvent[] movementModifierEvents = new MovementModifierEvent[0];
        #endregion

        #endregion

        #region Private Fields
        private bool[] previousEventStates; // Track previous active state for each event
        #endregion

        #region Unity Lifecycle Methods

        protected override void Awake()
        {
            base.Awake();
            // Initialize previous event states array
            if (movementModifierEvents != null)
            {
                previousEventStates = new bool[movementModifierEvents.Length];
            }
        }

        protected override void Update()
        {
            base.Update(); // Track modifier enabled state changes
            
            // Check movement modifier events
            if (IsModifierEnabled && PhysicsAuthority != null)
            {
                CheckMovementEvents();
            }
        }

        private void FixedUpdate()
        {
            // Handle movement input - must be in FixedUpdate to work with physics
            HandleMovement();
        }
        #endregion

        #region Movement Methods
        private bool TryGetMainCameraPlanarForward(out Vector3 planarForward)
        {
            Camera main = Camera.main;
            if (main == null)
            {
                planarForward = default;
                return false;
            }

            planarForward = main.transform.forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude < 0.0001f)
            {
                planarForward = default;
                return false;
            }

            planarForward.Normalize();
            return true;
        }

        private void HandleMovement()
        {
            if (!IsModifierEnabled || PhysicsAuthority == null)
                return;

            Vector2 moveInput = SIGS.GetInputVector2(moveActionName);

            // Check for sprint
            bool isSprinting = SIGS.GetInput(sprintActionName);
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

            // Apply air control reduction if not grounded
            if (!PhysicsAuthority.IsGrounded)
            {
                currentSpeed *= airControlMultiplier;
            }

            // Calculate movement direction - ONLY use Camera.main, never fallback to anything else
            Vector3 movementDirection;
            if (TryGetMainCameraPlanarForward(out Vector3 mainCameraForward))
            {
                // Camera-relative movement: use Camera.main forward direction
                Vector3 camRight = Vector3.Cross(Vector3.up, mainCameraForward).normalized;
                movementDirection = (camRight * moveInput.x + mainCameraForward * moveInput.y).normalized;
            }
            else
            {
                // World space movement: use input directly when Camera.main is not available
                movementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }

            // Update horizontal velocity through physics component
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 desiredVelocity = movementDirection * currentSpeed;
                desiredVelocity = ApplyConstraintAwareVelocity(desiredVelocity);

                PhysicsAuthority.SetInternalHorizontalVelocity(
                    desiredVelocity.x,
                    desiredVelocity.z
                );
            }
            else
            {
                // Stop horizontal movement when no input (but preserve vertical velocity)
                PhysicsAuthority.SetInternalHorizontalVelocity(0f, 0f);
            }
        }
        #endregion

        private Vector3 ApplyConstraintAwareVelocity(Vector3 desiredVelocity)
        {
            if (PhysicsAuthority == null)
                return desiredVelocity;

            var constraints = PhysicsAuthority.CurrentConstraints;
            if (constraints == null || constraints.Length == 0)
                return desiredVelocity;

            Vector3 position = transform.position;
            const float distanceEpsilon = 0.01f;

            for (int i = 0; i < constraints.Length; i++)
            {
                var constraint = constraints[i];
                if (!constraint.isActive)
                    continue;

                Vector3 toAnchor = constraint.anchorPoint - position;
                float currentDist = toAnchor.magnitude;
                if (currentDist < 0.0001f)
                    continue;

                Vector3 dirToAnchor = toAnchor / currentDist;
                float radialDot = Vector3.Dot(desiredVelocity, dirToAnchor);

                switch (constraint.type)
                {
                    case ConstraintType.Metallic:
                        // Always keep motion tangent to the orbit
                        desiredVelocity -= dirToAnchor * radialDot;
                        break;

                    case ConstraintType.Rope:
                    case ConstraintType.Sticky:
                        // Only block outward motion when at/over the limit
                        if (currentDist >= constraint.distance - distanceEpsilon && radialDot < 0f)
                        {
                            desiredVelocity -= dirToAnchor * radialDot;
                        }
                        break;
                }
            }

            return desiredVelocity;
        }

        #region Movement Event Methods

        /// <summary>
        /// Checks all movement modifier events and fires enter/exit events as needed.
        /// Lightweight check that only evaluates conditions when modifier is enabled.
        /// </summary>
        private void CheckMovementEvents()
        {
            if (movementModifierEvents == null || movementModifierEvents.Length == 0)
                return;

            // Ensure previous states array matches events array length
            if (previousEventStates == null || previousEventStates.Length != movementModifierEvents.Length)
            {
                previousEventStates = new bool[movementModifierEvents.Length];
            }

            bool isGrounded = PhysicsAuthority.IsGrounded;
            Vector3 velocity = PhysicsAuthority.InternalVelocity;
            float horizontalVelocityMagnitude = new Vector2(velocity.x, velocity.z).magnitude;

            for (int i = 0; i < movementModifierEvents.Length; i++)
            {
                var movementEvent = movementModifierEvents[i];
                if (movementEvent == null)
                    continue;

                // Check if conditions are met
                bool conditionsMet = horizontalVelocityMagnitude >= movementEvent.velocityThreshold;

                // Check state requirement
                if (conditionsMet)
                {
                    switch (movementEvent.requiredState)
                    {
                        case MovementModifierEvent.MovementState.Grounded:
                            conditionsMet = isGrounded;
                            break;
                        case MovementModifierEvent.MovementState.Aerial:
                            conditionsMet = !isGrounded;
                            break;
                        case MovementModifierEvent.MovementState.Any:
                            // Already set above
                            break;
                    }
                }

                // Check for state changes and fire events
                bool wasActive = previousEventStates[i];
                if (conditionsMet && !wasActive)
                {
                    // Enter state
                    movementEvent.onEnter?.Invoke();
                    previousEventStates[i] = true;
                }
                else if (!conditionsMet && wasActive)
                {
                    // Exit state
                    movementEvent.onExit?.Invoke();
                    previousEventStates[i] = false;
                }
            }
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Gets whether the character is currently grounded.
        /// </summary>
        public bool IsGrounded => PhysicsAuthority != null && PhysicsAuthority.IsGrounded;

        /// <summary>
        /// Gets or sets the base movement speed in units per second.
        /// </summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        /// <summary>
        /// Gets or sets the sprint multiplier applied when sprinting.
        /// </summary>
        public float SprintMultiplier
        {
            get => sprintMultiplier;
            set => sprintMultiplier = value;
        }

        /// <summary>
        /// Gets or sets the air control multiplier applied when airborne.
        /// </summary>
        public float AirControlMultiplier
        {
            get => airControlMultiplier;
            set => airControlMultiplier = value;
        }

        /// <summary>
        /// Gets the current movement speed (including sprint multiplier if applicable).
        /// </summary>
        public float CurrentSpeed
        {
            get
            {
                bool isSprinting = SIGS.GetInput(sprintActionName);
                return moveSpeed * (isSprinting ? sprintMultiplier : 1f);
            }
        }

        /// <summary>
        /// Deprecated: MovementModifier3D now ONLY uses Camera.main for camera-relative movement.
        /// This method is kept for API compatibility but does nothing.
        /// </summary>
        [System.Obsolete("MovementModifier3D now only uses Camera.main. This method does nothing.")]
        public void SetCameraForward(Vector3? forward)
        {
            // No-op: MovementModifier3D only uses Camera.main
        }
        #endregion
    }
}

