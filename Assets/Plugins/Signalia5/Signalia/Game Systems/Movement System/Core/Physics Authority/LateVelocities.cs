using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Velocity data from the previous physics frame.
    /// Use this when you need to know how fast an object was moving before a state change
    /// (e.g. OnBecameGrounded), since current velocity may be zeroed immediately on landing.
    /// </summary>
    [Serializable]
    public struct LateVelocities
    {
        /// <summary>
        /// Internal velocity from the previous frame (movement from within the object itself).
        /// </summary>
        public Vector3 InternalVelocity { get; internal set; }

        /// <summary>
        /// External velocity from the previous frame (decaying forces from external sources).
        /// </summary>
        public Vector3 ExternalVelocity { get; internal set; }

        /// <summary>
        /// Kinetic energy from the previous frame.
        /// </summary>
        public Vector3 KineticEnergy { get; internal set; }

        /// <summary>
        /// Combined total velocity from the previous frame (Internal + External + Kinetic).
        /// </summary>
        public Vector3 TotalVelocity => InternalVelocity + ExternalVelocity + KineticEnergy;

        /// <summary>
        /// Magnitude of the total velocity from the previous frame.
        /// </summary>
        public float TotalSpeed => TotalVelocity.magnitude;

        /// <summary>
        /// Horizontal velocity (X and Z) from the previous frame.
        /// </summary>
        public Vector2 HorizontalVelocity => new Vector2(InternalVelocity.x + ExternalVelocity.x + KineticEnergy.x,
            InternalVelocity.z + ExternalVelocity.z + KineticEnergy.z);

        /// <summary>
        /// Vertical velocity (Y) from the previous frame.
        /// </summary>
        public float VerticalVelocity => InternalVelocity.y + ExternalVelocity.y + KineticEnergy.y;

        internal static LateVelocities Create(Vector3 internalVel, Vector3 externalVel, Vector3 kineticEnergy)
        {
            return new LateVelocities
            {
                InternalVelocity = internalVel,
                ExternalVelocity = externalVel,
                KineticEnergy = kineticEnergy
            };
        }
    }
}
