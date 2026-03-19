using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using AHAKuo.Signalia.GameSystems.Entities;
using UnityEditor;

namespace AHAKuo.Signalia.GameSystems.Entities.Editors  
{
    /// <summary>
    /// Base node class for Entity Logic graph nodes.
    /// </summary>
    public class EntityLogicGraphNode : Node
    {
        protected EntityLogic ownerLogic;
        
        #region Constructor
        
        protected EntityLogicGraphNode(EntityLogic lgc, string title = "Node")
        {
            this.title = title;
            ownerLogic = lgc;
            SetupNodeStyle();
            SetupNodeClasses();
        }
        
        #endregion
        
        #region Setup Methods
        
        private void SetupNodeStyle()
        {
            styleSheets.Add(EntityEditorUtilities.LoadGraphViewStyleSheet());
        }
        
        private void SetupNodeClasses()
        {
            AddToClassList("entity-logic-node");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Node representing a State in the Entity Logic graph.
    /// </summary>
    public sealed class StateNode : EntityLogicGraphNode
    {
        public struct ConditionPortInfo
        {
            public Port Port { get; }
            public ConditionStack ConditionStack { get; }

            public ConditionPortInfo(Port port, ConditionStack conditionStack)
            {
                Port = port;
                ConditionStack = conditionStack;
            }
        }

        #region Static Events
        
        public static System.Action<StateNode> OnNodeSelected;
        public static System.Action<StateNode> OnNodeDeselected;
        
        #endregion
        
        #region Properties
        
        public EntityFSMState State { get; private set; }
        public int StateIndex { get; private set; }
        public Port InputPort => inputPort;
        public IReadOnlyList<ConditionPortInfo> ConditionPorts => conditionPorts;
        
        private Port inputPort;
        private readonly IEdgeConnectorListener edgeConnectorListener;
        private readonly List<ConditionPortInfo> conditionPorts = new List<ConditionPortInfo>();
        private VisualElement summaryContainer;
        private VisualElement actionsList;
        private VisualElement conditionsList;
        
        #endregion
        
        #region Constructor
        
        public StateNode(EntityFSMState state, int stateIndex, IEdgeConnectorListener edgeConnectorListener, EntityLogic lgc) : 
            base(lgc, state != null && !string.IsNullOrEmpty(state.stateName) ? state.stateName : "State")
        {
            State = state;
            StateIndex = stateIndex;
            this.edgeConnectorListener = edgeConnectorListener;
            SetupStateNodeClasses();
            BuildNodeContents();
            RegisterEventHandlers();
            UpdateTitle();
        }
        
        #endregion
        
        #region Setup Methods
        
        private void UpdateTitle()
        {
            if (State != null && !string.IsNullOrEmpty(State.stateName))
            {
                title = State.stateName;
            }
            else
            {
                title = $"State {StateIndex}";
            }
        }

        public void SyncWithState()
        {
            if (State == null)
            {
                UpdateTitle();
                BuildNodeContents();
                return;
            }

            if (NeedsPortRebuild())
            {
                BuildNodeContents();
                UpdateTitle();
                return;
            }

            UpdateTitle();
            if (actionsList != null)
            {
                PopulateActions(actionsList);
            }
            if (conditionsList != null)
            {
                PopulateConditions(conditionsList);
            }
            RefreshConditionDisplay();
        }
        
        public void UpdateState(EntityFSMState state)
        {
            State = state;
            UpdateTitle();
            BuildNodeContents();
        }

        public void RefreshConditionDisplay()
        {
            if (State == null) return;

            for (int i = 0; i < conditionPorts.Count; i++)
            {
                var stack = conditionPorts[i].ConditionStack;
                var port = conditionPorts[i].Port;
                if (port == null) continue;
                port.portName = GetConditionPortLabel(stack, i);
                port.tooltip = GetConditionPortTooltip(stack, i);
            }

            if (conditionsList != null)
            {
                PopulateConditions(conditionsList);
            }
        }
        
        #endregion
        
        #region Setup Methods
        
        private void SetupStateNodeClasses()
        {
            AddToClassList("state-node");
        }
        
        private void BuildNodeContents()
        {
            inputContainer.Clear();
            outputContainer.Clear();
            extensionContainer.Clear();
            conditionPorts.Clear();

            BuildPorts();
            BuildSummary();

            RefreshExpandedState();
            RefreshPorts();
        }
        
        private void BuildPorts()
        {
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "In";
            inputPort.AddToClassList("state-node-port");
            if (edgeConnectorListener != null)
            {
                inputPort.AddManipulator(new EdgeConnector<Edge>(edgeConnectorListener));
            }
            inputContainer.Add(inputPort);

            var stacks = State?.conditions;
            if (stacks == null || stacks.Length == 0)
            {
                var emptyPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                emptyPort.portName = "No conditions";
                emptyPort.AddToClassList("state-node-port");
                if (edgeConnectorListener != null)
                {
                    emptyPort.AddManipulator(new EdgeConnector<Edge>(edgeConnectorListener));
                }
                outputContainer.Add(emptyPort);
                return;
            }

            for (int i = 0; i < stacks.Length; i++)
            {
                var stack = stacks[i];
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                port.portName = GetConditionPortLabel(stack, i);
                port.tooltip = GetConditionPortTooltip(stack, i);
                port.AddToClassList("state-node-port");
                if (edgeConnectorListener != null)
                {
                    port.AddManipulator(new EdgeConnector<Edge>(edgeConnectorListener));
                }
                outputContainer.Add(port);
                conditionPorts.Add(new ConditionPortInfo(port, stack));
            }
        }
        
        private void BuildSummary()
        {
            summaryContainer = new VisualElement();
            summaryContainer.AddToClassList("state-node-summary");

            var actionsHeader = new Label("Actions");
            actionsHeader.AddToClassList("state-node-section-header");
            summaryContainer.Add(actionsHeader);

            actionsList = new VisualElement();
            actionsList.AddToClassList("state-node-section-list");
            PopulateActions(actionsList);
            summaryContainer.Add(actionsList);

            var conditionsHeader = new Label("Conditions");
            conditionsHeader.AddToClassList("state-node-section-header");
            summaryContainer.Add(conditionsHeader);

            conditionsList = new VisualElement();
            conditionsList.AddToClassList("state-node-section-list");
            PopulateConditions(conditionsList);
            summaryContainer.Add(conditionsList);

            extensionContainer.Add(summaryContainer);
        }

        private bool NeedsPortRebuild()
        {
            int conditionCount = State?.conditions?.Length ?? 0;
            int expectedOutputCount = conditionCount == 0 ? 1 : conditionCount;
            return outputContainer.childCount != expectedOutputCount;
        }
        
        private void PopulateActions(VisualElement container)
        {
            container.Clear();
            if (State?.actions == null || State.actions.Length == 0)
            {
                container.Add(CreateEmptyLabel("No actions"));
                return;
            }

            foreach (var action in State.actions)
            {
                var label = action == null
                    ? "Missing Action"
                    : ObjectNames.NicifyVariableName(action.GetType().Name);
                container.Add(CreateItemLabel(label));
            }
        }
        
        private void PopulateConditions(VisualElement container)
        {
            container.Clear();
            if (State?.conditions == null || State.conditions.Length == 0)
            {
                container.Add(CreateEmptyLabel("No conditions"));
                return;
            }

            for (int i = 0; i < State.conditions.Length; i++)
            {
                var stack = State.conditions[i];
                var name = GetConditionStackName(stack, i);
                var target = stack != null && !string.IsNullOrEmpty(stack.ExitState) ? stack.ExitState : "No target";
                var op = stack != null ? stack.Operator.ToString().ToUpperInvariant() : "UNKNOWN";
                var count = stack?.Conditions?.Length ?? 0;
                var label = $"{name} ({op}, {count} checks → {target})";
                container.Add(CreateItemLabel(label));
            }
        }
        
        private Label CreateItemLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("state-node-section-item");
            return label;
        }
        
        private Label CreateEmptyLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("state-node-section-empty");
            return label;
        }
        
