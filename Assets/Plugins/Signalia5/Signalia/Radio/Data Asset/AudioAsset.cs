using AHAKuo.Signalia.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// An asset containing configurations of audio that can be attached to buttons and such. They all play through the manager.
    /// </summary>
    [CreateAssetMenu(menuName = "Signalia/UI and Audio/Audio Asset", fileName = "New Audio Asset")]
    public class AudioAsset : ScriptableObject
    {
        [SerializeField] private List<AudioEntry> audioEntries = new();
        [SerializeField, Tooltip("When enabled, Signalia will preload every clip in this asset during initialization so that they are immediately ready for playback.")]
        private bool preload = false;

        [Serializable]
        public class AudioEntry
        {
            public string key;
            public AudioData data;
        }

        [Serializable]
        public class AudioData
        {
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            public bool looping = false;
            public MixerDefinition.MixerCategory category;
            
            [Header("Haptic Settings")]
            public HapticSettings hapticSettings = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        }

        public bool EmptyStringError() => audioEntries.Any(s => string.IsNullOrEmpty(s.key));
        public bool DuplicateStringError() => audioEntries.GroupBy(x => x.key).Any(g => g.Count() > 1);
        public bool EmptyClipError() => audioEntries.Any(s => s.data.clips.Length == 0);
        public bool WrongKey() => audioEntries.GroupBy(x => x.key).Any(g => g.Key == FrameworkConstants.StringConstants.NOAUDIO);

        public bool Preload => preload;

        public string[] GetListKeys => audioEntries.Select(s => s.key).ToArray();
        public AudioData GetAudioData(string key)
        {
            if (key == FrameworkConstants.StringConstants.NOAUDIO)
            {
                Debug.LogError("Key is NOAUDIO. This is a reserved key for no audio. Please use a different key.");
                return null;
            }
            return audioEntries.FirstOrDefault(e => e.key == key)?.data;
        }

        public IEnumerable<AudioClip> GetAllClips()
        {
            foreach (var entry in audioEntries)
            {
                if (entry == null || entry.data == null || entry.data.clips == null)
                    continue;

                foreach (var clip in entry.data.clips)
                {
                    if (clip == null)
                        continue;

                    yield return clip;
                }
            }
        }
    }
}
