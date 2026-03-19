using UnityEngine;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;

namespace AHAKuo.Signalia.GameSystems.Inventory.Game
{
    /// <summary>
    /// MonoBehaviour component for transferring items between inventories.
    /// Works with inventory IDs and uses InventoryManager for all transfer operations.
    /// </summary>
    public class GameInventoryTransferral : MonoBehaviour
    {
        [Header("Inventory IDs (Primary)")]
        [SerializeField] private string sourceInventoryID;
        [SerializeField] private string targetInventoryID;

        [Header("Editor Reference Helpers (Optional)")]
        [Tooltip("Optional: Drag a GameInventory component here for quick ID setup")]
        [SerializeField] private GameInventory sourceInventoryReference;
        
        [Tooltip("Optional: Drag a GameInventory component here for quick ID setup")]
        [SerializeField] private GameInventory targetInventoryReference;

        /// <summary>
        /// Gets the source inventory ID.
        /// </summary>
        public string SourceInventoryID => sourceInventoryID;

        /// <summary>
        /// Gets the target inventory ID.
        /// </summary>
        public string TargetInventoryID => targetInventoryID;

        #region Unity Lifecycle

        private void Start()
        {
            // Update IDs from references if they're set
            if (sourceInventoryReference != null && string.IsNullOrEmpty(sourceInventoryID))
            {
                sourceInventoryID = sourceInventoryReference.InventoryID;
            }

            if (targetInventoryReference != null && string.IsNullOrEmpty(targetInventoryID))
            {
                targetInventoryID = targetInventoryReference.InventoryID;
            }
        }

        #endregion

        #region Setup

        /// <summary>
        /// Updates inventory IDs from the referenced GameInventory components.
        /// </summary>
        [ContextMenu("Update IDs from References")]
        public void UpdateIDsFromReferences()
        {
            if (sourceInventoryReference != null)
            {
                sourceInventoryID = sourceInventoryReference.InventoryID;
            }

            if (targetInventoryReference != null)
            {
                targetInventoryID = targetInventoryReference.InventoryID;
            }
        }

        #endregion

        #region Transfer Operations

        /// <summary>
        /// Transfers a specific item from source to target using inventory IDs.
        /// </summary>
        /// <param name="item">The item to transfer</param>
        /// <param name="quantity">The quantity to transfer</param>
        /// <returns>True if the transfer was successful</returns>
        public bool TransferItem(ItemSO item, int quantity)
        {
            if (!ValidateIDs())
            {
                return false;
            }

            // Direct transfer between inventories - need to get references
            var sourceInventory = GetInventoryByID(sourceInventoryID);
            var targetInventory = GetInventoryByID(targetInventoryID);
            
            if (sourceInventory == null || targetInventory == null)
            {
                Debug.LogError("Source or target inventory not found");
                return false;
            }
            
            // Use the inventory's MoveItem method
            return sourceInventory.MoveItem(item, quantity, targetInventory);
        }

        /// <summary>
        /// Transfers all of a specific item from source to target.
        /// </summary>
        /// <param name="item">The item to transfer</param>
        /// <returns>True if the transfer was successful</returns>
        public bool TransferAllOfItem(ItemSO item)
        {
            if (!ValidateIDs())
            {
                return false;
            }

            var sourceInventory = GetInventoryByID(sourceInventoryID);
            if (sourceInventory == null)
            {
                return false;
            }

            int quantity = sourceInventory.GetItemQuantity(item);
            if (quantity > 0)
            {
                return TransferItem(item, quantity);
            }
            return false;
        }

        /// <summary>
        /// Transfers half of a specific item's quantity from source to target.
        /// </summary>
        /// <param name="item">The item to transfer</param>
        /// <returns>True if the transfer was successful</returns>
        public bool TransferHalfOfItem(ItemSO item)
        {
            if (!ValidateIDs())
            {
                return false;
            }

            var sourceInventory = GetInventoryByID(sourceInventoryID);
            if (sourceInventory == null)
            {
                return false;
            }

            int quantity = sourceInventory.GetItemQuantity(item);
            if (quantity > 1)
            {
                int halfQuantity = quantity / 2;
                return TransferItem(item, halfQuantity);
            }
            else if (quantity == 1)
            {
                return TransferItem(item, 1);
            }
            return false;
        }

        /// <summary>
        /// Transfers all items from source to target.
        /// </summary>
        /// <returns>The number of items successfully transferred</returns>
        public int TransferAllItems()
        {
            if (!ValidateIDs())
            {
                return 0;
            }

            var sourceInventory = GetInventoryByID(sourceInventoryID);
            if (sourceInventory == null || sourceInventory.Items == null)
            {
                return 0;
            }

            int transferredCount = 0;
            var items = sourceInventory.Items;

            // Create a temporary copy to avoid modification during iteration
            var itemsArray = new ItemDefinition[items.Count];
            items.CopyTo(itemsArray, 0);

            foreach (var itemDef in itemsArray)
            {
                if (itemDef.ItemReference != null)
                {
                    if (TransferItem(itemDef.ItemReference, itemDef.Quantity))
                    {
                        transferredCount++;
                    }
                }
            }

            return transferredCount;
        }

        private bool ValidateIDs()
        {
            if (string.IsNullOrEmpty(sourceInventoryID))
            {
                Debug.LogError($"[GameInventoryTransferral] Source inventory ID is not set on {gameObject.name}");
                return false;
            }

            if (string.IsNullOrEmpty(targetInventoryID))
            {
                Debug.LogError($"[GameInventoryTransferral] Target inventory ID is not set on {gameObject.name}");
                return false;
            }

            if (sourceInventoryID == targetInventoryID)
            {
                Debug.LogError($"[GameInventoryTransferral] Source and Target inventory IDs are the same on {gameObject.name}");
                return false;
            }

            return true;
        }

        #endregion

        #region Public Getters

        /// <summary>
        /// Gets the source inventory definition.
        /// </summary>
        public InventoryDefinition GetSourceInventory()
        {
            if (string.IsNullOrEmpty(sourceInventoryID))
            {
                return null;
            }
            return GetInventoryByID(sourceInventoryID);
        }

        /// <summary>
        /// Gets the target inventory definition.
        /// </summary>
        public InventoryDefinition GetTargetInventory()
        {
            if (string.IsNullOrEmpty(targetInventoryID))
            {
                return null;
            }
            return GetInventoryByID(targetInventoryID);
        }
        
        /// <summary>
        /// Helper method to find inventory by ID.
        /// Searches all GameInventory components in the scene.
        /// </summary>
        private InventoryDefinition GetInventoryByID(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
                
            // Search all GameInventory components in scene
#if UNITY_6000_0_OR_NEWER
            var allInventories = FindObjectsByType<GameInventory>(FindObjectsSortMode.None);
#else
            var allInventories = FindObjectsOfType<GameInventory>();
#endif
            foreach (var inv in allInventories)
            {
                if (inv.InventoryID == id)
                {
                    return inv.Inventory;
                }
            }
            
            return null;
        }

        #endregion
    }
}
