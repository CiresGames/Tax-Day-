using UnityEngine;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Game;
using UnityEditor;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.GameSystems.InlineScript;
using System;
using Object = UnityEngine.Object;
using static AHAKuo.Signalia.GameSystems.Inventory.Data.ItemSO;

namespace AHAKuo.Signalia.GameSystems.Inventory.Data
{
    /// <summary>
    /// A scriptable data asset with extensive modularity for items.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Signalia/Game Systems/Inventory System/Item")]
    public class ItemSO : ScriptableObject
    {
        [SerializeField] private string itemName;
        [SerializeField, TextArea(3, 6)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private string category;

        [Tooltip("Maximum items that can be stacked in a single visual slot (e.g., stack of arrows = 99). When exceeded, creates new stacks.")]
        [SerializeField] private int maxStackSize = 99;
        
        [Tooltip("Maximum total quantity of this item allowed across all stacks in an inventory (e.g., max of 999 total arrows). Used by AddItem to cap total quantity.")]
        [SerializeField] private int maxQuantity = 999;

        [SerializeField] private bool isUsable = false;
        [SerializeField] private bool isDiscardable = true;

        /// <summary>
        /// Enum for specifying the type of a custom property
        /// </summary>
        public enum PropertyType
        {
            Int,
            Float,
            String
        }

        /// <summary>
        /// Struct for storing custom properties on items.
        /// Allows developers to specify custom attributes like item price, rarity, health restoration, etc.
        /// 
        /// Example usage:
        /// <code>
        /// // In an event listener that receives an ItemSO parameter:
        /// int itemPrice = item.GetCustomPropertyInt("Price", 0);
        /// float healthRestored = item.GetCustomPropertyFloat("HealthRestored", 0f);
        /// string rarity = item.GetCustomPropertyString("Rarity", "Common");
        /// 
        /// // Or access all properties:
        /// foreach (var property in item.CustomProperties)
        /// {
        ///     Debug.Log($"{property.PropertyName}: {property.GetValue()}");
        /// }
        /// </code>
        /// </summary>
        [Serializable]
        public struct CustomProperty
        {
            [SerializeField] private string propertyName;
            [SerializeField] private PropertyType propertyType;
            [SerializeField] private int intValue;
            [SerializeField] private float floatValue;
            [SerializeField] private string stringValue;

            public readonly string PropertyName => propertyName;
            public readonly PropertyType Type => propertyType;

            /// <summary>
            /// Gets the property value as an integer.
            /// </summary>
            public readonly int GetIntValue() => intValue;

            /// <summary>
            /// Gets the property value as a float.
            /// </summary>
            public readonly float GetFloatValue() => floatValue;

            /// <summary>
            /// Gets the property value as a string.
            /// </summary>
            public readonly string GetStringValue() => stringValue;

            /// <summary>
            /// Gets the property value as an object based on the property type.
            /// </summary>
            public readonly object GetValue()
            {
                return propertyType switch
                {
                    PropertyType.Int => intValue,
                    PropertyType.Float => floatValue,
                    PropertyType.String => stringValue,
                    _ => null
                };
            }
        }

        [SerializeField, Tooltip("Custom properties that can be used to define item-specific attributes like price, rarity, health restored, etc.")]
        private CustomProperty[] customProperties = new CustomProperty[0];

        public enum UsePipeline
        {
            None,
            DefaultEvent,
            CustomEvent,
            CustomBehaviorObject
        }

        [Serializable]
        private struct UsagePipe
        {
            // usage pipelines
            [SerializeField] private UsePipeline usePipeline;
            [SerializeField] private string useEvent; // Event to send when item is used (if pipeline includes Event). Sends this item as parameter.
            [SerializeField] private InlineVoid behaviorCode; // Custom InlineVoid script to execute when item is used (if pipeline includes CustomBehaviorObject).
            [SerializeField] private InlineBoolFunction conditionCode; // Custom InlineBoolFunction script to check if item can be used (if passCondition is true).
            [SerializeField] private bool executeBehavior; // If true, executes the behaviorCode inline script.
            [SerializeField] private bool passCondition; // If true, checks conditionCode before allowing use. If fails, sends conditionFailEvent.
            [SerializeField] private string conditionFailEvent; // Event to send if condition check fails. Sends 'this' item as parameter.

            /// <summary>
            /// Executes this usage pipeline for the given item.
            /// </summary>
            /// <param name="item">The item that is being used</param>
            public void Perform(ItemSO item)
            {
                if (item == null) return;

                // Check if condition checking is enabled and validate
                if (passCondition && conditionCode != null)
                {
                    try
                    {
                        // Evaluate the inline condition script (returns bool)
                        bool canUse = conditionCode.Value;

                        if (!canUse)
                        {
                            // Condition check failed - send failure event and abort
                            if (!string.IsNullOrEmpty(conditionFailEvent))
                            {
                                conditionFailEvent.SendEvent(item);
                            }
                            return;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Item {item.ItemName} condition code evaluation failed: {e.Message}");
                        // On error, abort use and send failure event if configured
                        if (!string.IsNullOrEmpty(conditionFailEvent))
                        {
                            conditionFailEvent.SendEvent(item);
                        }
                        return;
                    }
                }

                // Execute the configured pipeline
                switch (usePipeline)
                {
                    case UsePipeline.None:
                        // No pipeline configured - do nothing
                        break;

                    case UsePipeline.DefaultEvent:
                        // Send the default use event with this item as parameter
                        string defaultEvent = item.DefaultUseEvent;
                        if (!string.IsNullOrEmpty(defaultEvent))
                        {
                            defaultEvent.SendEvent(item);
                        }
                        break;

                    case UsePipeline.CustomEvent:
                        // Send the custom use event with this item as parameter
                        if (!string.IsNullOrEmpty(useEvent))
                        {
                            useEvent.SendEvent(item);
                        }
                        break;

                    case UsePipeline.CustomBehaviorObject:
                        // Execute custom behavior logic
                        ExecuteCustomBehavior(item);
                        break;
                }
            }

            /// <summary>
            /// Executes custom behavior from the configured inline script.
            /// </summary>
            private void ExecuteCustomBehavior(ItemSO item)
            {
                if (behaviorCode == null) return;

                if (executeBehavior)
                {
                    try
                    {
                        // Execute the inline script behavior
                        behaviorCode.ExecuteNonMono();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Item {item.ItemName} behavior code execution failed: {e.Message}");
                    }
                }
            }
        }

        [SerializeField, Tooltip("List of usage pipelines to execute when this item is used")]
        private UsagePipe[] usagePipes = new UsagePipe[0];

        [SerializeField, Tooltip("Audio to play when this item is used")]
        private string useAudio;
        [SerializeField, Tooltip("Audio to play when this item is added to inventory")]
        private string addAudio;
        [SerializeField, Tooltip("Audio to play when this item is removed from inventory")]
        private string removeAudio;

        /// signalia events on add and remove
        [SerializeField, Tooltip("Event to send when this item is added. Can be a notification pipeline or what-have-you. Sends this item as parameter.")]
        private string onAddEvent;
        [SerializeField, Tooltip("Event to send when this item is removed. Can be a notification pipeline or what-have-you. Sends this item as parameter.")]
        private string onRemoveEvent;

        // Public accessors :: kinda boiler plate, but I need it like this so the item data is read-only
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public string Category => category;
        public int MaxStackSize => maxStackSize;
        public int MaxQuantity => maxQuantity;
        public bool IsUsable => isUsable;
        public bool IsDiscardable => isDiscardable;
        public string UseAudio => useAudio;
        public string AddAudio => addAudio;
        public string RemoveAudio => removeAudio;
        public string OnAddEvent => onAddEvent;
        public string OnRemoveEvent => onRemoveEvent;
        public CustomProperty[] CustomProperties => customProperties;

        /// default event, based on item name
        public string DefaultUseEvent => $"OnUse: '{itemName.Replace(" ", "_")}'";

        /// <summary>
        /// Gets a custom property by name.
        /// </summary>
        /// <param name="propertyName">The name of the property to find</param>
        /// <param name="property">The found property (if any)</param>
        /// <returns>True if the property was found, false otherwise</returns>
        public bool TryGetCustomProperty(string propertyName, out CustomProperty property)
        {
            if (customProperties != null)
            {
                foreach (var prop in customProperties)
                {
                    if (prop.PropertyName == propertyName)
                    {
                        property = prop;
                        return true;
                    }
                }
            }

            property = default;
            return false;
        }

        /// <summary>
        /// Gets a custom property value as an int.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="defaultValue">Default value if property not found or wrong type</param>
        /// <returns>The property value or default value</returns>
        public int GetCustomPropertyInt(string propertyName, int defaultValue = 0)
        {
            if (TryGetCustomProperty(propertyName, out var property) && property.Type == PropertyType.Int)
            {
                return property.GetIntValue();
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a custom property value as a float.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="defaultValue">Default value if property not found or wrong type</param>
        /// <returns>The property value or default value</returns>
        public float GetCustomPropertyFloat(string propertyName, float defaultValue = 0f)
        {
            if (TryGetCustomProperty(propertyName, out var property) && property.Type == PropertyType.Float)
            {
                return property.GetFloatValue();
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a custom property value as a string.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="defaultValue">Default value if property not found or wrong type</param>
        /// <returns>The property value or default value</returns>
        public string GetCustomPropertyString(string propertyName, string defaultValue = "")
        {
            if (TryGetCustomProperty(propertyName, out var property) && property.Type == PropertyType.String)
            {
                return property.GetStringValue();
            }
            return defaultValue;
        }

        /// <summary>
        /// Handles item usage logic based on the configured pipelines.
        /// Supports multiple pipelines: Event-based, InlineScript behavior, or a combination of both.
        /// Each pipeline in the list is executed in order.
        /// </summary>
        public virtual void UseItem()
        {
            // Execute each usage pipeline in the list
            if (usagePipes != null && usagePipes.Length > 0)
            {
                foreach (var pipe in usagePipes)
                {
                    pipe.Perform(this);
                }
            }
        }

        /// <summary>
        /// Consumes this item from the source inventory.
        /// Calls UseItem and then removes the item from inventory.
        /// </summary>
        /// <param name="sourceInventory">The inventory to consume from</param>
        public void ConsumeItem(InventoryDefinition sourceInventory)
        {
            if (sourceInventory == null)
            {
                Debug.LogWarning($"Cannot consume item {itemName}: Source inventory is null.");
                return;
            }

            // First use the item
            UseItem();

            // Then remove it from the inventory
            try
            {
                sourceInventory.RemoveItem(this, 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to consume item {itemName}: {e.Message}");
            }
        }

        #region Editor-Only Initialization

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to initialize item data. Used for generating stock assets.
        /// </summary>
        public void SetItemData(string name, string desc, string cat, int maxStack, int maxQty, bool usable, bool discardable)
        {
            itemName = name;
            description = desc;
            category = cat;
            maxStackSize = maxStack;
            maxQuantity = maxQty;
            isUsable = usable;
            isDiscardable = discardable;
        }
#endif

        #endregion
    }
}
