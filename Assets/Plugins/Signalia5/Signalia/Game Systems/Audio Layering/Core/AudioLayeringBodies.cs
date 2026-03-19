using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using static AHAKuo.Signalia.Radio.MixerDefinition;

namespace AHAKuo.Signalia.GameSystems.AudioLayering
{
    /// <summary>
    /// A layer which is a collection of tracks that can be played one at a time.
    /// </summary>
    public class Layer
    {
        public string Name { get; private set; }
        private readonly MixerDefinition.MixerCategory mixerType;
        private List<Track> tracks;

        public Layer(string id, MixerDefinition.MixerCategory mixerType)
        {
            Name = id;
            this.mixerType = mixerType;
            tracks = new List<Track>();
            Init();
        }

        private void Init()
        {
            if (tracks == null || tracks.Count == 0)
                Track("default");
        }

        public Track Track(string nameOfTrack = "")
        {
            if (string.IsNullOrEmpty(nameOfTrack))
                nameOfTrack = "default";

            tracks ??= new List<Track>();

            var existing = tracks.Find(t => t.Name == nameOfTrack);
            if (existing != null) return existing;

            var nw = new Track(nameOfTrack, mixerType, Name);
            tracks.Add(nw);
            return nw;
        }

        public void StopLayer(float fadeoutDuration = 0.5f)
        {
            Watchman.Watch();
            if (tracks == null) return;

            foreach (var track in tracks)
                track.StopAllTracks(fadeoutDuration);
        }
    }

    /// <summary>
    /// Track = ordered sources + a single "pointer" to the highest requested order.
    /// Only the pointer source should be audible/playing (others fade out).
    /// </summary>
    public class Track
    {
        private const string SilentClipName = "[SIGS_AL_CONSTANT_SILENCE]";

        private readonly GameObject audioObject;
        public string Name { get; private set; }
        private readonly MixerDefinition mixer_def;
        private readonly string layerName;

        // propertyOrder -> source
        private readonly Dictionary<int, TrackAudioSource> sourcesByOrder = new();

        // which propertyOrder the pointer is currently on
        private int currentPointerOrder = 0;

        private float GetFadeDuration()
        {
            var config = ConfigReader.GetConfig();
            if (config == null)
            {
                Debug.LogError("ConfigAsset is null! Please make sure to make it first!");
                return 0;
            }
            return config.AudioLayering.FadeDuration;
        }

        private AudioSource CreateSource()
        {
            var newSource = audioObject.AddComponent<AudioSource>();
            newSource.outputAudioMixerGroup = mixer_def.AudioMixerGroup;
            newSource.playOnAwake = false;
            newSource.ignoreListenerPause = mixer_def.IgnoreListenerPause;
            newSource.volume = 0f; // Always start at 0 volume
            return newSource;
        }

        public Track(string name, MixerCategory mixerType, string layerName)
        {
            Name = name;
            this.layerName = layerName;

            audioObject = new GameObject($"AL_{layerName}_{name}");
            audioObject.DDOL();

            var mixerAsset = ResourceHandler.LoadAudioAsset();
            if (mixerAsset == null)
            {
                Debug.LogError("Attempted to use AudioLayering without a valid AudioMixerAsset. Please ensure it is set in the config.");
                return;
            }

            mixer_def = mixerAsset.GetMixer(mixerType);

            EnsureBaseSource();
            InitializeSentryMan();
            EvaluatePointerAndApply(force: true);
        }

        private void EnsureBaseSource()
        {
            if (sourcesByOrder.ContainsKey(0) && sourcesByOrder[0].Valid)
                return;

            var baseSrc = new TrackAudioSource(0, CreateSource(), "");
            baseSrc.Requested = true; // base is always requested
            // Ensure base source starts at 0 volume
            if (baseSrc.Valid)
                baseSrc.source.volume = 0f;
            sourcesByOrder[0] = baseSrc;
        }

        private void InitializeSentryMan()
        {
            SIGS.Listener($"AL_UPDATE_TRACK: {layerName}:{Name}", (s) => UpdateSources(s));
        }

