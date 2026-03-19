using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning a CharacterController value.
    /// </summary>
    [Serializable]
    public sealed class InlineCharacterControllerFunction : IFB_TopLayer<CharacterController, ISB_FUNCTION<CharacterController>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<CharacterController>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "UnityEngine.CharacterController"), "UnityEngine.CharacterController",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public CharacterController Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public CharacterController ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
