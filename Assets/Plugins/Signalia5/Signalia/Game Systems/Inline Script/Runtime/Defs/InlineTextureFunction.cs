using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a Texture value.
    /// </summary>
    [Serializable]
    public sealed class InlineTextureFunction : IFB_TopLayer<Texture, ISB_FUNCTION<Texture>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<Texture>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.Texture"), "UnityEngine.Texture",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public Texture Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public Texture ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
