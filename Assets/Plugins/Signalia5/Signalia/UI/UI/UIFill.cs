using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using DG.Tweening;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AHAKuo.Signalia.UI
{
    [AddComponentMenu("Signalia/UI/Signalia | UI Fill")]
    /// <summary>
    /// Handles UI fill functionality. Can animate using an animation asset and animate either the fill property of an image or the X or Y scales of the rect transform.
    /// </summary>
    public class UIFill : MonoBehaviour
    {
        [SerializeField] private UIAnimationAsset animationAsset;
        [SerializeField] private float startFill;
        [SerializeField] private bool animateFill;
        [SerializeField] private float maxFill, minFill;
        [SerializeField] private string setFillListener; // Listener to set the fill value directly
        [SerializeField] private string adjustmentListener;
        [SerializeField] private string adjustedAudioPositive;
        [SerializeField] private string adjustedAudioNegative;
        [SerializeField] private bool brimAnimation = false;
        [SerializeField] private float brimStrength = 0.3f; // Strength of the brim animation punch effect
        [SerializeField] private string brimAudio;
        
        [SerializeField] private HapticSettings adjustedHapticsPositive = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings adjustedHapticsNegative = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings brimHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private DirectionFill directionFill = DirectionFill.LeftToRight;
        [SerializeField] private FillType fillType = FillType.ImageFill;

        //cache
        private RectTransform rectTransform;
        private Image image;
        private float currentFill;

        /// <summary>
        /// Depending on direction, we need to set the pivot of the rect transform to ensure the fill works correctly. Applicable when using scale fill type.
        /// </summary>
        private void SetPivot()
        {
            // if we arent using scale as fill type, skip this
            if (fillType != FillType.Scale)
            {
                return;
            }

            if (rectTransform == null)
            {
                Debug.LogError("RectTransform component not found on UIFill. Please ensure the GameObject has a RectTransform component.");
                return;
            }

            // Store the current pivot and position before changing
            Vector2 oldPivot = rectTransform.pivot;
            Vector2 oldAnchoredPosition = rectTransform.anchoredPosition;
            Vector2 sizeDelta = rectTransform.sizeDelta;

            Vector2 newPivot;
            switch (directionFill)
            {
                case DirectionFill.LeftToRight:
                    newPivot = new Vector2(0f, 0.5f); // Left
                    break;
                case DirectionFill.RightToLeft:
                    newPivot = new Vector2(1f, 0.5f); // Right
                    break;
                case DirectionFill.TopToBottom:
                    newPivot = new Vector2(0.5f, 1f); // Top
                    break;
                case DirectionFill.BottomToTop:
                    newPivot = new Vector2(0.5f, 0f); // Bottom
                    break;
                default:
                    return;
            }

            // Calculate the position adjustment needed to keep the object visually in the same place
            Vector2 pivotDifference = newPivot - oldPivot;
            Vector2 positionAdjustment = new Vector2(
                pivotDifference.x * sizeDelta.x,
                pivotDifference.y * sizeDelta.y
            );

            // Apply the new pivot and adjust the position
            rectTransform.pivot = newPivot;
            rectTransform.anchoredPosition = oldAnchoredPosition + positionAdjustment;
        }

        /// <summary>
        /// Sets the fill value using the animateFill field to determine animation.
        /// </summary>
        /// <param name="value">The target fill value</param>
        public void SetFill(float value, bool silent = false)
        {
            // normalize on the range of minFill and maxFill so we can use 0 to 1
            float normalizedValue = Mathf.InverseLerp(minFill, maxFill, value);
            float projectedChange = normalizedValue - currentFill;

            switch (fillType)
            {
                case FillType.ImageFill:

                    if (image == null)
                    {
                        Debug.LogError("Image component not found on UIFill. Please ensure the GameObject has an Image component.");
                        return;
                    }

                    // Set fill amount with or without animation
                    if (animateFill)
                    {
                        if (animationAsset == null || animationAsset.Animations.Count() == 0)
                        {
                            Debug.LogError("Animation asset is not set or has no animations. Please assign a valid UIAnimationAsset with animations.");
                            return;
                        }

                        var animation = animationAsset.Animations.FirstOrDefault();
                        image.DOFillAmount(normalizedValue, animation.FullEndTime)
                            .SetEase(animation.Easing)
                            .SetUpdate(animationAsset.UnscaledTime);
                    }
                    else
                    {
                        image.fillAmount = normalizedValue;
                    }
                    break;

                case FillType.Scale:

                    if (rectTransform == null)
                    {
                        Debug.LogError("RectTransform component not found on UIFill. Please ensure the GameObject has a RectTransform component.");
                        return;
                    }

                    // Determine which scale component to modify
                    bool isHorizontal = directionFill == DirectionFill.LeftToRight || directionFill == DirectionFill.RightToLeft;

                    var brimAnimationVisually = brimAnimation && normalizedValue == 1f && currentFill == 1; // if we are brim animating, we want to visually animate the brim fill with a punch instead of just scaling it

                    if (animateFill)
                    {
                        if (animationAsset == null || animationAsset.Animations.Count() == 0)
                        {
                            Debug.LogError("Animation asset is not set or has no animations. Please assign a valid UIAnimationAsset with animations.");
                            return;
                        }

                        var animation = animationAsset.Animations.FirstOrDefault();

                        if (brimAnimationVisually)
                        {
                            // If brim animation is active, use a punch effect instead of scaling
                            if (isHorizontal)
                            {
                                rectTransform.DOPunchScale(Vector3.right * brimStrength, animation.FullEndTime, 10, 0.5f)
                                    .SetEase(animation.Easing)
                                    .SetUpdate(animationAsset.UnscaledTime);
                            }
                            else
                            {
                                rectTransform.DOPunchScale(Vector3.up * brimStrength, animation.FullEndTime, 10, 0.5f)
                                    .SetEase(animation.Easing)
                                    .SetUpdate(animationAsset.UnscaledTime);
                            }

                            Finally(projectedChange, true);
                            return;
                        }

                        // Animate the scale
                        if (isHorizontal)
                        {
                            rectTransform.DOScaleX(normalizedValue, animation.FullEndTime)
                                .SetEase(animation.Easing)
                                .SetUpdate(animationAsset.UnscaledTime)
                                .OnUpdate(() =>
                                {
                                    // prevent the image from going under 0
                                    if (rectTransform.localScale.x < 0f)
                                    {
                                        rectTransform.localScale = new Vector3(0f, rectTransform.localScale.y, rectTransform.localScale.z);
                                    }
                                });
                        }
                        else
                        {
                            rectTransform.DOScaleY(normalizedValue, animation.FullEndTime)
                                .SetEase(animation.Easing)
                                .SetUpdate(animationAsset.UnscaledTime)
                                .OnUpdate(() =>
                                {
                                    // prevent the image from going under 0
                                    if (rectTransform.localScale.y < 0f)
                                    {
                                        rectTransform.localScale = new Vector3(rectTransform.localScale.x, 0f, rectTransform.localScale.z);
                                    }
                                });
                        }
                    }
                    else
                    {
                        // Set scale instantly
                        if (isHorizontal)
                        {
                            rectTransform.localScale = new Vector3(normalizedValue, 1f, 1f);
                        }
                        else
                        {
                            rectTransform.localScale = new Vector3(1f, normalizedValue, 1f);
                        }
                    }
                    break;
            }

            Finally(projectedChange);

            void Finally(float change, bool brim = false)
            {
                currentFill = normalizedValue;

                if (silent) return;

                if (brim)
                {
                    brimAudio?.PlayAudio();
                    if (brimHaptics.Enabled)
                        SIGS.TriggerHaptic(brimHaptics);
                }

                if (!brim)
                {
                    var positive = change >= 0;
                    var noChange = Mathf.Approximately(change, 0f);

                    if (noChange) return; // No change, no audio

                    if (positive)
                    {
                        adjustedAudioPositive?.PlayAudio();
                        if (adjustedHapticsPositive.Enabled)
                            SIGS.TriggerHaptic(adjustedHapticsPositive);
                    }
                    else
                    {
                        adjustedAudioNegative?.PlayAudio();
                        if (adjustedHapticsNegative.Enabled)
                            SIGS.TriggerHaptic(adjustedHapticsNegative);
                    }
                } 
            }
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            currentFill = startFill;

            if (animationAsset != null)
            {
                animationAsset = animationAsset.CreateInstance();
            }

            // Override the Image component's fill properties to align with UIFill direction
            OverrideImageFillProperties();

            SetPivot();
            SetFill(startFill, true);

            adjustmentListener.InitializeListener((s) =>
            {
                AdjustFill((Single)s[0]);
            });

            setFillListener.InitializeListener((s) =>
            {
                SetFill((Single)s[0], (bool)s[1]);
            });
        }

        /// <summary>
        /// Overrides the Image component's fill origin and method to align with UIFill direction
        /// Only applies when using ImageFill type.
        /// </summary>
        private void OverrideImageFillProperties()
        {
            if (image == null || fillType != FillType.ImageFill) return;

            // Set the fill method to Filled
            image.type = Image.Type.Filled;

            // Set fill origin based on direction
            switch (directionFill)
            {
                case DirectionFill.LeftToRight:
                    image.fillOrigin = (int)Image.OriginHorizontal.Left;
                    image.fillMethod = Image.FillMethod.Horizontal;
                    break;
                case DirectionFill.RightToLeft:
                    image.fillOrigin = (int)Image.OriginHorizontal.Right;
                    image.fillMethod = Image.FillMethod.Horizontal;
                    break;
                case DirectionFill.TopToBottom:
                    image.fillOrigin = (int)Image.OriginVertical.Top;
                    image.fillMethod = Image.FillMethod.Vertical;
                    break;
                case DirectionFill.BottomToTop:
                    image.fillOrigin = (int)Image.OriginVertical.Bottom;
                    image.fillMethod = Image.FillMethod.Vertical;
                    break;
            }
        }

        /// <summary>
        /// Adjusts the fill value based on a float input.
        /// </summary>
        /// <param name="f"></param>
        public void AdjustFill(float f)
        {
            // Convert currentFill back to the original scale, add adjustment, then normalize
            float currentValueInOriginalScale = Mathf.Lerp(minFill, maxFill, currentFill);
            float newValueInOriginalScale = currentValueInOriginalScale + f;
            
            // For brim animation, we want to allow the animation to play even when at limits
            if (brimAnimation && animateFill)
            {
                // Don't clamp here - let SetFill handle the brim animation logic
                SetFill(newValueInOriginalScale);
            }
            else
            {
                // Normal behavior - clamp the value in original scale
                newValueInOriginalScale = Mathf.Clamp(newValueInOriginalScale, minFill, maxFill);
                SetFill(newValueInOriginalScale);
            }
        }

        public void SetMax(float m) => maxFill = m;
        public void SetMin(float m) => minFill = m;

        private enum DirectionFill
        {
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop
        }

        private enum FillType
        {
            ImageFill,
            Scale,
        }
    }
}