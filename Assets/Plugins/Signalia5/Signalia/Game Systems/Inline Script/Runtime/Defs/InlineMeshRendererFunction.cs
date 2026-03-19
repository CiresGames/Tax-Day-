using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a MeshRenderer value.
    /// </summary>
    [Serializable]
    public sealed class InlineMeshRendererFunction : IFB_TopLayer<MeshRenderer, ISB_FUNCTION<MeshRenderer>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<MeshRenderer>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.MeshRenderer"), "UnityEngine.MeshRenderer",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public MeshRenderer Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public MeshRenderer ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
