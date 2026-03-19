using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AHAKuo.Signalia.GameSystems.AchievementSystem;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem.Examples
{
    /// <summary>
    /// UI slot component that displays a single achievement with its icon, title, description, and unlocked status.
    /// Used by AchievementViewer to display achievements in a list or grid.
    /// </summary>
    [AddComponentMenu("Signalia/Game Systems/Achievement System/Examples/Signalia | Achievement Slot")]
    public class AchievementSlot : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Image component that displays the achievement icon.")]
        [SerializeField] private Image iconImage;
        
        [Tooltip("Text component that displays the achievement title.")]
        [SerializeField] private TMP_Text titleText;
        
        [Tooltip("Text component that displays the achievement description (optional).")]
        [SerializeField] private TMP_Text descriptionText;
        
        [Tooltip("Optional GameObject that can be enabled/disabled to show unlocked status (e.g., a checkmark or overlay).")]
        [SerializeField] private GameObject unlockedIndicator;
        
        [Tooltip("Optional Image component that can be tinted or modified to show unlocked status.")]
        [SerializeField] private Image unlockedStatusImage;
        
        [Header("Canvas Group Control")]
        [Tooltip("Optional CanvasGroup component to control alpha and interactability based on unlocked status. Useful when no icon is available.")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Tooltip("If true, controls the CanvasGroup alpha based on unlocked status.")]
        [SerializeField] private bool controlAlpha = true;
        
        [Tooltip("If true, controls the CanvasGroup interactability based on unlocked status.")]
        [SerializeField] private bool controlInteractability = false;
        
        [Tooltip("Alpha value when the achievement is unlocked.")]
        [SerializeField, Range(0f, 1f)] private float unlockedAlpha = 1f;
        
        [Tooltip("Alpha value when the achievement is locked.")]
        [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.5f;
        
        [Header("Visual Settings")]
        [Tooltip("Color to apply to the slot when the achievement is unlocked.")]
        [SerializeField] private Color unlockedColor = Color.white;
        
        [Tooltip("Color to apply to the slot when the achievement is locked.")]
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        private AchievementSO currentAchievement;
        private bool isUnlocked;

        /// <summary>
        /// Updates this slot with achievement data and unlocked status.
        /// </summary>
        /// <param name="achievement">The achievement to display</param>
        /// <param name="unlocked">Whether the achievement is unlocked</param>
        public void UpdateSlot(AchievementSO achievement, bool unlocked)
        {
            currentAchievement = achievement;
            isUnlocked = unlocked;

            if (achievement == null)
            {
                ClearSlot();
                return;
            }

            // Update icon
            if (iconImage != null)
            {
                iconImage.sprite = achievement.Icon;
                iconImage.enabled = achievement.Icon != null;
                
                // Apply color based on unlocked status
                iconImage.color = unlocked ? unlockedColor : lockedColor;
            }

            // Update title
            if (titleText != null)
            {
                titleText.text = achievement.Title ?? "";
                titleText.color = unlocked ? unlockedColor : lockedColor;
            }

            // Update description
            if (descriptionText != null)
            {
                descriptionText.text = achievement.Description ?? "";
                descriptionText.color = unlocked ? unlockedColor : lockedColor;
            }

            // Update unlocked indicator
            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(unlocked);
            }

            // Update unlocked status image
            if (unlockedStatusImage != null)
            {
                unlockedStatusImage.color = unlocked ? unlockedColor : lockedColor;
            }

            // Update CanvasGroup
            if (canvasGroup != null)
            {
                if (controlAlpha)
                {
                    canvasGroup.alpha = unlocked ? unlockedAlpha : lockedAlpha;
                }

                if (controlInteractability)
                {
                    canvasGroup.interactable = unlocked;
                    canvasGroup.blocksRaycasts = unlocked;
                }
            }
        }

        /// <summary>
        /// Clears the slot, hiding all content.
        /// </summary>
        public void ClearSlot()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (titleText != null)
            {
                titleText.text = "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "";
            }

            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(false);
            }

            // Reset CanvasGroup
            if (canvasGroup != null)
            {
                if (controlAlpha)
                {
                    canvasGroup.alpha = lockedAlpha;
                }

                if (controlInteractability)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }

        /// <summary>
        /// Gets the currently displayed achievement.
        /// </summary>
        /// <returns>The achievement being displayed, or null if none</returns>
        public AchievementSO GetAchievement()
        {
            return currentAchievement;
        }

        /// <summary>
        /// Gets whether the displayed achievement is unlocked.
        /// </summary>
        /// <returns>True if unlocked, false otherwise</returns>
        public bool IsUnlocked()
        {
            return isUnlocked;
        }

        /// <summary>
        /// Refreshes the slot's unlocked status from the AchievementManager.
        /// </summary>
        public void RefreshUnlockedStatus()
        {
            if (currentAchievement != null && !string.IsNullOrWhiteSpace(currentAchievement.Id))
            {
                bool unlocked = AchievementManager.IsUnlocked(currentAchievement.Id);
                UpdateSlot(currentAchievement, unlocked);
            }
        }
    }
}
