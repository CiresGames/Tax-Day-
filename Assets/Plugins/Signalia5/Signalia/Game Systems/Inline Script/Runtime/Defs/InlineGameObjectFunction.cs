using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a GameObject value.
    /// </summary>
    [Serializable]
    public sealed class InlineGameObjectFunction : IFB_TopLayer<GameObject, ISB_FUNCTION<GameObject>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<GameObject>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.GameObject"), "UnityEngine.GameObject",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public GameObject Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public GameObject ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
