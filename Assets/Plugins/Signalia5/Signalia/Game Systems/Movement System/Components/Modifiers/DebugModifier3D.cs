using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Debug modifier for testing and debugging MovementPhysics3D behavior.
    /// Provides runtime testing utilities and diagnostics for all physics features
    /// including velocity, kinetic energy, constraints, frozen state, and mass.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Movement/Signalia | Debug Modifier 3D")]
    public class DebugModifier3D : MovementPhysicsModifier
    {
        #region Serialized Fields

        [SerializeField] private float testVelocity = 10f;
        [SerializeField] private float testJumpForce = 10f;
        [SerializeField] private float testDashForce = 15f;
        [SerializeField] private Vector3 testPosition = Vector3.zero;

        [SerializeField] private Vector3 testKineticEnergy = new Vector3(5f, 0f, 0f);

        [SerializeField] private ConstraintType testConstraintType = ConstraintType.Rope;
        [SerializeField] private float testConstraintDistance = 5f;
        [SerializeField] private float testConstraintElasticity = 0.5f;
        [SerializeField] private float testConstraintBreakForce = 15f;
        [SerializeField] private Vector3 testConstraintAnchor = Vector3.zero;

        [SerializeField] private float testMass = 1f;

        #endregion

        #region Public Debug Methods - Velocity

        /// <summary>
        /// Sets the full internal velocity vector.
        /// </summary>
        public void DebugSetVelocity(Vector3 velocity)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetInternalVelocity(velocity);
        }

        /// <summary>
        /// Sets horizontal velocity (X and Z).
        /// </summary>
        public void DebugSetHorizontalVelocity(Vector2 velocity)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetInternalHorizontalVelocity(velocity.x, velocity.y);
        }

        /// <summary>
        /// Sets horizontal velocity (X and Z) using floats.
        /// </summary>
        public void DebugSetHorizontalVelocity(float x, float z)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetInternalHorizontalVelocity(x, z);
        }

        /// <summary>
        /// Sets vertical velocity (Y).
        /// </summary>
        public void DebugSetVerticalVelocity(float velocity)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetInternalVerticalVelocity(velocity);
        }

        #endregion

        #region Public Debug Methods - External Force

        /// <summary>
        /// Adds an external force (decays over time).
        /// </summary>
        public void DebugAddExternalForce(Vector3 force)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.AddExternalForce(force);
        }

        /// <summary>
        /// Sets the external force directly.
        /// </summary>
        public void DebugSetExternalForce(Vector3 force)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetExternalForce(force);
        }

        /// <summary>
        /// Clears all external velocity.
        /// </summary>
        public void DebugClearExternalVelocity()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.ClearExternalVelocity();
        }

        #endregion

        #region Public Debug Methods - Kinetic Energy

        /// <summary>
        /// Adds kinetic energy to the physics object.
        /// </summary>
        public void DebugAddKineticEnergy(Vector3 energy)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.AddKineticEnergy(energy);
        }

        /// <summary>
        /// Removes kinetic energy from the physics object.
        /// </summary>
        public void DebugRemoveKineticEnergy(Vector3 energy)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.RemoveKineticEnergy(energy);
        }

        /// <summary>
        /// Clears all kinetic energy.
        /// </summary>
        public void DebugClearKineticEnergy()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.ClearKineticEnergy();
        }

        /// <summary>
        /// Adds the test kinetic energy value.
        /// </summary>
        public void DebugAddTestKineticEnergy()
        {
            DebugAddKineticEnergy(testKineticEnergy);
        }

        #endregion

        #region Public Debug Methods - Constraints

        /// <summary>
        /// Adds a test constraint at the specified anchor position.
        /// </summary>
        public void DebugAddConstraint(Vector3 anchorPoint, ConstraintType type, float distance, float elasticity = 0.5f, float breakForce = 15f)
        {
            if (PhysicsAuthority == null) return;

            string id = $"debug_constraint_{Time.time}";
            PhysicsConstraint constraint;

            switch (type)
            {
                case ConstraintType.Metallic:
                    constraint = PhysicsConstraint.CreateMetallic(id, anchorPoint, distance);
                    break;
                case ConstraintType.Rope:
                    constraint = PhysicsConstraint.CreateRope(id, anchorPoint, distance, elasticity);
                    break;
                case ConstraintType.Sticky:
                    constraint = PhysicsConstraint.CreateSticky(id, anchorPoint, distance, breakForce);
                    break;
                default:
                    constraint = PhysicsConstraint.CreateRope(id, anchorPoint, distance, elasticity);
                    break;
            }

            PhysicsAuthority.AddConstraint(constraint);
        }

        /// <summary>
        /// Adds the test constraint with serialized settings.
        /// </summary>
        public void DebugAddTestConstraint()
        {
            Vector3 anchor = testConstraintAnchor == Vector3.zero ? transform.position + Vector3.up * testConstraintDistance : testConstraintAnchor;
            DebugAddConstraint(anchor, testConstraintType, testConstraintDistance, testConstraintElasticity, testConstraintBreakForce);
        }

        /// <summary>
        /// Adds a constraint anchored above the current position.
        /// </summary>
        public void DebugAddConstraintAbove(float distance)
        {
            Vector3 anchor = transform.position + Vector3.up * distance;
            DebugAddConstraint(anchor, testConstraintType, distance, testConstraintElasticity, testConstraintBreakForce);
        }

        /// <summary>
        /// Removes a constraint by ID.
        /// </summary>
        public void DebugRemoveConstraint(string constraintId)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.RemoveConstraint(constraintId);
        }

        /// <summary>
        /// Clears all constraints.
        /// </summary>
        public void DebugClearAllConstraints()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.ClearConstraints();
        }

        #endregion

        #region Public Debug Methods - Frozen State

        /// <summary>
        /// Toggles the frozen state of the physics object.
        /// </summary>
        public void DebugToggleFrozen()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetFrozen(!PhysicsAuthority.Frozen);
        }

        /// <summary>
        /// Sets the frozen state.
        /// </summary>
        public void DebugSetFrozen(bool frozen)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetFrozen(frozen);
        }

        /// <summary>
        /// Freezes the physics object.
        /// </summary>
        public void DebugFreeze()
        {
            DebugSetFrozen(true);
        }

        /// <summary>
        /// Unfreezes the physics object.
        /// </summary>
        public void DebugUnfreeze()
        {
            DebugSetFrozen(false);
        }

        #endregion

        #region Public Debug Methods - Mass

        /// <summary>
        /// Sets the mass of the physics object.
        /// </summary>
        public void DebugSetMass(float mass)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.Mass = mass;
        }

        /// <summary>
        /// Sets the mass to the test value.
        /// </summary>
        public void DebugSetTestMass()
        {
            DebugSetMass(testMass);
        }

        /// <summary>
        /// Doubles the current mass.
        /// </summary>
        public void DebugDoubleMass()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.Mass *= 2f;
        }

        /// <summary>
        /// Halves the current mass.
        /// </summary>
        public void DebugHalveMass()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.Mass *= 0.5f;
        }

        #endregion

        #region Public Debug Methods - Gravity

        /// <summary>
        /// Sets the gravity multiplier.
        /// </summary>
        public void DebugSetGravityMultiplier(float multiplier)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetGravityMultiplier(multiplier);
        }

        /// <summary>
        /// Toggles custom gravity on/off.
        /// </summary>
        public void DebugToggleCustomGravity()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetUseCustomGravity(!PhysicsAuthority.UseCustomGravity);
        }

        #endregion

        #region Public Debug Methods - Teleport & Reset

        /// <summary>
        /// Teleports the character to a specific position.
        /// </summary>
        public void DebugTeleport(Vector3 position)
        {
            if (PhysicsAuthority == null) return;
            transform.position = position;
            DebugResetVelocities();
        }

        /// <summary>
        /// Teleports to the test position.
        /// </summary>
        public void DebugTeleportToTestPosition()
        {
            DebugTeleport(testPosition);
        }

        /// <summary>
        /// Resets all velocities to zero.
        /// </summary>
        public void DebugResetVelocities()
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetInternalVelocity(Vector3.zero);
            PhysicsAuthority.ClearExternalVelocity();
            PhysicsAuthority.ClearKineticEnergy();
        }

        /// <summary>
        /// Resets everything (velocities, constraints, frozen state).
        /// </summary>
        public void DebugResetAll()
        {
            DebugResetVelocities();
            DebugClearAllConstraints();
            DebugSetFrozen(false);
        }

        #endregion

        #region Public Debug Methods - Common Actions

        /// <summary>
        /// Applies a jump force upward.
        /// </summary>
        public void DebugJump(float force)
        {
            if (PhysicsAuthority == null) return;
            PhysicsAuthority.SetInternalVerticalVelocity(force);
        }

        /// <summary>
        /// Applies a jump with test force.
        /// </summary>
        public void DebugTestJump()
        {
            DebugJump(testJumpForce);
        }

        /// <summary>
        /// Applies a dash force in the forward direction.
        /// </summary>
        public void DebugDash(float force)
        {
            if (PhysicsAuthority == null) return;
            Vector3 dashDir = transform.forward;
            dashDir.y = 0f;
            dashDir.Normalize();
            PhysicsAuthority.AddExternalForce(dashDir * force);
        }

        /// <summary>
        /// Applies a dash with test force.
        /// </summary>
        public void DebugTestDash()
        {
            DebugDash(testDashForce);
        }

        /// <summary>
        /// Applies a dash force in a specific direction.
        /// </summary>
        public void DebugDashDirection(Vector3 direction, float force)
        {
            if (PhysicsAuthority == null) return;
            direction.y = 0f;
            direction.Normalize();
            PhysicsAuthority.AddExternalForce(direction * force);
        }

        /// <summary>
        /// Applies a knockback force.
        /// </summary>
        public void DebugKnockback(Vector3 direction, float force)
        {
            if (PhysicsAuthority == null) return;
            direction.y = 0f;
            direction.Normalize();
            PhysicsAuthority.AddExternalForce(direction * force);
        }

        #endregion

        #region Public Properties - State Information

        /// <summary>
        /// Gets current internal velocity.
        /// </summary>
        public Vector3 CurrentInternalVelocity => PhysicsAuthority?.InternalVelocity ?? Vector3.zero;

        /// <summary>
        /// Gets current external velocity.
        /// </summary>
        public Vector3 CurrentExternalVelocity => PhysicsAuthority?.ExternalVelocity ?? Vector3.zero;

        /// <summary>
        /// Gets current kinetic energy.
        /// </summary>
        public Vector3 CurrentKineticEnergy => PhysicsAuthority?.KineticEnergy ?? Vector3.zero;

        /// <summary>
        /// Gets velocity data from the previous physics frame.
        /// Useful when handling OnBecameGrounded to know impact speed before velocity is zeroed.
        /// </summary>
        public LateVelocities CurrentLateVelocities => PhysicsAuthority?.LateVelocities ?? default;

        /// <summary>
        /// Gets whether the character is grounded.
        /// </summary>
        public bool IsGrounded => PhysicsAuthority?.IsGrounded ?? false;

        /// <summary>
        /// Gets whether the physics is frozen.
        /// </summary>
        public bool IsFrozen => PhysicsAuthority?.Frozen ?? false;

        /// <summary>
        /// Gets the current mass.
        /// </summary>
        public float CurrentMass => PhysicsAuthority?.Mass ?? 1f;

        /// <summary>
        /// Gets the current constraints.
        /// </summary>
        public PhysicsConstraint[] CurrentConstraints => PhysicsAuthority?.CurrentConstraints ?? new PhysicsConstraint[0];

        /// <summary>
        /// Gets the constraint count.
        /// </summary>
        public int ConstraintCount => CurrentConstraints.Length;

        /// <summary>
        /// Gets the time since last grounded.
        /// </summary>
        public float TimeSinceLastGrounded => PhysicsAuthority != null ? Time.time - PhysicsAuthority.LastGroundedTime : 0f;

        /// <summary>
        /// Gets the combined velocity magnitude (internal + external + kinetic).
        /// </summary>
        public float CombinedVelocityMagnitude
        {
            get
            {
                if (PhysicsAuthority == null) return 0f;
                return (PhysicsAuthority.InternalVelocity + PhysicsAuthority.ExternalVelocity + PhysicsAuthority.KineticEnergy).magnitude;
            }
        }

        #endregion

        #region Legacy Properties (for backward compatibility)

        /// <summary>
        /// Gets current velocity (legacy, same as CurrentInternalVelocity).
        /// </summary>
        public Vector3 CurrentVelocity => CurrentInternalVelocity;

        #endregion
    }
}
