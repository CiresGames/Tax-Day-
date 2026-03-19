using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a Vector4 value.
    /// </summary>
    [Serializable]
    public sealed class InlineVector4Function : IFB_TopLayer<Vector4, ISB_FUNCTION<Vector4>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<Vector4>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.Vector4"), "UnityEngine.Vector4",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public Vector4 Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public Vector4 ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
