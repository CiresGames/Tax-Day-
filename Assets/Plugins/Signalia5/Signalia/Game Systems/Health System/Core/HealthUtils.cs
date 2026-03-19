using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Health
{
    /// <summary>
    /// Utility class for Health System operations, including collider overlap detection.
    /// </summary>
    public static class HealthUtils
    {
        /// <summary>
        /// Contact filter for collider overlap queries.
        /// Filters colliders by layer mask and trigger settings.
        /// </summary>
        public struct ContactFilter
        {
            private LayerMask layerMask;
            private bool useTriggersValue;

            /// <summary>
            /// Sets the layer mask for filtering colliders.
            /// </summary>
            public void SetLayerMask(LayerMask mask)
            {
                layerMask = mask;
            }

            /// <summary>
            /// Whether to include trigger colliders in the query.
            /// </summary>
            public bool useTriggers
            {
                get => useTriggersValue;
                set => useTriggersValue = value;
            }

            /// <summary>
            /// Gets the layer mask value.
            /// </summary>
            public LayerMask GetLayerMask()
            {
                return layerMask;
            }
        }

        /// <summary>
        /// Extension method for Collider to find overlapping colliders based on the collider's shape.
        /// </summary>
        /// <param name="collider">The collider to check overlaps for.</param>
        /// <param name="filter">The contact filter to apply.</param>
        /// <param name="results">Array to store the overlapping colliders.</param>
        /// <returns>The number of overlapping colliders found.</returns>
        public static int OverlapCollider(this Collider collider, ContactFilter filter, Collider[] results)
        {
            if (collider == null || results == null || results.Length == 0)
            {
                return 0;
            }

            QueryTriggerInteraction triggerInteraction = filter.useTriggers 
                ? QueryTriggerInteraction.Collide 
                : QueryTriggerInteraction.Ignore;

            LayerMask layerMask = filter.GetLayerMask();
            Collider[] allOverlaps;

            // Determine overlap based on collider type
            switch (collider)
            {
                case BoxCollider boxCollider:
                    allOverlaps = Physics.OverlapBox(
                        boxCollider.bounds.center,
                        boxCollider.bounds.extents,
                        boxCollider.transform.rotation,
                        layerMask,
                        triggerInteraction
                    );
                    break;

                case SphereCollider sphereCollider:
                    allOverlaps = Physics.OverlapSphere(
                        sphereCollider.bounds.center,
                        sphereCollider.radius * Mathf.Max(
                            sphereCollider.transform.lossyScale.x,
                            sphereCollider.transform.lossyScale.y,
                            sphereCollider.transform.lossyScale.z
                        ),
                        layerMask,
                        triggerInteraction
                    );
                    break;

                case CapsuleCollider capsuleCollider:
                    // Calculate capsule points
                    Vector3 center = capsuleCollider.bounds.center;
                    Vector3 direction;
                    float directionScale;
                    
                    // CapsuleCollider.direction: 0 = X-axis, 1 = Y-axis, 2 = Z-axis
                    switch (capsuleCollider.direction)
                    {
                        case 0: // X-axis
                            direction = capsuleCollider.transform.right;
                            directionScale = capsuleCollider.transform.lossyScale.x;
                            break;
                        case 1: // Y-axis
                            direction = capsuleCollider.transform.up;
                            directionScale = capsuleCollider.transform.lossyScale.y;
                            break;
                        default: // Z-axis (2)
                            direction = capsuleCollider.transform.forward;
                            directionScale = capsuleCollider.transform.lossyScale.z;
                            break;
                    }
                    
                    float height = capsuleCollider.height;
                    float radius = capsuleCollider.radius;
                    
                    // Calculate average scale for radius (use max of non-direction axes)
                    float radiusScale = capsuleCollider.direction switch
                    {
                        0 => Mathf.Max(capsuleCollider.transform.lossyScale.y, capsuleCollider.transform.lossyScale.z),
                        1 => Mathf.Max(capsuleCollider.transform.lossyScale.x, capsuleCollider.transform.lossyScale.z),
                        _ => Mathf.Max(capsuleCollider.transform.lossyScale.x, capsuleCollider.transform.lossyScale.y)
                    };
                    
                    float scaledRadius = radius * radiusScale;
                    float scaledHeight = height * directionScale;
                    float halfHeight = (scaledHeight * 0.5f) - scaledRadius;
                    halfHeight = Mathf.Max(0f, halfHeight);
                    
                    Vector3 point1 = center + direction * halfHeight;
                    Vector3 point2 = center - direction * halfHeight;

                    allOverlaps = Physics.OverlapCapsule(
                        point1,
                        point2,
                        scaledRadius,
                        layerMask,
                        triggerInteraction
                    );
                    break;

                case MeshCollider meshCollider:
                    // For MeshCollider, use bounds-based box overlap as fallback
                    allOverlaps = Physics.OverlapBox(
                        meshCollider.bounds.center,
                        meshCollider.bounds.extents,
                        meshCollider.transform.rotation,
                        layerMask,
                        triggerInteraction
                    );
                    break;

                default:
                    // For unknown collider types, use bounds-based box overlap
                    Bounds bounds = collider.bounds;
                    allOverlaps = Physics.OverlapBox(
                        bounds.center,
                        bounds.extents,
                        collider.transform.rotation,
                        layerMask,
                        triggerInteraction
                    );
                    break;
            }

            // Filter out the source collider itself
            int resultCount = 0;
            for (int i = 0; i < allOverlaps.Length && resultCount < results.Length; i++)
            {
                if (allOverlaps[i] != collider)
                {
                    results[resultCount] = allOverlaps[i];
                    resultCount++;
                }
            }

            return resultCount;
        }
    }
}

