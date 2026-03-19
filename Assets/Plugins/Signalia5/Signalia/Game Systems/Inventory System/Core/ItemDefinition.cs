using System;
using AHAKuo.Signalia.GameSystems.Inventory.Data;

namespace AHAKuo.Signalia.GameSystems.Inventory.Core
{
    /// <summary>
    /// A runtime immutable struct containing a reference to the item scriptable object and a quantity identifier.
    /// </summary>
    [Serializable]
    public struct ItemDefinition
    {
        private readonly ItemSO itemReference;
        private readonly int quantity;

        public ItemSO ItemReference => itemReference;
        public int Quantity => quantity;

        public ItemDefinition(ItemSO item, int quantity)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "ItemSO reference cannot be null.");
            
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));

            this.itemReference = item;
            this.quantity = quantity;
        }

        /// <summary>
        /// Creates a new ItemDefinition with updated quantity.
        /// </summary>
        public ItemDefinition WithQuantity(int newQuantity)
        {
            return new ItemDefinition(itemReference, newQuantity);
        }

        /// <summary>
        /// Creates a new ItemDefinition with quantity added.
        /// </summary>
        public ItemDefinition AddQuantity(int amount)
        {
            return new ItemDefinition(itemReference, quantity + amount);
        }

        /// <summary>
        /// Creates a new ItemDefinition with quantity subtracted.
        /// </summary>
        public ItemDefinition SubtractQuantity(int amount)
        {
            return new ItemDefinition(itemReference, Math.Max(0, quantity - amount));
        }
    }
}
