using UnityEngine;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using System.Linq;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Inventory.Game
{
    /// <summary>
    /// Simple MonoBehaviour implementation of the inventory system.
    /// Provides an easy gateway for users to immediately start working with the inventory framework.
    /// Manages a single InventoryDefinition and provides convenient methods to interact with it.
    /// </summary>
    public class GameInventory : MonoBehaviour
    {
        //[Header("Inventory Configuration")]
        [SerializeField] private string inventoryID = "";
        
        //[Header("Auto-Generate Settings")]
        [SerializeField] private bool autoGenerateID = true;
        [SerializeField] private string idPrefix = "Inventory_";

        //[Header("Auto-Initialize")]
        [SerializeField] private bool initializeOnAwake = true;

        //[Header("Persistence Settings")]
        [SerializeField] private bool persistent = true;
        [Tooltip("When enabled, this inventory will be saved and loaded automatically")]
        [SerializeField] private bool saveOnDestroy = true;

        private InventoryDefinition inventory;

        /// <summary>
        /// Gets the current inventory definition.
        /// </summary>
        public InventoryDefinition Inventory => inventory;

        /// <summary>
        /// Gets the inventory ID.
        /// </summary>
        public string InventoryID => inventoryID;

        /// <summary>
        /// Gets whether this inventory is persistent (saved/loaded).
        /// </summary>
        public bool IsPersistent => persistent;

        /// <summary>
        /// Gets whether this inventory will save on destroy.
        /// </summary>
        public bool WillSaveOnDestroy => saveOnDestroy;

        #region Unity Lifecycle

        private void Awake()
        {
            // Generate ID if needed
            if (autoGenerateID && string.IsNullOrEmpty(inventoryID))
            {
                GenerateInventoryID();
            }

            // Initialize inventory if configured
            if (initializeOnAwake)
            {
                InitializeInventory();

                // subscribe to the SIGS event for updates. Only useful to track it via editor view, not really needed otherwise
                SIGS.Listener(inventory.UpdateListenerString, (s) => InitializeInventory());
            }
        }

        private void OnDestroy()
        {
            // Save the inventory when destroyed (if configured)
            if (saveOnDestroy && inventory != null && persistent)
            {
                inventory.SaveToDisk();
            }
        }

        #endregion

        #region Initialization


        /// <summary>
        /// Generates a unique inventory ID based on the GameObject's instance ID.
        /// </summary>
        [ContextMenu("Generate Inventory ID")]
        public void GenerateInventoryID()
        {
            inventoryID = $"{idPrefix}{gameObject.GetInstanceID()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        /// <summary>
        /// Initializes the inventory definition.
        /// Can be called manually if initializeOnAwake is false.
        /// Uses runtime cache to avoid excessive I/O operations.
        /// </summary>
        public void InitializeInventory()
        {
            if (string.IsNullOrEmpty(inventoryID))
            {
                Debug.LogError($"Cannot initialize inventory on {gameObject.name}: Inventory ID is null or empty.");
                return;
            }

            // Get or create inventory from cache (this avoids excessive I/O)
            // If persistent=true, it will automatically load saved data only on first creation
            inventory = InventoryDefinition.GetOrCreateInventory(inventoryID, persistent);
        }

        #endregion

        #region Inventory Operations

        /// <summary>
        /// Adds an item to this inventory.
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="quantity">The quantity to add</param>
        public void AddItem(ItemSO item, int quantity)
        {
            if (inventory == null)
            {
                Debug.LogError($"Cannot add item: Inventory not initialized on {gameObject.name}");
                return;
            }

            if (item == null)
            {
                Debug.LogWarning($"Cannot add null item to inventory on {gameObject.name}");
                return;
            }

            // Call directly on inventory (will auto-save if persistent)
            inventory.AddItem(item, quantity);
        }

        /// <summary>
        /// Removes an item from this inventory.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <param name="quantity">The quantity to remove</param>
        /// <returns>True if the removal was successful</returns>
        public bool RemoveItem(ItemSO item, int quantity)
        {
            if (inventory == null)
            {
                Debug.LogError($"Cannot remove item: Inventory not initialized on {gameObject.name}");
                return false;
            }

            if (item == null)
            {
                Debug.LogWarning($"Cannot remove null item from inventory on {gameObject.name}");
                return false;
            }

            // Call directly on inventory (will auto-save if persistent)
            return inventory.RemoveItem(item, quantity);
        }

        /// <summary>
        /// Transfers an item from this inventory to a target inventory.
        /// </summary>
        /// <param name="item">The item to transfer</param>
        /// <param name="quantity">The quantity to transfer</param>
        /// <param name="targetInventory">The target GameInventory to move the item to</param>
        /// <returns>True if the transfer was successful</returns>
        public bool TransferItem(ItemSO item, int quantity, GameInventory targetInventory)
        {
            if (inventory == null)
            {
                Debug.LogError($"Cannot transfer item: Source inventory not initialized on {gameObject.name}");
                return false;
            }

            if (targetInventory == null)
            {
                Debug.LogWarning($"Cannot transfer item: Target inventory is null");
                return false;
            }

            if (targetInventory.Inventory == null)
            {
                Debug.LogError($"Cannot transfer item: Target inventory not initialized on {targetInventory.gameObject.name}");
                return false;
            }

            if (item == null)
            {
                Debug.LogWarning($"Cannot transfer null item");
                return false;
            }

            // Use the inventory's MoveItem method directly
            return inventory.MoveItem(item, quantity, targetInventory.Inventory);
        }

        /// <summary>
        /// Gets the quantity of a specific item in this inventory.
        /// </summary>
        public int GetItemQuantity(ItemSO item)
        {
            if (inventory == null)
            {
                Debug.LogWarning($"Cannot get item quantity: Inventory not initialized on {gameObject.name}");
                return 0;
            }

            return inventory.GetItemQuantity(item);
        }

        /// <summary>
        /// Checks if this inventory contains a specific item.
        /// </summary>
        public bool HasItem(ItemSO item)
        {
            if (inventory == null)
            {
                Debug.LogWarning($"Cannot check item: Inventory not initialized on {gameObject.name}");
                return false;
            }

            return inventory.HasItem(item);
        }

        /// <summary>
        /// Clears all items from this inventory.
        /// </summary>
        public void ClearInventory()
        {
            if (inventory == null)
            {
                Debug.LogWarning($"Cannot clear: Inventory not initialized on {gameObject.name}");
                return;
            }

            // Call directly on inventory (will auto-save if persistent)
            inventory.Clear();
        }

        #endregion

        #region Public Accessors

        /// <summary>
        /// Sets a new inventory ID and reinitializes. Warning: This will lose the current inventory.
        /// </summary>
        public void SetInventoryID(string newID)
        {
            if (string.IsNullOrEmpty(newID))
            {
                Debug.LogError("Cannot set null or empty inventory ID");
                return;
            }

            // Save current inventory if needed
            if (inventory != null && persistent && saveOnDestroy)
            {
                inventory.SaveToDisk();
            }

            inventoryID = newID;
            InitializeInventory();
        }

        /// <summary>
        /// Sets whether this inventory should save when destroyed.
        /// </summary>
        public void SetSaveOnDestroy(bool value)
        {
            saveOnDestroy = value;
        }

        #endregion
    }
}
