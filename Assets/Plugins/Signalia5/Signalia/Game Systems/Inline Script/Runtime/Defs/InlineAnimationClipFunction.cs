using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning an AnimationClip value.
    /// </summary>
    [Serializable]
    public sealed class InlineAnimationClipFunction : IFB_TopLayer<AnimationClip, ISB_FUNCTION<AnimationClip>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<AnimationClip>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.AnimationClip"), "UnityEngine.AnimationClip",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public AnimationClip Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public AnimationClip ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
