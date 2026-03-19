using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.AchievementSystem
{
    /// <summary>
    /// Definition data for an achievement.
    /// Achievements are referenced from <see cref="AHAKuo.Signalia.Framework.SignaliaConfigAsset"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAchievement", menuName = "Signalia/Game Systems/Achievement System/Achievement")]
    public class AchievementSO : ScriptableObject
    {
        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 6)] private string description;

        [Header("Identity")]
        [Tooltip("Unique ID used for saving/loading and backend unlock calls. Must be unique across all achievements.")]
        [SerializeField] private string id;

        public Sprite Icon => icon;
        public string Title => title;
        public string Description => description;
        public string Id => id;

        private void OnValidate()
        {
            // Keep IDs trim-clean to reduce accidental save-key mismatches.
            if (!string.IsNullOrWhiteSpace(id))
                id = id.Trim();
        }

        #region Editor-Only Initialization

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to initialize achievement data. Used for generating stock assets.
        /// </summary>
        public void SetAchievementData(string achievementId, string achievementTitle, string achievementDescription)
        {
            id = achievementId?.Trim() ?? "";
            title = achievementTitle ?? "";
            description = achievementDescription ?? "";
        }
#endif

        #endregion
    }
}

