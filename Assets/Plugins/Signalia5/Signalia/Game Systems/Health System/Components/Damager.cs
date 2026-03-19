using AHAKuo.Signalia.GameSystems.Health;
using System.Collections;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Health
{
    /// <summary>
    /// Types of damagers: Here (AOE at position) or There (directional raycast).
    /// </summary>
    public enum DamagerType
    {
        Here,   // SphereCast at damager position, triggers on enable
        There   // Directional SphereCast, hits first valid target
    }

    /// <summary>
    /// Shapes for Here-type damager queries.
    /// </summary>
    public enum DamagerShape
    {
        Sphere,
        Box,
        Capsule,
        Collider
    }

    /// <summary>
    /// Component that broadcasts damage events through Health Radio.
    /// Supports two types: Here (AOE/melee) and There (ranged/directional).
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Health/Signalia | Damager")]
    public class Damager : MonoBehaviour
    {
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private LayerMask damageableLayers = ~0;
        [SerializeField] private DamagerType damagerType = DamagerType.Here;
        
        [SerializeField] private DamagerShape hereShape = DamagerShape.Sphere;
        [SerializeField] private float sphereRadius = 1f;
        [SerializeField] private Vector3 boxSize = Vector3.one;
        [SerializeField] private float capsuleRadius = 0.5f;
        [SerializeField] private float capsuleHeight = 2f;
        [SerializeField] private Collider damagerCollider;
        [SerializeField] private bool triggerOnEnable = true;
        
        [SerializeField] private bool enableMultiHit = false;
        [SerializeField] private float activeDamageDuration = 0.2f;
        [SerializeField] private float damageInterval = 0.05f;
        
        [SerializeField] private Vector3 castDirection = Vector3.forward;
        [SerializeField] private float castDistance = 10f;
        [SerializeField] private float castRadius = 0.5f;
        
        [SerializeField] private bool useDamageFalloff = false;
        [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [SerializeField] private int sourceId = 0;
        
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private bool showGizmos = true;

        private Coroutine activeDamageCoroutine;
        private bool isDealingActiveDamage = false;

        private void OnEnable()
        {
            if (damagerType == DamagerType.Here && triggerOnEnable)
            {
                if (enableMultiHit)
                {
                    StartActiveDamage();
                }
                else
                {
                    DealDamageHere();
                }
            }
        }

        private void OnDisable()
        {
            StopActiveDamage();
        }

        /// <summary>
        /// Manually trigger Here-type damage (AOE at position).
        /// </summary>
        public void DealDamageHere()
        {
            Vector3 position = transform.position;
            float falloffRange = GetHereFalloffRange();

            Collider[] colliders = GetHereColliders(position);

            if (debugLogs)
            {
                Debug.Log($"[Damager] Here damager at {position} found {colliders.Length} colliders.", this);
            }

            foreach (var collider in colliders)
            {
                // Calculate damage with falloff if enabled
                float distance = Vector3.Distance(position, collider.transform.position);
                float falloffMultiplier = 1f;
                
                if (useDamageFalloff && falloffRange > 0f)
                {
                    float normalizedDistance = Mathf.Clamp01(distance / falloffRange);
                    falloffMultiplier = falloffCurve.Evaluate(normalizedDistance);
                }

                float finalDamage = damageAmount * falloffMultiplier;

                // Broadcast damage event through Health Radio
                var damageEvent = new HealthRadio.DamageEvent(
                    finalDamage,
                    collider.transform.position,
                    damageableLayers,
                    falloffRange
                );

                HealthRadio.BroadcastDamage(damageEvent);

                if (debugLogs)
                {
                    Debug.Log($"[Damager] Broadcast damage {finalDamage} to {collider.name} at distance {distance:F2}", this);
                }
            }
        }

        /// <summary>
        /// Manually trigger There-type damage (directional raycast).
        /// </summary>
        public void DealDamageThere()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.TransformDirection(castDirection.normalized);
            float distance = castDistance;
            float radius = castRadius;

            // Perform sphere cast
            RaycastHit hit;
            bool hasHit = Physics.SphereCast(
                origin,
                radius,
                direction,
                out hit,
                distance,
                damageableLayers
            );

            if (hasHit)
            {
                // Calculate damage with falloff if enabled
                float hitDistance = hit.distance;
                float falloffMultiplier = 1f;
                
                if (useDamageFalloff && distance > 0f)
                {
                    float normalizedDistance = Mathf.Clamp01(hitDistance / distance);
                    falloffMultiplier = falloffCurve.Evaluate(normalizedDistance);
                }

                float finalDamage = damageAmount * falloffMultiplier;

                // Broadcast damage event through Health Radio
                var damageEvent = new HealthRadio.DamageEvent(
                    finalDamage,
                    hit.point,
                    damageableLayers,
                    0f
                );

                HealthRadio.BroadcastDamage(damageEvent);

                if (debugLogs)
                {
                    Debug.Log($"[Damager] There damager hit {hit.collider.name} at {hit.point} with damage {finalDamage}", this);
                }
            }
            else if (debugLogs)
            {
                Debug.Log($"[Damager] There damager cast from {origin} in direction {direction} found no targets.", this);
            }
        }

        /// <summary>
        /// Deal damage based on the damager type.
        /// </summary>
        public void DealDamage()
        {
            if (damagerType == DamagerType.Here)
            {
                if (enableMultiHit)
                {
                    StartActiveDamage();
                }
                else
                {
                    DealDamageHere();
                }
            }
            else
            {
                DealDamageThere();
            }
        }

        /// <summary>
        /// Start active damage period for multi-hit Here-type damage.
        /// </summary>
        public void StartActiveDamage()
        {
            if (damagerType != DamagerType.Here)
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[Damager] StartActiveDamage can only be used with Here-type damagers.", this);
                }
                return;
            }

            if (isDealingActiveDamage)
            {
                StopActiveDamage();
            }

            isDealingActiveDamage = true;
            activeDamageCoroutine = StartCoroutine(ActiveDamageCoroutine());
        }

        /// <summary>
        /// Stop active damage period.
        /// </summary>
        public void StopActiveDamage()
        {
            if (activeDamageCoroutine != null)
            {
                StopCoroutine(activeDamageCoroutine);
                activeDamageCoroutine = null;
            }
            isDealingActiveDamage = false;
        }

        private System.Collections.IEnumerator ActiveDamageCoroutine()
        {
            float elapsedTime = 0f;
            float interval = Mathf.Max(0.01f, damageInterval); // Ensure minimum interval
            
            // Deal damage immediately
            DealDamageHere();

            // Continue dealing damage for the duration
            while (isDealingActiveDamage)
            {
                yield return new WaitForSeconds(interval);
                
                elapsedTime += interval;
                
                // If duration is 0 or less, continue until manually stopped
                if (activeDamageDuration > 0f && elapsedTime >= activeDamageDuration)
                {
                    break;
                }

                DealDamageHere();
            }

            isDealingActiveDamage = false;
            activeDamageCoroutine = null;
        }

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos)
                return;

            Gizmos.color = Color.red;

            if (damagerType == DamagerType.Here)
            {
                Vector3 position = transform.position;

                switch (hereShape)
                {
                    case DamagerShape.Sphere:
                        Gizmos.DrawWireSphere(position, sphereRadius);
                        break;
                    case DamagerShape.Box:
                        Gizmos.matrix = Matrix4x4.TRS(position, transform.rotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, boxSize);
                        Gizmos.matrix = Matrix4x4.identity;
                        break;
                    case DamagerShape.Capsule:
                        DrawCapsuleGizmo(position, transform.up, capsuleRadius, capsuleHeight);
                        break;
                    case DamagerShape.Collider:
                        if (damagerCollider != null)
                        {
                            Bounds bounds = damagerCollider.bounds;
                            Gizmos.DrawWireCube(bounds.center, bounds.size);
                        }
                        break;
                }
            }
            else
            {
                // Draw sphere cast for There damager
                Vector3 origin = transform.position;
                Vector3 direction = transform.TransformDirection(castDirection.normalized);
                
                // Draw start sphere
                Gizmos.DrawWireSphere(origin, castRadius);
                
                // Draw end sphere
                Vector3 end = origin + direction * castDistance;
                Gizmos.DrawWireSphere(end, castRadius);
                
                // Draw direction line
                Gizmos.DrawLine(origin, end);
            }
        }

        private Collider[] GetHereColliders(Vector3 position)
        {
            switch (hereShape)
            {
                case DamagerShape.Sphere:
                    return Physics.OverlapSphere(position, sphereRadius, damageableLayers);
                case DamagerShape.Box:
                    return Physics.OverlapBox(position, boxSize * 0.5f, transform.rotation, damageableLayers);
                case DamagerShape.Capsule:
                    return QueryCapsule(position);
                case DamagerShape.Collider:
                    return QueryColliderShape();
                default:
                    return System.Array.Empty<Collider>();
            }
        }

        private Collider[] QueryCapsule(Vector3 position)
        {
            float radius = Mathf.Max(0f, capsuleRadius);
            float height = Mathf.Max(radius * 2f, capsuleHeight);
            float halfHeight = height * 0.5f;
            Vector3 up = transform.up;
            float pointOffset = Mathf.Max(0f, halfHeight - radius);
            Vector3 point1 = position + up * pointOffset;
            Vector3 point2 = position - up * pointOffset;

            return Physics.OverlapCapsule(point1, point2, radius, damageableLayers);
        }

        private Collider[] QueryColliderShape()
        {
            if (damagerCollider == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[Damager] Collider shape is selected, but no collider is assigned.", this);
                }
                return System.Array.Empty<Collider>();
            }

            var filter = new HealthUtils.ContactFilter();
            filter.SetLayerMask(damageableLayers);
            filter.useTriggers = true;

            var results = new Collider[64];
            int count = damagerCollider.OverlapCollider(filter, results);
            if (count <= 0)
            {
                return System.Array.Empty<Collider>();
            }

            if (count == results.Length)
            {
                return results;
            }

            var trimmed = new Collider[count];
            System.Array.Copy(results, trimmed, count);
            return trimmed;
        }

        private float GetHereFalloffRange()
        {
            switch (hereShape)
            {
                case DamagerShape.Sphere:
                    return sphereRadius;
                case DamagerShape.Box:
                    return boxSize.magnitude * 0.5f;
                case DamagerShape.Capsule:
                    return Mathf.Max(capsuleRadius, capsuleHeight * 0.5f);
                case DamagerShape.Collider:
                    return damagerCollider != null ? damagerCollider.bounds.extents.magnitude : 0f;
                default:
                    return 0f;
            }
        }

        private static void DrawCapsuleGizmo(Vector3 position, Vector3 up, float radius, float height)
        {
            float clampedRadius = Mathf.Max(0f, radius);
            float clampedHeight = Mathf.Max(clampedRadius * 2f, height);
            float halfHeight = clampedHeight * 0.5f;
            float pointOffset = Mathf.Max(0f, halfHeight - clampedRadius);

            Vector3 top = position + up * pointOffset;
            Vector3 bottom = position - up * pointOffset;

            Gizmos.DrawWireSphere(top, clampedRadius);
            Gizmos.DrawWireSphere(bottom, clampedRadius);
            Gizmos.DrawLine(top + Vector3.right * clampedRadius, bottom + Vector3.right * clampedRadius);
            Gizmos.DrawLine(top - Vector3.right * clampedRadius, bottom - Vector3.right * clampedRadius);
            Gizmos.DrawLine(top + Vector3.forward * clampedRadius, bottom + Vector3.forward * clampedRadius);
            Gizmos.DrawLine(top - Vector3.forward * clampedRadius, bottom - Vector3.forward * clampedRadius);
        }

        #endregion
    }
}