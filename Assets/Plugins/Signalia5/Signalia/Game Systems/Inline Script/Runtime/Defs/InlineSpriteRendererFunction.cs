using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a SpriteRenderer value.
    /// </summary>
    [Serializable]
    public sealed class InlineSpriteRendererFunction : IFB_TopLayer<SpriteRenderer, ISB_FUNCTION<SpriteRenderer>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<SpriteRenderer>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.SpriteRenderer"), "UnityEngine.SpriteRenderer",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public SpriteRenderer Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public SpriteRenderer ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
