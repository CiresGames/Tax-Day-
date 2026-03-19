using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

// Entity nodal system todo: Change the way wiring works so it has more bend, possibly overriding its class, and overall add better look

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    /// <summary>
    /// Editor window for EntityNodalGraphView.
    /// </summary>
    public class EntityNodalWindow : EditorWindow
    {
        #region Static Fields
        
        private static EntityLogic inspected_logic;
        private static EntityNodalWindow currentWindow;
        
        #endregion
        
        #region Private Fields
        
        private Toolbar toolbar;
        private Toolbar secondaryToolbar;
        private ToolbarMenu entityLogicMenu;
        private EntityLogicGraphView graphView;
        private TwoPaneSplitView splitView;
        private EntityNodalInspectorPanel inspectorPanel;
        
        #endregion
        
        #region Window Lifecycle
        
        static void ShowWindow()
        {
            currentWindow = GetWindow<EntityNodalWindow>("Entity Nodal");
            currentWindow.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            currentWindow = this;
            InitializeWindow();
            SetupStyles();
            ConstructUI();
            
            // Auto-focus on nodes after UI is constructed
            EditorApplication.delayCall += AutoFocusOnNodes;
        }
        
        private void AutoFocusOnNodes()
        {
            if (graphView != null && inspected_logic != null)
            {
                graphView.FrameAll();
            }
            EditorApplication.delayCall -= AutoFocusOnNodes;
        }

        private void OnDisable()
        {
            // Save node positions before closing
            if (graphView != null && inspected_logic != null)
            {
                graphView.UpdateNodePositions();
                EditorUtility.SetDirty(inspected_logic);
            }
            
            Cleanup();
        }

        private void OnInspectorUpdate()
        {
            UpdateFromSelection();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeWindow()
        {
            rootVisualElement.Clear();
        }
        
        private void SetupStyles()
        {
            var styleSheet = EntityEditorUtilities.LoadGraphViewStyleSheet();
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
        }
        
        private void ConstructUI()
        {
            ConstructToolbar();
            ConstructSecondaryToolbar();
            ConstructSplitView();
            ConstructGraphView();
            ConstructInspectorPanel();
            ConstructWarningLabel();
        }
        
        private void Cleanup()
        {
            if (inspectorPanel != null)
            {
                inspectorPanel.Cleanup();
            }
            
            rootVisualElement.Clear();
            inspected_logic = null;
            toolbar = null;
            secondaryToolbar = null;
            entityLogicMenu = null;
            graphView = null;
            splitView = null;
            inspectorPanel = null;
        }
        
        #endregion
        
        #region Warning Label
        
        private void ConstructWarningLabel()
        {
            var warningLabel = new Label("This utility is experimental and might contain some issues");
            warningLabel.AddToClassList("warning-label");
            rootVisualElement.Add(warningLabel);
        }
        
        #endregion
        
        #region Toolbar Construction
        
        private void ConstructToolbar()
        {
            toolbar = new Toolbar();
            toolbar.AddToClassList("main-toolbar");
            
            AddToolbarLabel();
            AddEntityLogicMenu();
            AddToolbarSpacer();
            AddToolbarButtons();
            
            rootVisualElement.Add(toolbar);
        }
        
        private void AddToolbarLabel()
        {
            var label = new Label("Editing Entity Logic:");
            label.AddToClassList("toolbar-label");
            toolbar.Add(label);
        }
        
        private void AddEntityLogicMenu()
        {
            entityLogicMenu = new ToolbarMenu();
            entityLogicMenu.text = "Select EntityLogic...";
            RefreshEntityLogicMenu();
            toolbar.Add(entityLogicMenu);
        }
        
        private void AddToolbarSpacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList("toolbar-spacer");
            toolbar.Add(spacer);
        }
        
        private void AddToolbarButtons()
        {
            toolbar.Add(CreateToolbarButton("Auto-Arrange", OnAutoArrange));
            toolbar.Add(CreateToolbarButton("Focus", OnFocus));
            toolbar.Add(CreateToolbarButton("Refresh", OnRefresh));
            toolbar.Add(CreateToolbarButton("Save", OnSave));
        }
        
        private Button CreateToolbarButton(string text, Action onClick)
        {
            return new Button(onClick) { text = text };
        }
        
        private void ConstructSecondaryToolbar()
        {
            // Create container for toolbar and shadow
            var toolbarContainer = new VisualElement();
            toolbarContainer.AddToClassList("secondary-toolbar-container");
            
            secondaryToolbar = new Toolbar();
            secondaryToolbar.AddToClassList("secondary-toolbar");
            
            var spacer = new VisualElement();
            spacer.AddToClassList("toolbar-spacer");
            secondaryToolbar.Add(spacer);
            
            var addStateButton = new Button(OnAddState)
            {
                text = "Add State"
            };
            secondaryToolbar.Add(addStateButton);
            
            toolbarContainer.Add(secondaryToolbar);
            
            // Add shadow element below the toolbar
            var shadowElement = new VisualElement();
            shadowElement.AddToClassList("secondary-toolbar-shadow");
            toolbarContainer.Add(shadowElement);
            
            rootVisualElement.Add(toolbarContainer);
        }
        
        #endregion
        
        #region Graph View Construction
        
        private void ConstructSplitView()
        {
            splitView = new TwoPaneSplitView(
                fixedPaneIndex: 1, 
                fixedPaneStartDimension: 450, // this has to match the width of the inspector side panel, not sure why I can't unify them, maybe I'm just dumb
                TwoPaneSplitViewOrientation.Horizontal
            );
            
            splitView.AddToClassList("split-view");
            splitView.CollapseChild(1);
            rootVisualElement.Add(splitView);
        }
        
        private void ConstructGraphView()
        {
            graphView = new EntityLogicGraphView();
            graphView.Logic = inspected_logic;
            graphView.AddToClassList("entity-logic-graph-view");
            splitView.Add(graphView);
        }
        
        #endregion
        
        #region Inspector Panel Construction
        
        private void ConstructInspectorPanel()
        {
            inspectorPanel = new EntityNodalInspectorPanel(splitView, inspected_logic);
            inspectorPanel.SetRefreshCallback(OnRefresh);
            inspectorPanel.SetInspectorChangedCallback(OnInspectorStateChanged);
        }
        
        #endregion
        
        #region Toolbar Button Handlers
        
        private void OnAddState()
        {
            if (graphView == null || inspected_logic == null) return;
            
            // Add new state using accessor
            int newIndex = inspected_logic.AddState();
            
            // Refresh graph view to show new node
            graphView.RefreshNodes();
            
            // Mark as dirty
            EditorUtility.SetDirty(inspected_logic);
            
            var states = inspected_logic.GetStates();
            if (states != null && newIndex < states.Length)
            {
                Debug.Log($"Added new state '{states[newIndex].stateName}' to EntityLogic");
            }
        }
        
        private void OnAutoArrange()
        {
            if (graphView == null || inspected_logic == null) return;
            
            // TODO: Implement auto-arrange logic for nodes
            Debug.Log("Auto-Arrange: Arranging nodes in graph view");
        }
        
        private void OnFocus()
        {
            if (graphView == null || inspected_logic == null) return;
            
            graphView.FrameAll();
        }
        
        private void OnRefresh()
        {
            // Refresh entity logic menu
            RefreshEntityLogicMenu();
            
            if (graphView == null) return;

            int selectedStateIndex = inspectorPanel?.SelectedNode?.StateIndex ?? -1;
            
            // Update graph view logic reference
            if (inspected_logic != null)
            {
                graphView.Logic = inspected_logic;
            }
            
            // Explicitly refresh nodes to ensure they're updated even if logic reference hasn't changed
            graphView.RefreshNodes();
            
            // NOTE: Do NOT call UpdateNodePositions() here!
            // When nodes are newly created, their layout may not be resolved yet.
            // GetPosition() can return incorrect values (like 0,0) which would corrupt
            // the serialized positions. Node positions are already saved via:
            // - OnGraphViewChanged (when user moves nodes)
            // - OnDisable (when window closes)
            // - OnSave (when user clicks Save)

            if (selectedStateIndex >= 0)
            {
                var refreshedNode = graphView.GetStateNode(selectedStateIndex);
                if (refreshedNode != null)
                {
                    graphView.ClearSelection();
                    graphView.AddToSelection(refreshedNode);
                    inspectorPanel?.ShowInspector(refreshedNode);
                }
            }
            
            // Refresh inspector if a node is currently selected
            if (inspectorPanel != null && inspectorPanel.SelectedNode != null)
            {
                inspectorPanel.RefreshInspector();
            }
            
            // Force repaint of the window
            Repaint();
        }

        private void OnInspectorStateChanged(int stateIndex)
        {
            if (graphView == null || inspected_logic == null)
            {
                return;
            }

            graphView.RefreshStateNode(stateIndex);
            graphView.RefreshConditionConnections();

            if (inspectorPanel != null)
            {
                inspectorPanel.RefreshHeader();
            }

            Repaint();
        }
        
        private void OnSave()
        {
            if (inspected_logic == null) return;
            
            // Save node positions before saving
            if (graphView != null)
            {
                graphView.UpdateNodePositions();
            }
            
            EditorUtility.SetDirty(inspected_logic);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Saved EntityLogic: {GetLogicDisplayName(inspected_logic)}");
        }
        
        #endregion
        
        #region Entity Logic Menu Management
        
        private void RefreshEntityLogicMenu()
        {
            if (entityLogicMenu == null) return;
            
            entityLogicMenu.menu.MenuItems().Clear();
            
            var allLogics = FindAllEntityLogics();
            
            if (allLogics == null || allLogics.Length == 0)
            {
                SetEmptyMenuState();
                return;
            }
            
            var sortedLogics = SortLogicsByName(allLogics);
            UpdateMenuText(sortedLogics);
            PopulateMenuItems(sortedLogics);
        }
        
        private EntityLogic[] FindAllEntityLogics()
        {
#if UNITY_6000_0_OR_NEWER
            return FindObjectsByType<EntityLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return FindObjectsOfType<EntityLogic>(true);
#endif
        }
        
        private EntityLogic[] SortLogicsByName(EntityLogic[] logics)
        {
            return logics.OrderBy(l => l.gameObject.name).ToArray();
        }
        
        private void SetEmptyMenuState()
        {
            entityLogicMenu.text = "No EntityLogic Found";
            entityLogicMenu.menu.AppendAction(
                "No EntityLogic components in scene", 
                null, 
                DropdownMenuAction.Status.Disabled
            );
        }
        
        private void UpdateMenuText(EntityLogic[] sortedLogics)
        {
            if (inspected_logic != null && sortedLogics.Contains(inspected_logic))
            {
                entityLogicMenu.text = GetLogicDisplayName(inspected_logic);
            }
            else if (sortedLogics.Length > 0)
            {
                entityLogicMenu.text = "Select EntityLogic...";
            }
        }
        
        private void PopulateMenuItems(EntityLogic[] sortedLogics)
        {
            foreach (var logic in sortedLogics)
            {
                string displayName = GetLogicDisplayName(logic);
                bool isSelected = inspected_logic == logic;
                
                entityLogicMenu.menu.AppendAction(
                    displayName,
                    action => SelectEntityLogic(logic),
                    isSelected ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                );
            }
        }
        
        private string GetLogicDisplayName(EntityLogic logic)
        {
            if (logic == null) return "Null";
            
            string logicName = logic.GetLogicName();
            if (!string.IsNullOrEmpty(logicName))
            {
                return $"{logic.gameObject.name} ({logicName})";
            }
            
            return logic.gameObject.name;
        }
        
        private void SelectEntityLogic(EntityLogic logic)
        {
            inspected_logic = logic;
            
            if (graphView != null)
            {
                graphView.Logic = logic;
            }
            
            if (inspectorPanel != null)
            {
                inspectorPanel.SetEntityLogic(logic);
            }
            
            RefreshEntityLogicMenu();
            PingSelectedGameObject(logic);
            
            OnFocus(); //todo: fix this, it's not working
        }
        
        private void PingSelectedGameObject(EntityLogic logic)
        {
            if (logic != null)
            {
                EditorGUIUtility.PingObject(logic.gameObject);
                Selection.activeGameObject = logic.gameObject;
            }
        }
        
        #endregion
        
        #region Selection Management
        
        private void UpdateFromSelection()
        {
            if (currentWindow == null) return;
            
            if (Selection.activeGameObject != null)
            {
                TryUpdateFromSelectedGameObject();
            }
            else
            {
                RefreshEntityLogicMenu();
            }
        }
        
        private void TryUpdateFromSelectedGameObject()
        {
            var selectedLogic = Selection.activeGameObject.GetComponent<EntityLogic>();
            if (selectedLogic != null && selectedLogic != inspected_logic)
            {
                inspected_logic = selectedLogic;
                if (graphView != null)
                {
                    graphView.Logic = selectedLogic;
                }
                if (inspectorPanel != null)
                {
                    inspectorPanel.SetEntityLogic(selectedLogic);
                }
                RefreshEntityLogicMenu();
            }
        }
        
        #endregion
        
        #region Inspector Panel Management
        
        // Inspector panel management is now handled by EntityNodalInspectorPanel
        
        #endregion
        
        #region Public API
        
        public static void OpenWithContext(EntityLogic logic)
        {
            inspected_logic = logic;
            ShowWindow();
        }
        
        /// <summary>
        /// Refreshes the graph view in the current window instance.
        /// </summary>
        public static void RefreshGraphView()
        {
            if (currentWindow != null)
            {
                currentWindow.OnRefresh();
            }
        }
        
        #endregion
    }
}
