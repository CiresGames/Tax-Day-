using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a SphereCollider value.
    /// </summary>
    [Serializable]
    public sealed class InlineSphereColliderFunction : IFB_TopLayer<SphereCollider, ISB_FUNCTION<SphereCollider>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<SphereCollider>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.SphereCollider"), "UnityEngine.SphereCollider",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public SphereCollider Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public SphereCollider ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
