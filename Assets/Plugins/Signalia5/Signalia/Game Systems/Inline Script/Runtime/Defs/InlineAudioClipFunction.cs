using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning an AudioClip value.
    /// </summary>
    [Serializable]
    public sealed class InlineAudioClipFunction : IFB_TopLayer<AudioClip, ISB_FUNCTION<AudioClip>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<AudioClip>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.AudioClip"), "UnityEngine.AudioClip",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public AudioClip Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public AudioClip ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
