using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework.PackageHandlers;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AHAKuo.Signalia.GameSystems.PoolingSystem
{
    /// <summary>
    /// Spawns pooled prefabs and applies explosive forces in various shapes (Sphere, Cone, Ring, Line).
    /// Requires prefabs with dynamic rigidbodies (2D or 3D).
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Pooling/Signalia | Blow Up Pool")]
    public class BlowUpPool : MonoBehaviour
    {
        public enum ExplosionShape
        {
            // Explicit values to keep backward compatibility with scenes saved before Aura was removed:
            // Aura=0, Sphere=1, Cone=2, Ring=3, Line=4
            Sphere = 1,
            Cone = 2,
            Ring = 3,
            Line = 4
        }

        public enum PhysicsMode
        {
            Physics3D,
            Physics2D,
            /// <summary>
            /// Uses FakePhysics component for kinematic objects. Falls back to Physics3D if FakePhysics is not present.
            /// </summary>
            FakePhysics
        }

        [SerializeField] private GameObject prefab;
        [SerializeField] private ExplosionShape explosionShape = ExplosionShape.Sphere;
        [SerializeField] private PhysicsMode physicsMode = PhysicsMode.Physics3D;
        [SerializeField, Min(1)] private int spawnCount = 10;
        [SerializeField] private float explosionForce = 10f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private Vector3 explosionDirection = Vector3.up;
        [SerializeField, Range(0f, 180f)] private float sphereAngle = 180f;
        [SerializeField, Tooltip("If enabled, Sphere spawns are randomized (imperfect) instead of evenly distributed (perfect).")]
        private bool imperfectSpread = false;
        [SerializeField, Range(0f, 180f)] private float coneAngle = 45f;
        [SerializeField] private float ringRadius = 3f;
        [SerializeField] private float lineLength = 5f;
        [SerializeField] private Vector3 lineDirection = Vector3.forward;
        [SerializeField] private bool useRandomRotation = true;
        [SerializeField] private bool useRandomForce = false;
        [SerializeField] private float forceVariation = 0.2f;
        [SerializeField, Min(-1f)] private float lifetime = -1f;
        [SerializeField] private bool spawnEnabled = true;
        [SerializeField] private string afterSpawnEvent = "";
        [SerializeField] private string listenerEvent = "";
        [SerializeField] private bool enableWarming = false;
        [SerializeField, Min(1)] private int warmupCount = 5;
        [SerializeField] private bool warmupOnStart = true;
        [SerializeField] private bool warmupOnAwake = false;

        private Listener listenerDisposable;

        private void Awake()
        {
            if (enableWarming && warmupOnAwake)
            {
                WarmupPool();
            }
        }

        private void Start()
        {
            InitializeListener();

            if (enableWarming && warmupOnStart)
            {
                WarmupPool();
            }
        }

        private void OnDestroy()
        {
            CleanupListener();
        }

        private void InitializeListener()
        {
            if (!string.IsNullOrEmpty(listenerEvent))
            {
                listenerDisposable = SIGS.Listener(listenerEvent, OnListenerEvent);
            }
        }

        private void CleanupListener()
        {
            listenerDisposable?.Dispose();
            listenerDisposable = null;
        }

        private void OnListenerEvent()
        {
            BlowUp();
        }

        /// <summary>
        /// Spawns objects and applies explosive forces based on the configured shape.
        /// </summary>
        public void BlowUp()
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[BlowUpPool] Prefab is null on {gameObject.name}", this);
                return;
            }

            // Validate prefab has rigidbody
            if (!ValidatePrefab())
            {
                return;
            }

            List<GameObject> spawnedObjects = new List<GameObject>(spawnCount);

            // Spawn all objects first
            for (int i = 0; i < spawnCount; i++)
            {
                GameObject spawned = prefab.FromPool(lifetime, spawnEnabled);
                if (spawned == null) continue;

                spawnedObjects.Add(spawned);
            }

            // Apply forces based on explosion shape
            ApplyExplosionForces(spawnedObjects);

            // Fire after spawn event
            if (!string.IsNullOrEmpty(afterSpawnEvent))
            {
                foreach (var obj in spawnedObjects)
                {
                    afterSpawnEvent.SendEvent(obj);
                }
            }
        }

        private bool ValidatePrefab()
        {
            if (prefab == null) return false;

            if (physicsMode == PhysicsMode.Physics3D)
            {
                Rigidbody rb = prefab.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogError($"[BlowUpPool] Prefab '{prefab.name}' must have a Rigidbody component for 3D physics mode.", this);
                    return false;
                }
                if (rb.isKinematic)
                {
                    Debug.LogError($"[BlowUpPool] Prefab '{prefab.name}' has a kinematic Rigidbody. It must be dynamic (isKinematic = false) for BlowUpPool to work.", this);
                    return false;
                }
            }
            else if (physicsMode == PhysicsMode.Physics2D)
            {
                Rigidbody2D rb2D = prefab.GetComponent<Rigidbody2D>();
                if (rb2D == null)
                {
                    Debug.LogError($"[BlowUpPool] Prefab '{prefab.name}' must have a Rigidbody2D component for 2D physics mode.", this);
                    return false;
                }
                if (rb2D.isKinematic)
                {
                    Debug.LogError($"[BlowUpPool] Prefab '{prefab.name}' has a kinematic Rigidbody2D. It must be dynamic (isKinematic = false) for BlowUpPool to work.", this);
                    return false;
                }
            }
            else if (physicsMode == PhysicsMode.FakePhysics)
            {
                // FakePhysics mode: Check for FakePhysics component first, fallback to dynamic Rigidbody
                FakePhysics fakePhysics = prefab.GetComponent<FakePhysics>();
                if (fakePhysics != null)
                {
                    // FakePhysics found - valid for kinematic rigidbodies
                    return true;
                }
                
                // Fallback: Check for dynamic Rigidbody
                Rigidbody rb = prefab.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogError($"[BlowUpPool] Prefab '{prefab.name}' must have either a FakePhysics component or a dynamic Rigidbody for FakePhysics mode.", this);
                    return false;
                }
                if (rb.isKinematic)
                {
                    Debug.LogError($"[BlowUpPool] Prefab '{prefab.name}' has a kinematic Rigidbody without FakePhysics. Add a FakePhysics component or use a dynamic Rigidbody.", this);
                    return false;
                }
                // Has dynamic rigidbody, will use fallback
            }

            return true;
        }

        private void ApplyExplosionForces(List<GameObject> objects)
        {
            Vector3 center = transform.position;
            Quaternion rotation = transform.rotation;

            for (int i = 0; i < objects.Count; i++)
            {
                GameObject obj = objects[i];
                Vector3 position = GetSpawnPosition(center, rotation, i);
                obj.transform.position = position;

                if (useRandomRotation)
                {
                    obj.transform.rotation = Random.rotation;
                }
                else
                {
                    obj.transform.rotation = rotation;
                }

                Vector3 forceDirection = GetForceDirection(position, center, rotation);
                float force = useRandomForce 
                    ? explosionForce * (1f + Random.Range(-forceVariation, forceVariation))
                    : explosionForce;

                ApplyForce(obj, forceDirection, force);
            }
        }

        private Vector3 GetSpawnPosition(Vector3 center, Quaternion rotation, int index, bool deterministicForGizmos = false)
        {
            // Legacy safety: older scenes may still have ExplosionShape = 0 (previously Aura).
            // Aura is removed from the public enum list; treat legacy 0 as Sphere.
            if ((int)explosionShape == 0)
                explosionShape = ExplosionShape.Sphere;

            switch (explosionShape)
            {
                case ExplosionShape.Sphere:
                    // Spawn evenly distributed on a spherical cap around Explosion Direction.
                    // sphereAngle = 180 => full sphere, 90 => hemisphere, smaller => narrower cap.
                    {
                        float clampedAngle = Mathf.Clamp(sphereAngle, 0f, 180f);
                        Vector3 axis = rotation * (explosionDirection.sqrMagnitude > 0.0001f ? explosionDirection.normalized : Vector3.up);

                        Vector3 dirWorld;
                        if (imperfectSpread)
                        {
                            dirWorld = deterministicForGizmos
                                ? GetRandomDirectionInCapDeterministic(axis, clampedAngle, index)
                                : GetRandomDirectionInCap(axis, clampedAngle);
                        }
                        else
                        {
                            float n = Mathf.Max(1f, spawnCount);
                            float u = (index + 0.5f) / n; // 0..1

                            float cosMin = Mathf.Cos(clampedAngle * Mathf.Deg2Rad); // -1..1

                            // y is cos(polar). For a cap around +Y: y in [cosMin, 1]
                            float y = Mathf.Lerp(cosMin, 1f, u);
                            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));

                            // Golden angle distribution
                            const float goldenRatio = 1.61803398875f;
                            float theta = 2f * Mathf.PI * (index / goldenRatio);

                            Vector3 dirLocal = new Vector3(r * Mathf.Cos(theta), y, r * Mathf.Sin(theta));
                            Quaternion align = Quaternion.FromToRotation(Vector3.up, axis);
                            dirWorld = align * dirLocal;
                        }

                        return center + dirWorld * explosionRadius;
                    }

                case ExplosionShape.Cone:
                    // Spawn randomly within a cone shape
                    float distance;
                    float angle;
                    float radialOffset;
                    
                    if (deterministicForGizmos)
                    {
                        // Use seeded random for deterministic gizmo visualization
                        int seed = unchecked(index * 92821) ^ unchecked(Mathf.RoundToInt(center.x * 1000f) * 73856093)
                                             ^ unchecked(Mathf.RoundToInt(center.y * 1000f) * 19349663)
                                             ^ unchecked(Mathf.RoundToInt(center.z * 1000f) * 83492791);
                        System.Random rng = new System.Random(seed);
                        
                        distance = (float)rng.NextDouble() * explosionRadius;
                        angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                        float maxSpread = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * distance;
                        radialOffset = (float)rng.NextDouble() * maxSpread;
                    }
                    else
                    {
                        // Random distance along cone axis (0 to explosionRadius)
                        distance = Random.Range(0f, explosionRadius);
                        
                        // Random angle around cone axis
                        angle = Random.Range(0f, Mathf.PI * 2f);
                        
                        // Random radial offset within cone cross-section at this distance
                        // The spread radius increases linearly with distance
                        float maxSpread = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * distance;
                        radialOffset = Random.Range(0f, maxSpread);
                    }
                    
                    Vector3 coneDir = rotation * explosionDirection.normalized;
                    Vector3 perpendicular = Vector3.Cross(coneDir, Vector3.up).normalized;
                    if (perpendicular == Vector3.zero)
                        perpendicular = Vector3.Cross(coneDir, Vector3.forward).normalized;
                    Vector3 offset = (Mathf.Cos(angle) * perpendicular + Mathf.Sin(angle) * Vector3.Cross(coneDir, perpendicular).normalized) * radialOffset;
                    return center + coneDir * distance + offset;

                case ExplosionShape.Ring:
                    // Spawn in a ring around center
                    float ringAngle = (index / (float)spawnCount) * Mathf.PI * 2f;
                    Vector3 ringDir = rotation * new Vector3(Mathf.Cos(ringAngle), 0f, Mathf.Sin(ringAngle));
                    return center + ringDir * ringRadius;

                case ExplosionShape.Line:
                    // Spawn along a line
                    float t = spawnCount > 1 ? index / (float)(spawnCount - 1) : 0f;
                    Vector3 lineDir = rotation * lineDirection.normalized;
                    return center + lineDir * (lineLength * (t - 0.5f));

                default:
                    return center;
            }
        }

        private static Vector3 GetRandomDirectionInCap(Vector3 axis, float angleDegrees)
        {
            Vector3 a = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

            float clampedAngle = Mathf.Clamp(angleDegrees, 0f, 180f);
            float cosMin = Mathf.Cos(clampedAngle * Mathf.Deg2Rad);

            // Sample uniformly on spherical cap around +Y, then rotate to axis.
            float y = Random.Range(cosMin, 1f);
            float phi = Random.Range(0f, 2f * Mathf.PI);
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            Vector3 dirLocal = new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi));

            Quaternion align = Quaternion.FromToRotation(Vector3.up, a);
            return align * dirLocal;
        }

        private static Vector3 GetRandomDirectionInCapDeterministic(Vector3 axis, float angleDegrees, int index)
        {
            Vector3 a = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

            float clampedAngle = Mathf.Clamp(angleDegrees, 0f, 180f);
            float cosMin = Mathf.Cos(clampedAngle * Mathf.Deg2Rad);

            // Deterministic random for gizmos (prevents flicker).
            int seed = unchecked(index * 92821) ^ unchecked(Mathf.RoundToInt(a.x * 1000f) * 73856093)
                                     ^ unchecked(Mathf.RoundToInt(a.y * 1000f) * 19349663)
                                     ^ unchecked(Mathf.RoundToInt(a.z * 1000f) * 83492791);
            System.Random rng = new System.Random(seed);

            float y = Mathf.Lerp(cosMin, 1f, (float)rng.NextDouble());
            float phi = (float)rng.NextDouble() * 2f * Mathf.PI;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            Vector3 dirLocal = new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi));

            Quaternion align = Quaternion.FromToRotation(Vector3.up, a);
            return align * dirLocal;
        }

        private Vector3 GetForceDirection(Vector3 objectPosition, Vector3 center, Quaternion rotation)
        {
            // Legacy safety: older scenes may still have ExplosionShape = 0 (previously Aura).
            // Aura is removed from the public enum list; treat legacy 0 as Sphere.
            if ((int)explosionShape == 0)
                explosionShape = ExplosionShape.Sphere;

            switch (explosionShape)
            {
                case ExplosionShape.Sphere:
                    // Explode outward from center
                    Vector3 dir = (objectPosition - center).normalized;
                    if (dir == Vector3.zero) dir = Random.onUnitSphere;
                    return dir;

                case ExplosionShape.Cone:
                    // Force outward within the cone (direction depends on each spawn position).
                    // Using only the cone axis makes every object travel in a perfectly parallel line.
                    {
                        Vector3 coneAxis = rotation * (explosionDirection.sqrMagnitude > 0.0001f ? explosionDirection.normalized : Vector3.up);
                        Vector3 coneOut = (objectPosition - center);
                        if (coneOut.sqrMagnitude < 0.0001f)
                            return coneAxis.normalized;
                        return coneOut.normalized;
                    }

                case ExplosionShape.Ring:
                    // Force outward from center in horizontal plane
                    Vector3 ringForce = (objectPosition - center);
                    ringForce.y = 0f;
                    if (ringForce == Vector3.zero) ringForce = Vector3.forward;
                    return ringForce.normalized;

                case ExplosionShape.Line:
                    // Force along line direction
                    return rotation * lineDirection.normalized;

                default:
                    return Vector3.up;
            }
        }

        private void ApplyForce(GameObject obj, Vector3 direction, float force)
        {
            if (physicsMode == PhysicsMode.Physics3D)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddForce(direction * force, ForceMode.Impulse);
                }
            }
            else if (physicsMode == PhysicsMode.Physics2D)
            {
                Rigidbody2D rb2D = obj.GetComponent<Rigidbody2D>();
                if (rb2D != null && !rb2D.isKinematic)
                {
                    Vector2 force2D = new Vector2(direction.x, direction.y);
                    rb2D.AddForce(force2D * force, ForceMode2D.Impulse);
                }
            }
            else if (physicsMode == PhysicsMode.FakePhysics)
            {
                // Try FakePhysics first
                FakePhysics fakePhysics = obj.GetComponent<FakePhysics>();
                if (fakePhysics != null)
                {
                    fakePhysics.AddForce(direction * force);
                    return;
                }
                
                // Fallback to dynamic Rigidbody
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddForce(direction * force, ForceMode.Impulse);
                }
            }
        }

        /// <summary>
        /// Warms up the pool with the configured warmup count.
        /// </summary>
        public void WarmupPool()
        {
            if (!enableWarming || prefab == null)
            {
                return;
            }

            prefab.WarmupPool(warmupCount);
        }

        /// <summary>
        /// Enables warming and warms up the pool.
        /// </summary>
        public void EnableWarmingAndWarmup()
        {
            enableWarming = true;
            WarmupPool();
        }

        /// <summary>
        /// Disables warming.
        /// </summary>
        public void DisableWarming()
        {
            enableWarming = false;
        }

        public bool IsWarmingEnabled => enableWarming;
        public int WarmupCount => warmupCount;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        private void DrawGizmos(bool selected)
        {
            if (prefab == null) return;

            Color gizmoColor = selected ? Color.yellow : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.color = gizmoColor;
            Handles.color = gizmoColor;

            Vector3 center = transform.position;
            Quaternion rotation = transform.rotation;

            switch (explosionShape)
            {
                case ExplosionShape.Sphere:
                    Gizmos.DrawWireSphere(center, explosionRadius);

                    // Visualize sphereAngle cap (if not full sphere)
                    {
                        float clampedAngle = Mathf.Clamp(sphereAngle, 0f, 180f);
                        if (clampedAngle < 179.9f)
                        {
                            Vector3 axis = rotation * (explosionDirection.sqrMagnitude > 0.0001f ? explosionDirection.normalized : Vector3.up);
                            float dist = explosionRadius * Mathf.Cos(clampedAngle * Mathf.Deg2Rad);
                            float rimRadius = Mathf.Abs(explosionRadius * Mathf.Sin(clampedAngle * Mathf.Deg2Rad));
                            Vector3 rimCenter = center + axis * dist;

                            Handles.DrawWireDisc(rimCenter, axis, rimRadius);
                            Gizmos.DrawLine(center, rimCenter);
                        }
                    }

                    // Draw some sample spawn positions
                    for (int i = 0; i < Mathf.Min(spawnCount, 20); i++)
                    {
                        Vector3 pos = GetSpawnPosition(center, rotation, i, deterministicForGizmos: true);
                        Gizmos.DrawSphere(pos, 0.1f);
                        // Draw force direction
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(pos, (pos - center).normalized * 0.5f);
                        Gizmos.color = gizmoColor;
                    }
                    break;

                case ExplosionShape.Cone:
                    {
                        Vector3 coneDir = rotation * explosionDirection.normalized;
                        if (coneDir.sqrMagnitude < 0.0001f)
                            coneDir = rotation * Vector3.up;
                        coneDir.Normalize();

                        float coneLength = Mathf.Max(0f, explosionRadius);
                        float coneRadius = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * coneLength;
                        Vector3 coneTip = center + coneDir * coneLength;

                        // Stable orthonormal basis around coneDir (prevents zero-cross issues)
                        Vector3 axisA = Vector3.Cross(coneDir, Vector3.up);
                        if (axisA.sqrMagnitude < 0.0001f)
                            axisA = Vector3.Cross(coneDir, Vector3.right);
                        axisA.Normalize();
                        Vector3 axisB = Vector3.Cross(coneDir, axisA).normalized;

                        // Draw discs + outline
                        Handles.DrawWireDisc(center, coneDir, 0.1f);
                        Handles.DrawWireDisc(coneTip, coneDir, coneRadius);
                        for (int i = 0; i < 8; i++)
                        {
                            float a = (i / 8f) * Mathf.PI * 2f;
                            Vector3 rim = coneTip + (Mathf.Cos(a) * axisA + Mathf.Sin(a) * axisB) * coneRadius;
                            Gizmos.DrawLine(center, rim);
                        }

                        // Draw sample spawn positions using the same spawn math as runtime
                        int sampleCount = Mathf.Min(spawnCount, 12);
                        for (int i = 0; i < sampleCount; i++)
                        {
                            Vector3 pos = GetSpawnPosition(center, rotation, i, deterministicForGizmos: true);
                            Gizmos.DrawSphere(pos, 0.1f);
                            Gizmos.color = Color.red;
                            Vector3 forceDir = GetForceDirection(pos, center, rotation);
                            Gizmos.DrawRay(pos, forceDir * 0.5f);
                            Gizmos.color = gizmoColor;
                        }
                    }
                    break;

                case ExplosionShape.Ring:
                    Handles.DrawWireDisc(center, rotation * Vector3.up, ringRadius);
                    // Draw sample spawn positions
                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 pos = GetSpawnPosition(center, rotation, i);
                        Gizmos.DrawSphere(pos, 0.1f);
                        // Draw force direction
                        Gizmos.color = Color.red;
                        Vector3 forceDir = (pos - center);
                        forceDir.y = 0f;
                        if (forceDir.sqrMagnitude < 0.0001f) forceDir = rotation * Vector3.forward;
                        forceDir.Normalize();
                        Gizmos.DrawRay(pos, forceDir * 0.5f);
                        Gizmos.color = gizmoColor;
                    }
                    break;

                case ExplosionShape.Line:
                    Vector3 lineDir = rotation * lineDirection.normalized;
                    Vector3 lineStart = center - lineDir * (lineLength * 0.5f);
                    Vector3 lineEnd = center + lineDir * (lineLength * 0.5f);
                    Gizmos.DrawLine(lineStart, lineEnd);
                    Gizmos.DrawSphere(lineStart, 0.1f);
                    Gizmos.DrawSphere(lineEnd, 0.1f);
                    // Draw sample spawn positions
                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 pos = GetSpawnPosition(center, rotation, i);
                        Gizmos.DrawSphere(pos, 0.1f);
                        // Draw force direction
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(pos, lineDir * 0.5f);
                        Gizmos.color = gizmoColor;
                    }
                    break;
            }

            // Draw center point
            Gizmos.color = selected ? Color.red : Color.white;
            Gizmos.DrawSphere(center, 0.15f);
        }
#endif
    }
}

