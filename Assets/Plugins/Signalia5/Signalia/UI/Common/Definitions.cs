using AHAKuo.Signalia.Framework;
using DG.Tweening;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace AHAKuo.Signalia.UI
{
    public readonly struct AnimatedUIElement
    {
        public readonly GameObject gameObject;
        public readonly UIAnimationAsset animationAsset;

        public readonly string AnimatedFullId => (gameObject.GetInstanceID() + animationAsset.GetInstanceID()).ToString();
        public readonly string AnimatedShortId => animationAsset.GetInstanceID().ToString();
        public readonly string ObjectId => gameObject.GetInstanceID().ToString();


        public AnimatedUIElement(GameObject go, UIAnimationAsset anim)
        {
            gameObject = go;
            animationAsset = anim;
        }
    }

    /// <summary>
    /// Container for two strings. In particular, full animated ID and object animated ID.
    /// </summary>
    public readonly struct S2
    {
        /// <summary>
        /// Full animated ID.
        /// </summary>
        public readonly string s1;
        /// <summary>
        /// Object animated ID.
        /// </summary>
        public readonly string s2;
        public S2(string str1, string str2)
        {
            s1 = str1;
            s2 = str2;
        }
    }
}