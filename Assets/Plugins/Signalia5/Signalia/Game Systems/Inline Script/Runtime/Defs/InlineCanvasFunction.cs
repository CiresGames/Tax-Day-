using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a Canvas value.
    /// </summary>
    [Serializable]
    public sealed class InlineCanvasFunction : IFB_TopLayer<Canvas, ISB_FUNCTION<Canvas>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<Canvas>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.Canvas"), "UnityEngine.Canvas",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public Canvas Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public Canvas ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
