using UnityEngine;
using UnityEngine.Events;
using System;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using DG.Tweening;

namespace Signalia.Utilities.Animation
{
    /// <summary>
    /// Holds animation settings for a single animation instance within an Animatable component.
    /// Similar to UIAnimatableArrayable but for GameObjects with standard Transform.
    /// </summary>
    [System.Serializable]
    public class AnimatableArrayable
    {
        [SerializeField] private string label;

        [SerializeField] private AnimationAsset animationAsset;
        public AnimationAsset AnimationAsset => animationAsset;

        [SerializeField] private bool playOnEnable;

        [SerializeField] private bool differentTarget = false;
        public bool DifferentTarget => differentTarget;

        [SerializeField] private GameObject target;
        public GameObject Target => target;

        private GameObject _gameObject;
        public bool PlayOnEnable => playOnEnable;
        public string Label => label;
        public bool HasALoop => animationAsset != null && animationAsset.HasALoop();

        private GameObject context;
        private Animatable contextAnimatable;

        public void CreateInstance()
        {
            if (animationAsset != null)
            {
                animationAsset = animationAsset.CreateInstance();
            }
        }

        public void AssignGameObject(GameObject gameObject)
        {
            _gameObject = gameObject;
        }

        public void CancelAnimation()
        {
            if (animationAsset != null)
            {
                animationAsset.StopAnimations();
            }
        }

        public void AnimationPlay(GameObject gameObject, UnityAction endAction = null, bool instant = false, bool clearer = false)
        {
            if (differentTarget)
            {
                AssignGameObject(target);
            }
            else
            {
                AssignGameObject(gameObject);
            }

            if (animationAsset == null)
            {
                Debug.LogWarning($"Animation asset is null on {gameObject.name}");
                return;
            }

            if (animationAsset.HasAnInfiniteLoop() && animationAsset.Performing)
                return;

            DoAnimationStartEvents();
            DoAnimationStartUnityEvent();

            animationStartAudio.PlayAudio();
            if (animationStartHaptics.Enabled)
                SIGS.TriggerHaptic(animationStartHaptics);

            animationAsset.PerformAnimation(() =>
            {
                DoAnimationEndEvents();
                DoAnimationEndUnityEvent();
                animationEndAudio.PlayAudio();
                if (animationEndHaptics.Enabled)
                    SIGS.TriggerHaptic(animationEndHaptics);
                endAction?.Invoke();
            }, _gameObject, instant, clearer);
        }

        public void SetContext(GameObject gameObject)
        {
            context = gameObject;
            contextAnimatable = gameObject.GetComponent<Animatable>();
        }

        #region Events

        private void DoAnimationStartEvents()
        {
            foreach (var ev in animationStartEvents)
            {
                SimpleRadio.SendEventByContext(ev, context);
            }
        }

        private void DoAnimationEndEvents()
        {
            foreach (var ev in animationEndEvents)
            {
                SimpleRadio.SendEventByContext(ev, context);
            }
        }

        public string[] animationStartEvents = new string[0];

        public string[] animationEndEvents = new string[0];
        #endregion

        #region Unity Events
        private void DoAnimationStartUnityEvent()
        {
            animationStartUnityEvent?.Invoke();
        }

        private void DoAnimationEndUnityEvent()
        {
            animationEndUnityEvent?.Invoke();
        }

        public UnityEvent animationStartUnityEvent;

        public UnityEvent animationEndUnityEvent;
        #endregion

        #region Audio
        [SerializeField] private string animationStartAudio;

        [SerializeField] private string animationEndAudio;
        #endregion

        #region Haptics
        [SerializeField] private HapticSettings animationStartHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings animationEndHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        #endregion
    }

    /// <summary>
    /// Animation settings for a single animation within an AnimationAsset.
    /// Similar to UIAnimationSettings but for standard Transform operations.
    /// </summary>
    [System.Serializable]
    public class AnimationSettings
    {
        /// <summary>
        /// Copies the current settings to a new instance without keeping the reference.
        /// </summary>
        public AnimationSettings ShallowCopy()
        {
            return (AnimationSettings)this.MemberwiseClone();
        }

        [SerializeField] protected string label;
        [SerializeField] protected bool disabled;

