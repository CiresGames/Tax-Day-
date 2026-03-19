using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Inventory.Examples
{
    public class InventoryRandomItemAdder : MonoBehaviour
    {
        private ItemSO[] items;
        [SerializeField] private string targetId;

        private void Awake()
        {
            items = ConfigReader.GetConfig().InventorySystem.ItemReferences;
            SIGS.Listener("Add Random Item", OnAddRandomItem);
        }

        private void OnAddRandomItem()
        {
            if (items.Length == 0) return;
            int randomIndex = Random.Range(0, items.Length);
            ItemSO randomItem = items[randomIndex];
            var inventory = InventoryDefinition.GetOrCreateInventory(targetId);
            inventory.AddItem(randomItem, 1);
        }
    }
}
