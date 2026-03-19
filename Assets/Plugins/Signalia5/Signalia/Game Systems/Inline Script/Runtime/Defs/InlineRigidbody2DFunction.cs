using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a Rigidbody2D value.
    /// </summary>
    [Serializable]
    public sealed class InlineRigidbody2DFunction : IFB_TopLayer<Rigidbody2D, ISB_FUNCTION<Rigidbody2D>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<Rigidbody2D>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.Rigidbody2D"), "UnityEngine.Rigidbody2D",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public Rigidbody2D Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public Rigidbody2D ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