        [SerializeField] protected AnimationTarget animationType = AnimationTarget.Position;
        [SerializeField] protected TweenType tweenType = TweenType.From_To;
        [SerializeField] protected Ease easing = Ease.Linear;
        [SerializeField] protected float duration = 0.1f;
        [SerializeField] protected float delay = 0;
        [SerializeField] protected bool dontUseSource = false;
        [SerializeField] protected bool useLocalSpace = true;
        [SerializeField] protected int loops = 0;
        [SerializeField] protected LoopType loopType = LoopType.Restart;
        [SerializeField] protected bool randomizeEndValue;

        // come back settings
        [SerializeField] protected bool comeBack = false;
        [SerializeField] protected float comeBackDuration = 0.1f;
        [SerializeField] protected float comeBackDelay = 0;
        [SerializeField] protected Ease comeBackEasing = Ease.Linear;

        #region Events
        [SerializeField] private string[] startEvents = new string[0];
        [SerializeField] private string[] endEvents = new string[0];
        #endregion

        #region Audio
        [SerializeField] private string startAudio;
        [SerializeField] private string endAudio;
        #endregion

        #region Vector Animations
        [SerializeField] protected Vector3 startVector;
        [SerializeField] protected Vector3 endVector;
        #endregion

        #region Punch Specific
        [SerializeField] protected int vibrato = 10;
        [SerializeField] protected float elasticity = 1f;
        #endregion

        private Tween runningTween;

        public void CancelAnimation()
        {
            runningTween?.Kill();
        }

        public float EndTime => duration + (comeBack ? comeBackDuration : 0);
        public float DelayTime => delay + (comeBack ? comeBackDelay : 0);
        public float FullEndTime => EndTime + DelayTime;
        public int Loops => loops;
        public float Duration => duration;
        public AnimationTarget AnimationType => animationType;
        public TweenType _TweenType => tweenType;
        public Ease Easing => easing;
        public bool DontUseSource => dontUseSource;
        public bool Enabled => !disabled;

