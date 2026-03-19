using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.GameSystems.SaveSystem;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics
{
    /// <summary>
    /// Triggerbox provides a unified trigger utility that can work with both collider callbacks and raycast zones.
    /// Supports 2D and 3D gameplay, optional layer/tag filters, UnityEvent callbacks, Signalia string events, and
    /// persistent disable behaviour when combined with the Save System.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Common Mechanics/Signalia | Trigger Box")]
    public class Triggerbox : MonoBehaviour
    {
        public enum TriggerSpace
        {
            Space3D,
            Space2D
        }

        public enum TriggerMode
        {
            Collider,
            Raycast
        }

        public enum DisableMode
        {
            None,
            OneShot,
            Consistent
        }

        [Serializable]
        public class TriggerUnityEvent : UnityEvent<GameObject> { }

        [Header("Trigger Behaviour")]
        [SerializeField] private TriggerMode triggerMode = TriggerMode.Collider;
        [SerializeField] private TriggerSpace triggerSpace = TriggerSpace.Space3D;

        /// <summary>
        /// Current trigger mode (Collider or Raycast).
        /// </summary>
        public TriggerMode Mode => triggerMode;

        /// <summary>
        /// Current physics space (2D or 3D).
        /// </summary>
        public TriggerSpace Space => triggerSpace;

        /// <summary>
        /// Indicates whether the editor should render gizmos for the raycast zone.
        /// </summary>
        public bool DrawsGizmos() => drawDebugGizmos;

        [Header("Raycast Zone")]
        [Tooltip("Local space size of the raycast box. For 2D mode only X/Y are used.")]
        [SerializeField] private Vector3 raycastBoxSize = Vector3.one;
        [Tooltip("Local space offset from the transform where the raycast box is evaluated.")]
        [SerializeField] private Vector3 raycastBoxOffset = Vector3.zero;
        [Tooltip("Draw the configured raycast box in the Scene view for easier authoring.")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0f, 0.8f, 1f, 0.25f);

        [Header("Filters")]
        [Tooltip("Only allow objects on specific layers to trigger this box.")]
        [SerializeField] private bool useLayerFilter = false;
        [SerializeField] private LayerMask allowedLayers = ~0;
        [Tooltip("Only allow objects with a specific tag to trigger this box.")]
        [SerializeField] private bool useTagFilter = false;
        [SerializeField] private string requiredTag = "";

        [Header("Timings")]
        [Tooltip("Invoke OnStay events continuously. When enabled, a cooldown gate throttles the callbacks.")]
        [SerializeField] private bool useStayCooldown = true;
        [Tooltip("Cooldown duration (per object) between OnStay callbacks. Set to 0 to call every frame.")]
        [SerializeField] private float stayCooldown = 0.25f;

        [Header("Events")]
        [SerializeField] private TriggerUnityEvent onEnter = new TriggerUnityEvent();
        [SerializeField] private TriggerUnityEvent onStay = new TriggerUnityEvent();
        [SerializeField] private TriggerUnityEvent onExit = new TriggerUnityEvent();

        [SerializeField] private string onEnterStringEvent = string.Empty;
        [SerializeField] private string onStayStringEvent = string.Empty;
        [SerializeField] private string onExitStringEvent = string.Empty;

        [SerializeField] private string onEnterAudio = string.Empty;
        [SerializeField] private string onStayAudio = string.Empty;
        [SerializeField] private string onExitAudio = string.Empty;

        [Header("Disabling")]
        [SerializeField] private DisableMode disableAfterTrigger = DisableMode.None;
        [Tooltip("Disable the GameObject instead of only disabling this component when the trigger completes.")]
        [SerializeField] private bool deactivateGameObject = true;
        [Tooltip("Persistent key used when Consistent mode is active. Click the Generate button in the inspector to create a unique key.")]
        [SerializeField] private string consistentSaveKey = string.Empty;

        private readonly HashSet<GameObject> trackedObjects = new HashSet<GameObject>();
        private readonly HashSet<GameObject> raycastResults = new HashSet<GameObject>();
        private readonly List<GameObject> removalBuffer = new List<GameObject>();
        private readonly List<GameObject> cleanupBuffer = new List<GameObject>();

        private bool hasTriggered;
        private bool pendingDisable;

        /// <summary>
        /// Generates a unique key for consistent save state usage.
        /// </summary>
        public string GenerateConsistentKey() => $"trg_{Guid.NewGuid():N}";

        private void Awake()
        {
            if (disableAfterTrigger == DisableMode.Consistent && !string.IsNullOrWhiteSpace(consistentSaveKey))
            {
                bool alreadyConsumed = GameSaving.Load(consistentSaveKey,
                    ConfigReader.GetConfig().SavingSystem.SettingsFileName, false);
                if (alreadyConsumed)
                {
                    if (deactivateGameObject)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        enabled = false;
                    }
                }
            }
            else if (disableAfterTrigger == DisableMode.Consistent)
            {
                Debug.LogWarning("[Triggerbox] Consistent disable mode is enabled but no save key is provided.", this);
            }
        }

        private void OnEnable()
        {
            CleanupTrackedObjects();
            pendingDisable = false;
        }

        private void OnDisable()
        {
            ClearAllCooldowns();
            trackedObjects.Clear();
            raycastResults.Clear();
            removalBuffer.Clear();
            cleanupBuffer.Clear();
            pendingDisable = false;
        }

        private void FixedUpdate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (triggerMode == TriggerMode.Raycast)
            {
                EvaluateRaycastZone();
            }
        }

        private void EvaluateRaycastZone()
        {
            CleanupTrackedObjects();
            raycastResults.Clear();

            if (triggerSpace == TriggerSpace.Space3D)
            {
                Vector3 center = transform.TransformPoint(raycastBoxOffset);
                Vector3 halfExtents = new Vector3(Mathf.Max(0.001f, raycastBoxSize.x * 0.5f),
                    Mathf.Max(0.001f, raycastBoxSize.y * 0.5f),
                    Mathf.Max(0.001f, raycastBoxSize.z * 0.5f));
                int mask = useLayerFilter ? allowedLayers.value : Physics.AllLayers;
                Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, mask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hit = hits[i];
                    if (hit == null)
                    {
                        continue;
                    }

                    GameObject target = hit.gameObject;
                    if (!IsValidTarget(target))
                    {
                        continue;
                    }

                    raycastResults.Add(target);
                    ProcessEnter(target);
                    ProcessStay(target);
                }
            }
            else
            {
                Vector3 worldCenter = transform.TransformPoint(raycastBoxOffset);
                Vector2 center = new Vector2(worldCenter.x, worldCenter.y);
                Vector2 size = new Vector2(Mathf.Max(0.001f, raycastBoxSize.x), Mathf.Max(0.001f, raycastBoxSize.y));
                float angle = transform.eulerAngles.z;
                int mask = useLayerFilter ? allowedLayers.value : Physics2D.DefaultRaycastLayers;
                Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, mask);
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D hit = hits[i];
                    if (hit == null)
                    {
                        continue;
                    }

                    GameObject target = hit.gameObject;
                    if (!IsValidTarget(target))
                    {
                        continue;
                    }

                    raycastResults.Add(target);
                    ProcessEnter(target);
                    ProcessStay(target);
                }
            }

            removalBuffer.Clear();
            foreach (GameObject tracked in trackedObjects)
            {
                if (tracked == null || !raycastResults.Contains(tracked))
                {
                    removalBuffer.Add(tracked);
                }
            }

            for (int i = 0; i < removalBuffer.Count; i++)
            {
                ProcessExit(removalBuffer[i]);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space3D)
            {
                return;
            }

            ProcessEnter(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space3D)
            {
                return;
            }

            ProcessStay(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space3D)
            {
                return;
            }

            ProcessExit(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space2D)
            {
                return;
            }

            ProcessEnter(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space2D)
            {
                return;
            }

            ProcessStay(other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (triggerMode != TriggerMode.Collider || triggerSpace != TriggerSpace.Space2D)
            {
                return;
            }

            ProcessExit(other.gameObject);
        }

        private void ProcessEnter(GameObject target)
        {
            if (target == null || !IsValidTarget(target))
            {
                return;
            }

            if (hasTriggered && disableAfterTrigger != DisableMode.None && !trackedObjects.Contains(target))
            {
                return;
            }

            bool added = trackedObjects.Add(target);
            if (!added)
            {
                return;
            }

            InvokeUnityEvent(onEnter, target);
            InvokeStringEvent(onEnterStringEvent);

            if (onEnterAudio.HasValue())
            {
                SIGS.PlayAudio(onEnterAudio);
            }

            if (disableAfterTrigger != DisableMode.None && !hasTriggered)
            {
                hasTriggered = true;
                PersistDisableState();
                pendingDisable = true;
            }
        }

        private void ProcessStay(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (!trackedObjects.Contains(target) || !IsValidTarget(target))
            {
                return;
            }

            if (!useStayCooldown || stayCooldown <= 0f)
            {
                InvokeStayEvents(target);
                return;
            }

            if (SIGS.CooldownGate(GetStayCooldownKey(target), stayCooldown))
            {
                InvokeStayEvents(target);
            }
        }

        private void InvokeStayEvents(GameObject target)
        {
            InvokeUnityEvent(onStay, target);
            InvokeStringEvent(onStayStringEvent);

            if (onStayAudio.HasValue())
            {
                SIGS.PlayAudio(onStayAudio);
            }
        }

        private void ProcessExit(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (!trackedObjects.Remove(target))
            {
                return;
            }

            SIGS.KillCooldownGate(GetStayCooldownKey(target));

            InvokeUnityEvent(onExit, target);
            InvokeStringEvent(onExitStringEvent);

            if (onExitAudio.HasValue())
            {
                SIGS.PlayAudio(onExitAudio);
            }

            if (pendingDisable && trackedObjects.Count == 0)
            {
                DisableTriggerNow();
            }
        }

        private void DisableTriggerNow()
        {
            pendingDisable = false;

            switch (disableAfterTrigger)
            {
                case DisableMode.OneShot:
                    if (deactivateGameObject)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        enabled = false;
                    }
                    break;
                case DisableMode.Consistent:
                    if (deactivateGameObject)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        enabled = false;
                    }
                    break;
            }
        }

        private void PersistDisableState()
        {
            if (disableAfterTrigger != DisableMode.Consistent)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(consistentSaveKey))
            {
                Debug.LogWarning("[Triggerbox] Consistent mode is enabled but no save key is provided.", this);
                return;
            }

            GameSaving.Save(consistentSaveKey, true, ConfigReader.GetConfig().SavingSystem.SettingsFileName);
        }

        private bool IsValidTarget(GameObject target)
        {
            if (target == null || target == gameObject)
            {
                return false;
            }

            if (target.transform != null && target.transform.IsChildOf(transform))
            {
                return false;
            }

            if (useLayerFilter && (allowedLayers.value & (1 << target.layer)) == 0)
            {
                return false;
            }

            if (useTagFilter && !string.IsNullOrEmpty(requiredTag) && !target.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        private static void InvokeUnityEvent(TriggerUnityEvent unityEvent, GameObject target)
        {
            unityEvent?.Invoke(target);
        }

        private static void InvokeStringEvent(string eventName)
        {
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                eventName.SendEvent();
            }
        }

        private string GetStayCooldownKey(GameObject target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            return $"Triggerbox_{GetInstanceID()}_{target.GetInstanceID()}_Stay";
        }

        private void ClearAllCooldowns()
        {
            foreach (GameObject target in trackedObjects)
            {
                if (target == null)
                {
                    continue;
                }

                string key = GetStayCooldownKey(target);
                if (!string.IsNullOrEmpty(key))
                {
                    SIGS.KillCooldownGate(key);
                }
            }
        }

        private void CleanupTrackedObjects()
        {
            if (trackedObjects.Count == 0)
            {
                return;
            }

            cleanupBuffer.Clear();
            foreach (GameObject tracked in trackedObjects)
            {
                if (tracked == null)
                {
                    cleanupBuffer.Add(tracked);
                }
            }

            for (int i = 0; i < cleanupBuffer.Count; i++)
            {
                GameObject stale = cleanupBuffer[i];
                if (stale != null)
                {
                    SIGS.KillCooldownGate(GetStayCooldownKey(stale));
                }

                trackedObjects.Remove(stale);
            }

            cleanupBuffer.Clear();
        }

        private void OnValidate()
        {
            raycastBoxSize.x = Mathf.Max(0f, raycastBoxSize.x);
            raycastBoxSize.y = Mathf.Max(0f, raycastBoxSize.y);
            raycastBoxSize.z = Mathf.Max(0f, raycastBoxSize.z);
            stayCooldown = Mathf.Max(0f, stayCooldown);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || triggerMode != TriggerMode.Raycast)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Quaternion rotation = triggerSpace == TriggerSpace.Space3D
                ? transform.rotation
                : Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(raycastBoxOffset), rotation, Vector3.one);

            if (triggerSpace == TriggerSpace.Space3D)
            {
                Gizmos.DrawCube(Vector3.zero, new Vector3(raycastBoxSize.x, raycastBoxSize.y, raycastBoxSize.z));
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, new Vector3(raycastBoxSize.x, raycastBoxSize.y, 0.05f));
            }

            Gizmos.matrix = previousMatrix;
        }
    }
}
