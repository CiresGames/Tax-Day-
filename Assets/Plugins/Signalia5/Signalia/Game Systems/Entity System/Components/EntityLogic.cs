using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    /// <summary>
    /// Handles the connection between flow and logic pieces or abstracts.
    /// Uses a data asset to know and mark the flow of the logic.
    /// </summary>
    public sealed class EntityLogic : MonoBehaviour
    {
        [SerializeField] private bool isDisabled = false;
        [SerializeField] private string logicName;
        [SerializeField] private string logicDescription;
        [SerializeField] private float tickBuffer = 0; // closer to zero, faster brain,
        [SerializeField] private EntityFSMState[] states; // the states are the actual logic pieces.
        
        // state tracking
        public EntityFSMState CurrentState { get; private set; }
        public bool IsInState(EntityFSMState state) => CurrentState == state;
        public string TransitionTarget => CurrentState.ExitStateTarget; // if this has value, then a transition has been requested and will happen on next tick.
        
        // time tracking
        public float TimeInState { get; private set; }
        public float TimeInLogic { get; private set; }
        
        /// <summary>
        /// Logic has nothing to do.
        /// </summary>
        public bool InvalidLogic => states == null || states.Length == 0;
        public bool UnreadyLogic => CurrentState == null;
        public bool IsDisabled => isDisabled;
        
#if UNITY_EDITOR
        // Editor accessors for nodal editor
        public EntityFSMState[] GetStates() => states;
        public string GetLogicName() => logicName;
        public string GetLogicDescription() => logicDescription;
        
        /// <summary>
        /// Adds a new state to the states array. Returns the index of the newly added state.
        /// </summary>
        public int AddState(string stateName = null)
        {
            if (states == null)
            {
                states = new EntityFSMState[0];
            }
            
            // Create new state
            var newState = new EntityFSMState
            {
                stateName = stateName ?? $"New State {states.Length + 1}",
                actions = new EntityAction[0],
                conditions = new ConditionStack[0]
            };
            
            // Resize array and add new state
            var newStates = new EntityFSMState[states.Length + 1];
            System.Array.Copy(states, newStates, states.Length);
            newStates[states.Length] = newState;
            states = newStates;
            
            // Sync node data
            SyncNodeDataToStates();
            
            return states.Length - 1;
        }
        
        /// <summary>
        /// Removes a state at the specified index.
        /// </summary>
        public void RemoveState(int index)
        {
            if (states == null || index < 0 || index >= states.Length)
                return;
            
            var newStates = new EntityFSMState[states.Length - 1];
            for (int i = 0, j = 0; i < states.Length; i++)
            {
                if (i != index)
                {
                    newStates[j++] = states[i];
                }
            }
            states = newStates;
            
            // Sync node data
            SyncNodeDataToStates();
        }
        
        /// <summary>
        /// Sets the name of a state at the specified index.
        /// </summary>
        public void SetStateName(int index, string name)
        {
            if (states == null || index < 0 || index >= states.Length)
                return;
            
            states[index].stateName = name;
            SyncNodeDataToStates();
        }
#endif

        // runtime
        private Entity entity;
        private EntityCentral central => entity.Central;
        private float lastTickTime;

        /// <summary>
        /// Transitions to the specified state. Different than entering a state, which is invoked immediately.
        /// </summary>
        /// <param name="stateName"></param>
        public void TransitionToState(string stateName)
        {
            if (CurrentState == null)
            {
                // developer should not transition when they didn't even enter or have entered a state yet.
                Debug.LogError($"[EntityLogic] Cannot transition to state {stateName} as no state has been entered yet.");
                return;
            }
            
            var targetState = states.FirstOrDefault(x => x.stateName.Equals(stateName, System.StringComparison.OrdinalIgnoreCase));
            
            if (targetState == null)
            {
                Debug.LogError($"[EntityLogic] Could not transition to state {stateName} as it does not exist.");
                return;
            }
            
            ExitState(); // on the current state
            
            CurrentState = targetState;
            CurrentState.EnterState();
        }
        
        /// <summary>
        /// Happens when entering the logic for the first time. Different than transitioning to a state, which happens within the same logic.
        /// </summary>
        /// <param name="stateName"></param>
        public void EnterState(string stateName)
        {
            CurrentState = states.FirstOrDefault(x =>
                x.stateName.Equals(stateName, System.StringComparison.OrdinalIgnoreCase));
            CurrentState?.EnterState();
            
            TimerReset_Logic();
            TimerReset_State();
        }
        
        /// <summary>
        /// Happens when entering the logic for the first time. Different than transitioning to a state, which happens within the same logic.
        /// </summary>
        /// <param name="stateIndex"></param>
        public void EnterState(int stateIndex)
        {
            CurrentState = states[stateIndex];
            CurrentState?.EnterState();
            
            TimerReset_Logic();
            TimerReset_State();
        }
        
        private void TimerReset_State() => TimeInState = 0;
        private void TimerReset_Logic() => TimeInLogic = 0;
        
        public void TickFrame()
        {
            if (CurrentState == null)
            {
                // this is not possible, as the Entity would have assigned a state to go through and it would not invoke ticking if this state is invalid.
            }
            
            var buffering = Time.time - lastTickTime < tickBuffer;
            if (buffering) return;
            CurrentState?.TickState();
            CurrentState?.EvalConditions();
            lastTickTime = Time.time;
        }

        public void ExitState()
        {
            CurrentState.ExitState();
            CurrentState = null;
        }
        
        /// <summary>
        /// Nothing too fancy, just tells the logic which entity it belongs to so it can access its central.
        /// </summary>
        /// <param name="owner"></param>
        public void Setup(Entity owner)
        {
            entity = owner;
        }

        public void TransitionToRequested()
        {
            TransitionToState(TransitionTarget);
        }

        // the following is a section specific for the nodal editor window, to store node-specific data like positions, zoom, etc... Connection remains on the logic itself.
#if UNITY_EDITOR
        [SerializeField] private List<NodeData> _serializedNodeData = new List<NodeData>();
        private Dictionary<int, NodeData> _nodes;
        private int _selectedNodeIndex;

        private void InitializeNodeData()
        {
            if (_nodes == null)
            {
                _nodes = new Dictionary<int, NodeData>();
            }
            
            // Sync from serialized data
            if (states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    if (i < _serializedNodeData.Count)
                    {
                        _nodes[i] = _serializedNodeData[i];
                    }
                    else
                    {
                        // Create new node data for states that don't have serialized data
                        var nodeData = new NodeData(states[i].stateName, new Vector2(100 + i * 200, 100));
                        _nodes[i] = nodeData;
                        _serializedNodeData.Add(nodeData);
                    }
                }
                
                // Remove any extra serialized data for states that no longer exist
                while (_serializedNodeData.Count > states.Length)
                {
                    _serializedNodeData.RemoveAt(_serializedNodeData.Count - 1);
                }
            }
        }

        public NodeData RetrieveNodeData(int index)
        {
            InitializeNodeData();
            if (_nodes.ContainsKey(index))
                return _nodes[index];
            
            // Return default if not found
            return new NodeData("", Vector2.zero);
        }
        
        public NodeData RetrieveSelectedNodeData()
        {
            InitializeNodeData();
            if (_nodes.ContainsKey(_selectedNodeIndex))
                return _nodes[_selectedNodeIndex];
            return new NodeData("", Vector2.zero);
        }
        
        public void SetSelectedNodeIndex(int index) => _selectedNodeIndex = index;
        
        public void SetNodePosition(int nodeIndex, Vector2 position)
        {
            InitializeNodeData();
            var nd = RetrieveNodeData(nodeIndex);
            nd.position = position;
            _nodes[nodeIndex] = nd;
            
            // Update serialized data
            if (nodeIndex < _serializedNodeData.Count)
            {
                _serializedNodeData[nodeIndex] = nd;
            }
            else
            {
                // Add new entry if index is out of bounds
                while (_serializedNodeData.Count <= nodeIndex)
                {
                    _serializedNodeData.Add(new NodeData("", Vector2.zero));
                }
                _serializedNodeData[nodeIndex] = nd;
            }
        }
        
        public void SyncNodeDataToStates()
        {
            InitializeNodeData();
            
            // Ensure serialized data matches states array
            if (states != null)
            {
                // Update existing entries and add new ones
                for (int i = 0; i < states.Length; i++)
                {
                    if (i < _serializedNodeData.Count)
                    {
                        var nd = _serializedNodeData[i];
                        nd.stateName = states[i].stateName;
                        _serializedNodeData[i] = nd;
                        _nodes[i] = nd;
                    }
                    else
                    {
                        var nodeData = new NodeData(states[i].stateName, new Vector2(100 + i * 200, 100));
                        _serializedNodeData.Add(nodeData);
                        _nodes[i] = nodeData;
                    }
                }
                
                // Remove extra entries
                while (_serializedNodeData.Count > states.Length)
                {
                    _serializedNodeData.RemoveAt(_serializedNodeData.Count - 1);
                }
            }
        }
        
        [Serializable]
        public struct NodeData
        {
            public string stateName;
            public Vector2 position;

            public NodeData(string name, Vector2 pos) : this()
            {
                stateName = name;
                position = pos;
            }
        }
        
        // component retrieval
        public EntityComponent[] EntityComponents
        {
            get
            {
                var parentEntity = GetComponentInParent<Entity>();
                if (parentEntity == null)
                {
                    Debug.LogError("EntityLogic is not attached to an Entity! Add this logic as a child to an Entity.");
                    return Array.Empty<EntityComponent>();
                }

                return parentEntity.GetComponentsInChildren<EntityComponent>();
            }
        }
        
        /// <summary>
        /// Renames the exit target of all condition stacks to match the new state name. For renaming states in the nodal window.
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="evtNewValue"></param>
        public void RenameExitState(string oldName, string evtNewValue)
        {
            var condtionStacks = states.SelectMany(x => x.conditions.Where(x => x.ExitState == oldName));

            foreach (var condtionStack in condtionStacks)
            {
                condtionStack.SetExitState(evtNewValue);
            }
            
            // set dirty
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Changes the first state in the array.
        /// </summary>
        /// <param name="nodeName"></param>
        public void SetFirstState(string nodeName)
        {
            if (states == null || states.Length == 0)
            {
                Debug.LogError($"[EntityLogic] Cannot set first state - states array is null or empty.");
                return;
            }
            
            var statesToList = states.ToList();
            var index = statesToList.FindIndex(x => x.stateName.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
            
            if (index < 0)
            {
                Debug.LogError($"[EntityLogic] Could not find state {nodeName} to set as first state.");
                return;
            }
            
            // If already first, no need to reorder
            if (index == 0)
                return;
            
            // Remove the state from its current position and insert it at the beginning
            var stateToMove = statesToList[index];
            statesToList.RemoveAt(index);
            statesToList.Insert(0, stateToMove);
            
            // Update the states array
            states = statesToList.ToArray();
            
            // Sync node data to maintain consistency
            SyncNodeDataToStates();
            
            // Mark as dirty for editor
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
