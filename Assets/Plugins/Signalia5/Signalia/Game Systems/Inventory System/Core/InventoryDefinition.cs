using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.GameSystems.SaveSystem;

namespace AHAKuo.Signalia.GameSystems.Inventory.Core
{
    /// <summary>
    /// A mutable class containing a List of ItemDefinitions and a string ID.
    /// Used to track what this inventory contains.
    /// If persistent=true, automatically saves/loads from GameSaving system.
    /// </summary>
    [Serializable]
    public class InventoryDefinition
    {
        private const string INVENTORY_FILE_NAME = "inventory_data";
        
        // Runtime cache to avoid excessive I/O operations
        private static Dictionary<string, InventoryDefinition> runtimeCache = new Dictionary<string, InventoryDefinition>();
        private static bool cacheInitialized = false;
        
        [SerializeField] private string id;
        [SerializeField] private bool isPersistent;
        [SerializeField] private List<ItemDefinition> items;

        public string ID => id;
        public bool IsPersistent => isPersistent;
        public List<ItemDefinition> Items => items;
        public string UpdateListenerString => $"Inventory_{id}_Updated"; // for SIGS.Listener system

        /// <summary>
        /// Gets or creates an inventory from the runtime cache. This avoids excessive disk I/O.
        /// IMPORTANT: All calls to this method with the same ID will return the SAME instance.
        /// The 'persistent' parameter is only used when creating a NEW inventory, not when returning cached ones.
        /// </summary>
        public static InventoryDefinition GetOrCreateInventory(string id, bool persistent = false)
        {
            // Check if already cached
            if (runtimeCache.ContainsKey(id))
            {
                return runtimeCache[id];
            }
            
            // Create new inventory and add to cache
            InventoryDefinition newInventory = new InventoryDefinition(id, persistent);
            runtimeCache[id] = newInventory;
            
            return newInventory;
        }

        /// <summary>
        /// Initializes the inventory cache system. Should be called at game startup.
        /// </summary>
        public static void InitializeCache()
        {
            if (!cacheInitialized)
            {
                runtimeCache = new Dictionary<string, InventoryDefinition>();
                cacheInitialized = true;
            }
        }

        /// <summary>
        /// Clears the runtime cache and resets initialization state.
        /// </summary>
        public static void ClearCache()
        {
            if (runtimeCache != null)
            {
                // Save all persistent inventories before clearing
                foreach (var inventory in runtimeCache.Values)
                {
                    if (inventory.isPersistent)
                    {
                        inventory.SaveToDisk();
                    }
                }
                
                runtimeCache.Clear();
                cacheInitialized = false;
            }
        }

        /// <summary>
        /// Gets all cached inventories.
        /// </summary>
        public static Dictionary<string, InventoryDefinition> GetCachedInventories()
        {
            return runtimeCache;
        }

        /// <summary>
        /// Constructor for InventoryDefinition.
        /// If persistent=true, immediately loads existing data from save file.
        /// NOTE: Use GetOrCreateInventory() instead to leverage runtime caching.
        /// </summary>
        /// <param name="id">Unique identifier for this inventory</param>
        /// <param name="persistent">If true, this inventory will save and load from disk automatically</param>
        /// <exception cref="ArgumentException"></exception>
        public InventoryDefinition(string id, bool persistent = false)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Inventory ID cannot be null or empty.", nameof(id));

            this.id = id;
            this.isPersistent = persistent;
            this.items = new List<ItemDefinition>();
            
            // Load saved data if persistent
            if (persistent)
            {
                LoadFromSave();
            }

            // send notification to listeners, one second later to ensure proper initialization
            SIGS.DoNext(() =>
            {
                SIGS.Send(UpdateListenerString, this);
            });
        }

        public static string UpdateStringer(string id) => $"Inventory_{id}_Updated";

