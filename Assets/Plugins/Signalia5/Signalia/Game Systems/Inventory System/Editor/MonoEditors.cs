#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.GameSystems.Inventory.UI;
using AHAKuo.Signalia.GameSystems.Inventory.Core;
using AHAKuo.Signalia.GameSystems.Inventory.Data;
using AHAKuo.Signalia.Framework.Editors;

namespace AHAKuo.Signalia.GameSystems.Inventory.Editors
{
    /// <summary>
    /// Custom editor for ItemGrid abstract component
    /// </summary>
    [CustomEditor(typeof(ItemGrid))]
    [CanEditMultipleObjects]
    public class ItemGridEditor : Editor
    {
        private SerializedProperty inventoryIDProp;
        private SerializedProperty itemSlotsProp;
        private SerializedProperty itemDisplayerPanelProp;
        private SerializedProperty slotsContainerProp;
        private SerializedProperty slotPrefabProp;
        private SerializedProperty initialSlotCountProp;
        private SerializedProperty enableCategoryFilterProp;
        private SerializedProperty categoryFilterProp;
        
        private int selectedTab = 0;
        private readonly string[] tabNames = { "🎯 Configuration", "🔲 Components", "🎮 Runtime" };

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

            EditorGUILayout.HelpBox("Abstract UI component that displays inventory in a grid format. Updates slots based on the connected inventory definition.", MessageType.Info);

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
                    DrawConfigurationTab();
                    break;
                case 1:
                    DrawComponentsTab();
                    break;
                case 2:
                    DrawRuntimeTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawConfigurationTab()
        {
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
                    ItemGrid itemGrid = (ItemGrid)target;
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

            GUILayout.Space(15);

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
        }
        
        private void DrawComponentsTab()
        {
            EditorGUILayout.LabelField("🔲 Grid Components", EditorStyles.boldLabel);
            
            // Display slots as readonly/info
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Item Slots", "List of ItemSlot components in this grid (auto-populated)"), EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Count", itemSlotsProp.arraySize);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.PropertyField(itemDisplayerPanelProp, new GUIContent("Item Displayer Panel", "Panel that displays item details when a slot is selected"));
        }
        
        private void DrawRuntimeTab()
        {
            DrawRuntimeControls();
        }
        
        private void DrawRuntimeControls()
        {
            GUILayout.BeginVertical("box");

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
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime controls are only available during play mode. Press Play to test inventory operations!", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            ItemGrid itemGrid = (ItemGrid)target;

            // Display info
            var inventory = itemGrid.GetConnectedInventory();
            if (inventory != null)
            {
                int itemCount = inventory.Items != null ? inventory.Items.Count : 0;
                int slotCount = itemGrid.GetItemSlots() != null ? itemGrid.GetItemSlots().Count : 0;
                
                GUILayout.Space(5);
                EditorGUILayout.LabelField("📋 Grid Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Items in Inventory:", itemCount.ToString());
                EditorGUILayout.LabelField("Slots Available:", slotCount.ToString());
                
                // Category filter info
                bool isFiltered = itemGrid.IsCategoryFilterEnabled();
                if (isFiltered)
                {
                    string filter = itemGrid.GetCategoryFilter();
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
                        itemGrid.ClearCategoryFilter();
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
                EditorGUILayout.HelpBox("⚠️ Inventory ID is required. This grid needs an inventory ID to display items.", MessageType.Warning);
            }

            if (itemSlotsProp.arraySize == 0 && (slotPrefabProp.objectReferenceValue == null || slotsContainerProp.objectReferenceValue == null))
            {
                EditorGUILayout.HelpBox("⚠️ No item slots configured. Either manually assign slots or configure auto-population settings.", MessageType.Warning);
            }
        }
    }

    /// <summary>
    /// Custom editor for ItemSlot abstract component
    /// </summary>
    [CustomEditor(typeof(ItemSlot))]
    [CanEditMultipleObjects]
    public class ItemSlotEditor : Editor
    {
        private SerializedProperty iconImageProp;
        private SerializedProperty itemNameTextProp;
        private SerializedProperty quantityTextProp;
        private SerializedProperty descriptionTextProp;
        private SerializedProperty slotContainerProp;
        private SerializedProperty customPropertyViewsProp;
        
