using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

using AHAKuo.Signalia.GameSystems.DialogueSystem;

using AHAKuo.Signalia.GameSystems.SaveSystem;

using AHAKuo.Signalia.GameSystems.Localization.Internal;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics
{
    /// <summary>
    /// A raycast-box based interaction zone. The InteractorEye must be within the box bounds
    /// and on an allowed layer to be considered "in range". Optionally requires look-at check
    /// which is controlled by the InteractorEye component.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Common Mechanics/Signalia | Interactive Zone")]
    public class InteractiveZone : MonoBehaviour
    {
        public enum PromptDisplayMode
        {
            LocalUIView,
            UIViewByName
        }

        public enum PersistenceMode
        {
            None,
            OneShot,
            Consistent
        }

        [SerializeField] private Vector3 boxSize = new(2f, 2f, 2f);
        [SerializeField] private Vector3 boxOffset = Vector3.zero;
        [SerializeField] private LayerMask allowedLayers = ~0;
        [SerializeField] private bool use2DPhysics = false;

        [SerializeField] private Transform lookPoint;
        [SerializeField, Range(0f, 180f)] private float lookAngleThreshold = 45f;
        [SerializeField] private bool requireLineOfSight = false;
        [SerializeField] private LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;

        [SerializeField] private List<RadioCondition> radioConditions = new();
        [SerializeField] private List<InteractiveZoneConditionAsset> scriptableConditions = new();

        [SerializeField] private PromptDisplayMode promptDisplayMode = PromptDisplayMode.LocalUIView;
        [SerializeField] private UIView promptView;
        [SerializeField] private string promptViewName;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string promptDeadKey;
        [SerializeField] private string defaultPromptText = "Interact";
        [SerializeField] private bool hidePromptOnDisable = true;

        [SerializeField] private UnityEvent onInteract;
        [SerializeField] private List<string> stringEvents = new();

        [SerializeField] private string interactAudio;

        [SerializeField] private PersistenceMode persistence = PersistenceMode.None;
        [SerializeField] private string saveKeyOverride;
        [SerializeField] private bool autoDisableOnConsumed = true;

        [SerializeField, Min(0f)] private float interactionCooldown = 0f;
        [SerializeField] private bool requireReEntry = false;

        [SerializeField] private bool debugLogs = false;

        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new(0.2f, 1f, 0.9f, 0.35f);

        private bool promptVisible;
        private bool hasConsumed;
        private bool initialized;
        [SerializeField] private bool interactable = true;
        private InteractorEye activeEye;
        private string cachedSaveKey;
        private float lastInteractionTime = float.NegativeInfinity;
        private bool awaitingReEntry;
        private bool hasExitedSinceInteraction;

        private static readonly Dictionary<int, string> hierarchyPathCache = new();
        internal static readonly HashSet<InteractiveZone> RegisteredZones = new();

        public Vector3 BoxSize => boxSize;
        public Vector3 BoxOffset => boxOffset;
        public LayerMask AllowedLayers => allowedLayers;
        public bool Use2DPhysics => use2DPhysics;
        public float LookAngleThreshold => lookAngleThreshold;
        public bool RequireLineOfSight => requireLineOfSight;

        public bool IsConsumed => hasConsumed;
        public bool IsInteractable => interactable;
        public bool IsOnCooldown => interactionCooldown > 0f && Time.time < lastInteractionTime + interactionCooldown;
        public bool IsAwaitingReEntry => awaitingReEntry;
        public float CooldownRemaining => Mathf.Max(0f, (lastInteractionTime + interactionCooldown) - Time.time);

        private void Awake()
        {
            // Auto-find promptText if not assigned and using LocalUIView mode
            if (promptDisplayMode == PromptDisplayMode.LocalUIView && promptText == null && promptView != null)
            {
                promptText = promptView.GetComponentInChildren<TMP_Text>();
            }
            
            InitializePersistence();
            initialized = true;
        }

        private void OnEnable()
        {
            // Auto-find promptText if not assigned and using LocalUIView mode
            if (promptDisplayMode == PromptDisplayMode.LocalUIView && promptText == null && promptView != null)
            {
                promptText = promptView.GetComponentInChildren<TMP_Text>();
            }
            
            if (!initialized)
            {
                InitializePersistence();
                initialized = true;
            }

            RegisteredZones.Add(this);
        }

        private void OnDisable()
        {
            RegisteredZones.Remove(this);

            if (hidePromptOnDisable)
            {
                HidePrompt();
            }

            activeEye = null;
        }

        public Vector3 GetBoxCenter()
        {
            return transform.position + transform.TransformVector(boxOffset);
        }

        public Vector3 GetLookPointPosition()
        {
            return lookPoint != null ? lookPoint.position : GetBoxCenter();
        }

        /// <summary>
        /// Returns true if the given world position is inside the raycast box zone.
        /// </summary>
        public bool IsPointInZone(Vector3 worldPoint)
        {
            Vector3 center = GetBoxCenter();
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector3 localCenter = transform.InverseTransformPoint(center);
            Vector3 halfExtents = boxSize * 0.5f;

            Vector3 diff = localPoint - localCenter;
            return Mathf.Abs(diff.x) <= halfExtents.x &&
                   Mathf.Abs(diff.y) <= halfExtents.y &&
                   Mathf.Abs(diff.z) <= halfExtents.z;
        }

        /// <summary>
        /// Returns true if the given layer is allowed by this zone's layer mask.
        /// </summary>
        public bool IsLayerAllowed(int layer)
        {
            return (allowedLayers.value & (1 << layer)) != 0;
        }

        internal bool HasLineOfSight(Vector3 origin, Vector3 destination)
        {
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            direction /= distance;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Collide))
            {
                var hitZone = hit.collider != null ? hit.collider.GetComponentInParent<InteractiveZone>() : null;
                return hitZone == this;
            }

            return true;
        }

        internal void NotifyEyeEnter(InteractorEye eye)
        {
            activeEye = eye;

            // Clear re-entry flag when eye re-enters after having exited
            if (awaitingReEntry && hasExitedSinceInteraction)
            {
                awaitingReEntry = false;
                hasExitedSinceInteraction = false;
                if (debugLogs)
                {
                    Debug.Log("[InteractiveZone] Re-entry detected, interaction unlocked", this);
                }
            }

            TryShowPrompt();
        }

        internal void NotifyEyeExit(InteractorEye eye)
        {
            if (activeEye != eye)
            {
                return;
            }

            // Mark that the eye has exited (needed for re-entry requirement)
            if (awaitingReEntry)
            {
                hasExitedSinceInteraction = true;
                if (debugLogs)
                {
                    Debug.Log("[InteractiveZone] Eye exited, awaiting re-entry", this);
                }
            }

            activeEye = null;
            HidePrompt();
        }

        internal bool AcceptsEye(InteractorEye eye)
        {
            if (eye == null)
            {
                return false;
            }

            if (!interactable)
            {
                return false;
            }

            if (hasConsumed)
            {
                return false;
            }

            if (!enabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            if (IsBlockedByDialogue())
            {
                return false;
            }

            // Note: We don't block AcceptsEye for cooldown or re-entry here,
            // so the zone can still be detected and trigger exit/enter events.
            // Interaction blocking happens in TryInteract.

            return EvaluateConditions(eye.gameObject);
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (!interactable)
            {
                HidePrompt();
            }
            else
            {
                TryShowPrompt();
            }
        }

        public void Consume()
        {
            if (persistence == PersistenceMode.None)
            {
                return;
            }

            hasConsumed = true;

            if (persistence == PersistenceMode.Consistent)
            {
                GameSaving.Save(ResolveSaveKey(), true, GetSaveFileName());
            }

            if (autoDisableOnConsumed)
            {
                HidePrompt();
                enabled = false;
            }
        }

        public void ResetConsumed()
        {
            hasConsumed = false;
            TryShowPrompt();
        }

        /// <summary>
        /// Clears the interaction cooldown, allowing immediate re-interaction.
        /// </summary>
        public void ClearCooldown()
        {
            lastInteractionTime = float.NegativeInfinity;
            TryShowPrompt();
        }

        /// <summary>
        /// Clears the re-entry requirement, allowing interaction without leaving and re-entering.
        /// </summary>
        public void ClearReEntryRequirement()
        {
            awaitingReEntry = false;
            hasExitedSinceInteraction = false;
            TryShowPrompt();
        }

        /// <summary>
        /// Resets all interaction buffers (cooldown and re-entry).
        /// </summary>
        public void ResetInteractionBuffer()
        {
            lastInteractionTime = float.NegativeInfinity;
            awaitingReEntry = false;
            hasExitedSinceInteraction = false;
            TryShowPrompt();
        }

        public bool TryInteract(InteractorEye eye)
        {
            if (hasConsumed)
            {
                return false;
            }

            if (eye == null)
            {
                return false;
            }

            if (!interactable)
            {
                return false;
            }

            if (!enabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            if (IsBlockedByDialogue())
            {
                return false;
            }

            if (IsOnCooldown)
            {
                if (debugLogs)
                {
                    Debug.Log($"[InteractiveZone] Interaction blocked - on cooldown ({CooldownRemaining:F2}s remaining)", this);
                }
                return false;
            }

            if (awaitingReEntry)
            {
                if (debugLogs)
                {
                    Debug.Log("[InteractiveZone] Interaction blocked - awaiting re-entry", this);
                }
                return false;
            }

            if (!EvaluateConditions(eye.gameObject))
            {
                return false;
            }

            if (debugLogs)
            {
                Debug.Log("[InteractiveZone] Interaction performed", this);
            }

            onInteract?.Invoke();

            if (stringEvents.HasValue())
            {
                foreach (var evt in stringEvents)
                {
                    if (!evt.HasValue())
                    {
                        continue;
                    }

                    evt.SendEvent(gameObject);
                }
            }

            if (interactAudio.HasValue())
            {
                SIGS.PlayAudio(interactAudio);
            }

            // Apply interaction buffer
            if (interactionCooldown > 0f)
            {
                lastInteractionTime = Time.time;
            }

            if (requireReEntry)
            {
                awaitingReEntry = true;
                hasExitedSinceInteraction = false; // Reset exit flag, will be set when eye exits
                HidePrompt();
            }

            if (persistence == PersistenceMode.OneShot || persistence == PersistenceMode.Consistent)
            {
                hasConsumed = true;
            }

            if (persistence == PersistenceMode.Consistent)
            {
                GameSaving.Save(ResolveSaveKey(), true, GetSaveFileName());
            }

            if (persistence != PersistenceMode.None && autoDisableOnConsumed)
            {
                HidePrompt();
                enabled = false;
            }

            return true;
        }

        private bool IsBlockedByDialogue()
        {
            var config = ConfigReader.GetConfig();
            if (config?.CommonMechanics?.InteractiveZone?.DisableDuringDialogue != true)
            {
                return false;
            }

            return DialogueManager.InDialogueNow;
        }

        private void InitializePersistence()
        {
            if (persistence != PersistenceMode.Consistent)
            {
                return;
            }

            bool consumed = GameSaving.Load(ResolveSaveKey(), GetSaveFileName(), false);
            if (consumed)
            {
                hasConsumed = true;
                if (autoDisableOnConsumed)
                {
                    enabled = false;
                }
            }
        }

        private bool EvaluateConditions(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            foreach (var condition in radioConditions)
            {
                if (!condition.IsSatisfied())
                {
                    if (debugLogs)
                    {
                        Debug.Log($"[InteractiveZone] Radio condition failed for key: {condition.Key}", this);
                    }
                    return false;
                }
            }

            foreach (var scriptable in scriptableConditions)
            {
                if (scriptable == null)
                {
                    continue;
                }

                if (!scriptable.IsConditionMet(this, interactor))
                {
                    return false;
                }
            }

            return true;
        }

        private void TryShowPrompt()
        {
            if (promptVisible)
            {
                return;
            }

            if (activeEye == null)
            {
                return;
            }

            if (!AcceptsEye(activeEye))
            {
                return;
            }

            // Don't show prompt if on cooldown or awaiting re-entry
            if (IsOnCooldown || awaitingReEntry)
            {
                return;
            }

            UpdatePromptText();

            switch (promptDisplayMode)
            {
                case PromptDisplayMode.LocalUIView:
                    promptView?.Show();
                    break;
                case PromptDisplayMode.UIViewByName:
                    if (promptViewName.HasValue())
                    {
                        SIGS.UIViewControl(promptViewName, true);
                    }
                    break;
            }

            promptVisible = true;
        }

        private void HidePrompt()
        {
            if (!promptVisible)
            {
                return;
            }

            switch (promptDisplayMode)
            {
                case PromptDisplayMode.LocalUIView:
                    promptView?.Hide();
                    break;
                case PromptDisplayMode.UIViewByName:
                    if (promptViewName.HasValue())
                    {
                        SIGS.UIViewControl(promptViewName, false);
                    }
                    break;
            }

            promptVisible = false;
        }

        private void UpdatePromptText()
        {
            string formatted = BuildPromptMessage();
            
            if (promptDisplayMode == PromptDisplayMode.LocalUIView)
            {
                // Local mode: Set text directly on the referenced TMP_Text component
                if (promptText != null)
                {
                    // Use localization system - SetLocalizedText handles localization automatically
                    promptText.SetLocalizedText(formatted);
                }
            }
            else
            {
                // UIViewByName mode: Use DeadKey for text
                if (promptDeadKey.HasValue())
                {
                    SIGS.DeadKey(promptDeadKey, formatted, gameObject);
                }
            }
        }

        private string BuildPromptMessage()
        {
            return defaultPromptText.HasValue() ? defaultPromptText : "Interact";
        }

        private string ResolveSaveKey()
        {
            if (cachedSaveKey.HasValue())
            {
                return cachedSaveKey;
            }

            if (saveKeyOverride.HasValue())
            {
                cachedSaveKey = saveKeyOverride;
                return cachedSaveKey;
            }

            string scene = gameObject.scene.IsValid() ? gameObject.scene.path : "unspecified_scene";
            string hierarchyPath = GetHierarchyPath(transform);

            cachedSaveKey = $"cmn_interactive_{scene}_{hierarchyPath}";
            return cachedSaveKey;
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            int id = target.GetInstanceID();
            if (hierarchyPathCache.TryGetValue(id, out string cached))
            {
                return cached;
            }

            string path = target.name;
            Transform current = target.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            hierarchyPathCache[id] = path;
            return path;
        }

        private string GetSaveFileName()
        {
            return ConfigReader.GetConfig()?.CommonMechanics?.InteractiveZone?.SaveFileName ?? "cmn_interactions";
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Vector3 center = GetBoxCenter();

            // Draw box zone
            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = Matrix4x4.identity;

            // Draw look point
            Vector3 lookPos = GetLookPointPosition();
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(lookPos, 0.12f);

            // State hint
            Gizmos.color = hasConsumed ? new Color(1f, 0.2f, 0.2f, 0.9f) : (interactable ? new Color(0.2f, 1f, 0.2f, 0.9f) : new Color(1f, 0.6f, 0.1f, 0.9f));
            Gizmos.DrawWireSphere(transform.position, 0.08f);
        }

        [Serializable]
        public class RadioCondition
        {
            public enum ConditionType
            {
                LiveKeyExists,
                DeadKeyExists,
                LiveKeyBool,
                DeadKeyBool
            }

            [SerializeField] private ConditionType conditionType = ConditionType.LiveKeyExists;
            [SerializeField] private string key;
            [SerializeField] private bool expectedValue = true;

            public string Key => key;

            public bool IsSatisfied()
            {
                if (!key.HasValue())
                {
                    return true;
                }

                switch (conditionType)
                {
                    case ConditionType.LiveKeyExists:
                        return SIGS.LiveKeyExists(key);
                    case ConditionType.DeadKeyExists:
                        return SIGS.DeadKeyExists(key);
                    case ConditionType.LiveKeyBool:
                        return SIGS.GetLiveValue<bool>(key) == expectedValue;
                    case ConditionType.DeadKeyBool:
                        if (!SIGS.DeadKeyExists(key))
                        {
                            return !expectedValue;
                        }
                        return SIGS.GetDeadValue<bool>(key) == expectedValue;
                    default:
                        return true;
                }
            }
        }
    }

    public interface IInteractableCondition
    {
        bool IsConditionMet(InteractiveZone zone, GameObject interactor);
    }

    public abstract class InteractiveZoneConditionAsset : ScriptableObject, IInteractableCondition
    {
        public abstract bool IsConditionMet(InteractiveZone zone, GameObject interactor);
    }
}
