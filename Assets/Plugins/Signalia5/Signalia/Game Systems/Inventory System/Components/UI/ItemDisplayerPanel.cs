using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.UI;

namespace AHAKuo.Signalia.GameSystems.Inventory.UI
{
    /// <summary>
    /// Abstract class that displays detailed item information in a panel.
    /// Shows information that wouldn't fit in an ItemSlot.
    /// Called by ItemGrid when an ItemSlot is selected.
    /// </summary>
    public abstract class ItemDisplayerPanel : MonoBehaviour
    {
        //[Header("UI View")]
        /// <summary>
        /// UIView component that controls panel visibility with animations.
        /// NOTE: When using UIView:
        /// 1. Ensure the animation assets (Show Animation and Hide Animation) have "Don't Use Source" enabled.
        /// 2. Disable "Play Only When Changing Status" to allow animations to play during status transitions.
        /// </summary>
        [SerializeField] protected UIView uiView;

        //[Header("Detailed Display Fields")]
        [SerializeField] protected GameObject panelContainer;
        [SerializeField] protected Image detailedIcon;
        [SerializeField] protected TMP_Text detailedItemName;
        [SerializeField] protected TMP_Text detailedDescription;
        [SerializeField] protected TMP_Text detailedCategory;
        [SerializeField] protected TMP_Text detailedQuantity;
        
        //[Header("Item Properties")]
        [SerializeField] protected TMP_Text maxStackSizeText;
        [SerializeField] protected TMP_Text isUsableText;
        [SerializeField] protected TMP_Text isDiscardableText;

        //[Header("Custom Properties")]
        [SerializeField] protected CustomPropertyView[] customPropertyViews;

        [Header("Display Settings")]
        [SerializeField, Tooltip("When enabled, the display panel will keep showing the last previewed item even if no item is currently selected. When disabled, the panel will hide when no items are selected.")]
        protected bool stickyDisplayer = false;

        protected ItemDefinition currentDisplayedItem;
        protected int totalItemQuantity;

        public bool IsVisible
        {
            get
            {
                return (uiView != null && uiView.IsShown) ||
                    (panelContainer != null && panelContainer.activeSelf);
            }
        }

        /// <summary>
        /// Displays the given item's details in the panel.
        /// Overload that accepts total quantity for the item across all stacks.
        /// </summary>
        public virtual void DisplayItem(ItemDefinition item, int totalQuantity)
        {
            if (item.ItemReference == null)
            {
                HidePanel();
                return;
            }

            currentDisplayedItem = item;
            totalItemQuantity = totalQuantity;
            ShowPanel();
            UpdateDisplay();
        }

        /// <summary>
        /// Displays the given item's details in the panel.
        /// Uses the stack quantity as the total quantity.
        /// </summary>
        public virtual void DisplayItem(ItemDefinition item)
        {
            DisplayItem(item, item.Quantity);
        }

        /// <summary>
        /// Updates all display fields with the current item's information.
        /// </summary>
        protected virtual void UpdateDisplay()
        {
            if (currentDisplayedItem.ItemReference == null)
                return;

            ItemSO item = currentDisplayedItem.ItemReference;

            // Update icon
            if (detailedIcon != null)
            {
                detailedIcon.sprite = item.Icon;
                detailedIcon.enabled = item.Icon != null;
            }

            // Update item name
            if (detailedItemName != null)
            {
                detailedItemName.text = item.ItemName ?? "Unknown Item";
            }

            // Update description
            if (detailedDescription != null)
            {
                detailedDescription.text = item.Description ?? "No description available.";
            }

            // Update category
            if (detailedCategory != null)
            {
                detailedCategory.text = !string.IsNullOrEmpty(item.Category) 
                    ? $"Category: {item.Category}" 
                    : "Category: None";
            }

            // Update quantity (show total or stack quantity based on config)
            if (detailedQuantity != null)
            {
                int quantityToShow = GetQuantityToDisplay();
                detailedQuantity.text = $"Quantity: {quantityToShow}";
            }

            // Update max stack size
            if (maxStackSizeText != null)
            {
                maxStackSizeText.text = $"Max Stack: {item.MaxStackSize}";
            }

            // Update usable status
            if (isUsableText != null)
            {
                isUsableText.text = item.IsUsable ? "Usable: Yes" : "Usable: No";
            }

            // Update discardable status
            if (isDiscardableText != null)
            {
                isDiscardableText.text = item.IsDiscardable ? "Discardable: Yes" : "Discardable: No";
            }

            // Update custom properties
            UpdateCustomDetails(item);
        }

        /// <summary>
        /// Override this method to display custom properties from your ItemSO.
        /// </summary>
        protected virtual void UpdateCustomDetails(ItemSO item)
        {
            // Get fallback text from config
            string fallbackText = "-";
            try
            {
                var config = Framework.ConfigReader.GetConfig();
                if (config != null)
                {
                    fallbackText = config.InventorySystem.CustomPropertyFallbackText;
                }
            }
            catch
            {
                // Use default fallback if config not available
            }

            // Update custom property views
            if (customPropertyViews != null && customPropertyViews.Length > 0)
            {
                foreach (var propertyView in customPropertyViews)
                {
                    propertyView.UpdateDisplay(item, fallbackText);
                }
            }
        }

        /// <summary>
        /// Gets the quantity to display based on the configuration.
        /// Returns total quantity if ShowTotalQuantityInDisplayer is enabled, otherwise returns stack quantity.
        /// </summary>
        protected virtual int GetQuantityToDisplay()
        {
            try
            {
                var config = ConfigReader.GetConfig();
                if (config != null && config.InventorySystem.ShowTotalQuantityInDisplayer)
                {
                    return totalItemQuantity;
                }
            }
            catch
            {
                // Use default behavior if config not available
            }

            // Default: show stack quantity
            return currentDisplayedItem.Quantity;
        }

        /// <summary>
        /// Shows the display panel.
        /// </summary>
        protected virtual void ShowPanel()
        {
            if (uiView != null)
            {
                uiView.Show();
            }
            else if (panelContainer != null)
            {
                panelContainer.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the display panel.
        /// </summary>
        protected virtual void HidePanel()
        {
            if (uiView != null)
            {
                uiView.Hide();
            }
            else if (panelContainer != null)
            {
                panelContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Clears the current displayed item.
        /// </summary>
        public virtual void ClearDisplay()
        {
            currentDisplayedItem = new ItemDefinition();
            totalItemQuantity = 0;
            HidePanel();
        }

        /// <summary>
        /// Gets the currently displayed item.
        /// </summary>
        public ItemDefinition GetCurrentDisplayedItem()
        {
            return currentDisplayedItem;
        }

        /// <summary>
        /// Gets whether the sticky displayer option is enabled.
        /// </summary>
        public bool IsStickyDisplayerEnabled()
        {
            return stickyDisplayer;
        }
    }
}
