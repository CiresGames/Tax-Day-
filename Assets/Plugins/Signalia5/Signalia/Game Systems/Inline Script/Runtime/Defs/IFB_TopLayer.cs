using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Base class for inline functions returning a value.
    /// Should not be used externally or outside scope of asset.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the function.</typeparam>
    /// <typeparam name="TBehaviour">The boilerplate behaviour type.</typeparam>
    [Serializable]
    public abstract class IFB_TopLayer<TResult, TBehaviour> : ISB_TopLayer
        where TBehaviour : ISB_FUNCTION<TResult>
    {
        protected TResult ReturnInternal(Func<TBehaviour, TResult> func, string operationName)
        {
            return ExecuteBehaviour(behaviour =>
            {
                if (behaviour is TBehaviour typedBehaviour)
                {
                    return func != null ? func(typedBehaviour) : default;
                }

                Debug.LogError($"InlineScript: Behaviour '{behaviour.GetType().FullName}' cannot be cast to {typeof(TBehaviour).FullName}.");
                return default;
            }, operationName);
        }
    }
}
