namespace AHAKuo.Signalia.GameSystems.AudioLayering
{
    /// <summary>
    /// A collection of utilities to add audio layering into the game.
    /// Audio Layering is a concept where there are layers that can be played simultaneously, where-in each layer contains tracks of different priority and only one track can be played at a time.
    /// Whenever a clip is played in a track, it will smoothly fade out the currently playing clip in that track and fade in the new clip.
    /// Each layer has a mixer setting tied with the audio mixer system used in Signalia, and also audio atunement for volume or pitch or looping adjustments per track.
    /// This enables powerful controls on audio.
    /// NOTE: This uses the Audio ecosystem from the framework, and is not a unique implementation of audio playing.
    /// </summary>
    public static class AudioLayering
    {
        /// <summary>
        /// Get the layer by its id. This layer is assigned in the data asset AudioLayeringLayerData, which is set in the ConfigAsset.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Layer Layer(string id) => AudioLayeringManager.GetLayer(id);

        /// <summary>
        /// Stops all running audio layers. Useful when wanting to transition between scenes and make sure no layers are running, as audiolayering works in DDOL and is persistent across scenes.
        /// </summary>
        /// <param name="fadeoutDuration">Duration of the fadeout in seconds. Default is 0.5 seconds.</param>
        public static void StopAllLayers(float fadeoutDuration = 0.5f)
        {
            var layers = AudioLayeringManager.GetAllLayers();
            foreach (var layer in layers)
            {
                layer?.StopLayer(fadeoutDuration);
            }
        }
    }
}
