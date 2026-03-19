using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.Framework;
using UnityEngine.EventSystems;

namespace AHAKuo.Signalia.GameSystems.Inventory.UI
{
    /// <summary>
    /// Struct for binding custom property names to TMP_Text fields for display.
    /// </summary>
    [System.Serializable]
    public struct CustomPropertyView
    {
        [Tooltip("The name of the custom property to display (must match property name in ItemSO)")]
        public string propertyName;
        
        [Tooltip("The text component where this property will be displayed")]
        public TMP_Text displayText;
        
        /// <summary>
        /// Updates the display text with the custom property value from the item.
        /// </summary>
        /// <param name="item">The ItemSO to get the property from</param>
        /// <param name="fallbackText">Text to display if property is not found</param>
        public void UpdateDisplay(ItemSO item, string fallbackText = "-")
        {
            if (displayText == null) return;
            
            if (item.TryGetCustomProperty(propertyName, out var property))
            {
                string displayValue = property.Type switch
                {
                    ItemSO.PropertyType.Int => property.GetIntValue().ToString(),
                    ItemSO.PropertyType.Float => property.GetFloatValue().ToString("F2"),
                    ItemSO.PropertyType.String => property.GetStringValue(),
                    _ => fallbackText
                };
                displayText.text = displayValue;
            }
            else
            {
                displayText.text = fallbackText;
            }
        }
    }
    
    /// <summary>
    /// Abstract class that works with ItemGrid to display item icons, quantities, and uses.
    /// Includes UI click/press events, all of which are overridable.
    /// </summary>
    public abstract class ItemSlot : MonoBehaviour
    {
        //[Header("UI References")]
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TMP_Text itemNameText;
        [SerializeField] protected TMP_Text quantityText;
        [SerializeField] protected TMP_Text descriptionText;
        [SerializeField] protected GameObject slotContainer;

        //[Header("Custom Property Display")]
        [SerializeField] protected CustomPropertyView[] customPropertyViews;

        protected ItemDefinition currentItem;
        protected ItemGrid parentGrid;
        protected bool isActive = false;

        /// <summary>
        /// Initialize the slot with its parent grid reference.
        /// </summary>
        public virtual void Initialize(ItemGrid grid)
        {
            parentGrid = grid;
        }

        /// <summary>
        /// Updates this slot with new item data. Called by ItemGrid.
        /// Handles enabling/disabling the slot and updating all visual elements.
        /// </summary>
        public void UpdateSlot(ItemDefinition item)
        {
            currentItem = item;

            // Check if item is valid and has quantity > 0
            bool shouldBeActive = item.ItemReference != null && item.Quantity > 0;

            if (shouldBeActive)
            {
                isActive = true;
                EnableSlot();
                RedrawSlot();
            }
            else
            {
                isActive = false;
                DisableSlot();
            }
        }

        /// <summary>
        /// Redraws all UI elements to match the current item data.
        /// </summary>
        protected virtual void RedrawSlot()
        {
            if (currentItem.ItemReference == null)
                return;

            ItemSO item = currentItem.ItemReference;

            // Update icon
            if (iconImage != null)
            {
                iconImage.sprite = item.Icon;
                iconImage.enabled = item.Icon != null;
            }

            // Update item name
            if (itemNameText != null)
            {
                itemNameText.text = item.ItemName ?? "";
            }

            // Update quantity
            if (quantityText != null)
            {
                quantityText.text = currentItem.Quantity.ToString();
            }

            // Update description
            if (descriptionText != null)
            {
                descriptionText.text = item.Description ?? "";
            }

            // Update custom properties - override this method to customize
            UpdateCustomProperties(item);
        }

        /// <summary>
        /// Override this method to display custom properties from your ItemSO.
        /// </summary>
        protected virtual void UpdateCustomProperties(ItemSO item)
        {
            // Get fallback text from config
            string fallbackText = "-";
            try
            {
                var config = ConfigReader.GetConfig();
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
        /// Enables the slot visuals.
        /// </summary>
        protected virtual void EnableSlot()
        {
            if (slotContainer != null)
            {
                slotContainer.SetActive(true);
            }
        }

        /// <summary>
        /// Disables the slot visuals.
        /// </summary>
        protected virtual void DisableSlot()
        {
            if (slotContainer != null)
            {
                slotContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Abstract method called when the slot is clicked.
        /// Must be implemented by derived classes.
        /// </summary>
        protected abstract void OnClick();

        /// <summary>
        /// Virtual method called when the slot is selected.
        /// Base implementation updates the ItemDisplayerPanel through the ItemGrid.
        /// </summary>
        protected virtual void OnSelect()
        {
            if (parentGrid != null && currentItem.ItemReference != null)
            {
                parentGrid.DisplayItemDetails(currentItem);
            }

            // add to grid selections
            parentGrid.AddSelected(currentItem);
        }

        /// <summary>
        /// Public method to trigger click - can be called from UI events.
        /// </summary>
        public void TriggerClick()
        {
            if (isActive)
            {
                OnClick();
            }
        }

        /// <summary>
        /// Public method to trigger select - can be called from UI events.
        /// </summary>
        public void TriggerSelect()
        {
            if (isActive)
            {
                OnSelect();
            }
        }

        public void TriggerDeselect()
        {
            if (isActive && parentGrid != null)
            {
                parentGrid.RemoveSelected(currentItem);
            }
        }

        /// <summary>
        /// Gets the current item definition in this slot.
        /// </summary>
        public ItemDefinition GetCurrentItem()
        {
            return currentItem;
        }

        /// <summary>
        /// Checks if this slot is currently active (has an item).
        /// </summary>
        public bool IsActive()
        {
            return isActive;
        }
    }
}
