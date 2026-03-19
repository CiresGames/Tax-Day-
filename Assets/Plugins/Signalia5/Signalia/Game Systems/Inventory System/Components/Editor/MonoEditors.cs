using UnityEngine;
using UnityEditor;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.GameSystems.Inventory.Game;
using AHAKuo.Signalia.GameSystems.Inventory.UI;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.Framework.Editors;


namespace AHAKuo.Signalia.GameSystems.Inventory.Game.Editors
{
    /// <summary>
    /// Custom editor for GameItemGrid concrete component
    /// </summary>
    [CustomEditor(typeof(GameItemGrid))]
    [CanEditMultipleObjects]
    public class GameItemGridEditor : Editor
    {
        private SerializedProperty inventoryIDProp;
        private SerializedProperty itemSlotsProp;
        private SerializedProperty itemDisplayerPanelProp;
        private SerializedProperty slotsContainerProp;
        private SerializedProperty slotPrefabProp;
        private SerializedProperty initialSlotCountProp;
        private SerializedProperty enableCategoryFilterProp;
        private SerializedProperty categoryFilterProp;

        private void OnEnable()
        {
            inventoryIDProp = serializedObject.FindProperty("inventoryID");
            itemSlotsProp = serializedObject.FindProperty("itemSlots");
            itemDisplayerPanelProp = serializedObject.FindProperty("itemDisplayerPanel");
            slotsContainerProp = serializedObject.FindProperty("slotsContainer");
            slotPrefabProp = serializedObject.FindProperty("slotPrefab");
            initialSlotCountProp = serializedObject.FindProperty("initialSlotCount");
            enableCategoryFilterProp = serializedObject.FindProperty("enableCategoryFilter");
            categoryFilterProp = serializedObject.FindProperty("categoryFilter");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Simple concrete implementation of ItemGrid. Provides a ready-to-use grid UI component for displaying inventory items.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("📦 Inventory Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inventoryIDProp, new GUIContent("Inventory ID", "ID of the inventory to display"));

            GUILayout.Space(15);

            // Category Filtering Section
            EditorGUILayout.LabelField("🔍 Category Filtering", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Filter which items are displayed in this grid by category.", MessageType.Info);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(enableCategoryFilterProp, new GUIContent("Enable Filter", "Filter items by category"));
            
            if (enableCategoryFilterProp.boolValue)
            {
                EditorGUI.indentLevel++;
                
                // Show category dropdown if in play mode with connected inventory
                if (Application.isPlaying)
                {
                    GameItemGrid itemGrid = (GameItemGrid)target;
                    var inventory = itemGrid.GetConnectedInventory();
                    
                    if (inventory != null)
                    {
                        var categories = inventory.GetCategories();
                        string currentCategory = categoryFilterProp.stringValue;
                        
                        EditorGUILayout.LabelField("Available Categories", EditorStyles.boldLabel);
                        
                        if (categories.Count > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // Get current category index
                            int currentIndex = categories.IndexOf(currentCategory);
                            if (currentIndex < 0) currentIndex = 0;
                            
                            int selectedIndex = EditorGUILayout.Popup("Category:", currentIndex, categories.ToArray());
                            if (selectedIndex >= 0 && selectedIndex < categories.Count)
                            {
                                categoryFilterProp.stringValue = categories[selectedIndex];
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No categories found in inventory. All items will be shown.", MessageType.Info);
                        }
                    }
                }
                
                EditorGUILayout.PropertyField(categoryFilterProp, new GUIContent("Category Filter", "Category to filter by"));
                
                if (!string.IsNullOrEmpty(categoryFilterProp.stringValue))
                {
                    EditorGUILayout.HelpBox($"Showing only items with category: '{categoryFilterProp.stringValue}'", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Make auto-populate settings prominent and higher up
            EditorGUILayout.LabelField("⚙️ Auto-Populate Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Slots are auto-populated on Awake. Configure these settings to control slot generation.", MessageType.Info);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(slotsContainerProp, new GUIContent("Slots Container", "Parent transform for auto-populated slots"));
            
            if (slotsContainerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Slots Container is required for auto-population to work.", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(slotPrefabProp, new GUIContent("Slot Prefab", "Prefab to instantiate for each slot"));
            
            if (slotPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Slot Prefab is required for auto-population to work.", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(initialSlotCountProp, new GUIContent("Initial Slot Count", "Number of slots to create when auto-populating"));
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("🔲 Grid Components", EditorStyles.boldLabel);
            
            // Display slots as readonly/info
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Item Slots", "List of ItemSlot components in this grid (auto-populated)"), EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Count", itemSlotsProp.arraySize);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.PropertyField(itemDisplayerPanelProp, new GUIContent("Item Displayer Panel", "Panel that displays item details when a slot is selected"));

            GUILayout.Space(15);

            // Runtime Controls
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeControls()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);

            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("● PLAYING", EditorStyles.miniLabel);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.LabelField("● EDIT MODE", EditorStyles.miniLabel);
            }
            GUI.backgroundColor = originalColor;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test inventory operations!", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            GameItemGrid gameGrid = (GameItemGrid)target;

            // Force refresh button
            if (GUILayout.Button("🔄 Force Refresh Grid", GUILayout.Height(25)))
            {
                gameGrid.ForceRefreshGrid();
            }

            // Display info
            var inventory = gameGrid.GetConnectedInventory();
            
            if (inventory != null)
            {
                int itemCount = inventory.Items != null ? inventory.Items.Count : 0;
                int slotCount = gameGrid.GetItemSlots() != null ? gameGrid.GetItemSlots().Count : 0;
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("📋 Grid Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Items in Inventory:", itemCount.ToString());
                EditorGUILayout.LabelField("Slots Available:", slotCount.ToString());
                
                // Category filter info
                bool isFiltered = gameGrid.IsCategoryFilterEnabled();
                if (isFiltered)
                {
                    string filter = gameGrid.GetCategoryFilter();
                    EditorGUILayout.LabelField("Filter Active:", $"Category: {filter}");
                    
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button("🔍 View Filtered Items", GUILayout.Height(22)))
                    {
                        var filteredItems = inventory.GetItemsByCategory(filter);
                        Debug.Log($"Found {filteredItems.Count} items in category '{filter}'");
                        foreach (var item in filteredItems)
                        {
                            Debug.Log($"  - {item.ItemReference.ItemName} x{item.Quantity}");
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    
                    if (GUILayout.Button("❌ Clear Filter", GUILayout.Height(22)))
                    {
                        gameGrid.ClearCategoryFilter();
                        serializedObject.Update();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            if (string.IsNullOrEmpty(inventoryIDProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Inventory ID is required. Enter the ID of the inventory to display.", MessageType.Warning);
            }

            if (itemSlotsProp.arraySize == 0 && (slotPrefabProp.objectReferenceValue == null || slotsContainerProp.objectReferenceValue == null))
            {
                EditorGUILayout.HelpBox("⚠️ No item slots configured. Either manually assign slots or configure auto-population settings.", MessageType.Warning);
            }
        }
    }

    /// <summary>
    /// Custom editor for GameItemSlot concrete component
    /// </summary>
    [CustomEditor(typeof(GameItemSlot))]
    [CanEditMultipleObjects]
    public class GameItemSlotEditor : Editor
    {
        private SerializedProperty logClickEventsProp;
        private SerializedProperty iconImageProp;
        private SerializedProperty itemNameTextProp;
        private SerializedProperty quantityTextProp;
        private SerializedProperty descriptionTextProp;
        private SerializedProperty slotContainerProp;
        private SerializedProperty customPropertyViewsProp;
        
        private int selectedTab = 0;
        private readonly string[] tabNames = { "🎮 Settings", "🖼️ UI References", "⚙️ Custom Properties", "🎮 Runtime" };

        private void OnEnable()
        {
            logClickEventsProp = serializedObject.FindProperty("logClickEvents");
            iconImageProp = serializedObject.FindProperty("iconImage");
            itemNameTextProp = serializedObject.FindProperty("itemNameText");
            quantityTextProp = serializedObject.FindProperty("quantityText");
            descriptionTextProp = serializedObject.FindProperty("descriptionText");
            slotContainerProp = serializedObject.FindProperty("slotContainer");
            customPropertyViewsProp = serializedObject.FindProperty("customPropertyViews");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Simple concrete implementation of ItemSlot with basic click behavior. When clicked, it displays item details on the ItemDisplayerPanel.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            GUILayout.Space(5);

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0:
                    DrawSettingsTab();
                    break;
                case 1:
                    DrawUIReferencesTab();
                    break;
                case 2:
                    DrawCustomPropertiesTab();
                    break;
                case 3:
                    DrawRuntimeTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("🎮 Game Item Slot Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(logClickEventsProp, new GUIContent("Log Click Events", "Enable to log click events for debugging"));
        }
        
        private void DrawUIReferencesTab()
        {
            EditorGUILayout.LabelField("🖼️ UI References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(iconImageProp, new GUIContent("Icon Image", "Image component that displays the item icon"));
            EditorGUILayout.PropertyField(itemNameTextProp, new GUIContent("Item Name Text", "Text component that displays the item name"));
            EditorGUILayout.PropertyField(quantityTextProp, new GUIContent("Quantity Text", "Text component that displays the item quantity"));
            EditorGUILayout.PropertyField(descriptionTextProp, new GUIContent("Description Text", "Text component that displays the item description"));
            EditorGUILayout.PropertyField(slotContainerProp, new GUIContent("Slot Container", "GameObject that contains the slot visuals"));
        }
        
        private void DrawCustomPropertiesTab()
        {
            EditorGUILayout.LabelField("⚙️ Custom Properties", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure custom property views to automatically display ItemSO custom properties in UI text fields.", MessageType.Info);
            EditorGUILayout.PropertyField(customPropertyViewsProp, new GUIContent("Custom Property Views", "Bind custom property names to TMP_Text fields for automatic display"), true);
        }
        
        private void DrawRuntimeTab()
        {
            DrawRuntimeControls();
        }

        private void DrawRuntimeControls()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);

            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("● PLAYING", EditorStyles.miniLabel);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.LabelField("● EDIT MODE", EditorStyles.miniLabel);
            }
            GUI.backgroundColor = originalColor;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test slot interactions!", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            GameItemSlot gameSlot = (GameItemSlot)target;

            // Display info
            var currentItem = gameSlot.GetCurrentItem();
            bool isActive = gameSlot.IsActive();

            GUILayout.Space(5);
            EditorGUILayout.LabelField("📋 Slot Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Is Active:", isActive ? "Yes" : "No");

            if (isActive && currentItem.ItemReference != null)
            {
                EditorGUILayout.LabelField("Item Name:", currentItem.ItemReference.ItemName);
                EditorGUILayout.LabelField("Quantity:", currentItem.Quantity.ToString());
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("🎮 Actions", EditorStyles.boldLabel);
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("✨ Use Item", GUILayout.Height(25)))
                {
                    gameSlot.UseItemInSlot();
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("🗑️ Discard Item", GUILayout.Height(25)))
                {
                    gameSlot.DiscardItemInSlot();
                }
                GUI.backgroundColor = originalColor;
            }
            else
            {
                EditorGUILayout.HelpBox("Slot is empty.", MessageType.Info);
            }

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            if (iconImageProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Icon Image is not assigned. Item icons will not be displayed.", MessageType.Warning);
            }

            if (slotContainerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Slot Container is not assigned. Slot visibility toggling will not work.", MessageType.Warning);
            }
        }
    }

    /// <summary>
    /// Custom editor for GameInventoryTransferral component
    /// </summary>
    [CustomEditor(typeof(GameInventoryTransferral))]
    [CanEditMultipleObjects]
    public class GameInventoryTransferralEditor : Editor
    {
        private SerializedProperty sourceInventoryIDProp;
        private SerializedProperty targetInventoryIDProp;
        private SerializedProperty sourceInventoryReferenceProp;
        private SerializedProperty targetInventoryReferenceProp;

        private void OnEnable()
        {
            sourceInventoryIDProp = serializedObject.FindProperty("sourceInventoryID");
            targetInventoryIDProp = serializedObject.FindProperty("targetInventoryID");
            sourceInventoryReferenceProp = serializedObject.FindProperty("sourceInventoryReference");
            targetInventoryReferenceProp = serializedObject.FindProperty("targetInventoryReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //// Header image
            //GUILayout.Label(GraphicLoader.InventoryHeader, GUILayout.Height(150));

            EditorGUILayout.HelpBox("MonoBehaviour component for transferring items between inventories. Searches for inventories by ID in the scene. Mainly a helper.", MessageType.Info);

            GUILayout.Space(10);

            DrawErrorWarnings();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("📥 Source Inventory", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourceInventoryIDProp, new GUIContent("Source Inventory ID", "ID of the source inventory"));

            GUILayout.Space(5);

            EditorGUILayout.LabelField("📤 Target Inventory", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetInventoryIDProp, new GUIContent("Target Inventory ID", "ID of the target inventory"));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("⚙️ Editor Helper (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourceInventoryReferenceProp, new GUIContent("Source Inventory Reference", "Drag a GameInventory here to auto-populate the ID"));
            EditorGUILayout.PropertyField(targetInventoryReferenceProp, new GUIContent("Target Inventory Reference", "Drag a GameInventory here to auto-populate the ID"));

            GUILayout.Space(15);

            // Runtime Controls
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeControls()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);

            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎮 Transfer Operations", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("● PLAYING", EditorStyles.miniLabel);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.LabelField("● EDIT MODE", EditorStyles.miniLabel);
            }
            GUI.backgroundColor = originalColor;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Transfer operations are only available during play mode.", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            GameInventoryTransferral transferral = (GameInventoryTransferral)target;

            // Update IDs button
            if (GUILayout.Button("🔧 Update IDs from References", GUILayout.Height(25)))
            {
                transferral.UpdateIDsFromReferences();
                EditorUtility.SetDirty(transferral);
            }

            // Validation
            var sourceInventory = transferral.GetSourceInventory();
            var targetInventory = transferral.GetTargetInventory();

            if (string.IsNullOrEmpty(transferral.SourceInventoryID) || string.IsNullOrEmpty(transferral.TargetInventoryID))
            {
                EditorGUILayout.HelpBox("⚠️ Inventory IDs are not set.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            if (sourceInventory == null || targetInventory == null)
            {
                EditorGUILayout.HelpBox("⚠️ Source and/or Target inventories not found in scene.", MessageType.Warning);
                GUILayout.EndVertical();
                return;
            }

            // Show transfer options
            GUILayout.Space(5);
            EditorGUILayout.LabelField("📋 Available Items in Source", EditorStyles.boldLabel);

            // Create a copy of the items list to avoid "collection was modified" errors during iteration
            var itemsList = sourceInventory.Items;
            if (itemsList != null && itemsList.Count > 0)
            {
                EditorGUI.indentLevel++;
                
                // Convert to array to avoid modification during iteration
                var itemsArray = new ItemDefinition[itemsList.Count];
                itemsList.CopyTo(itemsArray, 0);
                
                foreach (var itemDef in itemsArray)
                {
                    if (itemDef.ItemReference != null)
                    {
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField($"{itemDef.ItemReference.ItemName}: x{itemDef.Quantity}", GUILayout.Width(200));
                        
                        GUI.backgroundColor = Color.cyan;
                        if (GUILayout.Button("Transfer All", GUILayout.Width(90)))
                        {
                            transferral.TransferAllOfItem(itemDef.ItemReference);
                            EditorUtility.SetDirty(transferral);
                        }
                        
                        GUI.backgroundColor = Color.yellow;
                        if (GUILayout.Button("Transfer Half", GUILayout.Width(90)))
                        {
                            transferral.TransferHalfOfItem(itemDef.ItemReference);
                            EditorUtility.SetDirty(transferral);
                        }
                        
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button("x1", GUILayout.Width(40)))
                        {
                            transferral.TransferItem(itemDef.ItemReference, 1);
                            EditorUtility.SetDirty(transferral);
                        }
                        GUI.backgroundColor = originalColor;
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Source inventory has no items to transfer.", MessageType.Info);
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField("🚀 Quick Actions", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Transfer ALL Items", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Transfer All Items", 
                    $"Transfer ALL items from source to target inventory?\n\nSource: {transferral.SourceInventoryID}\nTarget: {transferral.TargetInventoryID}",
                    "Yes", "No"))
                {
                    transferral.TransferAllItems();
                    EditorUtility.SetDirty(transferral);
                }
            }
            GUI.backgroundColor = originalColor;

            GUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            GameInventoryTransferral transferral = (GameInventoryTransferral)target;

            if (string.IsNullOrEmpty(sourceInventoryIDProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Source inventory ID is required. Enter an inventory ID or drag a GameInventory into the helper field.", MessageType.Warning);
            }

            if (string.IsNullOrEmpty(targetInventoryIDProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Target inventory ID is required. Enter an inventory ID or drag a GameInventory into the helper field.", MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(sourceInventoryIDProp.stringValue) && 
                !string.IsNullOrEmpty(targetInventoryIDProp.stringValue) &&
                sourceInventoryIDProp.stringValue == targetInventoryIDProp.stringValue)
            {
                EditorGUILayout.HelpBox("⚠️ Source and Target have the same ID. Transfer operations will have no effect.", MessageType.Warning);
            }
        }
    }

    /// <summary>
    /// Custom editor for GameInventory component
    /// </summary>
    [CustomEditor(typeof(GameInventory))]
    [CanEditMultipleObjects]
    public class GameInventoryEditor : Editor
    {
        private SerializedProperty inventoryIDProp;
        private SerializedProperty autoGenerateIDProp;
        private SerializedProperty idPrefixProp;
        private SerializedProperty initializeOnAwakeProp;
        private SerializedProperty persistentProp;
        private SerializedProperty saveOnDestroyProp;

        private void OnEnable()
        {
            inventoryIDProp = serializedObject.FindProperty("inventoryID");
            autoGenerateIDProp = serializedObject.FindProperty("autoGenerateID");
            idPrefixProp = serializedObject.FindProperty("idPrefix");
            initializeOnAwakeProp = serializedObject.FindProperty("initializeOnAwake");
            persistentProp = serializedObject.FindProperty("persistent");
            saveOnDestroyProp = serializedObject.FindProperty("saveOnDestroy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Force repaint in play mode to show updated data
            if (Application.isPlaying)
            {
                Repaint();
            }

            EditorGUILayout.HelpBox("Simple MonoBehaviour implementation of the inventory system. Provides an easy gateway for users to immediately start working with the inventory framework.", MessageType.Info);

            GUILayout.Space(10);

            EditorGUILayout.LabelField("📦 Inventory Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inventoryIDProp, new GUIContent("Inventory ID", "Unique identifier for this inventory"));

            if (string.IsNullOrEmpty(inventoryIDProp.stringValue) && !autoGenerateIDProp.boolValue)
            {
                EditorGUILayout.HelpBox("Inventory ID is empty. Enable Auto-Generate ID or assign a custom ID.", MessageType.Warning);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("🔧 Auto-Generate Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoGenerateIDProp, new GUIContent("Auto-Generate ID", "Automatically generate a unique ID on Awake"));
            
            if (autoGenerateIDProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(idPrefixProp, new GUIContent("ID Prefix", "Prefix for the generated ID"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(initializeOnAwakeProp, new GUIContent("Initialize On Awake", "Automatically initialize the inventory when the component awakes"));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("💾 Persistence Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(persistentProp, new GUIContent("Persistent", "When enabled, this inventory will save and load automatically"));
            EditorGUILayout.PropertyField(saveOnDestroyProp, new GUIContent("Save On Destroy", "When enabled, this inventory will save when the component is destroyed"));

            if (persistentProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("This inventory will be saved and loaded with the game save system.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("This inventory will NOT be saved. Perfect for temporary or session-specific inventories.", MessageType.Warning);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(15);

            // Runtime Controls
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeControls()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);

            GUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🎮 Runtime Controls", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.LabelField("● PLAYING", EditorStyles.miniLabel);
            }
            else
            {
                GUI.backgroundColor = Color.gray;
                EditorGUILayout.LabelField("● EDIT MODE", EditorStyles.miniLabel);
            }
            GUI.backgroundColor = originalColor;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test inventory operations!", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            GameInventory gameInventory = (GameInventory)target;

            // Generate ID button
            if (GUILayout.Button("🆔 Generate Inventory ID", GUILayout.Height(25)))
            {
                gameInventory.GenerateInventoryID();
                EditorUtility.SetDirty(gameInventory);
            }

            // Initialize button
            if (GUILayout.Button("🔧 Initialize Inventory", GUILayout.Height(25)))
            {
                gameInventory.InitializeInventory();
                EditorUtility.SetDirty(gameInventory);
            }

            GUILayout.Space(5);

            // Display inventory info
            if (gameInventory.Inventory != null)
            {
                EditorGUILayout.LabelField("📋 Inventory Info", EditorStyles.boldLabel);
                
                int itemCount = gameInventory.Inventory.Items != null ? gameInventory.Inventory.Items.Count : 0;
                EditorGUILayout.LabelField("Item Count:", itemCount.ToString());
                EditorGUILayout.LabelField("Inventory ID:", gameInventory.InventoryID);
                EditorGUILayout.LabelField("Is Persistent:", gameInventory.IsPersistent ? "Yes ✓" : "No ✗");
                EditorGUILayout.LabelField("Will Save On Destroy:", gameInventory.WillSaveOnDestroy ? "Yes" : "No");
                
                GUILayout.Space(5);
                
                // Display all items in the inventory
                if (itemCount > 0)
                {
                    EditorGUILayout.LabelField("📦 Inventory Contents", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    var itemsList = gameInventory.Inventory.Items;
                    if (itemsList != null)
                    {
                        var itemsArray = new ItemDefinition[itemsList.Count];
                        itemsList.CopyTo(itemsArray, 0);
                        
                        foreach (var itemDef in itemsArray)
                        {
                            if (itemDef.ItemReference != null)
                            {
                                EditorGUILayout.BeginHorizontal("box");
                                EditorGUILayout.LabelField($"📦 {itemDef.ItemReference.ItemName}", GUILayout.Width(150));
                                EditorGUILayout.LabelField($"x{itemDef.Quantity}", GUILayout.Width(50));
                                
                                if (!string.IsNullOrEmpty(itemDef.ItemReference.Description))
                                {
                                    EditorGUILayout.LabelField(itemDef.ItemReference.Description);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                    GUILayout.Space(5);
                }
                else
                {
                    EditorGUILayout.LabelField("📦 Inventory Contents", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("No items in inventory.", MessageType.Info);
                    GUILayout.Space(5);
                }
                
                GUI.backgroundColor = originalColor;
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("🧪 Testing Tools", EditorStyles.boldLabel);
                
                if (GUILayout.Button("➕ Add Item", GUILayout.Height(22)))
                {
                    ShowAddItemDialog(gameInventory);
                }
                if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(22)))
                {
                    if (EditorUtility.DisplayDialog("Clear Inventory", 
                        $"Are you sure you want to clear all items from this inventory?", 
                        "Yes", "No"))
                    {
                        gameInventory.ClearInventory();
                        EditorUtility.SetDirty(gameInventory);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Inventory not initialized yet. Click 'Initialize Inventory' to begin.", MessageType.Warning);
            }

            GUILayout.EndVertical();
        }

        private void ShowAddItemDialog(GameInventory inventory)
        {
            // Create a simple dialog to add test items
            GenericMenu menu = new GenericMenu();
            
            // Get all ItemSO assets in the project
            string[] guids = AssetDatabase.FindAssets("t:ItemSO");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Items Found", 
                    "No ItemSO assets found in the project. Create some items first.", 
                    "OK");
                return;
            }
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                
                if (item != null)
                {
                    menu.AddItem(new GUIContent($"Add {item.ItemName} x1"), false, () => {
                        inventory.AddItem(item, 1);
                        EditorUtility.SetDirty(inventory);
                    });
                    menu.AddItem(new GUIContent($"Add {item.ItemName} x10"), false, () => {
                        inventory.AddItem(item, 10);
                        EditorUtility.SetDirty(inventory);
                    });
                    menu.AddItem(new GUIContent($"Add {item.ItemName} x100"), false, () => {
                        inventory.AddItem(item, 100);
                        EditorUtility.SetDirty(inventory);
                    });
                }
            }
            
            menu.ShowAsContext();
        }
    }

    /// <summary>
    /// Custom editor for GameItemDisplayer concrete component
    /// </summary>
    [CustomEditor(typeof(GameItemDisplayer))]
    [CanEditMultipleObjects]
    public class GameItemDisplayerEditor : Editor
    {
        private SerializedProperty uiViewProp;
        private SerializedProperty panelContainerProp;
        private SerializedProperty detailedIconProp;
        private SerializedProperty detailedItemNameProp;
        private SerializedProperty detailedDescriptionProp;
        private SerializedProperty detailedCategoryProp;
        private SerializedProperty detailedQuantityProp;
        private SerializedProperty maxStackSizeTextProp;
        private SerializedProperty isUsableTextProp;
        private SerializedProperty isDiscardableTextProp;
        private SerializedProperty customPropertyViewsProp;
        private SerializedProperty stickyDisplayerProp;
        private SerializedProperty hideOnStartProp;
        private SerializedProperty logDisplayEventsProp;
        private SerializedProperty useButtonProp;
        private SerializedProperty discardButtonProp;
        private SerializedProperty closeButtonProp;
        
        private int selectedTab = 0;
        private readonly string[] tabNames = { "🎮 Settings", "🔲 Panel", "📋 Item Fields", "⚙️ Custom Properties", "🎯 Actions", "🎮 Runtime" };

        private void OnEnable()
        {
            uiViewProp = serializedObject.FindProperty("uiView");
            panelContainerProp = serializedObject.FindProperty("panelContainer");
            detailedIconProp = serializedObject.FindProperty("detailedIcon");
            detailedItemNameProp = serializedObject.FindProperty("detailedItemName");
            detailedDescriptionProp = serializedObject.FindProperty("detailedDescription");
            detailedCategoryProp = serializedObject.FindProperty("detailedCategory");
            detailedQuantityProp = serializedObject.FindProperty("detailedQuantity");
            maxStackSizeTextProp = serializedObject.FindProperty("maxStackSizeText");
            isUsableTextProp = serializedObject.FindProperty("isUsableText");
            isDiscardableTextProp = serializedObject.FindProperty("isDiscardableText");
            customPropertyViewsProp = serializedObject.FindProperty("customPropertyViews");
            stickyDisplayerProp = serializedObject.FindProperty("stickyDisplayer");
            hideOnStartProp = serializedObject.FindProperty("hideOnStart");
            logDisplayEventsProp = serializedObject.FindProperty("logDisplayEvents");
            useButtonProp = serializedObject.FindProperty("useButton");
            discardButtonProp = serializedObject.FindProperty("discardButton");
            closeButtonProp = serializedObject.FindProperty("closeButton");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Simple concrete implementation of ItemDisplayerPanel. Provides a ready-to-use detailed item display panel with optional action buttons.", MessageType.Info);
            GUILayout.Space(10);

            DrawErrorWarnings();
            GUILayout.Space(5);

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            GUILayout.Space(10);

            switch (selectedTab)
            {
                case 0: DrawSettingsTab(); break;
                case 1: DrawPanelTab(); break;
                case 2: DrawItemFieldsTab(); break;
                case 3: DrawCustomPropertiesTab(); break;
                case 4: DrawActionsTab(); break;
                case 5: DrawRuntimeTab(); break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("🎮 Game Item Displayer Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hideOnStartProp, new GUIContent("Hide On Start", "Hide the panel when the game starts"));
            EditorGUILayout.PropertyField(logDisplayEventsProp, new GUIContent("Log Display Events", "Enable to log display events for debugging"));
        }

        private void DrawPanelTab()
        {
            EditorGUILayout.LabelField("🔲 Panel Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("Assign a UIView to use the Signalia UI animation system for showing/hiding the panel. If no UIView is assigned, the legacy panel container will be used instead.", MessageType.Info);
            
            GUILayout.Space(5);
            
            EditorGUILayout.PropertyField(uiViewProp, new GUIContent("UI View", "UIView component that controls the panel visibility and animations"));
            
            if (uiViewProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("💡 Tip: Assigning a UIView enables professional show/hide animations. If not assigned, the legacy panel container method will be used.", MessageType.Info);
                
                GUILayout.Space(10);
                EditorGUILayout.LabelField("⚠️ Legacy: Panel Container (Fallback)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(panelContainerProp, new GUIContent("Panel Container", "GameObject that contains the panel visuals (used when UIView is not assigned)"));
                
                if (panelContainerProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Neither UIView nor Panel Container is assigned. Panel visibility toggling will not work.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Using UIView system for panel animations.", MessageType.Info);
                EditorGUILayout.HelpBox("⚠️ IMPORTANT: When using UIView:\n1. Ensure both Show Animation and Hide Animation assets have 'Don't Use Source' enabled.\n2. Disable 'Play Only When Changing Status' option on the UIView to allow animations during status transitions.", MessageType.Warning);
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Display Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stickyDisplayerProp, new GUIContent("Sticky Displayer", "When enabled, the display panel will keep showing the last previewed item even if no item is currently selected. When disabled, the panel will hide when no items are selected."));
        }

        private void DrawItemFieldsTab()
        {
            EditorGUILayout.LabelField("📋 Item Display Fields", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(detailedIconProp, new GUIContent("Detailed Icon", "Image component for the item icon"));
            EditorGUILayout.PropertyField(detailedItemNameProp, new GUIContent("Detailed Item Name", "Text component for the item name"));
            EditorGUILayout.PropertyField(detailedDescriptionProp, new GUIContent("Detailed Description", "Text component for the item description"));
            EditorGUILayout.PropertyField(detailedCategoryProp, new GUIContent("Detailed Category", "Text component for the item category"));
            EditorGUILayout.PropertyField(detailedQuantityProp, new GUIContent("Detailed Quantity", "Text component for the item quantity"));
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("📊 Item Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxStackSizeTextProp, new GUIContent("Max Stack Size Text", "Text component for max stack size"));
            EditorGUILayout.PropertyField(isUsableTextProp, new GUIContent("Is Usable Text", "Text component for usable status"));
            EditorGUILayout.PropertyField(isDiscardableTextProp, new GUIContent("Is Discardable Text", "Text component for discardable status"));
        }

        private void DrawCustomPropertiesTab()
        {
            EditorGUILayout.LabelField("⚙️ Custom Properties", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure custom property views to automatically display ItemSO custom properties in UI text fields.", MessageType.Info);
            EditorGUILayout.PropertyField(customPropertyViewsProp, new GUIContent("Custom Property Views", "Bind custom property names to TMP_Text fields for automatic display"), true);
        }

        private void DrawActionsTab()
        {
            EditorGUILayout.LabelField("🎯 Action Buttons", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Optional action buttons for using, discarding, and closing the panel.", MessageType.Info);
            
            EditorGUILayout.PropertyField(useButtonProp, new GUIContent("Use Button", "Button that triggers item use"));
            EditorGUILayout.PropertyField(discardButtonProp, new GUIContent("Discard Button", "Button that triggers item discard"));
            EditorGUILayout.PropertyField(closeButtonProp, new GUIContent("Close Button", "Button that closes the panel"));
        }

        private void DrawRuntimeTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime information is only available during play mode. Press Play to test panel interactions!", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("📋 Runtime Info", EditorStyles.boldLabel);
            
            GameItemDisplayer panel = (GameItemDisplayer)target;
            var currentItem = panel.GetCurrentDisplayedItem();
            
            if (currentItem.ItemReference != null)
            {
                EditorGUILayout.LabelField("Currently Displaying:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Item Name:", currentItem.ItemReference.ItemName);
                EditorGUILayout.LabelField("Quantity:", currentItem.Quantity.ToString());
                EditorGUILayout.LabelField("Category:", currentItem.ItemReference.Category ?? "None");
                EditorGUILayout.LabelField("Usable:", currentItem.ItemReference.IsUsable ? "Yes" : "No");
                EditorGUILayout.LabelField("Discardable:", currentItem.ItemReference.IsDiscardable ? "Yes" : "No");
            }
            else
            {
                EditorGUILayout.HelpBox("No item currently displayed.", MessageType.Info);
            }
        }

        private void DrawErrorWarnings()
        {
            if (uiViewProp.objectReferenceValue == null && panelContainerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Neither UIView nor Panel Container is assigned. Panel visibility toggling will not work.", MessageType.Warning);
            }
        }
    }
}
