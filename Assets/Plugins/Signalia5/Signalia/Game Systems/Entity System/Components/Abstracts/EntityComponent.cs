using System;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    /// <summary>
    /// Base abstract class for EntityAction and EntityCondition.
    /// Contains common properties and functionality shared between actions and conditions.
    /// </summary>
    public abstract class EntityComponent : MonoBehaviour, IEntityStateCallbacks
    {
        [SerializeField] private string _name;
        [SerializeField] private string _description;

        public string Name => _name;
        public string Description => _description;
        
        public abstract bool ConditionFunction { get; }
        public abstract bool IsAction { get; }
        
        // entity owner
        protected EntityCentral Central { get; set; }

        public void AssignCentral(EntityCentral ec)
        {
            Central = ec;
        }

        public virtual void OnStateEnter()
        {
        }

        public virtual void OnStateTick()
        {
        }

        public virtual void OnStateExit()
        {
        }
    }
}
