using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Deterministic kinematic character physics using capsule casts + penetration resolution.
    /// Supports internal velocity, external forces, kinetic energy buildup, and constraint-based motion.
    /// Pair it with modifier scripts that modify its velocity vectors (movement, dashing, jumping, constraints, etc.)
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Movement/Signalia | Physics 3D")]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class MovementPhysics3D : MonoBehaviour, IMovementPhysicsAuthority
    {
        #region Serialized Fields

        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("Small offset to keep capsule slightly above ground (prevents jitter).")]
        [SerializeField] private float skinWidth = 0.02f;

        [Tooltip("Max slope angle considered 'ground'.")]
        [Range(0f, 89f)]
        [SerializeField] private float maxSlopeAngle = 55f;

        [SerializeField] private bool useCustomGravity = true;
        [SerializeField] private float gravityMultiplier = 1f;
        [SerializeField] private float maxFallSpeed = 50f;

        [Tooltip("Mass of the object. Influences external force decay and kinetic energy behavior.")]
        [SerializeField] private float mass = 1f;

        [Tooltip("Rate at which kinetic energy decays. Lower = more momentum retention.")]
        [SerializeField] private float kineticEnergyDecayRate = 5f;

        [Tooltip("Rate at which kinetic energy builds up from motion.")]
        [SerializeField] private float kineticEnergyBuildupRate = 0.5f;

        [Tooltip("Maximum kinetic energy magnitude.")]
        [SerializeField] private float maxKineticEnergy = 20f;

        [Tooltip("Maximum horizontal velocity (X and Z components). Set to 0 to disable clamping.")]
        [SerializeField] private float maxHorizontalVelocity = 0f;

        [Tooltip("Maximum upward velocity. Set to 0 to disable clamping.")]
        [SerializeField] private float maxVerticalVelocity = 0f;

        [Tooltip("Maximum total velocity magnitude. Set to 0 to disable clamping.")]
        [SerializeField] private float maxTotalVelocity = 0f;

        [Tooltip("How many iterations to resolve casts + sliding per FixedUpdate.")]
        [Range(1, 8)]
        [SerializeField] private int moveIterations = 4;

        [Tooltip("How many overlap/penetration resolve passes to run after moving.")]
        [Range(0, 4)]
        [SerializeField] private int depenetrationIterations = 2;

        [SerializeField] private float externalVelocityDecayRate = 12f;

        [Tooltip("When frozen, no velocities are applied and the object remains stationary.")]
        [SerializeField] private bool frozen = false;

        [SerializeField] private bool isEnabled = true;

        [SerializeField] private UnityEvent<PhysicsCollisionInfo> onCollision = new UnityEvent<PhysicsCollisionInfo>();
        [SerializeField] private UnityEvent onGrounded = new UnityEvent();
        [SerializeField] private UnityEvent onAirborne = new UnityEvent();
        [SerializeField] private UnityEvent<PhysicsConstraint> onConstraintBroken = new UnityEvent<PhysicsConstraint>();

        [SerializeField] private bool showDebug = false;
        [SerializeField] private bool showVelocityVectors = true;
        [SerializeField] private bool showConstraints = true;
        [SerializeField] private bool showKineticEnergy = true;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private CapsuleCollider _capsule;

        // Core velocity vectors
        private Vector3 _internalVelocity;
        private Vector3 _externalForce;
        private Vector3 _kineticEnergy;

        // Grounding state
        private bool _isGrounded;
        private float _lastGroundedTime;
        private Vector3 _groundNormal = Vector3.up;
        private RaycastHit _groundHit;

        // State tracking
        private bool _wasGrounded;
        private bool _wasFrozen;

        // Constraints
        private readonly List<PhysicsConstraint> _constraints = new List<PhysicsConstraint>();

        // Events (C# events for interface)
        private event Action<PhysicsCollisionInfo> _onPhysicsCollision;
        private event Action _onBecameGrounded;
        private event Action _onBecameAirborne;
        private event Action<PhysicsConstraint> _onConstraintBroken;

        // Cached results
        private static readonly Collider[] s_overlapCache = new Collider[16];

        // Late velocities (previous frame) - captured at start of FixedUpdate
        private LateVelocities _lateVelocities;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();

            // Kinematic RB for deterministic movement
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Validate settings
            ValidateSettings();
        }

        private void OnEnable()
        {
            _wasGrounded = _isGrounded;
            _wasFrozen = frozen;
        }

        private void FixedUpdate()
        {
            if (!isEnabled)
                return;

            // Capture velocities from previous frame before any modifications
            _lateVelocities = LateVelocities.Create(_internalVelocity, _externalForce, _kineticEnergy);

            // Check frozen state
            if (frozen)
            {
                if (!_wasFrozen)
                {
                    // Just became frozen - clear velocities
                    _internalVelocity = Vector3.zero;
                    _externalForce = Vector3.zero;
                }
                _wasFrozen = frozen;
                return;
            }
            _wasFrozen = frozen;

            float dt = Time.fixedDeltaTime;

            // Store previous grounded state
            _wasGrounded = _isGrounded;

            // 1. Update grounding
            UpdateGrounding(dt);

            // 2. Apply gravity (if airborne)
            if (useCustomGravity)
                ApplyGravity(dt);

            // 3. Decay external forces (mass-influenced)
            DecayExternalForce(dt);

            // 4. Update kinetic energy
            UpdateKineticEnergy(dt);

            // 5. Apply constraint forces (velocity-space)
            ApplyConstraintForces(dt);

            // 6. Clamp velocities
            ClampVelocities();

            // 7. Step kinematic movement
            StepKinematic(dt);

            // 8. Re-evaluate grounding after movement
            UpdateGrounding(dt);

            // 9. Fire grounded/airborne events
            CheckGroundingStateChange();

            // 10. Zero vertical velocity when grounded and moving down
            if (_isGrounded && _internalVelocity.y < 0f)
                _internalVelocity.y = 0f;
        }

        private void ValidateSettings()
        {
            skinWidth = Mathf.Max(0.0001f, skinWidth);
            moveIterations = Mathf.Max(1, moveIterations);
            depenetrationIterations = Mathf.Clamp(depenetrationIterations, 0, 4);
            maxHorizontalVelocity = Mathf.Max(0f, maxHorizontalVelocity);
            maxVerticalVelocity = Mathf.Max(0f, maxVerticalVelocity);
            maxTotalVelocity = Mathf.Max(0f, maxTotalVelocity);
            mass = Mathf.Max(0.01f, mass);
            kineticEnergyDecayRate = Mathf.Max(0f, kineticEnergyDecayRate);
            kineticEnergyBuildupRate = Mathf.Max(0f, kineticEnergyBuildupRate);
            maxKineticEnergy = Mathf.Max(0f, maxKineticEnergy);
        }

        #endregion

        #region Core Physics

        private void StepKinematic(float dt)
        {
            Vector3 pos = _rb.position;

            // Total desired displacement this tick (internal + external + kinetic)
            Vector3 totalVel = _internalVelocity + _externalForce + _kineticEnergy;
            Vector3 desiredDelta = totalVel * dt;

            // If grounded, project horizontal motion onto ground plane
            if (_isGrounded)
            {
                Vector3 horiz = new Vector3(desiredDelta.x, 0f, desiredDelta.z);
                horiz = Vector3.ProjectOnPlane(horiz, _groundNormal);
                desiredDelta.x = horiz.x;
                desiredDelta.z = horiz.z;
            }

            // Resolve movement with capsule casts + sliding
            Vector3 remaining = desiredDelta;
            for (int iter = 0; iter < moveIterations; iter++)
            {
                float dist = remaining.magnitude;
                if (dist <= 0.000001f) break;

                Vector3 dir = remaining / dist;

                if (CapsuleCast(pos, dir, dist + skinWidth, out RaycastHit hit))
                {
                    float impactVelocity = totalVel.magnitude;

                    // Move up to contact minus skin
                    float moveDist = Mathf.Max(0f, hit.distance - skinWidth);
                    pos += dir * moveDist;

                    // Slide remaining along hit surface
                    Vector3 leftover = remaining - dir * moveDist;

                    // If we hit "ground-ish" while moving down, treat as ground
                    if (Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle)
                    {
                        _groundNormal = hit.normal;
                        if (Vector3.Dot(leftover, Vector3.down) > 0f)
                            leftover = Vector3.ProjectOnPlane(leftover, hit.normal);
                    }

                    // Slide along surface
                    leftover = Vector3.ProjectOnPlane(leftover, hit.normal);
                    remaining = leftover;

                    // Remove velocity component into surface
                    RemoveIntoSurfaceVelocity(hit.normal);

                    // Fire collision event
                    FireCollisionEvent(hit, impactVelocity);
                }
                else
                {
                    pos += remaining;
                    remaining = Vector3.zero;
                    break;
                }
            }

            // Snap down when appropriate
            pos = GroundSnap(pos, dt);

            // Apply constraint corrections after snapping
            pos = ApplyConstraintCorrections(pos);

            // Apply depenetration
            pos = Depenetrate(pos);

            _rb.MovePosition(pos);
        }

        private void UpdateGrounding(float dt)
        {
            Vector3 pos = _rb.position;
            // Use skinWidth as the probe distance - this ensures we detect ground
            // exactly at the collider boundary without extra offset
            float probeDist = skinWidth * 2f;

            if (CapsuleCast(pos, Vector3.down, probeDist, out RaycastHit hit))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle <= maxSlopeAngle)
                {
                    _isGrounded = true;
                    _groundNormal = hit.normal;
                    _groundHit = hit;
                    _lastGroundedTime = Time.time;
                    return;
                }
            }

            _isGrounded = false;
            _groundNormal = Vector3.up;
        }

        private void ApplyGravity(float dt)
        {
            if (_isGrounded)
                return;

            float g = Physics.gravity.y * gravityMultiplier;
            _internalVelocity.y += g * dt;

            if (_internalVelocity.y < -maxFallSpeed)
                _internalVelocity.y = -maxFallSpeed;
        }

        private void DecayExternalForce(float dt)
        {
            // Mass-influenced decay: heavier objects decay slower
            float effectiveDecay = externalVelocityDecayRate / Mathf.Max(0.01f, mass);
            float t = 1f / (1f + effectiveDecay * dt);

            _externalForce *= t;

            // Zero out tiny values
            if (_externalForce.sqrMagnitude < 0.0001f)
                _externalForce = Vector3.zero;
        }

        private void UpdateKineticEnergy(float dt)
        {
            // Build up from combined motion
            Vector3 combinedVelocity = _internalVelocity + _externalForce;
            float speed = combinedVelocity.magnitude;

            if (speed > 0.01f)
            {
                float speedContribution = speed * kineticEnergyBuildupRate * dt;
                _kineticEnergy += combinedVelocity.normalized * speedContribution / Mathf.Max(0.01f, mass);
            }

            // Clamp to max
            if (_kineticEnergy.magnitude > maxKineticEnergy)
                _kineticEnergy = _kineticEnergy.normalized * maxKineticEnergy;

            // Decay (mass-influenced: heavier = slower decay)
            float decayFactor = 1f / (1f + (kineticEnergyDecayRate / Mathf.Max(0.01f, mass)) * dt);
            _kineticEnergy *= decayFactor;

            // Zero out tiny values
            if (_kineticEnergy.sqrMagnitude < 0.0001f)
                _kineticEnergy = Vector3.zero;
        }

        private void ApplyConstraintForces(float dt)
        {
            for (int i = _constraints.Count - 1; i >= 0; i--)
            {
                var c = _constraints[i];
                if (!c.isActive) continue;

                Vector3 toAnchor = c.anchorPoint - _rb.position;
                float currentDist = toAnchor.magnitude;
                if (currentDist < 0.0001f)
                    continue;

                Vector3 dirToAnchor = toAnchor / currentDist;

                switch (c.type)
                {
                    case ConstraintType.Metallic:
                        // Fixed distance - remove radial velocity to avoid oscillation
                        _internalVelocity = RemoveRadialVelocity(_internalVelocity, dirToAnchor, true, true);
                        _externalForce = RemoveRadialVelocity(_externalForce, dirToAnchor, true, true);
                        _kineticEnergy = RemoveRadialVelocity(_kineticEnergy, dirToAnchor, true, true);
                        break;

                    case ConstraintType.Rope:
                        // Only pull back when exceeding distance
                        if (currentDist > c.distance)
                        {
                            // Prevent further separation
                            _internalVelocity = RemoveRadialVelocity(_internalVelocity, dirToAnchor, false, true);
                            _externalForce = RemoveRadialVelocity(_externalForce, dirToAnchor, false, true);
                            _kineticEnergy = RemoveRadialVelocity(_kineticEnergy, dirToAnchor, false, true);

                            float overshoot = currentDist - c.distance;
                            float springStrength = Mathf.Max(0f, c.elasticity);
                            if (springStrength > 0f)
                            {
                                float springAccel = overshoot * springStrength;
                                _internalVelocity += dirToAnchor * (springAccel * dt);
                            }
                        }
                        break;

                    case ConstraintType.Sticky:
                        // Check if kinetic energy exceeds break threshold
                        if (_kineticEnergy.magnitude > c.breakForce)
                        {
                            // Break constraint, apply external force from built-up energy
                            Vector3 breakForce = _kineticEnergy.normalized * c.breakForce;
                            AddExternalForce(breakForce);

                            // Fire event
                            FireConstraintBrokenEvent(c);

                            _constraints.RemoveAt(i);
                        }
                        else if (currentDist > c.distance)
                        {
                            // Build up kinetic energy while pushing against constraint
                            Vector3 combined = _internalVelocity + _externalForce;
                            float outwardSpeed = Vector3.Dot(combined, -dirToAnchor);
                            if (outwardSpeed > 0f)
                            {
                                _kineticEnergy += (-dirToAnchor) * outwardSpeed * kineticEnergyBuildupRate * dt;
                            }

                            // Prevent further separation
                            _internalVelocity = RemoveRadialVelocity(_internalVelocity, dirToAnchor, false, true);
                            _externalForce = RemoveRadialVelocity(_externalForce, dirToAnchor, false, true);
                            _kineticEnergy = RemoveRadialVelocity(_kineticEnergy, dirToAnchor, false, true);
                        }
                        break;
                }
            }
        }

        private Vector3 ApplyConstraintCorrections(Vector3 pos)
        {
            for (int i = _constraints.Count - 1; i >= 0; i--)
            {
                var c = _constraints[i];
                if (!c.isActive) continue;

                Vector3 toAnchor = c.anchorPoint - pos;
                float currentDist = toAnchor.magnitude;
                if (currentDist < 0.0001f)
                    continue;

                Vector3 dirToAnchor = toAnchor / currentDist;

                switch (c.type)
                {
                    case ConstraintType.Metallic:
                        if (Mathf.Abs(currentDist - c.distance) > 0.001f)
                            pos = c.anchorPoint - dirToAnchor * c.distance;
                        break;

                    case ConstraintType.Rope:
                        // Rope is elastic; allow stretch without hard snapping
                        break;

                    case ConstraintType.Sticky:
                        if (currentDist > c.distance)
                            pos = c.anchorPoint - dirToAnchor * c.distance;
                        break;
                }
            }

            return pos;
        }

        private static Vector3 RemoveRadialVelocity(Vector3 velocity, Vector3 dirToAnchor, bool removeToward, bool removeAway)
        {
            float dot = Vector3.Dot(velocity, dirToAnchor);
            if ((dot > 0f && removeToward) || (dot < 0f && removeAway))
            {
                velocity -= dirToAnchor * dot;
            }

            return velocity;
        }

        private void RemoveIntoSurfaceVelocity(Vector3 normal)
        {
            float vInto = Vector3.Dot(_internalVelocity, normal);
            if (vInto < 0f) _internalVelocity -= normal * vInto;

            float eInto = Vector3.Dot(_externalForce, normal);
            if (eInto < 0f) _externalForce -= normal * eInto;

            float kInto = Vector3.Dot(_kineticEnergy, normal);
            if (kInto < 0f) _kineticEnergy -= normal * kInto;
        }

        private void ClampVelocities()
        {
            // Clamp internal velocity horizontal
            if (maxHorizontalVelocity > 0f)
            {
                Vector2 horizontal = new Vector2(_internalVelocity.x, _internalVelocity.z);
                if (horizontal.magnitude > maxHorizontalVelocity)
                {
                    horizontal = horizontal.normalized * maxHorizontalVelocity;
                    _internalVelocity.x = horizontal.x;
                    _internalVelocity.z = horizontal.y;
                }

                // Also clamp external force horizontal
                Vector2 extHorizontal = new Vector2(_externalForce.x, _externalForce.z);
                if (extHorizontal.magnitude > maxHorizontalVelocity)
                {
                    extHorizontal = extHorizontal.normalized * maxHorizontalVelocity;
                    _externalForce.x = extHorizontal.x;
                    _externalForce.z = extHorizontal.y;
                }
            }

            // Clamp vertical velocity (upward)
            if (maxVerticalVelocity > 0f && _internalVelocity.y > maxVerticalVelocity)
            {
                _internalVelocity.y = maxVerticalVelocity;
            }

            // Clamp total velocity
            if (maxTotalVelocity > 0f)
            {
                Vector3 combined = _internalVelocity + _externalForce + _kineticEnergy;
                if (combined.magnitude > maxTotalVelocity)
                {
                    float scale = maxTotalVelocity / combined.magnitude;
                    _internalVelocity *= scale;
                    _externalForce *= scale;
                    _kineticEnergy *= scale;
                }
            }
        }

        private void CheckGroundingStateChange()
        {
            if (_isGrounded && !_wasGrounded)
            {
                onGrounded?.Invoke();
                _onBecameGrounded?.Invoke();
            }
            else if (!_isGrounded && _wasGrounded)
            {
                onAirborne?.Invoke();
                _onBecameAirborne?.Invoke();
            }
        }

        private void FireCollisionEvent(RaycastHit hit, float impactVelocity)
        {
            var info = PhysicsCollisionInfo.FromRaycastHit(hit, impactVelocity);
            onCollision?.Invoke(info);
            _onPhysicsCollision?.Invoke(info);
        }

        private void FireConstraintBrokenEvent(PhysicsConstraint constraint)
        {
            onConstraintBroken?.Invoke(constraint);
            _onConstraintBroken?.Invoke(constraint);
        }

        #endregion

        #region Ground Snap

        private Vector3 GroundSnap(Vector3 pos, float dt)
        {
            // Don't snap if moving upward
            if (_internalVelocity.y > 0.01f)
                return pos;

            // Use skinWidth for minimal snap distance - just enough to settle on ground
            // without floating or sinking through
            float snapDist = skinWidth * 2f;

            if (CapsuleCast(pos, Vector3.down, snapDist, out RaycastHit hit))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle <= maxSlopeAngle)
                {
                    float moveDown = Mathf.Max(0f, hit.distance - skinWidth);
                    pos += Vector3.down * moveDown;

                    _isGrounded = true;
                    _groundNormal = hit.normal;
                    _groundHit = hit;
                    _lastGroundedTime = Time.time;

                    if (_internalVelocity.y < 0f)
                        _internalVelocity.y = 0f;
                }
            }

            return pos;
        }

        #endregion

        #region Capsule Geometry

        private void GetCapsulePoints(Vector3 position, out Vector3 p0, out Vector3 p1, out float radius)
        {
            radius = Mathf.Max(0.0001f, _capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z));
            float height = Mathf.Max(_capsule.height * transform.lossyScale.y, radius * 2f);
            float half = height * 0.5f;

            Vector3 center = position + transform.rotation * Vector3.Scale(_capsule.center, transform.lossyScale);
            float inner = Mathf.Max(0f, half - radius);

            Vector3 up = transform.up;
            p0 = center + up * inner;
            p1 = center - up * inner;
        }

        private bool CapsuleCast(Vector3 position, Vector3 direction, float distance, out RaycastHit hit)
        {
            GetCapsulePoints(position, out Vector3 p0, out Vector3 p1, out float r);
            float castRadius = Mathf.Max(0.0001f, r - skinWidth * 0.5f);

            return Physics.CapsuleCast(p0, p1, castRadius, direction, out hit, distance, collisionMask, triggerInteraction);
        }

        private Vector3 Depenetrate(Vector3 pos)
        {
            if (depenetrationIterations <= 0)
                return pos;

            for (int iter = 0; iter < depenetrationIterations; iter++)
            {
                GetCapsulePoints(pos, out Vector3 p0, out Vector3 p1, out float r);
                float overlapRadius = Mathf.Max(0.0001f, r - skinWidth * 0.25f);

                int count = Physics.OverlapCapsuleNonAlloc(p0, p1, overlapRadius, s_overlapCache, collisionMask, triggerInteraction);
                if (count <= 0) break;

                bool moved = false;

                for (int i = 0; i < count; i++)
                {
                    Collider other = s_overlapCache[i];
                    if (other == null || other == _capsule) continue;

                    if (Physics.ComputePenetration(
                        _capsule, pos, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float dist))
                    {
                        pos += dir * (dist + skinWidth);
                        moved = true;
                        RemoveIntoSurfaceVelocity(dir);
                    }

                    s_overlapCache[i] = null;
                }

                if (!moved) break;
            }

            return pos;
        }

        #endregion

        #region Public API - IMovementPhysicsAuthority Implementation

        // Grounding
        public bool IsGrounded => _isGrounded;
        public float LastGroundedTime => _lastGroundedTime;

        // Velocity vectors
        public Vector3 InternalVelocity => _internalVelocity;
        public Vector3 Velocity => _internalVelocity;
        public Vector3 ExternalVelocity => _externalForce;
        public Vector3 KineticEnergy => _kineticEnergy;

        // Late velocities (previous frame)
        public LateVelocities LateVelocities => _lateVelocities;

        // Mass & Dynamics
        public float Mass
        {
            get => mass;
            set => mass = Mathf.Max(0.01f, value);
        }

        public float GravityMultiplier => gravityMultiplier;
        public bool UseCustomGravity => useCustomGravity;

        // Internal velocity methods
        public void SetInternalVelocity(Vector3 velocity)
        {
            _internalVelocity = velocity;
            ClampVelocities();
        }

        public void SetVelocity(Vector3 newVelocity)
        {
            SetInternalVelocity(newVelocity);
        }

        public void SetInternalVerticalVelocity(float y)
        {
            _internalVelocity.y = y;
            ClampVelocities();
        }

        public void SetVerticalVelocity(float verticalVelocity)
        {
            SetInternalVerticalVelocity(verticalVelocity);
        }

        public void SetInternalHorizontalVelocity(float x, float z)
        {
            _internalVelocity.x = x;
            _internalVelocity.z = z;
            ClampVelocities();
        }

        public void SetHorizontalVelocity(Vector2 horizontalVelocity)
        {
            SetInternalHorizontalVelocity(horizontalVelocity.x, horizontalVelocity.y);
        }

        // External force methods
        public void SetExternalForce(Vector3 force)
        {
            _externalForce = force;
            ClampVelocities();
        }

        public void SetExternalHorizontalForce(float x, float z)
        {
            _externalForce.x = x;
            _externalForce.z = z;
            ClampVelocities();
        }

        public void SetExternalVerticalForce(float y)
        {
            _externalForce.y = y;
            ClampVelocities();
        }

        public void AddExternalForce(Vector3 force)
        {
            _externalForce += force;
            ClampVelocities();
        }

        public void AddExternalHorizontalForce(float x, float z)
        {
            _externalForce.x += x;
            _externalForce.z += z;
            ClampVelocities();
        }

        public void AddExternalVerticalForce(float y)
        {
            _externalForce.y += y;
            ClampVelocities();
        }

        public void ClearExternalVelocity()
        {
            _externalForce = Vector3.zero;
        }

        public float ExternalVelocityDecayRate
        {
            get => externalVelocityDecayRate;
            set => externalVelocityDecayRate = Mathf.Max(0f, value);
        }

        // Kinetic energy methods
        public void AddKineticEnergy(Vector3 energy)
        {
            _kineticEnergy += energy;
            if (_kineticEnergy.magnitude > maxKineticEnergy)
                _kineticEnergy = _kineticEnergy.normalized * maxKineticEnergy;
        }

        public void RemoveKineticEnergy(Vector3 energy)
        {
            _kineticEnergy -= energy;
            if (_kineticEnergy.sqrMagnitude < 0.0001f)
                _kineticEnergy = Vector3.zero;
        }

        public void ClearKineticEnergy()
        {
            _kineticEnergy = Vector3.zero;
        }

        // Gravity methods
        public void SetGravityMultiplier(float multiplier) => gravityMultiplier = multiplier;
        public void SetUseCustomGravity(bool use) => useCustomGravity = use;

        // Constraints
        public PhysicsConstraint[] CurrentConstraints => _constraints.ToArray();

        public void AddConstraint(PhysicsConstraint constraint)
        {
            // Remove existing constraint with same ID
            RemoveConstraint(constraint.id);
            _constraints.Add(constraint);
        }

        public void RemoveConstraint(string constraintId)
        {
            for (int i = _constraints.Count - 1; i >= 0; i--)
            {
                if (_constraints[i].id == constraintId)
                {
                    _constraints.RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveConstraint(PhysicsConstraint constraint)
        {
            RemoveConstraint(constraint.id);
        }

        public void ClearConstraints()
        {
            _constraints.Clear();
        }

        public bool HasConstraint(string constraintId)
        {
            for (int i = 0; i < _constraints.Count; i++)
            {
                if (_constraints[i].id == constraintId)
                    return true;
            }
            return false;
        }

        // State control
        public bool Frozen => frozen;

        public void SetFrozen(bool isFrozen)
        {
            frozen = isFrozen;
        }

        public bool IsEnabled => isEnabled;

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        // Collider settings
        public LayerMask CollisionMask
        {
            get => collisionMask;
            set => collisionMask = value;
        }

        public QueryTriggerInteraction TriggerInteraction
        {
            get => triggerInteraction;
            set => triggerInteraction = value;
        }

        // Collider access
        public CapsuleCollider Collider => _capsule;

        // Detection settings
        public float SkinWidth
        {
            get => skinWidth;
            set => skinWidth = Mathf.Max(0.0001f, value);
        }

        public float MaxSlopeAngle
        {
            get => maxSlopeAngle;
            set => maxSlopeAngle = Mathf.Clamp(value, 0f, 89f);
        }

        public void SetSlopeAngle(float angle)
        {
            MaxSlopeAngle = angle;
        }

        // Events
        public event Action<PhysicsCollisionInfo> OnPhysicsCollision
        {
            add => _onPhysicsCollision += value;
            remove => _onPhysicsCollision -= value;
        }

        public event Action OnBecameGrounded
        {
            add => _onBecameGrounded += value;
            remove => _onBecameGrounded -= value;
        }

        public event Action OnBecameAirborne
        {
            add => _onBecameAirborne += value;
            remove => _onBecameAirborne -= value;
        }

        public event Action<PhysicsConstraint> OnConstraintBroken
        {
            add => _onConstraintBroken += value;
            remove => _onConstraintBroken -= value;
        }

        // UnityEvent accessors for editor
        public UnityEvent<PhysicsCollisionInfo> OnCollisionEvent => onCollision;
        public UnityEvent OnGroundedEvent => onGrounded;
        public UnityEvent OnAirborneEvent => onAirborne;
        public UnityEvent<PhysicsConstraint> OnConstraintBrokenEvent => onConstraintBroken;

        #endregion

        #region Additional Public Properties

        public float MaxKineticEnergy
        {
            get => maxKineticEnergy;
            set => maxKineticEnergy = Mathf.Max(0f, value);
        }

        public float KineticEnergyDecayRate
        {
            get => kineticEnergyDecayRate;
            set => kineticEnergyDecayRate = Mathf.Max(0f, value);
        }

        public float KineticEnergyBuildupRate
        {
            get => kineticEnergyBuildupRate;
            set => kineticEnergyBuildupRate = Mathf.Max(0f, value);
        }

        public float MaxHorizontalVelocity
        {
            get => maxHorizontalVelocity;
            set => maxHorizontalVelocity = Mathf.Max(0f, value);
        }

        public float MaxVerticalVelocity
        {
            get => maxVerticalVelocity;
            set => maxVerticalVelocity = Mathf.Max(0f, value);
        }

        public float MaxTotalVelocity
        {
            get => maxTotalVelocity;
            set => maxTotalVelocity = Mathf.Max(0f, value);
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showDebug) return;
            if (!TryGetComponent(out CapsuleCollider cap)) return;

            Vector3 pos = Application.isPlaying && _rb != null ? _rb.position : transform.position;

            _capsule = cap;
            GetCapsulePoints(pos, out Vector3 p0, out Vector3 p1, out float r);

            // Draw capsule - color based on state
            if (frozen)
            {
                Gizmos.color = Color.gray;
            }
            else
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
            }

            Gizmos.DrawWireSphere(p0, r);
            Gizmos.DrawWireSphere(p1, r);
            Gizmos.DrawLine(p0 + Vector3.forward * r, p1 + Vector3.forward * r);
            Gizmos.DrawLine(p0 - Vector3.forward * r, p1 - Vector3.forward * r);
            Gizmos.DrawLine(p0 + Vector3.right * r, p1 + Vector3.right * r);
            Gizmos.DrawLine(p0 - Vector3.right * r, p1 - Vector3.right * r);

            // Ground probe ray (shows skinWidth-based detection range)
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, Vector3.down * (skinWidth * 2f));

            if (Application.isPlaying)
            {
                // Velocity vectors
                if (showVelocityVectors)
                {
                    // Internal velocity - green
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(pos, _internalVelocity * 0.5f);

                    // External force - blue
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(pos + Vector3.up * 0.1f, _externalForce * 0.5f);

                    // Kinetic energy - magenta
                    if (showKineticEnergy)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawRay(pos + Vector3.up * 0.2f, _kineticEnergy * 0.5f);
                    }
                }

                // Constraints
                if (showConstraints)
                {
                    foreach (var c in _constraints)
                    {
                        if (!c.isActive) continue;

                        switch (c.type)
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

                        Gizmos.DrawLine(pos, c.anchorPoint);
                        Gizmos.DrawWireSphere(c.anchorPoint, 0.1f);

                        // Draw distance sphere
                        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                        Gizmos.DrawWireSphere(c.anchorPoint, c.distance);
                    }
                }
            }
        }

        #endregion
    }
}