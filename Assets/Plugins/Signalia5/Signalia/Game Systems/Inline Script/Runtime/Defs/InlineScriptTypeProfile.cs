using System;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Describes the runtime and generation characteristics for an inline script type.
    /// </summary>
    public readonly struct InlineScriptTypeProfile
    {
        public InlineScriptTypeProfile(Type behaviourBaseType, string boilerplateBaseClassName,
            string returnTypeName, bool hasReturnValue, string methodName)
        {
            BehaviourBaseType = behaviourBaseType ?? throw new ArgumentNullException(nameof(behaviourBaseType));
            BoilerplateBaseClassName = boilerplateBaseClassName ?? throw new ArgumentNullException(nameof(boilerplateBaseClassName));
            ReturnTypeName = returnTypeName ?? throw new ArgumentNullException(nameof(returnTypeName));
            HasReturnValue = hasReturnValue;
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }

        public Type BehaviourBaseType { get; }

        public string BoilerplateBaseClassName { get; }

        public string ReturnTypeName { get; }

        public bool HasReturnValue { get; }

        public string MethodName { get; }

        public static InlineScriptTypeProfile CreateVoidProfile(Type behaviourBaseType, string boilerplateBaseClassName)
        {
            return new InlineScriptTypeProfile(behaviourBaseType, boilerplateBaseClassName, "void", false, "ExecuteCode");
        }

        public static InlineScriptTypeProfile CreateFunctionProfile(Type behaviourBaseType, string boilerplateBaseClassName,
            string returnTypeName, string methodName)
        {
            return new InlineScriptTypeProfile(behaviourBaseType, boilerplateBaseClassName, returnTypeName, true, methodName);
        }
    }
}
