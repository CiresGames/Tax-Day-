using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Defines the type of physics constraint applied to an object.
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// Keeps the object at a fixed distance from the anchor point.
        /// Only allows orbital motion around the anchor, like a metal pipe connecting them.
        /// </summary>
        Metallic,

        /// <summary>
        /// Prevents the object from exceeding a maximum distance from the anchor.
        /// Allows movement within the distance and applies elastic pull when exceeded.
        /// Perfect for grappling mechanics.
        /// </summary>
        Rope,

        /// <summary>
        /// Keeps the object from going beyond a certain point but allows breaking
        /// when kinetic energy exceeds the break threshold.
        /// Builds up kinetic energy while constrained for a "rubbery detachment" effect.
        /// </summary>
        Sticky
    }

    /// <summary>
    /// Represents a physics constraint that restricts object movement relative to an anchor point.
    /// Used by MovementPhysics3D to apply constraint-based motion limitations.
    /// </summary>
    [Serializable]
    public struct PhysicsConstraint : IEquatable<PhysicsConstraint>
    {
        /// <summary>
        /// Unique identifier for this constraint. Used for removal and lookup.
        /// </summary>
        public string id;

        /// <summary>
        /// The type of constraint behavior to apply.
        /// </summary>
        public ConstraintType type;

        /// <summary>
        /// The world-space anchor point for this constraint.
        /// </summary>
        public Vector3 anchorPoint;

        /// <summary>
        /// The distance parameter for the constraint.
        /// For Metallic: exact fixed distance.
        /// For Rope: maximum allowed distance.
        /// For Sticky: distance threshold before resistance begins.
        /// </summary>
        public float distance;

        /// <summary>
        /// Elasticity factor for Rope constraints.
        /// Higher values create stronger pull-back when distance is exceeded.
        /// Range: 0.0 to 1.0 typically, but can be higher for snappy ropes.
        /// </summary>
        public float elasticity;

        /// <summary>
        /// The kinetic energy threshold required to break a Sticky constraint.
        /// When the object's kinetic energy exceeds this value, the constraint breaks
        /// and applies an external force in the direction of motion.
        /// </summary>
        public float breakForce;

        /// <summary>
        /// Whether this constraint is currently active and should be processed.
        /// </summary>
        public bool isActive;

        /// <summary>
        /// Creates a new Metallic constraint that maintains fixed distance from anchor.
        /// </summary>
        public static PhysicsConstraint CreateMetallic(string id, Vector3 anchor, float distance)
        {
            return new PhysicsConstraint
            {
                id = id,
                type = ConstraintType.Metallic,
                anchorPoint = anchor,
                distance = distance,
                elasticity = 0f,
                breakForce = 0f,
                isActive = true
            };
        }

        /// <summary>
        /// Creates a new Rope constraint with elastic pull-back.
        /// </summary>
        public static PhysicsConstraint CreateRope(string id, Vector3 anchor, float maxDistance, float elasticity = 0.5f)
        {
            return new PhysicsConstraint
            {
                id = id,
                type = ConstraintType.Rope,
                anchorPoint = anchor,
                distance = maxDistance,
                elasticity = elasticity,
                breakForce = 0f,
                isActive = true
            };
        }

        /// <summary>
        /// Creates a new Sticky constraint that can break when kinetic energy exceeds threshold.
        /// </summary>
        public static PhysicsConstraint CreateSticky(string id, Vector3 anchor, float distance, float breakForce)
        {
            return new PhysicsConstraint
            {
                id = id,
                type = ConstraintType.Sticky,
                anchorPoint = anchor,
                distance = distance,
                elasticity = 0f,
                breakForce = breakForce,
                isActive = true
            };
        }

        public bool Equals(PhysicsConstraint other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is PhysicsConstraint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return id != null ? id.GetHashCode() : 0;
        }

        public static bool operator ==(PhysicsConstraint left, PhysicsConstraint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PhysicsConstraint left, PhysicsConstraint right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Constraint[{id}] Type={type}, Distance={distance:F2}, Active={isActive}";
        }
    }
}
