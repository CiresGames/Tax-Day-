using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Framework.PackageHandlers;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.Radio
{
    [AddComponentMenu("Signalia/Tools/Signalia | VFXSFX")]
    /// <summary>
    /// Convenience component for triggering both VFX and SFX together.
    /// - SFX: Plays Signalia audio keys (with optional haptics) similarly to AudioPlayer.
    /// - VFX: Can spawn a pooled prefab, play particle systems, invoke a UnityEvent, and/or send events / show & hide menus like EventionBox.
    /// </summary>
    public class VFXSFX : MonoBehaviour
    {
        [Serializable]
        public class VFXSFXEntry
        {
            public string Label => label;
            public bool CreateListener => createListener;

            public enum EventTiming
            {
                Manual,
                Awake,
                Start,
                Enable,
                Disable,
                Destroy
            }

            [Header("Main")]
            [SerializeField] private string label;
            [SerializeField] private EventTiming timing = EventTiming.Manual;
            [SerializeField, Min(0f)] private float delay = 0f;
            [SerializeField] private bool useTimeScale = true;
            [SerializeField] private bool createListener = false;
            [SerializeField] private bool once = true;

            [Header("SFX")]
            [SerializeField] private List<AudioPlayer.AudioEntry> audioEntries = new();
            [SerializeField] private bool playAllAudioEntries = false;
            [SerializeField, Min(0)] private int audioIndex = 0;

            [Header("VFX")]
            [SerializeField] private GameObject pooledEffectPrefab;
            [SerializeField, Min(-1f)] private float pooledEffectLifetime = 2f;
            [SerializeField] private bool pooledEffectEnabled = true;
            [SerializeField] private bool pooledEffectParentToThis = false;
            [SerializeField] private Vector3 pooledEffectOffset = Vector3.zero;
            [SerializeField] private bool autoPlaySpawnedParticles = true;
            [SerializeField] private bool playSpawnedParticlesFromChildren = true;

            [SerializeField] private List<ParticleSystem> particleSystems = new();
            [SerializeField] private UnityEvent unityEvent;

            [Header("Evention (optional)")]
            [SerializeField] private string[] eventsToFire;
            [SerializeField] private string[] menusToShow;
            [SerializeField] private string[] menusToHide;

            [NonSerialized] private bool hasFired = false;
            [NonSerialized] private Tween pendingTween;

            internal void ResetRuntimeState()
            {
                hasFired = false;
                pendingTween?.Kill();
                pendingTween = null;
            }

            internal void FireEvent(EventTiming eventTiming, VFXSFX host, GameObject context = null)
            {
                if (host == null) return;
                if (once && hasFired) return;
                if (timing != eventTiming && eventTiming != EventTiming.Manual) return;

                pendingTween?.Kill();
                var ignoreTimeScale = !useTimeScale;
                var t = timing == EventTiming.Destroy ? 0f : delay;
                pendingTween = DOVirtual.DelayedCall(t, () => Execute(host, context), ignoreTimeScale);
                hasFired = true;
            }

            private void Execute(VFXSFX host, GameObject context = null)
            {
                // --- VFX: pooled effect ---
                if (pooledEffectPrefab != null)
                {
                    var spawned = pooledEffectPrefab.FromPool(pooledEffectLifetime, pooledEffectEnabled);
                    if (spawned != null)
                    {
                        spawned.transform.position = host.transform.position + pooledEffectOffset;

                        if (pooledEffectParentToThis)
                        {
                            spawned.transform.SetParent(host.transform);
                        }

                        if (autoPlaySpawnedParticles)
                        {
                            if (playSpawnedParticlesFromChildren)
                            {
                                foreach (var ps in spawned.GetComponentsInChildren<ParticleSystem>(true))
                                {
                                    ps?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                    ps?.Play(true);
                                }
                            }
                            else
                            {
                                var ps = spawned.GetComponent<ParticleSystem>();
                                if (ps != null)
                                {
                                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                    ps.Play(true);
                                }
                            }
                        }
                    }
                }

                // --- VFX: direct particle systems ---
                if (particleSystems != null && particleSystems.Count > 0)
                {
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Play(true);
                    }
                }

                // --- Evention-style behavior ---
                unityEvent?.Invoke();
                eventsToFire?.ToList().ForEach(e => SimpleRadio.SendEventByContext(e, context));
                menusToShow?.ToList().ForEach(m => m.ShowMenu());
                menusToHide?.ToList().ForEach(m => m.HideMenu());

                // --- SFX ---
                if (audioEntries == null || audioEntries.Count == 0) return;

                if (playAllAudioEntries)
                {
                    foreach (var entry in audioEntries)
                    {
                        host.PlayAudioEntry(entry);
                    }
                    return;
                }

                var idx = Mathf.Clamp(audioIndex, 0, audioEntries.Count - 1);
                host.PlayAudioEntry(audioEntries[idx]);
            }
        }

        [SerializeField] private List<VFXSFXEntry> entries = new();

        [Header("Shared Audio Settings")]
        [SerializeField] private bool rememberMe = false;
        [SerializeField] private bool use3DAudio = false;
        [SerializeField] private bool parented = false;
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 500f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float dopplerLevel = 1f;
        [SerializeField] private float spread = 0f;
        [SerializeField] private AudioVelocityUpdateMode velocityUpdateMode = AudioVelocityUpdateMode.Auto;

        [SerializeField] private bool useAudioFilters = false;
        [SerializeField] private AudioFilters audioFilters = new AudioFilters(false, 5000f, 1f, false, 10f);

        private void Awake()
        {
            FireByTiming(VFXSFXEntry.EventTiming.Awake);

            foreach (var e in entries.Where(x => x.CreateListener))
            {
                if (e.Label.IsNullOrEmpty()) continue;
                var listenerName = $"VFXSFX#_{e.Label}";
                listenerName.InitializeListener(() => e.FireEvent(VFXSFXEntry.EventTiming.Manual, this, gameObject));
            }
        }

        private void Start() => FireByTiming(VFXSFXEntry.EventTiming.Start);
        private void OnEnable() => FireByTiming(VFXSFXEntry.EventTiming.Enable);
        private void OnDisable() => FireByTiming(VFXSFXEntry.EventTiming.Disable);
        private void OnDestroy() => FireByTiming(VFXSFXEntry.EventTiming.Destroy);

        private void FireByTiming(VFXSFXEntry.EventTiming timing)
        {
            foreach (var e in entries)
            {
                e.FireEvent(timing, this, gameObject);
            }
        }

        public void FireManuallyByIndex(int index)
        {
            if (index >= 0 && index < entries.Count)
            {
                entries[index].FireEvent(VFXSFXEntry.EventTiming.Manual, this, gameObject);
            }
        }

        public void FireManuallyByName(string name)
        {
            foreach (var e in entries.Where(x => x.Label == name))
            {
                e.FireEvent(VFXSFXEntry.EventTiming.Manual, this, gameObject);
            }
        }

        public void ResetEntryRuntimeState()
        {
            foreach (var e in entries) e.ResetRuntimeState();
        }

        private void PlayAudioEntry(AudioPlayer.AudioEntry entry)
        {
            if (entry == null) return;
            if (entry.AudioKey.IsNullOrEmpty() || entry.AudioKey == FrameworkConstants.StringConstants.NOAUDIO) return;

            var settings = new List<IAudioPlayingSettings>
            {
                use3DAudio ? Get3DSettings() : Get2DSettings(),
            };

            if (rememberMe)
            {
                settings.Add(new Remembrance(GetInstanceID().ToString()));
            }

            if (parented)
            {
                settings.Add(new ParentedEmitter(transform, offset));
            }

            if (useAudioFilters)
            {
                settings.Add(audioFilters);
            }

            // Play audio
            entry.AudioKey.PlayAudio(settings.ToArray());

            // Trigger haptics if enabled (AudioPlayer-style: entry overrides asset when enabled & configured)
            if (entry.EnableHaptics)
            {
                var audioAssetData = ResourceHandler.GetAudio(entry.AudioKey);
                var assetHapticSettings = audioAssetData?.hapticSettings;

                HapticSettings finalHapticSettings = null;

                if (entry.HapticSettings != null && entry.HapticSettings.Enabled)
                {
                    finalHapticSettings = entry.HapticSettings;
                }
                else if (assetHapticSettings != null && assetHapticSettings.Enabled)
                {
                    finalHapticSettings = assetHapticSettings;
                }

                if (finalHapticSettings != null)
                {
                    HapticsManager.TriggerHaptic(finalHapticSettings);
                }
            }
        }

        private Audio3D Get3DSettings()
        {
            return new Audio3D(
                transform.position,
                minDistance,
                maxDistance,
                rolloffMode,
                spatialBlend,
                dopplerLevel,
                spread,
                velocityUpdateMode
            );
        }

        private Audio3D Get2DSettings()
        {
            return new Audio3D(
                transform.position,
                minDistance,
                maxDistance,
                rolloffMode,
                0f,
                dopplerLevel,
                spread,
                velocityUpdateMode
            );
        }

        private void OnDrawGizmosSelected()
        {
            if (!use3DAudio) return;
            Gizmos.color = SSColors.Cyan;
            Gizmos.DrawWireSphere(transform.position, minDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}
