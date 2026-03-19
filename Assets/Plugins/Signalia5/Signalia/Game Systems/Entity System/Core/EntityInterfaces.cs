using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    public interface IEntityStateCallbacks
    {
        /// <summary>
        /// Called when the state is entered. Once.
        /// </summary>
        void OnStateEnter();
        
        /// <summary>
        /// Called every frame while the state is active.
        /// </summary>
        void OnStateTick();
        
        /// <summary>
        /// Called when the state is exited. Once.
        /// </summary>
        void OnStateExit();
    }
}
