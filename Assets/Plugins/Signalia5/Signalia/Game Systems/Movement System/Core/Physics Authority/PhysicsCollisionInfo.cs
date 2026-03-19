using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Contains information about a physics collision detected by MovementPhysics3D.
    /// Passed to collision event handlers for processing.
    /// </summary>
    [Serializable]
    public struct PhysicsCollisionInfo
    {
        /// <summary>
        /// The world-space point where the collision occurred.
        /// </summary>
        public Vector3 point;

        /// <summary>
        /// The surface normal at the collision point.
        /// Points away from the surface that was hit.
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// The collider that was hit during the collision.
        /// May be null in some edge cases.
        /// </summary>
        public Collider collider;

        /// <summary>
        /// The magnitude of the velocity at impact.
        /// Useful for determining collision intensity for effects/damage.
        /// </summary>
        public float impactVelocity;

        /// <summary>
        /// The GameObject that was hit.
        /// Convenience property that returns null if collider is null.
        /// </summary>
        public GameObject GameObject => collider != null ? collider.gameObject : null;

        /// <summary>
        /// The Transform of the hit object.
        /// Convenience property that returns null if collider is null.
        /// </summary>
        public Transform Transform => collider != null ? collider.transform : null;

        /// <summary>
        /// Creates a new PhysicsCollisionInfo from a RaycastHit and impact velocity.
        /// </summary>
        public static PhysicsCollisionInfo FromRaycastHit(RaycastHit hit, float impactVelocity)
        {
            return new PhysicsCollisionInfo
            {
                point = hit.point,
                normal = hit.normal,
                collider = hit.collider,
                impactVelocity = impactVelocity
            };
        }

        /// <summary>
        /// Creates a new PhysicsCollisionInfo with manual values.
        /// </summary>
        public static PhysicsCollisionInfo Create(Vector3 point, Vector3 normal, Collider collider, float impactVelocity)
        {
            return new PhysicsCollisionInfo
            {
                point = point,
                normal = normal,
                collider = collider,
                impactVelocity = impactVelocity
            };
        }

        public override string ToString()
        {
            string objName = collider != null ? collider.gameObject.name : "null";
            return $"Collision at {point} (normal: {normal}, impact: {impactVelocity:F2}, object: {objName})";
        }
    }
}
