using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a RectTransform value.
    /// </summary>
    [Serializable]
    public sealed class InlineRectTransformFunction : IFB_TopLayer<RectTransform, ISB_FUNCTION<RectTransform>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<RectTransform>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.RectTransform"), "UnityEngine.RectTransform",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public RectTransform Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public RectTransform ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
