using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a bool value.
    /// </summary>
    [Serializable]
    public sealed class InlineBoolFunction : IFB_TopLayer<bool, ISB_FUNCTION<bool>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<bool>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "bool"), "bool",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public bool Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public bool ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