        /// <summary>
        /// Adds X of the item to this inventory.
        /// Respects MaxQuantity limit only. Stacking is handled by the UI layer.
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="quantity">Amount to add</param>
        public void AddItem(ItemSO item, int quantity)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot add null item to inventory.");
                return;
            }

            if (quantity <= 0)
            {
                Debug.LogWarning($"Cannot add non-positive quantity ({quantity}) of item {item.ItemName}.");
                return;
            }

            // Check MaxQuantity limit
            int currentTotal = GetTotalItemQuantity(item);
            int maxAddable = item.MaxQuantity - currentTotal;
            
            if (maxAddable <= 0)
            {
                Debug.LogWarning($"Cannot add {item.ItemName}: MaxQuantity limit of {item.MaxQuantity} reached.");
                return;
            }

            int quantityToAdd = Math.Min(quantity, maxAddable);

            // Find existing item
            int existingIndex = items.FindIndex(i => i.ItemReference == item);

            if (existingIndex >= 0)
            {
                // Item exists, update quantity
                ItemDefinition existing = items[existingIndex];
                int newQuantity = existing.Quantity + quantityToAdd;
                items[existingIndex] = existing.WithQuantity(newQuantity);
            }
            else
            {
                // New item, add to list
                items.Add(new ItemDefinition(item, quantityToAdd));
            }

            // Send add event if configured
            if (!string.IsNullOrEmpty(item.OnAddEvent))
            {
                item.OnAddEvent.SendEvent(item);
            }

            // Save and notify listeners
            SaveAndNotifyListeners();
        }

        /// <summary>
        /// Removes X of the item from this inventory if it exists.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <param name="quantity">Amount to remove</param>
        /// <returns>True if removal was successful</returns>
        public bool RemoveItem(ItemSO item, int quantity)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot remove null item from inventory.");
                return false;
            }

            if (quantity <= 0)
            {
                Debug.LogWarning($"Cannot remove non-positive quantity ({quantity}) of item {item.ItemName}.");
                return false;
            }

            int existingIndex = items.FindIndex(i => i.ItemReference == item);

            if (existingIndex < 0)
            {
                Debug.LogWarning($"Item {item.ItemName} not found in inventory {id}.");
                return false;
            }

            ItemDefinition existing = items[existingIndex];

            if (existing.Quantity < quantity)
            {
                Debug.LogWarning($"Insufficient quantity of {item.ItemName}. Has {existing.Quantity}, tried to remove {quantity}.");
                return false;
            }

            int newQuantity = existing.Quantity - quantity;

            if (newQuantity <= 0)
            {
                // Remove item completely
                items.RemoveAt(existingIndex);
            }
            else
            {
                // Update quantity
                items[existingIndex] = existing.WithQuantity(newQuantity);
            }

            // Send remove event if configured
            if (!string.IsNullOrEmpty(item.OnRemoveEvent))
            {
                item.OnRemoveEvent.SendEvent(item);
            }

            // Save and notify listeners
            SaveAndNotifyListeners();
            
            return true;
        }

        /// <summary>
        /// Moves X quantity of item from this inventory to the target inventory.
        /// </summary>
        /// <param name="item">The item to move</param>
        /// <param name="quantity">The quantity to move</param>
        /// <param name="targetInventory">The target inventory to move the item to</param>
        /// <returns>True if the move was successful</returns>
        public bool MoveItem(ItemSO item, int quantity, InventoryDefinition targetInventory)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot move null item.");
                return false;
            }

            if (targetInventory == null)
            {
                Debug.LogWarning("Cannot move item: Target inventory is null.");
                return false;
            }

            if (quantity <= 0)
            {
                Debug.LogWarning($"Cannot move non-positive quantity ({quantity}) of item {item.ItemName}.");
                return false;
            }

            // Check if source has enough items
            if (GetItemQuantity(item) < quantity)
            {
                Debug.LogError($"Insufficient quantity of {item.ItemName} in source inventory '{id}'.");
                return false;
            }

            // Remove from source
            bool removed = RemoveItem(item, quantity);
            if (!removed)
            {
                Debug.LogError($"Failed to remove item from source inventory '{id}'.");
                return false;
            }

            // Add to target (this will auto-save and notify listeners for the target)
            targetInventory.AddItem(item, quantity);

            return true;
        }


        /// <summary>
        /// Gets the total quantity of a specific item across all stacks in this inventory.
        /// </summary>
        public int GetItemQuantity(ItemSO item)
        {
            return GetTotalItemQuantity(item);
        }

        /// <summary>
        /// Checks if this inventory contains a specific item.
        /// </summary>
        public bool HasItem(ItemSO item)
        {
            if (item == null) return false;
            return items.Any(i => i.ItemReference == item);
        }

        /// <summary>
        /// Clears all items from this inventory.
        /// </summary>
        public void Clear()
        {
            items.Clear();
            
            // Save and notify listeners
            SaveAndNotifyListeners();
        }

        #region Query Methods

        /// <summary>
        /// Gets all items in this inventory that match the specified category.
        /// </summary>
        /// <param name="category">The category to filter by (case-insensitive)</param>
        /// <returns>List of ItemDefinitions matching the category</returns>
        public List<ItemDefinition> GetItemsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return new List<ItemDefinition>(items);
            
            return items.Where(i => i.ItemReference != null && 
                              string.Equals(i.ItemReference.Category, category, StringComparison.OrdinalIgnoreCase))
                       .ToList();
        }

        /// <summary>
        /// Gets all items in this inventory that match the given predicate.
        /// </summary>
        /// <param name="predicate">A function that returns true for items to include</param>
        /// <returns>List of ItemDefinitions matching the predicate</returns>
        public List<ItemDefinition> GetItemsByPredicate(Func<ItemDefinition, bool> predicate)
        {
            if (predicate == null)
                return new List<ItemDefinition>(items);
            
            return items.Where(predicate).ToList();
        }

        /// <summary>
        /// Gets all ItemDefinitions for a specific item type.
        /// </summary>
        /// <param name="item">The ItemSO to search for</param>
        /// <returns>List of all ItemDefinitions matching the ItemSO</returns>
        public List<ItemDefinition> GetAllItemsOfType(ItemSO item)
        {
            if (item == null)
                return new List<ItemDefinition>();
            
            return items.Where(i => i.ItemReference == item).ToList();
        }

        /// <summary>
        /// Counts the number of unique item types in this inventory.
        /// </summary>
        /// <returns>The number of distinct item types</returns>
        public int CountUniqueItems()
        {
            return items.Where(i => i.ItemReference != null)
                       .Select(i => i.ItemReference)
                       .Distinct()
                       .Count();
        }

        /// <summary>
        /// Gets a list of all unique categories present in this inventory.
        /// </summary>
        /// <returns>List of unique category names</returns>
        public List<string> GetCategories()
        {
            return items.Where(i => i.ItemReference != null && !string.IsNullOrEmpty(i.ItemReference.Category))
                      .Select(i => i.ItemReference.Category)
                      .Distinct()
                      .ToList();
        }

        /// <summary>
        /// Gets the total quantity of an item across all stacks in this inventory.
        /// This is useful when items are split across multiple stacks due to MaxStackSize.
        /// </summary>
        /// <param name="item">The item to count</param>
        /// <returns>Total quantity across all stacks</returns>
        public int GetTotalItemQuantity(ItemSO item)
        {
            if (item == null) return 0;
            
            return items.Where(i => i.ItemReference == item)
                       .Sum(i => i.Quantity);
        }

        /// <summary>
        /// Gets the total number of items in this inventory (counting all stacks as separate entries).
        /// </summary>
        /// <returns>The total number of item entries</returns>
        public int GetTotalItemCount()
        {
            return items.Count;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Checks if the specified item can be added to this inventory.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <param name="quantity">The quantity to check</param>
        /// <param name="failureReason">The reason if addition would fail</param>
        /// <returns>True if the item can be added</returns>
        public bool CanAddItem(ItemSO item, int quantity, out string failureReason)
        {
            failureReason = "";

            if (item == null)
            {
                failureReason = "Item is null";
                return false;
            }

            if (quantity <= 0)
            {
                failureReason = "Quantity must be positive";
                return false;
            }

            // Check MaxQuantity limit
            int currentTotal = GetTotalItemQuantity(item);
            int maxAddable = item.MaxQuantity - currentTotal;

            if (quantity > maxAddable)
            {
                failureReason = $"Would exceed MaxQuantity limit of {item.MaxQuantity} (current: {currentTotal})";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the specified item can be removed from this inventory.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <param name="quantity">The quantity to check</param>
        /// <param name="failureReason">The reason if removal would fail</param>
        /// <returns>True if the item can be removed</returns>
        public bool CanRemoveItem(ItemSO item, int quantity, out string failureReason)
        {
            failureReason = "";

            if (item == null)
            {
                failureReason = "Item is null";
                return false;
            }

            if (quantity <= 0)
            {
                failureReason = "Quantity must be positive";
                return false;
            }

            int totalQuantity = GetTotalItemQuantity(item);
            
            if (totalQuantity < quantity)
            {
                failureReason = $"Insufficient quantity. Have {totalQuantity}, need {quantity}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the specified item can be moved from this inventory to the target.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <param name="quantity">The quantity to check</param>
        /// <param name="target">The target inventory</param>
        /// <param name="failureReason">The reason if move would fail</param>
        /// <returns>True if the item can be moved</returns>
        public bool CanMoveItem(ItemSO item, int quantity, InventoryDefinition target, out string failureReason)
        {
            failureReason = "";

            if (item == null)
            {
                failureReason = "Item is null";
                return false;
            }

            if (target == null)
            {
                failureReason = "Target inventory is null";
                return false;
            }

            if (quantity <= 0)
            {
                failureReason = "Quantity must be positive";
                return false;
            }

            // Check source has enough
            string sourceReason;
            if (!CanRemoveItem(item, quantity, out sourceReason))
            {
                failureReason = $"Source inventory: {sourceReason}";
                return false;
            }

            // Check target can accept
            string targetReason;
            if (!target.CanAddItem(item, quantity, out targetReason))
            {
                failureReason = $"Target inventory: {targetReason}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates if multiple items can be added in a batch.
        /// </summary>
        /// <param name="itemsToAdd">Dictionary of items and quantities to check</param>
        /// <param name="failures">Dictionary of items that would fail and their reasons</param>
        /// <returns>True if all items can be added</returns>
        public bool CanAddItemsBatch(Dictionary<ItemSO, int> itemsToAdd, out Dictionary<ItemSO, string> failures)
        {
            failures = new Dictionary<ItemSO, string>();

            if (itemsToAdd == null)
            {
                failures[null] = "Items dictionary is null";
                return false;
            }

            foreach (var kvp in itemsToAdd)
            {
                string reason;
                if (!CanAddItem(kvp.Key, kvp.Value, out reason))
                {
                    failures[kvp.Key] = reason;
                }
            }

            return failures.Count == 0;
        }

        /// <summary>
        /// Validates if multiple items can be removed in a batch.
        /// </summary>
        /// <param name="itemsToRemove">Dictionary of items and quantities to check</param>
        /// <param name="failures">Dictionary of items that would fail and their reasons</param>
        /// <returns>True if all items can be removed</returns>
        public bool CanRemoveItemsBatch(Dictionary<ItemSO, int> itemsToRemove, out Dictionary<ItemSO, string> failures)
        {
            failures = new Dictionary<ItemSO, string>();

            if (itemsToRemove == null)
            {
                failures[null] = "Items dictionary is null";
                return false;
            }

            foreach (var kvp in itemsToRemove)
            {
                string reason;
                if (!CanRemoveItem(kvp.Key, kvp.Value, out reason))
                {
                    failures[kvp.Key] = reason;
                }
            }

            return failures.Count == 0;
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Adds multiple items to this inventory in a single atomic transaction.
        /// </summary>
        /// <param name="itemsToAdd">Dictionary of items and quantities to add</param>
        /// <param name="validateFirst">If true, validates all can be added before proceeding</param>
        /// <returns>True if all items were added successfully</returns>
        public bool AddItemsBatch(Dictionary<ItemSO, int> itemsToAdd, bool validateFirst = true)
        {
            if (itemsToAdd == null || itemsToAdd.Count == 0)
                return true;

            // Validate if requested
            if (validateFirst)
            {
                Dictionary<ItemSO, string> failures;
                if (!CanAddItemsBatch(itemsToAdd, out failures))
                {
                    foreach (var kvp in failures)
                    {
                        Debug.LogWarning($"Cannot add {kvp.Key?.ItemName ?? "null"}: {kvp.Value}");
                    }
                    return false;
                }
            }

            // Add all items
            foreach (var kvp in itemsToAdd)
            {
                if (kvp.Key != null && kvp.Value > 0)
                {
                    AddItemInternal(kvp.Key, kvp.Value);
                }
            }

            // Send add events for all items
            foreach (var kvp in itemsToAdd)
            {
                if (kvp.Key != null && !string.IsNullOrEmpty(kvp.Key.OnAddEvent))
                {
                    kvp.Key.OnAddEvent.SendEvent(kvp.Key);
                }
            }

            // Single save and notify
            SaveAndNotifyListeners();

            return true;
        }

        /// <summary>
        /// Removes multiple items from this inventory in a single atomic transaction.
        /// </summary>
        /// <param name="itemsToRemove">Dictionary of items and quantities to remove</param>
        /// <param name="validateFirst">If true, validates all can be removed before proceeding</param>
        /// <returns>True if all items were removed successfully</returns>
        public bool RemoveItemsBatch(Dictionary<ItemSO, int> itemsToRemove, bool validateFirst = true)
        {
            if (itemsToRemove == null || itemsToRemove.Count == 0)
                return true;

            // Validate if requested
            if (validateFirst)
            {
                Dictionary<ItemSO, string> failures;
                if (!CanRemoveItemsBatch(itemsToRemove, out failures))
                {
                    foreach (var kvp in failures)
                    {
                        Debug.LogWarning($"Cannot remove {kvp.Key?.ItemName ?? "null"}: {kvp.Value}");
                    }
                    return false;
                }
            }

            // Remove all items
            foreach (var kvp in itemsToRemove)
            {
                if (kvp.Key != null && kvp.Value > 0)
                {
                    RemoveItemInternal(kvp.Key, kvp.Value);
                }
            }

            // Send remove events for all items
            foreach (var kvp in itemsToRemove)
            {
                if (kvp.Key != null && !string.IsNullOrEmpty(kvp.Key.OnRemoveEvent))
                {
                    kvp.Key.OnRemoveEvent.SendEvent(kvp.Key);
                }
            }

            // Single save and notify
            SaveAndNotifyListeners();

            return true;
        }

        /// <summary>
        /// Moves multiple items from this inventory to the target in a single atomic transaction.
        /// </summary>
        /// <param name="itemsToMove">Dictionary of items and quantities to move</param>
        /// <param name="target">The target inventory</param>
        /// <param name="validateFirst">If true, validates all can be moved before proceeding</param>
        /// <returns>True if all items were moved successfully</returns>
        public bool MoveItemsBatch(Dictionary<ItemSO, int> itemsToMove, InventoryDefinition target, bool validateFirst = true)
        {
            if (itemsToMove == null || itemsToMove.Count == 0)
                return true;

            if (target == null)
            {
                Debug.LogError("Cannot move items batch: Target inventory is null");
                return false;
            }

            // Validate all operations
            foreach (var kvp in itemsToMove)
            {
                if (kvp.Key != null)
                {
                    string reason;
                    if (!CanMoveItem(kvp.Key, kvp.Value, target, out reason))
                    {
                        Debug.LogWarning($"Cannot move {kvp.Key.ItemName}: {reason}");
                        return false;
                    }
                }
            }

            // Perform the move
            foreach (var kvp in itemsToMove)
            {
                if (kvp.Key != null && kvp.Value > 0)
                {
                    RemoveItemInternal(kvp.Key, kvp.Value);
                    target.AddItemInternal(kvp.Key, kvp.Value);
                }
            }

            // Send events and save
            SaveAndNotifyListeners();
            target.SaveAndNotifyListeners();

            return true;
        }

        /// <summary>
        /// Internal method to add item without triggering save/notify (for batch operations).
        /// </summary>
        private void AddItemInternal(ItemSO item, int quantity)
        {
            // Find existing item
            int existingIndex = items.FindIndex(i => i.ItemReference == item);

            if (existingIndex >= 0)
            {
                // Item exists, update quantity
                ItemDefinition existing = items[existingIndex];
                int newQuantity = existing.Quantity + quantity;
                items[existingIndex] = existing.WithQuantity(newQuantity);
            }
            else
            {
                // New item, add to list
                items.Add(new ItemDefinition(item, quantity));
            }
        }

        /// <summary>
        /// Internal method to remove item without triggering save/notify (for batch operations).
        /// </summary>
        private void RemoveItemInternal(ItemSO item, int quantity)
        {
            int existingIndex = items.FindIndex(i => i.ItemReference == item);

            if (existingIndex < 0)
                return;

            ItemDefinition existing = items[existingIndex];
            int newQuantity = existing.Quantity - quantity;

            if (newQuantity <= 0)
            {
                // Remove item completely
                items.RemoveAt(existingIndex);
            }
            else
            {
                // Update quantity
                items[existingIndex] = existing.WithQuantity(newQuantity);
            }
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Saves the current inventory state to disk if persistent.
        /// Key format: "{id}_{itemName}" -> "{quantity}"
        /// </summary>
        public void SaveToDisk()
        {
            if (!isPersistent)
                return;

            try
            {
                // First, clear all old entries for this inventory
                var allData = GameSaving.LoadAllData(INVENTORY_FILE_NAME);
                var keysToDelete = new List<string>();
                
                foreach (var kvp in allData)
                {
                    if (kvp.Key.StartsWith($"{id}_"))
                    {
                        keysToDelete.Add(kvp.Key);
                    }
                }
                
                foreach (string key in keysToDelete)
                {
                    GameSaving.DeleteKey(key, INVENTORY_FILE_NAME);
                }

                // Now save each item
                foreach (var itemDef in items)
                {
                    if (itemDef.ItemReference != null)
                    {
                        string key = $"{id}_{itemDef.ItemReference.ItemName}";
                        int quantity = itemDef.Quantity;
                        
                        GameSaving.Save(key, quantity, INVENTORY_FILE_NAME);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Inventory] Failed to save inventory '{id}': {e.Message}");
            }
        }

        /// <summary>
        /// Loads the inventory state from disk if persistent.
        /// </summary>
        public void LoadFromSave()
        {
            if (!isPersistent)
                return;

            try
            {
                var allData = GameSaving.LoadAllData(INVENTORY_FILE_NAME);
                items.Clear();

                foreach (var kvp in allData)
                {
                    // Check if this key belongs to this inventory
                    if (!kvp.Key.StartsWith($"{id}_"))
                        continue;

                    // Parse key: "id_itemName"
                    string[] parts = kvp.Key.Substring(id.Length + 1).Split(new char[] { '_' }, 2);
                    if (parts.Length < 1)
                        continue;

                    string itemName = parts[0];
                    int quantity;

                    if (int.TryParse(kvp.Value, out quantity))
                    {
                        // Find the ItemSO by name
                        ItemSO itemSO = FindItemSOByName(itemName);
                        if (itemSO != null)
                        {
                            items.Add(new ItemDefinition(itemSO, quantity));
                        }
                        else
                        {
                            Debug.LogWarning($"[Inventory] Could not find ItemSO '{itemName}' for inventory '{id}'");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Inventory] Failed to load inventory '{id}': {e.Message}");
            }
        }

        /// <summary>
        /// Helper method to find ItemSO by name from the config.
        /// </summary>
        private ItemSO FindItemSOByName(string itemName)
        {
            try
            {
                var config = ConfigReader.GetConfig();
                if (config?.InventorySystem?.ItemReferences != null)
                {
                    foreach (var item in config.InventorySystem.ItemReferences)
                    {
                        if (item != null && item.ItemName == itemName)
                        {
                            return item;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Inventory] Error finding ItemSO '{itemName}': {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Saves to disk (if persistent) and notifies listeners of changes.
        /// Also updates the cache to ensure consistency.
        /// </summary>
        private void SaveAndNotifyListeners()
        {
            // Update the cache with this instance (important for shared references)
            if (runtimeCache.ContainsKey(id))
            {
                // Update the cache to point to this exact instance
                // This ensures all components share the same reference and see the latest changes
                runtimeCache[id] = this;
            }
            
            if (isPersistent)
            {
                SaveToDisk();
            }
            
            // Notify listeners via SIGS
            SIGS.Send(UpdateListenerString, this);
        }

        #endregion
    }
}