        private string GetConditionPortLabel(ConditionStack stack, int index)
        {
            var name = GetConditionStackName(stack, index);
            var target = stack != null && !string.IsNullOrEmpty(stack.ExitState) ? stack.ExitState : "No target";
            return $"{name} → {target}";
        }
        
        private string GetConditionPortTooltip(ConditionStack stack, int index)
        {
            var name = GetConditionStackName(stack, index);
            var op = stack != null ? stack.Operator.ToString().ToUpperInvariant() : "UNKNOWN";
            var target = stack != null && !string.IsNullOrEmpty(stack.ExitState) ? stack.ExitState : "No target";
            var count = stack?.Conditions?.Length ?? 0;
            return $"{name}\nOperator: {op}\nChecks: {count}\nExit: {target}";
        }
        
        private string GetConditionStackName(ConditionStack stack, int index)
        {
            if (stack != null && !string.IsNullOrEmpty(stack.StackName))
            {
                return stack.StackName;
            }

            return $"Condition Stack {index + 1}";
        }
        
        private void RegisterEventHandlers()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
        }
        
        #endregion
        
        #region Selection Overrides
        
        public override void OnSelected()
        {
            base.OnSelected();
            NotifyNodeSelected();
        }
        
        public override void OnUnselected()
        {
            base.OnUnselected();
            NotifyNodeDeselected();
        }
        
        private void NotifyNodeSelected()
        {
            OnNodeSelected?.Invoke(this);
        }
        
        private void NotifyNodeDeselected()
        {
            OnNodeDeselected?.Invoke(this);
        }
        
        #endregion
        
        #region Mouse Event Handlers
        
        /// <summary>
        /// Handles mouse down events (left and right click).
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0) // Left mouse button
            {
                HandleLeftClick(evt);
            }
            else if (evt.button == 1) // Right mouse button
            {
                HandleRightClick(evt);
            }
        }
        
        /// <summary>
        /// Handles left-click input on the node.
        /// Override this method or subscribe to events to customize behavior.
        /// </summary>
        private void HandleLeftClick(MouseDownEvent evt)
        {
        }
        
        /// <summary>
        /// Handles right-click input on the node.
        /// Override this method to add custom right-click behavior.
        /// Note: The contextual menu will still be shown unless you call evt.StopPropagation().
        /// </summary>
        private void HandleRightClick(MouseDownEvent evt)
        {
            //nothing here for now :)
        }
        
        #endregion
        
        #region Contextual Menu
        
        /// <summary>
        /// Called when the contextual menu (right-click menu) is being populated.
        /// Override this method to add custom menu items to the right-click menu.
        /// </summary>
        private void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            // add SetAsInitial action
            evt.menu.AppendAction("Set as Initial", (x) =>
            {
                // Use 'this' since we're already in the StateNode context
                if (State == null || string.IsNullOrEmpty(State.stateName))
                {
                    Debug.LogError("[StateNode] Cannot set as initial - state is null or has no name.");
                    return;
                }
                
                if (ownerLogic == null)
                {
                    Debug.LogError("[StateNode] Cannot set as initial - ownerLogic is null.");
                    return;
                }
                
                ownerLogic.SetFirstState(State.stateName);
                
                // Refresh the graph view to reflect the new state order
                EntityNodalWindow.RefreshGraphView();
            });
        }
        
        #endregion
    }
}
