using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Inventory.UI
{
    /// <summary>
    /// Abstract UI component that displays inventory in a grid format.
    /// Updates slots based on the connected inventory definition.
    /// </summary>
    public abstract class ItemGrid : MonoBehaviour
    {
        //[Header("Inventory Settings")]
        [SerializeField] protected string inventoryID;

        //[Header("Grid Components")]
        [SerializeField] protected List<ItemSlot> itemSlots = new();
        [SerializeField] protected ItemDisplayerPanel itemDisplayerPanel;

        //[Header("Auto-populate Settings")]
        [SerializeField] protected Transform slotsContainer;
        [SerializeField] protected GameObject slotPrefab;
        [SerializeField] protected int initialSlotCount = 20;

        //[Header("Category Filtering")]
        [SerializeField, Tooltip("Enable category filtering to display only specific categories")]
        protected bool enableCategoryFilter = false;
        
        [SerializeField, Tooltip("Category to filter by. Leave empty to show all items.")]
        protected string categoryFilter = "";

        protected Listener connectionUpdater;
        protected InventoryDefinition connectedInventory;

        protected HashSet<ItemDefinition> selectedItems = new();
        internal void AddSelected(ItemDefinition def) => selectedItems.Add(def);
        internal void RemoveSelected(ItemDefinition def) => selectedItems.Remove(def);

        #region Initialization

        protected virtual void Awake()
        {
            // Auto-populate slots if needed
            if (itemSlots.Count == 0 && slotPrefab != null && slotsContainer != null)
            {
                AutoPopulateSlots();
            }

            connectionUpdater = SIGS.Listener(InventoryDefinition.UpdateStringer(inventoryID), (s) => { RefreshGrid(s[0] as InventoryDefinition); }); // some of the weirdest code this side of the mississippi ;D
        }

        /// <summary>
        /// Auto-populates the grid with ItemSlot instances.
        /// </summary>
        protected virtual void AutoPopulateSlots()
        {
            itemSlots.Clear();

            for (int i = 0; i < initialSlotCount; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                ItemSlot slot = slotObj.GetComponent<ItemSlot>();

                if (slot != null)
                {
                    slot.Initialize(this);
                    itemSlots.Add(slot);
                }
                else
                {
                    Debug.LogError($"Slot prefab does not have an ItemSlot component on {slotObj.name}");
                    Destroy(slotObj);
                }
            }
        }

        #endregion

        #region Lifecycle
        protected virtual void Update()
        {
            // check if no items are selected, if none and displayer visible, and config is not sticky, hide it
            if (itemDisplayerPanel != null && itemDisplayerPanel.IsStickyDisplayerEnabled())
                return;

            var noContext = selectedItems.Count == 0;

            if (noContext && itemDisplayerPanel != null && itemDisplayerPanel.IsVisible)
            {
                itemDisplayerPanel.ClearDisplay();
            }
        }
        #endregion

        #region Grid Updates

        /// <summary>
        /// Refreshes the entire grid to match the inventory contents.
        /// Uses cached data directly - no disk I/O needed.
        /// Splits items into stacks based on MaxStackSize for display.
        /// </summary>
        public virtual void RefreshGrid(InventoryDefinition def)
        {
            if (def == null)
            {
                Debug.LogWarning("Cannot refresh grid: No connected inventory.");
                return;
            }

            connectedInventory = def;

            // Get items - either filtered or all items
            List<ItemDefinition> items = GetFilteredItems();

            // Split items into stacks and fill slots
            List<ItemDefinition> stackedItems = DistributeIntoStacks(items);
            
            // Clear only slots that need to be cleared (slots beyond the current item count)
            for (int i = stackedItems.Count; i < itemSlots.Count; i++)
            {
                itemSlots[i].UpdateSlot(new ItemDefinition());
            }

            // Fill slots with stacked items (only update slots that need updating)
            for (int i = 0; i < itemSlots.Count && i < stackedItems.Count; i++)
            {
                itemSlots[i].UpdateSlot(stackedItems[i]);
            }

            // If we have more stacks than slots, log a warning
            if (stackedItems.Count > itemSlots.Count)
            {
                Debug.LogWarning($"Inventory '{inventoryID}' has {stackedItems.Count} stacks but only {itemSlots.Count} slots. Some items won't be displayed.");
            }

            // Refresh the display panel if it's showing an item that's in this inventory
            RefreshDisplayPanelIfNeeded(def);
        }

        /// <summary>
        /// Distributes items into individual stacks based on MaxStackSize.
        /// This is a display layer operation - InventoryDefinition only stores total quantities.
        /// </summary>
        private List<ItemDefinition> DistributeIntoStacks(List<ItemDefinition> items)
        {
            List<ItemDefinition> stacks = new List<ItemDefinition>();

            foreach (var item in items)
            {
                if (item.ItemReference == null || item.Quantity <= 0)
                    continue;

                int maxStackSize = item.ItemReference.MaxStackSize;
                int remainingQuantity = item.Quantity;

                // Split into stacks
                while (remainingQuantity > 0)
                {
                    int stackQuantity = Mathf.Min(remainingQuantity, maxStackSize);
                    stacks.Add(new ItemDefinition(item.ItemReference, stackQuantity));
                    remainingQuantity -= stackQuantity;
                }
            }

            return stacks;
        }

        /// <summary>
        /// Gets the filtered list of items based on the current filter settings.
        /// </summary>
        private List<ItemDefinition> GetFilteredItems()
        {
            if (connectedInventory == null)
                return new List<ItemDefinition>();

            if (!enableCategoryFilter || string.IsNullOrEmpty(categoryFilter))
                return connectedInventory.Items;

            return connectedInventory.GetItemsByCategory(categoryFilter);
        }

        /// <summary>
        /// Sets the category filter and optionally refreshes the grid.
        /// </summary>
        /// <param name="category">The category to filter by. Pass empty string to clear filter.</param>
        /// <param name="refresh">Whether to immediately refresh the grid</param>
        public virtual void SetCategoryFilter(string category, bool refresh = true)
        {
            categoryFilter = category;
            enableCategoryFilter = !string.IsNullOrEmpty(category);
            
            if (refresh && connectedInventory != null)
            {
                RefreshGrid(connectedInventory);
            }
        }

        /// <summary>
        /// Clears the category filter and optionally refreshes the grid.
        /// </summary>
        /// <param name="refresh">Whether to immediately refresh the grid</param>
        public virtual void ClearCategoryFilter(bool refresh = true)
        {
            categoryFilter = "";
            enableCategoryFilter = false;
            
            if (refresh && connectedInventory != null)
            {
                RefreshGrid(connectedInventory);
            }
        }

        /// <summary>
        /// Gets the current category filter.
        /// </summary>
        /// <returns>The category filter string</returns>
        public string GetCategoryFilter()
        {
            return categoryFilter;
        }

        /// <summary>
        /// Checks if category filtering is enabled.
        /// </summary>
        /// <returns>True if filtering is active</returns>
        public bool IsCategoryFilterEnabled()
        {
            return enableCategoryFilter;
        }

        public void ForceRefreshGrid() => RefreshGrid(connectedInventory);

        /// <summary>
        /// Refreshes the display panel if it's currently showing an item.
        /// Called after grid refresh to ensure the displayed item has up-to-date quantity.
        /// </summary>
        private void RefreshDisplayPanelIfNeeded(InventoryDefinition def)
        {
            if (itemDisplayerPanel == null || def == null)
                return;

            ItemDefinition displayedItem = itemDisplayerPanel.GetCurrentDisplayedItem();
            
            // Check if there's a currently displayed item
            if (displayedItem.ItemReference == null)
                return;

            // Check if this item still exists in the inventory
            int totalQuantity = def.GetTotalItemQuantity(displayedItem.ItemReference);
            
            if (totalQuantity > 0)
            {
                // Item still exists - refresh display with updated quantity
                // Find the first slot that contains this item to get the current stack quantity
                ItemSlot matchingSlot = itemSlots.FirstOrDefault(slot => 
                    slot.IsActive() && 
                    slot.GetCurrentItem().ItemReference == displayedItem.ItemReference);
                
                // Use the stack quantity if found, otherwise use the total quantity
                int stackQuantity = matchingSlot != null 
                    ? matchingSlot.GetCurrentItem().Quantity 
                    : totalQuantity;
                
                // Create updated item with the proper stack quantity
                ItemDefinition updatedItem = new ItemDefinition(displayedItem.ItemReference, stackQuantity);
                itemDisplayerPanel.DisplayItem(updatedItem, totalQuantity);
            }
            else
            {
                // Item no longer exists - clear the display
                itemDisplayerPanel.ClearDisplay();
            }
        }

        #endregion

        #region Item Display

        /// <summary>
        /// Displays item details on the ItemDisplayerPanel.
        /// Called by ItemSlot when selected.
        /// Calculates and passes the total quantity across all stacks if the item exists in the inventory.
        /// </summary>
        public virtual void DisplayItemDetails(ItemDefinition item)
        {
            if (itemDisplayerPanel != null)
            {
                int totalQuantity = GetTotalItemQuantity(item);
                itemDisplayerPanel.DisplayItem(item, totalQuantity);
            }
            else
            {
                Debug.LogWarning("No ItemDisplayerPanel assigned to this ItemGrid.");
            }
        }

        /// <summary>
        /// Gets the total quantity of the item across all stacks in the inventory.
        /// </summary>
        private int GetTotalItemQuantity(ItemDefinition item)
        {
            if (item.ItemReference == null || connectedInventory == null)
                return item.Quantity;

            // Use the inventory's GetTotalItemQuantity method to get the total across all stacks
            return connectedInventory.GetTotalItemQuantity(item.ItemReference);
        }

        #endregion

        #region Public Accessors

        /// <summary>
        /// Gets the inventory ID this grid is displaying.
        /// </summary>
        public string GetInventoryID()
        {
            return inventoryID;
        }

        /// <summary>
        /// Sets the inventory ID and reconnects to the inventory.
        /// </summary>
        public void SetInventoryID(string newInventoryID)
        {
            inventoryID = newInventoryID;

        }

        /// <summary>
        /// Gets the connected inventory definition.
        /// </summary>
        public InventoryDefinition GetConnectedInventory()
        {
            return connectedInventory;
        }

        /// <summary>
        /// Gets all item slots in this grid.
        /// </summary>
        public List<ItemSlot> GetItemSlots()
        {
            return itemSlots;
        }

        #endregion
    }
}
