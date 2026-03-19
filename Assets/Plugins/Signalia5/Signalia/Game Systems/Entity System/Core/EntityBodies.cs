using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AHAKuo.Signalia.Utilities;
using UnityEngine;
using UnityEngine.AI;

using AHAKuo.Signalia.GameSystems.Health;
using AHAKuo.Signalia.GameSystems.Movement;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    /// <summary>
    /// A condition stack is a collection of EntityConditions that need to pass a certain check.
    /// Used in EntityLogic to determine how an entity goes to and exits states.
    /// </summary>
    [Serializable]
    public class ConditionStack : IEntityStateCallbacks
    {
        [SerializeField] private string stackName;
        [SerializeField] private string exitState; // defines where the entity logic will transition to if the stack passes.
        [SerializeField] private ConditionStackOperator _operator = ConditionStackOperator.And;
        [SerializeField] private ConditionAndExpectation[] _conditions;

        public string StackName => stackName;
        public string ExitState => exitState;
        public ConditionStackOperator Operator => _operator;
        public ConditionAndExpectation[] Conditions => _conditions;

        public void SetExitState(string exitStateValue)
        {
            exitState = exitStateValue;
        }
        
        /// <summary>
        /// Merely for editor tools like the node window so it can adjust the node's state fsm condition stack. Can be improved for needed logic.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="exitState"></param>
        /// <param name="conditions"></param>
        public void SetOptions(ConditionStackOperator op, string exitState, ConditionAndExpectation[] conditions)
        {
            _operator = op;
            this.exitState = exitState;
            _conditions = conditions;
        }
        
        /// <summary>
        /// Returns true if the stack passes as expected.
        /// </summary>
        public bool StackPassed(out string exitStateToSet)
        {
            var pass = _operator switch
            {
                ConditionStackOperator.And => _conditions.All(x => x.AsExpected),
                ConditionStackOperator.Or => _conditions.Any(x => x.AsExpected),
                _ => throw new ArgumentOutOfRangeException()
            };

            exitStateToSet = pass ? exitState : string.Empty;

            if (pass && exitStateToSet.IsNullOrEmpty())
            {
                Debug.LogWarning($"[Entity Logic] Condition stack '{stackName}' passed but no exit state was set.");
            }
            
            return pass;
        }

        public void OnStateEnter()
        {
            foreach (var conditionAndExpectation in _conditions)
            {
                conditionAndExpectation.OnStateEnter();
            }
        }

        public void OnStateTick()
        {
            foreach (var conditionAndExpectation in _conditions)
            {
                conditionAndExpectation.OnStateTick();
            }
        }

        public void OnStateExit()
        {
            foreach (var conditionAndExpectation in _conditions)
            {
                conditionAndExpectation.OnStateExit();
            }
        }
    }
    
    /// <summary>
    /// A collection of condition + expectation.
    /// </summary>
    [Serializable]
    public class ConditionAndExpectation : IEntityStateCallbacks
    {
        [SerializeField] private EntityCondition _condition;
        [SerializeField] private bool _expect;

        public bool AsExpected => _condition.ConditionResult() == _expect;
        
        public void OnStateEnter()
        {
            _condition.OnStateEnter();
        }

        public void OnStateTick()
        {
            _condition.OnStateTick();
        }

        public void OnStateExit()
        {
            _condition.OnStateExit();
        }
    }
    
    /// <summary>
    /// Connection central for game systems: Health and Movement or other components that make up an entity.
    /// Provides centralized access to all regional systems and Unity components.
    /// </summary>
    [Serializable]
    public class EntityCentral
    {
        // unity components \\
        public Rigidbody Rigidbody { get; private set; }
        public Rigidbody2D Rigidbody2D { get; private set; }
        public Collider Collider { get; private set; }
        public Collider2D Collider2D { get; private set; }
        public NavMeshAgent NavMeshAgent { get; private set; }
        
        /// <summary>
        /// The object that the entity is currently targeting. Usually a player or object. This is set by the entity scanner settings.
        /// </summary>
        public Transform TargetOfInterest { get; private set; } // todo: make this a component?
        
        // regional systems \\
        public ObjectHealth Health { get; private set; }
        public Damager[] Damagers { get; private set; }
        
        public IMovementPhysicsAuthority PhysicsAuthority { get; private set; }
        public MovementPhysicsModifier[] PhysicsModifiers { get; private set; }
        
        // entity systems \\
        public EntityLogic[] Logics { get; private set; }
        
        // entity actions and conditions \\
        public EntityComponent[] EntityComponents { get; private set; }
        
        /// <summary>
        /// Collects and caches all components from the entity object hierarchy.
        /// Unity components, regional systems (Health, Movement), and entity-specific components are gathered here.
        /// </summary>
        /// <param name="entityHeader">The root GameObject of the entity</param>
        public void Build(GameObject entityHeader)
        {
            // unity components
            Rigidbody  = entityHeader.GetComponentInChildren<Rigidbody>();
            Rigidbody2D = entityHeader.GetComponentInChildren<Rigidbody2D>();
            Collider  = entityHeader.GetComponentInChildren<Collider>();
            Collider2D = entityHeader.GetComponentInChildren<Collider2D>();
            
            // health system
            Health  = entityHeader.GetComponentInChildren<ObjectHealth>();
            Damagers = entityHeader.GetComponentsInChildren<Damager>();
            
            // movement system
            PhysicsAuthority = entityHeader.GetComponentInChildren<IMovementPhysicsAuthority>();
            // todo: add 2d when its ready
            PhysicsModifiers = entityHeader.GetComponentsInChildren<MovementPhysicsModifier>();
            
            // entity systems
            Logics = entityHeader.GetComponentsInChildren<EntityLogic>();
            EntityComponents = entityHeader.GetComponentsInChildren<EntityComponent>();
            
            // assign central to EntityComponents (i hope it's not ugly -_-')
            foreach (var entityComponent in EntityComponents)
            {
                entityComponent.AssignCentral(this);
            }
        }
    }
    
    /// <summary>
    /// Describes a state of an entity logic during which Actions and Conditions can be evaluated.
    /// </summary>
    [Serializable]
    public class EntityFSMState
    {
        public string stateName;
        public EntityAction[] actions;
        public ConditionStack[] conditions;

        /// <summary>
        /// This is the runtime read value by the Entity script that notifies the Entity that it should transition to this state.
        /// IT IS NOT THE EXIT TARGET INSIDE THE CONDITION STACKS.
        /// DO NOT MANUALLY SET THIS VALUE.
        /// </summary>
        public string ExitStateTarget { get; private set; } // default empty. An exit string is set immediately from the first passing condition stack, and the entity logic will transition to that state.
        
        /// <summary>
        /// Invoked via EntityLogic.
        /// </summary>
        public void EnterState()
        {
            foreach (var entityAction in actions)
            {
                entityAction.OnStateEnter();
            }

            foreach (var conditionStack in conditions)
            {
                conditionStack.OnStateEnter();
            }
            
            // reset exit state
            ExitStateTarget = string.Empty;
        }
        
        /// <summary>
        /// Invoked via EntityLogic.
        /// </summary>
        public void TickState()
        {
            if (ExitStateTarget.HasValue())
                return; // if we have an exit state, this means this state is about to or already exiting, so don't tick it.
            
            foreach (var entityAction in actions)
            {
                entityAction.OnStateTick();
            }
            
            foreach (var conditionStack in conditions)
            {
                conditionStack.OnStateTick();
            }
        }

        public void EvalConditions()
        {
            foreach (var conditionStack in conditions)
            {
                conditionStack.StackPassed(out var s);
                ExitStateTarget = s;
                if (!ExitStateTarget.HasValue()) continue;
                break; // we have value, so, break out of the loop as EntityLogic should transition us eventually.
            }
        }
        
        /// <summary>
        /// Invoked via EntityLogic. On the next tick frame that entity logic does, it will check if an exit is set, and if so, will invoke this.
        /// </summary>
        public void ExitState()
        {
            foreach (var entityAction in actions)
            {
                entityAction.OnStateExit();
            }
            
            foreach (var conditionStack in conditions)
            {
                conditionStack.OnStateExit();
            }
        }
    }
}