        private int selectedTab = 0;
        private readonly string[] tabNames = { "🖼️ UI References", "⚙️ Custom Properties", "🎮 Runtime" };

        private void OnEnable()
        {
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

            EditorGUILayout.HelpBox("Abstract class that works with ItemGrid to display item icons, quantities, and uses. Includes UI click/press events, all of which are overridable.", MessageType.Info);

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
                    DrawUIReferencesTab();
                    break;
                case 1:
                    DrawCustomPropertiesTab();
                    break;
                case 2:
                    DrawRuntimeTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
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

            ItemSlot itemSlot = (ItemSlot)target;

            // Display info
            var currentItem = itemSlot.GetCurrentItem();
            bool isActive = itemSlot.IsActive();

            GUILayout.Space(5);
            EditorGUILayout.LabelField("📋 Slot Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Is Active:", isActive ? "Yes" : "No");

            if (isActive && currentItem.ItemReference != null)
            {
                EditorGUILayout.LabelField("Item Name:", currentItem.ItemReference.ItemName);
                EditorGUILayout.LabelField("Quantity:", currentItem.Quantity.ToString());
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
    /// Custom editor for ItemDisplayerPanel abstract component
    /// </summary>
    [CustomEditor(typeof(ItemDisplayerPanel))]
    [CanEditMultipleObjects]
    public class ItemDisplayerPanelEditor : Editor
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
        
        private int selectedTab = 0;
        private readonly string[] tabNames = { "🔲 Panel", "📋 Item Fields", "⚙️ Custom Properties", "🎮 Runtime" };

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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("Abstract class that displays detailed item information in a panel. Shows information that wouldn't fit in an ItemSlot.", MessageType.Info);
            GUILayout.Space(10);

            DrawErrorWarnings();
            GUILayout.Space(5);

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            GUILayout.Space(10);

            switch (selectedTab)
            {
                case 0: DrawPanelTab(); break;
                case 1: DrawItemFieldsTab(); break;
                case 2: DrawCustomPropertiesTab(); break;
                case 3: DrawRuntimeTab(); break;
            }

            serializedObject.ApplyModifiedProperties();
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

        private void DrawRuntimeTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("🎯 Runtime information is only available during play mode. Press Play to test panel interactions!", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("📋 Runtime Info", EditorStyles.boldLabel);
            
            ItemDisplayerPanel panel = (ItemDisplayerPanel)target;
            var currentItem = panel.GetCurrentDisplayedItem();
            
            if (currentItem.ItemReference != null)
            {
                EditorGUILayout.LabelField("Currently Displaying:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Item Name:", currentItem.ItemReference.ItemName);
                EditorGUILayout.LabelField("Quantity:", currentItem.Quantity.ToString());
                EditorGUILayout.LabelField("Category:", currentItem.ItemReference.Category ?? "None");
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

    /// <summary>
    /// Custom editor for ItemSO ScriptableObject
    /// </summary>
    [CustomEditor(typeof(ItemSO))]
    [CanEditMultipleObjects]
    public class ItemSOEditor : Editor
    {
        private SerializedProperty itemNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty iconProp;
        private SerializedProperty categoryProp;
        private SerializedProperty maxStackSizeProp;
        private SerializedProperty maxQuantityProp;
        private SerializedProperty isUsableProp;
        private SerializedProperty isDiscardableProp;
        private SerializedProperty useAudioProp;
        private SerializedProperty addAudioProp;
        private SerializedProperty removeAudioProp;
        
        // Usage pipeline properties
        private SerializedProperty usagePipesProp;
        private SerializedProperty onAddEventProp;
        private SerializedProperty onRemoveEventProp;
        
        // Custom properties
        private SerializedProperty customPropertiesProp;

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Basic", "Stack", "Properties", "Usage", "Audio" };

        private void OnEnable()
        {
            itemNameProp = serializedObject.FindProperty("itemName");
            descriptionProp = serializedObject.FindProperty("description");
            iconProp = serializedObject.FindProperty("icon");
            categoryProp = serializedObject.FindProperty("category");
            maxStackSizeProp = serializedObject.FindProperty("maxStackSize");
            maxQuantityProp = serializedObject.FindProperty("maxQuantity");
            isUsableProp = serializedObject.FindProperty("isUsable");
            isDiscardableProp = serializedObject.FindProperty("isDiscardable");
            useAudioProp = serializedObject.FindProperty("useAudio");
            addAudioProp = serializedObject.FindProperty("addAudio");
            removeAudioProp = serializedObject.FindProperty("removeAudio");
            
            // Usage pipeline properties
            usagePipesProp = serializedObject.FindProperty("usagePipes");
            onAddEventProp = serializedObject.FindProperty("onAddEvent");
            onRemoveEventProp = serializedObject.FindProperty("onRemoveEvent");
            
            // Custom properties
            customPropertiesProp = serializedObject.FindProperty("customProperties");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //// Header image
            //GUILayout.Label(GraphicLoader.InventoryHeader, GUILayout.Height(150));

            EditorGUILayout.HelpBox("A scriptable data asset with extensive modularity for items. Used as the base data definition for all inventory items.", MessageType.Info);

            //GUILayout.Space(10);

            //DrawErrorWarnings();

            // Item Preview Section
            DrawPreviewSection();

            GUILayout.Space(5);

            // Tab selection
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            GUILayout.Space(10);

            // Draw content based on selected tab
            switch (selectedTab)
            {
                case 0: DrawBasicTab(); break;
                case 1: DrawStackTab(); break;
                case 2: DrawPropertiesTab(); break;
                case 3: DrawUsageTab(); break;
                case 4: DrawAudioTab(); break;
            }

            GUILayout.Space(15);

            // Config Management Section
            DrawConfigManagement();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigManagement()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⚙️ Config Management", EditorStyles.boldLabel);

            ItemSO itemSO = (ItemSO)target;
            SignaliaConfigAsset config = ConfigReader.GetConfig(true);

            if (config == null)
            {
                EditorGUILayout.HelpBox("Signalia Config not found. Cannot manage references.", MessageType.Warning);
                return;
            }

            // Check if already in config
            bool alreadyInConfig = IsItemInConfig(itemSO);

            if (alreadyInConfig)
            {
                EditorGUILayout.HelpBox("✓ This ItemSO is already in the Signalia Config.", MessageType.Info);

                if (GUILayout.Button("🗑️ Remove from Config", GUILayout.Height(25)))
                {
                    RemoveItemFromConfig(itemSO);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This ItemSO is not in Signalia Config. Add it to enable it in the save/load system.", MessageType.Warning);

                if (GUILayout.Button("➕ Add to Config", GUILayout.Height(30)))
                {
                    AddItemToConfig(itemSO);
                }
            }
        }

        private bool IsItemInConfig(ItemSO item)
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig(true);
            if (config?.InventorySystem?.ItemReferences == null) return false;

            foreach (var refItem in config.InventorySystem.ItemReferences)
            {
                if (refItem == item) return true;
            }
            return false;
        }

        private void AddItemToConfig(ItemSO item)
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig(true);
            if (config == null)
            {
                EditorUtility.DisplayDialog("Error", "Signalia Config not found.", "OK");
                return;
            }

            var list = new System.Collections.Generic.List<ItemSO>(config.InventorySystem.ItemReferences);

            if (!list.Contains(item))
            {
                list.Add(item);
                config.InventorySystem.ItemReferences = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void RemoveItemFromConfig(ItemSO item)
        {
            SignaliaConfigAsset config = ConfigReader.GetConfig(true);
            if (config == null) return;

            var list = new System.Collections.Generic.List<ItemSO>(config.InventorySystem.ItemReferences);

            if (list.Remove(item))
            {
                config.InventorySystem.ItemReferences = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Header
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("👁️ Quick Preview", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Content area
            GUILayout.BeginHorizontal();
            
            // Icon preview (left side)
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            var icon = iconProp.objectReferenceValue as Sprite;
            if (icon != null)
            {
                try
                {
                    // Calculate texture coordinates for the sprite
                    Rect spriteRect = icon.textureRect;
                    float spriteWidth = spriteRect.width;
                    float spriteHeight = spriteRect.height;
                    
                    // Calculate aspect ratio
                    float aspectRatio = spriteWidth / spriteHeight;
                    
                    // Calculate display size while preserving aspect ratio (max 80x80)
                    float maxSize = 80f;
                    float displayWidth, displayHeight;
                    
                    if (aspectRatio > 1f)
                    {
                        // Wider than tall
                        displayWidth = maxSize;
                        displayHeight = maxSize / aspectRatio;
                    }
                    else
                    {
                        // Taller than wide or square
                        displayHeight = maxSize;
                        displayWidth = maxSize * aspectRatio;
                    }
                    
                    Rect texCoords = new Rect(
                        spriteRect.x / icon.texture.width,
                        spriteRect.y / icon.texture.height,
                        spriteRect.width / icon.texture.width,
                        spriteRect.height / icon.texture.height
                    );
                    
                    // Get the rect for drawing (centered within the 80px space)
                    Rect fullRect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80), GUILayout.Height(80));
                    Rect drawRect = new Rect(
                        fullRect.x + (fullRect.width - displayWidth) * 0.5f,
                        fullRect.y + (fullRect.height - displayHeight) * 0.5f,
                        displayWidth,
                        displayHeight
                    );
                    
                    // Draw the sprite with preserved aspect ratio
                    GUI.DrawTextureWithTexCoords(drawRect, icon.texture, texCoords);
                    
                    EditorGUILayout.LabelField("Icon", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80));
                }
                catch
                {
                    GUILayout.Space(80);
                    EditorGUILayout.LabelField("Icon Error", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80));
                }
            }
            else
            {
                GUILayout.Space(80);
                EditorGUILayout.LabelField("No Icon", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80));
            }
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Name and Description (right side)
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            // Item Name
            string name = itemNameProp.stringValue;
            if (!string.IsNullOrEmpty(name))
            {
                EditorGUILayout.LabelField("📝 Name:", name, EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("📝 Name:", "<Unnamed Item>", EditorStyles.boldLabel);
            }
            
            GUILayout.Space(5);
            
            // Category
            string category = categoryProp.stringValue;
            if (!string.IsNullOrEmpty(category))
            {
                EditorGUILayout.LabelField("🏷️ Category:", category);
            }
            
            GUILayout.Space(8);
            
            // Description
            EditorGUILayout.LabelField("📄 Description:", EditorStyles.miniLabel);
            string description = descriptionProp.stringValue;
            if (!string.IsNullOrEmpty(description))
            {
                // Truncate long descriptions for the preview
                string previewDesc = description.Length > 150 ? description.Substring(0, 150) + "..." : description;
                EditorGUILayout.LabelField(previewDesc, EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("<No description>", EditorStyles.wordWrappedMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Quick Stats - Compact layout
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📚 Stack: {maxStackSizeProp.intValue}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"📊 Max: {maxQuantityProp.intValue}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(isUsableProp.boolValue ? "✓ Usable" : "✗ Usable", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(isDiscardableProp.boolValue ? "✓ Discard" : "✗ Discard", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            // Usage Pipelines count
            int pipelineCount = usagePipesProp.arraySize;
            EditorGUILayout.BeginHorizontal();
            string pipelineStatus = pipelineCount > 0 ? $"🔧 Usage Pipelines: {pipelineCount}" : "🔧 Usage Pipelines: None";
            EditorGUILayout.LabelField(pipelineStatus, EditorStyles.miniLabel);
            
            if (pipelineCount > 0 && !isUsableProp.boolValue)
            {
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField("⚠️ Pipelines won't run - item not usable", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawBasicTab()
        {
            EditorGUILayout.LabelField("📝 Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(itemNameProp, new GUIContent("Item Name", "Display name of this item"));

            if (string.IsNullOrEmpty(itemNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("Item Name is required.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Description", "Description text for this item"), true);

            if (string.IsNullOrEmpty(descriptionProp.stringValue))
            {
                EditorGUILayout.HelpBox("Consider adding a description to help players understand what this item does.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon", "Sprite icon to display for this item"));

            if (iconProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No icon assigned. The item will not have a visual representation.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(categoryProp, new GUIContent("Category", "Category this item belongs to (e.g., 'Weapon', 'Consumable', 'Resource')"));

            if (string.IsNullOrEmpty(categoryProp.stringValue))
            {
                EditorGUILayout.HelpBox("Consider adding a category to help organize your inventory items.", MessageType.Info);
            }
        }

        private void DrawStackTab()
        {
            EditorGUILayout.LabelField("📚 Stack Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(maxStackSizeProp, new GUIContent("Max Stack Size", "Maximum number of this item that can stack together"));

            if (maxStackSizeProp.intValue < 1)
            {
                EditorGUILayout.HelpBox("Max Stack Size should be at least 1. Items cannot stack with a size of 0 or less.", MessageType.Warning);
            }
            else if (maxStackSizeProp.intValue == 1)
            {
                EditorGUILayout.HelpBox("Stack Size is 1: This item will not stack.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(maxQuantityProp, new GUIContent("Max Quantity", "Maximum total quantity a player can have of this item"));

            if (maxQuantityProp.intValue < 1)
            {
                EditorGUILayout.HelpBox("Max Quantity should be at least 1. Players cannot have quantities of 0 or less.", MessageType.Warning);
            }

            if (maxQuantityProp.intValue < maxStackSizeProp.intValue)
            {
                EditorGUILayout.HelpBox("Max Quantity is less than Max Stack Size. This may cause issues with stacking.", MessageType.Warning);
            }

            EditorGUILayout.HelpBox($"Players can stack up to {maxStackSizeProp.intValue} of this item, with a total maximum of {maxQuantityProp.intValue} items.", MessageType.Info);
        }

        private void DrawPropertiesTab()
        {
            EditorGUILayout.LabelField("⚙️ Item Properties", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(isUsableProp, new GUIContent("Is Usable", "Whether this item can be used (consumed) by the player"));

            if (isUsableProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("This item can be used. Override the UseItem() method in a derived class to define custom behavior.", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(isDiscardableProp, new GUIContent("Is Discardable", "Whether this item can be discarded from inventory"));

            if (!isDiscardableProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("This item cannot be discarded. Players will need to use it or transfer it to another inventory.", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(15);

            // Custom Properties Section
            EditorGUILayout.LabelField("🔧 Custom Properties", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define custom attributes for this item such as price, rarity, health restored, etc. These properties are accessible in event listeners and custom behaviors.", MessageType.Info);
            
            GUILayout.Space(5);
            
            EditorGUILayout.PropertyField(customPropertiesProp, new GUIContent("Custom Properties", "Custom item-specific properties accessible via event listeners"), true);

            if (customPropertiesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No custom properties defined. Add properties to store custom data like item price, rarity, or other attributes.", MessageType.Info);
            }

            // Show preview of current values
            if (Application.isPlaying)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("📊 Runtime Preview", EditorStyles.boldLabel);

                ItemSO item = (ItemSO)target;
                EditorGUILayout.LabelField("Item Name:", item.ItemName);
                EditorGUILayout.LabelField("Category:", item.Category ?? "None");
                EditorGUILayout.LabelField("Usable:", item.IsUsable ? "Yes" : "No");
                EditorGUILayout.LabelField("Discardable:", item.IsDiscardable ? "Yes" : "No");

                // Show custom properties in runtime
                if (item.CustomProperties != null && item.CustomProperties.Length > 0)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Custom Properties:", EditorStyles.boldLabel);
                    foreach (var prop in item.CustomProperties)
                    {
                        EditorGUILayout.LabelField($"  {prop.PropertyName}:", prop.GetValue()?.ToString() ?? "null");
                    }
                }

                // Testing helpers
                GUILayout.Space(10);
                EditorGUILayout.LabelField("🧪 Testing Tools", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Right-click on this ItemSO asset in the Project window to use testing tools with all inventories.", MessageType.Info);
            }
        }

        private void DrawAudioTab()
        {
            EditorGUILayout.LabelField("🔊 Audio Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure audio events for various item actions. These audio keys should match entries in your AudioAsset scriptable objects.", MessageType.Info);

            GUILayout.Space(10);

            // Use Audio
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎮 Use Audio", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio to play when this item is used or consumed.", MessageType.None);
            Rect useAudioRect = EditorGUILayout.GetControlRect();
            PropertyHelpers.DrawAudioDropdownInline(useAudioRect, "Use Audio", useAudioProp, serializedObject);
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // Add Audio
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("➕ Add Audio", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio to play when this item is added to an inventory.", MessageType.None);
            Rect addAudioRect = EditorGUILayout.GetControlRect();
            PropertyHelpers.DrawAudioDropdownInline(addAudioRect, "Add Audio", addAudioProp, serializedObject);
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // Remove Audio
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("➖ Remove Audio", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio to play when this item is removed from an inventory.", MessageType.None);
            Rect removeAudioRect = EditorGUILayout.GetControlRect();
            PropertyHelpers.DrawAudioDropdownInline(removeAudioRect, "Remove Audio", removeAudioProp, serializedObject);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("💡 Tip: Use the search button (🔍) next to each field to browse and select from all available audio assets in your project.", MessageType.Info);
        }

        private void DrawUsageTab()
        {
            EditorGUILayout.LabelField("🎮 Usage Pipelines", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure how this item behaves when used. You can add multiple usage pipelines - they will all execute in order when the item is used.", MessageType.Info);

            // Warning if item is not usable but has pipelines configured
            if (!isUsableProp.boolValue && usagePipesProp.arraySize > 0)
            {
                GUILayout.Space(5);
                GUI.backgroundColor = Color.yellow;
                EditorGUILayout.HelpBox("⚠️ Warning: Usage pipelines are configured but this item is not marked as usable. Pipelines will not execute when this item is used. Enable 'Is Usable' in the Properties tab for pipelines to work.", MessageType.Warning);
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(10);

            // Usage Pipelines List
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📡 Usage Pipelines ({usagePipesProp.arraySize})", EditorStyles.boldLabel);
            
            if (GUILayout.Button("➕ Add Pipeline", GUILayout.Width(120)))
            {
                usagePipesProp.arraySize++;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (usagePipesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No usage pipelines configured. Add a pipeline to define what happens when this item is used.", MessageType.Warning);
            }
            
            // Draw each pipeline
            for (int i = 0; i < usagePipesProp.arraySize; i++)
            {
                DrawUsagePipeline(i);
                
                if (i < usagePipesProp.arraySize - 1)
                {
                    GUILayout.Space(5);
                }
            }
            
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Signalia Events on Add/Remove
            EditorGUILayout.LabelField("📡 Inventory Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure events to send when this item is added or removed from inventories.", MessageType.None);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("➕ Add Event", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onAddEventProp, new GUIContent("On Add Event", "Event to send when this item is added to any inventory. This item will be passed as parameter."));
            
            if (!string.IsNullOrEmpty(onAddEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"💡 Event '{onAddEventProp.stringValue}' will be sent when this item is added to an inventory.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("➖ Remove Event", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onRemoveEventProp, new GUIContent("On Remove Event", "Event to send when this item is removed from any inventory. This item will be passed as parameter."));
            
            if (!string.IsNullOrEmpty(onRemoveEventProp.stringValue))
            {
                EditorGUILayout.HelpBox($"💡 Event '{onRemoveEventProp.stringValue}' will be sent when this item is removed from an inventory.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Usage Flow Diagram
            if (usagePipesProp.arraySize > 0)
            {
                EditorGUILayout.LabelField("🔄 Usage Flow", EditorStyles.boldLabel);
                
                if (isUsableProp.boolValue)
                {
                    EditorGUILayout.HelpBox("When this item is used:\n" +
                        "1. Each pipeline checks its condition (if enabled) → If returns false, sends fail event and skips that pipeline\n" +
                        "2. Each pipeline executes its configured action (DefaultEvent uses auto-generated name, CustomEvent uses custom name, CustomBehavior executes InlineScript)\n" +
                        "3. Play use audio (if configured)\n" +
                        "4. Item is consumed from inventory", MessageType.Info);
                }
                else
                {
                    GUI.backgroundColor = Color.yellow;
                    EditorGUILayout.HelpBox("⚠️ Usage pipelines are configured but this item is not usable. Pipelines will not execute.\n\nEnable 'Is Usable' in the Properties tab for this flow to work.", MessageType.Warning);
                    GUI.backgroundColor = Color.white;
                }
            }
        }
        
        private void DrawUsagePipeline(int index)
        {
            SerializedProperty pipeProp = usagePipesProp.GetArrayElementAtIndex(index);
            
            SerializedProperty usePipelineProp = pipeProp.FindPropertyRelative("usePipeline");
            SerializedProperty useEventProp = pipeProp.FindPropertyRelative("useEvent");
            SerializedProperty behaviorCodeProp = pipeProp.FindPropertyRelative("behaviorCode");
            SerializedProperty conditionCodeProp = pipeProp.FindPropertyRelative("conditionCode");
            SerializedProperty executeBehaviorProp = pipeProp.FindPropertyRelative("executeBehavior");
            SerializedProperty passConditionProp = pipeProp.FindPropertyRelative("passCondition");
            SerializedProperty conditionFailEventProp = pipeProp.FindPropertyRelative("conditionFailEvent");
            
            EditorGUILayout.BeginVertical("box");
            
            // Header with delete button
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Pipeline {index + 1}", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("🗑️", GUILayout.Width(30)))
            {
                usagePipesProp.DeleteArrayElementAtIndex(index);
                return;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            // Pipeline Type
            EditorGUILayout.PropertyField(usePipelineProp, new GUIContent("Pipeline Type", "Determines how the item is used: DefaultEvent uses the auto-generated event name, CustomEvent lets you specify an event, CustomBehavior executes InlineScript code."));

            int pipelineValue = usePipelineProp.enumValueIndex;
            bool isDefaultEvent = pipelineValue == 1; // DefaultEvent
            bool isCustomEvent = pipelineValue == 2; // CustomEvent
            bool isCustomBehavior = pipelineValue == 3; // CustomBehaviorObject
            bool showEventFields = isCustomEvent;
            bool showBehaviorFields = isCustomBehavior;

            // Default Event Configuration
            if (isDefaultEvent)
            {
                GUILayout.Space(3);
                ItemSO itemSO = (ItemSO)serializedObject.targetObject;
                string defaultEvent = itemSO.DefaultUseEvent;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Default Event", "Auto-generated event name based on item name"), EditorStyles.boldLabel);
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("📋 Copy", GUILayout.Width(60)))
                {
                    EditorGUIUtility.systemCopyBuffer = defaultEvent;
                    Debug.Log($"Copied event name to clipboard: {defaultEvent}");
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.SelectableLabel(defaultEvent, EditorStyles.textField, GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox($"💡 This pipeline will send the event '{defaultEvent}' when the item is used.", MessageType.Info);
            }

            // Custom Event Configuration
            if (showEventFields)
            {
                GUILayout.Space(3);
                EditorGUILayout.PropertyField(useEventProp, new GUIContent("Use Event", "Signalia event name to send when item is used. This item will be passed as a parameter."));
                
                if (string.IsNullOrEmpty(useEventProp.stringValue))
                {
                    EditorGUILayout.HelpBox("⚠️ Event name is required when using Custom Event pipeline.", MessageType.Warning);
                }
            }

            // InlineScript Behavior Configuration
            if (showBehaviorFields)
            {
                GUILayout.Space(3);
                EditorGUILayout.PropertyField(behaviorCodeProp, new GUIContent("Behavior Code", "InlineVoid asset that contains custom behavior code to execute."));
                EditorGUILayout.PropertyField(executeBehaviorProp, new GUIContent("Execute Behavior", "If enabled, the behaviorCode InlineScript will be executed when this item is used."));
            }

            // Condition Configuration
            GUILayout.Space(3);
            EditorGUILayout.PropertyField(passConditionProp, new GUIContent("Check Condition", "If enabled, will evaluate conditionCode before allowing the item to be used."));
            
            if (passConditionProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(conditionCodeProp, new GUIContent("Condition Code", "InlineBoolFunction asset that returns true/false to determine if item can be used."));
                EditorGUILayout.PropertyField(conditionFailEventProp, new GUIContent("Condition Fail Event", "Event to send if condition check fails. This item will be passed as parameter."));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawErrorWarnings()
        {
            if (string.IsNullOrEmpty(itemNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("⚠️ Item Name is required.", MessageType.Error);
            }

            if (iconProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ No icon assigned.", MessageType.Warning);
            }
        }
    }
}
#endif
