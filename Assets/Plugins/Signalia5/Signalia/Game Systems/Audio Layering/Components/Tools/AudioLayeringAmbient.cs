using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AudioLayering
{
    [AddComponentMenu("Signalia/Game Systems/Audio Layering/Signalia | Ambient Audio")]
    /// <summary>
    /// Plays an audio clip in the background, typically used for ambient sounds or background music.
    /// </summary>
    public class AudioLayeringAmbient : MonoBehaviour
    {
        [SerializeField] private string layerName;
        [SerializeField] private string trackName;
        [SerializeField] private string audioName;
        [SerializeField] private int propertyOrder = 0; // Priority for the audio track, lower values are higher priority
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private bool useAudioFilters = false;
        [SerializeField] private AudioFilters audioFilters = new AudioFilters(true, 800f, 1f, false, 10f);

        public void PlayNow()
        {
            SIGS.AudioLayer(layerName).Track(trackName).Play(audioName, propertyOrder, useAudioFilters ? audioFilters : (AudioFilters?)null);
        }

        private void Start()
        {
            if (autoPlay)
            {
                PlayNow();
            }
        }
    }
}
