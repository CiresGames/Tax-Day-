using UnityEngine;
using UnityEngine.Audio;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// A mixer definition that contains a category and an audio mixer group. Used to define the mixer group for a specific category. Can also control listener pause overrides.
    /// </summary>
    [System.Serializable]
    public class MixerDefinition
    {
        [SerializeField] private MixerCategory category;
        [SerializeField] private string volumeParameter;
        [SerializeField] private int defaultVolume;
        [SerializeField] private AudioMixerGroup mixer;
        [SerializeField] private bool ignoreListenerPause;
        public string VolumeParameter => volumeParameter;
        public MixerCategory Category => category;
        public AudioMixerGroup AudioMixerGroup => mixer;
        public bool IgnoreListenerPause => ignoreListenerPause;

        public bool Valid => mixer != null;

        public void ApplyVolumeToMixer(float v)
        {
            if (mixer != null)
            {
                mixer.audioMixer.SetFloat(volumeParameter, v);
            }
        }

        public void Save()
        {
            var vol = mixer.audioMixer.GetFloat(volumeParameter, out var currentValue) ? currentValue : defaultVolume;
            PlayerPrefs.SetFloat("AudioMixer_" + volumeParameter, vol);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            var loaded = PlayerPrefs.GetFloat("AudioMixer_" + volumeParameter, defaultVolume);
            mixer.audioMixer.SetFloat(volumeParameter, loaded);
        }

        public MixerDefinition(MixerCategory cat, AudioMixerGroup mix, bool ignore)
        {
            category = cat;
            mixer = mix;
            ignoreListenerPause = ignore;
        }

        /// <summary>
        /// A predetermined list of categories used to connect mixer groups to audio lists.
        /// </summary>
        public enum MixerCategory
        {
            Master,
            UI1,
            UI2,
            UI3,
            UI4,
            UI5,
            UI6,
            UI7,
            UI8,
            UI9,
            UI10,
            Game1,
            Game2,
            Game3,
            Game4,
            Game5,
            Game6,
            Game7,
            Game8,
            Game9,
            Game10
        }
    }

    /// <summary>
    /// Predefined haptic types with different feel characteristics.
    /// </summary>
    public enum HapticType
    {
        None,
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Error,
        Selection,
        Impact,
        Rigid,
        Soft
    }

    /// <summary>
    /// Haptic settings that can be applied to audio or used standalone.
    /// </summary>
    [System.Serializable]
    public class HapticSettings
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private HapticType hapticType = HapticType.Light;
        [SerializeField] private float intensity = 1f;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private bool overrideAudioSettings = false;

        public bool Enabled => enabled;
        public HapticType HapticType => hapticType;
        public float Intensity => intensity;
        public float Duration => duration;
        public bool OverrideAudioSettings => overrideAudioSettings;

        public HapticSettings()
        {
            enabled = true;
            hapticType = HapticType.Light;
            intensity = 1f;
            duration = 0.1f;
            overrideAudioSettings = false;
        }

        public HapticSettings(HapticType type, float intensity = 1f, float duration = 0.1f, bool enabled = true)
        {
            this.enabled = enabled;
            this.hapticType = type;
            this.intensity = intensity;
            this.duration = duration;
            this.overrideAudioSettings = false;
        }

        /// <summary>
        /// Creates a haptic settings instance with predefined values for common haptic types.
        /// </summary>
        public static HapticSettings CreatePreset(HapticType type)
        {
            return type switch
            {
                HapticType.Light => new HapticSettings(HapticType.Light, 0.3f, 0.05f),
                HapticType.Medium => new HapticSettings(HapticType.Medium, 0.6f, 0.1f),
                HapticType.Heavy => new HapticSettings(HapticType.Heavy, 1f, 0.2f),
                HapticType.Success => new HapticSettings(HapticType.Success, 0.8f, 0.15f),
                HapticType.Warning => new HapticSettings(HapticType.Warning, 0.7f, 0.12f),
                HapticType.Error => new HapticSettings(HapticType.Error, 1f, 0.25f),
                HapticType.Selection => new HapticSettings(HapticType.Selection, 0.4f, 0.08f),
                HapticType.Impact => new HapticSettings(HapticType.Impact, 0.9f, 0.18f),
                HapticType.Rigid => new HapticSettings(HapticType.Rigid, 0.8f, 0.1f),
                HapticType.Soft => new HapticSettings(HapticType.Soft, 0.3f, 0.15f),
                _ => new HapticSettings(HapticType.Light, 0.5f, 0.1f)
            };
        }
    }
}