using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a BoxCollider value.
    /// </summary>
    [Serializable]
    public sealed class InlineBoxColliderFunction : IFB_TopLayer<BoxCollider, ISB_FUNCTION<BoxCollider>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<BoxCollider>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.BoxCollider"), "UnityEngine.BoxCollider",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public BoxCollider Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public BoxCollider ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
