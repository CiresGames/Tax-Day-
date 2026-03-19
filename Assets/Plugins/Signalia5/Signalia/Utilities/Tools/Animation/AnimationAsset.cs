using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using AHAKuo.Signalia.Framework;
using DG.Tweening;

namespace Signalia.Utilities.Animation
{
    /// <summary>
    /// A data asset that contains a list of animations that can be performed on a GameObject with standard Transform.
    /// Similar to UIAnimationAsset but adapted for scene GameObjects without RectTransform.
    /// </summary>
    [CreateAssetMenu(menuName = "Signalia/Utilities/Animation Asset", fileName = "New Animation Asset")]
    public class AnimationAsset : ScriptableObject
    {
        /// <summary>
        /// Delegate for editor preview functionality.
        /// </summary>
        public delegate void EditorPreview(Tween tween);

        private static Dictionary<Transform, OldValue> _previewedObjects = new();

        public class OldValue
        {
            public TransformSnapshot snapshot;
            public float rendererAlpha = -1f;
        }

        /// <summary>
        /// Called to create a local reference of this asset that doesn't share a direct link to the main asset.
        /// </summary>
        public AnimationAsset CreateInstance()
        {
            var newAsset = CreateInstance<AnimationAsset>();
            newAsset.animations = new List<AnimationSettings>();

            newAsset.name = "[INSTANCE] " + name;

            foreach (var animation in animations)
            {
                newAsset.animations.Add(animation.ShallowCopy());
            }

            return newAsset;
        }

        /// <summary>
        /// For editor use only. This is used to preview the animation in the editor.
        /// </summary>
        public void PreviewAnimation(GameObject target, EditorPreview prepMet)
        {
            var instance = CreateInstance();

            if (!_previewedObjects.ContainsKey(target.transform))
            {
                var oldValue = new OldValue
                {
                    snapshot = new TransformSnapshot(target.transform, true)
                };

                if (target.TryGetComponent(out Renderer rend) && rend.sharedMaterial != null)
                {
                    oldValue.rendererAlpha = rend.sharedMaterial.color.a;
                }

                _previewedObjects.Add(target.transform, oldValue);
            }
            else
            {
                // reset object before replaying
                var old = _previewedObjects[target.transform];
                target.transform.localPosition = old.snapshot.position;
                target.transform.localScale = old.snapshot.scale;
                target.transform.localEulerAngles = old.snapshot.rotation;

                if (target.TryGetComponent(out Renderer rend) && old.rendererAlpha >= 0)
                {
                    var c = rend.sharedMaterial.color;
                    rend.sharedMaterial.color = new Color(c.r, c.g, c.b, old.rendererAlpha);
                }
            }

            instance.DoPreviewAnim(target, prepMet);
        }

        /// <summary>
        /// For editor use only. This is used to stop the preview of the animation in the editor.
        /// </summary>
        public void StopPreview(GameObject target)
        {
            if (target == null)
            {
                Debug.LogWarning("No target provided to stop preview animation.");
                return;
            }

            target.transform.DOKill();

            if (target.TryGetComponent(out Renderer rend) && rend.sharedMaterial != null)
            {
                rend.sharedMaterial.DOKill();
            }

            if (_previewedObjects.TryGetValue(target.transform, out var oldValue))
            {
                target.transform.localPosition = oldValue.snapshot.position;
                target.transform.localScale = oldValue.snapshot.scale;
                target.transform.localEulerAngles = oldValue.snapshot.rotation;

                if (oldValue.rendererAlpha >= 0 && target.TryGetComponent(out Renderer restoreRend))
                {
                    var c = restoreRend.sharedMaterial.color;
                    restoreRend.sharedMaterial.color = new Color(c.r, c.g, c.b, oldValue.rendererAlpha);
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

        [SerializeField] private List<AnimationSettings> animations = new();

        private Tween callbackTween;

        public AnimationSettings[] Animations => animations.ToArray();

        /// <summary>
        /// Returns the proposed full end time of the animation (taking into account the delays).
        /// </summary>
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
            return animations.Any(animationClass => animationClass._TweenType == AnimationSettings.TweenType.Punch);
        }

        public void PerformAnimation(Action endAction, GameObject target, bool instant, bool clearer = false)
        {
            if (animations.Count <= 0)
            {
                Debug.LogWarning("No animations inside of the animation asset placed on [" + target.name + "].");
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

            var frags = new TransformSnapshot();

            // for punches
            if (HasPunch())
            {
                frags = new TransformSnapshot(target.transform, true);

                if (disableFrags)
                {
                    frags = new TransformSnapshot(target.transform, true);
                }
            }

            foreach (var s in animations)
            {
                s.Perform(target, instant, unscaledTime, null, frags);
            }

            if (!HasAnInfiniteLoop())
            {
                callbackTween?.Kill();
                callbackTween = SIGS.DoIn(maxTime, () =>
                {
                    endAction?.Invoke();
                    Performing = false;
                    callbackTween = null;
                }, unscaledTime);
            }
        }

        #region Quick add actions
#if UNITY_EDITOR
        public void Add_ScaleUp()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_ScaleUp();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Scale Up added to the asset.");
        }

        public void Add_ScaleDown()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_ScaleDown();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Scale Down added to the asset.");
        }

        public void Add_PunchPosition()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_PunchPosition();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Punch Position added to the asset.");
        }

        public void Add_PunchRotation()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_PunchRotation();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Punch Rotation added to the asset.");
        }

        public void Add_PunchScale()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_PunchScale();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Punch Scale added to the asset.");
        }

        public void Add_LoopRotation()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_LoopRotation();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Loop Rotation added to the asset.");
        }

        public void Add_MoveUp()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_MoveUp();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Move Up added to the asset.");
        }

        public void Add_Bounce()
        {
            UnityEditor.Undo.RecordObject(this, "Quick Add Animation");
            var newAnim = new AnimationSettings();
            newAnim.Prepare_Bounce();
            animations.Add(newAnim);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Bounce added to the asset.");
        }
#endif
        #endregion
    }
}
