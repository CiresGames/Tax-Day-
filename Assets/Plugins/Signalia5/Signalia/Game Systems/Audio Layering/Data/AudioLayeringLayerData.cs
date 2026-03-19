using AHAKuo.Signalia.Radio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AudioLayering
{
    [CreateAssetMenu(fileName = "AudioLayeringLayerData", menuName = "Signalia/Game Systems/Audio Layering/Layer Data", order = 1)]
    /// <summary>
    /// Contains data for layers. Only sets the name of the layer body, and its mixer category.
    /// </summary>
    public class AudioLayeringLayerData : ScriptableObject
    {
        [SerializeField] private List<LayerData> layers = new();

        public List<(string id, MixerDefinition.MixerCategory category)> GetLayersForLoading() =>
            layers.ConvertAll(layer => layer.ToTuple());
    }
}