        /// <summary>
        /// Radio callback: updates "requested" flags and clip config, then re-evaluates pointer.
        /// args = clipName(string), propertyOrder(int), isPlayCall(bool), filters?(AudioFilters?)
        /// </summary>
        private void UpdateSources(params object[] args)
        {
            if (args == null || args.Length < 3) return;

            var clipName = args[0] as string;
            var propertyOrder = (int)args[1];
            var isPlayCall = (bool)args[2];
            AudioFilters? filters = args.Length > 3 && args[3] != null ? (AudioFilters?)args[3] : null;

            EnsureBaseSource();

            if (isPlayCall)
            {
                var src = GetOrCreateSource(propertyOrder);
                src.Requested = true;

                // configure clip (silent or real) + filters
                if (clipName == SilentClipName)
                {
                    var silentClip = AudioClip.Create("SilentClip", 1, 1, 44100, false);
                    src.ConfigureClip(silentClip, SilentClipName, looping: true, volume: 1f);
                }
                else
                {
                    var clipData = ResourceHandler.GetAudio(clipName);
                    var audioClip = clipData?.clips.Random();
                    if (audioClip != null)
                        src.ConfigureClip(audioClip, clipName, clipData.looping, clipData.volume);
                    else
                        Debug.LogWarning($"AudioLayering: Audio clip {clipName} not found or empty.");
                }

                ApplyFilters(src, filters);
            }
            else
            {
                // stop call: unrequest matching order (or clipName fallback)
                if (sourcesByOrder.TryGetValue(propertyOrder, out var byOrder) && byOrder.Valid)
                {
                    byOrder.Requested = false;
                }
                else if (!string.IsNullOrEmpty(clipName))
                {
                    foreach (var s in sourcesByOrder.Values.Where(x => x.Valid && x.clipName == clipName))
                        s.Requested = false;
                }

                // base never becomes unrequested
                sourcesByOrder[0].Requested = true;
            }

            EvaluatePointerAndApply(force: false);
        }

        private TrackAudioSource GetOrCreateSource(int order)
        {
            if (sourcesByOrder.TryGetValue(order, out var existing) && existing.Valid)
                return existing;

            var nw = new TrackAudioSource(order, CreateSource(), "");
            sourcesByOrder[order] = nw;
            return nw;
        }

        private void EvaluatePointerAndApply(bool force)
        {
            var desiredPointer = sourcesByOrder.Values
                .Where(s => s.Valid && s.Requested)
                .OrderByDescending(s => s.propertyOrder)
                .FirstOrDefault();

            if (desiredPointer == null)
            {
                desiredPointer = sourcesByOrder[0];
                desiredPointer.Requested = true;
            }

            int desiredOrder = desiredPointer.propertyOrder;
            float fade = GetFadeDuration();

            if (force || desiredOrder != currentPointerOrder)
            {
                // fade out EVERYTHING else
                foreach (var s in sourcesByOrder.Values.Where(x => x.Valid && x.propertyOrder != desiredOrder))
                    s.FadeOutAndMaybeStop(fade);

                // fade in pointer
                desiredPointer.FadeInOrPlay(fade);

                currentPointerOrder = desiredOrder;
            }
            else
            {
                // pointer hasn't moved, but ensure it is actually playing/audible
                desiredPointer.FadeInOrPlay(fade);
            }
        }

        private void ApplyFilters(TrackAudioSource source, AudioFilters? filters)
        {
            if (!source.Valid) return;

            if (filters.HasValue) AudioFilterUtility.ApplyFilters(source.source, filters.Value);
            else AudioFilterUtility.ResetFilters(source.source);
        }

        public void PlaySilentTrack(int propertyOrder = 0, AudioFilters? filters = null)
        {
            Watchman.Watch();
            $"AL_UPDATE_TRACK: {layerName}:{Name}".SendEvent(SilentClipName, propertyOrder, true, filters);
        }

        public void Play(string clip, int propertyOrder = 0, AudioFilters? filters = null)
        {
            Watchman.Watch();

            var clipData = ResourceHandler.GetAudio(clip);
            if (clipData == null)
            {
                Debug.LogWarning($"AudioLayering: Audio clip {clip} not found.");
                return;
            }

            $"AL_UPDATE_TRACK: {layerName}:{Name}".SendEvent(clip, propertyOrder, true, filters);
        }

