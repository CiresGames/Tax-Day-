using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    [AddComponentMenu("Signalia/Tools/Signalia | Audio Player")]
    /// <summary>
    /// A method to play audio through Signalia.
    /// </summary>
    public class AudioPlayer : MonoBehaviour
    {
        /// <summary>
        /// Serializable class for audio entries in AudioPlayer.
        /// </summary>
        [System.Serializable]
        public class AudioEntry
        {
            [SerializeField] private string audioKey = FrameworkConstants.StringConstants.NOAUDIO;
            [SerializeField] private HapticSettings hapticSettings = new();
            [SerializeField] private bool enableHaptics = true;

            /// <summary>
            /// The audio key for this entry.
            /// </summary>
            public string AudioKey => audioKey;

            /// <summary>
            /// The haptic settings for this audio entry.
            /// </summary>
            public HapticSettings HapticSettings => hapticSettings;

            /// <summary>
            /// Whether haptics are enabled for this entry.
            /// </summary>
            public bool EnableHaptics => enableHaptics;

            /// <summary>
            /// Implicit conversion to string for easy usage.
            /// </summary>
            public static implicit operator string(AudioEntry entry) => entry?.audioKey ?? FrameworkConstants.StringConstants.NOAUDIO;

            public AudioEntry(string key) => this.audioKey = key;
        }

        [SerializeField] private bool legacyMode = false;
        [SerializeField] private List<string> audioLists = new();
        [SerializeField] private List<AudioEntry> audioEntries = new();

        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool playOnEnableAll = false;
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

        /// <summary>
        /// Public property to access the audio lists for editor scripts.
        /// </summary>
        public List<AudioEntry> AudioLists => audioEntries;

        private void OnEnable()
        {
            if (playOnEnable)
            {
                if (playOnEnableAll)
                {
                    PlayAll();
                }
                else
                {
                    PlayFirstAudioIndex();
                }
            }
        }

        public void PlayFirstAudioIndex()
        {
            PlayAudio(0);
        }

        public void PlayAudio(int i)
        {
            var settings = new List<IAudioPlayingSettings>();

            if (use3DAudio)
                settings.Add(Get3DSettings());
            else
                settings.Add(Get2DSettings());

            if (rememberMe)
                settings.Add(new Remembrance(GetInstanceID().ToString()));

            if (parented)
                settings.Add(new ParentedEmitter(transform, offset));

            if (useAudioFilters)
                settings.Add(audioFilters);

            if (legacyMode)
            {
                if (audioLists.Count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }

                audioLists[i].PlayAudio(settings.ToArray());
            }
            else
            {
                if (audioEntries.Count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }

                PlayAudioWithHaptics(i, settings.ToArray());
            }
        }

        public void PlayAudioWithGentleFadeIn(int i)
        {
            var settings = new List<IAudioPlayingSettings>();

            if (use3DAudio)
                settings.Add(Get3DSettings());
            else
                settings.Add(Get2DSettings());

            if (rememberMe)
                settings.Add(new Remembrance(GetInstanceID().ToString()));

            if (parented)
                settings.Add(new ParentedEmitter(transform, offset));

            if (useAudioFilters)
                settings.Add(audioFilters);

            settings.Add(new FadeIn(0.5f, true, 0, false));

            if (legacyMode)
            {
                if (audioLists.Count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }

                audioLists[i].PlayAudio(settings.ToArray());
            }
            else
            {
                if (audioEntries.Count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }

                audioEntries[i].AudioKey.PlayAudio(settings.ToArray());
            }
        }

        public void StopAudio(int i)
        {
            if (legacyMode)
            {
                if (audioLists.Count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }
                audioLists[i].StopAudio();
            }
            else
            {
                if (audioEntries.Count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }
                audioEntries[i].AudioKey.StopAudio();
            }
        }

        public void StopAudioWithGentleFadeOut(int i)
        {
            if (legacyMode)
            {
                if (audioLists.Count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }

                audioLists[i].StopAudio(true);
            }
            else
            {
                if (audioEntries.Count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }

                audioEntries[i].AudioKey.StopAudio(true);
            }
        }

        /// <summary>
        /// Gets the 3D audio settings based on the component's configuration.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the 2D audio settings to ensure spatial blend is reset to 0.
        /// </summary>
        /// <returns></returns>
        private Audio3D Get2DSettings()
        {
            return new Audio3D(
                transform.position,
                minDistance,
                maxDistance,
                rolloffMode,
                0f, // spatialBlend = 0 for 2D audio
                dopplerLevel,
                spread,
                velocityUpdateMode
            );
        }

        public void StopWithFadeout(int i, float fadeOutTime)
        {
            if (legacyMode)
            {
                if (audioLists.Count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }

                audioLists[i].StopAudio(true, fadeOutTime);
            }
            else
            {
                if (audioEntries.Count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }

                audioEntries[i].AudioKey.StopAudio(true, fadeOutTime);
            }
        }

        /// <summary>
        /// Plays all audio entries simultaneously. Useful for stacking audios that sound good together.
        /// </summary>
        public void PlayAll()
        {
            // call PlayAudio() for each audio entry

            if (legacyMode)
            {
                var count = audioLists.Count;

                if (count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    PlayAudio(i);
                }
            }
            else
            {
                var count = audioEntries.Count;

                if (count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    PlayAudio(i);
                }
            }
        }

        /// <summary>
        /// Plays all audio entries simultaneously with fade in. Useful for stacking audios that sound good together.
        /// </summary>
        /// <param name="fadeInTime">Time in seconds for the fade in effect</param>
        public void PlayAllWithGentleFadeIn(float fadeInTime = 0.5f)
        {
            if (legacyMode)
            {
                var count = audioLists.Count;
                if (count == 0)
                {
                    Debug.LogError("No audio lists found on AudioPlayer component.");
                    return;
                }
                for (int i = 0; i < count; i++)
                {
                    PlayAudioWithGentleFadeIn(i);
                }
            }
            else
            {
                var count = audioEntries.Count;
                if (count == 0)
                {
                    Debug.LogError("No audio entries found on AudioPlayer component.");
                    return;
                }
                for (int i = 0; i < count; i++)
                {
                    PlayAudioWithGentleFadeIn(i);
                }
            }
        }

        /// <summary>
        /// Plays audio with haptic feedback. Uses AudioPlayer's global haptic settings or AudioAsset's haptic settings.
        /// </summary>
        /// <param name="index">Index of the audio to play</param>
        /// <param name="settings">Additional audio settings</param>
        private void PlayAudioWithHaptics(int index, params IAudioPlayingSettings[] settings)
        {
            // Play the audio
            audioEntries[index].AudioKey.PlayAudio(settings);

            // Trigger haptics if enabled
            if (index >= 0 && index < audioEntries.Count)
            {
                var audioEntry = audioEntries[index];

                // Get haptic settings from AudioAsset first
                var audioAssetData = ResourceHandler.GetAudio(audioEntry.AudioKey);
                var assetHapticSettings = audioAssetData?.hapticSettings;

                // Use AudioPlayer's haptic settings if enabled, otherwise use AudioAsset settings
                HapticSettings finalHapticSettings = null;

                if (audioEntry.EnableHaptics && audioEntry.HapticSettings.Enabled)
                {
                    // AudioPlayer settings override AudioAsset settings
                    finalHapticSettings = audioEntry.HapticSettings;
                }
                else if (assetHapticSettings != null && assetHapticSettings.Enabled)
                {
                    // Use AudioAsset settings if AudioPlayer haptics are disabled
                    finalHapticSettings = assetHapticSettings;
                }

                // Trigger haptics if we have valid settings
                if (finalHapticSettings != null)
                {
                    HapticsManager.TriggerHaptic(finalHapticSettings);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // show the minimum and maximum distance as a sphere
            Gizmos.color = SSColors.Cyan;
            Gizmos.DrawWireSphere(transform.position, minDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
    }
}