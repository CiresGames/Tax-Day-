using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a Color value.
    /// </summary>
    [Serializable]
    public sealed class InlineColorFunction : IFB_TopLayer<Color, ISB_FUNCTION<Color>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<Color>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.Color"), "UnityEngine.Color",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public Color Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public Color ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
