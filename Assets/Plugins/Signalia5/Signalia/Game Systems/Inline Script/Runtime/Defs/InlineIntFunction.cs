using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Provides inline scripting support for functions returning an int value.
    /// </summary>
    [Serializable]
    public sealed class InlineIntFunction : IFB_TopLayer<int, ISB_FUNCTION<int>>
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateFunctionProfile(typeof(ISB_FUNCTION<int>),
                string.Format(InlineScriptConstants.FunctionBoilerPlateClassFormat, "int"), "int",
                InlineScriptConstants.FunctionMethodName);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        public int Value => ReturnInternal(behaviour => behaviour.Evaluate(), nameof(Value));

        public int ReturnMono(GameObject gameObject)
        {
            return ReturnInternal(behaviour => behaviour.Evaluate(gameObject), nameof(ReturnMono));
        }
    }
}
