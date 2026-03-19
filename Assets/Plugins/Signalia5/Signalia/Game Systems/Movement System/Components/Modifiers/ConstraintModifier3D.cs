using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Modifier that allows adding physics constraints to the movement physics system.
    /// Supports Metallic (fixed distance), Rope (elastic), and Sticky (breakable) constraint types.
    /// Perfect for grappling hooks, tethers, and sticky surfaces.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Movement/Signalia | Constraint Modifier 3D")]
    public class ConstraintModifier3D : MovementPhysicsModifier
    {
        #region Serialized Fields

        [Tooltip("Optional transform to use as the anchor point. If set, anchorOffset is applied relative to this transform.")]
        [SerializeField] private Transform anchorTarget;

        [Tooltip("World-space offset from anchorTarget, or absolute world position if anchorTarget is null.")]
        [SerializeField] private Vector3 anchorOffset;

        [Tooltip("If true, uses the current distance to anchor when activated (ignoring the distance field).")]
        [SerializeField] private bool useCurrentDistanceOnActivate = false;

        [Tooltip("Type of constraint to apply.")]
        [SerializeField] private ConstraintType constraintType = ConstraintType.Rope;

        [Tooltip("Distance parameter for the constraint. For Metallic: fixed distance. For Rope: max distance. For Sticky: threshold distance.")]
        [SerializeField] private float distance = 5f;

        [Tooltip("Elasticity factor for Rope constraints. Higher = stronger pull-back.")]
        [Range(0f, 2f)]
        [SerializeField] private float elasticity = 0.5f;

        [Tooltip("Kinetic energy threshold required to break a Sticky constraint.")]
        [SerializeField] private float breakForce = 15f;

        [Tooltip("If true, the constraint is automatically activated when this component is enabled.")]
        [SerializeField] private bool autoActivate = false;

        [Tooltip("Unique identifier for this constraint. Used for removal and lookup.")]
        [SerializeField] private string constraintId = "constraint_default";

        [Tooltip("Invoked when the constraint is activated.")]
        [SerializeField] private UnityEvent onConstraintActivated = new UnityEvent();

        [Tooltip("Invoked when the constraint is deactivated.")]
        [SerializeField] private UnityEvent onConstraintDeactivated = new UnityEvent();

        [Tooltip("Invoked when a Sticky constraint breaks due to kinetic energy exceeding the break force.")]
        [SerializeField] private UnityEvent onConstraintBroken = new UnityEvent();

        [SerializeField] private bool showDebugGizmos = true;

        #endregion

        #region Private Fields

        private bool _isConstraintActive;
        private PhysicsConstraint _currentConstraint;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Generate unique ID if default
            if (constraintId == "constraint_default")
            {
                constraintId = $"constraint_{GetInstanceID()}";
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Subscribe to constraint broken event
            if (PhysicsAuthority != null)
            {
                PhysicsAuthority.OnConstraintBroken += HandleConstraintBroken;
            }

            if (autoActivate && IsModifierEnabled)
            {
                ActivateConstraint();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unsubscribe from constraint broken event
            if (PhysicsAuthority != null)
            {
                PhysicsAuthority.OnConstraintBroken -= HandleConstraintBroken;
            }

            // Deactivate constraint when disabled
            if (_isConstraintActive)
            {
                DeactivateConstraint();
            }
        }

        protected override void Update()
        {
            base.Update();

            // Update anchor position if using a target transform
            if (_isConstraintActive && anchorTarget != null && PhysicsAuthority != null)
            {
                UpdateAnchorPosition();
            }
        }

        #endregion

        #region Constraint Management

        /// <summary>
        /// Activates the constraint with current settings.
        /// </summary>
        public void ActivateConstraint()
        {
            if (!IsModifierEnabled || PhysicsAuthority == null)
                return;

            if (_isConstraintActive)
            {
                // Already active, update it
                DeactivateConstraint();
            }

            Vector3 anchorPos = GetAnchorPosition();
            float constraintDistance = distance;

            // Use current distance if specified
            if (useCurrentDistanceOnActivate)
            {
                constraintDistance = Vector3.Distance(transform.position, anchorPos);
            }

            // Create the constraint based on type
            switch (constraintType)
            {
                case ConstraintType.Metallic:
                    _currentConstraint = PhysicsConstraint.CreateMetallic(constraintId, anchorPos, constraintDistance);
                    break;

                case ConstraintType.Rope:
                    _currentConstraint = PhysicsConstraint.CreateRope(constraintId, anchorPos, constraintDistance, elasticity);
                    break;

                case ConstraintType.Sticky:
                    _currentConstraint = PhysicsConstraint.CreateSticky(constraintId, anchorPos, constraintDistance, breakForce);
                    break;
            }

            PhysicsAuthority.AddConstraint(_currentConstraint);
            _isConstraintActive = true;

            onConstraintActivated?.Invoke();
        }

        /// <summary>
        /// Deactivates the current constraint.
        /// </summary>
        public void DeactivateConstraint()
        {
            if (!_isConstraintActive || PhysicsAuthority == null)
                return;

            PhysicsAuthority.RemoveConstraint(constraintId);
            _isConstraintActive = false;

            onConstraintDeactivated?.Invoke();
        }

        /// <summary>
        /// Toggles the constraint on/off.
        /// </summary>
        public void ToggleConstraint()
        {
            if (_isConstraintActive)
                DeactivateConstraint();
            else
                ActivateConstraint();
        }

        /// <summary>
        /// Updates the anchor position when using a target transform.
        /// </summary>
        private void UpdateAnchorPosition()
        {
            if (!_isConstraintActive || PhysicsAuthority == null)
                return;

            Vector3 newAnchorPos = GetAnchorPosition();

            // Update the constraint with new anchor position
            _currentConstraint.anchorPoint = newAnchorPos;

            // Re-add constraint with updated position
            PhysicsAuthority.AddConstraint(_currentConstraint);
        }

        private Vector3 GetAnchorPosition()
        {
            if (anchorTarget != null)
            {
                return anchorTarget.TransformPoint(anchorOffset);
            }
            return anchorOffset;
        }

        private void HandleConstraintBroken(PhysicsConstraint brokenConstraint)
        {
            if (brokenConstraint.id == constraintId)
            {
                _isConstraintActive = false;
                onConstraintBroken?.Invoke();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the constraint is currently active.
        /// </summary>
        public bool IsConstraintActive => _isConstraintActive;

        /// <summary>
        /// Gets or sets the anchor target transform.
        /// </summary>
        public Transform AnchorTarget
        {
            get => anchorTarget;
            set => anchorTarget = value;
        }

        /// <summary>
        /// Gets or sets the anchor offset.
        /// </summary>
        public Vector3 AnchorOffset
        {
            get => anchorOffset;
            set => anchorOffset = value;
        }

        /// <summary>
        /// Gets or sets the constraint type.
        /// </summary>
        public ConstraintType ConstraintType
        {
            get => constraintType;
            set => constraintType = value;
        }

        /// <summary>
        /// Gets or sets the constraint distance.
        /// </summary>
        public float Distance
        {
            get => distance;
            set => distance = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the elasticity for Rope constraints.
        /// </summary>
        public float Elasticity
        {
            get => elasticity;
            set => elasticity = Mathf.Clamp(value, 0f, 2f);
        }

        /// <summary>
        /// Gets or sets the break force for Sticky constraints.
        /// </summary>
        public float BreakForce
        {
            get => breakForce;
            set => breakForce = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the constraint ID.
        /// </summary>
        public string ConstraintId
        {
            get => constraintId;
            set => constraintId = value;
        }

        /// <summary>
        /// Gets the current anchor position in world space.
        /// </summary>
        public Vector3 CurrentAnchorPosition => GetAnchorPosition();

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the anchor point to a specific world position.
        /// </summary>
        public void SetAnchorPoint(Vector3 worldPoint)
        {
            anchorTarget = null;
            anchorOffset = worldPoint;

            if (_isConstraintActive)
            {
                UpdateAnchorPosition();
            }
        }

        /// <summary>
        /// Sets the anchor to follow a transform with optional offset.
        /// </summary>
        public void SetAnchorTarget(Transform target, Vector3 localOffset = default)
        {
            anchorTarget = target;
            anchorOffset = localOffset;

            if (_isConstraintActive)
            {
                UpdateAnchorPosition();
            }
        }

        /// <summary>
        /// Updates the constraint distance.
        /// </summary>
        public void SetDistance(float newDistance)
        {
            distance = Mathf.Max(0f, newDistance);

            if (_isConstraintActive)
            {
                _currentConstraint.distance = distance;
                PhysicsAuthority?.RemoveConstraint(constraintId);
                PhysicsAuthority?.AddConstraint(_currentConstraint);
            }
        }

        /// <summary>
        /// Sets the distance to the current distance from the anchor.
        /// </summary>
        public void SetDistanceToCurrentDistance()
        {
            Vector3 anchorPos = GetAnchorPosition();
            distance = Vector3.Distance(transform.position, anchorPos);

            if (_isConstraintActive)
            {
                _currentConstraint.distance = distance;
                PhysicsAuthority?.RemoveConstraint(constraintId);
                PhysicsAuthority?.AddConstraint(_currentConstraint);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the constraint is activated.
        /// </summary>
        public UnityEvent OnConstraintActivated => onConstraintActivated;

        /// <summary>
        /// Event invoked when the constraint is deactivated.
        /// </summary>
        public UnityEvent OnConstraintDeactivated => onConstraintDeactivated;

        /// <summary>
        /// Event invoked when a Sticky constraint breaks.
        /// </summary>
        public UnityEvent OnConstraintBrokenEvent => onConstraintBroken;

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 anchorPos = GetAnchorPosition();
            Vector3 myPos = transform.position;

            // Draw line to anchor
            switch (constraintType)
            {
                case ConstraintType.Metallic:
                    Gizmos.color = Color.cyan;
                    break;
                case ConstraintType.Rope:
                    Gizmos.color = Color.yellow;
                    break;
                case ConstraintType.Sticky:
                    Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                    break;
            }

            // Draw connection line
            if (_isConstraintActive)
            {
                Gizmos.DrawLine(myPos, anchorPos);
            }
            else
            {
                // Dashed line when inactive
                int segments = 10;
                for (int i = 0; i < segments; i += 2)
                {
                    float t1 = (float)i / segments;
                    float t2 = (float)(i + 1) / segments;
                    Gizmos.DrawLine(
                        Vector3.Lerp(myPos, anchorPos, t1),
                        Vector3.Lerp(myPos, anchorPos, t2)
                    );
                }
            }

            // Draw anchor point
            Gizmos.DrawWireSphere(anchorPos, 0.15f);

            // Draw distance sphere
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
            Gizmos.DrawWireSphere(anchorPos, distance);

            // Draw current distance indicator
            float currentDist = Vector3.Distance(myPos, anchorPos);
            if (currentDist > distance)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(anchorPos, currentDist);
            }
        }

        #endregion
    }
}
