using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using AHAKuo.Signalia.Framework;
using DG.Tweening;
using AHAKuo.Signalia.Utilities;

namespace AHAKuo.Signalia.UI
{
    /// <summary>
    /// A data asset that contains a list of animations that can be performed on a UI element.
    /// </summary>
    [CreateAssetMenu(menuName = "Signalia/UI and Audio/UI Animation Asset", fileName = "New UI Animation Asset")]
    public class UIAnimationAsset : ScriptableObject
    {
        /// <summary>
        /// Not wrapping this in editor code, but it is only used by the preview button.
        /// </summary>
        /// <param name="tween"></param>
        public delegate void EditorPreview(Tween tween);

        private static Dictionary<Transform, OldValue> _previewedObjects = new();

        public class OldValue
        {
            public V3 v3;

            public float canvasGroupAlpha = -1f;
            public float imageAlpha = -1f;
            public float textAlpha = -1f;
        }

        /// <summary>
        /// Called to create a local reference of this asset in the calling script that doesn't share a direct link to the main asset.
        /// </summary>
        /// <returns></returns>
        public UIAnimationAsset CreateInstance()
        {
            var newAsset = CreateInstance<UIAnimationAsset>();
            newAsset.animations = new List<UIAnimationSettings>();

            // name it as Instance with the name of this asset
            newAsset.name = "[INSTANCE] " + name;

            // Make a deep copy of the animations array
            foreach (var animation in animations)
            {
                newAsset.animations.Add(animation.ShallowCopy());
            }

            return newAsset;
        }

        public UIAnimationAsset CreateNegativeDuplicate()
        {
            var newAsset = CreateInstance<UIAnimationAsset>();
            newAsset.animations = new List<UIAnimationSettings>();
            newAsset.unscaledTime = unscaledTime;
            newAsset.disableFrags = disableFrags;

            foreach (var animation in animations)
            {
                newAsset.animations.Add(animation.CreateNegative());
            }

            return newAsset;
        }

        /// <summary>
        /// For editor use only. This is used to preview the animation in the editor.
        /// </summary>
        /// <param name="target"></param>
        public void PreviewAnimation(GameObject target, EditorPreview prepMet)
        {
            var instance = CreateInstance(); // optional

            if (!_previewedObjects.ContainsKey(target.transform))
            {
                var oldValue = new OldValue
                {
                    v3 = new V3(target.transform.position, target.transform.localScale, target.transform.rotation.eulerAngles)
                };

                if (target.TryGetComponent(out CanvasGroup cg))
                {
                    oldValue.canvasGroupAlpha = cg.alpha;
                }

                if (target.TryGetComponent(out UnityEngine.UI.Image img))
                {
                    oldValue.imageAlpha = img.color.a;
                }

                if (target.TryGetComponent(out TMPro.TextMeshProUGUI tmp))
                {
                    oldValue.textAlpha = tmp.alpha;
                }

                _previewedObjects.Add(target.transform, oldValue);
            }
            else
            {
                // reset object before replaying
                var old = _previewedObjects[target.transform];
                target.transform.position = old.v3.position;
                target.transform.localScale = old.v3.scale;
                target.transform.eulerAngles = old.v3.rotation;

                if (target.TryGetComponent(out CanvasGroup cg))
                {
                    cg.alpha = old.canvasGroupAlpha;
                }

                if (target.TryGetComponent(out UnityEngine.UI.Image img))
                {
                    var c = img.color;
                    img.color = new Color(c.r, c.g, c.b, old.imageAlpha);
                }

                if (target.TryGetComponent(out TMPro.TextMeshProUGUI tmp))
                {
                    tmp.alpha = old.textAlpha;
                }
            }

            instance.DoPreviewAnim(target, prepMet);
        }

        /// <summary>
        /// For editor use only. This is used to stop the preview of the animation in the editor.
        /// </summary>
        /// <param name="target"></param>
        public void StopPreview(GameObject target)
        {
            if (target == null)
            {
                Debug.LogWarning("No target provided to stop preview animation.");
                return;
            }

            // Kill all tweens on the target object and its components
            target.transform.DOKill();
            
            // Also kill tweens on components that might be animated (fade animations)
            if (target.TryGetComponent(out CanvasGroup cg))
            {
                cg.DOKill();
            }

            if (target.TryGetComponent(out UnityEngine.UI.Image img))
            {
                img.DOKill();
            }

            if (target.TryGetComponent(out TMPro.TextMeshProUGUI tmp))
            {
                tmp.DOKill();
            }

            // Always restore the object to its original state, even if the tween has finished
            if (_previewedObjects.TryGetValue(target.transform, out var oldValue))
            {
                target.transform.position = oldValue.v3.position;
                target.transform.localScale = oldValue.v3.scale;
                target.transform.eulerAngles = oldValue.v3.rotation;

                if (oldValue.canvasGroupAlpha >= 0 && target.TryGetComponent(out CanvasGroup cgRestore))
                {
                    cgRestore.alpha = oldValue.canvasGroupAlpha;
                }

                if (oldValue.imageAlpha >= 0 && target.TryGetComponent(out UnityEngine.UI.Image imgRestore))
                {
                    var c = imgRestore.color;
                    imgRestore.color = new Color(c.r, c.g, c.b, oldValue.imageAlpha);
                }

                if (oldValue.textAlpha >= 0 && target.TryGetComponent(out TMPro.TextMeshProUGUI tmpRestore))
                {
                    tmpRestore.alpha = oldValue.textAlpha;
                }

                _previewedObjects.Remove(target.transform);
            }
        }

