using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning an AudioSource value.
    /// </summary>
    [Serializable]
    public sealed class InlineAudioSourceFunction : IFB_TopLayer<AudioSource, ISB_FUNCTION<AudioSource>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<AudioSource>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.AudioSource"), "UnityEngine.AudioSource",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public AudioSource Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public AudioSource ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
