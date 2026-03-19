using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AudioLayering
{
    public static class AudioLayeringManager
    {
        private static bool inited = false;
        private static readonly List<Layer> _layers = new();

        /// <summary>
        /// Loads the audio layer data set in the config and initializes the audio layers.
        /// </summary>
        public static void InitializeWithResources()
        {
            Watchman.Watch();
            var layerData = ResourceHandler.LoadAudioLayeringLayerData();
            if (layerData == null)
            {
                Debug.LogError("AudioLayeringLayerData is null! Please make sure to set it in the ConfigAsset.");
                return;
            }
            foreach (var (id, category) in layerData.GetLayersForLoading())
            {
                _layers.Add(new(id, category));
            }
            inited = true;
        }

        public static Layer GetLayer(string id)
        {
            if (id.IsNullOrEmpty())
            {
                Debug.LogError("Layer ID is null or empty!");
                return null;
            }

            if (!inited)
            {
                InitializeWithResources();
            }

            return _layers.Find(layer => layer.Name == id);
        }

        /// <summary>
        /// Gets all initialized layers. Returns an empty list if not initialized.
        /// </summary>
        /// <returns></returns>
        public static List<Layer> GetAllLayers()
        {
            if (!inited)
            {
                InitializeWithResources();
            }
            return new List<Layer>(_layers);
        }

        /// <summary>
        /// Called by the Signalia Watchman on game end. Otherwise, do not call this method unless you know what you are doing.
        /// </summary>
        public static void Cleanse()
        {
            _layers.Clear();
            inited = false;
        }
    }
}
