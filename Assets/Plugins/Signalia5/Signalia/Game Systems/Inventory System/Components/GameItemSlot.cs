using UnityEngine;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.UI;
using AHAKuo.Signalia.GameSystems.Inventory.Data;

namespace AHAKuo.Signalia.GameSystems.Inventory.Game
{
    /// <summary>
    /// Simple concrete implementation of ItemSlot.
    /// Provides a ready-to-use slot UI component with basic click behavior.
    /// When clicked, it displays the item details on the ItemDisplayerPanel.
    /// </summary>
    public class GameItemSlot : ItemSlot
    {
        //[Header("Game Item Slot Settings")]
        [SerializeField] private bool logClickEvents = false;

        /// <summary>
        /// Called when the slot is clicked.
        /// Default behavior: Triggers OnSelect to display item details.
        /// </summary>
        protected override void OnClick()
        {
            if (!isActive || currentItem.ItemReference == null)
            {
                if (logClickEvents)
                {
                    Debug.Log($"Clicked empty slot on {gameObject.name}");
                }
                return;
            }

            if (logClickEvents)
            {
                Debug.Log($"Clicked slot with item: {currentItem.ItemReference.ItemName} (Quantity: {currentItem.Quantity})");
            }

            // Default behavior: Show item details
            OnSelect();
        }

        /// <summary>
        /// Override of OnSelect to provide additional logging if enabled.
        /// </summary>
        protected override void OnSelect()
        {
            base.OnSelect();

            if (logClickEvents && currentItem.ItemReference != null)
            {
                Debug.Log($"Selected item: {currentItem.ItemReference.ItemName}");
            }
        }

        /// <summary>
        /// Public method to use the item in this slot (if it's usable).
        /// Can be called from UI buttons or events.
        /// </summary>
        public void UseItemInSlot()
        {
            if (!isActive || currentItem.ItemReference == null)
            {
                Debug.LogWarning("Cannot use item: Slot is empty. This shouldn't happen.");
                return;
            }

            ItemSO item = currentItem.ItemReference;

            if (!item.IsUsable)
            {
                return;
            }

            // Get the inventory this slot belongs to
            if (parentGrid != null)
            {
                InventoryDefinition inventory = parentGrid.GetConnectedInventory();
                
                if (inventory != null)
                {
                    item.ConsumeItem(inventory);
                }
                else
                {
                    Debug.LogError("Cannot use item: Parent grid has no connected inventory");
                }
            }
            else
            {
                Debug.LogError("Cannot use item: Slot has no parent grid");
            }
        }

        /// <summary>
        /// Public method to discard the item in this slot (if it's discardable).
        /// Can be called from UI buttons or events.
        /// </summary>
        public void DiscardItemInSlot(int quantity = 1)
        {
            if (!isActive || currentItem.ItemReference == null)
            {
                Debug.LogWarning("Cannot discard item: Slot is empty");
                return;
            }

            ItemSO item = currentItem.ItemReference;

            if (!item.IsDiscardable)
            {
                Debug.LogWarning($"Item {item.ItemName} cannot be discarded");
                return;
            }

            // Get the inventory this slot belongs to
            if (parentGrid != null)
            {
                InventoryDefinition inventory = parentGrid.GetConnectedInventory();
                
                if (inventory != null)
                {
                    quantity = Mathf.Min(quantity, currentItem.Quantity);
                    inventory.RemoveItem(item, quantity);
                }
                else
                {
                    Debug.LogError("Cannot discard item: Parent grid has no connected inventory");
                }
            }
            else
            {
                Debug.LogError("Cannot discard item: Slot has no parent grid");
            }
        }
    }
}
