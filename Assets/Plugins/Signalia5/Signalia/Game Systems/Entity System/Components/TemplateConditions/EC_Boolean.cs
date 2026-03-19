namespace AHAKuo.Signalia.GameSystems.Entities.Templates
{
    public class EC_Boolean : EntityCondition
    {
        public bool returnValue;

        public override bool ConditionResult()
        {
            return returnValue;
        }
    }
}
