using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// The fuller and more systematized version of the Radio class. Uses an almost TV channel-like system. More controllable and structured than the SimpleRadio.
    /// </summary>
    public static class ComplexRadio
    {
        public static Dictionary<string, ResonanceChannel> channels = new();

        // Listener tracking for debugging and system vitals
        private static readonly Dictionary<string, ListenerInfo> trackedComplexListeners = new();
        private static readonly Dictionary<string, List<string>> complexListenersByChannel = new();

        /// <summary>
        /// Cleans up all channels.
        /// </summary>
        public static void CleanUp()
        {
            foreach (var channel in channels)
            {
                channel.Value.CleanseContents(); // precaution? not sure if necessary
            }

            channels.Clear();
        }

        public static bool IsChannelLive(string channelName)
        {
            return channels.ContainsKey(channelName);
        }

        /// <summary>
        /// Removes a channel.
        /// </summary>
        /// <param name="channelName"></param>
        public static void KillChannel(string channelName)
        {
            if (!IsChannelLive(channelName)) return;

            // Debug logging
            RuntimeValues.Debugging.LogChannelDisposal(null, channelName);

            // cleanse
            channels[channelName].CleanseContents();
            channels.Remove(channelName);
        }

        private static void CreateChannel(string channelName)
        {
            Watchman.Watch();

            if (IsChannelLive(channelName)) return;

            channels.Add(channelName, new ResonanceChannel(channelName));

            // Debug logging
            RuntimeValues.Debugging.LogChannelCreation(null, channelName);
        }

        /// <summary>
        /// Get a channel and perform operations on it. If it doesn't exist, create it.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public static ResonanceChannel Channel(string channelName)
        {
            CreateChannel(channelName);
            return channels[channelName];
        }

        /// <summary>
        /// Add static content to a channel. If the channel doesn't exist, create it.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="contentKey"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static ResonanceChannel AddContent(string channelName, string contentKey, Func<object> content)
        {
            return Channel(channelName).AddContent(contentKey, content);
        }

        /// <summary>
        /// Add non-static content to a channel. If the channel doesn't exist, create it.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="contentKey"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static ResonanceChannel AddNonStaticContent(string channelName, string contentKey, object content)
        {
            return Channel(channelName).AddNonStaticContent(contentKey, content);
        }

        /// <summary>
        /// Get static content from a channel.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelName"></param>
        /// <param name="contentKey"></param>
        /// <returns></returns>
        public static T GetContent<T>(string channelName, string contentKey)
        {
            if (IsChannelLive(channelName))
            {
                return channels[channelName].GetContent<T>(contentKey);
            }
            return default;
        }

        /// <summary>
        /// Get non-static content from a channel.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelName"></param>
        /// <param name="contentKey"></param>
        /// <returns></returns>
        public static T GetNonStaticContent<T>(string channelName, string contentKey)
        {
            if (IsChannelLive(channelName))
            {
                return channels[channelName].GetNonStaticContent<T>(contentKey);
            }
            return default;
        }

        /// <summary>
        /// Play an audio list by name.
        /// </summary>
        /// <param name="audioName"></param>
        /// <param name="settings"></param>
        public static void PlayAudio(string audioName, params IAudioPlayingSettings[] settings)
        {
            if (Effector.Instance == null)
            {
                SIGS.DoWhen(() => Effector.Instance != null, () => Effector.Instance.PlayAudio(audioName, settings));
            }
            else
            {
                Effector.Instance.PlayAudio(audioName, settings);
            }
            return;
        }

        /// <summary>
        /// Simply plays an audio if it is not already playing. If the Effector is not available, it will wait until it is available.
        /// </summary>
        /// <param name="audioName"></param>
        /// <param name="settings"></param>
        public static void PlayAudioIfNotPlaying(string audioName, params IAudioPlayingSettings[] settings)
        {
            if (Effector.Instance == null)
            {
                SIGS.DoWhen(() => Effector.Instance != null, () => Effector.Instance.PlayAudioIfNotPlaying(audioName, settings));
            }
            else
            {
                Effector.Instance.PlayAudioIfNotPlaying(audioName, settings);
            }
        }

        /// <summary>
        /// Play audio at a specific 3D position with full 3D spatial audio settings.
        /// </summary>
        /// <param name="audioName"></param>
        /// <param name="position"></param>
        /// <param name="settings"></param>
        public static void PlayAudioAtPosition(string audioName, Vector3 position, params IAudioPlayingSettings[] settings)
        {
            if (Effector.Instance == null)
            {
                SIGS.DoWhen(() => Effector.Instance != null, () => Effector.Instance.PlayAudioAtPosition(audioName, position, settings));
            }
            else
            {
                Effector.Instance.PlayAudioAtPosition(audioName, position, settings);
            }
        }

        public static void StopAudio(string audioName, bool fadeOut, float fadeTime)
        {
            if (Effector.Instance == null)
            {
                SIGS.DoWhen(() => Effector.Instance != null, () => Effector.Instance.StopAudio(audioName, fadeOut, fadeTime));
            }
            else
            {
                Effector.Instance.StopAudio(audioName, fadeOut, fadeTime);
            }
        }

        /// <summary>
        /// Register a complex radio listener for tracking purposes
        /// </summary>
        internal static void RegisterComplexListener(ListenerInfo info)
        {
            trackedComplexListeners[info.uniqueId] = info;

            if (!complexListenersByChannel.ContainsKey(info.eventName))
                complexListenersByChannel[info.eventName] = new List<string>();

            complexListenersByChannel[info.eventName].Add(info.uniqueId);
        }

        /// <summary>
        /// Unregister a complex radio listener from tracking
        /// </summary>
        internal static void UnregisterComplexListener(string uniqueId)
        {
            if (trackedComplexListeners.TryGetValue(uniqueId, out var info))
            {
                trackedComplexListeners.Remove(uniqueId);

                if (complexListenersByChannel.ContainsKey(info.eventName))
                {
                    complexListenersByChannel[info.eventName].Remove(uniqueId);
                    if (complexListenersByChannel[info.eventName].Count == 0)
                        complexListenersByChannel.Remove(info.eventName);
                }
            }
        }

        /// <summary>
        /// Get all tracked complex listeners
        /// </summary>
        public static List<ListenerInfo> GetAllTrackedComplexListeners()
        {
            return new List<ListenerInfo>(trackedComplexListeners.Values);
        }

        /// <summary>
        /// Get listeners for a specific channel
        /// </summary>
        public static List<ListenerInfo> GetListenersForChannel(string channelName)
        {
            var result = new List<ListenerInfo>();
            if (complexListenersByChannel.ContainsKey(channelName))
            {
                foreach (var id in complexListenersByChannel[channelName])
                {
                    if (trackedComplexListeners.ContainsKey(id))
                        result.Add(trackedComplexListeners[id]);
                }
            }
            return result;
        }

        /// <summary>
        /// Get tracked complex listener count
        /// </summary>
        public static int TrackedComplexListenerCount()
        {
            return trackedComplexListeners.Count;
        }

        /// <summary>
        /// Clear all tracked complex listeners (for system reset)
        /// </summary>
        public static void ClearTrackedComplexListeners()
        {
            trackedComplexListeners.Clear();
            complexListenersByChannel.Clear();
        }

        /// <summary>
        /// Dispose a specific complex listener by its unique ID
        /// </summary>
        /// <param name="uniqueId">The unique ID of the listener to dispose</param>
        /// <returns>True if the listener was found and disposed, false otherwise</returns>
        public static bool DisposeComplexListener(string uniqueId)
        {
            if (trackedComplexListeners.TryGetValue(uniqueId, out var info))
            {
                // Dispose the actual listener object
                if (info.listenerObject is ResonanceListener listener)
                {
                    // Find the channel and remove the listener
                    if (channels.TryGetValue(info.eventName, out var channel))
                    {
                        channel.RemoveListener(listener);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}