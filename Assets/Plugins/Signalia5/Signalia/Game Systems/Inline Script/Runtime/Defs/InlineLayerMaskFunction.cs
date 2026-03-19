using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a LayerMask value.
    /// </summary>
    [Serializable]
    public sealed class InlineLayerMaskFunction : IFB_TopLayer<LayerMask, ISB_FUNCTION<LayerMask>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<LayerMask>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.LayerMask"), "UnityEngine.LayerMask",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public LayerMask Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public LayerMask ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
