using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a string value.
    /// </summary>
    [Serializable]
    public sealed class InlineStringFunction : IFB_TopLayer<string, ISB_FUNCTION<string>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<string>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "string"), "string",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public string Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public string ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