        public void Stop(string clip)
        {
            Watchman.Watch();

            // stop ALL sources that currently hold that clip
            var targets = sourcesByOrder.Values.Where(x => x.Valid && x.clipName == clip).ToList();
            if (targets.Count == 0) return;

            foreach (var src in targets)
                $"AL_UPDATE_TRACK: {layerName}:{Name}".SendEvent(clip, src.propertyOrder, false);
        }

        public void StopSilentTrack() => Stop(SilentClipName);

        public void StopAllTracks(float fadeoutDuration = 0.5f)
        {
            Watchman.Watch();

            foreach (var src in sourcesByOrder.Values.Where(x => x.Valid))
            {
                src.Requested = false;
                src.FadeOutAndMaybeStop(fadeoutDuration);
            }

            // return pointer to base - ensure it's silent (volume 0)
            EnsureBaseSource();
            var baseSrc = sourcesByOrder[0];
            baseSrc.Requested = true;
            currentPointerOrder = 0;
            
            // Base should be silent when all tracks are stopped
            if (baseSrc.Valid)
            {
                baseSrc.KillMovement();
                baseSrc.source.volume = 0f;
                var config = ConfigReader.GetConfig().AudioLayering;
                if (!config.ContinuousPlaying && baseSrc.source.isPlaying)
                    baseSrc.source.Stop();
            }
        }
    }

    [Serializable]
    public struct LayerData
    {
        public string id;
        public MixerDefinition.MixerCategory category;

        public LayerData(string id, MixerDefinition.MixerCategory category = MixerCategory.Master)
        {
            this.id = id;
            this.category = category;
        }

        public (string id, MixerDefinition.MixerCategory category) ToTuple() => (id, category);
    }

    /// <summary>
    /// Single audio source slot for a property order.
    /// "Requested" is the only flag Track uses to decide the pointer.
    /// </summary>
    public class TrackAudioSource
    {
        public int propertyOrder;
        public AudioSource source;
        public string clipName;

        private float clipVolumeFromAsset = 1f;
        private readonly List<Tween> tweens = new();

        public bool Requested { get; set; }
        public bool Valid => source != null;

        public TrackAudioSource(int propertyOrder, AudioSource source, string clipName)
        {
            this.propertyOrder = propertyOrder;
            this.source = source;
            this.clipName = clipName;
            Requested = propertyOrder == 0;
            // Always start at 0 volume
            if (source != null)
                source.volume = 0f;
        }

        public void ConfigureClip(AudioClip clip, string clipName, bool looping, float volume)
        {
            if (!Valid) return;

            // if clip changed, set it (even if already playing)
            if (source.clip != clip)
                source.clip = clip;

            this.clipName = clipName;
            clipVolumeFromAsset = volume;
            source.loop = looping;
        }

        public void FadeInOrPlay(float duration)
        {
            if (!Valid) return;

            KillMovement();

            // Ensure playing if needed
            var config = ConfigReader.GetConfig().AudioLayering;

            if (source.clip != null)
            {
                if (config.ContinuousPlaying)
                {
                    if (!source.isPlaying) source.Play();
                }
                else
                {
                    // in non-continuous, always play when pointer lands here
                    source.Play();
                }

                // fade to clip volume
                if (duration <= 0f) source.volume = clipVolumeFromAsset;
                else tweens.Add(source.DOFade(clipVolumeFromAsset, duration));
            }
        }

        public void FadeOutAndMaybeStop(float duration)
        {
            if (!Valid) return;

            KillMovement();

            var config = ConfigReader.GetConfig().AudioLayering;

            if (duration <= 0f)
            {
                source.volume = 0f;
                if (!config.ContinuousPlaying && source.isPlaying)
                    source.Stop();
                return;
            }

            tweens.Add(
                source.DOFade(0f, duration)
                      .OnComplete(() =>
                      {
                          if (!config.ContinuousPlaying && source.isPlaying)
                              source.Stop();
                      })
            );
        }

        public void KillMovement()
        {
            for (int i = 0; i < tweens.Count; i++)
                tweens[i]?.Kill();

            tweens.Clear();
        }

        public bool Moving() => tweens.Any(t => t.IsActive() && !t.IsComplete());
    }
}
