using AHAKuo.Signalia.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Handles jumping mechanics for 3D character movement.
    /// Supports multiple jumps, coyote time, and jump cooldown.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Movement/Signalia | Jump Modifier 3D")]
    public class JumpModifier3D : MovementPhysicsModifier
    {
        #region Serialized Fields

        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private int maxJumps = 1;
        [SerializeField] private float jumpCooldown = 0.1f;
        [SerializeField] private float coyoteTime = 0.2f;

        [SerializeField] private string jumpActionName = "Jump";

        /// <summary>
        /// Event invoked when a jump is performed.
        /// Triggered immediately when the jump action executes.
        /// </summary>
        [SerializeField] private UnityEvent onJumpBegin = new UnityEvent();

        /// <summary>
        /// Event invoked when the character lands after jumping.
        /// Triggered when transitioning from air to ground.
        /// </summary>
        [SerializeField] private UnityEvent onJumpEnd = new UnityEvent();

        #endregion

        #region Private Fields

        private int jumpCount;
        private float lastJumpTime;
        private bool wasGrounded;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Initialize wasGrounded to current state
            if (PhysicsAuthority != null)
            {
                wasGrounded = PhysicsAuthority.IsGrounded;
            }
        }

        protected override void Update()
        {
            base.Update(); // Track modifier enabled state changes
            HandleJump();
        }

        #endregion

        #region Jump Logic

        private void HandleJump()
        {
            if (!IsModifierEnabled || PhysicsAuthority == null)
                return;

            // Reset jump count when landing (transitioning from air to ground)
            bool isGrounded = PhysicsAuthority.IsGrounded;
            if (isGrounded && !wasGrounded)
            {
                // Just landed - reset jump count and trigger jump end event
                jumpCount = 0;
                onJumpEnd?.Invoke();
            }
            wasGrounded = isGrounded;

            // Check cooldown
            if (Time.time - lastJumpTime < jumpCooldown)
            {
                return;
            }

            // Check if jump button was pressed
            if (!SIGS.GetInputDown(jumpActionName))
            {
                return;
            }

            // Check if we can jump (grounded or within coyote time, and haven't exceeded max jumps)
            bool canJump = false;
            bool withinCoyoteTime = Time.time - PhysicsAuthority.LastGroundedTime <= coyoteTime;

            if (isGrounded || withinCoyoteTime)
            {
                // When grounded or in coyote time, allow jump if we haven't exceeded max jumps
                canJump = jumpCount < maxJumps;
            }
            else
            {
                // When in the air, allow jump if we've already jumped at least once and haven't exceeded max jumps
                // This enables multiple mid-air jumps
                canJump = jumpCount > 0 && jumpCount < maxJumps;
            }

            if (!canJump)
            {
                return;
            }

            // Perform jump
            jumpCount++;
            lastJumpTime = Time.time;

            // Set vertical velocity for jump through physics component
            PhysicsAuthority.SetInternalVerticalVelocity(jumpForce);

            // Trigger jump begin event
            onJumpBegin?.Invoke();
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Event invoked when a jump is performed.
        /// </summary>
        public UnityEvent OnJumpBegin => onJumpBegin;

        /// <summary>
        /// Event invoked when the character lands after jumping.
        /// </summary>
        public UnityEvent OnJumpEnd => onJumpEnd;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current jump count.
        /// </summary>
        public int JumpCount => jumpCount;

        /// <summary>
        /// Gets whether the character can currently jump.
        /// </summary>
        public bool CanJump
        {
            get
            {
                if (PhysicsAuthority == null)
                    return false;

                bool withinCoyoteTime = Time.time - PhysicsAuthority.LastGroundedTime <= coyoteTime;
                if (PhysicsAuthority.IsGrounded || withinCoyoteTime)
                {
                    return jumpCount < maxJumps;
                }
                return jumpCount > 0 && jumpCount < maxJumps;
            }
        }

        #endregion
    }
}

