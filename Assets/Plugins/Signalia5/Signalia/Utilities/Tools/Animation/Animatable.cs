using UnityEngine;

namespace Signalia.Utilities.Animation
{
    /// <summary>
    /// Component for animating GameObjects with standard Transform (not RectTransform).
    /// Similar to UIAnimatable but for scene objects. Includes pivot offset visualization in gizmos.
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Signalia | Animatable")]
    public class Animatable : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private AnimatableArrayable[] animationArray;

        public AnimatableArrayable[] AnimationArray => animationArray;

        private void Awake()
        {
            if (animationArray == null) return;

            foreach (var anim in animationArray)
            {
                anim.SetContext(gameObject);
                anim.CreateInstance();
            }
        }

        private void OnEnable()
        {
            if (animationArray == null) return;

            foreach (var anim in animationArray)
            {
                if (anim.PlayOnEnable)
                {
                    anim.AnimationPlay(this.gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (animationArray == null) return;

            foreach (var anim in animationArray)
            {
                anim.CancelAnimation();
            }
        }

        /// <summary>
        /// Plays the first animation in the array.
        /// </summary>
        public void PlayFirstAnimation()
        {
            if (animationArray != null && animationArray.Length > 0)
            {
                animationArray[0].AnimationPlay(this.gameObject);
            }
        }

        /// <summary>
        /// Plays an animation at the specified index.
        /// </summary>
        public void PlayAnimationFromIndex(int i)
        {
            if (animationArray != null && animationArray.Length - 1 >= i)
            {
                animationArray[i].AnimationPlay(this.gameObject);
            }
        }

        /// <summary>
        /// Plays an animation at the specified index instantly.
        /// </summary>
        public void PlayAnimationFromIndex_Instant(int i)
        {
            if (animationArray != null && animationArray.Length - 1 >= i)
            {
                animationArray[i].AnimationPlay(this.gameObject, null, true);
            }
        }

        /// <summary>
        /// Plays an animation by its label.
        /// </summary>
        public void PlayAnimation(string label)
        {
            if (animationArray == null) return;

            foreach (var an in animationArray)
            {
                if (an.Label == label)
                {
                    an.AnimationPlay(this.gameObject);
                    return;
                }
            }
        }

        /// <summary>
        /// Cancels an animation at the specified index.
        /// </summary>
        public void CancelAnimation(int i)
        {
            if (animationArray != null && animationArray.Length - 1 >= i)
            {
                animationArray[i].CancelAnimation();
            }
        }

        /// <summary>
        /// Cancels an animation by its label.
        /// </summary>
        public void CancelAnimation(string label)
        {
            if (animationArray == null) return;

            foreach (var an in animationArray)
            {
                if (an.Label == label)
                {
                    an.CancelAnimation();
                    return;
                }
            }
        }

        /// <summary>
        /// Cancels all animations.
        /// </summary>
        public void CancelAllAnimations()
        {
            if (animationArray == null) return;

            foreach (var an in animationArray)
            {
                an.CancelAnimation();
            }
        }

        /// <summary>
        /// Gets the animation target for a given index.
        /// </summary>
        public GameObject GetAnimatableTarget(int i)
        {
            if (animationArray != null && animationArray.Length - 1 >= i)
            {
                if (animationArray[i].DifferentTarget)
                    return animationArray[i].Target;
                return gameObject;
            }
            return null;
        }

        /// <summary>
        /// Gets the animation target for a given label.
        /// </summary>
        public GameObject GetAnimatableTarget(string label)
        {
            if (animationArray == null) return null;

            foreach (var an in animationArray)
            {
                if (an.Label == label)
                {
                    if (an.DifferentTarget)
                        return an.Target;
                    return gameObject;
                }
            }
            return null;
        }
    }
}