        public virtual void Perform(GameObject gameObject, bool instant, bool unscaledTime, AnimationAsset.EditorPreview editorprev = null, TransformSnapshot frags = default)
        {
            if (disabled) { return; }

            Transform transform = gameObject.transform;
            var loops = comeBack ? 0 : this.loops;

            var transformAnimation = animationType == AnimationTarget.Position
                || animationType == AnimationTarget.Rotation
                || animationType == AnimationTarget.Scale;

            float duration = instant ? Mathf.Epsilon : this.duration;
            float delay = instant ? Mathf.Epsilon : this.delay;
            bool punches = tweenType == TweenType.Punch;

            PerformEventsAndElse(gameObject);

            switch (animationType)
            {
                case AnimationTarget.Position:
                    if (!dontUseSource)
                    {
                        if (useLocalSpace)
                            transform.localPosition = startVector;
                        else
                            transform.position = startVector;
                    }

                    Action posCompletion = () =>
                    {
                        if (punches) return;

                        if (comeBack)
                        {
                            var startPos = useLocalSpace ? transform.localPosition : transform.position;
                            if (useLocalSpace)
                            {
                                runningTween = transform.DOLocalMove(startVector, comeBackDuration)
                                    .SetDelay(comeBackDelay)
                                    .SetEase(comeBackEasing)
                                    .SetUpdate(unscaledTime);
                            }
                            else
                            {
                                runningTween = transform.DOMove(startVector, comeBackDuration)
                                    .SetDelay(comeBackDelay)
                                    .SetEase(comeBackEasing)
                                    .SetUpdate(unscaledTime);
                            }
                        }
                    };

                    if (punches)
                    {
                        var endValue = randomizeEndValue ? endVector + RandomizeVector(endVector) : endVector;

                        Vector3 basePos = dontUseSource ? (frags.IsValid ? frags.position : (useLocalSpace ? transform.localPosition : transform.position)) : (useLocalSpace ? transform.localPosition : transform.position);
                        Vector3 punchDelta = Vector3.zero;

                        runningTween = DOTween.Punch(
                            () => punchDelta,
                            x =>
                            {
                                punchDelta = x;
                                if (useLocalSpace)
                                    transform.localPosition = basePos + punchDelta;
                                else
                                    transform.position = basePos + punchDelta;
                            },
                            endValue,
                            duration,
                            vibrato,
                            elasticity
                        )
                        .SetDelay(delay)
                        .SetUpdate(unscaledTime)
                        .OnComplete(() => posCompletion.Invoke())
                        .SetLoops(loops, loopType);

                        break;
                    }

                    if (useLocalSpace)
                    {
                        runningTween = transform.DOLocalMove(endVector, duration)
                            .SetDelay(delay)
                            .SetEase(easing)
                            .SetLoops(loops, loopType)
                            .SetUpdate(unscaledTime)
                            .OnComplete(() => posCompletion.Invoke());
                    }
                    else
                    {
                        runningTween = transform.DOMove(endVector, duration)
                            .SetDelay(delay)
                            .SetEase(easing)
                            .SetLoops(loops, loopType)
                            .SetUpdate(unscaledTime)
                            .OnComplete(() => posCompletion.Invoke());
                    }
                    break;


                case AnimationTarget.Rotation:
                    if (!dontUseSource)
                    {
                        if (useLocalSpace)
                            transform.localEulerAngles = startVector;
                        else
                            transform.eulerAngles = startVector;
                    }

                    if (punches)
                    {
                        var endValue = randomizeEndValue ? endVector + RandomizeVector(endVector) : endVector;

                        Vector3 baseRot = dontUseSource ? (frags.IsValid ? frags.rotation : (useLocalSpace ? transform.localEulerAngles : transform.eulerAngles)) : (useLocalSpace ? transform.localEulerAngles : transform.eulerAngles);
                        Vector3 punchDelta = Vector3.zero;

                        runningTween = DOTween.Punch(
                            () => punchDelta,
                            x =>
                            {
                                punchDelta = x;
                                if (useLocalSpace)
                                    transform.localEulerAngles = baseRot + punchDelta;
                                else
                                    transform.eulerAngles = baseRot + punchDelta;
                            },
                            endValue,
                            duration,
                            vibrato,
                            elasticity
                        )
                        .SetDelay(delay)
                        .SetUpdate(unscaledTime)
                        .SetLoops(loops, loopType);

                        break;
                    }

                    if (useLocalSpace)
                    {
                        runningTween = transform.DOLocalRotate(endVector, duration)
                            .SetDelay(delay)
                            .SetEase(easing)
                            .SetLoops(loops, loopType)
                            .SetUpdate(unscaledTime);
                    }
                    else
                    {
                        runningTween = transform.DORotate(endVector, duration)
                            .SetDelay(delay)
                            .SetEase(easing)
                            .SetLoops(loops, loopType)
                            .SetUpdate(unscaledTime);
                    }
                    break;


                case AnimationTarget.Scale:
                    if (!dontUseSource)
                    {
                        transform.localScale = startVector;
                    }

                    if (punches)
                    {
                        var endValue = randomizeEndValue ? endVector + RandomizeVector(endVector) : endVector;

                        Vector3 baseScale = dontUseSource ? (frags.IsValid ? frags.scale : transform.localScale) : transform.localScale;
                        Vector3 punchDelta = Vector3.zero;

                        runningTween = DOTween.Punch(
                            () => punchDelta,
                            x =>
                            {
                                punchDelta = x;
                                transform.localScale = baseScale + punchDelta;
                            },
                            endValue,
                            duration,
                            vibrato,
                            elasticity
                        )
                        .SetDelay(delay)
                        .SetUpdate(unscaledTime)
                        .SetLoops(loops, loopType);

                        break;
                    }

                    runningTween = transform.DOScale(endVector, duration)
                        .SetDelay(delay)
                        .SetEase(easing)
                        .SetLoops(loops, loopType)
                        .SetUpdate(unscaledTime);
                    break;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            // editor preview
            editorprev?.Invoke(runningTween?.SetLoops(loops != 0 ? 6 : loops));
            if (loops != 0)
            {
                Debug.Log("Infinite loops won't be shown in the editor preview. Limiting the number of loops for brevity.");
            }
#endif
        }

        private Vector3 RandomizeVector(Vector3 v)
        {
            return new Vector3(
                UnityEngine.Random.Range(-v.x, v.x),
                UnityEngine.Random.Range(-v.y, v.y),
                UnityEngine.Random.Range(-v.z, v.z)
            );
        }

        private void PerformEventsAndElse(GameObject gameObject)
        {
            foreach (var ev in startEvents)
            {
                SimpleRadio.SendEventByContext(ev, gameObject);
            }

            SIGS.DoIn(FullEndTime, () =>
            {
                foreach (var ev in endEvents)
                {
                    SimpleRadio.SendEventByContext(ev, gameObject);
                }
            });

            startAudio.PlayAudio();

            SIGS.DoIn(FullEndTime, () =>
            {
                endAudio.PlayAudio();
            });
        }

        public enum AnimationTarget
        {
            Position = 0,
            Rotation = 1,
            Scale = 2,
        }

        public enum TweenType
        {
            From_To,
            Punch
        }

        #region Quick Defines
        public void Prepare_ScaleUp()
        {
            label = "Scale Up";
            animationType = AnimationTarget.Scale;
            tweenType = TweenType.From_To;
            easing = Ease.OutBack;
            duration = 0.3f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            startVector = Vector3.zero;
            endVector = Vector3.one;
        }

        public void Prepare_ScaleDown()
        {
            label = "Scale Down";
            animationType = AnimationTarget.Scale;
            tweenType = TweenType.From_To;
            easing = Ease.InBack;
            duration = 0.3f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            startVector = Vector3.one;
            endVector = Vector3.zero;
        }

        public void Prepare_PunchPosition()
        {
            label = "Punch Position";
            animationType = AnimationTarget.Position;
            tweenType = TweenType.Punch;
            dontUseSource = true;
            duration = 0.5f;
            delay = 0;
            loops = 0;
            endVector = new Vector3(0.5f, 0, 0);
            vibrato = 10;
            elasticity = 1;
        }

        public void Prepare_PunchRotation()
        {
            label = "Punch Rotation";
            animationType = AnimationTarget.Rotation;
            tweenType = TweenType.Punch;
            dontUseSource = true;
            duration = 0.5f;
            delay = 0;
            loops = 0;
            endVector = new Vector3(0, 0, 15);
            vibrato = 10;
            elasticity = 1;
        }

        public void Prepare_PunchScale()
        {
            label = "Punch Scale";
            animationType = AnimationTarget.Scale;
            tweenType = TweenType.Punch;
            dontUseSource = true;
            duration = 0.5f;
            delay = 0;
            loops = 0;
            endVector = new Vector3(0.2f, 0.2f, 0.2f);
            vibrato = 10;
            elasticity = 1;
        }

        public void Prepare_LoopRotation()
        {
            label = "Loop Rotation";
            animationType = AnimationTarget.Rotation;
            tweenType = TweenType.From_To;
            easing = Ease.Linear;
            duration = 1f;
            delay = 0;
            loops = -1;
            loopType = LoopType.Incremental;
            startVector = Vector3.zero;
            endVector = new Vector3(0, 360, 0);
        }

        public void Prepare_MoveUp()
        {
            label = "Move Up";
            animationType = AnimationTarget.Position;
            tweenType = TweenType.From_To;
            easing = Ease.OutQuad;
            duration = 0.5f;
            delay = 0;
            loops = 0;
            useLocalSpace = true;
            dontUseSource = true;
            endVector = new Vector3(0, 1, 0);
        }

        public void Prepare_Bounce()
        {
            label = "Bounce";
            animationType = AnimationTarget.Position;
            tweenType = TweenType.From_To;
            easing = Ease.OutBounce;
            duration = 0.8f;
            delay = 0;
            loops = 0;
            useLocalSpace = true;
            startVector = new Vector3(0, 1, 0);
            endVector = Vector3.zero;
        }
        #endregion
    }

    /// <summary>
    /// Snapshot of a transform's state for restoration after animations.
    /// </summary>
    [System.Serializable]
    public struct TransformSnapshot
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public bool IsValid;

        public TransformSnapshot(Vector3 pos, Vector3 scale, Vector3 rot)
        {
            position = pos;
            this.scale = scale;
            rotation = rot;
            IsValid = true;
        }

        public TransformSnapshot(Transform t, bool useLocal = true)
        {
            if (useLocal)
            {
                position = t.localPosition;
                rotation = t.localEulerAngles;
            }
            else
            {
                position = t.position;
                rotation = t.eulerAngles;
            }
            scale = t.localScale;
            IsValid = true;
        }
    }
}
