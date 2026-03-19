using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using AHAKuo.Signalia.Framework;
using DG.Tweening;
using TMPro;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.UI
{
    [System.Serializable]
    public class Cascadable
    {
        [SerializeField] private UIAnimatable target;
        [SerializeField] private UIView targetView;
        [SerializeField] private float showDelay;
        [SerializeField] private bool showOnEnd;
        [SerializeField] private string audioAtStart;
        [SerializeField] private string audioAtEnd;

        public bool ShowOnEnd => showOnEnd;
        public UIAnimatable Target => target;

        public void Show()
        {
            if (target == null
            && targetView == null) { Debug.LogWarning("Cascade target is null."); return; }

            if (targetView != null)
            {
                SIGS.DoIn(showDelay, () =>
                {
                    targetView.Show();
                    SIGS.PlayAudio(audioAtStart);
                });
            }

            if (target != null)
            {
                SIGS.DoIn(showDelay, () =>
                {
                    if (target.AnimationArray.Length < 2) { Debug.LogWarning("Cascade target has less than 2 animations."); return; }
                    target.PlayAnimationFromIndex(0);
                    SIGS.PlayAudio(audioAtStart);
                });
            }
        }

        public void Hide()
        {
            // in hide animations, we hide directly and instantly using the end target of the animation

            if (target == null
            && targetView == null) { Debug.LogWarning("Cascade target is null."); return; }

            if (targetView != null)
            {
                targetView.Hide(true);
                SIGS.PlayAudio(audioAtEnd);
                return;
            }

            if (target != null)
            {
                if (target.AnimationArray.Length < 2) { Debug.LogWarning("Cascade target has less than 2 animations."); return; }
                target.PlayAnimationFromIndex_ForCascadeHide();
                SIGS.PlayAudio(audioAtEnd);
            }
        }

        public Cascadable(UIAnimatable target, int dominoIndex = 0, bool showDomino = false, bool hideDomino = false)
        {
            this.target = target;

            if (showDomino)
            {
                showOnEnd = true;
                showDelay = dominoIndex * 0.1f;
            }
        }
    }

    [System.Serializable]
    public class UIAnimatableArrayable
    {
        [SerializeField] private string label;

        [SerializeField] private UIAnimationAsset animationAsset;
        public UIAnimationAsset AnimationAsset => animationAsset;

        [SerializeField] private bool playOnEnable;

        [SerializeField] private bool differentTarget = false;
        public bool DifferentTarget => differentTarget;

        [SerializeField] private GameObject target;
        public GameObject Target => target;

        private GameObject _gameObject;
        public bool PlayOnEnable => playOnEnable;
        public string Label => label;
        public bool HasALoop => animationAsset.HasALoop();

        private GameObject context;
        private UIAnimatable contextAnimatable;
        private bool stored_O;

        public bool BlocksClicks => contextAnimatable == null || !contextAnimatable.DontBlockClicks;

        public void CreateInstance()
        {
            if(animationAsset != null)
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
            if (!stored_O)
            {
                stored_O = true;
            }

            if (differentTarget)
            {
                AssignGameObject(target);
            }
            else
            {
                AssignGameObject(gameObject);
            }

            if (animationAsset.HasAnInfiniteLoop()
                && animationAsset.Performing)
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

            if (!animationAsset.HasALoop() && BlocksClicks)
            {
                RuntimeValues.TrackedValues.LogMovingAnimatableLength(this, animationAsset);
            }
        }

        public void SetContext(GameObject gameObject)
        {
            context = gameObject;
            contextAnimatable = gameObject.GetComponent<UIAnimatable>();
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

    [System.Serializable]
    public class UIAnimationSettings
    {
        /// <summary>
        /// Copies the current settings to a new instance without keeping the reference.
        /// </summary>
        /// <returns></returns>
        public UIAnimationSettings ShallowCopy()
        {
            return (UIAnimationSettings)this.MemberwiseClone();
        }

        public UIAnimationSettings CreateNegative()
        {
            var copy = ShallowCopy();
            copy.startEvents = startEvents?.Length > 0 ? (string[])startEvents.Clone() : new string[0];
            copy.endEvents = endEvents?.Length > 0 ? (string[])endEvents.Clone() : new string[0];

            if (tweenType == TweenType.Punch)
            {
                if (animationType == AnimationTarget.Fade)
                {
                    (copy.startFloat, copy.endFloat) = (copy.endFloat, copy.startFloat);
                }
                else
                {
                    copy.endVector = -copy.endVector;
                }

                return copy;
            }

            if (animationType == AnimationTarget.Fade)
            {
                (copy.startFloat, copy.endFloat) = (copy.endFloat, copy.startFloat);
                return copy;
            }

            (copy.startVector, copy.endVector) = (copy.endVector, copy.startVector);
            return copy;
        }

        [SerializeField] protected string label;
        [SerializeField] protected bool disabled;

        [SerializeField] protected AnimationTarget animationType = AnimationTarget.Position;
        [SerializeField] protected TweenType tweenType = TweenType.From_To;
        [SerializeField] protected Ease easing = Ease.Linear;
        [SerializeField] protected float duration = 0.1f;
        [SerializeField] protected float delay = 0;
        [SerializeField] protected bool dontUseSource = false;            
        [SerializeField] protected int loops = 0;
        [SerializeField] protected LoopType loopType = LoopType.Restart;
        [SerializeField] protected bool randomizeEndValue;

        // come back settings
        [SerializeField] protected bool comeBack = false; // if true, the tween will return to the original value after the tween is done
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

        #region Float Animations
        [SerializeField] protected float startFloat;
        [SerializeField] protected float endFloat;
        #endregion

        #region Punch Specific
        [SerializeField] protected int vibrato;
        [SerializeField] protected float elasticity;
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
        public AnimationTarget AnimationType => animationType;
        public TweenType _TweenType => tweenType;
        public Ease Easing => easing;
        public bool DontUseSource => dontUseSource;
        public bool Enabled => !disabled;

        public virtual void Perform(GameObject gameObject, bool instant, bool unscaledTime, UIAnimationAsset.EditorPreview editorprev = null, V3 frags = default)
        {
            if (disabled) { return; }

            // prepare source holders
            RectTransform rectSource = null;
            Image imgSource = null;
            TextMeshProUGUI textField = null;
            CanvasGroup cgSource = null;
            var loops = comeBack ? 0 : this.loops;

            // define booleans
            var rectAnimation = animationType == AnimationTarget.Position
                || animationType == AnimationTarget.Rotation
                || animationType == AnimationTarget.Scale;

            var fadeAnimation = animationType == AnimationTarget.Fade;

            // assign source holders
            if (rectAnimation)
            {
                gameObject.TryGetComponent(out rectSource);

                if (rectSource == null)
                {
                    Debug.LogWarning("No rect transform found on the object.");
                    return;
                }
            }

            if (fadeAnimation)
            {
                gameObject.TryGetComponent(out cgSource);

                gameObject.TryGetComponent(out imgSource);

                gameObject.TryGetComponent(out textField);

                if (cgSource == null && imgSource == null && textField == null)
                {
                    Debug.LogWarning("No fadeable component found on the object.");
                    return;
                }
            }

            float duration = instant ? Mathf.Epsilon : this.duration; // overrides

            float delay = instant ? Mathf.Epsilon : this.delay; // overrides

            bool punches = tweenType == TweenType.Punch;

            PerformEventsAndElse(gameObject);

            switch (animationType)
            {
                case AnimationTarget.Position:
                    Vector2 pos1 = UICameraTag.UICamera.ViewportToScreenPoint(startVector);
                    Vector2 pos2 = UICameraTag.UICamera.ViewportToScreenPoint(endVector);

                    // Use the canvas of the rectSource to convert the screen point to a local point
                    var canvas = rectSource.GetComponentInParent<Canvas>();

                    var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    var invalidRenderMode = canvas.renderMode == RenderMode.WorldSpace;

                    if (invalidRenderMode)
                    {
                        Debug.LogWarning("The canvas render mode is set to world space. This is not supported.");
                        return;
                    }

                    RectTransform canvasRT = canvas.transform as RectTransform;

                    // Convert screen space to local space (taking anchor into account)
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, pos1, camera, out pos1);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, pos2, camera, out pos2);

                    if (!dontUseSource)
                    {
                        rectSource.anchoredPosition = pos1;
                    }

                    Action completion = () =>
                    {
                        if (punches) return;

                        if (comeBack)
                        {
                            runningTween = rectSource.DOAnchorPos(pos1, comeBackDuration)
                                .SetDelay(comeBackDelay)
                                .SetEase(comeBackEasing)
                                .SetUpdate(unscaledTime);
                        }
                    };

                    if (punches)
                    {
                        var endValue = randomizeEndValue ? endVector + endVector.RandomizeInRange() : endVector;

                        Vector2 basePos = dontUseSource ? frags.position : rectSource.anchoredPosition;
                        Vector2 punchDelta = Vector2.zero;

                        runningTween = DOTween.Punch(
                            () => punchDelta,
                            x =>
                            {
                                punchDelta = x;
                                rectSource.anchoredPosition = basePos + punchDelta;
                            },
                            endValue,
                            duration,
                            vibrato,
                            elasticity
                        )
                        .SetDelay(delay)
                        .SetUpdate(unscaledTime)
                        .OnComplete(() => completion.Invoke())
                        .SetLoops(loops, loopType);

                        break;
                    }

                    runningTween = rectSource.DOAnchorPos(pos2, duration)
                        .SetDelay(delay)
                        .SetEase(easing)
                        .SetLoops(loops, loopType)
                        .SetUpdate(unscaledTime)
                        .OnComplete(() => completion.Invoke());
                    break;


                case AnimationTarget.Rotation:
                    if (!dontUseSource)
                    {
                        rectSource.localEulerAngles = startVector;
                    }

                    if (punches)
                    {
                        var endValue = randomizeEndValue ? endVector + endVector.RandomizeInRange() : endVector;

                        Vector3 baseRot = dontUseSource ? frags.rotation : rectSource.localEulerAngles;   // fragment baseline
                        Vector3 punchDelta = Vector3.zero;      // ghost delta

                        runningTween = DOTween.Punch(
                            () => punchDelta,
                            x =>
                            {
                                punchDelta = x;
                                rectSource.localEulerAngles = baseRot + punchDelta;
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

                    runningTween = rectSource.DOLocalRotate(endVector, duration)
                        .SetDelay(delay)
                        .SetEase(easing)
                        .SetLoops(loops, loopType)
                        .SetUpdate(unscaledTime);
                    break;


                case AnimationTarget.Scale:
                    if (!dontUseSource)
                    {
                        rectSource.localScale = startVector;
                    }

                    if (punches)
                    {
                        var endValue = randomizeEndValue ? endVector + endVector.RandomizeInRange() : endVector;

                        Vector3 baseScale = dontUseSource ? frags.scale : rectSource.localScale;   // fragment baseline
                        Vector3 punchDelta = Vector3.zero;          // ghost variable

                        runningTween = DOTween.Punch(
                            () => punchDelta,
                            x =>
                            {
                                punchDelta = x;
                                rectSource.localScale = baseScale + punchDelta; // always around baseline
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

                    runningTween = rectSource.DOScale(endVector, duration)
                        .SetDelay(delay)
                        .SetEase(easing)
                        .SetLoops(loops, loopType)
                        .SetUpdate(unscaledTime);
                    break;


                case AnimationTarget.Fade:
                    if (!dontUseSource)
                    {
                        if (cgSource != null)
                        {
                            cgSource.alpha = startFloat;
                        }
                        else if (imgSource != null)
                        {
                            imgSource.color = new Color(imgSource.color.r, imgSource.color.g, imgSource.color.b, startFloat);
                        }
                        else if (textField != null)
                        {
                            textField.alpha = startFloat;
                        }
                    }

                    if (cgSource != null)
                    {
                        runningTween = cgSource.DOFade(endFloat, duration).SetDelay(delay).SetEase(easing).SetLoops(loops, loopType).SetUpdate(unscaledTime);
                    }
                    else if (imgSource != null)
                    {
                        runningTween = imgSource.DOFade(endFloat, duration).SetDelay(delay).SetEase(easing).SetLoops(loops, loopType).SetUpdate(unscaledTime);
                    }
                    else if (textField != null)
                    {
                        runningTween = textField.DOFade(endFloat, duration).SetDelay(delay).SetEase(easing).SetLoops(loops, loopType).SetUpdate(unscaledTime);
                    }
                    break;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            // editor preview
            editorprev?.Invoke(runningTween.SetLoops(loops != 0 ? 6 : loops)); // keep loops minimal and non-infinite for preview
            if (loops != 0)
            {
                Debug.Log("Infinite loops won't be shown in the editor preview. Limiting the number of loops for brevity.");
            }
#endif
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
            Fade = 3,
        }

        public enum TweenType
        {
            From_To,
            Punch
        }

        #region Quick Defines
        public void Prepare_Fade01()
        {
            label = "Fade 0 to 1";
            animationType = AnimationTarget.Fade;
            tweenType = TweenType.From_To;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startFloat = 0;
            endFloat = 1;
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
        }

        public void Prepare_Fade10()
        {
            label = "Fade 1 to 0";
            animationType = AnimationTarget.Fade;
            tweenType = TweenType.From_To;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startFloat = 1;
            endFloat = 0;
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
        }

        public void Prepare_ScaleUp()
        {
            label = "Scale Up";
            animationType = AnimationTarget.Scale;
            tweenType = TweenType.From_To;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startVector = new Vector3(0, 0, 0);
            endVector = new Vector3(1, 1, 1);
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
        }

        public void Prepare_ScaleDown()
        {
            label = "Scale Down";
            animationType = AnimationTarget.Scale;
            tweenType = TweenType.From_To;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startVector = new Vector3(1, 1, 1);
            endVector = new Vector3(0, 0, 0);
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
        }

        public void Prepare_PunchPosition()
        {
            label = "Punch Position";
            animationType = AnimationTarget.Position;
            tweenType = TweenType.Punch;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startVector = new Vector3(0, 0, 0);
            endVector = new Vector3(0, 0, 0);
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
            vibrato = 10;
            elasticity = 1;
        }

        public void Prepare_PunchRotation()
        {
            label = "Punch Rotation";
            animationType = AnimationTarget.Rotation;
            tweenType = TweenType.Punch;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startVector = new Vector3(0, 0, 0);
            endVector = new Vector3(0, 0, 0);
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
            vibrato = 10;
            elasticity = 1;
        }

        public void Prepare_PunchScale()
        {
            label = "Punch Scale";
            animationType = AnimationTarget.Scale;
            tweenType = TweenType.Punch;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = 0;
            loopType = LoopType.Restart;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startVector = new Vector3(1, 1, 1);
            endVector = new Vector3(1, 1, 1);
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
            vibrato = 10;
            elasticity = 1;
        }

        public void Prepare_LoopZRotation()
        {
            label = "Loop Z Rotation";
            animationType = AnimationTarget.Rotation;
            tweenType = TweenType.From_To;
            easing = Ease.Linear;
            duration = 0.1f;
            delay = 0;
            loops = -1;
            loopType = LoopType.Incremental;
            comeBack = false;
            comeBackDuration = 0.1f;
            comeBackDelay = 0;
            comeBackEasing = Ease.Linear;
            startVector = new Vector3(0, 0, 0);
            endVector = new Vector3(0, 0, 45);
            startEvents = new string[0];
            endEvents = new string[0];
            startAudio = "";
            endAudio = "";
        }
        #endregion
    }
}
