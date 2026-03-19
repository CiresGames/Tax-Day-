using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    [Icon("Assets/AHAKuo Creations/Signalia/Framework/Graphics/Icons/SIGS_EDITOR_ICON_ENTITYACTION.png")]
    public abstract class EntityAction : EntityComponent
    {
        public override bool ConditionFunction => false; // this is not a condition, so just return false.
        public override bool IsAction => true;
    }
}
