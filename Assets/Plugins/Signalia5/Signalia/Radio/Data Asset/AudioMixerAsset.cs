using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    [CreateAssetMenu(menuName = "Signalia/UI and Audio/Audio Mixer Asset", fileName = "New Audio Mixer Asset")]
    public class AudioMixerAsset : ScriptableObject
    {
        [SerializeField] private List<MixerDefinition> mixerDefinitions = new();

        public MixerDefinition GetMixer(MixerDefinition.MixerCategory category)
        {
            if (mixerDefinitions == null
                || mixerDefinitions.Count <= 0)
            {
                Debug.LogError("Not categories in the asset!");
                return null;
            }

            var mixer = mixerDefinitions.FirstOrDefault(x => x.Category == category);

            if (mixer == null
                || mixer.AudioMixerGroup == null)
            {
                Debug.LogError("Could not find a mixer using the category: " + category.ToString() + ". Or the mixer has no mixer defined.");
                return null;
            }

            return mixer;
        }

        public MixerDefinition[] AllMixers => mixerDefinitions
            .Where(x => x.Valid)
            .ToArray();

        public void InitializeDefaultAsset()
        {
            // master
            mixerDefinitions.Add(new(MixerDefinition.MixerCategory.Master, null, false));

            // ui1
            mixerDefinitions.Add(new(MixerDefinition.MixerCategory.UI1, null, false));

            // game1
            mixerDefinitions.Add(new(MixerDefinition.MixerCategory.Game1, null, false));
        }

        public int MixerCount => mixerDefinitions.Where(x => x.Valid).Count();

        public bool MixerHasDuplicateCategory =>
            mixerDefinitions.Count > 0 &&
            mixerDefinitions.GroupBy(x => x.Category).Any(g => g.Count() > 1);
    }
}