using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Utilities.SIGInput;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.CommonMechanics
{
    /// <summary>
    /// Scans for InteractiveZones using raycast box detection. When an eye is inside a zone's
    /// box bounds and on an allowed layer, it becomes the active target. Optionally requires
    /// the eye to be looking at the zone's look point.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Common Mechanics/Signalia | Interactor")]
    public class InteractorEye : MonoBehaviour
    {
        [SerializeField] private Transform eyeOrigin;
        [SerializeField] private Vector3 originOffset;
        [SerializeField, Min(0f)] private float scanInterval = 0.1f;

        [SerializeField] private bool requireLookAt = true;

        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColorIdle = new(0.2f, 0.8f, 1f, 0.9f);
        [SerializeField] private Color gizmoColorHit = new(0.2f, 1f, 0.2f, 0.95f);

        public bool RequireLookAt => requireLookAt;
        public InteractiveZone CurrentZone => currentZone;

        private float nextScanTime;
        private InteractiveZone currentZone;
        private Listener inputListener;

        private void OnEnable()
        {
            SetupRadioListenerIfNeeded();
        }

        private void OnDisable()
        {
            SetCurrentZone(null);
            StopListening();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (scanInterval <= 0f || Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + Mathf.Max(0f, scanInterval);
                Scan();
            }

            if (ShouldTriggerFromSignaliaInput())
            {
                TryInteract();
                return;
            }

            if (ShouldCheckLegacyInput())
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                if (UnityEngine.Input.GetKeyDown(GetLegacyKey()))
                {
                    TryInteract();
                }
#endif
            }
        }

        private void Scan()
        {
            InteractiveZone found = FindZoneInRange();
            if (found == currentZone)
            {
                return;
            }

            SetCurrentZone(found);
        }

        public Vector3 ResolveOrigin()
        {
            Transform originTransform = eyeOrigin != null ? eyeOrigin : transform;
            return originTransform.position + originTransform.TransformVector(originOffset);
        }

        public Vector3 ResolveForward()
        {
            Transform originTransform = eyeOrigin != null ? eyeOrigin : transform;
            return originTransform.forward;
        }

        private InteractiveZone FindZoneInRange()
        {
            Vector3 eyePos = ResolveOrigin();
            Vector3 forward = ResolveForward();
            int eyeLayer = gameObject.layer;

            float bestScore = float.MaxValue;
            InteractiveZone best = null;

            foreach (var zone in InteractiveZone.RegisteredZones)
            {
                if (zone == null)
                {
                    continue;
                }

                if (!zone.AcceptsEye(this))
                {
                    continue;
                }

                // Check layer mask
                if (!zone.IsLayerAllowed(eyeLayer))
                {
                    continue;
                }

                // Check if eye is inside the zone's box
                if (!zone.IsPointInZone(eyePos))
                {
                    continue;
                }

                // Optional look-at check
                if (requireLookAt)
                {
                    Vector3 lookPos = zone.GetLookPointPosition();
                    Vector3 toTarget = lookPos - eyePos;
                    float distance = toTarget.magnitude;

                    if (distance > Mathf.Epsilon)
                    {
                        float angle = Vector3.Angle(forward, toTarget);
                        if (zone.LookAngleThreshold > 0f && angle > zone.LookAngleThreshold)
                        {
                            continue;
                        }
                    }

                    if (zone.RequireLineOfSight && !zone.HasLineOfSight(eyePos, lookPos))
                    {
                        continue;
                    }

                    // Score: prefer smaller angle, then nearer distance
                    float angle2 = Vector3.Angle(forward, toTarget);
                    float score = (angle2 * 10f) + distance;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = zone;
                    }
                }
                else
                {
                    // Without look-at, prefer nearest zone center
                    float dist = Vector3.Distance(eyePos, zone.GetBoxCenter());
                    if (dist < bestScore)
                    {
                        bestScore = dist;
                        best = zone;
                    }
                }
            }

            return best;
        }

        private void SetCurrentZone(InteractiveZone next)
        {
            if (currentZone == next)
            {
                return;
            }

            if (currentZone != null)
            {
                currentZone.NotifyEyeExit(this);
            }

            currentZone = next;

            if (currentZone != null)
            {
                currentZone.NotifyEyeEnter(this);
            }
        }

        public void TryInteract()
        {
            currentZone?.TryInteract(this);
        }

        public void SetRequireLookAt(bool value)
        {
            requireLookAt = value;
        }

        private void SetupRadioListenerIfNeeded()
        {
            StopListening();

            var settings = ConfigReader.GetConfig()?.CommonMechanics?.InteractiveZone;
            if (settings == null)
            {
                return;
            }

            if (settings.InvokeType != InteractiveZoneInvokeType.SignaliaRadioEvent)
            {
                return;
            }

            string eventName = settings.InputEventName;
            if (!eventName.HasValue())
            {
                return;
            }

            inputListener = SIGS.Listener(eventName, TryInteract, false, gameObject);
        }

        private void StopListening()
        {
            inputListener?.Dispose();
            inputListener = null;
        }

        private bool ShouldTriggerFromSignaliaInput()
        {
            var settings = ConfigReader.GetConfig()?.CommonMechanics?.InteractiveZone;
            if (settings == null)
            {
                return false;
            }

            if (settings.InvokeType != InteractiveZoneInvokeType.SignaliaInputAction)
            {
                return false;
            }

            if (!SignaliaInputWrapper.Exists)
            {
                return false;
            }

            string actionName = settings.InputActionName;
            if (!actionName.HasValue())
            {
                return false;
            }

            if (settings.RequireActionEnabled && !SIGS.IsInputActionEnabled(actionName))
            {
                return false;
            }

            switch (settings.ActionTrigger)
            {
                case InteractiveZoneInputTriggerMode.Down:
                    return SIGS.GetInputDown(actionName, settings.OneFrameConsume);
                case InteractiveZoneInputTriggerMode.Held:
                    return SIGS.GetInput(actionName);
                case InteractiveZoneInputTriggerMode.Up:
                    return SIGS.GetInputUp(actionName, settings.OneFrameConsume);
                default:
                    return false;
            }
        }

        private bool ShouldCheckLegacyInput()
        {
            var settings = ConfigReader.GetConfig()?.CommonMechanics?.InteractiveZone;
            if (settings == null)
            {
                return false;
            }

            return settings.UseLegacyInputFallback && !SignaliaInputWrapper.Exists;
        }

        private KeyCode GetLegacyKey()
        {
            var settings = ConfigReader.GetConfig()?.CommonMechanics?.InteractiveZone;
            if (settings != null && settings.LegacyFallbackKey != KeyCode.None)
            {
                return settings.LegacyFallbackKey;
            }

            return KeyCode.E;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Vector3 origin = ResolveOrigin();
            Vector3 forward = ResolveForward();

            Gizmos.color = currentZone != null ? gizmoColorHit : gizmoColorIdle;

            // Draw eye origin
            Gizmos.DrawWireSphere(origin, 0.1f);

            // Draw forward direction
            Gizmos.DrawLine(origin, origin + forward * 1.5f);

            if (currentZone != null)
            {
                // Draw line to current zone's look point
                Gizmos.DrawLine(origin, currentZone.GetLookPointPosition());
                Gizmos.DrawWireSphere(currentZone.GetLookPointPosition(), 0.08f);
            }
        }
    }
}
