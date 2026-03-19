using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a ParticleSystem value.
    /// </summary>
    [Serializable]
    public sealed class InlineParticleSystemFunction : IFB_TopLayer<ParticleSystem, ISB_FUNCTION<ParticleSystem>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<ParticleSystem>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.ParticleSystem"), "UnityEngine.ParticleSystem",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public ParticleSystem Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public ParticleSystem ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
