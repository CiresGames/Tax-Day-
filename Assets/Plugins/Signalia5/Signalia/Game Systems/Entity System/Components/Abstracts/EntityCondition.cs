using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities
{
    [Icon("Assets/AHAKuo Creations/Signalia/Framework/Graphics/Icons/SIGS_EDITOR_ICON_ENTITYCONDITION.png")]
    public abstract class EntityCondition : EntityComponent
    {
        /// <summary>
        /// Returns condition result.
        /// </summary>
        /// <returns></returns>
        public abstract bool ConditionResult();

        public override bool ConditionFunction => ConditionResult();
        public override bool IsAction => false;
    }
}
