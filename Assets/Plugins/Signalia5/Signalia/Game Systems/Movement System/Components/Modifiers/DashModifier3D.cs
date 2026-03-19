using AHAKuo.Signalia.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Handles dashing mechanics for 3D character movement.
    /// Supports dash cooldown and camera-relative or world-space dashing.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Movement/Signalia | Dash Modifier 3D")]
    public class DashModifier3D : MovementPhysicsModifier
    {
        #region Enums
        /// <summary>
        /// Direction mode for dashing.
        /// </summary>
        public enum DashDirectionMode
        {
            CameraForward,
            TransformForward
        }
        #endregion

        #region Serialized Fields

        [SerializeField] private float dashForce = 15f;
        [SerializeField] private float dashCooldown = 0.5f;
        [Tooltip("Direction mode for dashing. CameraForward uses Camera.main's forward direction. TransformForward uses the character's transform.forward.")]
        [SerializeField] private DashDirectionMode dashDirectionMode = DashDirectionMode.CameraForward;

        [SerializeField] private string dashActionName = "Dash";

        /// <summary>
        /// Event invoked when a dash is performed.
        /// Triggered immediately when the dash action executes.
        /// </summary>
        [SerializeField] private UnityEvent onDashBegin = new UnityEvent();

        /// <summary>
        /// Event invoked when the dash cooldown ends and dash becomes available again.
        /// Triggered when transitioning from cooldown to available state.
        /// </summary>
        [SerializeField] private UnityEvent onDashEnd = new UnityEvent();

        #endregion

        #region Private Fields

        private float lastDashTime;
        private bool wasOnCooldown;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Initialize cooldown state
            wasOnCooldown = false;
        }

        protected override void Update()
        {
            base.Update(); // Track modifier enabled state changes
            HandleDash();
            CheckDashCooldown();
        }

        #endregion

        #region Dash Logic
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

        private void HandleDash()
        {
            if (!IsModifierEnabled || PhysicsAuthority == null)
                return;

            // Check if dash is on cooldown
            if (Time.time - lastDashTime < dashCooldown)
            {
                return;
            }

            // Check if dash button was pressed
            if (!SIGS.GetInputDown(dashActionName))
            {
                return;
            }

            lastDashTime = Time.time;

            // Calculate dash direction based on selected mode
            Vector3 dashDir;
            if (dashDirectionMode == DashDirectionMode.CameraForward)
            {
                // Dash in Camera.main's forward direction - ONLY use Camera.main, never fallback to anything else
                if (TryGetMainCameraPlanarForward(out Vector3 mainCameraForward))
                {
                    dashDir = mainCameraForward;
                }
                else
                {
                    // Fallback to transform forward when Camera.main is not available
                    dashDir = transform.forward;
                }
            }
            else // TransformForward
            {
                // Dash in the character's facing direction
                dashDir = transform.forward;
            }
            
            // Flatten the direction to horizontal plane (remove Y component)
            dashDir.y = 0f;
            dashDir.Normalize();

            // Apply dash as external force through physics component
            Vector3 dashForceVector = new Vector3(dashDir.x * dashForce, 0f, dashDir.z * dashForce);
            PhysicsAuthority.AddExternalForce(dashForceVector);

            // Reset y velocity
            PhysicsAuthority.SetInternalVerticalVelocity(0f); // todo: make this optional?

            // Trigger dash begin event
            onDashBegin?.Invoke();
            wasOnCooldown = true;
        }

        /// <summary>
        /// Checks if dash cooldown has ended and triggers end event.
        /// </summary>
        private void CheckDashCooldown()
        {
            if (!IsModifierEnabled)
                return;

            bool isOnCooldown = Time.time - lastDashTime < dashCooldown;
            
            // If we were on cooldown and now we're not, trigger end event
            if (wasOnCooldown && !isOnCooldown)
            {
                onDashEnd?.Invoke();
            }
            
            wasOnCooldown = isOnCooldown;
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Event invoked when a dash is performed.
        /// </summary>
        public UnityEvent OnDashBegin => onDashBegin;

        /// <summary>
        /// Event invoked when the dash cooldown ends and dash becomes available again.
        /// </summary>
        public UnityEvent OnDashEnd => onDashEnd;

        #endregion

        #region Public Properties

        /// <summary>
        /// Deprecated: DashModifier3D now ONLY uses Camera.main for camera-relative dashing.
        /// This method is kept for API compatibility but does nothing.
        /// </summary>
        [System.Obsolete("DashModifier3D now only uses Camera.main. This method does nothing.")]
        public void SetCameraForward(Vector3? forward)
        {
            // No-op: DashModifier3D only uses Camera.main
        }

        /// <summary>
        /// Gets whether the dash is currently on cooldown.
        /// </summary>
        public bool IsOnCooldown => Time.time - lastDashTime < dashCooldown;

        /// <summary>
        /// Gets the remaining cooldown time.
        /// </summary>
        public float RemainingCooldown => Mathf.Max(0f, dashCooldown - (Time.time - lastDashTime));

        /// <summary>
        /// Gets or sets the dash direction mode.
        /// </summary>
        public DashDirectionMode DirectionMode
        {
            get => dashDirectionMode;
            set => dashDirectionMode = value;
        }

        #endregion
    }
}

