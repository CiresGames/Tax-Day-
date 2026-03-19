using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Interface for movement physics authority components.
    /// Provides a common API for both 2D and 3D physics implementations.
    /// Supports deterministic kinematic physics with internal velocity, external forces,
    /// kinetic energy buildup, and constraint-based motion.
    /// </summary>
    public interface IMovementPhysicsAuthority
    {
        #region Grounding State

        /// <summary>
        /// Gets whether the character is currently grounded.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// Gets the time when the character was last grounded.
        /// </summary>
        float LastGroundedTime { get; }

        #endregion

        #region Core Velocity Vectors

        /// <summary>
        /// Gets the internal velocity vector (movement from within the object itself).
        /// This is the primary controlled velocity set by movement modifiers.
        /// </summary>
        Vector3 InternalVelocity { get; }

        /// <summary>
        /// Gets the current velocity vector (legacy property, same as InternalVelocity).
        /// </summary>
        [Obsolete("Use InternalVelocity instead for clarity")]
        Vector3 Velocity { get; }

        /// <summary>
        /// Gets the external velocity vector (decaying forces from external sources).
        /// These forces decay over time based on decay rate and mass.
        /// </summary>
        Vector3 ExternalVelocity { get; }

        /// <summary>
        /// Gets the current kinetic energy vector.
        /// Built up from combined motion, influenced by mass and drag.
        /// Provides momentum-like behavior while maintaining deterministic control.
        /// </summary>
        Vector3 KineticEnergy { get; }

        /// <summary>
        /// Gets velocity data from the previous physics frame.
        /// Use this when you need to know how fast an object was moving before a state change
        /// (e.g. OnBecameGrounded), since current velocity may be zeroed immediately on landing.
        /// </summary>
        LateVelocities LateVelocities { get; }

        #endregion

        #region Mass & Dynamics

        /// <summary>
        /// Gets or sets the mass of the physics object.
        /// Influences external force decay rate and kinetic energy buildup/decay.
        /// Heavier objects have slower decay and more stable kinetic energy.
        /// </summary>
        float Mass { get; set; }

        /// <summary>
        /// Gets the gravity multiplier.
        /// </summary>
        float GravityMultiplier { get; }

        /// <summary>
        /// Gets whether custom gravity is being used.
        /// </summary>
        bool UseCustomGravity { get; }

        #endregion

        #region Internal Velocity Methods

        /// <summary>
        /// Sets the internal velocity vector directly.
        /// </summary>
        void SetInternalVelocity(Vector3 velocity);

        /// <summary>
        /// Sets the full velocity vector (legacy method).
        /// </summary>
        [Obsolete("Use SetInternalVelocity instead for clarity")]
        void SetVelocity(Vector3 newVelocity);

        /// <summary>
        /// Sets only the vertical component (Y) of internal velocity.
        /// </summary>
        void SetInternalVerticalVelocity(float y);

        /// <summary>
        /// Sets only the vertical component of velocity (legacy method).
        /// </summary>
        [Obsolete("Use SetInternalVerticalVelocity instead for clarity")]
        void SetVerticalVelocity(float verticalVelocity);

        /// <summary>
        /// Sets the horizontal components (X and Z) of internal velocity.
        /// </summary>
        void SetInternalHorizontalVelocity(float x, float z);

        /// <summary>
        /// Sets the horizontal velocity using a Vector2 (X and Z components).
        /// </summary>
        [Obsolete("Use SetInternalHorizontalVelocity(float x, float z) instead for clarity")]
        void SetHorizontalVelocity(Vector2 horizontalVelocity);

        #endregion

        #region External Force Methods

        /// <summary>
        /// Sets the external force vector directly, replacing any existing external force.
        /// </summary>
        void SetExternalForce(Vector3 force);

        /// <summary>
        /// Sets only the horizontal components (X and Z) of external force.
        /// </summary>
        void SetExternalHorizontalForce(float x, float z);

        /// <summary>
        /// Sets only the vertical component (Y) of external force.
        /// </summary>
        void SetExternalVerticalForce(float y);

        /// <summary>
        /// Adds to the external force vector (accumulative).
        /// External forces decay over time based on decay rate and mass.
        /// </summary>
        void AddExternalForce(Vector3 force);

        /// <summary>
        /// Adds to the horizontal components (X and Z) of external force.
        /// </summary>
        void AddExternalHorizontalForce(float x, float z);

        /// <summary>
        /// Adds to the vertical component (Y) of external force.
        /// </summary>
        void AddExternalVerticalForce(float y);

        /// <summary>
        /// Clears all external velocity/force.
        /// </summary>
        void ClearExternalVelocity();

        /// <summary>
        /// Gets or sets the decay rate for external velocity (higher values decay faster).
        /// </summary>
        float ExternalVelocityDecayRate { get; set; }

        #endregion

        #region Kinetic Energy Methods

        /// <summary>
        /// Manually adds kinetic energy to the object.
        /// Note: Kinetic energy is typically built up automatically from motion.
        /// Use sparingly for special effects.
        /// </summary>
        void AddKineticEnergy(Vector3 energy);

        /// <summary>
        /// Removes kinetic energy from the object.
        /// Useful for consuming kinetic energy on impact or special moves.
        /// </summary>
        void RemoveKineticEnergy(Vector3 energy);

        /// <summary>
        /// Clears all kinetic energy immediately.
        /// </summary>
        void ClearKineticEnergy();

        #endregion

        #region Gravity Methods

        /// <summary>
        /// Sets the gravity multiplier.
        /// </summary>
        void SetGravityMultiplier(float multiplier);

        /// <summary>
        /// Sets whether to use custom gravity.
        /// </summary>
        void SetUseCustomGravity(bool use);

        #endregion

        #region Constraints

        /// <summary>
        /// Gets the current array of active constraints.
        /// </summary>
        PhysicsConstraint[] CurrentConstraints { get; }

        /// <summary>
        /// Adds a constraint to the physics object.
        /// </summary>
        void AddConstraint(PhysicsConstraint constraint);

        /// <summary>
        /// Removes a constraint by its unique ID.
        /// </summary>
        void RemoveConstraint(string constraintId);

        /// <summary>
        /// Removes a constraint using the constraint struct's ID.
        /// </summary>
        void RemoveConstraint(PhysicsConstraint constraint);

        /// <summary>
        /// Removes all constraints from the physics object.
        /// </summary>
        void ClearConstraints();

        /// <summary>
        /// Checks if a constraint with the given ID exists.
        /// </summary>
        bool HasConstraint(string constraintId);

        #endregion

        #region State Control

        /// <summary>
        /// Gets whether the physics object is frozen (no velocity updates applied).
        /// </summary>
        bool Frozen { get; }

        /// <summary>
        /// Sets the frozen state of the physics object.
        /// When frozen, no velocities are applied and the object remains stationary.
        /// </summary>
        void SetFrozen(bool frozen);

        /// <summary>
        /// Gets whether the physics authority is currently enabled and processing movement.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Enables or disables the physics authority. When disabled, all physics updates are paused.
        /// </summary>
        void SetEnabled(bool enabled);

        #endregion

        #region Collider Settings

        /// <summary>
        /// Gets or sets the collision mask (layers that the character can collide with).
        /// </summary>
        LayerMask CollisionMask { get; set; }

        /// <summary>
        /// Gets or sets how trigger colliders are handled during physics checks.
        /// </summary>
        QueryTriggerInteraction TriggerInteraction { get; set; }

        #endregion

        #region Collider Access

        /// <summary>
        /// Gets the capsule collider used for physics calculations.
        /// Ground detection and collision resolution are based entirely on this collider's geometry.
        /// </summary>
        CapsuleCollider Collider { get; }

        #endregion

        #region Detection Settings

        /// <summary>
        /// Gets or sets the small offset to keep capsule slightly above ground (prevents jitter).
        /// This is the only distance parameter - ground detection uses the collider geometry plus this offset.
        /// </summary>
        float SkinWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum slope angle (in degrees) considered as 'ground'.
        /// </summary>
        float MaxSlopeAngle { get; set; }

        /// <summary>
        /// Sets the maximum slope angle considered as ground.
        /// </summary>
        void SetSlopeAngle(float angle);

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the physics object collides with something.
        /// Provides collision information including point, normal, and impact velocity.
        /// </summary>
        event Action<PhysicsCollisionInfo> OnPhysicsCollision;

        /// <summary>
        /// Event fired when the object becomes grounded (transition from airborne to grounded).
        /// </summary>
        event Action OnBecameGrounded;

        /// <summary>
        /// Event fired when the object becomes airborne (transition from grounded to airborne).
        /// </summary>
        event Action OnBecameAirborne;

        /// <summary>
        /// Event fired when a Sticky constraint breaks due to kinetic energy exceeding threshold.
        /// Provides the constraint that was broken.
        /// </summary>
        event Action<PhysicsConstraint> OnConstraintBroken;

        #endregion
    }
}