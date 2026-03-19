using System;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    public enum EntityType
    {
        Player = 0,
        AI = 1
    }
    
    public enum ConditionStackOperator
    {
        And = 0,
        Or = 1,
    }
    
    public enum EntityHealthState
    {
        Alive = 0,
        Dead = 1
    }

    public enum EntityGroundedState
    {
        Grounded = 0,
        Aerial = 1
    }

    [Flags]
    public enum EntityLogicStopMode
    {
        None = 0,
        WhenDead = 1,
        WhenTimeZero = 2,
    }
}