        private void DoPreviewAnim(GameObject target, EditorPreview editorPrepMethod)
        {
            if (animations.Count <= 0)
            {
                Debug.LogWarning("No animations inside of the animation asset placed on [" + target.name + "].");
                return;
            }
            foreach (var s in animations)
            {
                s.Perform(target, false, unscaledTime, editorPrepMethod);
            }
        }

        public bool Performing { get; private set; }
        public bool UnscaledTime => unscaledTime;

        [SerializeField] private bool unscaledTime = true;

        [SerializeField] private bool disableFrags = false;

        [SerializeField] private List<UIAnimationSettings> animations = new();

        private Tween callbackTween;

        public UIAnimationSettings[] Animations => animations.ToArray();

        /// <summary>
        /// Returns the proposed full end time of the animation (taking into account the delays) if there are no overrides on that animation's time or if the animation call is not instant.
        /// </summary>
        /// <returns></returns>
        public float FullEndTime()
        {
            if (animations.Count <= 0)
            {
                return 0;
            }

            return animations.Max(animationClass => animationClass.FullEndTime);
        }

        public void StopAnimations()
        {
            foreach (var s in animations)
            {
                s.CancelAnimation();
            }
            // Kill the scheduled callback if it exists
            callbackTween?.Kill();
            callbackTween = null;
            Performing = false;
        }

        public bool HasALoop()
        {
            return animations.Any(animationClass => animationClass.Loops != 0);
        }

        public bool HasAnInfiniteLoop()
        {
            return animations.Any(animationClass => animationClass.Loops == -1);
        }

        public bool HasPunch()
        {
            return animations.Any(animationClass => animationClass._TweenType == UIAnimationSettings.TweenType.Punch);
        }

        public void PerformAnimation(Action endAction, GameObject target, bool instant, bool clearer = false)
        {
            if (animations.Count <= 0)
            {
                Debug.LogWarning("No animations inside of the animation asset placed on [" + target.name + "].");
                return;
            }

            int allDisabled = -1;

            if (allDisabled == animations.Count - 1)
            {
                Debug.LogWarning($"All tweens on the asset in {target.gameObject.name} are disabled.");
                return;
            }

            float maxTime = FullEndTime();

            Performing = true;

            if (animations.Count <= 0)
            {
                endAction?.Invoke();
                Performing = false;
                return;
            }

            var frags = new V3();

            // for punches
            if (HasPunch())
            {
                var rectSource = target.GetComponent<RectTransform>();
                frags = RuntimeValues.TrackedValues.GetFragment((target.GetInstanceID()).ToString(), rectSource,
                    new(rectSource != null ? rectSource.anchoredPosition : Vector3.zero, rectSource != null ? rectSource.localScale : Vector3.one, rectSource != null ? rectSource.eulerAngles : Vector3.zero));

                if (disableFrags) // use the original values as the fragment values
                {
                    frags = rectSource != null ? new V3(rectSource.anchoredPosition, rectSource.localScale, rectSource.eulerAngles) : new V3();
                }
            }

            if (clearer)
            {
                // kill all animations on the object first
                RuntimeValues.TrackedValues.KillNonLoopingOnObjectTarget(target.GetInstanceID().ToString());
            }

            if (!HasAnInfiniteLoop())
                RuntimeValues.TrackedValues.LogFiniteMover(new((target.GetInstanceID() + GetInstanceID()).ToString(), target.GetInstanceID().ToString()), FullEndTime(), unscaledTime, new(target, this));

            foreach (var s in animations)
            {
                s.Perform(target, instant, unscaledTime, null, frags);
            }

            if (!HasAnInfiniteLoop())
            {
                // Kill any existing callback tween before creating a new one
                callbackTween?.Kill();
                callbackTween = SIGS.DoIn(maxTime, () => {
                    endAction?.Invoke();
                    Performing = false;
                    callbackTween = null;
                }, unscaledTime);
            }
        }

        #region Quick add actions
#if UNITY_EDITOR
        public void Add_Fade01()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_Fade01();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("Fade01 added to the asset.");
        }
        public void Add_Fade10()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_Fade10();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("Fade10 added to the asset.");
        }

        public void Add_ScaleUp()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");

            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_ScaleUp();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("ScaleUp added to the asset.");
        }

        public void Add_ScaleDown()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");

            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_ScaleDown();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("ScaleDown added to the asset.");
        }

        public void Add_PunchPosition()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");

            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_PunchPosition();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("PunchPosition added to the asset.");
        }

        public void Add_PunchRotation()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");

            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_PunchRotation();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("PunchRotation added to the asset.");
        }

        public void Add_PunchScale()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");

            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_PunchScale();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("PunchScale added to the asset.");
        }

        public void Add_LoopZRotation()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");

            var newAnim = new UIAnimationSettings();
            newAnim.Prepare_LoopZRotation();
            animations.Add(newAnim);

            // Mark as dirty
            UnityEditor.EditorUtility.SetDirty(this);

            // Log
            Debug.Log("LoopZRotation added to the asset.");
        }
#endif
        #endregion
    }
}
