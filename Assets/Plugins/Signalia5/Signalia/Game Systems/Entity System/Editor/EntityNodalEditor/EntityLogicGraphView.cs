using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors
{
    /// <summary>
    /// GraphView for Entity Logic nodal editor.
    /// </summary>
    public class EntityLogicGraphView : GraphView
    {
        #region Properties
        
        private EntityLogic _logic;
        public EntityLogic Logic 
        { 
            get => _logic;
            set
            {
                if (_logic != value)
                {
                    _logic = value;
                    RefreshNodes();
                }
            }
        }
        
        private Dictionary<int, StateNode> stateNodes = new Dictionary<int, StateNode>();
        private readonly Dictionary<Port, ConditionStack> conditionStackByPort = new Dictionary<Port, ConditionStack>();
        private readonly EntityLogicEdgeConnectorListener edgeConnectorListener;
        
        #endregion
        
        #region Constructor
        
        public EntityLogicGraphView()
        {
            edgeConnectorListener = new EntityLogicEdgeConnectorListener();
            SetupStyles();
            SetupBackground();
            SetupManipulators();
            RegisterNodePositionCallbacks();
        }
        
        #endregion
        
        #region Setup Methods
        
        private void SetupStyles()
        {
            var styleSheet = EntityEditorUtilities.LoadGraphViewStyleSheet();
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }
        
        private void SetupBackground()
        {
            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
        }
        
        private void SetupManipulators()
        {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }
        
        private void RegisterNodePositionCallbacks()
        {
            // Save positions when graph view changes
            graphViewChanged += OnGraphViewChanged;
        }
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_logic == null) return graphViewChange;

            if (graphViewChange.elementsToRemove != null)
            {
                HandleEdgeRemovals(graphViewChange.elementsToRemove);
            }

            if (graphViewChange.edgesToCreate != null)
            {
                HandleEdgeCreations(graphViewChange.edgesToCreate);
            }

            // Save positions of all nodes when graph changes
            UpdateNodePositions();
            
            return graphViewChange;
        }
        
        #endregion
        
        #region Node Management
        
        public void RefreshNodes()
        {
            ClearNodes();
            
            if (_logic == null) return;
            
            // Initialize node data
            _logic.SyncNodeDataToStates();
            
            // Get states array via accessor
            var states = _logic.GetStates();
            if (states == null) return;
            
            // Create nodes for each state
            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null) continue;
                
                var node = new StateNode(state, i, edgeConnectorListener, _logic);
                stateNodes[i] = node;
                
                // Load position from serialized data
                var nodeData = _logic.RetrieveNodeData(i);
                // Use serialized position if state name matches (data is valid), otherwise use default
                if (nodeData.stateName == state.stateName && nodeData.position != Vector2.zero)
                {
                    node.SetPosition(new Rect(nodeData.position, Vector2.zero));
                }
                else
                {
                    // Default position if not set or data doesn't match
                    node.SetPosition(new Rect(100 + i * 200, 100, 0, 0));
                    // Update the node data with the default position
                    _logic.SetNodePosition(i, new Vector2(100 + i * 200, 100));
                }
                
                AddElement(node);
            }

            RebuildConditionPortMap();
            CreateConditionConnections();
        }

        public void RefreshStateNode(int stateIndex)
        {
            if (_logic == null) return;

            if (!stateNodes.TryGetValue(stateIndex, out var node))
            {
                RefreshNodes();
                return;
            }

            node.SyncWithState();
            RebuildConditionPortMap();
        }

        public StateNode GetStateNode(int stateIndex)
        {
            stateNodes.TryGetValue(stateIndex, out var node);
            return node;
        }
        
        public void AddStateNode(EntityFSMState state, int stateIndex)
        {
            if (state == null || _logic == null) return;
            
            var node = new StateNode(state, stateIndex, edgeConnectorListener, _logic);
            stateNodes[stateIndex] = node;
            
            // Load position from serialized data or use default
            var nodeData = _logic.RetrieveNodeData(stateIndex);
            if (nodeData.position != Vector2.zero)
            {
                node.SetPosition(new Rect(nodeData.position, Vector2.zero));
            }
            else
            {
                // Default position
                node.SetPosition(new Rect(100 + stateIndex * 200, 100, 0, 0));
            }
            
            AddElement(node);
            RebuildConditionPortMap();
            CreateConditionConnections();
        }
        
        public void RemoveStateNode(int stateIndex)
        {
            if (stateNodes.ContainsKey(stateIndex))
            {
                var node = stateNodes[stateIndex];
                RemoveElement(node);
                stateNodes.Remove(stateIndex);
            }

            RebuildConditionPortMap();
            CreateConditionConnections();
        }
        
        public void ClearNodes()
        {
            foreach (var node in stateNodes.Values)
            {
                RemoveElement(node);
            }
            stateNodes.Clear();
            conditionStackByPort.Clear();
            ClearConnections();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(port => port != startPort && port.node != startPort.node && port.direction != startPort.direction)
                .ToList();
        }

        private void RebuildConditionPortMap()
        {
            conditionStackByPort.Clear();
            foreach (var node in stateNodes.Values)
            {
                foreach (var conditionPort in node.ConditionPorts)
                {
                    if (conditionPort.Port != null && conditionPort.ConditionStack != null)
                    {
                        conditionStackByPort[conditionPort.Port] = conditionPort.ConditionStack;
                    }
                }
            }
        }

        private void HandleEdgeCreations(IEnumerable<Edge> edges)
        {
            foreach (var edge in edges)
            {
                if (edge?.output == null || edge.input == null) continue;
                if (!conditionStackByPort.TryGetValue(edge.output, out var stack)) continue;
                var targetNode = edge.input.node as StateNode;
                if (targetNode?.State == null) continue;

                stack.SetExitState(targetNode.State.stateName);
                RefreshNodeVisuals(edge.output.node as StateNode);
                EditorUtility.SetDirty(_logic);
            }
        }

        private void HandleEdgeRemovals(IEnumerable<GraphElement> elements)
        {
            foreach (var element in elements)
            {
                if (element is not Edge edge) continue;
                if (edge.output == null) continue;
                if (!conditionStackByPort.TryGetValue(edge.output, out var stack)) continue;
                stack.SetExitState(string.Empty);
                RefreshNodeVisuals(edge.output.node as StateNode);
                EditorUtility.SetDirty(_logic);
            }
        }

        private void RefreshNodeVisuals(StateNode node)
        {
            if (node == null) return;
            node.RefreshConditionDisplay();
        }
        
        public void UpdateNodePositions()
        {
            if (_logic == null) return;
            
            foreach (var kvp in stateNodes)
            {
                var node = kvp.Value;
                var position = node.GetPosition().position;
                _logic.SetNodePosition(kvp.Key, position);
            }
            
            EditorUtility.SetDirty(_logic);
        }

        public void RefreshConditionConnections()
        {
            RebuildConditionPortMap();
            CreateConditionConnections();
        }

        private void CreateConditionConnections()
        {
            if (_logic == null)
            {
                ClearConnections();
                return;
            }

            ClearConnections();

            var states = _logic.GetStates();
            if (states == null) return;

            var stateIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] == null || string.IsNullOrEmpty(states[i].stateName)) continue;
                if (!stateIndexByName.ContainsKey(states[i].stateName))
                {
                    stateIndexByName.Add(states[i].stateName, i);
                }
            }

            foreach (var node in stateNodes.Values)
            {
                foreach (var conditionPort in node.ConditionPorts)
                {
                    var stack = conditionPort.ConditionStack;
                    if (stack == null || string.IsNullOrEmpty(stack.ExitState)) continue;
                    if (!stateIndexByName.TryGetValue(stack.ExitState, out var targetIndex)) continue;
                    if (!stateNodes.TryGetValue(targetIndex, out var targetNode)) continue;
                    if (conditionPort.Port == null || targetNode.InputPort == null) continue;

                    var edge = conditionPort.Port.ConnectTo(targetNode.InputPort);
                    AddElement(edge);
                }
            }
        }

        private void ClearConnections()
        {
            if (!edges.Any()) return;
            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }
        }

        private sealed class EntityLogicEdgeConnectorListener : IEdgeConnectorListener
        {
            public void OnDrop(GraphView view, Edge edge)
            {
                if (edge?.input == null || edge.output == null) return;

                var edgesToRemove = new List<Edge>();

                if (edge.input.capacity == Port.Capacity.Single)
                {
                    edgesToRemove.AddRange(edge.input.connections);
                }

                if (edge.output.capacity == Port.Capacity.Single)
                {
                    edgesToRemove.AddRange(edge.output.connections);
                }

                foreach (var existingEdge in edgesToRemove)
                {
                    if (existingEdge == edge) continue;
                    view.RemoveElement(existingEdge);
                }

                view.AddElement(edge);
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
            }
        }
        
        #endregion
    }
}
