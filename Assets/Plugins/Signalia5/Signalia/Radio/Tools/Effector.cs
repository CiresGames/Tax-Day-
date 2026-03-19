using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace AHAKuo.Signalia.Radio
{
    [AddComponentMenu("Signalia/Tools/Signalia | Effector")]
    /// <summary>
    /// A class that handles UI effects and audio.
    /// </summary>
    public class Effector : InstancerSingleton<Effector>
    {
        [SerializeField] private AudioMixerAsset mixerAsset;

        [SerializeField] private List<AudioEmitter> emitters = new();

        private readonly Dictionary<string, HashSet<(AudioSource src, string id)>> keyToEmitter = new();

        [Serializable]
        public struct AudioEmitter
        {
            public GameObject gameObject;
            public AudioSource audioSource;

            public readonly bool Occupied => audioSource.isPlaying;
        }

        public static void AddMe()
        {
            var go = new GameObject("Signalia Effector");
            go.AddComponent<Effector>();
        }

        protected override void Awake()
        {
            base.Awake();

            // load mixer asset
            if (mixerAsset == null)
                mixerAsset = ResourceHandler.LoadAudioAsset();

            PrepareResources();
            PrepareAudioSources();
        }

        private void Start()
        {
            LoadMixerSettings();
        }

        private void PrepareResources() => ResourceHandler.WarmUpAudio();

        private void PrepareAudioSources()
        {
            if (mixerAsset == null)
            {
                Debug.LogWarning("No Audio Mixer Asset assigned to Effector. Follow the steps in the previous warning to create one.");
                return;
            }

            var mixerCount = mixerAsset.MixerCount;

            for (int i = 0; i < mixerCount; i++)
            {
                emitters.Add(NewEmitter());
            }
        }

        /// <summary>
        /// Play an audio, with an option to loop it.
        /// </summary>
        /// <param name="audioKey"></param>
        /// <param name="settings"></param>
        public void PlayAudio(string audioKey, params IAudioPlayingSettings[] settings)
        {
            if (audioKey.IsNullOrEmpty())
            {
                return;
            }

            var remembrance = settings.FirstOrDefault(x => x is IRememberMe);
            IRememberMe remembranceSetting = null;
            if (remembrance != null)
                remembranceSetting = remembrance as IRememberMe;

            // if remember me active, check if this source from this id is already playing, if yes, do not play again
            if (remembrance != null)
            {
                var id = (settings.First(x => x is IRememberMe) as IRememberMe).GetIdentifier();
                if (keyToEmitter.TryGetValue(audioKey, out var emitters) && emitters.Any(e => e.id == id && e.src.isPlaying))
                    return; // already playing from this id
            }

            var audio = ResourceHandler.GetAudio(audioKey);
            if (audio == null) { return; }
            var emitter = NextFreeEmitter();
            var mixer = mixerAsset.GetMixer(audio.category);
            if (mixer == null)
            {
                Debug.LogWarning("[Signalia Audio] No mixer was found for the category: " + audio.category);
                return;
            }

            // unparent the emitter in case it was parented to something else
            emitter.audioSource.transform.SetParent(null);

            emitter.audioSource.outputAudioMixerGroup = mixer.AudioMixerGroup;
            emitter.audioSource.clip = audio.clips.Random();
            emitter.audioSource.volume = audio.volume;
            emitter.audioSource.ignoreListenerPause = mixer.IgnoreListenerPause;
            emitter.audioSource.loop = audio.looping;

            // Track all audio (both looping and non-looping) for stopping capability
            if (!keyToEmitter.ContainsKey(audioKey))
                keyToEmitter[audioKey] = new HashSet<(AudioSource src, string id)>();
            keyToEmitter[audioKey].Add(new(emitter.audioSource, remembranceSetting != null ? remembranceSetting.GetIdentifier() : ""));

            var hasFilterSettings = settings != null && settings.Any(x => x is AudioFilters);
            if (!hasFilterSettings)
            {
                AudioFilterUtility.ResetFilters(emitter.audioSource);
            }

            // Apply all settings
            if (settings != null && settings.Length > 0)
            {
                foreach (var setting in settings)
                {
                    setting?.ApplyOnSource(emitter.audioSource);
                }
            }

            emitter.audioSource.Play();
        }

        /// <summary>
        /// Play an audio only if it is not already playing. By Key, not by ID. This is useful for a global sound effect that should not overlap. Not instanaced sounds. For those, pass RememberMe struct.
        /// </summary>
        /// <param name="audioKey"></param>
        /// <param name="settings"></param>
        public void PlayAudioIfNotPlaying(string audioKey, params IAudioPlayingSettings[] settings)
        {
            if (audioKey.IsNullOrEmpty())
            {
                return;
            }

            if (keyToEmitter.TryGetValue(audioKey, out var _emitters) && _emitters.Any(e => e.src.isPlaying))
            {
                return; // already playing
            }

            PlayAudio(audioKey, settings);
        }

        /// <summary>
        /// Stops the audio using the key.
        /// </summary>
        /// <param name="audioKey"></param>
        public void StopAudio(string audioKey, bool fadeOut = false, float fadeTime = 0.5f)
        {
            if (audioKey.IsNullOrEmpty())
            {
                return;
            }

            if (keyToEmitter.TryGetValue(audioKey, out var emitters))
            {
                if (fadeOut)
                {
                    foreach (var (src, id) in emitters)
                    {
                        var sets = new FadeOut(fadeTime, 0, false);
                        sets.ApplyOnSource(src);
                    }
                    SIGS.DoIn(fadeTime, () => keyToEmitter.Remove(audioKey), false);
                }
                else
                {
                    foreach (var emitter in emitters)
                    {
                        emitter.src.Stop();
                    }
                    keyToEmitter.Remove(audioKey);
                }
            }
        }

        private AudioEmitter NewEmitter()
        {
            var newObject = new GameObject("Audio Emitter");
            var emitter = new AudioEmitter()
            {
                gameObject = newObject,
                audioSource = newObject.AddComponent<AudioSource>()
            };
            emitter.audioSource.playOnAwake = false;
            emitter.audioSource.loop = false;
            return emitter;
        }

        /// <summary>
        /// Clean out mixers that have been destroyed.
        /// </summary>
        private void Cleanse()
        {
            for (int i = emitters.Count - 1; i >= 0; i--)
            {
                var emitter = emitters[i];
                if (emitter.gameObject == null)
                {
                    emitters.RemoveAt(i);
                }
            }

            for (int i = keyToEmitter.Count - 1; i >= 0; i--)
            {
                var kvp = keyToEmitter.ElementAt(i);
                var emitters = kvp.Value;

                // Remove null AudioSources from the HashSet
                emitters.RemoveWhere(emitter => emitter.src == null);

                // If HashSet is empty, remove the entire entry
                if (emitters.Count == 0)
                {
                    keyToEmitter.Remove(kvp.Key);
                }
            }
        }

        private AudioEmitter NextFreeEmitter()
        {
            Cleanse();

            var freeEmitters = emitters.Any(x => !x.Occupied);

            if (freeEmitters)
            {
                var emitter = emitters.FirstOrDefault(x => !x.Occupied);
                ResetAudioSourceSettings(emitter);
                return emitter;
            }
            else
            {
                var newEmitter = NewEmitter();
                emitters.Add(newEmitter);
                ResetAudioSourceSettings(newEmitter);
                return newEmitter;
            }
        }

        /// <summary>
        /// Update the mixer group by group and value. This is used to set the volume of the mixer group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="value"></param>
        public static void UpdateMixerGroup(AudioMixerGroup group, float value)
        {
            if (group == null)
            {
                Debug.LogError("AudioMixerGroup is null!");
                return;
            }

            // load the mixer asset and get the name of the parameter then set
            var mixer = ResourceHandler.LoadAudioAsset().AllMixers.FirstOrDefault(x => x.AudioMixerGroup == group);
            if (mixer == null)
            {
                Debug.LogError("AudioMixerGroup not found in the mixer asset!");
                return;
            }
            mixer.ApplyVolumeToMixer(value);
        }

        /// <summary>
        /// Save the mixer settings being used in Signalia config under Preferences + [file extension you set in the config]
        /// </summary>
        public static void SaveMixerSettings()
        {
            // get all mixers first
            var mixers = ResourceHandler.LoadAudioAsset().AllMixers.Where(v => v.VolumeParameter.HasValue()).ToArray();

            foreach (var item in mixers)
            {
                item.Save();
            }
        }

        /// <summary>
        /// Get the current volumes of all mixers in the asset. Recommend to call late, as loading sometimes can be reset to 0 if you call too early.
        /// </summary>
        /// <returns></returns>
        public static List<(AudioMixerGroup mixerGroup, float vol)> CurrentVolumes()
        {
            var mixers = ResourceHandler.LoadAudioAsset().AllMixers.Where(v => v.VolumeParameter.HasValue()).ToArray();

            var currentVolumes = new List<(AudioMixerGroup mixerGroup, float vol)>();

            foreach (var mixer in mixers)
            {
                var vol = mixer.AudioMixerGroup.audioMixer.GetFloat(mixer.VolumeParameter, out var cursrentValue) ? cursrentValue : 0f;
                var parameterName = mixer.VolumeParameter;
                var value = mixer.AudioMixerGroup.audioMixer.GetFloat(parameterName, out var currentValue) ? currentValue : 0f;
                currentVolumes.Add((mixer.AudioMixerGroup, value));
            }

            return currentVolumes;
        }

        /// <summary>
        /// Play audio at a specific 3D position with full 3D spatial audio settings.
        /// </summary>
        /// <param name="audioKey"></param>
        /// <param name="position"></param>
        /// <param name="settings"></param>
        public void PlayAudioAtPosition(string audioKey, Vector3 position, params IAudioPlayingSettings[] settings)
        {
            if (audioKey.IsNullOrEmpty())
            {
                return;
            }

            var remembrance = settings.FirstOrDefault(x => x is IRememberMe);
            IRememberMe remembranceSetting = null;
            if (remembrance != null)
                remembranceSetting = remembrance as IRememberMe;

            // if remember me active, check if this source from this id is already playing, if yes, do not play again
            if (remembrance != null)
            {
                var id = (settings.First(x => x is IRememberMe) as IRememberMe).GetIdentifier();
                if (keyToEmitter.TryGetValue(audioKey, out var emitters) && emitters.Any(e => e.id == id && e.src.isPlaying))
                    return; // already playing from this id
            }

            var audio = ResourceHandler.GetAudio(audioKey);
            if (audio == null) { return; }
            var emitter = NextFreeEmitter();
            var mixer = mixerAsset.GetMixer(audio.category);

            // unparent the emitter in case it was parented to something else
            emitter.audioSource.transform.SetParent(null);

            emitter.audioSource.outputAudioMixerGroup = mixer.AudioMixerGroup;
            emitter.audioSource.clip = audio.clips.Random();
            emitter.audioSource.volume = audio.volume;
            emitter.audioSource.ignoreListenerPause = mixer.IgnoreListenerPause;
            emitter.audioSource.loop = audio.looping;

            // Set 3D audio settings
            emitter.audioSource.spatialBlend = 1f; // Fully 3D
            emitter.audioSource.transform.position = position;

            // Track all audio (both looping and non-looping) for stopping capability
            if (!keyToEmitter.ContainsKey(audioKey))
                keyToEmitter[audioKey] = new HashSet<(AudioSource src, string id)>();
            keyToEmitter[audioKey].Add(new(emitter.audioSource, remembranceSetting != null ? remembranceSetting.GetIdentifier() : ""));

            var hasFilterSettings = settings != null && settings.Any(x => x is AudioFilters);
            if (!hasFilterSettings)
            {
                AudioFilterUtility.ResetFilters(emitter.audioSource);
            }

            // Apply all settings
            if (settings != null && settings.Length > 0)
            {
                foreach (var setting in settings)
                {
                    setting?.ApplyOnSource(emitter.audioSource);
                }
            }

            emitter.audioSource.Play();
        }

        /// <summary>
        /// Load the mixer settings from the saved file in the preferences. Already called by the Effector.
        /// </summary>
        public static void LoadMixerSettings()
        {
            // Check if audio mixer loading is disabled in the configuration
            var config = ConfigReader.GetConfig();
            if (config != null && config.DisableAudioMixerLoading)
            {
                return;
            }

            if (config.AudioMixerAsset == null)
            {
                Debug.LogWarning("[Signalia] There is no audio mixer asset defined in the config!");
                return;
            }

            // apply the saved settings to the mixers using the parameter name as key, prefixed with "AudioMixer_"
            var mixers = ResourceHandler.LoadAudioAsset().AllMixers.Where(v => v.VolumeParameter.HasValue());

            foreach (var item in mixers)
            {
                item.Load();
            }
        }

        private static void ResetAudioSourceSettings(AudioEmitter emitter)
        {
            emitter.audioSource.spatialBlend = 0f;
            emitter.audioSource.dopplerLevel = 1f;
            emitter.audioSource.spread = 0f;
            emitter.audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Auto;
            emitter.audioSource.minDistance = 1f;
            emitter.audioSource.maxDistance = 500f;
            emitter.audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            emitter.audioSource.panStereo = 0f;
            emitter.audioSource.pitch = 1f;
            emitter.audioSource.reverbZoneMix = 1f;
            emitter.audioSource.bypassEffects = false;
            emitter.audioSource.bypassReverbZones = false;
            emitter.audioSource.mute = false;
            emitter.audioSource.playOnAwake = false;
            emitter.audioSource.priority = 128;
        }
    }
}