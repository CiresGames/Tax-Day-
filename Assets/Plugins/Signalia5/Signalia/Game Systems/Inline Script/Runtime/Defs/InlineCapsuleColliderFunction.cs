using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a CapsuleCollider value.
    /// </summary>
    [Serializable]
    public sealed class InlineCapsuleColliderFunction : IFB_TopLayer<CapsuleCollider, ISB_FUNCTION<CapsuleCollider>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<CapsuleCollider>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.CapsuleCollider"), "UnityEngine.CapsuleCollider",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public CapsuleCollider Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public CapsuleCollider ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
