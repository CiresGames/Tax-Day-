using System;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils
{
    /// <summary>
    /// Contains constants and definitions used throughout the InlineScript system
    /// </summary>
    public static class InlineScriptConstants
    {
        /// <summary>
        /// C# control flow keywords that don't require semicolons
        /// </summary>
        public static readonly string[] ControlKeywords =
        {
            "if", "else", "for", "foreach", "while", "switch", "case", "default", 
            "try", "catch", "finally", "do", "using"
        };

        public const string GeneratedPrefix = "_ISGENERATED";
        public const string VoidBoilerPlateClass = "ISB_VOID";
        public const string FunctionBoilerPlateClassFormat = "ISB_FUNCTION<{0}>";
        public const string FunctionMethodName = "Evaluate";

        /// <summary>
        /// Compilation status enumeration
        /// </summary>
        public enum CompileStatus
        {
            Unknown,
            Pending,
            Dirty,
            Compiled
        }
    }
}
