using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;
using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AudioLayering
{
    [AddComponentMenu("Signalia/Game Systems/Audio Layering/Signalia | Room Audio")]
    /// <summary>
    /// Used in combination with a collider zone to make this area play a specific audio.
    /// When mustBeInvoked is enabled, the room will not use trigger zones and must be manually controlled.
    /// </summary>
    public class AudioLayeringRoom : MonoBehaviour
    {
        [Serializable]
        public class RoomTracks
        {
            public string layerName;
            public string trackName;
            public string audioName;
            public int propertyOrder = 1; // rooms should usually have a property order of 1, so they can be played on top of the main track.
            public bool useSilentTrack = false; // When enabled, plays a silent track instead of the specified audio
            public bool useAudioFilters = false;
            public AudioFilters audioFilters = new AudioFilters(true, 800f, 1.2f, false, 10f);
        }

        [SerializeField] private LayerMask triggeringLayer;
        [SerializeField] private bool mustBeInvoked = false;
        [SerializeField] private string roomEnterEvent = "";
        [SerializeField] private RoomTracks[] roomTracks;

        /// <summary>
        /// Manually trigger the room audio to start playing
        /// </summary>
        public void EnterRoom()
        {
            // Send room enter event if configured
            if (!string.IsNullOrEmpty(roomEnterEvent))
            {
                roomEnterEvent.SendEvent(gameObject);
            }

            foreach (var item in roomTracks)
            {
                Play(item);
            }
        }

        /// <summary>
        /// Manually trigger the room audio to stop playing
        /// </summary>
        public void ExitRoom()
        {
            foreach (var item in roomTracks)
            {
                Stop(item);
            }
        }

        /// <summary>
        /// Manually trigger a specific room track to start playing
        /// </summary>
        /// <param name="trackIndex">Index of the track in the roomTracks array</param>
        public void EnterRoomTrack(int trackIndex)
        {
            if (trackIndex >= 0 && trackIndex < roomTracks.Length)
            {
                Play(roomTracks[trackIndex]);
            }
        }

        /// <summary>
        /// Manually trigger a specific room track to stop playing
        /// </summary>
        /// <param name="trackIndex">Index of the track in the roomTracks array</param>
        public void ExitRoomTrack(int trackIndex)
        {
            if (trackIndex >= 0 && trackIndex < roomTracks.Length)
            {
                Stop(roomTracks[trackIndex]);
            }
        }

        private void ProcessEnter(GameObject go)
        {
            if (mustBeInvoked) return; // Skip trigger processing if must be invoked
            if (go == null) return;
            if (!go.IsOnLayer(triggeringLayer)) return;

            // Send room enter event if configured
            if (!string.IsNullOrEmpty(roomEnterEvent))
            {
                roomEnterEvent.SendEvent(gameObject);
            }

            // Only process if this object wasn't already in the room
            foreach (var item in roomTracks)
            {
                Play(item);
            }
        }

        private void ProcessExit(GameObject go)
        {
            if (mustBeInvoked) return; // Skip trigger processing if must be invoked
            if (go == null) return;
            if (!go.IsOnLayer(triggeringLayer)) return;

            // Only process if this object was actually in the room
            foreach (var item in roomTracks)
            {
                Stop(item);
            }
        }

        private void Play(RoomTracks track)
        {
            var filters = track.useAudioFilters ? track.audioFilters : (AudioFilters?)null;
            if (track.useSilentTrack)
            {
                SIGS.AudioLayer(track.layerName).Track(track.trackName).PlaySilentTrack(track.propertyOrder, filters);
            }
            else
            {
                SIGS.AudioLayer(track.layerName).Track(track.trackName).Play(track.audioName, track.propertyOrder, filters);
            }
        }

        private void Stop(RoomTracks track)
        {
            if (track.useSilentTrack)
            {
                SIGS.AudioLayer(track.layerName).Track(track.trackName).StopSilentTrack();
                return;
            }
            SIGS.AudioLayer(track.layerName).Track(track.trackName).Stop(track.audioName);
        }

        #region Triggers
        private void OnTriggerEnter(Collider other) => ProcessEnter(other.gameObject);

        private void OnTriggerEnter2D(Collider2D collision) => ProcessEnter(collision.gameObject);

        private void OnTriggerExit(Collider other) => ProcessExit(other.gameObject);

        private void OnTriggerExit2D(Collider2D collision) => ProcessExit(collision.gameObject);
        #endregion
    }
}
