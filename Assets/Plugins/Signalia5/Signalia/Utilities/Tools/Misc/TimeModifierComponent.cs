using System.Collections;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    [AddComponentMenu("Signalia/Tools/Signalia | Time Modifier")]
    /// <summary>
    /// Helper component that modifies time scale for feedback events such as slow-downs and freeze frames.
    /// Uses SignaliaTime system for proper time management.
    /// </summary>
    public class TimeModifierComponent : MonoBehaviour
    {
        [Header("Default Templates")]
        [SerializeField, Tooltip("Time scale used for SetTime options.")]
        private float setTimeScale = 1f;
        [SerializeField, Tooltip("Time scale used for slow down options.")]
        private float slowDownScale = 0.5f;
        [SerializeField, Tooltip("Duration used for temporary SetTime/SlowDown actions.")]
        private float temporaryDuration = 0.5f;
        [SerializeField, Tooltip("Duration used for quick freeze frame actions.")]
        private float freezeFrameDuration = 0.08f;

        [Header("Behavior")]
        [SerializeField, Tooltip("Use unscaled time for temporary durations so they expire even when time is slowed.")]
        private bool useUnscaledTimeForTemporary = true;
        [SerializeField, Tooltip("Restore the time scale if this component is disabled or destroyed.")]
        private bool restoreOnDisable = true;

        // Modifier IDs for tracking
        private const string TEMPORARY_MODIFIER_ID_PREFIX = "TimeModifierComponent_Temporary_";
        private const string PERSISTENT_MODIFIER_ID_PREFIX = "TimeModifierComponent_Persistent_";
        
        private Coroutine activeRoutine;
        private string activeTemporaryModifierId;
        private string activePersistentModifierId;
        
        private int instanceId = -1;

        private int InstanceId
        {
            get
            {
                if (instanceId == -1)
                    instanceId = GetInstanceID();
                return instanceId;
            }
        }

        public void SetTimeScaleTemporary()
        {
            ApplyTemporary(setTimeScale, temporaryDuration, useUnscaledTimeForTemporary);
        }

        public void SetTimeScaleTemporary(float duration)
        {
            ApplyTemporary(setTimeScale, duration, useUnscaledTimeForTemporary);
        }

        public void SetTimeScalePersistent()
        {
            ApplyPersistent(setTimeScale);
        }

        public void SlowDownTemporary()
        {
            ApplyTemporary(slowDownScale, temporaryDuration, useUnscaledTimeForTemporary);
        }

        public void SlowDownTemporary(float duration)
        {
            ApplyTemporary(slowDownScale, duration, useUnscaledTimeForTemporary);
        }

        public void SlowDownPersistent()
        {
            ApplyPersistent(slowDownScale);
        }

        public void FreezeFrameQuick()
        {
            ApplyTemporary(0f, freezeFrameDuration, true);
        }

        public void FreezeFrame(float duration)
        {
            ApplyTemporary(0f, duration, true);
        }

        public void FreezeFrameOneFrame()
        {
            FreezeFrameFrames(1);
        }

        public void FreezeFrameFrames(int frameCount)
        {
            if (frameCount <= 0)
                return;

            StopActiveRoutine(false);
            string modifierId = GenerateTemporaryModifierId();
            activeTemporaryModifierId = modifierId;
            
            // Add freeze modifier
            SignaliaTime.SetModifier(new TimeModifier(modifierId, "Freeze Frame", 0f, gameObject.name));
            activeRoutine = StartCoroutine(FreezeFramesRoutine(frameCount, modifierId));
        }

        public void ApplyCustomScaleTemporary(float scale, float duration)
        {
            ApplyTemporary(scale, duration, useUnscaledTimeForTemporary);
        }

        public void ApplyCustomScalePersistent(float scale)
        {
            ApplyPersistent(scale);
        }

        public void ClearPersistent()
        {
            if (string.IsNullOrEmpty(activePersistentModifierId))
                return;

            StopActiveRoutine(false);
            SignaliaTime.RemoveModifier(activePersistentModifierId);
            activePersistentModifierId = null;
        }

        private void ApplyTemporary(float scale, float duration, bool useUnscaledTime)
        {
            StopActiveRoutine(false);
            string modifierId = GenerateTemporaryModifierId();
            activeTemporaryModifierId = modifierId;
            
            // Clamp scale to valid range (0-1 for SignaliaTime modifiers)
            float clampedScale = Mathf.Clamp01(scale);
            
            // Add temporary modifier
            SignaliaTime.SetModifier(new TimeModifier(modifierId, "Temporary Time Scale", clampedScale, gameObject.name));
            activeRoutine = StartCoroutine(RestoreAfter(duration, useUnscaledTime, modifierId));
        }

        private void ApplyPersistent(float scale)
        {
            // Remove existing persistent modifier if any
            if (!string.IsNullOrEmpty(activePersistentModifierId))
            {
                SignaliaTime.RemoveModifier(activePersistentModifierId);
            }
            
            StopActiveRoutine(false);
            string modifierId = GeneratePersistentModifierId();
            activePersistentModifierId = modifierId;
            
            // Clamp scale to valid range (0-1 for SignaliaTime modifiers)
            float clampedScale = Mathf.Clamp01(scale);
            
            // Add persistent modifier
            SignaliaTime.SetModifier(new TimeModifier(modifierId, "Persistent Time Scale", clampedScale, gameObject.name));
        }

        private string GenerateTemporaryModifierId()
        {
            return $"{TEMPORARY_MODIFIER_ID_PREFIX}{InstanceId}_{Time.frameCount}";
        }

        private string GeneratePersistentModifierId()
        {
            return $"{PERSISTENT_MODIFIER_ID_PREFIX}{InstanceId}";
        }

        private IEnumerator RestoreAfter(float duration, bool useUnscaledTime, string modifierId)
        {
            if (duration > 0f)
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(duration);
                else
                    yield return new WaitForSeconds(duration);
            }

            // Remove the temporary modifier
            if (!string.IsNullOrEmpty(modifierId))
            {
                SignaliaTime.RemoveModifier(modifierId);
            }
            
            activeTemporaryModifierId = null;
            activeRoutine = null;
        }

        private IEnumerator FreezeFramesRoutine(int frameCount, string modifierId)
        {
            for (int i = 0; i < frameCount; i++)
                yield return new WaitForEndOfFrame();

            // Remove the freeze modifier
            if (!string.IsNullOrEmpty(modifierId))
            {
                SignaliaTime.RemoveModifier(modifierId);
            }
            
            activeTemporaryModifierId = null;
            activeRoutine = null;
        }

        private void StopActiveRoutine(bool restoreScale)
        {
            if (activeRoutine == null)
                return;

            StopCoroutine(activeRoutine);
            activeRoutine = null;

            // Remove temporary modifier if it exists
            if (restoreScale && !string.IsNullOrEmpty(activeTemporaryModifierId))
            {
                SignaliaTime.RemoveModifier(activeTemporaryModifierId);
                activeTemporaryModifierId = null;
            }
        }

        private void OnDisable()
        {
            RestoreTimeIfNeeded();
        }

        private void OnDestroy()
        {
            RestoreTimeIfNeeded();
        }

        private void RestoreTimeIfNeeded()
        {
            if (!restoreOnDisable)
                return;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            // Remove persistent modifier
            if (!string.IsNullOrEmpty(activePersistentModifierId))
            {
                SignaliaTime.RemoveModifier(activePersistentModifierId);
                activePersistentModifierId = null;
            }

            // Remove temporary modifier
            if (!string.IsNullOrEmpty(activeTemporaryModifierId))
            {
                SignaliaTime.RemoveModifier(activeTemporaryModifierId);
                activeTemporaryModifierId = null;
            }
        }
    }
}
