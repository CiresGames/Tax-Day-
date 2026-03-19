using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils
{
    /// <summary>
    /// DO NOT INHERIT OR USE OUTSIDE SCOPE.
    /// </summary>
    public abstract class ISB_FUNCTION<TResult> : ISB_BASE
    {
        public virtual TResult Evaluate(GameObject gameObject = null)
        {
            base.ExecuteCode(gameObject);
            return default;
        }
    }
}
