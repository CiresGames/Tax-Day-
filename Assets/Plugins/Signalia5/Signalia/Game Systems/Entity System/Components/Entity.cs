using System;
using System.Collections;
using System.Collections.Generic;
using AHAKuo.Signalia.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    /// <summary>
    /// A representation of an entity in the game.
    /// Placed on an object so it can be logic controlled or Player driven.
    /// This object also handles the Tick frame of Entity Logic and the Enter and Exit.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        [SerializeField] private EntityType entityType = EntityType.AI; // ai is default because it's the most recreated option.
        [SerializeField] private EntityLogic firstLogic; // if left empty, the first logic found will be used.
        [SerializeField] private EntityLogicStopMode stopMode = EntityLogicStopMode.None;
        
        
        public EntityCentral Central { get; private set; }
        
        // accessors
        public EntityType EntityType => entityType;
        
        // runtime
        public EntityLogic CurrentEntityLogic { get; private set; }
        public EntityFSMState CurrentEntityState => CurrentEntityLogic.CurrentState;

        // health system accessors \\
        private EntityHealthState currentHealthStatus => Central?.Health != null ? (Central.Health.IsDead ? EntityHealthState.Dead : EntityHealthState.Alive) : EntityHealthState.Alive;
        
        public bool IsDead => currentHealthStatus == EntityHealthState.Dead;
        public bool IsAlive => currentHealthStatus == EntityHealthState.Alive;

        // movement system accessors \\
        private EntityGroundedState currentGroundedState => Central?.PhysicsAuthority != null ? (Central.PhysicsAuthority.IsGrounded ? EntityGroundedState.Grounded : EntityGroundedState.Aerial) : EntityGroundedState.Grounded;
        
        public bool IsGrounded => currentGroundedState == EntityGroundedState.Grounded;
        public bool IsAerial => currentGroundedState == EntityGroundedState.Aerial;

        // logics
        private EntityLogic[] logics;

        private void Awake()
        {
            Central = new();
            Central.Build(gameObject);

            SetupLogics();
        }

        private void Start()
        {
            ProcessFirstLogic();
        }

        private void SetupLogics()
        {
            if (EntityType == EntityType.Player)
                return;
            
            logics = GetComponentsInChildren<EntityLogic>();

            if (logics == null || logics.Length == 0)
                return;

            foreach (var entityLogic in logics)
            {
                entityLogic.Setup(this);
            }
            
            CurrentEntityLogic = firstLogic ?? logics[0];
        }
        
        private void ProcessFirstLogic()
        {
            if (firstLogic != null)
                firstLogic.EnterState(0);
        }

        private void Update()
        {
            LogicProcess();
        }

        private void LogicProcess()
        {
            if (entityType == EntityType.Player
                || CurrentEntityLogic == null
                || CurrentEntityLogic.InvalidLogic
                || CurrentEntityLogic.IsDisabled)
                return;

            switch (stopMode)
            {
                case EntityLogicStopMode.None:
                    break;
                case EntityLogicStopMode.WhenDead:
                    if (IsDead)
                        return;
                    break;
                case EntityLogicStopMode.WhenTimeZero:
                    if (SignaliaTime.IsPaused)
                        return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // if not ready, enter the first state
            if (CurrentEntityLogic.UnreadyLogic)
            {
                CurrentEntityLogic.EnterState(0);
                return;
            }

            var transitionRequested = CurrentEntityLogic.TransitionTarget;

            if (transitionRequested.HasValue())
                CurrentEntityLogic.TransitionToRequested();
            else
            {
                CurrentEntityLogic.TickFrame();
            }
        }
    }
}
