using DG.Tweening;
using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// Used to simply animate a UI object that isn't a button or a view.
    /// </summary>
    [AddComponentMenu("Signalia/UI/Signalia | UI Animatable")]
    public class UIAnimatable : MonoBehaviour
    {
        [SerializeField] private UIAnimatableArrayable[] animationArray;
        [SerializeField] private bool dontBlockClicks = false;

        public UIAnimatableArrayable[] AnimationArray => animationArray;
        public bool DontBlockClicks => dontBlockClicks;

        private void Awake()
        {
            foreach (var anim in animationArray)
            {
                anim.SetContext(gameObject);
                anim.CreateInstance();
            }
        }

        private void OnEnable()
        {
            // go through all animations and play those that play on enable
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
            if(animationArray.Length > 0)
            {
                animationArray[0].AnimationPlay(this.gameObject);
            }
        }

        /// <summary>
        /// Plays the first animation in the array.
        /// </summary>
        public void PlayAnimationFromIndex(int i)
        {
            if (animationArray.Length - 1 >= i)
            {
                animationArray[i].AnimationPlay(this.gameObject);
            }
        }

        public void PlayAnimationFromIndex_Instant(int i)
        {
            if (animationArray.Length - 1 >= i)
            {
                animationArray[i].AnimationPlay(this.gameObject, null, true);
            }
        }

        public void PlayAnimationFromIndex_ForCascadeHide()
        {
            //second index
            if (animationArray.Length > 1)
            {
                animationArray[1].AnimationPlay(this.gameObject, null, true, true);
            }
        }

        public void PlayAnimation(string label)
        {
            foreach (var an in animationArray)
            {
                if (an.Label == label)
                {
                    an.AnimationPlay(this.gameObject);
                }
            }
        }

        public void CancelAnimation(int i)
        {
            if (animationArray.Length - 1 >= i)
            {
                animationArray[i].CancelAnimation();
            }
        }

        public void CancelAnimation(string label)
        {
            foreach (var an in animationArray)
            {
                if (an.Label == label)
                {
                    an.CancelAnimation();
                }
            }
        }

        public GameObject GetAnimatableTarget(int i)
        {
            if (animationArray.Length - 1 >= i)
            {
                if (animationArray[i].DifferentTarget)
                    return animationArray[i].Target;
                return gameObject;
            }
            return null;
        }

        public GameObject GetAnimatableTarget(string label)
        {
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