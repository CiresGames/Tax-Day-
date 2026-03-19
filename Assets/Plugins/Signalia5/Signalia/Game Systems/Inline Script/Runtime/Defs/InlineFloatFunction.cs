using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a float value.
    /// </summary>
    [Serializable]
    public sealed class InlineFloatFunction : IFB_TopLayer<float, ISB_FUNCTION<float>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<float>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "float"), "float",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public float Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public float ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
