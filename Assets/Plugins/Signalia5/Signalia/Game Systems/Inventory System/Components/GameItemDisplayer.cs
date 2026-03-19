using UnityEngine;
using UnityEngine.UI;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.UI;
using AHAKuo.Signalia.GameSystems.Inventory.Data;

namespace AHAKuo.Signalia.GameSystems.Inventory.Game
{
    /// <summary>
    /// Simple concrete implementation of ItemDisplayerPanel.
    /// Provides a ready-to-use detailed item display panel.
    /// Shows comprehensive information about the selected item.
    /// Can be extended for custom display behaviors.
    /// </summary>
    public class GameItemDisplayer : ItemDisplayerPanel
    {
        //[Header("Game Item Displayer Settings")]
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool logDisplayEvents = false;

        //[Header("Optional Action Buttons")]
        [SerializeField] private Button useButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (hideOnStart)
            {
                HidePanel();
            }

            // Setup button listeners
            if (useButton != null)
            {
                useButton.onClick.AddListener(OnUseButtonClicked);
            }

            if (discardButton != null)
            {
                discardButton.onClick.AddListener(OnDiscardButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (useButton != null)
            {
                useButton.onClick.RemoveListener(OnUseButtonClicked);
            }

            if (discardButton != null)
            {
                discardButton.onClick.RemoveListener(OnDiscardButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        /// <summary>
        /// Override to add logging and button state updates.
        /// </summary>
        public override void DisplayItem(ItemDefinition item)
        {
            base.DisplayItem(item);

            if (item.ItemReference != null && logDisplayEvents)
            {
                Debug.Log($"Displaying item details: {item.ItemReference.ItemName}");
            }

            UpdateActionButtons();
        }

        /// <summary>
        /// Updates the state of action buttons based on the current item.
        /// </summary>
        protected virtual void UpdateActionButtons()
        {
            if (currentDisplayedItem.ItemReference == null)
            {
                // No item displayed, disable all buttons
                if (useButton != null) useButton.interactable = false;
                if (discardButton != null) discardButton.interactable = false;
                return;
            }

            ItemSO item = currentDisplayedItem.ItemReference;

            // Update use button
            if (useButton != null)
            {
                useButton.interactable = item.IsUsable && currentDisplayedItem.Quantity > 0;
            }

            // Update discard button
            if (discardButton != null)
            {
                discardButton.interactable = item.IsDiscardable && currentDisplayedItem.Quantity > 0;
            }
        }

        /// <summary>
        /// Called when the use button is clicked.
        /// Override this method to implement custom use behavior.
        /// </summary>
        protected virtual void OnUseButtonClicked()
        {
            if (currentDisplayedItem.ItemReference == null)
            {
                Debug.LogWarning("Cannot use item: No item displayed");
                return;
            }
            // Note: Actual use implementation should be handled by the GameItemSlot
            // or through direct inventory access. This is just a placeholder.
        }

        /// <summary>
        /// Called when the discard button is clicked.
        /// Override this method to implement custom discard behavior.
        /// </summary>
        protected virtual void OnDiscardButtonClicked()
        {
            if (currentDisplayedItem.ItemReference == null)
            {
                Debug.LogWarning("Cannot discard item: No item displayed");
                return;
            }
            // Note: Actual discard implementation should be handled by the GameItemSlot
            // or through direct inventory access. This is just a placeholder.
        }

        /// <summary>
        /// Called when the close button is clicked.
        /// </summary>
        protected virtual void OnCloseButtonClicked()
        {
            ClearDisplay();
            
            if (logDisplayEvents)
            {
                Debug.Log("Item displayer panel closed");
            }
        }

        /// <summary>
        /// Override to add button state updates when hiding.
        /// </summary>
        protected override void HidePanel()
        {
            base.HidePanel();

            // Disable action buttons when panel is hidden
            if (useButton != null) useButton.interactable = false;
            if (discardButton != null) discardButton.interactable = false;
        }

        /// <summary>
        /// Public method to manually close the panel.
        /// Can be called from external scripts or UI events.
        /// </summary>
        public void Close()
        {
            ClearDisplay();
        }
    }
}
