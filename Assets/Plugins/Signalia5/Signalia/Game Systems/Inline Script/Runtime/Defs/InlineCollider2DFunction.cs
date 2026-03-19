using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a Collider2D value.
    /// </summary>
    [Serializable]
    public sealed class InlineCollider2DFunction : IFB_TopLayer<Collider2D, ISB_FUNCTION<Collider2D>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<Collider2D>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.Collider2D"), "UnityEngine.Collider2D",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public Collider2D Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public Collider2D ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
